
namespace CotcSdk {
	/// @ingroup model_classes
	/// <summary>Social network used for identification / signing in.</summary>
	public enum LoginNetwork {
		Anonymous,
		Email,
		Facebook,
		GooglePlus,
	}

	/// @ingroup model_classes
	/// <summary>You can call LoginNetwork.Describe() to stringify the login network and pass it to various APIs.</summary>
	public static class LoginNetworkExtensions {
		public static string Describe(this LoginNetwork n) {
			return n.ToString().ToLower();
		}
	}
}

