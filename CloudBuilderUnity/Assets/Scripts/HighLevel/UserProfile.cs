using System;
using System.Text;
using System.Collections.Generic;

namespace CloudBuilderLibrary
{
	public class UserProfile {
		/**
		 * Might contain the following:
			{
				"displayname" : "xxx",
				"email" : "xxx',
				"lang" : "en"
			}
		 */
		public Bundle Properties;

		internal UserProfile(Bundle data) {
			Properties = data["properties"];
		}
	}
	
}
