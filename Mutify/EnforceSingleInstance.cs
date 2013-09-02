using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace Mutify.Utility
{
    static class SingleInstance
    {
        #region DllImports
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
        #endregion

        private static Mutex _mutex = new Mutex(true, Assembly.GetExecutingAssembly().FullName);

        // this doesn't work when app is showing only a tray icon
        public static void Enforce()
        {
            if (!_mutex.WaitOne(0, true)) // app is already running;
            {                             // bring its window to front
                var thisProcess = Process.GetCurrentProcess();
                foreach (var otherProcess in Process.GetProcessesByName(thisProcess.ProcessName))
                    if (otherProcess.Id != thisProcess.Id)
                        SetForegroundWindow(otherProcess.MainWindowHandle);
                Environment.Exit(0);
            }
        }

    }
}
