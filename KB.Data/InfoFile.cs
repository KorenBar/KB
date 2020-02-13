using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace KB.Data
{
    /// <summary>
    /// Create a new info file to store or load data
    /// </summary>
    public class InfoFile
    {

        private string filePath;

        public string this[string key]
        {
            get
            {
                return this.ReadValue(key);
            }
            set
            {
                this.WriteValue(key, value);
            }
        }

        /// <summary>
        /// InfoFile Constructor
        /// </summary>
        /// <param name="filePath"></param>
        public InfoFile(string filePath)
        {
            this.filePath = filePath;
            if (!File.Exists(filePath))
            {
                Directory.CreateDirectory(Directory.GetParent(filePath).FullName);
                File.Create(filePath).Close();
            }
        }

        /// <summary>
        /// Write Data to the Info File
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void WriteValue(string key, string value)
        {
            if (!File.Exists(filePath))
            {
                Directory.CreateDirectory(Directory.GetParent(filePath).FullName);
                File.Create(filePath).Close();
            }
            value = Regex.Replace(value, @"\r\n|\t|\n|\r", @"\r\n");
            string[] lines = File.ReadAllLines(filePath);
            for (int l = 0; l < lines.Length; l++)
            {
                string[] lineSplit = lines[l].Split(new char[] { '=' }, 2);
                if (lineSplit[0].Trim() == key && lineSplit.Length > 1)
                {
                    lines[l] = lineSplit[0] + "=" + value;
                    File.WriteAllLines(filePath, lines);
                    return;
                }
            }
            List<string> linesList = new List<string>(lines);
            linesList.Add(key + "=" + value);
            File.WriteAllLines(filePath, linesList.ToArray());
        }

        /// <summary>
        /// Write Data from Dictionary to the Info File
        /// </summary>
        /// <param name="dictionary"></param>
        public void WriteValues(Dictionary<string, string> dictionary)
        {
            if (!File.Exists(filePath))
            {
                Directory.CreateDirectory(Directory.GetParent(filePath).FullName);
                File.Create(filePath).Close();
            }
            string[] lines = File.ReadAllLines(filePath);
            List<string> newLines = new List<string>();
            foreach (KeyValuePair<string, string> keyValue in dictionary)
            {
                string value = Regex.Replace(keyValue.Value, @"\r\n|\t|\n|\r", @"\r\n");
                bool newLine = true;
                for (int l = 0; l < lines.Length; l++)
                {
                    string[] lineSplit = lines[l].Split(new char[] { '=' }, 2);
                    if (lineSplit[0].Trim() == keyValue.Key && lineSplit.Length > 1)
                    {
                        lines[l] = lineSplit[0] + "=" + value;
                        newLine = false;
                        break;
                    }
                }
                if (newLine)
                    newLines.Add(keyValue.Key + "=" + value);
            }
            File.WriteAllLines(filePath, lines.Concat(newLines));
        }

        /// <summary>
        /// Read Data Value From the Info File
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string ReadValue(string key)
        {
            if (File.Exists(filePath))
            {
                using (StreamReader sr = File.OpenText(filePath))
                {
                    string s = string.Empty;
                    while ((s = sr.ReadLine()) != null)
                    {
                        string[] ls = s.Split(new char[] { '=' }, 2);
                        if (ls[0].Trim() == key && ls.Length > 1)
                            return ls[1].Trim().Replace(@"\r\n", Environment.NewLine);
                    }
                }
            }
            return string.Empty;

            // Second way
            //if (File.Exists(filePath))
            //{
            //    string line = File.ReadAllLines(filePath).FirstOrDefault(l => l.TrimStart().StartsWith(key) && l.Split(new char[] { '=' }, 2).Length > 1);
            //    if (line != null)
            //        return line.Split(new char[] { '=' }, 2)[1].Trim();
            //}
            //return string.Empty;
        }

        /// <summary>
        /// Read All Data From the Info File to Dictionary
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> ToDictionary()
        {
            if (File.Exists(filePath))
            {
                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                using (StreamReader sr = File.OpenText(filePath))
                {
                    string s = string.Empty;
                    while ((s = sr.ReadLine()) != null)
                    {
                        string[] ls = s.Split(new char[] { '=' }, 2);
                        if (ls.Length > 1)
                            if (!dictionary.Keys.Contains(ls[0].Trim()))
                                dictionary.Add(ls[0].Trim(), ls[1].Trim().Replace(@"\r\n", Environment.NewLine));
                    }
                }
                return dictionary;
            }
            return new Dictionary<string, string>();

            // Second way
            //if (File.Exists(filePath))
            //{
            //    Dictionary<string, string> dictionary = new Dictionary<string, string>();
            //    foreach (string line in File.ReadAllLines(filePath).Where(l => l.Split(new char[] { '=' }, 2).Length > 1))
            //    {
            //        string[] ls = line.Split(new char[] { '=' }, 2);
            //        if (!dictionary.Keys.Contains(ls[0].Trim()))
            //            dictionary.Add(ls[0].Trim(), ls[1].Trim());
            //    }
            //    return dictionary;
            //}
            //return new Dictionary<string,string>();
        }

        /// <summary>
        /// Read All Text From the Info File
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (File.Exists(filePath))
                return File.ReadAllText(filePath);
            return string.Empty;
        }
    }
}