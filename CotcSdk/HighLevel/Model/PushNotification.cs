
namespace CotcSdk {

	/// <summary>
	/// Push notifications can be specified in some API calls to push an OS push notification to inactive users.
	/// It is typically a JSON with made of attributes which represent language -> message pairs.
	/// Here is an example: `new PushNotification().Message("en", "Help me!").Message("fr", "Aidez moi!")`.
	/// </summary>
	public class PushNotification {

		/// <summary>Default constructor.</summary>
		public PushNotification() {
			Data = Bundle.CreateObject();
		}

		/// <summary>Adds or replaces a string for a given language.</summary>
		/// <param name="language">Language code, ex. "en", "ja", etc.</param>
		/// <param name="text">The text for this language.</param>
		public PushNotification Message(string language, string text) {
			Data[language] = text;
			return this;
		}

		internal Bundle Data;
	}
}
