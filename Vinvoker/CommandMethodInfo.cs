using ArchiSteamFarm.Steam.Storage;

namespace Vinvoker {
	public class CommandMethodInfo {
		public CommandMethodInfo(byte argumentCount, ExecutorFunction executeDelegate, BotConfig.EAccess permission, bool useBotsSelector) {
			ArgumentCount = argumentCount;
			ExecuteDelegate = executeDelegate;
			Permission = permission;
			UseBotsSelector = useBotsSelector;
		}

		public byte ArgumentCount { get; }
		public ExecutorFunction ExecuteDelegate { get; }
		public BotConfig.EAccess Permission { get; }
		public bool UseBotsSelector { get; }
	}
}
