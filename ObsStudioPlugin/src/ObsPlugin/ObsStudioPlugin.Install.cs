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

            this.Log.Info($"Install: OBS Installed: {this.ClientApplication.GetApplicationStatus() == ClientApplicationStatus.Installed}. WebServer Json file exists/good:{this._webSocketServerJsonFile.jsonFileExists}/{this._webSocketServerJsonFile.jsonFileGood}");

            if (this.ClientApplication.GetApplicationStatus() == ClientApplicationStatus.Installed
                        && this._webSocketServerJsonFile.jsonFileExists
                        && !this._webSocketServerJsonFile.jsonFileGood
                        )
            {
                this.Log.Info("Install: OBS Installed but WebServer Json file is bad, fixing");

                //Attempting to fix WebServer Json file if it is not good. Can only be done when app is not running (for Portable app we need to know its location first
                this._webSocketServerJsonFile.FixJsonFile();
            }

            this.Update_PluginStatus();



            return true;
        }

    }
}