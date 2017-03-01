using System.Collections.Generic;

namespace CotcSdk {

	/// @ingroup data_classes
	/// <summary>
	/// Push notifications can be specified in some API calls to push an OS push notification to inactive users.
	/// It is typically a JSON with made of attributes which represent language -> message pairs.
	/// Here is an example: `new PushNotification().Message("en", "Help me!").Message("fr", "Aidez moi!")`.
	/// </summary>
	public class PushNotification {

		/// <summary>Creates a PushNotification object.</summary>
		/// <returns>A new PushNotification object.</returns>
		public PushNotification() {
			Data = Bundle.CreateObject();
		}

		/// <summary>Creates a PushNotification object with one language/text pair which will be put in the object initially.</summary>
		/// <returns>A new bundle filled with one language/text pair.</returns>
		public PushNotification(string language, string text) {
			Data = Bundle.CreateObject();
			Data[language] = text;
		}

		/// <summary>Creates a PushNotification object with two language/text pairs which will be put in the object initially.</summary>
		/// <returns>A new bundle filled with two language/text pairs.</returns>
		public PushNotification(string language1, string text1, string language2, string text2) {
			Data = Bundle.CreateObject();
			Data[language1] = text1;
			Data[language2] = text2;
		}

		/// <summary>Creates a PushNotification object with three language/text pairs which will be put in the object initially.</summary>
		/// <returns>A new bundle filled with three language/text pairs.</returns>
		public PushNotification(string language1, string text1, string language2, string text2, string language3, string text3) {
			Data = Bundle.CreateObject();
			Data[language1] = text1;
			Data[language2] = text2;
			Data[language3] = text3;
		}

		/// <summary>Creates a PushNotification object with many language/text pairs which will be put in the object initially.</summary>
		/// <returns>A new bundle filled with many language/text pairs.</returns>
		public PushNotification(params KeyValuePair<string, string>[] languageTextPairs) {
			Data = Bundle.CreateObject();
			foreach (KeyValuePair<string, string> languageTextPair in languageTextPairs) {
				Data[languageTextPair.Key] = languageTextPair.Value;
			}
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
