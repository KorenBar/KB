using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KB.Data
{
    /// <summary>
    /// Use JSON as dynamic IEnumerable.
    /// </summary>
    public class JSONData : System.Collections.Generic.IEnumerable<JSONData>
    {
        // Notes for Newtonsoft.Json:
        // 1. DeserializeObject("Clear String") returns error. (Add '' to avoid this error => DeserializeObject("'Value String'"))
        // 2. SerializeObject(null) returns "null" string.
        // 3. JObject.ToString() returns designed JSON.
        // 4. DeserializeObject<List<object>>("No JSON List") returns error.
        // 5. (DeserializeObject("{'a':'1'}") as JArray) = null. (this is how the "as" operator works)
        // 6. JObject["No Exists Key"] returns null.
        // 7. DeserializeObject("'Text'") returns string.
        // 8. new JValue(null) returns error. (Use JValue.CreateString(string) for string case)
        // 9. "JObject[key] = null/string/bool" make JObject[key] to JValue.
        // 10. "JObject[key] = JObject[key]"" with the same key deos nothing.
        // 11. SerializeObject(JValue.CreateString(string)) also returns "null" string. (Note 2 and 8)

        /// <summary>
        /// Check if json string is valid and deserializable.
        /// </summary>
        /// <param name="str">the json string to check</param>
        /// <returns>true if the string is a valid json and deserializable</returns>
        public static bool IsValidJson(string str)
        {
            if (string.IsNullOrEmpty(str)) return false;
            string input = str.Trim();
            // TODO: Use the regular expression for better code.
            if ((input.StartsWith("{") && input.EndsWith("}"))
                || (input.StartsWith("[") && input.EndsWith("]"))
                || (input.StartsWith("\"") && input.EndsWith("\"")))
                try
                {
                    var obj = JToken.Parse(input);
                    return true;
                }
                catch (JsonReaderException jex)
                {
                    // Exception in parsing json.
                    Console.WriteLine(jex.Message);
                    return false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return false;
                }
            else return false;
        }

        #region Properties
        /// <summary>
        /// Get the key of this data.
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// Get the object that contains this data. 
        /// </summary>
        public JSONData Parent { get; private set; }

        /// <summary>
        /// Contains any child that was created and still connected to this, even if the child does not really exists on the JToken (conceptual).
        /// </summary>
        private List<JSONData> Children { get; set; }

        /// <summary>
        /// The JToken this object is based on, null as long as this object is conceptual (not actually exists).
        /// </summary>
        private JToken Source { get; set; }

        /// <summary>
        /// Get the type of this data, none will be returned if not actually exists.
        /// </summary>
        public JTokenType Type => this.IsExists ? this.Source.Type : JTokenType.None;   

        /// <summary>
        /// Get true if this data is actually exists or false if conceptual.
        /// </summary>
        public bool IsExists => this.Source != null;

        /// <summary>
        /// Get true this data is an array.
        /// </summary>
        public bool IsArray => this.Type == JTokenType.Array;

        /// <summary>
        /// Get or set this data as string.
        /// </summary>
        public string Value
        {
            get
            {
                return this.IsExists // Note 2
                    ? this.Source.Type == JTokenType.String
                        ? this.Source.ToString()
                        : this.Source is JValue && (this.Source as JValue).Value == null // Note 11
                            ? null
                            : JsonConvert.SerializeObject(this.Source)
                    : null;
            }
            set
            {
                this.Set(JValue.CreateString(value)); // Note 7 and 8
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a conceptual data.
        /// </summary>
        public JSONData() : this(null) { }

        /// <summary>
        /// Deserialize string to data
        /// </summary>
        /// <param name="json"></param>
        public JSONData(string json) : this((json != null ? JsonConvert.DeserializeObject(json) : null) as JToken) { }

        /// <summary>
        /// Create a new instance with JToken source.
        /// </summary>
        /// <param name="source">the JToken source of this data</param>
        public JSONData(JToken source) : this(source, null, null) { }

        /// <summary>
        /// Create a new instance with JToken source, key and a parent. (only for parent to create his childern)
        /// </summary>
        /// <param name="source">the JToken source of this data</param>
        /// <param name="key">the key of this data</param>
        /// <param name="parent">the parent that should contains this data</param>
        private JSONData(JToken source, string key, JSONData parent)
        {
            this.Source = source;
            this.Key = key;
            this.Parent = parent;
            this.Children = new List<JSONData>();
        }
        #endregion

        /// <summary>
        /// Get a child of this source.
        /// </summary>
        /// <param name="key">the key of the child to get.</param>
        /// <returns>the child from the source or null if not exists or not an object.</returns>
        private JToken GetChildObject(string key) => (this.Source as JObject)?[key];

        /// <summary>
        /// Get or set data on this object by key.
        /// if this data is not an object it will be overwritten to an object when setting a child of it.
        /// </summary>
        /// <param name="key">The key of the child data</param>
        /// <returns>if the key exists you will get the child data, if not exists or this data is not an object you will get a conceptual data that will be actually exists just when setting it or one of its children.</returns>
        public JSONData this[string key]
        {
            get
            {
                JSONData child = this.Children.FirstOrDefault(c => c.Key == key);
                if (child != null) return child;
                JSONData jd = new JSONData(this.GetChildObject(key), key, this);
                this.Children.Add(jd);
                return jd;
            }
            set
            {
                if (value == null) // "this[key] = null" is same as "this.Remove(key)". why?
                    if (this.Remove(key)) return;
                    else return;

                // Assuming that value != null
                // If the value is a child of this and with the same key, end here.
                if (this.Children.FirstOrDefault(c => c.Key == key) == value) return;

                // Disconnect current child (if there is). (Needed in case that value has JToken and this JToken is JObject, else Set function will done this)
                this.Remove(key);

                // Connect value JToken:
                // JToken connecting is important to be done before connecting value as child to this,
                // For the reason that Set function will disconnect any child.
                if (value.IsExists) // If value has JToken
                    if (this.Source == null || !(this.Source is JObject)) // If this JToken is not a JObject
                        this.Set(new JObject() { { key, value.Source } }); // Set this JToken to new JObject with value JToken as child.
                    else // This JToken is a JObject.
                        this.Source[key] = value.Source; // Set value JToken as child of this JToken. (This will save the other children of this JToken)
                else // Value JToken is null
                    this.Remove(key, false); // Make sure this JToken does not contains key.

                // Connect value as child to this.
                value.Remove(); // Disconnect value from his current parent. (if there is a parent)
                value.Key = key; // Set value key.
                value.Parent = this; // Set value parent.
                this.Children.Add(value); // Add value to this children.
            }
        }

        #region Adders
        /// <summary>
        /// Add an array as child of this data.
        /// </summary>
        /// <param name="key">the key of the array child.</param>
        /// <param name="value">the IEnumerable to add as array child.</param>
        public void Add(string key, IEnumerable<JSONData> value) => Add(key, value.ToArray());

        /// <summary>
        /// Add an array as child of this data.
        /// </summary>
        /// <param name="key">the key of the array child.</param>
        /// <param name="value">the child.</param>
        public void Add(string key, JSONData[] value)
        {
            JSONData jd = new JSONData();
            jd.Set(value);
            Add(key, jd);
        }

        /// <summary>
        /// Add a data as child of this data.
        /// </summary>
        /// <param name="key">the key of the child.</param>
        /// <param name="value">the child.</param>
        public void Add(string key, JSONData value) => this[key] = value;

        /// <summary>
        /// Add a string as child of this data.
        /// </summary>
        /// <param name="key">the key of the child.</param>
        /// <param name="value">the child.</param>
        public void Add(string key, string value) => this.Add(key, JValue.CreateString(value));

        /// <summary>
        /// Add a JToken as child of this data.
        /// </summary>
        /// <param name="key">the key of the child.</param>
        /// <param name="value">the child.</param>
        public void Add(string key, JToken value)
        {
            if (key != null)
                if (this.Children.All(c => c.Key != key)) // the key does not exists.
                    this[key] = new JSONData(value);
                else throw new ArgumentException("The key already exists.", "key");
            else throw new ArgumentException("Parameter cannot be null.", "key");
        }
        #endregion

        #region Setters
        /// <summary>
        /// Set this data as array (overwriting).
        /// </summary>
        /// <param name="array">the data to set</param>
        public void Set(IEnumerable<JSONData> array) => this.Set(array.ToArray());

        /// <summary>
        /// Set this data as array (overwriting).
        /// </summary>
        /// <param name="array">the data array to set</param>
        public void Set(JSONData[] array) => this.Set(JArray.FromObject(array.Select(i => i.Source)));

        /// <summary>
        /// Set this data as some JToken.
        /// </summary>
        /// <param name="jToken">the JToken to set as source of this data</param>
        /// <returns></returns>
        public bool Set(JToken jToken) // Recreate Source
        {
            this.DisconnectAnyChildren();
            this.Source = jToken; // We should not dispose the old JToken source, because it may in use of children.
            return ReconnectParent();
        }
        #endregion

        /// <summary>
        /// Remove children from this and set them with no parent.
        /// </summary>
        /// <returns>true if was children</returns>
        private bool DisconnectAnyChildren()
        {
            if (this.Children.Count == 0) return false;
            foreach (JSONData child in this.Children)
                child.Parent = null;
            this.Children.Clear();
            return true;
        }

        /// <summary>
        /// Re-set this data as child of his parent with the same key.
        /// </summary>
        /// <returns>true if has a parent</returns>
        private bool ReconnectParent()
        {
            if (this.Parent == null) return false;
            JSONData parent = this.Parent;
            this.Remove();
            parent[this.Key] = this;
            return true;
        }

        /// <summary>
        /// Check if child exists on this object data by key.
        /// </summary>
        /// <param name="key">the key of the child to check for</param>
        /// <returns>true if a child with that key is exists or false if not exists or not an object</returns>
        public bool ContainsKey(string key) => ((this.Source as JObject)?[key] ?? null) != null;

        /// <summary>
        /// Merge another data to this data if both of the same type.
        /// </summary>
        /// <param name="jsonData">the data to merge into this data</param>
        /// <returns>true if was possible to merge</returns>
        private bool Merge(JSONData jsonData)
        {
            // TODO: write that method and make it public.
            return false;
        }

        /// <summary>
        /// Remove this data from his parent, this data will stay usable whit no parent.
        /// </summary>
        /// <returns>true if had a parent, otherwise false</returns>
        public bool Remove() => this.Parent?.Remove(this.Key) ?? false;

        /// <summary>
        /// Remove a child of this data by key.
        /// </summary>
        /// <param name="key">key of the child to remove.</param>
        /// <returns>true if was exist or false if was not exist or not an object.</returns>
        public bool Remove(string key) => Remove(key, true);

        /// <summary>
        /// Remove a child of this data by key.
        /// </summary>
        /// <param name="key">key of the child to remove.</param>
        /// <param name="disconnect">whether keep it as conceptual child of this data or disconnect it</param>
        /// <returns>true if was exist or false if was not exist or not an object.</returns>
        private bool Remove(string key, bool disconnect) // Remove key from this
        {
            if (disconnect)
            {   // Disconnect Child
                foreach (JSONData child in this.Children.Where(c => c.Key == key))
                    child.Parent = null;
                this.Children.RemoveAll(c => c.Key == key);
            }

            // Remove from JObject
            return (this.Source as JObject)?.Remove(key) ?? false;
        }

        /// <summary>
        /// Get this array data as a list.
        /// </summary>
        /// <returns>returns this array data as a list, if this data is not an array an empty list will be returned.</returns>
        public List<JSONData> GetList()
        {
            JArray arr = this.Source as JArray; // will be null if not exists or not an array
            List<JSONData> result = new List<JSONData>();
            if (arr != null) // this data is an array
                foreach (JToken o in arr.ToObject<IList<object>>())
                    result.Add(new JSONData(o));
            return result;
        }

        /// <summary>
        /// Deep clone this data.
        /// </summary>
        /// <returns>Cloned data</returns>
        public JSONData Clone() => new JSONData(this.Source.DeepClone(), this.Key, null);

        /// <summary>
        /// Get this data as beautified json.
        /// </summary>
        /// <returns>Beautified json string</returns>
        public override string ToString() => this.IsExists ? this.Source.ToString() : string.Empty;

        /// <summary>
        /// Get this object data as dictionary.
        /// </summary>
        /// <returns>this object data as dictionary, if this data is not an object an empty dictionary will be returned.</returns>
        public Dictionary<string, JSONData> ToDictionary() => this.ToDictionary(j => j.Key);

        /// <summary>
        /// Get this object data as IEnumerator.
        /// </summary>
        /// <returns>this object data as IEnumerator, if this data is not an object an empty IEnumerator will be returned.</returns>
        public IEnumerator<JSONData> GetEnumerator()
        {
            return this.IsExists
                ? this.Source is JObject
                    ? ((this.Source as JObject).Properties().Select<JProperty, JSONData>(j => this[j.Name])).GetEnumerator()
                    : new List<JSONData>().GetEnumerator()
                : new List<JSONData>().GetEnumerator();
        }

        /// <summary>
        /// Dispose this data and its resources.
        /// </summary>
        public void Dispose()
        { // TODO: What we have to dispose here?
            ((IEnumerator<JSONData>)this).Dispose();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => this.GetEnumerator();

        IEnumerator<JSONData> IEnumerable<JSONData>.GetEnumerator() => this.GetEnumerator();
    }
}
