using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using LitJson;

namespace CotcSdk
{
	/// @ingroup main_classes
	/// <summary>
	/// The bundle is a main concept of the CotC SDK. It is basically the equivalent of a JSON object, behaving
	/// like a C# dictionary, but with inferred typing and more safety.
	/// 
	/// You need bundles in many calls, either to decode generic data received by the server (when such data can be
	/// enriched by hooks typically) or to pass generic user parameters, such as when creating a match.
	/// 
	/// Bundles are instantiated through their factory methods: Bundle.CreateObject or Bundle.CreateArray. The type of
	/// the resulting object can be inspected via the Type member. While objects and arrays are containers for further
	/// objects, a bundle is a simple node in the graph and may as such contain a value, such as a string.
	/// 
	/// Sub-objects are fetched using the index operator (brackets), either with a numeric argument for arrays or a
	/// string argument for dictionaries. Conversions are performed automatically based. Let's consider the following
	/// example:
	/// 
	/// ~~~~{.cs}
	/// Bundle b = Bundle.CreateObject();
	/// b["hello"] = "world";
	/// ~~~~
	/// 
	/// The bundle object, considered as dictionary will have its key "hello" set with a new Bundle node of type string
	/// (implicitly created from the string). So later on, you might fetch this value as well:
	/// 
	/// ~~~~{.cs}
	/// string value = b["hello"];
	/// ~~~~
	/// 
	/// What happens here is that the bundle under the key "hello" is fetched, and then implicitly converted to a string.
	/// 
	/// Bundle provides a safe way to browse a predefined graph, as when a key doesn't exist it returns the
	/// Bundle.Empty (no null value). This special bundle allows to fetch sub-objects but they will always translate to
	/// Bundle.Empty as well. If the values are to be converted to a primitive type, the result will be the default
	/// value for this type (null for a string, 0 for an int, etc.). As such, you may do this:
	/// 
	/// ~~~~{.cs}
	/// int value = bundle["nonexisting"]["key"];
	/// ~~~~
	/// 
	/// Since the "nonexisting" key is not found on bundle, Bundle.Empty is returned. Further fetching "key" will return
	/// an empty bundle as well. Which will be converted implicitly to an integer as 0. Bundle.Empty is a constant value
	/// that always refers to an empty bundle, and attempting to modify it will result in an exception.
	/// 
	/// ~~~~{.cs}
	/// Bundle b = Bundle.Empty;
	/// b["value"] = "something"; // Exception
	/// ~~~~
	/// 
	/// The bundle hierarchy doesn't accept null or Bundle.Empty values (it just rejects them). You should avoid
	/// manipulating null Bundles and use Bundle.Empty wherever possible, however you may assign a null bundle to a key,
	/// which will have no effect.
	/// This can be useful for optional arguments. For instance, the following snippet will not affect the bundle.
	/// 
	/// ~~~~{.cs}
	/// string value = null; // converts to Bundle.Empty and rejects assignment
	/// bundle["key"] = value;
	/// ~~~~
	/// 
	/// Note that Bundle.Empty is not strictly identical to an empty bundle object. Bundle.Empty is never considered
	/// as a value and is discarded upon assignment. For instance:
	/// 
	/// ~~~~{.cs}
	/// Bundle a = Bundle.CreateObject();
	/// Bundle b = Bundle.CreateObject();
	/// a["key"] = Bundle.Empty;
	/// b["key"] = Bundle.CreateObject();
	/// Log(a.ToJson()); // {}
	/// Log(b.ToJson()); // {"key": {}}
	/// ~~~~
	/// 
	/// If you need a special value for keys that do not match the expected type or are not found in the hierarchy, you
	/// may as well use the .As* methods. For instance, the previous snippet could be written as follows to have a default
	/// value of one:
	/// 
	/// ~~~~{.cs}
	/// int value = bundle["nonexisting"]["key"].AsInt(1);
	/// ~~~~
	/// 
	/// It is also possible to inspect the Type property of the Bundle in order to ensure that the value was provided as
	/// expected.
	/// 
	/// A bundle may be pre-filled at creation by passing arguments to Bundle.CreateObject and Bundle.CreateArray. For
	/// instance:
	/// 
	/// ~~~~{.cs}
	/// Bundle b = Bundle.CreateObject("key1", "value1", "key2", "value2");
	/// ~~~~
	/// 
	/// Is equivalent to writing:
	/// 
	/// ~~~~{.cs}
	/// Bundle b = Bundle.CreateObject();
	/// b["key1"] = "value1";
	/// b["key2"] = "value2";
	/// ~~~~
	/// 
	/// A bundle can quickly be transformed from/to JSON using ToJson and Bundle.FromJson methods. One can also check
	/// for the presence of keys and remove them with the .Has respectively .Remove methods.
	/// 
	/// Iterating a JSON object is made using the explicit .As* methods. For instance, here is how you iterate over an
	/// array bundle (no harm will happen if the key doesn't exist or is not an array, since an empty array is returned):
	/// 
	/// ~~~~{.cs}
	/// Bundle b;
	/// foreach (Bundle value in b) { ... }
	/// ~~~~
	/// 
	/// For an object, use AsDictionary().
	/// 
	/// ~~~~{.cs}
	/// Bundle b;
	/// foreach (KeyValuePair<string, Bundle> pair in b["key"].AsDictionary()) { ... }
	/// ~~~~
	/// 
	/// This loop is safe as well even if the bundle doesn't contain a "key" entry or the "key" entry is not an object.
	/// 
	/// Null bundles should be avoided! Use Bundle.Empty every time you need a "void", non mutable bundle value.
	/// Converting from a null bundle will result in an exception.
	/// 
	/// ~~~~{.cs}
	/// Bundle b = null;
	/// string value = b; // Null pointer exception!
	/// ~~~~
	/// 
	/// That's all what there is to know about bundles. In general they should make any code interacting with generic
	/// objects simple and safe.
	/// </summary>
	public class Bundle {
		public enum DataType {
			None, Boolean, Integer, Double, String, Array, Object
		}
		private DataType type;
		private double doubleValue;
		private long longValue;
		private string stringValue;
		private Dictionary<string, Bundle> objectValue;
		private List<Bundle> arrayValue;
		private Bundle parent;

