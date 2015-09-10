using System.Collections.Generic;

namespace CotcSdk {

	/// @ingroup model_classes
	/// <summary>
	/// Defines an object that can be queried for additional properties using indexers. The structure of the object
	/// remains read only though.
	/// 
	/// ~~~~{.cs}
	/// GamerInfo gamer = ...;
	/// // Browse all keys
	/// foreach (KeyValuePair<string, Bundle> pair in gamer) { ... }
	/// // Or using indexers
	/// foreach (string key in gamer.Keys()) { Debug.Log(gamer[key]); }
	/// ~~~~
	/// 
	/// Usually these objects have a proper structure representing the predefined properties, and you can use the
	/// PropertiesObject methods to query additional properties that might have been enriched in a hook for instance.
	/// 
	/// Beware however, querying a PropertiesObject using keys will return directly what the server has responded. Thus,
	/// if you have modified a property and query its equivalent key, the values will differ. That is why we don't
	/// usually make PropertiesObjects publicly writable.
	/// </summary>
	public class PropertiesObject {
		protected Bundle Props;

		protected PropertiesObject(Bundle baseData) {
			Props = baseData;
		}

		/// <summary>Allows to query additional properties via an indexer (can be enriched via hooks).</summary>
		public Bundle this[string key] {
			get { return Props[key]; }
		}

		/// <summary>
		/// Gets the underlying Bundle. Dangerous, only use internally, when you want to put the contents
		/// of a properties object into an existing bundle.
		/// </summary>
		public Bundle AsBundle() {
			return Props;
		}

		/// <summary>Allows to browse all keys (might include some that are already exposed as typed properties in the object).</summary>
		/// <returns>The list of keys.</returns>
		public Dictionary<string, Bundle>.KeyCollection Keys() {
			return Props.AsDictionary().Keys;
		}
		/// <summary>Allows to browse all keys (might include some that are already exposed as typed properties in the object).</summary>
		/// <returns>An enumerator that allows to browse all key-value pairs. The values are #CotcSdk.Bundle, on which you can
		///     perform all the usual conversions, such as casting it as a string if the property is expected to be a
		///     string for instance.</returns>
		public Dictionary<string, Bundle>.Enumerator GetEnumerator() {
			return Props.AsDictionary().GetEnumerator();
		}

		/// <summary>You may use this to debug what is inside this property object.</summary>
		/// <returns>A JSON string representing the object.</returns>
		public override string ToString() {
			return Props.ToJson();
		}

		/// <summary>Builds a JSON representation of this object, same as ToString actually.</summary>
		/// <returns>A JSON string representing the object.</returns>
		public string ToJson() {
			return Props.ToJson();
		}
	}
}
