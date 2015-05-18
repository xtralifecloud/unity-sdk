using System;
using System.Text;
using System.Collections.Generic;

namespace CloudBuilderLibrary
{
	public sealed class GamerProfile {
		/**
		 * Might contain the following:
			{
				"displayname" : "xxx",
				"email" : "xxx',
				"lang" : "en"
			}
		 */
		public Bundle Properties;

		internal GamerProfile(Bundle data) {
			Properties = data;
		}
	}
	
}
