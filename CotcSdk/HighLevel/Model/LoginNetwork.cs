
namespace CotcSdk {
	/// <summary>Social network used for identification / signing in.</summary>
	public enum LoginNetwork {
		Anonymous,
		Email,
		Facebook,
		GooglePlus,
	}

	/// <summary>You can call LoginNetwork.Describe() to stringify the login network and pass it to various APIs.</summary>
	public static class LoginNetworkExtensions {
		public static string Describe(this LoginNetwork n) {
			return n.ToString().ToLower();
		}
	}
}

