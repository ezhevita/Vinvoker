using System.Collections.Generic;

namespace Vinvoker.Interfaces {
	public interface IBotProvider {
		IBot? GetBot(string botName);
		IList<IBot>? GetBots(string botNames);
	}
}
