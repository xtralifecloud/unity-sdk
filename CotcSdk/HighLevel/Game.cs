
namespace CotcSdk {

	public class Game {

		/// <summary>Provides an API to run game-scoped (unauthenticated) batches.</summary>
		/// <returns>an object that allows to manipulate batches.</returns>
		public GameBatches Batches {
			get { return new GameBatches(Cloud); }
		}

		/// <summary>Returns an object that allows to manipulate the key/value system associated with this game.</summary>
		/// <returns>an object allowing to manipulate key/values for this user/game/domain.</returns>
		public GameVfs GameVfs {
			get { return new GameVfs(Cloud); }
		}

		#region Private
		internal Game(Cloud cloud) {
			Cloud = cloud;
		}
		private Cloud Cloud;
		#endregion
	}

}
