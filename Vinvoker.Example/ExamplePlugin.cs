using System;
using System.Composition;
using System.Reflection;
using System.Threading.Tasks;
using ArchiSteamFarm;
using ArchiSteamFarm.Plugins;

namespace Vinvoker.Example {
	[Export(typeof(IPlugin))]
	public class ExamplePlugin : IBotCommand {
		private CommandExecutor Executor { get; set; }

		public void OnLoaded() {
			ASF.ArchiLogger.LogGenericInfo("This is an example plugin for " + nameof(Vinvoker));
			Executor = new CommandExecutor();
			Executor.Load();
		}

		public string Name => nameof(ExamplePlugin);
		public Version Version => Assembly.GetExecutingAssembly().GetName().Version;

		public Task<string> OnBotCommand(Bot bot, ulong steamID, string message, string[] args) => Executor.Execute(bot, steamID, message, args);
	}
}
