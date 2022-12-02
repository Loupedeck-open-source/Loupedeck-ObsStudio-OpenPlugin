namespace Loupedeck.ObsStudioPlugin.Actions
{
    using System;

    internal class ReplayBufferSaveCommand : PluginDynamicCommand
    {
        private const String IMGAction = "STREAM_SaveReplay.png";
        
        public ReplayBufferSaveCommand()
            : base(displayName: "Replay Buffer Save", 
                   description: "Creates a recording of the Replay Buffer content", 
                   groupName: "")
        {
        }

        protected override Boolean OnLoad()
        {
            ObsStudioPlugin.Proxy.AppConnected += this.OnAppConnected;
            ObsStudioPlugin.Proxy.AppDisconnected += this.OnAppDisconnected;

            ObsStudioPlugin.Proxy.AppEvtReplayBufferOff += this.OnAppReplayBufferOff;
            ObsStudioPlugin.Proxy.AppEvtReplayBufferOn += this.OnAppReplayBufferOn;
            this.IsEnabled = false;

            return true;
        }

        protected override Boolean OnUnload()
        {
            ObsStudioPlugin.Proxy.AppConnected -= this.OnAppConnected;
            ObsStudioPlugin.Proxy.AppDisconnected -= this.OnAppDisconnected;
            ObsStudioPlugin.Proxy.AppEvtReplayBufferOff -= this.OnAppReplayBufferOff;
            ObsStudioPlugin.Proxy.AppEvtReplayBufferOn -= this.OnAppReplayBufferOn;

            return true;
        }

        private void OnAppConnected(Object sender, EventArgs e) => this.OnAppReplayBufferOff(sender, e);

        private void OnAppReplayBufferOn(Object sender, EventArgs e) => this.IsEnabled = true;

        private void OnAppReplayBufferOff(Object sender, EventArgs e) => this.IsEnabled = false;

        private void OnAppDisconnected(Object sender, EventArgs e) => this.OnAppReplayBufferOff(sender, e);

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize) => (this.Plugin as ObsStudioPlugin).GetPluginCommandImage(imageSize, IMGAction);

        protected override void RunCommand(String actionParameter) => ObsStudioPlugin.Proxy.AppSaveReplayBuffer();
    }
}
