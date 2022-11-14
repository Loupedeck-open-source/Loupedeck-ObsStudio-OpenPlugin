namespace Loupedeck.ObsPlugin
{
    using System;
    using System.IO;

    // This class can be used to connect the Loupedeck plugin to an application.

    public class ObsApplication : ClientApplication
    {
        public ObsApplication()
        {
        }

        // This method can be used to link the plugin to a Windows application.
        protected override String GetProcessName() => "obs64";

        // This method can be used to link the plugin to a macOS application.
        protected override String GetBundleName() => "";

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
            return Helpers.IsWindows()
                ? File.Exists(this.GetExecutablePath())
                                        ? ClientApplicationStatus.Installed
                                        : ClientApplicationStatus.NotInstalled
                : ClientApplicationStatus.NotInstalled;
        }
    }
}
