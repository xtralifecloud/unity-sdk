
namespace CotcSdk {

	/// @ingroup main_classes
	/// <summary>
	/// Provides functionality related to the entire game.
	/// </summary>
	public class Game {

		/// <summary>Provides an API to run game-scoped (unauthenticated) batches.</summary>
		/// <returns>An object that allows to manipulate batches.</returns>
		public GameBatches Batches {
			get { return new GameBatches(Cloud); }
		}

		/// <summary>Returns an object that allows to manipulate the key/value system associated with this game.</summary>
		/// <returns>An object allowing to manipulate key/values for this user/game/domain.</returns>
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
