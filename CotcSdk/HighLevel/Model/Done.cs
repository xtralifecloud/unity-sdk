
namespace CotcSdk
{
	/// <summary>
	/// Generic result for an API call that has been successful and simply gives an indication of whether it was done properly or not.
	/// Usually the result should be true, but some operations may succeed and give an indication that nothing was done.
	/// You may use it as a boolean or get additional information by using it as a PropertiesObject.
	/// @code{.cs} Gamer.ChangeEmailAddress("a@localhost.localdomain").Then(done => {
	/// // Call was performed successfully, but the address might not have been changed.
	/// if (!done)
	/// throw new YourException("Address not changed");
	/// })
	/// .Catch(ex => {
	/// // Either the call has failed (ex is CotcException) or we caught YourException
	/// // because the call did nothing.
	/// }); @endcode
	/// </summary>
	public class Done : PropertiesObject {
		public static implicit operator bool (Done d) {
			return d.Successful;
		}
		public bool Successful { get; private set; }

		public Done(Bundle serverData)
			: base(serverData) {
			Successful = serverData["done"];
		}
		public Done(bool done, Bundle serverData)
			: base(serverData) {
			Successful = done;
		}
	}
}
