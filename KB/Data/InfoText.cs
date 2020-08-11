using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KB.Data
{
    /// <summary>
    /// Create a New Info Text to store or load data
    /// </summary>
    public class InfoText
    {
        public static implicit operator InfoText(string value) => new InfoText(value);

        public static implicit operator string(InfoText value) => value.ToString();

        List<string> lines;

        /// <summary>
        /// InfoText Constructor
        /// </summary>
        public InfoText()
        {
            this.lines = new List<string>();
        }

        /// <summary>
        /// InfoText Constructor
        /// </summary>
        /// <param name="text"></param>
        public InfoText(string text)
        {
            this.lines = new List<string>(text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None));
        }

        /// <summary>
        /// InfoText Constructor
        /// </summary>
        /// <param name="lines"></param>
        public InfoText(string[] lines)
        {
            this.lines = new List<string>(lines);
        }

        /// <summary>
        /// Write Data to the Info Text
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void WriteValue(string key, string value)
        {
            for (int l = 0; l < lines.Count; l++)
            {
                string[] lineSplit = lines[l].Split(new char[] { '=' }, 2);
                if (lineSplit[0].Trim() == key && lineSplit.Length > 1)
                {
                    lines[l] = lineSplit[0] + "=" + value;
                    return;
                }
            }
            lines.Add(key + "=" + value);
        }

        /// <summary>
        /// Write Data from Dictionary to the Info Text
        /// </summary>
        /// <param name="dictionary"></param>
        public void WriteValues(Dictionary<string, string> dictionary)
        {
            List<string> newLines = new List<string>();
            foreach (KeyValuePair<string, string> keyValue in dictionary)
            {
                bool newLine = true;
                for (int l = 0; l < lines.Count; l++)
                {
                    string[] lineSplit = lines[l].Split(new char[] { '=' }, 2);
                    if (lineSplit[0].Trim() == keyValue.Key && lineSplit.Length > 1)
                    {
                        lines[l] = lineSplit[0] + "=" + keyValue.Value;
                        newLine = false;
                        break;
                    }
                }
                if (newLine)
                    lines.Add(keyValue.Key + "=" + keyValue.Value);
            }
        }

        /// <summary>
        /// Read Data Value From the Info Text
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string ReadValue(string key)
        {
            string line = lines.FirstOrDefault(l => l.TrimStart().StartsWith(key) && l.Split(new char[] { '=' }, 2).Length > 1);
            if (line != null)
                return line.Split(new char[] { '=' }, 2)[1].Trim();
            return null;
        }

        /// <summary>
        /// Read All Data From the Info Text to Dictionary
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> ToDictionary()
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            foreach (string line in lines.Where(l => l.Split(new char[] { '=' }, 2).Length > 1))
            {
                string[] ls = line.Split(new char[] { '=' }, 2);
                if (!dictionary.Keys.Contains(ls[0].Trim()))
                    dictionary.Add(ls[0].Trim(), ls[1].Trim());
            }
            return dictionary;
        }

        /// <summary>
        /// Read All Text From the Info Text
        /// </summary>
        /// <returns></returns>
        public override string ToString() => string.Join(Environment.NewLine, this.lines);
    }
}