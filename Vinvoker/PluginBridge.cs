using System;
using System.Collections.Generic;
using System.Composition;
using System.Reflection;
using System.Threading.Tasks;
using ArchiSteamFarm;
using ArchiSteamFarm.Plugins;
using JetBrains.Annotations;
using McMaster.NETCore.Plugins;

namespace Vinvoker {
	[Export(typeof(IPlugin))]
	[UsedImplicitly]
	public class PluginBridge : IBotCommand {
		private Version? CachedVersion { get; set; }
		public void OnLoaded() {
			ASF.ArchiLogger.LogGenericInfo($"{Name} v{Version} | Made by Vital7 | Source code & support: https://github.com/Vital7/Vinvoker");

			ASF.ArchiLogger.LogGenericTrace("Initializing...");
			ASF.ArchiLogger.LogGenericTrace("Loading assemblies...");

			var assemblies = LoadAssemblies();
		}

		private HashSet<Assembly>? LoadAssemblies() {
			var loaders = new List<PluginLoader>();

		}
		
		public string Name => nameof(Vinvoker);
		public Version Version => (CachedVersion ??= Assembly.GetExecutingAssembly().GetName().Version) ?? throw new ArgumentNullException(nameof(Version));

		public async Task<string?> OnBotCommand(Bot bot, ulong steamID, string message, string[] args) => throw new NotImplementedException();
	}
}