		/// <summary>Creates a bundle of type object. You may also pass up to three key/value pairs (in this order),
		/// which will be put in the object initially.</summary>
		/// <returns>A new bundle.</returns>
		public static Bundle CreateObject() {
			return new Bundle(DataType.Object);
		}
		public static Bundle CreateObject(string onlyKey, Bundle onlyValue) {
			Bundle result = new Bundle(DataType.Object);
			result[onlyKey] = onlyValue;
			return result;
		}
		public static Bundle CreateObject(string key1, Bundle value1, string key2, Bundle value2) {
			Bundle result = new Bundle(DataType.Object);
			result[key1] = value1;
			result[key2] = value2;
			return result;
		}
		public static Bundle CreateObject(string key1, Bundle value1, string key2, Bundle value2, string key3, Bundle value3) {
			Bundle result = new Bundle(DataType.Object);
			result[key1] = value1;
			result[key2] = value2;
			result[key3] = value3;
			return result;
		}
		/// <summary>Creates a bundle of type array.</summary>
		/// <param name="values">Optional values to pre-fill the array with. Since bundle are implicitly converted, remember
		/// that you may pass an integer, string, etc.</param>
		/// <returns>A new bundle.</returns>
		public static Bundle CreateArray(params Bundle[] values) {
			Bundle result = new Bundle(DataType.Array);
			foreach (Bundle b in values) result.Add(b);
			return result;
		}

		/// <summary>Empty (null-like) Bundle. See class documentation for more information.</summary>
		public static readonly EmptyBundle Empty = new EmptyBundle();

