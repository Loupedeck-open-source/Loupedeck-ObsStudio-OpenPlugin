namespace Loupedeck.ObsPlugin.Actions
{
    using System;

    public  class SourceVisibilityCommand : PluginMultistateDynamicCommand
    {
        private ObsAppProxy Proxy => (this.Plugin as ObsPlugin).Proxy;

        private const String IMGSceneSelected = "SourceOn.png";
        private const String IMGSceneUnselected = "SourceOff.png";
        private const String IMGSceneInaccessible = "SourceOff.png";
        private const String SourceNameUnknown = "Offline";

        public SourceVisibilityCommand()
        {
            this.Name = "Source Visibility";
            this.Description = "Shows/Hides a Source";
            this.GroupName = "Current Sources";
            _ = this.AddState("Hidden", "Source hidden");
            _ = this.AddState("Visible", "Source visible");
        }

        protected override Boolean OnLoad()
        {
            this.IsEnabled = false;

            this.Proxy.EvtAppConnected += this.OnAppConnected;
            this.Proxy.EvtAppDisconnected += this.OnAppDisconnected;

            this.Proxy.AppEvtSceneListChanged += this.OnSceneListChanged;
            this.Proxy.AppEvtCurrentSceneChanged += this.OnCurrentSceneChanged;

            this.Proxy.AppEvtSceneItemVisibilityChanged += this.OnSceneItemVisibilityChanged;

            this.Proxy.AppEvtSceneItemAdded += this.OnSceneItemAdded;
            this.Proxy.AppEvtSceneItemRemoved += this.OnSceneItemRemoved;

            this.OnAppDisconnected(this, null);

            return true;
        }

        protected override Boolean OnUnload()
        {
            this.Proxy.EvtAppConnected -= this.OnAppConnected;
            this.Proxy.EvtAppDisconnected -= this.OnAppDisconnected;

            this.Proxy.AppEvtSceneListChanged -= this.OnSceneListChanged;
            this.Proxy.AppEvtCurrentSceneChanged -= this.OnCurrentSceneChanged;
            this.Proxy.AppEvtSceneItemVisibilityChanged -= this.OnSceneItemVisibilityChanged;

            this.Proxy.AppEvtSceneItemAdded -= this.OnSceneItemAdded;
            this.Proxy.AppEvtSceneItemRemoved -= this.OnSceneItemRemoved;

            return true;
        }

        protected override void RunCommand(String actionParameter) => this.Proxy.AppToggleSceneItemVisibility(actionParameter);

        private void OnSceneListChanged(Object sender, EventArgs e) => this.ResetParameters(true);

        private void OnCurrentSceneChanged(Object sender, EventArgs e) => this.ActionImageChanged();

        private void OnSceneItemAdded(OBSWebsocketDotNet.OBSWebsocket sender, String sceneName, String itemName)
        {
            this.AddSceneItemParameter(sceneName, itemName);
            this.ParametersChanged();
        }

        private void OnSceneItemRemoved(OBSWebsocketDotNet.OBSWebsocket sender, String sceneName, String itemName)
        {
            this.RemoveParameter(SceneItemKey.Encode(this.Proxy?.CurrentSceneCollection, sceneName, itemName));
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
            var actionParameter = SceneItemKey.Encode(this.Proxy?.CurrentSceneCollection, sceneName, itemName);
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
                imageName = parsed.Collection != this.Proxy.CurrentSceneCollection
                    ? IMGSceneInaccessible
                    : stateIndex== 1 ? IMGSceneSelected : IMGSceneUnselected;
            }

            return (this.Plugin as ObsPlugin).GetPluginCommandImage(imageSize, imageName, sourceName, stateIndex == 1);
            

        }

        internal void AddSceneItemParameter(String sceneName, String itemName)
        {
            var key = SceneItemKey.Encode(this.Proxy.CurrentSceneCollection, sceneName, itemName);
            this.AddParameter(key, $"{itemName}", $"{this.GroupName}{CommonStrings.SubgroupSeparator}{sceneName}");
            _ = this.SetCurrentState(key, this.Proxy.AllSceneItems[key].Visible ? 1 : 0);
        }

        internal void ResetParameters(Boolean readContent)
        {
            this.RemoveAllParameters();

            if (readContent)
            {
                ObsPlugin.Trace($"Adding {this.Proxy.AllSceneItems?.Count} sources");

                foreach (var item in this.Proxy.AllSceneItems)
                {
                    this.AddSceneItemParameter(item.Value.SceneName, item.Value.SourceName);
                }
            }

            this.ParametersChanged();
        }
    }
}
