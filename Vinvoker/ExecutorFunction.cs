using System.Threading.Tasks;
using Vinvoker.Interfaces;

namespace Vinvoker {
	public delegate Task<string?> ExecutorFunction(IBot bot, ulong steamID, string message, string[] args);
}
