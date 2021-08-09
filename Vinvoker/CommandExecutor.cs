using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Storage;
using Vinvoker.Attributes;
using Vinvoker.Implementations;
using Vinvoker.Interfaces;

namespace Vinvoker {
	public class CommandExecutor {
		private Dictionary<string, List<CommandMethodInfo>> CommandMethods { get; set; } = new();

		public Task<string?> Execute(Bot bot, ulong steamID, string message, string[] args) {
			string commandName = args[0];
			if (!CommandMethods.TryGetValue(commandName.ToUpperInvariant(), out List<CommandMethodInfo>? methods)) {
				return Task.FromResult<string?>(null);
			}

			IEnumerable<CommandMethodInfo> suitableMethods = methods.Where(method => method.ArgumentCount == args.Length - 1 - (method.UseBotsSelector ? 1 : 0));

			return suitableMethods.FirstOrDefault()?.ExecuteDelegate(new ASFBot(bot), steamID, message, args[1..]) ?? Task.FromResult<string?>(null);
		}

		public void LoadAssembly(Assembly assembly) {
			Dictionary<string, ICommand> commands = assembly.GetTypes()
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

		internal static CommandMethodInfo? PrepareMethodInfo(MethodInfo methodInfo, ICommand command) {
			Type sourceType = typeof(string);
			if ((methodInfo.ReturnType != sourceType) && (methodInfo.ReturnType != typeof(Task<string>))) {
				ASF.ArchiLogger.LogGenericError($"{methodInfo.Name} has an invalid return type {methodInfo.ReturnType.FullName}!");
				return null;
			}

			List<Attribute> attributes = methodInfo.GetCustomAttributes().ToList();

			ParameterInfo[] arguments = methodInfo.GetParameters();

			byte argumentCount = (byte) arguments.Count(arg => !(((arg.Name?.ToUpperInvariant() == "STEAMID") && (arg.ParameterType == typeof(ulong))) || ((arg.Name?.ToUpperInvariant() == "BOT") && (arg.ParameterType == typeof(IBot)))));
			BotConfig.EAccess permission = attributes.OfType<AccessAttribute>().FirstOrDefault()?.MinimalAccess ?? BotConfig.EAccess.Master;

			DynamicMethod method = new(methodInfo.Name + "Executor", typeof(Task<string>), new[] {typeof(ICommand), typeof(IBot), typeof(ulong), sourceType, typeof(string[])});
			ILGenerator generator = method.GetILGenerator();
			if (permission != BotConfig.EAccess.None) {
				generator.ValidatePermission(permission);
			}

			if (attributes.OfType<BotMustBeConnectedAttribute>().Any()) {
				generator.CheckIfConnected();
			}

			byte argIndex = 0;
			foreach (ParameterInfo argument in arguments) {
				Type targetType = argument.ParameterType;
				LocalBuilder local = generator.DeclareLocal(targetType);

				switch (argument.Name?.ToUpperInvariant()) {
					case "BOT" when targetType == typeof(IBot):
						generator.LoadAndStoreArg(1, local);
						break;
					case "STEAMID" when targetType == typeof(ulong):
						generator.LoadAndStoreArg(2, local);
						break;
					default:
						argIndex++;

						// [TextAttribute] case - parsing string argument as a text (e.g. including spaces), can be declared only for the latest argument
						if ((targetType == sourceType) && IsArgument<TextAttribute>(argument)) {
							generator.LoadArgAsText(argIndex);
							
							generator.StoreArg(local);
							goto argumentsParsed;
						}

						generator.LoadArg(argIndex - 1);

						if (targetType == sourceType) {
							// No processing required
						} else if (argument.ParameterType == typeof(IBot)) {
							// Bot case - we can parse it using GetBot method
							generator.EmitCall(OpCodes.Call, typeof(Bot).GetMethod(nameof(Bot.GetBot))!, null);
						} else if (typeof(HashSet<Bot>).IsAssignableFrom(argument.ParameterType)) {
							// Multiple bots case - we can parse it using Bot.GetBots method, we support any interfaces implemented by HashSet as well by casting]
							generator.EmitCall(OpCodes.Call, typeof(Bot).GetMethod(nameof(Bot.GetBots))!, null);
							if (argument.ParameterType != typeof(HashSet<Bot>)) {
								generator.Emit(OpCodes.Castclass, argument.ParameterType);
							}
						} else if (targetType.IsAssignableFrom(sourceType)) {
							// It's something else - trying to use generic cast at first
							generator.Emit(OpCodes.Castclass, targetType);
						} else {
							// Trying to find custom casting method
							MethodInfo? castMethod = GetCastMethod(targetType, m => m.GetParameters()[0].ParameterType, _ => sourceType) ??
								GetCastMethod(sourceType, _ => targetType, m => m.ReturnType);

							if (castMethod != null) {
								generator.EmitCall(OpCodes.Call, castMethod, null);
							} else {
								// Trying to find TryParse(string, out T) method in order to convert string to target type
								MethodInfo? parseMethod = targetType.GetMethod("TryParse", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy, null,
									new[] {sourceType, targetType.MakeByRefType()}, null);

								if (parseMethod == null) {
									ASF.ArchiLogger.LogGenericError($"Type {targetType.FullName} in {command.CommandName}/{methodInfo.Name} could not be parsed!");
									return null;
								}

								generator.Emit(OpCodes.Ldloca, local.LocalIndex);
								generator.EmitCall(OpCodes.Call, parseMethod, null);
								generator.GenerateInvalidParseBranch(argument.Name!);
								generator.Emit(OpCodes.Ldloc, local.LocalIndex);
							}
						}

						generator.StoreArg(local);

						if (IsArgument<MustBeNonDefaultAttribute>(argument)) {
							generator.Emit(OpCodes.Ldloc, local);
							generator.CheckForDefault(argument);
						}

						break;
				}
			}

			argumentsParsed:
			generator.Emit(OpCodes.Ldarg_0);
			for (int i = 0; i < arguments.Length; i++) {
				generator.Emit(OpCodes.Ldloc, i);
			}

			generator.EmitCall(OpCodes.Callvirt, methodInfo, null);
			if (methodInfo.ReturnType == sourceType) {
				generator.EmitCall(OpCodes.Call, ((Func<string, Task<string>>) Task.FromResult).Method, null);
			}

			generator.Emit(OpCodes.Ret);

			ExecutorFunction function = (ExecutorFunction) method.CreateDelegate(typeof(ExecutorFunction), command);

			bool useBotsSelector = attributes.OfType<UseBotsSelectorAttribute>().Any();
			if (useBotsSelector) {
				ExecutorFunction sourceFunction = function;
				function = (_, id, message, args) => BotSelectorProxy.ResponseBotSelectorProxy(id, message, args, sourceFunction);
			}

			return new CommandMethodInfo(argumentCount, function, permission, useBotsSelector);
		}

		private static MethodInfo? GetCastMethod(IReflect type, Func<MethodInfo, Type> baseType, Func<MethodInfo, Type> derivedType) {
			return type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
				.FirstOrDefault(m => (m.Name is "op_Implicit" or "op_Explicit") && baseType(m).IsAssignableFrom(derivedType(m)));
		}

		private static bool IsArgument<T>(ParameterInfo argument) where T : Attribute {
			return argument.CustomAttributes.Any(attr => attr.AttributeType == typeof(T));
		}
	}
}
