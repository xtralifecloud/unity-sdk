using System;
using System.Collections.Generic;

namespace CloudBuilderLibrary {

	public class Game {

		/**
		 * Returns an object that allows to manipulate the key/value system associated with this game.
		 * @return an object allowing to manipulate key/values for this user/game/domain.
		 */
		public GameVfs GameVfs {
			get { return new GameVfs(Gamer); }
		}

		#region Private
		internal Game(Gamer gamer) {
			Gamer = gamer;
		}
		private Gamer Gamer;
		#endregion
	}

}
