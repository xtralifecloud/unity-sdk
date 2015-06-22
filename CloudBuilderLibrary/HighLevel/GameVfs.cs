using System;

namespace CotcSdk {

	/**
	 * Represents a key/value system, also known as virtual file system, to be used for game properties.
	 * This class is scoped by domain, meaning that you can call .Domain("yourdomain") and perform
	 * additional calls that are scoped.
	 */
	public sealed class GameVfs {

		/**
		 * Sets the domain affected by this object.
		 * You should typically use it this way: `gamer.GamerVfs.Domain("private").SetKey(...);`
		 * @param domain domain on which to scope the VFS. Defaults to `private` if not specified.
		 */
		public void Domain(string domain) {
			this.domain = domain;
		}

		#region Private
		internal GameVfs(Cloud cloud) {
			Cloud = cloud;
		}

		private string domain = Common.PrivateDomain;
		private Cloud Cloud;
		#endregion
	}
}
