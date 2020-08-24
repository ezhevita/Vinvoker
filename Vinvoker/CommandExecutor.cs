using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using ArchiSteamFarm;
using Vinvoker.Attributes;
using Vinvoker.Interfaces;
using CommandFunction = Vinvoker.CommandMethodInfo.ExecutorFunction;

namespace Vinvoker {
	public class CommandExecutor {
		private Dictionary<string, List<CommandMethodInfo>> CommandMethods { get; set; }

		public Task<string> Execute(Bot bot, ulong steamID, string message, string[] args) {
			string commandName = args[0];
			if (!CommandMethods.TryGetValue(commandName.ToUpperInvariant(), out List<CommandMethodInfo> methods)) {
				return Task.FromResult<string>(null);
			}

			IEnumerable<CommandMethodInfo> suitableMethods = methods.Where(method => method.ArgumentAmount == args.Length - 1 - (method.UseBotsSelector ? 1 : 0));

			return suitableMethods.First().ExecuteDelegate(bot, steamID, message, args.Skip(1).ToArray());
		}

		public void Load() {
			if (CommandMethods != null) {
				throw new InvalidOperationException(nameof(CommandMethods) + " are already initialized!");
			}

			Dictionary<string, ICommand> commands = Assembly.GetCallingAssembly().GetTypes()
			   .Where(type => typeof(ICommand).IsAssignableFrom(type) && !type.IsAbstract)
			   .Select(Activator.CreateInstance)
			   .Cast<ICommand>().ToDictionary(command => command.CommandName, command => command);

			CommandMethods = commands
			   .Select(command => (command.Key,
					Methods: command.Value.GetType().GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public)
					   .Where(method => method.Name != "get_CommandName")
					   .Select(method => ParseMethodInfo(method, command.Value))
					   .Where(result => result != null)))
			   .Where(command => command.Methods.Any())
			   .ToDictionary(command => command.Key.ToUpperInvariant(), command => command.Methods.ToList());
		}

		private CommandMethodInfo ParseMethodInfo(MethodInfo methodInfo, ICommand command) {
			if ((methodInfo.ReturnType != typeof(string)) && (methodInfo.ReturnType != typeof(Task<string>))) {
				ASF.ArchiLogger.LogGenericError($"{methodInfo.Name} has an invalid return type {methodInfo.ReturnType.FullName}!");
				return null;
			}

			List<Attribute> attributes = methodInfo.GetCustomAttributes().ToList();

			ParameterInfo[] arguments = methodInfo.GetParameters();

			CommandMethodInfo cmi = new CommandMethodInfo {
				ArgumentAmount = (byte) arguments.Count(arg => !(((arg.Name?.ToUpperInvariant() == "STEAMID") && (arg.ParameterType == typeof(ulong))) || ((arg.Name?.ToUpperInvariant() == "BOT") && (arg.ParameterType == typeof(Bot))))),
				Permission = attributes.OfType<PermissionAttribute>().FirstOrDefault()?.MinimumPermission ?? BotConfig.EPermission.None,
				UseBotsSelector = attributes.OfType<UseBotsSelectorAttribute>().Any()
			};

			DynamicMethod method = new DynamicMethod(methodInfo.Name + "Executor", typeof(Task<string>), new[] {typeof(ICommand), typeof(Bot), typeof(ulong), typeof(string), typeof(string[])});
			ILGenerator generator = method.GetILGenerator();
			if (cmi.Permission != BotConfig.EPermission.None) {
				generator.ValidatePermission(cmi.Permission);
			}

			byte index = 0;
			foreach (ParameterInfo argument in arguments) {
				LocalBuilder local = generator.DeclareLocal(argument.ParameterType);

				switch (argument.Name?.ToUpperInvariant()) {
					case "BOT" when argument.ParameterType == typeof(Bot):
						generator.Emit(OpCodes.Ldarg, 1);
						generator.Emit(OpCodes.Stloc, local.LocalIndex);
						break;
					case "STEAMID" when argument.ParameterType == typeof(ulong):
						generator.Emit(OpCodes.Ldarg, 2);
						generator.Emit(OpCodes.Stloc, local.LocalIndex);
						break;
					default:
						index++;
						if ((argument.ParameterType == typeof(string)) && argument.CustomAttributes.Any(attr => attr.AttributeType == typeof(TextArgumentAttribute))) {
							generator.LoadArgAsText(index - 1);
							generator.Emit(OpCodes.Stloc, local.LocalIndex);
							goto end;
						}

						generator.LoadArg(index - 1);
						if (argument.ParameterType == typeof(string)) {
							generator.Emit(OpCodes.Stloc, local.LocalIndex);
							break;
						}

						generator.Emit(OpCodes.Ldloca, local.LocalIndex);
						MethodInfo parseMethod = argument.ParameterType.GetMethod("TryParse", BindingFlags.Static | BindingFlags.Public, null,
							new[] {typeof(string), argument.ParameterType.MakeByRefType()}, null);

						if (parseMethod == null) {
							ASF.ArchiLogger.LogGenericError("Type " + argument.ParameterType.FullName + " could not be parsed!");
							return null;
						}

						generator.EmitCall(OpCodes.Call, parseMethod, null);
						generator.GenerateInvalidParseBranch(argument.Name);

						break;
				}
			}

			end:
			generator.Emit(OpCodes.Ldarg_0);
			for (int i = 0; i < arguments.Length; i++) {
				generator.Emit(OpCodes.Ldloc_S, i);
			}

			generator.EmitCall(OpCodes.Callvirt, methodInfo, null);
			if (methodInfo.ReturnType == typeof(string)) {
				generator.EmitCall(OpCodes.Call, ((Func<string, Task<string>>) Task.FromResult).Method, null);
			}

			generator.Emit(OpCodes.Ret);

			CommandFunction function = (CommandFunction) method.CreateDelegate(typeof(CommandFunction), command);
			if (cmi.UseBotsSelector) {
				BotSelectorHelper botSelectorHelper = new BotSelectorHelper {
					FunctionToExecute = function
				};

				cmi.ExecuteDelegate = botSelectorHelper.ResponseBotSelector;
			} else {
				cmi.ExecuteDelegate = function;
			}

			return cmi;
		}
	}
}
