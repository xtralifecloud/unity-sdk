
namespace CotcSdk {
	/**
	 * Social network used for identification / signing in.
	 */
	public enum LoginNetwork {
		Anonymous,
		Email,
		Facebook,
		GooglePlus,
	}

	/**
	 * You can call LoginNetwork.Describe() to stringify the login network and pass it to various APIs.
	 */
	public static class LoginNetworkExtensions {
		public static string Describe(this LoginNetwork n) {
			return n.ToString().ToLower();
		}
	}
}

