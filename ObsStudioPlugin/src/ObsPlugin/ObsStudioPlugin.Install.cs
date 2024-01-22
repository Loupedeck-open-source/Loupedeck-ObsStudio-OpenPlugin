namespace Loupedeck.ObsStudioPlugin
{
    using System;
    using System.IO;

    public partial class ObsStudioPlugin : Plugin
    {
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
#warning "Here we detect if app is installed and modify ini file if needed"
            return true;
        }

    }
}