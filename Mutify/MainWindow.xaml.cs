using Mutify.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace Mutify
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region DllImports
        // returns thread id
        [DllImport("user32")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int processId);
        #endregion

        #region fields
        private Process _spotify = null;
        private WinEventHook _windowNameHook = null;
        private WinEventHook _objectCreateHook = null;
        private HashSet<string> _blacklist = null;
        private NotifyIcon _trayIcon = null;
        private string _currentTitle
        {
            get
            {
                if (_spotify == null)
                    return null;

                _spotify.Refresh();
                int dashIndex = _spotify.MainWindowTitle.IndexOf("-");
                if (dashIndex < 0)
                    return null; // Spotify isn't playing anything

                // "Artist – Title"
                // the – is not on the keyboard; blacklist load/save routines compensate
                return _spotify.MainWindowTitle.Substring(dashIndex + 2);
            }
        }
        #endregion

        private NotifyIcon InitializeTrayIcon()
        {
            var trayIconMenu = new MenuItem[] 
                {
                    new MenuItem("Mute Ad", MuteAdButton_Click),
                    new MenuItem("Edit Blacklist", EditBlacklistButton_Click),
                    new MenuItem("Show Window", new EventHandler( (_, __) => { this.WindowState = WindowState.Normal; } )),
                    new MenuItem("Exit", new EventHandler( (_, __) => { this.Close(); } ))
                };

            var notifyIcon = new NotifyIcon()
                {
                    Text = "Mutify",
                    ContextMenu = new ContextMenu(trayIconMenu),
                    Visible = false,
                    Icon = new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream("Mutify.Icon.ico"))
                };

            notifyIcon.MouseUp += (_, __) =>
            {
                notifyIcon.ContextMenu.MenuItems[0].Enabled = MuteAdButton.IsEnabled;

                // this hack courtesy of Hans Passant (http://stackoverflow.com/a/2208910)
                var mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                mi.Invoke(notifyIcon, null);
            };

            return notifyIcon;
        }
        
        public MainWindow()
        {
            SingleInstance.Enforce();

            InitializeComponent();
            _trayIcon = InitializeTrayIcon();
            _blacklist = LoadBlacklist();

            HookSpotify();
        }

        private void HookSpotify()
        {
            try
            {
                _spotify = Process.GetProcessesByName("spotify")[0];
            }
            catch (IndexOutOfRangeException)
            {
                // spotify isn't running; hook ObjectCreate and wait for it to launch
                _objectCreateHook = new WinEventHook(OnObjectCreate, WinEventHook.EventConstant.EVENT_OBJECT_CREATE);
                return;
            }

            // in case Mutify has been launched while Spotify is playing something
            if (_currentTitle != null)
            {
                VolumeControl.SetApplicationMute(_spotify.Id, _blacklist.Contains(_currentTitle));
                MuteAdButton.IsEnabled = true;
            }
            
            _spotify.EnableRaisingEvents = true; // register a handler
            _spotify.Exited += SpotifyExited;    // for spotify's exit

            // hook changes to Spotify's main window title so titles can be checked against the blacklist
            _windowNameHook = new WinEventHook(OnWindowNameChange, WinEventHook.EventConstant.EVENT_OBJECT_NAMECHANGE, _spotify.Id);
        }

        private void UnhookSpotify()
        {
            if (_windowNameHook != null)
            {
                _windowNameHook.Stop();
                _windowNameHook = null;
            }
        }

        #region ui event handlers
        private void MuteAdButton_Click(object sender, EventArgs e) // was RoutedEventArgs.  Consequences?
        {
            var ad = _currentTitle;
            if (!_blacklist.Contains(ad))
            {
                _blacklist.Add(ad);
                _blacklistChanged = true;
            }

            VolumeControl.SetApplicationMute(_spotify.Id, true);
        }

        private async void EditBlacklistButton_Click(object sender, EventArgs e) // was RoutedEventArgs.  Consequences?
        {
            SaveBlacklist(_blacklist);

            var muteButtonState = MuteAdButton.IsEnabled;
            MuteAdButton.IsEnabled = EditBlacklistButton.IsEnabled = false;

            await Task.Factory.StartNew(() => EditBlacklist(_blacklist));

            MuteAdButton.IsEnabled = muteButtonState;
            EditBlacklistButton.IsEnabled = true;
        }

        // only tray icon when minimized, only taskbar presence otherwise
        private void MutifyWindow_StateChanged(object sender, EventArgs e)
        {
            _trayIcon.Visible = this.WindowState == WindowState.Minimized;
            this.ShowInTaskbar = !_trayIcon.Visible;
        }
        #endregion

        #region other event handlers
        private void OnObjectCreate(IntPtr hWinEventHook, uint eventType, IntPtr hWnd, 
            int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            int pid;
            GetWindowThreadProcessId(hWnd, out pid); // return value is thread id i.e. not useful here

            if (Process.GetProcessById(pid).ProcessName == "spotify")
            {
                _objectCreateHook.Stop();
                _objectCreateHook = null;
                HookSpotify();
            }
        }

        private void OnWindowNameChange(IntPtr hWinEventHook, uint eventType,
              IntPtr hWnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (hWnd != _spotify.MainWindowHandle)
                return;

            if (_currentTitle == null) // nothing playing
                this.MuteAdButton.IsEnabled = false;
            else
            {
                this.MuteAdButton.IsEnabled = true;
                VolumeControl.SetApplicationMute(_spotify.Id, _blacklist.Contains(_currentTitle));
            }
        }

        private void SpotifyExited(object sender, EventArgs e)
        {
            this.Close();
        }

        private void MutifyWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            UnhookSpotify();
            SaveBlacklist(_blacklist);
        }
        #endregion
    }
}