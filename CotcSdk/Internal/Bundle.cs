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
	public class Bundle : IEquatable<Bundle> {
		/// <summary>Possible types of data storable into a bundle.</summary>
		public enum DataType { None, Boolean, Integer, Double, String, Array, Object }
		private DataType type;
		private double doubleValue;
		private long longValue;
		private string stringValue;
		private Dictionary<string, Bundle> objectValue;
		private List<Bundle> arrayValue;
		private Bundle parent;

		/// <summary>Empty (null-like) Bundle. See class documentation for more information.</summary>
		public static readonly EmptyBundle Empty = new EmptyBundle();

		/// <summary>Builds a fresh new Bundle from a data type.</summary>
		/// <returns>A new Bundle instance.</returns>
		protected Bundle(DataType dataType) {
			type = dataType;
			if (dataType == DataType.Object) objectValue = new Dictionary<string, Bundle>();
			else if (dataType == DataType.Array) arrayValue = new List<Bundle>();
		}

		/// <summary>Creates a bundle of type object.</summary>
		/// <returns>A new bundle.</returns>
		public static Bundle CreateObject() { return new Bundle(DataType.Object); }

		/// <summary>Creates a bundle of type object with one key/value pair which will be put in the object initially.</summary>
		/// <returns>A new bundle filled with one key/value pair.</returns>
		public static Bundle CreateObject(string key, Bundle value) {
			Bundle result = new Bundle(DataType.Object);
			result[key] = value;
			return result;
		}

		/// <summary>Creates a bundle of type object with two key/value pairs which will be put in the object initially.</summary>
		/// <returns>A new bundle filled with two key/value pairs.</returns>
		public static Bundle CreateObject(string key1, Bundle value1, string key2, Bundle value2) {
			Bundle result = new Bundle(DataType.Object);
			result[key1] = value1;
			result[key2] = value2;
			return result;
		}

		/// <summary>Creates a bundle of type object with three key/value pairs which will be put in the object initially.</summary>
		/// <returns>A new bundle filled with three key/value pairs.</returns>
		public static Bundle CreateObject(string key1, Bundle value1, string key2, Bundle value2, string key3, Bundle value3) {
			Bundle result = new Bundle(DataType.Object);
			result[key1] = value1;
			result[key2] = value2;
			result[key3] = value3;
			return result;
		}

		/// <summary>Creates a bundle of type object with many key/value pairs which will be put in the object initially.</summary>
		/// <returns>A new bundle filled with many key/value pairs.</returns>
		public static Bundle CreateObject(params KeyValuePair<string, Bundle>[] keyValuePairs) {
			Bundle result = new Bundle(DataType.Object);
			foreach (KeyValuePair<string, Bundle> keyValuePair in keyValuePairs) {
				result[keyValuePair.Key] = keyValuePair.Value;
			}
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

		/// <summary>Creates a new Bundle of Boolean type from a bool value.</summary>
		/// <returns>A new Boolean Bundle from a bool value.</returns>
		public Bundle(bool value) { type = DataType.Boolean; longValue = value ? 1L : 0L; }

		/// <summary>Creates a new Bundle of Integer type from an int value.</summary>
		/// <returns>A new Integer Bundle from an int value.</returns>
        public Bundle(int value) : this((long)value) { }

		/// <summary>Creates a new Bundle of Integer type from a long value.</summary>
		/// <returns>A new Integer Bundle from a long value.</returns>
        public Bundle(long value) { type = DataType.Integer; longValue = value; }

		/// <summary>Creates a new Bundle of Double type from a float value.</summary>
		/// <returns>A new Double Bundle from a float value.</returns>
        public Bundle(float value) : this((double)value) { }

		/// <summary>Creates a new Bundle of Double type from a double value.</summary>
		/// <returns>A new Double Bundle from a double value.</returns>
		public Bundle(double value) { type = DataType.Double; doubleValue = value; }

		/// <summary>Creates a new Bundle of String type from a string value.</summary>
		/// <returns>A new String Bundle from a string value.</returns>
		public Bundle(string value) { type = DataType.String; stringValue = value; }

		/// <summary>Implicitly creates a new Bundle of Boolean type from a bool value.</summary>
		/// <returns>A new Boolean Bundle from a bool value.</returns>
		public static implicit operator Bundle(bool value) { return new Bundle(value); }

		/// <summary>Implicitly creates a new Bundle of Integer type from an int value.</summary>
		/// <returns>A new Integer Bundle from an int value.</returns>
		public static implicit operator Bundle(int value) { return new Bundle(value); }

		/// <summary>Implicitly creates a new Bundle of Integer type from a long value.</summary>
		/// <returns>A new Integer Bundle from a long value.</returns>
		public static implicit operator Bundle(long value) { return new Bundle(value); }

		/// <summary>Implicitly creates a new Bundle of Double type from a float value.</summary>
		/// <returns>A new Double Bundle from a float value.</returns>
		public static implicit operator Bundle(float value) { return new Bundle(value); }

		/// <summary>Implicitly creates a new Bundle of Double type from a double value.</summary>
		/// <returns>A new Double Bundle from a double value.</returns>
		public static implicit operator Bundle(double value) { return new Bundle(value); }

		/// <summary>Implicitly creates a new Bundle of String type from a string value.</summary>
		/// <returns>A new String Bundle from a string value.</returns>
		public static implicit operator Bundle(string value) { return value != null ? new Bundle(value) : Empty; }

		/// <summary>Implicitly gets a Bundle's value as a bool converted value.</summary>
		/// <returns>Bundle's value converted as a bool value.</returns>
		public static implicit operator bool(Bundle b) { return b.AsBool(); }

		/// <summary>Implicitly gets a Bundle's value as an int converted value.</summary>
		/// <returns>Bundle's value converted as an int value.</returns>
		public static implicit operator int(Bundle b) { return b.AsInt(); }

		/// <summary>Implicitly gets a Bundle's value as a long converted value.</summary>
		/// <returns>Bundle's value converted as a long value.</returns>
		public static implicit operator long(Bundle b) { return b.AsLong(); }

		/// <summary>Implicitly gets a Bundle's value as a float converted value.</summary>
		/// <returns>Bundle's value converted as a float value.</returns>
		public static implicit operator float(Bundle b) { return b.AsFloat(); }

		/// <summary>Implicitly gets a Bundle's value as a double converted value.</summary>
		/// <returns>Bundle's value converted as a double value.</returns>
		public static implicit operator double(Bundle b) { return b.AsDouble(); }

		/// <summary>Implicitly gets a Bundle's value as a string converted value.</summary>
		/// <returns>Bundle's value converted as a string value.</returns>
		public static implicit operator string(Bundle b) { return b.AsString(); }

		/// <summary>Call the standard Object.GetHashCode() method to compute a hash code for this Bundle.</summary>
		/// <returns>Bundle's hash code.</returns>
		public override int GetHashCode() { return base.GetHashCode(); }

		/// <summary>Compares the Bundle with another one to find out if they are equal. Compares instances
		/// references first, then tries to compare Bundles' values. Warning: The converted values are actually
		/// compared, then a "1" integer or string type Bundle would match a "true" bool type Bundle for example.</summary>
		/// <param name="b">The Bundle from which to compare the value with the current Bundle's one.</param>
		/// <returns>If the bundles' references or their converted values are equal.</returns>
		public bool Equals(Bundle b) {
			if (b == null) return false;
			if (object.ReferenceEquals(this, b)) return true; // This line handles the Empty Bundle case
			switch (b.type) {
				case DataType.Boolean: case DataType.Integer: return this.AsLong().Equals(b.longValue);
				case DataType.Double: return this.AsDouble().Equals(b.doubleValue);
				case DataType.String: return this.AsString().Equals(b.stringValue);
				case DataType.Array: return this.AsArray().Equals(b.arrayValue);
				case DataType.Object: return this.AsDictionary().Equals(b.objectValue);
				default: return false;
			}
		}

		/// <summary>Compares the Bundle with any object (expected to be another Bundle) to find out if they are
		/// equal. Compares instances references first, then tries to compare Bundles' values. Warning: The converted
		/// values are actually compared, then a "1" integer or string type Bundle would match a "true" bool type
		/// Bundle for example.</summary>
		/// <param name="obj">The object from which to compare the value with the current Bundle's one.</param>
		/// <returns>If the object's and bundle's references or their converted values are equal.</returns>
		public override bool Equals(object obj) {
			if (obj == null) return false;
			Bundle b = obj as Bundle;
			if (b != null) return Equals(b);
			else if (obj is bool) return (bool)obj ? this.AsLong().Equals(1L) : this.AsLong().Equals(0L);
			else if (obj is int) return this.AsLong().Equals((long)(int)obj);
			else if (obj is long) return this.AsLong().Equals((long)obj);
			else if (obj is float) return this.AsDouble().Equals((double)(float)obj);
			else if (obj is double) return this.AsDouble().Equals((double)obj);
			else if (obj is string) return this.AsString().Equals((string)obj);
			else return false;
		}

		/// <summary>Compares the Bundle with any object (expected to be another Bundle) to find out if they are
		/// equal. Compares instances references first, then tries to compare Bundles' values. Warning: The
		/// converted values are actually compared, then a "1" integer or string type Bundle would match a "true"
		/// bool type Bundle for example.</summary>
		/// <param name="b">The Bundle to compare the value with the object's one.</param>
		/// <param name="obj">The object from which to compare the value with the Bundle's one.</param>
		/// <returns>If the object's and bundle's references or their converted values are equal.</returns>
		public static bool operator ==(Bundle b, object obj) {
			if (((object)b == null) || (obj == null)) return Object.Equals((object)b, obj);
			else return b.Equals(obj);
		}

		/// <summary>Compares the Bundle with any object (expected to be another Bundle) to find out if they are
		/// different. Compares instances references first, then tries to compare Bundles' values. Warning: The
		/// converted values are actually compared, then a "1" integer or string type Bundle would match a "true"
		/// bool type Bundle for example.</summary>
		/// <param name="b">The Bundle to compare the value with the object's one.</param>
		/// <param name="obj">The object from which to compare the value with the Bundle's one.</param>
		/// <returns>If the object's and bundle's references or their converted values are equal.</returns>
		public static bool operator !=(Bundle b, object obj) {
			if (((object)b == null) || (obj == null)) return !Object.Equals((object)b, obj);
			else return !b.Equals(obj);
		}

		/// <summary>Gets object type (Dictionary) Bundle's key value.</summary>
		/// <param name="key">Key of the value to return.</param>
		/// <returns>A Bundle being the value of the given key.</returns>
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

		/// <summary>Gets array type (List) Bundle's index value.</summary>
		/// <param name="index">Index of the value to return.</param>
		/// <returns>A Bundle being the value of the given index.</returns>
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

		/// <summary>Tests if a Bundle (any type) has any value set.</summary>
		/// <returns>If Bundle has any value set.</returns>
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

		/// <summary>Tests if an object type (Dictionary) Bundle has a given key defined.</summary>
		/// <param name="key">The key to be searched for.</param>
		/// <returns>If the key does exist.</returns>
		public bool Has(string key) { return Dictionary.ContainsKey(key); }

		/// <summary>Removes an object type (Dictionary) Bundle's key value.</summary>
		/// <param name="key">The key to delete the value.</param>
		public void Remove(string key) { Dictionary.Remove(key); }

		/// <summary>Adds a Bundle value to this array-type (List) Bundle.</summary>
		/// <param name="value">The Bundle value to add.</param>
		public void Add(Bundle value) {
			value.parent = this;
			Array.Add (value);
		}

		/// <summary>Deep copies the bundle.</summary>
		public Bundle Clone() {
			Bundle thisNode = null;
			if (parent == null) return CloneRecursive(null, ref thisNode);
			// Clone the whole structure and return a pointer to the same node in the new structure.
			#pragma warning disable 0219
			Bundle cloned = Root.CloneRecursive(this, ref thisNode);
			#pragma warning restore 0219
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

		/// <summary>Gets Bundle's value data type. Should be DataType.None until any value is set to this Bundle.</summary>
		/// <returns>Bundle's value data type.</returns>
		public DataType Type { get { return type; } }

		/// <summary>Gets an object type (Dictionary) Bundle's key bool value.</summary>
		/// <param name="key">Key of the bool value to return.</param>
		/// <param name="defaultValue">The default bool value to return if the given key doesn't exist.</param>
		/// <returns>A bool being the value of the given key.</returns>
		[Obsolete("Will be removed soon. Use bundle[key].AsBool(defaultValue) instead.")]
		public bool GetBool(string key, bool defaultValue = false) { return Has(key) ? Dictionary[key].AsBool(defaultValue) : defaultValue; }

		/// <summary>Gets an object type (Dictionary) Bundle's key int value.</summary>
		/// <param name="key">Key of the int value to return.</param>
		/// <param name="defaultValue">The default int value to return if the given key doesn't exist.</param>
		/// <returns>A int being the value of the given key.</returns>
		[Obsolete("Will be removed soon. Use bundle[key].AsInt(defaultValue) instead.")]
		public int GetInt(string key, int defaultValue = 0) { return Has(key) ? Dictionary[key].AsInt(defaultValue) : defaultValue; }

		/// <summary>Gets an object type (Dictionary) Bundle's key long value.</summary>
		/// <param name="key">Key of the long value to return.</param>
		/// <param name="defaultValue">The default long value to return if the given key doesn't exist.</param>
		/// <returns>A long being the value of the given key.</returns>
		[Obsolete("Will be removed soon. Use bundle[key].AsLong(defaultValue) instead.")]
		public long GetLong(string key, long defaultValue = 0L) { return Has(key) ? Dictionary[key].AsLong(defaultValue) : defaultValue; }

		/// <summary>Gets an object type (Dictionary) Bundle's key float value.</summary>
		/// <param name="key">Key of the float value to return.</param>
		/// <param name="defaultValue">The default float value to return if the given key doesn't exist.</param>
		/// <returns>A float being the value of the given key.</returns>
		[Obsolete("Will be removed soon. Use bundle[key].AsFloat(defaultValue) instead.")]
		public float GetFloat(string key, float defaultValue = 0f) { return Has(key) ? Dictionary[key].AsFloat(defaultValue) : defaultValue; }

		/// <summary>Gets an object type (Dictionary) Bundle's key double value.</summary>
		/// <param name="key">Key of the double value to return.</param>
		/// <param name="defaultValue">The default double value to return if the given key doesn't exist.</param>
		/// <returns>A double being the value of the given key.</returns>
		[Obsolete("Will be removed soon. Use bundle[key].AsDouble(defaultValue) instead.")]
		public double GetDouble(string key, double defaultValue = 0d) { return Has(key) ? Dictionary[key].AsDouble(defaultValue) : defaultValue; }

		/// <summary>Gets an object type (Dictionary) Bundle's key string value.</summary>
		/// <param name="key">Key of the string value to return.</param>
		/// <param name="defaultValue">The default string value to return if the given key doesn't exist.</param>
		/// <returns>A string being the value of the given key.</returns>
		[Obsolete("Will be removed soon. Use bundle[key].AsString(defaultValue) instead.")]
		public string GetString(string key, string defaultValue = null) { return Has(key) ? Dictionary[key].AsString(defaultValue) : defaultValue; }

		/// <summary>Gets an array type (List) Bundle's index bool value.</summary>
		/// <param name="index">Index of the bool value to return.</param>
		/// <param name="defaultValue">The default bool value to return if the given index doesn't exist.</param>
		/// <returns>A bool being the value of the given index.</returns>
		[Obsolete("Will be removed soon. Use bundle[index].AsBool(defaultValue) instead.")]
		public bool GetBool(int index, bool defaultValue = false) { return index < Array.Count ? Array[index].AsBool(defaultValue) : defaultValue; }

		/// <summary>Gets an array type (List) Bundle's index int value.</summary>
		/// <param name="index">Index of the int value to return.</param>
		/// <param name="defaultValue">The default int value to return if the given index doesn't exist.</param>
		/// <returns>A int being the value of the given index.</returns>
		[Obsolete("Will be removed soon. Use bundle[index].AsInt(defaultValue) instead.")]
		public int GetInt(int index, int defaultValue = 0) { return index < Array.Count ? Array[index].AsInt(defaultValue) : defaultValue; }

		/// <summary>Gets an array type (List) Bundle's index long value.</summary>
		/// <param name="index">Index of the long value to return.</param>
		/// <param name="defaultValue">The default long value to return if the given index doesn't exist.</param>
		/// <returns>A long being the value of the given index.</returns>
		[Obsolete("Will be removed soon. Use bundle[index].AsLong(defaultValue) instead.")]
		public long GetLong(int index, long defaultValue = 0L) { return index < Array.Count ? Array[index].AsLong(defaultValue) : defaultValue; }

		/// <summary>Gets an array type (List) Bundle's index float value.</summary>
		/// <param name="index">Index of the float value to return.</param>
		/// <param name="defaultValue">The default float value to return if the given index doesn't exist.</param>
		/// <returns>A float being the value of the given index.</returns>
		[Obsolete("Will be removed soon. Use bundle[index].AsFloat(defaultValue) instead.")]
		public float GetFloat(int index, float defaultValue = 0f) { return index < Array.Count ? Array[index].AsFloat(defaultValue) : defaultValue; }

		/// <summary>Gets an array type (List) Bundle's index double value.</summary>
		/// <param name="index">Index of the double value to return.</param>
		/// <param name="defaultValue">The default double value to return if the given index doesn't exist.</param>
		/// <returns>A double being the value of the given index.</returns>
		[Obsolete("Will be removed soon. Use bundle[index].AsDouble(defaultValue) instead.")]
		public double GetDouble(int index, double defaultValue = 0d) { return index < Array.Count ? Array[index].AsDouble(defaultValue) : defaultValue; }

		/// <summary>Gets an array type (List) Bundle's index string value.</summary>
		/// <param name="index">Index of the string value to return.</param>
		/// <param name="defaultValue">The default string value to return if the given index doesn't exist.</param>
		/// <returns>A string being the value of the given index.</returns>
		[Obsolete("Will be removed soon. Use bundle[index].AsString(defaultValue) instead.")]
		public string GetString(int index, string defaultValue = null) { return index < Array.Count ? Array[index].AsString(defaultValue) : defaultValue; }

		/// <summary>Returns the parent of this bundle, if it was detached from any tree. May be null
		/// if it is the root or has never been attached.</summary>
		public Bundle Parent { get { return parent; } }

		/// <summary>Gets a Bundle's value as a bool converted value.</summary>
		/// <param name="defaultValue">The default bool value to return if Bundle's value couldn't be converted.</param>
		/// <returns>Bundle's value converted as a bool value.</returns>
		public bool AsBool(bool defaultValue = false) {
			switch (Type) {
				case DataType.Boolean: return longValue != 0;
				case DataType.Integer: return longValue != 0;
				case DataType.Double: return doubleValue != 0;
				case DataType.String: return String.Compare(stringValue, "true", true) == 0;
			}
			return defaultValue;
		}

		/// <summary>Gets a Bundle's value as a int converted value.</summary>
		/// <param name="defaultValue">The default int value to return if Bundle's value couldn't be converted.</param>
		/// <returns>Bundle's value converted as a int value.</returns>
		public int AsInt(int defaultValue = 0) {
			switch (Type) {
				case DataType.Boolean: return (int)longValue;
				case DataType.Integer: return (int)longValue;
				case DataType.Double: return (int)doubleValue;
				case DataType.String: int.TryParse(stringValue, out defaultValue); return defaultValue;
			}
			return defaultValue;
		}

		/// <summary>Gets a Bundle's value as a long converted value.</summary>
		/// <param name="defaultValue">The default long value to return if Bundle's value couldn't be converted.</param>
		/// <returns>Bundle's value converted as a long value.</returns>
		public long AsLong(long defaultValue = 0L) {
			switch (Type) {
				case DataType.Boolean: return longValue;
				case DataType.Integer: return longValue;
				case DataType.Double: return (long)doubleValue;
				case DataType.String: long.TryParse(stringValue, out defaultValue); return defaultValue;
			}
			return defaultValue;
		}

		/// <summary>Gets a Bundle's value as a float converted value.</summary>
		/// <param name="defaultValue">The default float value to return if Bundle's value couldn't be converted.</param>
		/// <returns>Bundle's value converted as a float value.</returns>
		public float AsFloat(float defaultValue = 0f) {
			switch (Type) {
				case DataType.Boolean: return (float)longValue;
				case DataType.Integer: return (float)longValue;
				case DataType.Double: return (float)doubleValue;
				case DataType.String: float.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out defaultValue); return defaultValue;
			}
			return defaultValue;
		}

		/// <summary>Gets a Bundle's value as a double converted value.</summary>
		/// <param name="defaultValue">The default double value to return if Bundle's value couldn't be converted.</param>
		/// <returns>Bundle's value converted as a double value.</returns>
		public double AsDouble(double defaultValue = 0d) {
			switch (Type) {
				case DataType.Boolean: return (double)longValue;
				case DataType.Integer: return (double)longValue;
				case DataType.Double: return doubleValue;
				case DataType.String: double.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out defaultValue); return defaultValue;
			}
			return defaultValue;
		}

		/// <summary>Gets a Bundle's value as a string converted value.</summary>
		/// <param name="defaultValue">The default string value to return if Bundle's value couldn't be converted.</param>
		/// <returns>Bundle's value converted as a string value.</returns>
		public string AsString(string defaultValue = null) {
			switch (Type) {
				case DataType.Boolean: return longValue != 0 ? "true" : "false";
				case DataType.Integer: return longValue.ToString();
				case DataType.Double: return doubleValue.ToString();
				case DataType.String: return stringValue;
			}
			return defaultValue;
		}

		/// <summary>Gets object type Bundle as a Dictionary.</summary>
		/// <returns>Bundle's data as a Dictionary.</returns>
		public Dictionary<string, Bundle> AsDictionary() { return Dictionary; }

		/// <summary>Gets array type Bundle as a List.</summary>
		/// <returns>Bundle's data as a List.</returns>
		public List<Bundle> AsArray() { return Array; }

		/// <summary>Builds a complete Bundle hierarchy representing data from a json-like string. Works even if the
		/// root json token is a simple value type like a string or a number.</summary>
		/// <param name="json">The json-like string to parse.</param>
		/// <returns>Bundle's data from a json-like string.</returns>
        public static Bundle FromAnyJson(string json)
        {
			if (json == null) return null;
			if (json.Length == 0) return null;

            int idx = json.Length-1;
            if ((json[0] == '{' && json[idx] == '}') || (json[0] == '[' && json[idx] == ']'))
                return FromJson(JsonMapper.ToObject(json));
            else if (json[0] == '\"' && json[idx] == '\"')
                return new Bundle(json.Substring(1, idx-1));
            else if (json == "true")
                return new Bundle(true);
            else if (json == "false")
                return new Bundle(false);
            else
            {
                double res;
                if (Double.TryParse(json, out res))
                    return new Bundle(res);
            }

            return null;
        }
        
		/// <summary>Builds a complete Bundle hierarchy representing data from a json-like string. Works only if the
		/// root json token is an object or an array, but not a simple value type like a string or a number (in this
		/// case use FromAnyJson() instead).</summary>
		/// <param name="json">The json-like string to parse.</param>
		/// <returns>Bundle's data from a json-like string.</returns>
		public static Bundle FromJson(string json) {
			if (json == null) return null;
			return FromJson(JsonMapper.ToObject(json));
		}

		/// <summary>Gets all bundle's data as a json-like string.</summary>
		/// <returns>Bundle's data as a json-like string.</returns>
		public string ToJson() {
			// IDEA One day we could probably implement the JSON by ourselves, without requiring LitJson :)
			return JsonMapper.ToJson(ToJson(this));
		}

		/// <summary>Gets all bundle's data as a human readable string. (actually calls ToJson())</summary>
		/// <returns>Bundle's data as a human readable string.</returns>
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
            if (bundle.Type == DataType.Object)
            {
                target.SetJsonType(JsonType.Object);
                foreach (KeyValuePair<string, Bundle> entry in bundle.Dictionary)
                {
                    switch (entry.Value.Type)
                    {
                        case DataType.Boolean: target[entry.Key] = entry.Value.AsBool(); break;
                        case DataType.Integer: target[entry.Key] = entry.Value.AsLong(); break;
                        case DataType.Double: target[entry.Key] = entry.Value.AsDouble(); break;
                        case DataType.String: target[entry.Key] = entry.Value.AsString(); break;
                        default: target[entry.Key] = ToJson(entry.Value); break;
                    }
                }
            }
            else if (bundle.type == DataType.Array)
            {
                target.SetJsonType(JsonType.Array);
                foreach (Bundle entry in bundle.Array)
                {
                    switch (entry.Type)
                    {
                        case DataType.Boolean: target.Add(entry.AsBool()); break;
                        case DataType.Integer: target.Add(entry.AsInt()); break;
                        case DataType.Double: target.Add(entry.AsDouble()); break;
                        case DataType.String: target.Add(entry.AsString()); break;
                        default: target.Add(ToJson(entry)); break;
                    }
                }
            }
            else if (bundle.type == DataType.String)
            {
                target = new JsonData(bundle.AsString());
            }
            else if (bundle.type == DataType.Integer)
            {
                target = new JsonData(bundle.AsLong());
            }
            else if (bundle.type == DataType.Double)
            {
                target = new JsonData(bundle.AsDouble());
            }
            else if (bundle.type == DataType.Boolean)
            {
                target = new JsonData(bundle.AsBool());
            }
            else /*if (bundle.type == DataType.None)*/
            {
                target = new JsonData();
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

		/// <summary>Gets empty Bundle (as an empty Bundle can't contain anything else).</summary>
		/// <param name="key">Any key (unused).</param>
		/// <returns>An empty Bundle.</returns>
		public override Bundle this[string key]
		{
			get { return Bundle.Empty; }
			set { throw new ArgumentException("Trying to assign to non-existing bundle node. Make sure you create the appropriate hierarchy."); }
		}

		/// <summary>Gets empty Bundle (as an empty Bundle can't contain anything else).</summary>
		/// <param name="index">Any index (unused).</param>
		/// <returns>An empty Bundle.</returns>
		public override Bundle this[int index]
		{
			get { return Bundle.Empty; }
			set { throw new ArgumentException("Trying to assign to non-existing bundle node. Make sure you create the appropriate hierarchy."); }
		}
	}
}
