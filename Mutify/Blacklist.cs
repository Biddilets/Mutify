using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace Mutify
{
    public partial class MainWindow : Window
    {
        private const string _blacklistFileName = "blacklist.txt";
        private bool _blacklistChanged = false;
        private readonly string _blacklistFilePath = 
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\" + _blacklistFileName;

        // load and save functions ensure the blacklist in memory uses the same
        // (non-keyboard) hyphen character as spotify's track titles while the
        // blacklist text file on disk contains only standard, easily keyed dashes
        private HashSet<string> LoadBlacklist()
        {
            try
            {
                return new HashSet<string>
                    (from track in File.ReadAllLines(_blacklistFilePath) select track.Replace("-", "–"));
            }
            catch (Exception)
            {
                return new HashSet<string>();
            }
        }

        private void SaveBlacklist(HashSet<string> blacklist)
        {
            if (!_blacklistChanged)
                return;

            // could implement a solution that tracks additions to the list and
            // writes only them to disk, but presently I doubt the blacklist 
            // will ever be long enough to warrant it
            File.WriteAllLines(_blacklistFilePath, from track in blacklist select track.Replace("–", "-"));

            return;
        }

        private void EditBlacklist(HashSet<string> blacklist)
        {
            SaveBlacklist(_blacklist);

            Process.Start("notepad.exe", _blacklistFilePath).WaitForExit();

            _blacklist = LoadBlacklist();
            _blacklistChanged = true;
        }
    }
}
