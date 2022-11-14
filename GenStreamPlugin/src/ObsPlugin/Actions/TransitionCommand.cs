namespace Loupedeck.ObsPlugin.Actions
{
    using System;

    public class TransitionCommand: PluginDynamicCommand
    {
        private const String IMGAction = "Loupedeck.ObsPlugin.icons.STREAM_SaveReplay.png";

        private ObsAppProxy Proxy => (this.Plugin as ObsPlugin).Proxy;

         public TransitionCommand() : base(displayName: "Transition To Program", description: "Transitions the currently previewed scene to the main output.", groupName: null) { }

        protected override Boolean OnLoad()
        {
            this.Proxy.EvtAppConnected += this.OnAppConnected;
            this.Proxy.EvtAppDisconnected += this.OnAppDisconnected;

            this.Proxy.AppEvtStudioModeOn += this.OnAppStudioModeOn;
            this.Proxy.AppEvtStudioModeOff += this.OnAppStudioModeOff;
            this.IsEnabled = false;
            return true;
        }

        protected override Boolean OnUnload()
        {
            this.Proxy.EvtAppConnected -= this.OnAppConnected;
            this.Proxy.EvtAppDisconnected -= this.OnAppDisconnected;
            this.Proxy.AppEvtStudioModeOn -= this.OnAppStudioModeOn;
            this.Proxy.AppEvtStudioModeOff -= this.OnAppStudioModeOff;
            return true;
        }

        private void OnAppConnected(Object sender, EventArgs e) => this.OnAppStudioModeOff(sender, e);
        
        private void OnAppStudioModeOn(Object sender, EventArgs e) => this.IsEnabled = true;
        
        private void OnAppStudioModeOff(Object sender, EventArgs e) => this.IsEnabled = false;

        private void OnAppDisconnected(Object sender, EventArgs e) => this.OnAppStudioModeOff(sender, e);

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize) => EmbeddedResources.ReadImage(IMGAction);

        protected override void RunCommand(String actionParameter) => this.Proxy.AppRunTransition();


    }
}