		// Construction (internal)
		protected Bundle(DataType dataType) {
			type = dataType;
			if (dataType == DataType.Object) objectValue = new Dictionary<string, Bundle>();
			else if (dataType == DataType.Array) arrayValue = new List<Bundle>();
		}
		public Bundle(bool value) { type = DataType.Boolean; longValue = value ? 1 : 0; }
		public Bundle(long value) { type = DataType.Integer; longValue = value; }
		public Bundle(float value) : this((double)value) { }
		public Bundle(double value) { type = DataType.Double; doubleValue = value; }
		public Bundle(string value) { type = DataType.String; stringValue = value; }
		public static implicit operator Bundle(bool value) { return new Bundle(value); }
		public static implicit operator Bundle(long value) { return new Bundle(value); }
		public static implicit operator Bundle(float value) { return new Bundle(value); }
		public static implicit operator Bundle(double value) { return new Bundle(value); }
		public static implicit operator Bundle(string value) { return value != null ? new Bundle(value) : Empty; }
		public static implicit operator bool(Bundle b) { return b.AsBool(); }
		public static implicit operator int(Bundle b) { return b.AsInt(); }
		public static implicit operator long(Bundle b) { return b.AsLong(); }
		public static implicit operator float(Bundle b) { return (float)b.AsDouble(); }
		public static implicit operator double(Bundle b) { return b.AsDouble(); }
		public static implicit operator string(Bundle b) { return b.AsString(); }

		public virtual Bundle this[string key] {
			get { return Has(key) ? Dictionary[key] : Empty; }
			set {
				if (value == null || (object)value == (object)Empty) {
					Dictionary.Remove(key);
					return;
				}
				Dictionary[key] = value;
				value.parent = this;
			}
		}
		public virtual Bundle this[int index] {
			get { return Array[index]; }
			set {
				if (value == null || (object)value == (object)Empty) return;
				value.parent = this;
				if (index != -1)
					Array[index] = value;
				else
					Array.Add(value);
			}
		}
		public void Add(Bundle value) {
			value.parent = this;
			Array.Add (value);
		}

		/// <summary>Deep copies the bundle.</summary>
		public Bundle Clone() {
			Bundle thisNode = null;
			if (parent == null) return CloneRecursive(null, ref thisNode);
			// Clone the whole structure and return a pointer to the same node in the new structure.
			Bundle cloned = Root.CloneRecursive(this, ref thisNode);
			return thisNode;
		}
		// Does a simple (deep) clone, down from this node to the leaves only.
		// You can specify a 'searchedNode' from the original tree. 'foundBundle' will contain the equivalent of 'searchedNode' in the cloned structure.
		private Bundle CloneRecursive(Bundle searchedBundle, ref Bundle foundBundle) {
			Bundle result;
			switch (Type) {
				case DataType.Object:
					// Deep copy
					result = Bundle.CreateObject();
					foreach (var pair in Dictionary) result[pair.Key] = pair.Value.CloneRecursive(searchedBundle, ref foundBundle);
					if (this == searchedBundle) foundBundle = result;
					return result;

				case DataType.Array:
					result = Bundle.CreateArray();
					foreach (var value in Array) result.Add(value.CloneRecursive(searchedBundle, ref foundBundle));
					if (this == searchedBundle) foundBundle = result;
					return result;

				default:
					// No need to clone basic types
					return this;
			}
		}

		// Dictionary getters
		[Obsolete("Will be removed soon. Use bundle[key].As*(defaultValue) instead.")]
		public bool GetBool(string key, bool defaultValue = false) {
			return Has(key) ? Dictionary[key].AsBool(defaultValue) : defaultValue;
		}
		[Obsolete("Will be removed soon. Use bundle[key].As*(defaultValue) instead.")]
		public int GetInt(string key, int defaultValue = 0) {
			return Has(key) ? Dictionary[key].AsInt(defaultValue) : defaultValue;
		}
		[Obsolete("Will be removed soon. Use bundle[key].As*(defaultValue) instead.")]
		public long GetLong(string key, long defaultValue = 0) {
			return Has(key) ? Dictionary[key].AsLong(defaultValue) : defaultValue;
		}
		[Obsolete("Will be removed soon. Use bundle[key].As*(defaultValue) instead.")]
		public double GetDouble(string key, double defaultValue = 0) {
			return Has(key) ? Dictionary[key].AsDouble(defaultValue) : defaultValue;
		}
		[Obsolete("Will be removed soon. Use bundle[key].As*(defaultValue) instead.")]
		public string GetString(string key, string defaultValue = null) {
			return Has(key) ? Dictionary[key].AsString(defaultValue) : defaultValue;
		}

