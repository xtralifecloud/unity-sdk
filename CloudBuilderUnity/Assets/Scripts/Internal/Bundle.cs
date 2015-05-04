using System;
using System.Collections.Generic;
using System.Text;
using LitJson;
using System.Collections;

namespace CloudBuilderLibrary
{
	public class Bundle {
		public enum DataType {
			None, Boolean, Integer, Double, String, Array, Object
		}
		public static readonly EmptyBundle Empty = new EmptyBundle();
		private DataType type;
		private double doubleValue;
		private long longValue;
		private string stringValue;
		private Dictionary<string, Bundle> objectValue;
		private List<Bundle> arrayValue;

		// Explicit constructors
		public static Bundle CreateArray() {
			return new Bundle(DataType.Array);
		}
		public static Bundle CreateObject() {
			return new Bundle(DataType.Object);
		}

		// Construction (internal)
		protected Bundle(DataType dataType) {
			type = dataType;
			if (dataType == DataType.Object) objectValue = new Dictionary<string, Bundle>();
			else if (dataType == DataType.Array) arrayValue = new List<Bundle>();
		}
		public Bundle(bool value) { type = DataType.Boolean; longValue = value ? 1 : 0; }
		public Bundle(long value) { type = DataType.Integer; longValue = value; }
		public Bundle(double value) { type = DataType.Double; doubleValue = value; }
		public Bundle(string value) { type = DataType.String; stringValue = value; }
		public static implicit operator Bundle(bool value) { return new Bundle(value); }
		public static implicit operator Bundle(long value) { return new Bundle(value); }
		public static implicit operator Bundle(double value) { return new Bundle(value); }
		public static implicit operator Bundle(string value) { return new Bundle(value); }
		public static implicit operator bool(Bundle b) { return b.AsBool(); }
		public static implicit operator int(Bundle b) { return b.AsInt(); }
		public static implicit operator long(Bundle b) { return b.AsLong(); }
		public static implicit operator double(Bundle b) { return b.AsDouble(); }
		public static implicit operator string(Bundle b) { return b.AsString(); }

		public virtual Bundle this [string key] {
			get { return Has(key) ? Dictionary[key] : Empty; }
			set { Dictionary[key] = value; }
		}
		public virtual Bundle this[int index] {
			get { return Array[index]; }
			set {
				if (index != -1)
					Array[index] = value;
				else
					Array.Add(value);
			}
		}
		public void Add(Bundle value) {
			Array.Add (value);
		}

		// Dictionary getters
		public bool GetBool(string key, bool defaultValue = false) {
			return Has(key) ? Dictionary[key].AsBool(defaultValue) : defaultValue;
		}
		public int GetInt(string key, int defaultValue = 0) {
			return Has(key) ? Dictionary[key].AsInt(defaultValue) : defaultValue;
		}
		public long GetLong(string key, long defaultValue = 0) {
			return Has(key) ? Dictionary[key].AsLong(defaultValue) : defaultValue;
		}
		public double GetDouble(string key, double defaultValue = 0) {
			return Has(key) ? Dictionary[key].AsDouble(defaultValue) : defaultValue;
		}
		public string GetString(string key, string defaultValue = null) {
			return Has(key) ? Dictionary[key].AsString(defaultValue) : defaultValue;
		}

		// Array getters
		public bool GetBool(int index) {
			return Array[index].AsBool();
		}
		public int GetInt(int index) {
			return Array[index].AsInt();
		}
		public long GetLong(int index) {
			return Array[index].AsLong();
		}
		public double GetDouble(int index) {
			return Array[index].AsDouble();
		}
		public string GetString(int index) {
			return Array[index].AsString();
		}

		// Key management
		public bool Has(string key) {
			return Dictionary.ContainsKey(key);
		}
		public void Remove(string key) {
			Dictionary.Remove(key);
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
		public double AsDouble(double defaultValue = 0) {
			double result = defaultValue;
			switch (Type) {
				case DataType.Boolean: return (int)longValue;
				case DataType.Integer: return (int)longValue;
				case DataType.Double: return (int)doubleValue;
				case DataType.String: double.TryParse(stringValue, out result); return result;
			}
			return defaultValue;
		}
		public string AsString(string defaultValue = null) {
			switch (Type) {
				case DataType.Boolean: return longValue != 0 ? "true" : "false";
				case DataType.Integer: return longValue.ToString();
				case DataType.Double: return longValue.ToString();
				case DataType.String: return stringValue;
			}
			return defaultValue;
		}

		// Json methods
		public static Bundle FromJson(string json) {
			return FromJson(JsonMapper.ToObject(json));
		}

		public string ToJson() {
			// IDEA One day we could probably implement the JSON by ourselves, without requiring LitJson :)
			return JsonMapper.ToJson(ToJson(this));
		}

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
					subBundle[key] = FromJson((JsonData) dict[key]);
				}
				return subBundle;
			}
			if (data.IsArray) {
				Bundle subBundle = Bundle.CreateArray();
				IList dict = (IList) data;
				foreach (JsonData value in dict) {
					subBundle.Add(FromJson(value));
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

	/**
	 * Never instantiate this class. Use Bundle.Empty instead. Pass that everywhere an explicit configuration is not wanted.
	 */
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
