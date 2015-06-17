using System;

namespace CotcSdk {

	/**
	 * Push notifications can be specified in some API calls to push an OS push notification to inactive users.
	 * It is typically a JSON with made of attributes which represent language -> message pairs.
	 * Here is an example: `new PushNotification().Message("en", "Help me!").Message("fr", "Aidez moi!")`.
	 */
	public class PushNotification {

		/**
		 * Default constructor.
		 */
		public PushNotification() {
			Data = Bundle.CreateObject();
		}

		/**
		 * Adds or replaces a string for a given language.
		 * @param language language code, ex. "en", "ja", etc.
		 * @param text the text for this language.
		 */
		public PushNotification Message(string language, string text) {
			Data[language] = text;
			return this;
		}

		internal Bundle Data;
	}
}
