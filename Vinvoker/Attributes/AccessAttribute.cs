using System;
using ArchiSteamFarm.Steam.Storage;

namespace Vinvoker.Attributes {
	[AttributeUsage(AttributeTargets.Method)]
	public class AccessAttribute : Attribute {
		public AccessAttribute(BotConfig.EAccess minimalAccess) => MinimalAccess = minimalAccess;

		public BotConfig.EAccess MinimalAccess { get; }
	}
}
