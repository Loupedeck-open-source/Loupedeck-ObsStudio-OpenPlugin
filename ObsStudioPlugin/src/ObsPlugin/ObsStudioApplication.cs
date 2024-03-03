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
            var status = ClientApplicationStatus.NotInstalled;

            status = (Helpers.IsWindows() && File.Exists(this.GetExecutablePath()))
                 || (Helpers.IsMacintosh() && GetOBSAppDirsForMac()?.Count() > 0)
                 ? ClientApplicationStatus.Installed 
                 : ClientApplicationStatus.NotInstalled;
            
            //Tracer.Error($"GetApplicationStatus Installed: {status == ClientApplicationStatus.Installed}");
            return status;
        }

        public static List<String> GetOBSAppDirsForMac()
        {
            var applicationFolder = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

            String[] allApps = null;

            try
            {
                allApps = Directory.GetDirectories(applicationFolder);
            }
            catch (Exception)
            {
                Tracer.Error($"GetOBSAppDirsForMac:Cannot get drs for the appfolder {applicationFolder}");
                return null;
            }

            var dirs = allApps?.Where(x => x.Contains("OBS") && !x.ContainsNoCase("Streamlabs "));

            var dirlist = dirs?.ToList<String>();
            
            var msg = "GetOBSAppDirsForMac:: OBS app dirs for Mac:";
            foreach (var s in dirlist)
            { 
                msg += s + ";";
            }

            Tracer.Trace(msg);

            return dirlist;
        }
    }
}