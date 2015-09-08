
namespace CotcSdk {

	public class MatchListResult {
		/// <summary>Describes the creator of the match.</summary>
		public GamerInfo Creator { get; private set; }
		/// <summary>Custom properties, as passed at the creation of the match.</summary>
		public Bundle CustomProperties { get; private set; }
		/// <summary>Description of the match, as defined by the user upon creation.</summary>
		public string Description { get; private set; }
		/// <summary>The ID of the match. Keep this for later as it is useful to continue a match.</summary>
		public string MatchId { get; private set; }
		/// <summary>Maximum number of players, as passed.</summary>
		public int MaxPlayers { get; private set; }
		/// <summary>The current state of the match (running, finished).</summary>
		public MatchStatus Status { get; private set; }

		internal MatchListResult(Bundle serverData) {
			Creator = new GamerInfo(serverData["creator"]);
			CustomProperties = serverData["customProperties"];
			Description = serverData["description"];
			MatchId = serverData["_id"];
			MaxPlayers = serverData["maxPlayers"];
			Status = Common.ParseEnum<MatchStatus>(serverData["status"]);
		}
	}
}
