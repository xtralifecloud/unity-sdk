using System;
using System.Collections.Generic;

namespace CloudBuilderLibrary {

	public class Game {

		/**
		 * Provides an API to run game-scoped (unauthenticated) batches.
		 * @return an object that allows to manipulate batches.
		 */
		public GameBatches Batches {
			get { return new GameBatches(Clan); }
		}

		/**
		 * Returns an object that allows to manipulate the key/value system associated with this game.
		 * @return an object allowing to manipulate key/values for this user/game/domain.
		 */
		public GameVfs GameVfs {
			get { return new GameVfs(Clan); }
		}

		#region Private
		internal Game(Clan clan) {
			Clan = clan;
		}
		private Clan Clan;
		#endregion
	}

}
