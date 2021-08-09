using System;
using System.Collections.Generic;
using System.Composition;
using System.Reflection;
using System.Threading.Tasks;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Plugins.Interfaces;
using ArchiSteamFarm.Steam;
using JetBrains.Annotations;

namespace Vinvoker {
	[Export(typeof(IPlugin))]
	[UsedImplicitly]
	public class PluginBridge : IBotCommand {
		private Version? CachedVersion { get; set; }
		public void OnLoaded() {
			ASF.ArchiLogger.LogGenericInfo($"{Name} v{Version} | Made by Vital7 | Source code & support: https://github.com/Vital7/Vinvoker");

			ASF.ArchiLogger.LogGenericTrace("Initializing...");
			ASF.ArchiLogger.LogGenericTrace("Loading assemblies...");

			HashSet<Assembly>? assemblies = LoadAssemblies();
		}

		private HashSet<Assembly>? LoadAssemblies() {
			return null;
		}
		
		public string Name => nameof(Vinvoker);
		public Version Version => (CachedVersion ??= Assembly.GetExecutingAssembly().GetName().Version) ?? throw new ArgumentNullException(nameof(Version));

		public async Task<string?> OnBotCommand(Bot bot, ulong steamID, string message, string[] args) => throw new NotImplementedException();
	}
}
