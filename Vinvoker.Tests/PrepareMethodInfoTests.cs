using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam.Interaction;
using ArchiSteamFarm.Steam.Storage;
using HarmonyLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SteamKit2;
using Vinvoker.Interfaces;
using Vinvoker.Tests.Helpers;

namespace Vinvoker.Tests {
	[TestClass]
	public class PrepareMethodInfoTests {
		public const uint AccountID = 12345;
		private ulong OwnerSteamID;
		private TestMethods TestCommand;

		[AssemblyInitialize]
		public static void AssemblySetup(TestContext context) {
			Harmony harmony = new("tests.Vinvoker");
			while (!Debugger.IsAttached) {
				Thread.Sleep(100);
			}
			Debugger.Break();
			harmony.PatchAll();
		}

		[TestMethod]
		public void BotArg() {
			const string botName = "bot";
			Mock<IBot> bot = new();
			bot.Setup(x => x.HasAccess(OwnerSteamID, BotConfig.EAccess.Master)).Returns(true);
			bot.Setup(x => x.BotName).Returns(botName);

			Func<IBot, string> method = TestCommand.BotArg;
			CommandMethodInfo commandMethodInfo = CommandExecutor.PrepareMethodInfo(method.Method, TestCommand);
			Assert.IsNotNull(commandMethodInfo);
			Assert.AreEqual(0, commandMethodInfo.ArgumentCount);
			Assert.AreEqual(BotConfig.EAccess.Master, commandMethodInfo.Permission);

			Task<string> result = ExecuteCommand(commandMethodInfo, bot, OwnerSteamID);

			Assert.AreEqual(botName, result.Result);
		}

		private Task<string> ExecuteCommand(CommandMethodInfo commandMethodInfo, IMock<IBot> bot, ulong steamID, params string[] args) {
			Assert.IsNotNull(commandMethodInfo);
			Task<string> result = commandMethodInfo!.ExecuteDelegate(bot.Object, steamID, TestCommand.CommandName + (args.Length > 0 ? " " + string.Join(' ', args) : ""), args);

			return result;
		}

		[TestMethod]
		public void IntAndDefaultArgs() {
			const string botName = "bot";
			const int param = 123;
			Mock<IBot> bot = new();
			bot.Setup(x => x.HasAccess(OwnerSteamID, BotConfig.EAccess.Master)).Returns(true);
			bot.Setup(x => x.BotName).Returns(botName);

			Func<IBot, ulong, int, string> method = TestCommand.IntAndDefaultArgs;
			CommandMethodInfo commandMethodInfo = CommandExecutor.PrepareMethodInfo(method.Method, TestCommand);
			Assert.IsNotNull(commandMethodInfo);
			Assert.AreEqual(1, commandMethodInfo.ArgumentCount);
			Assert.AreEqual(BotConfig.EAccess.Master, commandMethodInfo.Permission);

			Task<string> result = ExecuteCommand(commandMethodInfo, bot, OwnerSteamID, param.ToString(CultureInfo.InvariantCulture));

			Assert.AreEqual(botName + "/" + OwnerSteamID + "/" + param, result.Result);
		}

		[TestMethod]
		public void IntArg() {
			const int param = 123;
			Mock<IBot> bot = new();
			bot.Setup(x => x.HasAccess(OwnerSteamID, BotConfig.EAccess.Master)).Returns(true);

			Func<int, string> method = TestCommand.IntArg;
			CommandMethodInfo commandMethodInfo = CommandExecutor.PrepareMethodInfo(method.Method, TestCommand);
			Assert.IsNotNull(commandMethodInfo);
			Assert.AreEqual(1, commandMethodInfo.ArgumentCount);
			Assert.AreEqual(BotConfig.EAccess.Master, commandMethodInfo.Permission);

			Task<string> result = ExecuteCommand(commandMethodInfo, bot, OwnerSteamID, param.ToString(CultureInfo.InvariantCulture));

			Assert.AreEqual(param.ToString(CultureInfo.InvariantCulture), result.Result);
		}

