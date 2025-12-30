using System;

namespace SEMM91
{
    public static class BotConfig
    {
        public static bool HasArg(string arg)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == arg) return true;

                // support -arg=1, -arg=true as "present"
                if (args[i].StartsWith(arg + "=", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public static int GetIntArg(string arg, int defaultValue)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                // -arg 123
                if (args[i] == arg && i + 1 < args.Length && int.TryParse(args[i + 1], out var v))
                    return v;

                // -arg=123
                if (args[i].StartsWith(arg + "=", StringComparison.OrdinalIgnoreCase))
                {
                    var s = args[i].Substring(arg.Length + 1);
                    if (int.TryParse(s, out var v2)) return v2;
                }
            }
            return defaultValue;
        }

        public static string GetStringArg(string arg, string defaultValue)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                // -arg value
                if (args[i] == arg && i + 1 < args.Length)
                    return args[i + 1];

                // -arg=value
                if (args[i].StartsWith(arg + "=", StringComparison.OrdinalIgnoreCase))
                    return args[i].Substring(arg.Length + 1);
            }
            return defaultValue;
        }
    }
}