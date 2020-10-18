using System.Threading.Tasks;
using ArchiSteamFarm;

namespace Vinvoker {
	public class CommandMethodInfo {
		public delegate Task<string?> ExecutorFunction(Bot bot, ulong steamID, string message, string[] args);

		public CommandMethodInfo(byte argumentCount, ExecutorFunction executeDelegate, BotConfig.EPermission permission, bool useBotsSelector) {
			ArgumentCount = argumentCount;
			ExecuteDelegate = executeDelegate;
			Permission = permission;
			UseBotsSelector = useBotsSelector;
		}

		public byte ArgumentCount { get; }
		public ExecutorFunction ExecuteDelegate { get; }
		public BotConfig.EPermission Permission { get; }
		public bool UseBotsSelector { get; }
	}
}
