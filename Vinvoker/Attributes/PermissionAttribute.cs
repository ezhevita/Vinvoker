using System;
using ArchiSteamFarm;

namespace Vinvoker.Attributes {
	[AttributeUsage(AttributeTargets.Method)]
	public class PermissionAttribute : Attribute {
		public PermissionAttribute(BotConfig.EPermission minimumPermission) => MinimumPermission = minimumPermission;

		public BotConfig.EPermission MinimumPermission { get; }
	}
}
