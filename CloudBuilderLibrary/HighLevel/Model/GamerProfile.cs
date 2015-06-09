using System;
using System.Text;
using System.Collections.Generic;

namespace CloudBuilderLibrary
{
	/**
	 * Might contain the following:
		{
			"displayName" : "xxx",
			"email" : "xxx',
			"lang" : "en"
		}
	 *  Usage: `string name = gamerProfile["displayName"];`.
	 */
	public sealed class GamerProfile : PropertiesObject {

		internal GamerProfile(Bundle data) : base(data) {}
	}
	
}
