namespace Loupedeck.ObsStudioPlugin.DynamicActions
{
    using System;

    using Loupedeck.ObsStudioPlugin.Actions;

    internal class SourcesProfileActionsTreeCommands : PluginDynamicCommand
    {
        //Note DeviceTypeNone -- so that actions is not visible in the UI' action tree.
        public SourcesProfileActionsTreeCommands()
            : base(displayName: "LegacySourcesAction",
                   description: "",
                   groupName: "",
                   DeviceType.None)
        {

        }

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

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var imageName = SourceVisibilityCommand.IMGSceneInaccessible;
            var sourceName = "Unknown";
            
            if (ObsStudioPlugin.Proxy.TryConvertLegacyActionParamToKey(actionParameter, out var key_struct))
            {
                sourceName = key_struct.Source;
                var key = key_struct.Stringize();

                if (ObsStudioPlugin.Proxy.AllSceneItems.ContainsKey(key))
                {
                    imageName = ObsStudioPlugin.Proxy.AllSceneItems[key].Visible 
                                     ? SourceVisibilityCommand.IMGSceneSelected
                                     : SourceVisibilityCommand.IMGSceneUnselected;
                }
            }

            return (this.Plugin as ObsStudioPlugin).GetPluginCommandImage(imageSize, imageName, sourceName, imageName == SourceVisibilityCommand.IMGSceneSelected);
        }

        protected override void RunCommand(String actionParameter)
        {
            if (ObsStudioPlugin.Proxy.TryConvertLegacyActionParamToKey(actionParameter, out var key_struct))
            {
                ObsStudioPlugin.Proxy.AppToggleSceneItemVisibility(key_struct.Stringize());
            }
        }
    }
}
