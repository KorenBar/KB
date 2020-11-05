using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace KB.Data
{
    /// <summary>
    /// Create a new ini file to store or load data
    /// </summary>
    public class IniFile
    {
        /// <summary>
        /// Get ini file path.
        /// </summary>
        public string FileName { get; private set; }

        public Dictionary<string, string> this[string section] { get => ToDictionary()[section]; set => WriteValues(section, value); }

        public string this[string section, string key] { get => ReadValue(section, key); set => WriteValue(section, key, value); }

        /// <summary>
        /// IniFile Constructor
        /// </summary>
        /// <param name="filePath"></param>
        public IniFile(string fileName)
        {
            FileName = Path.GetFullPath(fileName);
        }

        private static bool IsSectionName(string line)
        {
            string l = line.Trim();
            return l.StartsWith("[") && l.EndsWith("]") && !l.Contains("=");
        }

        // TODO: this external function does not supports utf-8 writing, need to be replaced with custom code.
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        /// <summary>
		/// Write Data to the INI File
		/// </summary>
		/// <param name="section"></param>
		/// Section name
		/// <param name="key"></param>
		/// Key Name
		/// <param name="value"></param>
		/// Value Name
		public void WriteValue(string section, string key, string value)
        {
            if (!File.Exists(FileName))
            {
                Directory.CreateDirectory(Directory.GetParent(FileName).FullName);
                File.Create(FileName).Close();
            }
            value = Regex.Replace(value, @"\r\n|\t|\n|\r", @"\r\n");
            WritePrivateProfileString(section, key, value, FileName);
        }

        /// <summary>
        /// Write Data from Dictionary to section in the Ini File
        /// </summary>
        /// <param name="dictionary"></param>
        public void WriteValues(string section, Dictionary<string, string> dictionary)
        {
            var d = this.ToDictionary();
            d[section] = dictionary;
            WriteValues(d);
        }

        /// <summary>
        /// Write Data from Dictionary to the Ini File
        /// </summary>
        /// <param name="dictionary"></param>
        public void WriteValues(Dictionary<string, Dictionary<string, string>> dictionary)
        {
            if (!File.Exists(FileName))
            {
                Directory.CreateDirectory(Directory.GetParent(FileName).FullName);
                File.Create(FileName).Close();
            }
            foreach (var secDic in dictionary)
                foreach (var keyValue in secDic.Value)
                    WritePrivateProfileString(secDic.Key, keyValue.Key, Regex.Replace(keyValue.Value, @"\r\n|\t|\n|\r", @"\r\n"), FileName);
        }

        /// <summary>
		/// Read Data Value From the Ini File
		/// </summary>
		/// <param name="section"></param>
		/// <param name="key"></param>
		/// <returns></returns>
		public string ReadValue(string section, string key)
        {
            StringBuilder temp = new StringBuilder(255);
            if (File.Exists(FileName))
                GetPrivateProfileString(section, key, "", temp, 255, FileName);
            return temp.ToString();
        }

        /// <summary>
        /// Read All Data From section in the Ini File to Dictionary
        /// </summary>
        /// <returns>If section does not exists returns an empty dictionary.</returns>
        public Dictionary<string, string> ToDictionary(string section) => ToDictionary().Where(i => i.Key == section).DefaultIfEmpty(new KeyValuePair<string, Dictionary<string, string>>(string.Empty, new Dictionary<string, string>())).First().Value;

        /// <summary>
        /// Read All Data From the Ini File to Dictionary
        /// </summary>
        /// <returns>If section does not exists returns an empty dictionary.</returns>
        public Dictionary<string, Dictionary<string, string>> ToDictionary()
        {
            var dictionary = new Dictionary<string, Dictionary<string, string>>();
            if (File.Exists(FileName))
            {
                using (StreamReader sr = File.OpenText(FileName))
                {
                    string sectionName = "";
                    var sectionDic = new Dictionary<string, string>();
                    string s = string.Empty;
                    while ((s = sr.ReadLine()) != null)
                    {
                        if (!s.TrimStart().StartsWith(";")) // Make sure that line is not a comment.
                            if (IsSectionName(s))
                            {
                                dictionary.Add(sectionName, sectionDic);
                                sectionName = s.Trim().Substring(1, s.Length - 2);
                                sectionDic = new Dictionary<string, string>();
                            }
                            else
                            {
                                string[] ls = s.Split(new char[] { '=' }, 2);
                                if (ls.Length > 1)
                                    if (!sectionDic.Keys.Contains(ls[0].Trim()))
                                        sectionDic.Add(ls[0].Trim(), ls[1].Trim().Replace(@"\r\n", Environment.NewLine));
                            }
                    }
                    if (dictionary.ContainsKey(sectionName))
                        foreach (var kv in sectionDic)
                            dictionary[sectionName][kv.Key] = kv.Value;
                    else
                        dictionary[sectionName] = sectionDic;
                }
            }
            return dictionary;
        }

        /// <summary>
        /// Read All Text From the Ini File
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (File.Exists(FileName))
                return File.ReadAllText(FileName);
            return string.Empty;
        }
    }
}