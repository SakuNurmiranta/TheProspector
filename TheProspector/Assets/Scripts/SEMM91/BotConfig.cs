using UnityEngine;

namespace SEMM91
{
    public static class BotConfig
    {
        public static bool HasArg(string arg)
        {
            var args = System.Environment.GetCommandLineArgs();
            foreach (var a in args)
                if (a == arg)
                    return true;
            return false;
        }


        public static int GetIntArg(string arg, int defaultValue)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
                if (args[i] == arg && int.TryParse(args[i + 1], out var v))
                    return v;
            return defaultValue;
        }

        public static string GetStringArg(string arg, string defaultValue)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
                if (args[i] == arg)
                    return args[i + 1];
            return defaultValue;
        }
    }


}