		[TestMethod]
		public void IntNonDefaultArg() {
			const int param = 123;
			Mock<IBot> bot = new();
			bot.Setup(x => x.HasAccess(OwnerSteamID, BotConfig.EAccess.Master)).Returns(true);

			Func<int, string> method = TestCommand.IntNonDefaultArg;
			CommandMethodInfo commandMethodInfo = CommandExecutor.PrepareMethodInfo(method.Method, TestCommand);
			Assert.IsNotNull(commandMethodInfo);
			Assert.AreEqual(1, commandMethodInfo.ArgumentCount);
			Assert.AreEqual(BotConfig.EAccess.Master, commandMethodInfo.Permission);

			Task<string> result = ExecuteCommand(commandMethodInfo, bot, OwnerSteamID, param.ToString(CultureInfo.InvariantCulture));

			Assert.AreEqual(param.ToString(CultureInfo.InvariantCulture), result.Result);
		}

		[TestMethod]
		public void IntNonDefaultButDefaultArg() {
			const int param = 0;
			Mock<IBot> bot = new();
			bot.Setup(x => x.HasAccess(OwnerSteamID, BotConfig.EAccess.Master)).Returns(true);

			Func<int, string> method = TestCommand.IntNonDefaultArg;
			CommandMethodInfo commandMethodInfo = CommandExecutor.PrepareMethodInfo(method.Method, TestCommand);
			Assert.IsNotNull(commandMethodInfo);
			Assert.AreEqual(1, commandMethodInfo.ArgumentCount);
			Assert.AreEqual(BotConfig.EAccess.Master, commandMethodInfo.Permission);

			Task<string> result = ExecuteCommand(commandMethodInfo, bot, OwnerSteamID, param.ToString(CultureInfo.InvariantCulture));

			Assert.AreEqual(Commands.FormatStaticResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsInvalid, nameof(param))), result.Result);
		}

		[TestMethod]
		public void InvalidReturnType() {
			Func<int> method = TestCommand.InvalidReturnType;
			CommandMethodInfo commandMethodInfo = CommandExecutor.PrepareMethodInfo(method.Method, TestCommand);
			Assert.IsNull(commandMethodInfo);
		}

		[TestMethod]
		public void ImpossibleCast() {
			Func<ICommand, string> method = TestCommand.ImpossibleCast;
			CommandMethodInfo commandMethodInfo = CommandExecutor.PrepareMethodInfo(method.Method, TestCommand);
			Assert.IsNull(commandMethodInfo);
		}

		[TestMethod]
		public void NoArgs() {
			Mock<IBot> bot = new();
			bot.Setup(x => x.HasAccess(OwnerSteamID, BotConfig.EAccess.Master)).Returns(true);

			Func<string> method = TestCommand.NoArgs;
			CommandMethodInfo commandMethodInfo = CommandExecutor.PrepareMethodInfo(method.Method, TestCommand);
			Assert.IsNotNull(commandMethodInfo);
			Assert.AreEqual(0, commandMethodInfo.ArgumentCount);
			Assert.AreEqual(BotConfig.EAccess.Master, commandMethodInfo.Permission);

			Task<string> result = ExecuteCommand(commandMethodInfo, bot, OwnerSteamID);

			Assert.AreEqual(method(), result.Result);
		}

		[TestMethod]
		public void NoArgsConnected() {
			Mock<IBot> bot = new();
			bot.Setup(x => x.HasAccess(OwnerSteamID, BotConfig.EAccess.Master)).Returns(true);
			bot.Setup(x => x.IsConnectedAndLoggedOn).Returns(true);

			Func<string> method = TestCommand.NoArgsMustBeConnected;
			CommandMethodInfo commandMethodInfo = CommandExecutor.PrepareMethodInfo(method.Method, TestCommand);
			Assert.IsNotNull(commandMethodInfo);
			Assert.AreEqual(0, commandMethodInfo.ArgumentCount);
			Assert.AreEqual(BotConfig.EAccess.Master, commandMethodInfo.Permission);

			Task<string> result = ExecuteCommand(commandMethodInfo, bot, OwnerSteamID);

			Assert.AreEqual(method(), result.Result);
		}

		[TestMethod]
		public void NoArgsNoAccess() {
			Mock<IBot> bot = new();
			bot.Setup(x => x.HasAccess(OwnerSteamID, BotConfig.EAccess.Master)).Returns(false);

			Func<string> method = TestCommand.NoArgs;
			CommandMethodInfo commandMethodInfo = CommandExecutor.PrepareMethodInfo(method.Method, TestCommand);
			Assert.IsNotNull(commandMethodInfo);
			Assert.AreEqual(0, commandMethodInfo.ArgumentCount);
			Assert.AreEqual(BotConfig.EAccess.Master, commandMethodInfo.Permission);

			Task<string> result = ExecuteCommand(commandMethodInfo, bot, OwnerSteamID);

			Assert.IsNull(result.Result);
		}

		[TestMethod]
		public void NoArgsNone() {
			Mock<IBot> bot = new();

			Func<string> method = TestCommand.NoArgsAccessNone;
			CommandMethodInfo commandMethodInfo = CommandExecutor.PrepareMethodInfo(method.Method, TestCommand);
			Assert.IsNotNull(commandMethodInfo);
			Assert.AreEqual(0, commandMethodInfo.ArgumentCount);
			Assert.AreEqual(BotConfig.EAccess.None, commandMethodInfo.Permission);

			Task<string> result = ExecuteCommand(commandMethodInfo, bot, OwnerSteamID);

			Assert.AreEqual(method(), result.Result);
		}

		[TestMethod]
		public void NoArgsNotConnected() {
			Mock<IBot> bot = new();
			bot.Setup(x => x.HasAccess(OwnerSteamID, BotConfig.EAccess.Master)).Returns(true);
			bot.Setup(x => x.IsConnectedAndLoggedOn).Returns(false);

			Func<string> method = TestCommand.NoArgsMustBeConnected;
			CommandMethodInfo commandMethodInfo = CommandExecutor.PrepareMethodInfo(method.Method, TestCommand);
			Assert.IsNotNull(commandMethodInfo);
			Assert.AreEqual(0, commandMethodInfo.ArgumentCount);
			Assert.AreEqual(BotConfig.EAccess.Master, commandMethodInfo.Permission);

			Task<string> result = ExecuteCommand(commandMethodInfo, bot, OwnerSteamID);

			Assert.AreEqual(Commands.FormatStaticResponse(Strings.BotNotConnected), result.Result);
		}

		[TestMethod]
		public void NoArgsNotConnectedNotOwner() {
			ulong steamID = OwnerSteamID + 1;
			Mock<IBot> bot = new();
			bot.Setup(x => x.HasAccess(steamID, BotConfig.EAccess.Master)).Returns(true);
			bot.Setup(x => x.IsConnectedAndLoggedOn).Returns(false);

			Func<string> method = TestCommand.NoArgsMustBeConnected;
			CommandMethodInfo commandMethodInfo = CommandExecutor.PrepareMethodInfo(method.Method, TestCommand);
			Assert.IsNotNull(commandMethodInfo);
			Assert.AreEqual(0, commandMethodInfo.ArgumentCount);
			Assert.AreEqual(BotConfig.EAccess.Master, commandMethodInfo.Permission);

			Task<string> result = ExecuteCommand(commandMethodInfo, bot, steamID);

			Assert.IsNull(result.Result);
		}

		[TestMethod]
		public void NoArgsTask() {
			Mock<IBot> bot = new();
			bot.Setup(x => x.HasAccess(OwnerSteamID, BotConfig.EAccess.Master)).Returns(true);

			Func<Task<string>> method = TestCommand.NoArgsTask;
			CommandMethodInfo commandMethodInfo = CommandExecutor.PrepareMethodInfo(method.Method, TestCommand);
			Assert.IsNotNull(commandMethodInfo);
			Assert.AreEqual(0, commandMethodInfo.ArgumentCount);
			Assert.AreEqual(BotConfig.EAccess.Master, commandMethodInfo.Permission);

			Task<string> result = ExecuteCommand(commandMethodInfo, bot, OwnerSteamID);

			Assert.AreEqual(method().Result, result.Result);
		}

		[TestInitialize]
		public void Setup() {
			TestCommand = new TestMethods();

			OwnerSteamID = new SteamID(AccountID, EUniverse.Public, EAccountType.Individual);
		}

		[TestMethod]
		public void SteamIDArg() {
			Mock<IBot> bot = new();
			bot.Setup(x => x.HasAccess(OwnerSteamID, BotConfig.EAccess.Master)).Returns(true);

			Func<ulong, string> method = TestCommand.SteamIDArg;
			CommandMethodInfo commandMethodInfo = CommandExecutor.PrepareMethodInfo(method.Method, TestCommand);
			Assert.IsNotNull(commandMethodInfo);
			Assert.AreEqual(0, commandMethodInfo.ArgumentCount);
			Assert.AreEqual(BotConfig.EAccess.Master, commandMethodInfo.Permission);

			Task<string> result = ExecuteCommand(commandMethodInfo, bot, OwnerSteamID);

			Assert.AreEqual(OwnerSteamID.ToString(CultureInfo.InvariantCulture), result.Result);
		}

		[TestMethod]
		public void ObjectArg() {
			const string param = nameof(param);
			Mock<IBot> bot = new();
			bot.Setup(x => x.HasAccess(OwnerSteamID, BotConfig.EAccess.Master)).Returns(true);

			Func<object, string> method = TestCommand.ObjectArg;
			CommandMethodInfo commandMethodInfo = CommandExecutor.PrepareMethodInfo(method.Method, TestCommand);
			Assert.IsNotNull(commandMethodInfo);
			Assert.AreEqual(1, commandMethodInfo.ArgumentCount);
			Assert.AreEqual(BotConfig.EAccess.Master, commandMethodInfo.Permission);

			Task<string> result = ExecuteCommand(commandMethodInfo, bot, OwnerSteamID, param);

			Assert.AreEqual(param, result.Result);
		}

		[TestMethod]
		public void StringAndDefaultArgs() {
			const string botName = "bot";
			const string param = nameof(param);
			Mock<IBot> bot = new();
			bot.Setup(x => x.HasAccess(OwnerSteamID, BotConfig.EAccess.Master)).Returns(true);
			bot.Setup(x => x.BotName).Returns(botName);

			Func<IBot, ulong, string, string> method = TestCommand.StringAndDefaultArgs;
			CommandMethodInfo commandMethodInfo = CommandExecutor.PrepareMethodInfo(method.Method, TestCommand);
			Assert.IsNotNull(commandMethodInfo);
			Assert.AreEqual(1, commandMethodInfo.ArgumentCount);
			Assert.AreEqual(BotConfig.EAccess.Master, commandMethodInfo.Permission);

			Task<string> result = ExecuteCommand(commandMethodInfo, bot, OwnerSteamID, param);

			Assert.AreEqual(botName + "/" + OwnerSteamID + "/" + param, result.Result);
		}

		[TestMethod]
		public void StringArg() {
			const string param = nameof(param);
			Mock<IBot> bot = new();
			bot.Setup(x => x.HasAccess(OwnerSteamID, BotConfig.EAccess.Master)).Returns(true);

			Func<string, string> method = TestCommand.StringArg;
			CommandMethodInfo commandMethodInfo = CommandExecutor.PrepareMethodInfo(method.Method, TestCommand);
			Assert.IsNotNull(commandMethodInfo);
			Assert.AreEqual(1, commandMethodInfo.ArgumentCount);
			Assert.AreEqual(BotConfig.EAccess.Master, commandMethodInfo.Permission);

			Task<string> result = ExecuteCommand(commandMethodInfo, bot, OwnerSteamID, param);

			Assert.AreEqual(param, result.Result);
		}

		[TestMethod]
		public void StringNonDefault() {
			const string param = nameof(param);
			Mock<IBot> bot = new();
			bot.Setup(x => x.HasAccess(OwnerSteamID, BotConfig.EAccess.Master)).Returns(true);

			Func<string, string> method = TestCommand.StringNonDefaultArg;
			CommandMethodInfo commandMethodInfo = CommandExecutor.PrepareMethodInfo(method.Method, TestCommand);
			Assert.IsNotNull(commandMethodInfo);
			Assert.AreEqual(1, commandMethodInfo.ArgumentCount);
			Assert.AreEqual(BotConfig.EAccess.Master, commandMethodInfo.Permission);

			Task<string> result = ExecuteCommand(commandMethodInfo, bot, OwnerSteamID, param);

			Assert.AreEqual(param, result.Result);
		}

		[TestMethod]
		public void StringNonDefaultButEmptyArg() {
			const string param = "";
			Mock<IBot> bot = new();
			bot.Setup(x => x.HasAccess(OwnerSteamID, BotConfig.EAccess.Master)).Returns(true);

			Func<string, string> method = TestCommand.StringNonDefaultArg;
			CommandMethodInfo commandMethodInfo = CommandExecutor.PrepareMethodInfo(method.Method, TestCommand);
			Assert.IsNotNull(commandMethodInfo);
			Assert.AreEqual(1, commandMethodInfo.ArgumentCount);
			Assert.AreEqual(BotConfig.EAccess.Master, commandMethodInfo.Permission);

			Task<string> result = ExecuteCommand(commandMethodInfo, bot, OwnerSteamID, param);

			Assert.AreEqual(Commands.FormatStaticResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsInvalid, nameof(param))), result.Result);
		}

		[TestMethod]
		public void StringNonDefaultButNullArg() {
			const string param = null;
			Mock<IBot> bot = new();
			bot.Setup(x => x.HasAccess(OwnerSteamID, BotConfig.EAccess.Master)).Returns(true);

			Func<string, string> method = TestCommand.StringNonDefaultArg;
			CommandMethodInfo commandMethodInfo = CommandExecutor.PrepareMethodInfo(method.Method, TestCommand);
			Assert.IsNotNull(commandMethodInfo);
			Assert.AreEqual(1, commandMethodInfo.ArgumentCount);
			Assert.AreEqual(BotConfig.EAccess.Master, commandMethodInfo.Permission);

			Task<string> result = ExecuteCommand(commandMethodInfo, bot, OwnerSteamID, param);

			Assert.AreEqual(Commands.FormatStaticResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsInvalid, nameof(param))), result.Result);
		}

		[TestMethod]
		public void StringTextArg() {
			const string botName = "bot";
			const string param = nameof(param);
			Mock<IBot> bot = new();
			bot.Setup(x => x.HasAccess(OwnerSteamID, BotConfig.EAccess.Master)).Returns(true);
			bot.Setup(x => x.BotName).Returns(botName);

			Func<string, string> method = TestCommand.StringTextArg;
			CommandMethodInfo commandMethodInfo = CommandExecutor.PrepareMethodInfo(method.Method, TestCommand);
			Assert.IsNotNull(commandMethodInfo);
			Assert.AreEqual(1, commandMethodInfo.ArgumentCount);
			Assert.AreEqual(BotConfig.EAccess.Master, commandMethodInfo.Permission);

			string[] inputArgs = Enumerable.Range(1, 3).Select(_ => param).ToArray();
			Task<string> result = ExecuteCommand(commandMethodInfo, bot, OwnerSteamID, inputArgs);

			Assert.AreEqual(string.Join(' ', inputArgs), result.Result);
		}

		[TestMethod]
		public void StringWrapperArg() {
			const string botName = "bot";
			const string param = nameof(param);
			Mock<IBot> bot = new();
			bot.Setup(x => x.HasAccess(OwnerSteamID, BotConfig.EAccess.Master)).Returns(true);
			bot.Setup(x => x.BotName).Returns(botName);

			Func<StringWrapper, string> method = TestCommand.StringWrapperArg;
			CommandMethodInfo commandMethodInfo = CommandExecutor.PrepareMethodInfo(method.Method, TestCommand);
			Assert.IsNotNull(commandMethodInfo);
			Assert.AreEqual(1, commandMethodInfo.ArgumentCount);
			Assert.AreEqual(BotConfig.EAccess.Master, commandMethodInfo.Permission);

			Task<string> result = ExecuteCommand(commandMethodInfo, bot, OwnerSteamID, param);

			Assert.AreEqual(param, result.Result);
		}
	}
}