		// Array getters
		[Obsolete("Will be removed soon. Use bundle[index].As*(defaultValue) instead.")]
		public bool GetBool(int index) {
			return Array[index].AsBool();
		}
		[Obsolete("Will be removed soon. Use bundle[index].As*(defaultValue) instead.")]
		public int GetInt(int index) {
			return Array[index].AsInt();
		}
		[Obsolete("Will be removed soon. Use bundle[index].As*(defaultValue) instead.")]
		public long GetLong(int index) {
			return Array[index].AsLong();
		}
		[Obsolete("Will be removed soon. Use bundle[index].As*(defaultValue) instead.")]
		public double GetDouble(int index) {
			return Array[index].AsDouble();
		}
		[Obsolete("Will be removed soon. Use bundle[index].As*(defaultValue) instead.")]
		public string GetString(int index) {
			return Array[index].AsString();
		}

		// Key management
		public bool Has(string key) {
			return Dictionary.ContainsKey(key);
		}
		public bool IsEmpty {
			get {
				switch (type) {
					case DataType.Object: return Dictionary.Count == 0;
					case DataType.Array: return Array.Count == 0;
					case DataType.None: return true;
					default: return false;
				}
			}
		}
		/// <summary>Returns the parent of this bundle, if it was detached from any tree. May be null
		/// if it is the root or has never been attached.</summary>
		public Bundle Parent { get { return parent; } }
		public void Remove(string key) {
			Dictionary.Remove(key);
		}
		/// <summary>Returns the root of this tree. Goes as far as possible back in the hierarchy.</summary>
		public Bundle Root {
			get {
				Bundle lastNonNil = this, prev = parent;
				while (prev != null) {
					lastNonNil = prev;
					prev = lastNonNil.parent;
				}
				return lastNonNil;
			}
		}

		// Representations
		public DataType Type {
			get { return type; }
		}
		public bool AsBool(bool defaultValue = false) {
			switch (Type) {
				case DataType.Boolean: return longValue != 0;
				case DataType.Integer: return longValue != 0;
				case DataType.Double: return doubleValue != 0;
				case DataType.String: return String.Compare(stringValue, "true", true) == 0;
			}
			return defaultValue;
		}
		public int AsInt(int defaultValue = 0) {
			int result = defaultValue;
			switch (Type) {
				case DataType.Boolean: return (int)longValue;
				case DataType.Integer: return (int)longValue;
				case DataType.Double: return (int)doubleValue;
				case DataType.String: int.TryParse(stringValue, out result); return result;
			}
			return defaultValue;
		}
		public long AsLong(long defaultValue = 0) {
			long result = defaultValue;
			switch (Type) {
				case DataType.Boolean: return (int)longValue;
				case DataType.Integer: return (int)longValue;
				case DataType.Double: return (int)doubleValue;
				case DataType.String: long.TryParse(stringValue, out result); return result;
			}
			return defaultValue;
		}
		public float AsFloat(float defaultValue = 0) {
			return (float)AsDouble(defaultValue);
		}
		public double AsDouble(double defaultValue = 0) {
			double result = defaultValue;
			switch (Type) {
				case DataType.Boolean: return (double)longValue;
				case DataType.Integer: return (double)longValue;
				case DataType.Double: return doubleValue;
				case DataType.String: double.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out result); return result;
			}
			return defaultValue;
		}
		public string AsString(string defaultValue = null) {
			switch (Type) {
				case DataType.Boolean: return longValue != 0 ? "true" : "false";
				case DataType.Integer: return longValue.ToString();
				case DataType.Double: return doubleValue.ToString();
				case DataType.String: return stringValue;
			}
			return defaultValue;
		}
		public List<Bundle> AsArray() {
			return Array;
		}
		public Dictionary<string, Bundle> AsDictionary() {
			return Dictionary;
		}

