using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KB.Data
{
    public class JSONData : System.Collections.Generic.IEnumerable<JSONData>
    {
        // Notes for Newtonsoft.Json:
        // 1. DeserializeObject("Clear String") returns error. (Add '' to avoid this error => DeserializeObject("'Value String'"))
        // 2. SerializeObject(null) returns "null" string.
        // 3. JObject.ToString() returns designed JSON.
        // 4. DeserializeObject<List<object>>("No JSON List") returns error.
        // 5. (DeserializeObject("{'a':'1'}") as JArray) = null.
        // 6. JObject["No Exists Key"] returns null.
        // 7. DeserializeObject("'Text'") returns string.
        // 8. new JValue(null) returns error. (Use JValue.CreateString(string) for string case)
        // 9. "JObject[key] = null/string/bool" make JObject[key] to JValue.
        // 10. "JObject[key] = JObject[key]"" with the same key deos nothing.
        // 11. SerializeObject(JValue.CreateString(string)) also returns "null" string. (Note 2 and 8)

        public static bool IsValidJson(string str)
        {
            if (string.IsNullOrEmpty(str)) return false;
            string input = str.Trim();
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
        public string Key { get; private set; }

        public JSONData Parent { get; private set; }

        private List<JSONData> Children { get; set; } // Contains any child that was created and still connected, even if the child does not really exists on the JToken.

        private JToken Source { get; set; }

        public JTokenType Type { get { return this.Source != null ? this.Source.Type : JTokenType.None; } }

        public string Value // String Value
        {
            get 
            {
                return this.Source != null // Note 2
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

        public bool IsExists { get { return this.Source != null; } }

        public bool IsList { get { return this.Type == JTokenType.Array; } }
        #endregion

        #region Constructors
        public JSONData()
            : this(null)
        {

        }

        public JSONData(string json)
            : this((json != null ? JsonConvert.DeserializeObject(json) : null) as JToken)
        {

        }

        public JSONData(JToken source)
            : this(source, null, null)
        {

        }

        private JSONData(JToken source, string key, JSONData parent)
        {
            this.Source = source;
            this.Key = key;
            this.Parent = parent;
            this.Children = new List<JSONData>();
        }
        #endregion

        private JToken GetChildObject(string key)
        {
            return this.Source != null && this.Source is JObject ? (this.Source as JObject)[key] : null;
        }

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
                if (value == null) // "jd[key] = null" is same as "jd.Remove(key)".
                    if (this.Remove(key)) return;
                    else return;

                // Assuming that value != null
                // If the value is a child of this and with the same key, end here.
                if (this.Children.FirstOrDefault(c => c.Key == key) == value) return;

                // Disconnect current child. (if there is a child) (Needed in case that value has JToken and this JToken is JObject, else Set function will done this)
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

        public void Add(string key, IEnumerable<JSONData> value)
        {
            Add(key, value.ToArray());
        }

        public void Add(string key, JSONData[] value)
        {
            JSONData jd = new JSONData();
            jd.Set(value);
            Add(key, jd);
        }

        public void Add(string key, JSONData value)
        {
            this[key] = value;
        }

        public void Add(string key, string value)
        {
            this.Add(key, JValue.CreateString(value));
        }

        public void Add(string key, JToken value)
        {
            if (key != null)
                if (this.Children.FirstOrDefault(child => child.Key == key) == null)
                    this[key] = new JSONData(value);
                else
                    throw new ArgumentException("The key already exists.", "key");
            else
                throw new ArgumentException("Parameter cannot be null.", "key");
        }

        public void Set(IEnumerable<JSONData> list)
        {
            this.Set(list.ToArray());
        }

        public void Set(JSONData[] array)
        {
            this.Set(JArray.FromObject(array.Select(i => i.Source))); // .ToArray() ?
        }

        public bool Set(JToken jtoken) // Recreate Source
        {
            this.DisconnectAnyChildren();
            this.Source = jtoken;
            return ReconnectParent();
        }

        private bool DisconnectAnyChildren()
        {
            if (this.Children.Count == 0) return false;
            foreach (JSONData child in this.Children)
                child.Parent = null;
            this.Children.Clear();
            return true;
        }

        private bool ReconnectParent() // Return True if parent is connected.
        {
            if (this.Parent == null) return false;
            JSONData parent = this.Parent;
            this.Remove();
            parent[this.Key] = this;
            return true;
        }

        public bool ContainsKey(string key)
        {
            return this.Source != null && this.Source is JObject && (this.Source as JObject)[key] != null;
        }

        public bool Merge(JSONData jsonData)
        {
            // TODO:
            return false;
        }

        public bool Remove() // Remove this from parent
        {
            if (this.Parent != null)
                return this.Parent.Remove(this.Key);
            else
                return false;
        }

        public bool Remove(string key) // Remove key from this
        {
            return Remove(key, true);
        }

        private bool Remove(string key, bool disconnect) // Remove key from this
        {
            if (disconnect)
            {   // Disconnect Child
                foreach (JSONData child in this.Children.Where(c => c.Key == key))
                    child.Parent = null;
                this.Children.RemoveAll(c => c.Key == key);
            }

            // Remove from JObject
            if (this.Source != null && this.Source is JObject && (this.Source as JObject).Remove(key)) 
                return true;
            return false;
        }

        // Note: If source object is not a list return will be an empty list.
        public List<JSONData> GetList()
        {
            JArray arr = this.Source != null && this.Source is JArray ? (JArray)this.Source : null; // Note 5
            List<JSONData> result = new List<JSONData>();
            if (arr != null)
                foreach (JToken o in arr.ToObject<IList<object>>())
                    result.Add(new JSONData(o));
            return result;
        }

        public JSONData Clone()
        {
            return new JSONData(this.Source.DeepClone(), this.Key, null);
        }

        public override string ToString()
        {
            return this.Source != null ? this.Source.ToString() : string.Empty;
        }

        public Dictionary<string, JSONData> ToDictionary()
        {
            return this.ToDictionary(j => j.Key);
        }

        public IEnumerator<JSONData> GetEnumerator()
        {
            return this.Source != null
                ? this.Source is JObject
                    ? ((this.Source as JObject).Properties().Select<JProperty, JSONData>(j => this[j.Name])).GetEnumerator()
                    : new List<JSONData>().GetEnumerator()
                : new List<JSONData>().GetEnumerator();
        }

        public void Dispose()
        {
            ((IEnumerator<JSONData>)this).Dispose();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        IEnumerator<JSONData> IEnumerable<JSONData>.GetEnumerator()
        {
            return (this).GetEnumerator();
        }
    }
}
