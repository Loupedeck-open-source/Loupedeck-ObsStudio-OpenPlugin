namespace Loupedeck.ObsStudioPlugin.DynamicActions
{
    using System;

    using Loupedeck.ObsStudioPlugin.Actions;

    public class SourcesProfileActionsTreeCommands : PluginDynamicCommand
    {
        private ObsAppProxy Proxy => (this.Plugin as ObsStudioPlugin).Proxy;

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
            this.Proxy.AppConnected += this.OnAppConnected;
            this.Proxy.AppDisconnected += this.OnAppDisconnected;
            this.IsEnabled = false;
            return true;
        }

        protected override Boolean OnUnload()
        {
            this.Proxy.AppConnected -= this.OnAppConnected;
            this.Proxy.AppDisconnected -= this.OnAppDisconnected;
            return true;
        }

        private void OnAppConnected(Object sender, EventArgs e) => this.IsEnabled = true;
        private void OnAppDisconnected(Object sender, EventArgs e) => this.IsEnabled = false;

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var imageName = SourceVisibilityCommand.IMGSceneInaccessible;
            var sourceName = "Unknown";
            
            if (this.Proxy.TryConvertLegacyActionParamToKey(actionParameter, out var key_struct))
            {
                sourceName = key_struct.Source;
                var key = key_struct.Stringize();

                if (this.Proxy.AllSceneItems.ContainsKey(key))
                {
                    imageName = this.Proxy.AllSceneItems[key].Visible 
                                     ? SourceVisibilityCommand.IMGSceneSelected
                                     : SourceVisibilityCommand.IMGSceneUnselected;
                }
            }

            return (this.Plugin as ObsStudioPlugin).GetPluginCommandImage(imageSize, imageName, sourceName, imageName == SourceVisibilityCommand.IMGSceneSelected);
        }

        protected override void RunCommand(String actionParameter)
        {
            if (this.Proxy.TryConvertLegacyActionParamToKey(actionParameter, out var key_struct))
            {
                this.Proxy.AppToggleSceneItemVisibility(key_struct.Stringize());
            }
        }
    }
}
