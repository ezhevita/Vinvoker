using System.Diagnostics.CodeAnalysis;
using ArchiSteamFarm.Core;
using HarmonyLib;
using JetBrains.Annotations;

namespace Vinvoker.Tests.Patchers {
	[SuppressMessage("ReSharper", "RedundantAssignment")]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	[UsedImplicitly]
	[HarmonyPatch(typeof(ASF), nameof(ASF.IsOwner))]
	public static class ASFIsOwnerPatcher {
		private const ulong SteamID = (1UL << 56) + (1UL << 52) + (1UL << 32) + PrepareMethodInfoTests.AccountID;

		[UsedImplicitly]
		public static bool Prefix(ulong steamID, ref bool __result) {
			__result = steamID == SteamID;

			return false;
		}
	}
}
