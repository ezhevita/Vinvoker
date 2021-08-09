using System.Collections.Generic;
using System.Linq;
using ArchiSteamFarm.Steam;
using Vinvoker.Interfaces;

namespace Vinvoker.Implementations {
	public class ASFBotProvider : IBotProvider {
		public IBot? GetBot(string botName) {
			var result = Bot.GetBot(botName);
			return result != null ? new ASFBot(result) : null;
		}

		public IList<IBot>? GetBots(string botNames) {
			var result = Bot.GetBots(botNames);
			return result?.Select(x => (IBot) new ASFBot(x)).ToList();
		}
	}
}
