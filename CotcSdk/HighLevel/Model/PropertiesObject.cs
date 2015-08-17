using System.Collections.Generic;

namespace CotcSdk {

	/**
	 * Defines an object that can be queried for additional properties using indexers. The structure of the object
	 * remains read only though.
	 * 
	 * ~~~~{.cs}
	 * GamerInfo gamer = ...;
	 * // Browse all keys
	 * foreach (KeyValuePair<string, Bundle> pair in gamer) { ... }
	 * // Or using indexers
	 * foreach (string key in gamer.Keys()) { Debug.Log(gamer[key]); }
	 * ~~~~
	 * 
	 * Usually these objects have a proper structure representing the predefined properties, and you can use the
	 * PropertiesObject methods to query additional properties that might have been enriched in a hook for instance.
	 * 
	 * Beware however, querying a PropertiesObject using keys will return directly what the server has responded. Thus,
	 * if you have modified a property and query its equivalent key, the values will differ. That is why we don't
	 * usually make PropertiesObjects publicly writable.
	 */
	public class PropertiesObject {
		protected Bundle Props;

		protected PropertiesObject(Bundle baseData) {
			Props = baseData;
		}

		/**
		 * Allows to query additional properties via an indexer (can be enriched via hooks).
		 */
		public Bundle this[string key] {
			get { return Props[key]; }
		}

		/**
		 * Gets the underlying Bundle. Dangerous, only use internally, when you want to put the contents
		 * of a properties object into an existing bundle.
		 */
		public Bundle AsBundle() {
			return Props;
		}

		/**
		 * Allows to browse all keys (might include some that are already exposed as typed properties in the object).
		 * @return the list of keys.
		 */
		public Dictionary<string, Bundle>.KeyCollection Keys() {
			return Props.AsDictionary().Keys;
		}
		/**
		 * Allows to browse all keys (might include some that are already exposed as typed properties in the object).
		 * @return an enumerator that allows to browse all key-value pairs. The values are #CotcSdk.Bundle, on which you can
		 * perform all the usual conversions, such as casting it as a string if the property is expected to be a
		 * string for instance.
		 */
		public Dictionary<string, Bundle>.Enumerator GetEnumerator() {
			return Props.AsDictionary().GetEnumerator();
		}

		/**
		 * You may use this to debug what is inside this property object.
		 * @return a JSON string representing the object.
		 */
		public override string ToString() {
			return Props.ToJson();
		}

		/**
		 * Builds a JSON representation of this object, same as ToString actually.
		 * @return a JSON string representing the object.
		 */
		public string ToJson() {
			return Props.ToJson();
		}
	}
}
