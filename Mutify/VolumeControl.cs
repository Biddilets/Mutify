using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

// thanks to Simon Mourier http://stackoverflow.com/a/14322736
// he may have referenced http://whenimbored.xfx.net/2011/01/core-audio-for-net/

namespace Mutify.Utility
{
    static class VolumeControl
    {
        public static float? GetApplicationVolume(int pid)
        {
            ISimpleAudioVolume volume = GetVolumeObject(pid);
            if (volume == null)
                return null;

            float level;
            volume.GetMasterVolume(out level);
            return level * 100;
        }

        public static bool? GetApplicationMute(int pid)
        {
            ISimpleAudioVolume volume = GetVolumeObject(pid);
            if (volume == null)
                return null;

            bool mute;
            volume.GetMute(out mute);
            return mute;
        }

        public static void SetApplicationVolume(int pid, float level)
        {
            ISimpleAudioVolume volume = GetVolumeObject(pid);
            if (volume == null)
                return;

            Guid guid = Guid.Empty;
            volume.SetMasterVolume(level / 100, ref guid);
        }

        public static void SetApplicationMute(int pid, bool mute)
        {
            ISimpleAudioVolume volume = GetVolumeObject(pid);
            if (volume == null)
                return;

            Guid guid = Guid.Empty;
            volume.SetMute(mute, ref guid);
        }

        public static IEnumerable<string> EnumerateApplications()
        {
            // get the speakers (1st render + multimedia) device
            IMMDeviceEnumerator deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
            IMMDevice speakers;
            deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);

            // activate the session manager. we need the enumerator
            Guid IID_IAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;
            object o;
            speakers.Activate(ref IID_IAudioSessionManager2, 0, IntPtr.Zero, out o);
            IAudioSessionManager2 mgr = (IAudioSessionManager2)o;

            // enumerate sessions for on this device
            IAudioSessionEnumerator sessionEnumerator;
            mgr.GetSessionEnumerator(out sessionEnumerator);
            int count;
            sessionEnumerator.GetCount(out count);

            for (int i = 0; i < count; i++)
            {
                IAudioSessionControl2 ctl;
                sessionEnumerator.GetSession(i, out ctl);
                string dn;
                ctl.GetDisplayName(out dn);
                uint pid;
                ctl.GetProcessId(out pid);
                yield return dn;
                Marshal.ReleaseComObject(ctl);
            }
            Marshal.ReleaseComObject(sessionEnumerator);
            Marshal.ReleaseComObject(mgr);
            Marshal.ReleaseComObject(speakers);
            Marshal.ReleaseComObject(deviceEnumerator);
        }

        private static ISimpleAudioVolume GetVolumeObject(int processId)
        {
            // get the speakers (1st render + multimedia) device
            IMMDeviceEnumerator deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
            IMMDevice speakers;
            deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);

            // activate the session manager. we need the enumerator
            Guid IID_IAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;
            object o;
            speakers.Activate(ref IID_IAudioSessionManager2, 0, IntPtr.Zero, out o);
            IAudioSessionManager2 mgr = (IAudioSessionManager2)o;

            // enumerate sessions for on this device
            IAudioSessionEnumerator sessionEnumerator;
            mgr.GetSessionEnumerator(out sessionEnumerator);
            int count;
            sessionEnumerator.GetCount(out count);

            // search for an audio session with the right pid
            ISimpleAudioVolume volumeControl = null;
            for (int i = 0; i < count; i++)
            {
                IAudioSessionControl2 ctl;
                sessionEnumerator.GetSession(i, out ctl);
                uint pid = 0;
                ctl.GetProcessId(out pid);
                if (pid == processId)
                {
                    volumeControl = ctl as ISimpleAudioVolume;
                    break;
                }
                Marshal.ReleaseComObject(ctl);
            }
            Marshal.ReleaseComObject(sessionEnumerator);
            Marshal.ReleaseComObject(mgr);
            Marshal.ReleaseComObject(speakers);
            Marshal.ReleaseComObject(deviceEnumerator);
            return volumeControl;
        }

        [ComImport]
        [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        internal class MMDeviceEnumerator
        {
        }

        internal enum EDataFlow
        {
            eRender,
            eCapture,
            eAll,
            EDataFlow_enum_count
        }

        internal enum ERole
        {
            eConsole,
            eMultimedia,
            eCommunications,
            ERole_enum_count
        }

        #region interfaces
        [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IMMDeviceEnumerator
        {
            int NotImpl1();

            [PreserveSig]
            int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppDevice);

            // the rest is not implemented
        }

        [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IMMDevice
        {
            [PreserveSig]
            int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);

            // the rest is not implemented
        }

        [Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IAudioSessionManager2
        {
            int NotImpl1();
            int NotImpl2();

            [PreserveSig]
            int GetSessionEnumerator(out IAudioSessionEnumerator SessionEnum);

            // the rest is not implemented
        }

        [Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IAudioSessionEnumerator
        {
            [PreserveSig]
            int GetCount(out int SessionCount);

            [PreserveSig]
            int GetSession(int SessionCount, out IAudioSessionControl2 Session);
        }

        [Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface ISimpleAudioVolume
        {
            [PreserveSig]
            int SetMasterVolume(float fLevel, ref Guid EventContext);

            [PreserveSig]
            int GetMasterVolume(out float pfLevel);

            [PreserveSig]
            int SetMute(bool bMute, ref Guid EventContext);

            [PreserveSig]
            int GetMute(out bool pbMute);
        }

        [Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IAudioSessionControl2
        {
            [PreserveSig]
            int Null1(); //GetState(out AudioSessionState state);
            [PreserveSig]
            int GetDisplayName([Out(), MarshalAs(UnmanagedType.LPWStr)] out string name);
            [PreserveSig]
            int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string value, Guid EventContext);
            [PreserveSig]
            int GetIconPath([Out(), MarshalAs(UnmanagedType.LPWStr)] out string Path);
            [PreserveSig]
            int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string Value, Guid EventContext);
            [PreserveSig]
            int GetGroupingParam(out Guid GroupingParam);
            [PreserveSig]
            int SetGroupingParam(Guid Override, Guid Eventcontext);
            [PreserveSig]
            int Null2(); //RegisterAudioSessionNotification(IAudioSessionEvents NewNotifications);
            [PreserveSig]
            int Null3(); //UnregisterAudioSessionNotification(IAudioSessionEvents NewNotifications);
            [PreserveSig]
            int GetSessionIdentifier([Out(), MarshalAs(UnmanagedType.LPWStr)] out string retVal);
            [PreserveSig]
            int GetSessionInstanceIdentifier([Out(), MarshalAs(UnmanagedType.LPWStr)] out string retVal);
            [PreserveSig]
            int GetProcessId(out UInt32 retvVal);
            [PreserveSig]
            int IsSystemSoundsSession();
            [PreserveSig]
            int SetDuckingPreference(bool optOut);
        }
        #endregion
    }
}