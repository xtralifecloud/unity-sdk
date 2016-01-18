
namespace CotcSdk {

	/// @ingroup model_classes
	/// <summary>
	/// Info about a player.
	/// Can be enriched with information, accessible using the index operator [].
	/// Typically contains a profile field, with displayname, email and lang. You can fetch this by doing
	/// `string name = GamerInfo["profile"]["displayName"];`
	/// </summary>
	public class GamerInfo: PropertiesObject {
		/// <summary>Id of the gamer.</summary>
		public string GamerId {
			get { return Props["gamer_id"]; }
		}

		#region Private
		internal GamerInfo(Bundle serverData) : base(serverData) {}
		#endregion
	}
}
