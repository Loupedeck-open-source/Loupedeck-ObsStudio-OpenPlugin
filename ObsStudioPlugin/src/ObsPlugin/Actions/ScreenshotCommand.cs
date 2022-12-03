namespace Loupedeck.ObsStudioPlugin.Actions
{
    using System;

    internal class ScreenshotCommand : PluginDynamicCommand
    {
        private const String IMGAction = "Workspaces.png";

        public ScreenshotCommand()
            : base(displayName: "Screenshot",
                   description: "Take a screenshot of currently active source with default parameters",
                   groupName: "") => this.Name = "Screenshot";

        protected override Boolean OnLoad()
        {
            ObsStudioPlugin.Proxy.AppConnected += this.OnAppConnected;
            ObsStudioPlugin.Proxy.AppDisconnected += this.OnAppDisconnected;
            this.IsEnabled = false;
            return true;
        }

        protected override Boolean OnUnload()
        {
            ObsStudioPlugin.Proxy.AppConnected -= this.OnAppConnected;
            ObsStudioPlugin.Proxy.AppDisconnected -= this.OnAppDisconnected;
            return true;
        }

        private void OnAppConnected(Object sender, EventArgs e) => this.IsEnabled = true;

        private void OnAppDisconnected(Object sender, EventArgs e) => this.IsEnabled = false;

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize) => (this.Plugin as ObsStudioPlugin).GetPluginCommandImage(imageSize, IMGAction);

        protected override void RunCommand(String actionParameter) => ObsStudioPlugin.Proxy.AppTakeScreenshot();
    }
}
