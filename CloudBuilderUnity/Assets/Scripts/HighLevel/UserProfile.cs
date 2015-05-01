using System;
using System.Text;
using System.Collections.Generic;

namespace CloudBuilderLibrary
{
	public class UserProfile {
		public Bundle Properties;

		internal UserProfile(Bundle data) {
			Properties = data["properties"];
		}
	}
	
}
