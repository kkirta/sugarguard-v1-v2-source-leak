using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace SugarGuard.Protector.Protections.Runtime
{
    internal static class AntiDebug
    {
        #region Imports
        [DllImport("kernel32.dll", EntryPoint = "CloseHandle")]
        internal static extern int CloseHandle(IntPtr hModule);

        [DllImport("kernel32.dll", EntryPoint = "OpenProcess")]
        internal static extern IntPtr OpenProcess(uint hModule, int procName, uint procId);

        [DllImport("kernel32.dll", EntryPoint = "GetCurrentProcessId")]
        internal static extern uint GetCurrentProcessId();

        [DllImport("kernel32.dll", EntryPoint = "LoadLibrary", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr LoadLibrary(string hModule);

        [DllImport("kernel32.dll", EntryPoint = "GetProcAddress", CharSet = CharSet.Ansi)]
        internal static extern GetProcA GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", EntryPoint = "GetProcAddress", CharSet = CharSet.Ansi)]
        internal static extern GetProcA2 GetProcAddress_2(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", EntryPoint = "GetProcAddress", CharSet = CharSet.Ansi)]
        internal static extern GetProcA3 GetProcAddress_3(IntPtr hModule, string procName);

        [DllImport("user32.dll", EntryPoint = "GetClassName", CharSet = CharSet.Auto)]
        internal static extern int GetClassName(IntPtr hModule, StringBuilder procName, int procId);
        #endregion

        internal delegate int GetProcA();
        internal delegate int GetProcA2(IntPtr hProcess, ref int pbDebuggerPresent);
        internal delegate int WL(IntPtr wnd, IntPtr lParam);
        internal delegate int GetProcA3(WL lpEnumFunc, IntPtr lParam);

        internal static string SplitMenuItem(IntPtr hModule)
        {
            StringBuilder procName = new StringBuilder(0x104);
            GetClassName(hModule, procName, procName.Capacity);

            return procName.ToString();
        }

        static void Initialize()
        {
            if (Detected())
                throw new Exception(string.Format("Debugger was found! - This software cannot be executed under the debugger."));
        }

        internal static bool Detected()
        {
            try
            {
                IntPtr hModule = LoadLibrary("kernel32.dll");

                if (Debugger.IsAttached)
                    return true;

                GetProcA DebuggerP = GetProcAddress(hModule, "IsDebuggerPresent");

                if (DebuggerP != null && DebuggerP() != 0)
                    return true;

                IntPtr num1 = OpenProcess(0x400, 0, GetCurrentProcessId());

                if (num1 != IntPtr.Zero)
                {
                    try
                    {
                        GetProcA2 RDebuggerP = GetProcAddress_2(hModule, "CheckRemoteDebuggerPresent");
                        if (RDebuggerP != null)
                        {
                            int pbDebuggerPresent = 0;

                            if (RDebuggerP(num1, ref pbDebuggerPresent) != 0)
                                if (pbDebuggerPresent != 0)
                                    return true;
                        }
                    }
                    finally
                    {
                        CloseHandle(num1);
                    }
                }

                bool Detected = false;

                try
                {
                    CloseHandle(new IntPtr(0x12345678));
                }
                catch
                {
                    Detected = true;
                }

                if (Detected)
                    return true;
            }
            catch { }

            return false;
        }
    }
}