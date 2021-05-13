using System;
using ArchiSteamFarm;

namespace Vinvoker.Attributes {
	[AttributeUsage(AttributeTargets.Method)]
	public class PermissionAttribute : Attribute {
		public PermissionAttribute(BotConfig.EAccess minimumPermission) => MinimumPermission = minimumPermission;

		public BotConfig.EAccess MinimumPermission { get; }
	}
}
