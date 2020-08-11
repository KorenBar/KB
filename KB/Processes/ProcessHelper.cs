using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace KB.Processes
{
    public static class ProcessHelper
    {
        public static string[] Args { get; } = Environment.GetCommandLineArgs();
        public static Dictionary<string, string> ArgsValues { get; } = Args.Select((v, i) => new { Value = v, Index = i }).Where(arg => arg.Value.TrimStart().StartsWith("-")).ToDictionary(arg => string.Concat(arg.Value.TrimStart().Skip(1)), arg => Args.Length > arg.Index + 1 ? Args[arg.Index + 1] : null);

        public static bool IsArgumentExists(string arg) => Args.Contains(arg);
        public static bool IsArgumentExists(params string[] args) => args.Any(a => IsArgumentExists(a));

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private static IntPtr hWnd = Process.GetCurrentProcess().MainWindowHandle;

        public static bool HideMainWindow()
        {
            if (hWnd != IntPtr.Zero) return ShowWindow(hWnd, 0);
            return false;
        }

        public static bool ShowMainWindow()
        {
            if (hWnd != IntPtr.Zero) return ShowWindow(hWnd, 1);
            return false;
        }
    }
}
