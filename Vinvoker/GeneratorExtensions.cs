using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam.Interaction;
using ArchiSteamFarm.Steam.Storage;
using Vinvoker.Interfaces;

#pragma warning disable 8602
#pragma warning disable 8604

namespace Vinvoker {
	[SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
	public static class GeneratorExtensions {
		/// <summary>
		/// Generates:
		/// <br/>
		/// if <c>stack is string</c>:
		/// <code>
		///	if (string.IsNullOrEmpty(string)) {
		/// 	return Task.FromResult(bot.Commands.FormatBotResponse(string.Format(Strings.ErrorIsInvalid, parameterName)));
		/// }
		/// </code>
		/// else:
		/// <br/>
		/// <code>
		///	if (stack == default) {
		/// 	return Task.FromResult(bot.Commands.FormatBotResponse(string.Format(Strings.ErrorIsInvalid, parameterName)));
		/// }
		/// </code>
		/// </summary>
		public static void CheckForDefault(this ILGenerator generator, ParameterInfo parameterInfo) {
			if (parameterInfo.ParameterType == typeof(string)) {
				generator.EmitCall(OpCodes.Call, typeof(string).GetMethod(nameof(string.IsNullOrEmpty), BindingFlags.Static | BindingFlags.Public), null);
				generator.Emit(OpCodes.Ldc_I4_0);
				generator.Emit(OpCodes.Ceq);
			}

			Label nonDefaultValue = generator.DefineLabel();
			generator.Emit(OpCodes.Brtrue_S, nonDefaultValue);
			
			generator.GenerateResponse(nameof(Strings.ErrorIsInvalid), parameterInfo.Name);
			generator.MarkLabel(nonDefaultValue);
		}

		/// <summary>
		/// Generates:
		///
		/// <code>
		///	if (!stack) {
		///		return Task.FromResult(Commands.FormatStaticResponse(string.Format(Strings.ErrorIsInvalid, parameterName)));
		///	}
		/// </code>
		/// </summary>
		public static void GenerateInvalidParseBranch(this ILGenerator generator, string parameterName) {
			Label parsedLabel = generator.DefineLabel();
			generator.Emit(OpCodes.Brtrue_S, parsedLabel);
			generator.GenerateResponse(nameof(Strings.ErrorIsInvalid), parameterName);
			generator.MarkLabel(parsedLabel);
		}

		private static void GenerateResponse(this ILGenerator generator, string responseName, string? argument = null) {
			generator.Emit(OpCodes.Ldarg_2);
			generator.EmitCall(OpCodes.Call, typeof(ASF).GetMethod(nameof(ASF.IsOwner), BindingFlags.Static | BindingFlags.Public), null);
			Label notNullLabel = generator.DefineLabel();
			Label taskFromResultLabel = generator.DefineLabel();
			generator.Emit(OpCodes.Brtrue_S, notNullLabel);
			generator.Emit(OpCodes.Ldnull);
			generator.Emit(OpCodes.Br_S, taskFromResultLabel);
			generator.MarkLabel(notNullLabel);
			if (!string.IsNullOrEmpty(argument)) {
				generator.EmitCall(OpCodes.Call, typeof(CultureInfo).GetProperty(nameof(CultureInfo.CurrentCulture), BindingFlags.Static | BindingFlags.Public).GetGetMethod(), null);
			}

			generator.EmitCall(OpCodes.Call, typeof(Strings).GetProperty(responseName, BindingFlags.Static | BindingFlags.Public).GetGetMethod(), null);

			if (!string.IsNullOrEmpty(argument)) {
				generator.Emit(OpCodes.Ldstr, argument);
				generator.EmitCall(OpCodes.Call, ((Func<CultureInfo, string, object, string>) string.Format).Method, null);
			}

			generator.EmitCall(OpCodes.Call, typeof(Commands).GetMethod(nameof(Commands.FormatStaticResponse), BindingFlags.Static | BindingFlags.Public), null);
			generator.MarkLabel(taskFromResultLabel);
			generator.EmitCall(OpCodes.Call, ((Func<string, Task<string>>) Task.FromResult).Method, null);
			generator.Emit(OpCodes.Ret);
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
		public static void ValidatePermission(this ILGenerator generator, BotConfig.EAccess permission) {
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Ldarg_2);
			generator.Emit(OpCodes.Ldc_I4, (int) permission);
			generator.EmitCall(OpCodes.Callvirt, typeof(IBot).GetMethod(nameof(IBot.HasAccess)), null);

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
			generator.EmitCall(OpCodes.Callvirt, typeof(IBot).GetProperty(nameof(IBot.IsConnectedAndLoggedOn)).GetGetMethod(), null);

			Label connectedLabel = generator.DefineLabel();
			generator.Emit(OpCodes.Brtrue_S, connectedLabel);
			
			generator.GenerateResponse(nameof(Strings.BotNotConnected));
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
