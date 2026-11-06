// SPDX-License-Identifier: GPL-3.0-or-later
namespace Android.Runtime
{
    [System.AttributeUsage(System.AttributeTargets.Method)]
    internal sealed class PreserveAttribute : System.Attribute { }
}

namespace Android.Util
{
    internal static class Log
    {
        public static int Warn(string tag, string message)
        {
            System.Threading.Interlocked.Increment(ref Warnings);
            return 0;
        }

        public static int Warnings;
    }
}
