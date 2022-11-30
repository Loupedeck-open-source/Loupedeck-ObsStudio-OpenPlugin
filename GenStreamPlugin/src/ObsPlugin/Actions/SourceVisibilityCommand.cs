namespace Loupedeck.ObsStudioPlugin.Actions
{
    using System;

    internal class SourceVisibilityCommand : PluginMultistateDynamicCommand
    {
        public const String IMGSceneSelected = "SourceOn.png";
        public const String IMGSceneUnselected = "SourceOff.png";
        public const String IMGSceneInaccessible = "SourceInaccessible.png";
        public const String SourceNameUnknown = "Offline";

        public SourceVisibilityCommand()
        {
            this.Description = "Shows/Hides a Source";
            this.GroupName = "2. Sources";
            _ = this.AddState("Hidden", "Source hidden");
            _ = this.AddState("Visible", "Source visible");
        }

        protected override Boolean OnLoad()
        {
            this.IsEnabled = false;

            ObsStudioPlugin.Proxy.AppConnected += this.OnAppConnected;
            ObsStudioPlugin.Proxy.AppDisconnected += this.OnAppDisconnected;

            ObsStudioPlugin.Proxy.AppEvtSceneListChanged += this.OnSceneListChanged;
            ObsStudioPlugin.Proxy.AppEvtCurrentSceneChanged += this.OnCurrentSceneChanged;

            ObsStudioPlugin.Proxy.AppEvtSceneItemVisibilityChanged += this.OnSceneItemVisibilityChanged;

            ObsStudioPlugin.Proxy.AppEvtSceneItemAdded += this.OnSceneItemAdded;
            ObsStudioPlugin.Proxy.AppEvtSceneItemRemoved += this.OnSceneItemRemoved;

            this.OnAppDisconnected(this, null);

            return true;
        }

        protected override Boolean OnUnload()
        {
            ObsStudioPlugin.Proxy.AppConnected -= this.OnAppConnected;
            ObsStudioPlugin.Proxy.AppDisconnected -= this.OnAppDisconnected;

            ObsStudioPlugin.Proxy.AppEvtSceneListChanged -= this.OnSceneListChanged;
            ObsStudioPlugin.Proxy.AppEvtCurrentSceneChanged -= this.OnCurrentSceneChanged;
            ObsStudioPlugin.Proxy.AppEvtSceneItemVisibilityChanged -= this.OnSceneItemVisibilityChanged;

            ObsStudioPlugin.Proxy.AppEvtSceneItemAdded -= this.OnSceneItemAdded;
            ObsStudioPlugin.Proxy.AppEvtSceneItemRemoved -= this.OnSceneItemRemoved;

            return true;
        }

        protected override void RunCommand(String actionParameter) => ObsStudioPlugin.Proxy.AppToggleSceneItemVisibility(actionParameter);

        private void OnSceneListChanged(Object sender, EventArgs e) => this.ResetParameters(true);

        private void OnCurrentSceneChanged(Object sender, EventArgs e) => this.ActionImageChanged();

        private void OnSceneItemAdded(OBSWebsocketDotNet.OBSWebsocket sender, String sceneName, String itemName)
        {
            this.AddSceneItemParameter(sceneName, itemName);
            this.ParametersChanged();
        }

        private void OnSceneItemRemoved(OBSWebsocketDotNet.OBSWebsocket sender, String sceneName, String itemName)
        {
            this.RemoveParameter(SceneItemKey.Encode(ObsStudioPlugin.Proxy.CurrentSceneCollection, sceneName, itemName));
            this.ParametersChanged();
        }

        private void OnAppConnected(Object sender, EventArgs e) => this.IsEnabled = true;

        private void OnAppDisconnected(Object sender, EventArgs e)
        {
            this.IsEnabled = false;

            this.ResetParameters(false);
            this.ActionImageChanged();
        }

        protected void OnSceneItemVisibilityChanged(OBSWebsocketDotNet.OBSWebsocket sender, String sceneName, String itemName, Boolean isVisible)
        {
            var actionParameter = SceneItemKey.Encode(ObsStudioPlugin.Proxy.CurrentSceneCollection, sceneName, itemName);
            _ = this.SetCurrentState(actionParameter, isVisible ? 1 : 0);
            this.ActionImageChanged(actionParameter);
        }

        protected override BitmapImage GetCommandImage(String actionParameter, Int32 stateIndex, PluginImageSize imageSize)
        {
            var sourceName = SourceNameUnknown;
            var imageName = IMGSceneInaccessible;
            if (SceneItemKey.TryParse(actionParameter, out var parsed))
            {
                sourceName = parsed.Source;
                imageName = parsed.Collection != ObsStudioPlugin.Proxy.CurrentSceneCollection
                    ? IMGSceneInaccessible
                    : stateIndex == 1 ? IMGSceneSelected : IMGSceneUnselected;
            }

            return (this.Plugin as ObsStudioPlugin).GetPluginCommandImage(imageSize, imageName, sourceName, imageName == IMGSceneSelected);
        }

        internal void AddSceneItemParameter(String sceneName, String itemName)
        {
            var key = SceneItemKey.Encode(ObsStudioPlugin.Proxy.CurrentSceneCollection, sceneName, itemName);
            this.AddParameter(key, $"{itemName}", $"{this.GroupName}{CommonStrings.SubgroupSeparator}{sceneName}").Description = 
                        ObsStudioPlugin.Proxy.AllSceneItems[key].Visible ? "Hide" : "Show" + $" source \"{itemName}\" of scene \"{sceneName}\"";
            this.SetCurrentState(key, ObsStudioPlugin.Proxy.AllSceneItems[key].Visible ? 1 : 0);
        }

        internal void ResetParameters(Boolean readContent)
        {
            this.RemoveAllParameters();

            if (readContent)
            {
                ObsStudioPlugin.Trace($"Adding {ObsStudioPlugin.Proxy.AllSceneItems?.Count} sources");

                foreach (var item in ObsStudioPlugin.Proxy.AllSceneItems)
                {
                    this.AddSceneItemParameter(item.Value.SceneName, item.Value.SourceName);
                }
            }

            this.ParametersChanged();
        }
    }
}
