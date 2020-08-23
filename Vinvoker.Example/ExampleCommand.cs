using System.Threading.Tasks;
using ArchiSteamFarm;
using Vinvoker.Attributes;
using Vinvoker.Interfaces;

namespace Vinvoker.Example {
	public class ExampleCommand : ICommand {
		public string CommandName => "example";

		// !example [from any account]
		public Task<string> Command() => Task.FromResult(nameof(Command) + " executed!");

		[Permission(BotConfig.EPermission.Master)]
		[UseBotsSelector]
		public Task<string> CommandWithParsing(Bot bot, int arg) => Task.FromResult($"Executed from bot {bot.BotName} with arg {arg}");

		// !example [from Master account]
		[Permission(BotConfig.EPermission.Master)]
		public Task<string> CommandWithPermission() => Task.FromResult(nameof(CommandWithPermission) + " executed from master!");

		// !example test [from Master account]
		[Permission(BotConfig.EPermission.Master)]
		public Task<string> CommandWithPermissionAndArgument(string arg) => Task.FromResult(nameof(CommandWithPermissionAndArgument) + " executed with argument " + arg);
	}
}
