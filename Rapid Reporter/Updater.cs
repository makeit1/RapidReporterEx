using System;
using System.Reflection;
using System.Windows;
using Rapid_Reporter.Forms;
using Rapid_Reporter.HTML;

namespace Rapid_Reporter
{
    internal static class Updater
    {
        internal static void ManualCheckVersion()
        {
            var parsedVersion = GetServerVersion();
            if (parsedVersion == null)
            {
                SetAllowedToCheckValue(new Version(0, 0, 0, 0));
                MessageBox.Show("Unable to retrieve latest version number from GitHub.");
                return;
            }
            if (UpToDateWithLatest(parsedVersion))
            {
                SetAllowedToCheckValue(new Version(0, 0, 0, 0));
                MessageBox.Show("You are currently up to date!\r\nWe will let you know if a new version comes down the pipeline.");
                return;
            }
            ShowUpdateDlg(parsedVersion);
        }

        private static void ShowUpdateDlg(Version parsedVersion)
        {
            var updateDlg = new UpdateForm();
            updateDlg.UpdateServerVersion(parsedVersion);
            updateDlg.ShowDialog();
            switch (updateDlg.Choice)
            {
                case UpdateChosen.Update:
                    SetAllowedToCheckValue(new Version(0, 0, 0, 0));
                    System.Diagnostics.Process.Start("https://github.com/makeit1/RapidReporterEx/releases");
                    return;
                case UpdateChosen.Skip:
                    SetAllowedToCheckValue(parsedVersion);
                    return;
                case UpdateChosen.Later:
                    SetAllowedToCheckValue(new Version(0, 0, 0, 0));
                    return;
            }
        }

        internal static void CheckVersion()
        {
            var parsedVersion = GetServerVersion();
            if (parsedVersion == null) return;
            if (UpToDateWithLatest(parsedVersion)) return;
            if (!AllowedToCheckForUpdates(parsedVersion)) return;
            ShowUpdateDlg(parsedVersion);
        }

        private static bool AllowedToCheckForUpdates(Version serverVersion)
        {
            var str = RegUtil.ReadRegKey("UpdateToSkip");
            if (string.IsNullOrWhiteSpace(str))
                return true;
            Version regver;
            try
            {
                regver = new Version(str);
            }
            catch
            {
                return true;
            }
            return regver != serverVersion;
        }

        private static void SetAllowedToCheckValue(Version updateToSkip)
        {
            RegUtil.CreateRegKey("UpdateToSkip", updateToSkip.ToString());
        }

        private static bool UpToDateWithLatest(Version latest)
        {
            var appVersion = Assembly.GetExecutingAssembly().GetName().Version;
            return appVersion >= latest;
        }

        private static Version GetServerVersion()
        {
            var verCall = HttpCallUtil.HttpGetCall(@"https://raw.githubusercontent.com/makeit1/RapidReporterEx/master/currentVersion.txt"); // master
            if (string.IsNullOrWhiteSpace(verCall.Message)) return null;
            var ver = verCall.Message.Split(Convert.ToChar("."));
            if (ver.Length != 4) return null;
            try
            {
                var parsedVersion = new Version(verCall.Message);
                return parsedVersion;
            }
            catch
            {
                return null;
            }
        }
    }
}
