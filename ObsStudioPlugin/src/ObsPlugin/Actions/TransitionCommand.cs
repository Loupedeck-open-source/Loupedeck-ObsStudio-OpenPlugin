namespace Loupedeck.ObsStudioPlugin.Actions
{
    using System;

    internal class TransitionCommand : PluginDynamicCommand
    {
        private const String IMGAction = "STREAM_Transition.png";

        public TransitionCommand()
            : base(displayName: "Studio Mode Transition",
                   description: "Changes your preview in Studio Mode to the active program scene",
                   groupName: "") => this.Name = "TransitionCommand";

        protected override Boolean OnLoad()
        {
            ObsStudioPlugin.Proxy.AppConnected += this.OnAppConnected;
            ObsStudioPlugin.Proxy.AppDisconnected += this.OnAppDisconnected;

            ObsStudioPlugin.Proxy.AppEvtStudioModeOn += this.OnAppStudioModeOn;
            ObsStudioPlugin.Proxy.AppEvtStudioModeOff += this.OnAppStudioModeOff;
            this.IsEnabled = false;
            return true;
        }

        protected override Boolean OnUnload()
        {
            ObsStudioPlugin.Proxy.AppConnected -= this.OnAppConnected;
            ObsStudioPlugin.Proxy.AppDisconnected -= this.OnAppDisconnected;
            ObsStudioPlugin.Proxy.AppEvtStudioModeOn -= this.OnAppStudioModeOn;
            ObsStudioPlugin.Proxy.AppEvtStudioModeOff -= this.OnAppStudioModeOff;
            return true;
        }

        private void OnAppConnected(Object sender, EventArgs e) => this.OnAppStudioModeOff(sender, e);

        private void OnAppStudioModeOn(Object sender, EventArgs e) => this.IsEnabled = true;

        private void OnAppStudioModeOff(Object sender, EventArgs e) => this.IsEnabled = false;

        private void OnAppDisconnected(Object sender, EventArgs e) => this.OnAppStudioModeOff(sender, e);

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize) => (this.Plugin as ObsStudioPlugin).GetPluginCommandImage(imageSize, IMGAction);

        protected override void RunCommand(String actionParameter) => ObsStudioPlugin.Proxy.AppRunTransition();
    }
}
