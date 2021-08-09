namespace Vinvoker.Tests.Helpers {
	public class StringWrapper {
		private StringWrapper(string value) => Value = value;
		private string Value { get; }

		public static explicit operator StringWrapper(string value) => new(value);

		public override string ToString() => Value;
	}
}
