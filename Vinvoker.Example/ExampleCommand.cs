using ArchiSteamFarm;
using Vinvoker.Attributes;
using Vinvoker.Interfaces;

namespace Vinvoker.Example {
	public class ExampleCommand : ICommand {
		public string CommandName => "example";

		// !example [from any account]
		[Permission(BotConfig.EAccess.None)]
		public string Command() => nameof(Command) + " executed!";

		[BotMustBeConnected]
		[UseBotsSelector]
		public string CommandWithParsing(Bot bot, int arg) => $"Executed from bot {bot.BotName} with arg {arg}";

		// !example [from Master account]
		public string CommandWithPermission() => nameof(CommandWithPermission) + " executed from master!";

		// !example test [from Master account]
		public string CommandWithPermissionAndArgument(string arg) => nameof(CommandWithPermissionAndArgument) + " executed with argument " + arg;
	}
}
