namespace Loupedeck.GenStreamPlugin
{
    using System;

    // This class contains the plugin-level logic of the Loupedeck plugin.

    public class GenStreamPlugin : Plugin
    {
        public GenStreamProxy Proxy = new GenStreamProxy();

        // Gets a value indicating whether this is an Universal plugin or an Application plugin.
        public override Boolean UsesApplicationApiOnly => true;

        // Gets a value indicating whether this is an API-only plugin.
        public override Boolean HasNoApplication => true;

        // Load is called once as plugin is being initialized during service start.
        public override void Load()
        {
            this.ClientApplication.ApplicationStarted += this.OnApplicationStarted;
            this.ClientApplication.ApplicationStopped += this.OnApplicationStopped;

            this.ClientApplication.ApplicationInstanceStarted += this.OnApplicationStarted;
            this.ClientApplication.ApplicationInstanceStopped += this.OnApplicationStopped;

            this.Proxy.EvtAppConnected += this.OnAppConnStatusChange;
            this.Proxy.EvtAppDisconnected += this.OnAppConnStatusChange;

            // If Loupedeck is started after App, check for the status.
            // Otherwise, we will connect to App in OnApplicationStarted
            if (this.ClientApplication.IsRunning())
            {
                Tracer.Trace("GenStreamPlugin: app already started: Running");
                this.OnApplicationStarted(null, null);
            }

            this.Update_PluginStatus();
        }

        // Unload is called once when plugin is being unloaded.
        public override void Unload()
        {
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
            Tracer.Trace("GenStreamPlugin:OnApplicationStarted");
            this.Proxy.Connect();
        }

        private void OnApplicationStopped(Object sender, EventArgs e)
        {
            this.Proxy.Disconnect();
            this.Update_PluginStatus();
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
