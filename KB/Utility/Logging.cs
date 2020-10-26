using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

namespace KB.Utility
{
    public static class Logging
    {
        private static string assemblyDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        public static string TraceFile { get; set; } = Path.Combine(assemblyDirectory, "Trace.log");
        public static string ErrorFile { get; set; } = Path.Combine(assemblyDirectory, "Error.log");

        private static void AppendLogLine(string file, string text) => File.AppendAllText(file, DateTime.Now.ToString() + " => " + text + Environment.NewLine);
        public static void WriteError(string error) => AppendLogLine(ErrorFile, error);
        public static void WriteError(Exception exception) => WriteError(exception.JoinMessages());
        public static void WriteTrace(string text) => AppendLogLine(TraceFile, text);
        public static void WriteTrace(MethodBase method, string text) => WriteTrace(method.Name + " : " + text);
    }
}
