using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KB.Data;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace KB.Configuration
{
    public class Ini
    {
        public static Ini Default { get; set; } = 
            new Ini((from f in new StackTrace().GetFrames() select f.GetMethod().ReflectedType.Assembly.GetName().Name).Distinct().Last() + ".ini");
        
        public IniFile IniFile { get; private set; }

        public Ini(string fileName)
        {
            this.IniFile = new IniFile(fileName);
        }

        /// <summary>
        /// Create default ini file if not exists.
        /// </summary>
        /// <returns>True if default file created or false if already exists.</returns>
        public bool CreateDefault(string content)
        {
            string fn = this.IniFile.FileName;
            if (File.Exists(fn))
                return false;
            File.WriteAllText(fn, content);
            return true;
        }

        public object LoadProperties(object obj) => LoadProperties(obj, obj.GetType().Name);

        public object LoadProperties(object obj, string sectionName) => LoadProperties(obj, IniFile.ToDictionary(sectionName));

        public object LoadProperties(object obj, Dictionary<string, string> properties) => LoadProperties(obj, properties.ToDictionary(i => i.Key, i => i.Value as object));

        public object LoadProperties(object obj, Dictionary<string, object> properties)
        {
            foreach (PropertyInfo pi in obj.GetType().GetProperties())
                if (properties.Keys.Contains(pi.Name) && pi.CanWrite)
                    try
                    {
                        if (pi.PropertyType.IsEnum)
                            pi.SetValue(obj, Enum.Parse(pi.PropertyType, (string)properties[pi.Name]));
                        else if (pi.PropertyType == typeof(TimeSpan))
                            pi.SetValue(obj, TimeSpan.Parse(properties[pi.Name] as string));
                        else
                            pi.SetValue(obj, Convert.ChangeType(properties[pi.Name], pi.PropertyType), null);
                    }
                    catch { }
            return obj;
        }
    }
}
