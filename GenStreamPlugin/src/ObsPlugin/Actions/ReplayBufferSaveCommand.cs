namespace Loupedeck.ObsPlugin.Actions
{
    using System;

    public class ReplayBufferSaveCommand: PluginDynamicCommand
    {
        private const String IMGAction = "Loupedeck.ObsPlugin.icons.STREAM_SaveReplayBuffer.png";

        private ObsAppProxy Proxy => (this.Plugin as ObsPlugin).Proxy;

         public ReplayBufferSaveCommand() : base(displayName: "Save Replay Buffer", description: "Flush and save the contents of the Replay Buffer to disk. ", groupName: null) { }

        protected override Boolean OnLoad()
        {
            this.Proxy.EvtAppConnected += this.OnAppConnected;
            this.Proxy.EvtAppDisconnected += this.OnAppDisconnected;

            this.Proxy.AppEvtReplayBufferOff += this.OnAppReplayBufferOff;
            this.Proxy.AppEvtReplayBufferOn += this.OnAppReplayBufferOn;
            this.IsEnabled = false;

            return true;
        }

        protected override Boolean OnUnload()
        {
            this.Proxy.EvtAppConnected -= this.OnAppConnected;
            this.Proxy.EvtAppDisconnected -= this.OnAppDisconnected;
            this.Proxy.AppEvtReplayBufferOff -= this.OnAppReplayBufferOff;
            this.Proxy.AppEvtReplayBufferOn -= this.OnAppReplayBufferOn;

            return true;
        }

        private void OnAppConnected(Object sender, EventArgs e) => this.OnAppReplayBufferOff(sender, e);
        
        private void OnAppReplayBufferOn(Object sender, EventArgs e) => this.IsEnabled = true;
        
        private void OnAppReplayBufferOff(Object sender, EventArgs e) => this.IsEnabled = false;

        private void OnAppDisconnected(Object sender, EventArgs e) => this.OnAppReplayBufferOff(sender, e);

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize) => EmbeddedResources.ReadImage(IMGAction);

        protected override void RunCommand(String actionParameter) => this.Proxy.AppSaveReplayBuffer();


    }
}
