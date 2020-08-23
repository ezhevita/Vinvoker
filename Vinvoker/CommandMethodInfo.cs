using System.Threading.Tasks;
using ArchiSteamFarm;

namespace Vinvoker {
	public class CommandMethodInfo {
		public delegate Task<string> ExecutorFunction(Bot bot, ulong steamID, string message, string[] args);

		public byte ArgumentAmount { get; internal set; }
		public ExecutorFunction ExecuteDelegate { get; internal set; }
		public BotConfig.EPermission Permission { get; internal set; }
		public bool UseBotsSelector { get; internal set; }
	}
}
