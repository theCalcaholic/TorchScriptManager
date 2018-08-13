using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Utils;

namespace ScriptManager.ClientMod
{
    public static class ModLogger
    {
        private static readonly string Prefix = "[ScriptManagerClientMod] ";
        public static void Info(string message)
        {
            MyLog.Default.Info(Prefix + message);
        }
        public static void Info(string message, params object[] args)
        {
            MyLog.Default.Info(Prefix + message, args);
        }

        public static void Warning(string message)
        {
            MyLog.Default.Warning(Prefix + message);
        }
        public static void Warning(string message, params object[] args)
        {
            MyLog.Default.Error(Prefix + message, args);
        }

        public static void Error(string message)
        {
            MyLog.Default.Error(Prefix + message);
        }

        public static void Error(string message, params object[] args)
        {
            MyLog.Default.Error(Prefix + message, args);
        }
    }
}
