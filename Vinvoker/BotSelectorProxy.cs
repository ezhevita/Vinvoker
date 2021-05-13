using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ArchiSteamFarm;
using ArchiSteamFarm.Localization;

namespace Vinvoker {
	public static class BotSelectorProxy {
		public static async Task<string?> ResponseBotSelectorProxy(ulong steamID, string message, IReadOnlyList<string> args, CommandMethodInfo.ExecutorFunction functionToExecute) {
			if (steamID == 0) {
				ASF.ArchiLogger.LogNullError(nameof(steamID));
				return null;
			}

			string botNames = args[0];

			HashSet<Bot>? bots = Bot.GetBots(botNames);
			if ((bots == null) || (bots.Count == 0)) {
				return ASF.IsOwner(steamID) ? Commands.FormatStaticResponse(string.Format(CultureInfo.CurrentCulture, Strings.BotNotFound, botNames)) : null;
			}
	
			IList<string?> results = await Utilities.InParallel(bots.Select(bot => functionToExecute(bot, steamID, message, args.Skip(1).ToArray()))).ConfigureAwait(false);

			List<string> responses = new(results.Where(result => !string.IsNullOrEmpty(result))!);
			return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
		}
	}
}
