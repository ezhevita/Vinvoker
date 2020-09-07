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
		/// <summary>
		/// Generates:
		///
		/// <code>
		///	if (!stack) {
		///		return Task.FromResult(bot.Commands.FormatBotResponse(string.Format(Strings.ErrorIsInvalid, parameterName)));
		///	}
		/// </code>
		/// </summary>
		public static void GenerateInvalidParseBranch(this ILGenerator generator, string parameterName) {
			Label parsedLabel = generator.DefineLabel();
			generator.Emit(OpCodes.Brtrue_S, parsedLabel);
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Ldfld, typeof(Bot).GetField(nameof(Bot.Commands), BindingFlags.Instance | BindingFlags.Public));
			generator.EmitCall(OpCodes.Call, typeof(Strings).GetProperty(nameof(Strings.ErrorIsInvalid), BindingFlags.Static | BindingFlags.Public).GetGetMethod(), null);
			generator.Emit(OpCodes.Ldstr, parameterName);
			generator.EmitCall(OpCodes.Call, ((Func<string, object, string>) string.Format).Method, null);
			generator.EmitCall(OpCodes.Callvirt, typeof(Commands).GetMethod(nameof(Commands.FormatBotResponse), BindingFlags.Instance | BindingFlags.Public), null);
			generator.EmitCall(OpCodes.Call, ((Func<string, Task<string>>) Task.FromResult).Method, null);
			generator.Emit(OpCodes.Ret);
			generator.MarkLabel(parsedLabel);
		}

		/// <summary>
		/// Generates:
		///
		/// <code>
		///	args[argsIndex]
		/// </code>
		/// </summary>
		public static void LoadArg(this ILGenerator generator, int argsIndex) {
			generator.Emit(OpCodes.Ldarg, 4);
			generator.Emit(OpCodes.Ldc_I4, argsIndex);
			generator.Emit(OpCodes.Ldelem_Ref);
		}

		/// <summary>
		/// Generates:
		///
		/// <code>
		///	Utilities.GetArgsAsText(message, argsIndex)
		/// </code>
		/// </summary>
		public static void LoadArgAsText(this ILGenerator generator, int argsIndex) {
			generator.Emit(OpCodes.Ldarg, 3);
			generator.Emit(OpCodes.Ldc_I4, argsIndex);
			generator.EmitCall(OpCodes.Call, ((Func<string, byte, string>) Utilities.GetArgsAsText).Method, null);
		}

		/// <summary>
		/// Generates:
		///
		/// <code>
		/// if (!bot.HasPermission(permission)) {
		///		return Task.FromResult&lt;string&gt;(null);
		///	}
		/// </code>
		/// </summary>
		public static void ValidatePermission(this ILGenerator generator, BotConfig.EPermission permission) {
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Ldarg_2);
			generator.Emit(OpCodes.Ldc_I4, (int) permission);
			generator.EmitCall(OpCodes.Callvirt, typeof(Bot).GetMethod(nameof(Bot.HasPermission)), null);

			Label validPermission = generator.DefineLabel();
			generator.Emit(OpCodes.Brtrue_S, validPermission);
			generator.Emit(OpCodes.Ldnull);
			generator.EmitCall(OpCodes.Call, ((Func<string, Task<string>>) Task.FromResult).Method, null);
			generator.Emit(OpCodes.Ret);
			generator.MarkLabel(validPermission);
		}

		/// <summary>
		/// Generates:
		///
		/// <code>
		/// if (!bot.IsConnectedAndLoggedOn) {
		///		return Task.FromResult&lt;string&gt;(bot.Commands.FormatBotResponse(Strings.BotNotConnected));
		///	}
		/// </code>
		/// </summary>
		public static void CheckIfConnected(this ILGenerator generator) {
			generator.Emit(OpCodes.Ldarg_1);
			generator.EmitCall(OpCodes.Callvirt, typeof(Bot).GetProperty(nameof(Bot.IsConnectedAndLoggedOn)).GetGetMethod(), null);

			Label connectedLabel = generator.DefineLabel();
			generator.Emit(OpCodes.Brtrue_S, connectedLabel);
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Ldfld, typeof(Bot).GetField(nameof(Bot.Commands), BindingFlags.Instance | BindingFlags.Public));
			generator.EmitCall(OpCodes.Call, typeof(Strings).GetProperty(nameof(Strings.BotNotConnected), BindingFlags.Static | BindingFlags.Public).GetGetMethod(), null);
			generator.EmitCall(OpCodes.Callvirt, typeof(Commands).GetMethod(nameof(Commands.FormatBotResponse), BindingFlags.Instance | BindingFlags.Public), null);
			generator.EmitCall(OpCodes.Call, ((Func<string, Task<string>>) Task.FromResult).Method, null);
			
			generator.Emit(OpCodes.Ret);
			generator.MarkLabel(connectedLabel);
		}

		public static void StoreArg(this ILGenerator generator, LocalBuilder local) {
			generator.Emit(OpCodes.Stloc, local.LocalIndex);
		}
		
		public static void LoadAndStoreArg(this ILGenerator generator, int arg, LocalBuilder local) {
			generator.Emit(OpCodes.Ldarg, arg);
			generator.StoreArg(local);
		}
	}
}
