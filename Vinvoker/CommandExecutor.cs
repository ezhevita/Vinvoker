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
		private Dictionary<string, List<CommandMethodInfo>> CommandMethods { get; set; } = new Dictionary<string, List<CommandMethodInfo>>();

		public Task<string?> Execute(Bot bot, ulong steamID, string message, string[] args) {
			string commandName = args[0];
			if (!CommandMethods.TryGetValue(commandName.ToUpperInvariant(), out List<CommandMethodInfo>? methods)) {
				return Task.FromResult<string?>(null);
			}

			IEnumerable<CommandMethodInfo> suitableMethods = methods.Where(method => method.ArgumentCount == args.Length - 1 - (method.UseBotsSelector ? 1 : 0));

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

			// Get all the commands variations (all user-declared methods) in each ICommand instance and prepare them
			CommandMethods = commands
			   .Select(command => (command.Key,
					Methods: command.Value.GetType().GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public)
					   .Where(method => !method.IsSpecialName)
					   .Select(method => PrepareMethodInfo(method, command.Value))
					   .Where(result => result != null)))
			   .Where(command => command.Methods.Any())
			   .ToDictionary(command => command.Key.ToUpperInvariant(), command => command.Methods.ToList())!;
		}

		private CommandMethodInfo? PrepareMethodInfo(MethodInfo methodInfo, ICommand command) {
			if ((methodInfo.ReturnType != typeof(string)) && (methodInfo.ReturnType != typeof(Task<string>))) {
				ASF.ArchiLogger.LogGenericError($"{methodInfo.Name} has an invalid return type {methodInfo.ReturnType.FullName}!");
				return null;
			}

			List<Attribute> attributes = methodInfo.GetCustomAttributes().ToList();

			ParameterInfo[] arguments = methodInfo.GetParameters();

			byte argumentCount = (byte) arguments.Count(arg => !(((arg.Name?.ToUpperInvariant() == "STEAMID") && (arg.ParameterType == typeof(ulong))) || ((arg.Name?.ToUpperInvariant() == "BOT") && (arg.ParameterType == typeof(Bot)))));
			BotConfig.EPermission permission = attributes.OfType<PermissionAttribute>().FirstOrDefault()?.MinimumPermission ?? BotConfig.EPermission.None;

			DynamicMethod method = new DynamicMethod(methodInfo.Name + "Executor", typeof(Task<string>), new[] {typeof(ICommand), typeof(Bot), typeof(ulong), typeof(string), typeof(string[])});
			ILGenerator generator = method.GetILGenerator();
			if (permission != BotConfig.EPermission.None) {
				generator.ValidatePermission(permission);
			}

			if (attributes.OfType<BotMustBeConnectedAttribute>().Any()) {
				generator.CheckIfConnected();
			}

			byte argIndex = 0;
			foreach (ParameterInfo argument in arguments) {
				LocalBuilder local = generator.DeclareLocal(argument.ParameterType);

				switch (argument.Name?.ToUpperInvariant()) {
					case "BOT" when argument.ParameterType == typeof(Bot):
						generator.LoadAndStoreArg(1, local);
						break;
					case "STEAMID" when argument.ParameterType == typeof(ulong):
						generator.LoadAndStoreArg(2, local);
						break;
					default:
						argIndex++;

						// [TextArgumentAttribute] case - parsing string argument as a text (e.g. including spaces), can be declared only for the latest argument
						if ((argument.ParameterType == typeof(string)) && IsArgument<TextAttribute>(argument)) {
							generator.LoadArgAsText(argIndex - 1);
							
							generator.StoreArg(local);
							goto argumentsParsed;
						}

						generator.LoadArg(argIndex - 1);

						if (argument.ParameterType == typeof(string)) {
							if (IsArgument<NonDefaultValueAttribute>(argument)) {
								generator.CheckForDefault(argument);
							}

							// string case - save it as it is
							generator.StoreArg(local);
						} else if (argument.ParameterType == typeof(Bot)) {
							// Bot case - we can parse it using Bot.GetBot method
							generator.EmitCall(OpCodes.Call, ((Func<string, Bot?>) Bot.GetBot).Method, null);
							if (IsArgument<NonDefaultValueAttribute>(argument)) {
								generator.CheckForDefault(argument);
							}

							generator.StoreArg(local);
						} else if (argument.ParameterType.IsAssignableFrom(typeof(HashSet<Bot>))) {
							// Multiple bots case - we can parse it using Bot.GetBots method, we support any interfaces implemented by HashSet as well by casting
							generator.EmitCall(OpCodes.Call, ((Func<string, HashSet<Bot>?>) Bot.GetBots).Method, null);
							if (argument.ParameterType != typeof(HashSet<Bot>)) {
								generator.Emit(OpCodes.Castclass, argument.ParameterType);
							}

							if (IsArgument<NonDefaultValueAttribute>(argument)) {
								generator.CheckForDefault(argument);
							}

							generator.StoreArg(local);
						} else {
							// It's something else - we can try to find TryParse(string, out T) method in order to convert string to target type
							MethodInfo? parseMethod = argument.ParameterType.GetMethod("TryParse", BindingFlags.Static | BindingFlags.Public, null,
								new[] {typeof(string), argument.ParameterType.MakeByRefType()}, null);

							if (parseMethod == null) {
								ASF.ArchiLogger.LogGenericError("Type " + argument.ParameterType.FullName + " could not be parsed!");
								return null;
							}

							generator.Emit(OpCodes.Ldloca, local.LocalIndex);
							generator.EmitCall(OpCodes.Call, parseMethod, null);
							if (IsArgument<NonDefaultValueAttribute>(argument)) {
								generator.Emit(OpCodes.Ldloc, local.LocalIndex);
								generator.CheckForDefault(argument);
							}

							generator.GenerateInvalidParseBranch(argument.Name!);
						}

						break;
				}
			}

			argumentsParsed:
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

			bool useBotsSelector = attributes.OfType<UseBotsSelectorAttribute>().Any();
			if (useBotsSelector) {
				CommandFunction sourceFunction = function;
				function = (bot, id, message, args) => BotSelectorProxy.ResponseBotSelectorProxy(id, message, args, sourceFunction);
			}

			return new CommandMethodInfo(argumentCount, function, permission, useBotsSelector);
		}

		private static bool IsArgument<T>(ParameterInfo argument) where T : Attribute {
			return argument.CustomAttributes.Any(attr => attr.AttributeType == typeof(T));
		}
	}
}
