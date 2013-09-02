using System;
using System.Runtime.InteropServices;

// thanks to Wade Hatler
// (http://stackoverflow.com/a/11943387)

namespace Mutify.Utility
{
    public class WinEventHook
    {
        #region DllImports and enums
        // this is not a complete enumeration
        public enum EventConstant : uint
        {
            WINEVENT_OUTOFCONTEXT = 0,
            EVENT_SYSTEM_FOREGROUND = 3,
            EVENT_OBJECT_NAMECHANGE = 0x800C,
            EVENT_OBJECT_CREATE = 0x8000
        }

        [DllImport("user32.dll")]
        public static extern IntPtr SetWinEventHook(
                EventConstant eventMin, EventConstant eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc,
                uint idProcess, uint idThread, EventConstant dwFlags);

        [DllImport("user32.dll")]
        public static extern bool UnhookWinEvent(IntPtr hWinEventHook);
        #endregion

        public delegate void WinEventDelegate(
                IntPtr hWinEventHook, uint eventType,
                IntPtr hWnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        readonly WinEventDelegate _procDelegate;
        readonly IntPtr _hWinEventHook;

        public WinEventHook(WinEventDelegate handler, EventConstant eventMin, EventConstant eventMax, int processId = 0, int threadId = 0)
        {
            _procDelegate = handler;
            _hWinEventHook = SetWinEventHook(eventMin, eventMax, IntPtr.Zero, handler, (uint)processId, (uint)threadId, EventConstant.WINEVENT_OUTOFCONTEXT);
        }

        public WinEventHook(WinEventDelegate handler, EventConstant eventMin, int processId = 0, int threadId = 0)
            : this(handler, eventMin, eventMin, processId, threadId)
        {
        }

        public void Stop()
        {
            UnhookWinEvent(_hWinEventHook);
        }

        // Usage Example for EVENT_OBJECT_CREATE (http://msdn.microsoft.com/en-us/library/windows/desktop/dd318066%28v=vs.85%29.aspx)
        // var _objectCreateHook = new EventHook(OnObjectCreate, EventHook.EVENT_OBJECT_CREATE);
        // ...
        // static void OnObjectCreate(IntPtr hWinEventHook, uint eventType, IntPtr hWnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime) {
        //    if (!Win32.GetClassName(hWnd).StartsWith("ClassICareAbout"))
        //        return;
        // Note - in Console program, doesn't fire if you have a Console.ReadLine active, so use a Form
    }
}