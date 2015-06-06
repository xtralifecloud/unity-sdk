using System;
using System.Text;
using System.Collections.Generic;

namespace CloudBuilderLibrary
{
	/**
	 * Might contain the following:
		{
			"displayname" : "xxx",
			"email" : "xxx',
			"lang" : "en"
		}
	 *  Usage: `string name = gamerProfile["displayname"];`.
	 */
	public sealed class GamerProfile : PropertiesObject {

		internal GamerProfile(Bundle data) : base(data) {}
	}
	
}
