using System;

namespace CloudBuilderLibrary {

	/**
	 * Push notifications can be specified in some API calls to push an OS push notification to inactive users.
	 * It is typically a JSON with made of attributes which represent language -> message pairs.
	 * Here is an example: `Bundle.CreateObject("en", "Help me!", "fr", "Aidez moi!")`.
	 */
	public class PushNotification {
		public Bundle Data;

		public PushNotification(Bundle data) {
			Data = data;
		}
	}
}
