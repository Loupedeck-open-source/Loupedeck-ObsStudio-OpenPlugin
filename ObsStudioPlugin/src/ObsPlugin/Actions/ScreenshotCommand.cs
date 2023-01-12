namespace Loupedeck.ObsStudioPlugin.Actions
{
    using System;

    internal class ScreenshotCommand : PluginDynamicCommand
    {
        private const String IMGAction = "Workspaces.png";

        private const String InvalidScreenshotFolder = "Cannot find folder for screenshot saving, feature disabled";

        public ScreenshotCommand()
            : base(displayName: "Screenshot",
                   description: String.IsNullOrEmpty(ObsAppProxy.ScreenshotsSavingPath) ? InvalidScreenshotFolder  : "Takes a screenshot of currently active scene and saves it to " + ObsAppProxy.ScreenshotsSavingPath,
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

        private void OnAppConnected(Object sender, EventArgs e) => this.IsEnabled = !String.IsNullOrEmpty(ObsAppProxy.ScreenshotsSavingPath);

        private void OnAppDisconnected(Object sender, EventArgs e) => this.IsEnabled = false;

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize) => (this.Plugin as ObsStudioPlugin).GetPluginCommandImage(imageSize, IMGAction);

        protected override void RunCommand(String actionParameter) => ObsStudioPlugin.Proxy.AppTakeScreenshot();
    }
}
