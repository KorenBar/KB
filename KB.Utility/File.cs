using System;
using System.Collections.Generic;

namespace KB.Utility
{
    public static class File
    {
        public static bool AddLines(string fileName, string[] lines)
        {
            bool res = false;
            string fn = System.IO.Path.GetFullPath(fileName);
            List<string> ll = new List<string>();
            if (res = System.IO.File.Exists(fn))
                ll.AddRange(System.IO.File.ReadAllLines(fn));
            System.IO.File.Create(fn).Close();
            foreach (string l in lines) ll.Add(l);
            System.IO.File.WriteAllLines(fn, ll);
            return res; // Returns true if file already exists.
        }

        public static bool AddText(string fileName, string text)
        {
            bool res = false;
            string fn = System.IO.Path.GetFullPath(fileName);
            string content = "";
            if (res = System.IO.File.Exists(fn))
                content = System.IO.File.ReadAllText(fn);
            System.IO.File.Create(fn).Close();
            System.IO.File.WriteAllText(fn, content + text + Environment.NewLine);
            return res; // Returns true if file already exists.
        }
    }
}
