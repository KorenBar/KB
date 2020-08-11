using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace KB.Utility
{
    public static class ConsoleHelper
    {
        public static string[] Args { get; private set; } = System.Environment.GetCommandLineArgs();
        public static Dictionary<string, string> ArgsValues { get; private set; } = Args.Select((v, i) => new { Value = v, Index = i }).Where(arg => arg.Value.TrimStart().StartsWith("-")).ToDictionary(arg => string.Concat(arg.Value.TrimStart().Skip(1)), arg => Args.Length > arg.Index + 1 ? Args[arg.Index + 1] : null);

        public static System.ConsoleKeyInfo? WaitForKey(int ms)
        {
            System.ConsoleKeyInfo? result = null;
            int delay = 100;
            for (int i = delay; i <= ms; i += delay)
            {
                System.Console.Write("\rWaiting for " + (ms - i) / 1000 + " seconds, press a key to continue ...");
                if (System.Console.KeyAvailable)
                {
                    result = System.Console.ReadKey();
                    break;
                }
                Task.Delay(delay).GetAwaiter().GetResult();
            }
            System.Console.WriteLine();
            return result;
        }
    }
}
