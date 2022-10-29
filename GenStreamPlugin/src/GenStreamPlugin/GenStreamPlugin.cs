namespace Loupedeck.GenStreamPlugin
{
    using System;

    // This class contains the plugin-level logic of the Loupedeck plugin.

    public class GenStreamPlugin : Plugin
    {
        public readonly GenStreamProxy Proxy;
        
        // Gets a value indicating whether this is an Universal plugin or an Application plugin.
        public override Boolean UsesApplicationApiOnly => true;

        // Gets a value indicating whether this is an API-only plugin.
        public override Boolean HasNoApplication => true;

        private readonly ObsConnector _connector;

        public GenStreamPlugin()
        {
            this.Proxy = new GenStreamProxy();
            this._connector = new ObsConnector(this.Proxy, this.GetPluginDataDirectory() +"\\..\\ObsStudio",  /*"C:\\Users\\Andrei Laperie\\AppData\\Local\\Loupedeck\\PluginData\\ObsStudio"/**/
                                () => this.OnPluginStatusChanged(Loupedeck.PluginStatus.Warning, this.Localization.GetString("Connecting to OBS"), "https://support.loupedeck.com/obs-guide", ""));
        }

        // Load is called once as plugin is being initialized during service start.
        public override void Load()
        {
            this.ClientApplication.ApplicationStarted += this.OnApplicationStarted;
            this.ClientApplication.ApplicationStopped += this.OnApplicationStopped;

            this.ClientApplication.ApplicationInstanceStarted += this.OnApplicationStarted;
            this.ClientApplication.ApplicationInstanceStopped += this.OnApplicationStopped;

            this.Proxy.EvtAppConnected += this.OnAppConnStatusChange;
            this.Proxy.EvtAppDisconnected += this.OnAppConnStatusChange;

            this._connector.Start();

            this.Update_PluginStatus();
        }

        // Unload is called once when plugin is being unloaded.
        public override void Unload()
        {
            this._connector.Stop();

            this.OnApplicationStopped(this, null);

            this.ClientApplication.ApplicationStarted -= this.OnApplicationStarted;
            this.ClientApplication.ApplicationStopped -= this.OnApplicationStopped;

            this.ClientApplication.ApplicationInstanceStarted -= this.OnApplicationStarted;
            this.ClientApplication.ApplicationInstanceStopped -= this.OnApplicationStopped;

            this.Proxy.EvtAppConnected -= this.OnAppConnStatusChange;
            this.Proxy.EvtAppDisconnected -= this.OnAppConnStatusChange;

            //this.Proxy = null;
        }

        private void OnAppConnStatusChange(Object sender, EventArgs e) => this.Update_PluginStatus();

        private void OnApplicationStarted(Object sender, EventArgs e)
        {
            
        }

        private void OnApplicationStopped(Object sender, EventArgs e)
        {
        
        }

        private void Update_PluginStatus()
        {
            if (!this.IsApplicationInstalled())
            {
                this.OnPluginStatusChanged(Loupedeck.PluginStatus.Error, "App is not installed", "https://support.GenStreamPlugin.com", "more details");
            }
            else if (this.Proxy != null && this.Proxy.IsAppConnected)
            {
                this.OnPluginStatusChanged(Loupedeck.PluginStatus.Normal, "");
            }
            else
            {
                this.OnPluginStatusChanged(Loupedeck.PluginStatus.Warning, "Not connected to App", "https://support.GenStreamPlugin.com", "more details");
            }

        }

    }
}
