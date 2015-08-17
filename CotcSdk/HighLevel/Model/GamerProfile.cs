
namespace CotcSdk
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
