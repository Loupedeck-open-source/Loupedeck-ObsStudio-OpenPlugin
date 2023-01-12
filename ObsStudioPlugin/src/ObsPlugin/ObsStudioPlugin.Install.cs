namespace Loupedeck.ObsStudioPlugin
{
    using System;
    using System.IO;

    public partial class ObsStudioPlugin : Plugin
    {
        // Note, that on need-to-be basis we can support 32 bit version of the Windows OBS connector, loupedeck-obs.dll.win32
        private const String WinX64WSPluginLib = "loupedeck-obs.dll.x64";
        private const String MacWSPluginLib = "loupedeck-obs.so";
        private const String MacPluginDestinationFolder =
                                "/Library/Application Support/obs-studio/plugins/loupedeck-obs/bin";

        private static String GetWSPluginSourcePath()
                                => $"Loupedeck.ObsStudioPlugin.ObsWebsocketServer.{(Helpers.IsWindows() ? WinX64WSPluginLib : MacWSPluginLib)}";

        // Returns full path + filename for to-be-installed plugin.
        private static String GetWSPluginDestinationPath()
        {
            if (Helpers.IsMacintosh())
            {
                return Path.Combine(MacPluginDestinationFolder, MacWSPluginLib);
            }

            // we're in 'windows' branch
            var pluginDestination = ObsStudioApplication.GetWindowsInstallationRoot();

            // Get OBS installation folder
            if (!pluginDestination.IsNullOrEmpty())
            {
                // NOTE: Assuming the base path for plugins ( obs-plugins\\64bit )
                // is created during OBS installation
                // NOTE1: If OBS is running and plugin is installed we won't be able to overwrite it.
                // Shall we fail?
                pluginDestination = Path.Combine(pluginDestination, "obs-plugins", "64bit", "loupedeck-obs.dll");
            }

            return pluginDestination;
        }

        // Note that plugin data directory MUST be created in Install, not InstallAdmin,
        //      otherwise we can have wrong permissions there
        public override Boolean Install()
        {
            if (!IoHelpers.EnsureDirectoryExists(this.GetPluginDataDirectory()))
            {
                Tracer.Error("Cannot create data directory ");

                // Note, if this is not possible it is something really not good with LD installation
                return false;
            }

            return true;
        }

        // NOTE! Install/UninstallAdmin is NOT executed during Marketplace plugin installation.
        public override Boolean InstallAdmin()
        {
            var pluginDestination = GetWSPluginDestinationPath();

            if (pluginDestination.IsNullOrEmpty())
            {
                Tracer.Warning("OBS: installation not detected!");
                return true;
            }

            // Following section will be uncommented once loupedeck's own plugin for Mac is fixed. it will replace the section above
            if (Helpers.IsMacintosh() && (!IoHelpers.EnsureDirectoryExists(MacPluginDestinationFolder)))
            {
                // Note: On MAC the destination folder might not exist!.
                // Note1:  This folder needs to be created by admin (?)

                Tracer.Error($"OBS: Cannot create Loupedeck OBS plugin folder \"{MacPluginDestinationFolder}\"");
                return false;
            }

            try
            {
                this.Assembly.ExtractFile(GetWSPluginSourcePath(), pluginDestination);
                Tracer.Trace($"OBS: Loupedeck OBS plugin installed to : \"{pluginDestination}\"");
            }
            catch (Exception ex)
            {
                if (!File.Exists(pluginDestination))
                {
                    Tracer.Error("OBS: Cannot install Loupedeck OBS plugin!:", ex);

                    // Bad problem: Cannot install plugin and this is not a re-install/OBS running case (no locked plugin available)
                    return false;
                }

                Tracer.Warning("OBS: Cannot install Loupedeck OBS plugin. File exists!:", ex);

                // NOT RETURNING FALSE:  It's likely a situation when OBS is running. We will need to address this as a support question and let people attempt to install
                // OBS side plugin manually

                // return false;
            }

            return true;
        }

        public override Boolean UninstallAdmin()
        {
            // 1. Remove Data directory (nb: With all files!)
            if (!IoHelpers.DeleteDirectory(this.GetPluginDataDirectory()))
            {
                Tracer.Error("OBS: Cannot delete plugin data directory");

                // NOTE: Not returning false as this is not a fatal error
                // return false;
                return true;
            }

            // 2. Remove Loupedeck plugin
            // We will just delete files, if we can.
            // Note that if OBS is running that'll fail
            var pluginLocation = GetWSPluginDestinationPath();
            if (File.Exists(pluginLocation))
            {
                if (!IoHelpers.DeleteFile(pluginLocation))
                {
                    Tracer.Error("OBS: Error deleting Loupedeck OBS connector: ");

                    // NOTE: Not returning false as this is not a fatal error
                    // return false;
                    return true;
                }

                Tracer.Trace("OBS: Loupedeck OBS connector deleted");
            }

            return true;
        }
    }
}