namespace Loupedeck.ObsStudioPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    public class ObsStudioApplication : ClientApplication
    {
        protected override String GetProcessName() => "obs64";

        protected override String GetBundleName() => "com.obsproject.obs-studio";

        public ObsStudioApplication()
        {
        }

        protected override Boolean IsProcessNameSupported(String procName)
        {
            if (!procName.EqualsNoCase(Helpers.IsWindows() ? this.GetProcessName() : this.GetBundleName()))
            {
                return false;
            }

            // exclude false call when Streamlabs OBS starts (it contains obs64 process too)
            return Process.GetProcessesByName("Streamlabs OBS").Length == 0;
        }

        /// <summary>
        /// Get installation root path for OBS Studio on Windows from Registry
        /// </summary>
        /// <returns>Path or empty string if OBS Studio not installed</returns>
        public static String GetWindowsInstallationRoot() =>
                                    Registry64.ReadValue(Registry64Hive.LocalMachine, @"SOFTWARE\OBS Studio", null) as String;

        protected override String GetExecutablePath()
        {
            if (Helpers.IsWindows())
            {
                var path = GetWindowsInstallationRoot();

                return path.IsNullOrEmpty() ? null : Path.Combine(path, "bin", "64bit", "obs64.exe");
            }
            else // Mac
            {
                return base.GetExecutablePath();
            }
        }

        // For OBS Studio we can tell for sure, is it installed or not
        public override ClientApplicationStatus GetApplicationStatus()
        {
            if (Helpers.IsWindows())
            {
                return File.Exists(this.GetExecutablePath())
                                        ? ClientApplicationStatus.Installed
                                        : ClientApplicationStatus.NotInstalled;
            }
            else
            {
                var allOBSDirs = GetOBSAppDirsForMac();

                return allOBSDirs != null && allOBSDirs.Count > 0
                                        ? ClientApplicationStatus.Installed
                                        : ClientApplicationStatus.NotInstalled;
            }
        }

        public static List<String> GetOBSAppDirsForMac()
        {
            Tracer.Trace($"Getting OBS app dirs for Mac.");

            var applicationFolder = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

            String[] allApps = null;

            try
            {
                allApps = Directory.GetDirectories(applicationFolder);
            }
            catch (Exception)
            {
                return null;
            }

            var dirs = allApps?.Where(x => x.Contains("OBS") && !x.ContainsNoCase("Streamlabs "));

            return dirs?.ToList<String>();
        }
    }
}