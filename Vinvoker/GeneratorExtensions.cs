using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using ArchiSteamFarm;
using ArchiSteamFarm.Localization;

namespace Vinvoker {
	[SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
	[SuppressMessage("ReSharper", "PossibleNullReferenceException")]
	public static class GeneratorExtensions {
		public static void GenerateInvalidParseBranch(this ILGenerator generator, string parameterName) {
			// if (!stack) {
			//    return Task.FromResult(bot.Commands.FormatBotResponse(string.Format(Strings.ErrorIsInvalid, parameterName)));
			// }
			Label parsedLabel = generator.DefineLabel();
			generator.Emit(OpCodes.Brtrue_S, parsedLabel);
			generator.Emit(OpCodes.Ldarg_1);
			generator.EmitCall(OpCodes.Call, typeof(Strings).GetProperty(nameof(Strings.ErrorIsInvalid), BindingFlags.Static | BindingFlags.Public).GetGetMethod(), null);
			generator.Emit(OpCodes.Ldstr, parameterName);
			generator.EmitCall(OpCodes.Call, ((Func<string, object, string>) string.Format).Method, null);
			generator.Emit(OpCodes.Ldfld, typeof(Bot).GetField(nameof(Bot.Commands), BindingFlags.Instance | BindingFlags.Public));
			generator.EmitCall(OpCodes.Callvirt, typeof(Commands).GetMethod("FormatBotResponse", BindingFlags.Instance | BindingFlags.Public), null);
			generator.EmitCall(OpCodes.Call, ((Func<string, Task<string>>) Task.FromResult).Method, null);
			generator.Emit(OpCodes.Ret);
			generator.MarkLabel(parsedLabel);
		}

		public static void LoadArg(this ILGenerator generator, int argsIndex) {
			// args[argsIndex]
			generator.Emit(OpCodes.Ldarg, 4);
			generator.Emit(OpCodes.Ldc_I4, argsIndex);
			generator.Emit(OpCodes.Ldelem_Ref);
		}

		public static void LoadArgAsText(this ILGenerator generator, int argsIndex) {
			// Utilities.GetArgsAsText(message, argsIndex)
			generator.Emit(OpCodes.Ldarg, 3);
			generator.Emit(OpCodes.Ldc_I4, argsIndex);
			generator.EmitCall(OpCodes.Call, ((Func<string, byte, string>) Utilities.GetArgsAsText).Method, null);
		}

		public static void ValidatePermission(this ILGenerator generator, BotConfig.EPermission permission) {
			// stack = bot.HasPermission(permission)
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Ldarg_2);
			generator.Emit(OpCodes.Ldc_I4, (int) permission);
			generator.EmitCall(OpCodes.Callvirt, typeof(Bot).GetMethod("HasPermission"), null);

			// if (!stack) {
			//     return Task.FromResult<string>(null);
			// }
			Label validPermission = generator.DefineLabel();
			generator.Emit(OpCodes.Brtrue_S, validPermission);
			generator.Emit(OpCodes.Ldnull);
			generator.EmitCall(OpCodes.Call, ((Func<string, Task<string>>) Task.FromResult).Method, null);
			generator.Emit(OpCodes.Ret);
			generator.MarkLabel(validPermission);
		}
	}
}
