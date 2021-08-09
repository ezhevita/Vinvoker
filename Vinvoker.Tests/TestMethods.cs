using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading.Tasks;
using ArchiSteamFarm.Steam.Storage;
using Vinvoker.Attributes;
using Vinvoker.Interfaces;
using Vinvoker.Tests.Helpers;

namespace Vinvoker.Tests {
	[SuppressMessage("ReSharper", "CA1822")]
	[SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
	public class TestMethods : ICommand {
		private const string Response = "Done";

		public string CommandName => "test";

		public string BotArg(IBot bot) => bot.BotName;

		public string IntAndDefaultArgs(IBot bot, ulong steamID, int param) => string.Join('/', bot.BotName, steamID, param);

		public string IntArg(int param) => param.ToString(CultureInfo.InvariantCulture);

		public string IntNonDefaultArg([MustBeNonDefault] int param) => param.ToString(CultureInfo.InvariantCulture);

		public int InvalidReturnType() => 123;

		public string ImpossibleCast(ICommand command) => command.CommandName;

		public string NoArgs() => Response;

		[Access(BotConfig.EAccess.None)]
		public string NoArgsAccessNone() => Response;

		[BotMustBeConnected]
		public string NoArgsMustBeConnected() => Response;

		public Task<string> NoArgsTask() => Task.FromResult(Response);

		public string ObjectArg(object obj) => obj.ToString();

		public string SteamIDArg(ulong steamID) => steamID.ToString(CultureInfo.InvariantCulture);

		public string StringAndDefaultArgs(IBot bot, ulong steamID, string param) => string.Join('/', bot.BotName, steamID, param);

		public string StringArg(string param) => param;

		public string StringNonDefaultArg([MustBeNonDefault] string param) => param;

		public string StringTextArg([Text] string param) => param;

		public string StringWrapperArg(StringWrapper param) => param.ToString();
	}
}