		// Json methods
		public static Bundle FromJson(string json) {
			if (json == null) return null;
			return FromJson(JsonMapper.ToObject(json));
		}

		public string ToJson() {
			// IDEA One day we could probably implement the JSON by ourselves, without requiring LitJson :)
			return JsonMapper.ToJson(ToJson(this));
		}

		public override string ToString() { return ToJson(); }

		private static Bundle FromJson(JsonData data) {
			if (data.IsBoolean) return ((IJsonWrapper) data).GetBoolean();
			if (data.IsDouble) return ((IJsonWrapper) data).GetDouble();
			if (data.IsInt) return ((IJsonWrapper) data).GetInt();
			if (data.IsLong) return ((IJsonWrapper) data).GetLong();
			if (data.IsString) return ((IJsonWrapper) data).GetString();
			if (data.IsObject) {
				Bundle subBundle = Bundle.CreateObject();
				IDictionary dict = (IDictionary) data;
				foreach (string key in dict.Keys) {
					JsonData item = (JsonData)dict[key];
					if (item != null) subBundle[key] = FromJson(item);
				}
				return subBundle;
			}
			if (data.IsArray) {
				Bundle subBundle = Bundle.CreateArray();
				IList dict = (IList) data;
				foreach (JsonData value in dict) {
					if (value != null) subBundle.Add(FromJson(value));
				}
				return subBundle;
			}
			return null;
		}

		private JsonData ToJson(Bundle bundle) {
			JsonData target = new JsonData();
			if (bundle.Type == DataType.Object) {
				target.SetJsonType(JsonType.Object);
				foreach (KeyValuePair<string, Bundle> entry in bundle.Dictionary) {
					switch (entry.Value.Type) {
					case DataType.Boolean: target[entry.Key] = entry.Value.AsBool(); break;
					case DataType.Integer: target[entry.Key] = entry.Value.AsLong(); break;
					case DataType.Double: target[entry.Key] = entry.Value.AsDouble(); break;
					case DataType.String: target[entry.Key] = entry.Value.AsString(); break;
					default: target[entry.Key] = ToJson(entry.Value); break;
					}
				}
			}
			else {
				target.SetJsonType(JsonType.Array);
				foreach (Bundle entry in bundle.Array) {
					switch (entry.Type) {
					case DataType.Boolean: target.Add(entry.AsBool()); break;
					case DataType.Integer: target.Add(entry.AsInt()); break;
					case DataType.Double: target.Add(entry.AsDouble()); break;
					case DataType.String: target.Add(entry.AsString()); break;
					default: target.Add(ToJson(entry)); break;
					}
				}
			}
			return target;
		}

		// Private
		private List<Bundle> Array {
			get { return arrayValue ?? new List<Bundle>(); }
		}
		private Dictionary<string, Bundle> Dictionary {
			get { return objectValue ?? new Dictionary<string, Bundle>(); }
		}
	}

	/// <summary>Never instantiate this class. Use Bundle.Empty instead. Pass that everywhere an explicit configuration is not wanted.</summary>
	public class EmptyBundle : Bundle
	{
		internal EmptyBundle() : base(Bundle.DataType.None) { }

		public override Bundle this[string key]
		{
			get { return Bundle.Empty; }
			set { throw new ArgumentException("Trying to assign to non-existing bundle node. Make sure you create the appropriate hierarchy."); }
		}
		public override Bundle this[int index]
		{
			get { return Bundle.Empty; }
			set { throw new ArgumentException("Trying to assign to non-existing bundle node. Make sure you create the appropriate hierarchy."); }
		}
	}
}
