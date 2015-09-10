
namespace CotcSdk {

	/// @ingroup model_classes
	/// <summary>
	/// Outline information about a player.
	/// Can be enriched with information, accessible using the index operator [].
	/// Typically contains a profile field, with displayname, email and lang. You can fetch this by doing
	/// `string name = GamerOutline["profile"]["displayname"];`
	/// </summary>
	public class GamerOutline: PropertiesObject {
		public LoginNetwork Network {
			get { return Common.ParseEnum(Props["network"], LoginNetwork.Anonymous); }
		}
		public string NetworkId {
			get { return Props["networkid"]; }
		}

		#region Private
		internal GamerOutline(Bundle serverData) : base(serverData) {}
		#endregion
	}
}
