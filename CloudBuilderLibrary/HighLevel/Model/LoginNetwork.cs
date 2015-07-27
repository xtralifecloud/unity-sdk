
namespace CotcSdk {
	public enum LoginNetwork {
		Anonymous,
		Email,
		Facebook,
		GooglePlus,
	}

	public static class LoginNetworkExtensions {
		public static string Describe(this LoginNetwork n) {
			return n.ToString().ToLower();
		}
	}
}

