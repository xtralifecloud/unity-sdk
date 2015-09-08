
namespace CotcSdk
{
	/// <summary>
	/// Might contain the following:
	/// {
	/// "displayName" : "xxx",
	/// "email" : "xxx',
	/// "lang" : "en"
	/// }
	/// Usage: `string name = gamerProfile["displayName"];`.
	/// </summary>
	public sealed class GamerProfile : PropertiesObject {

		internal GamerProfile(Bundle data) : base(data) {}
	}
	
}
