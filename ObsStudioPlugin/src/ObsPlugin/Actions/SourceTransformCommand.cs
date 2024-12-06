namespace Loupedeck.ObsStudioPlugin.Actions
{
    using System;

    internal class SourceTransformCommand : PluginMultistateDynamicCommand
    {
        public const String IMGSceneSelected = "SourceVisibilityOn.svg";
        public const String IMGSceneUnselected = "SourceVisibilityOff.svg";
        public const String IMGSceneInaccessible = "SourceDisabled.png";
        public const String SourceNameUnknown = "Name Not Available";

        private const Int16 SOURCE_UNSELECTED = 0;
        private const Int16 SOURCE_SELECTED = 1;

        public SourceTransformCommand()
        {
            this.Description = "Enables Move Top/Bottom/Lef/Right and Zoom In/Out of a Source";
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
            ObsStudioPlugin.Proxy.AppEvtSceneNameChanged += this.OnSceneListChanged; //Note using same handler since we just re-generate params


            ObsStudioPlugin.Proxy.AppEvtSceneItemVisibilityChanged += this.OnSceneItemVisibilityChanged;

            ObsStudioPlugin.Proxy.AppEvtSceneItemAdded += this.OnSceneItemAdded;
            ObsStudioPlugin.Proxy.AppEvtSceneItemRemoved += this.OnSceneItemRemoved;

            ObsStudioPlugin.Proxy.AppInputRenamed += this.OnSourceRenamed;


            this.OnAppDisconnected(this, null);

            return true;
        }

        protected override Boolean OnUnload()
        {
            ObsStudioPlugin.Proxy.AppConnected -= this.OnAppConnected;
            ObsStudioPlugin.Proxy.AppDisconnected -= this.OnAppDisconnected;

            ObsStudioPlugin.Proxy.AppEvtSceneListChanged -= this.OnSceneListChanged;
            ObsStudioPlugin.Proxy.AppEvtCurrentSceneChanged -= this.OnCurrentSceneChanged;
            ObsStudioPlugin.Proxy.AppEvtSceneNameChanged -= this.OnSceneListChanged;

            ObsStudioPlugin.Proxy.AppEvtSceneItemVisibilityChanged -= this.OnSceneItemVisibilityChanged;

            ObsStudioPlugin.Proxy.AppEvtSceneItemAdded -= this.OnSceneItemAdded;
            ObsStudioPlugin.Proxy.AppEvtSceneItemRemoved -= this.OnSceneItemRemoved;

            ObsStudioPlugin.Proxy.AppInputRenamed -= this.OnSourceRenamed;


            return true;
        }

        protected override void RunCommand(String actionParameter) => ObsStudioPlugin.Proxy.AppSceneItemVisibilityToggle(actionParameter);

        private void OnSceneListChanged(Object sender, EventArgs e) => this.ResetParameters(true);

        private void OnCurrentSceneChanged(Object sender, EventArgs e) => this.ActionImageChanged();

        private void OnSceneItemAdded(Object sender, SceneItemArgs arg)
        {
            this.AddSceneItemParameter(arg.SceneName, arg.ItemName, arg.ItemId);
            this.ParametersChanged();
        }

        private void OnSceneItemRemoved(Object sender, SceneItemArgs arg)
        {
            var s = SceneItemKey.Encode(ObsStudioPlugin.Proxy.CurrentSceneCollection, arg.SceneName, arg.ItemId,arg.ItemName);
            this.Plugin.Log.Info($"Removing scene item {s} sources");
            this.RemoveParameter(s);
            this.ParametersChanged();
        }

        //Note: We can possibly do cherry-picking on the parameters but that require quite a bit of code. 
        private void OnSourceRenamed(Object sender, OldNewStringChangeEventArgs args) => this.ResetParameters(true);

        private void OnAppConnected(Object sender, EventArgs e) => this.IsEnabled = true;

        private void OnAppDisconnected(Object sender, EventArgs e)
        {
            this.IsEnabled = false;

            this.ResetParameters(false);
            this.ActionImageChanged();
        }

        protected void OnSceneItemVisibilityChanged(Object sender, SceneItemVisibilityChangedArgs arg)
        {
            var actionParameter = SceneItemKey.Encode(ObsStudioPlugin.Proxy.CurrentSceneCollection, arg.SceneName, arg.ItemId);
            _ = this.SetCurrentState(actionParameter, arg.Visible ? SOURCE_SELECTED : SOURCE_UNSELECTED);
            //this.ActionImageChanged(actionParameter);
        }

        protected override BitmapImage GetCommandImage(String actionParameter, Int32 stateIndex, PluginImageSize imageSize)
        {
            var imageName = IMGSceneInaccessible;
            if (SceneItemKey.TryParse(actionParameter, out var parsed))
            {
                imageName = parsed.Collection != ObsStudioPlugin.Proxy.CurrentSceneCollection || !ObsStudioPlugin.Proxy.AllSceneItems.ContainsKey(actionParameter)
                    ? IMGSceneInaccessible
                    : stateIndex == SOURCE_SELECTED ? IMGSceneSelected : IMGSceneUnselected;
            }

            return EmbeddedResources.ReadBinaryFile(ObsStudioPlugin.ImageResPrefix + imageName).ToImage();
        }

        private void AddSceneItemParameter(String sceneName, String itemName,Int32 itemId)
        {
            var key = SceneItemKey.Encode(ObsStudioPlugin.Proxy.CurrentSceneCollection, sceneName, itemId);
            this.AddParameter(key, $"{itemName}", $"{this.GroupName}{CommonStrings.SubgroupSeparator}{sceneName}").Description = 
                        ObsStudioPlugin.Proxy.AllSceneItems[key].Visible ? "Hide" : "Show" + $" source \"{itemName}\" of scene \"{sceneName}\"";
            this.SetCurrentState(key, ObsStudioPlugin.Proxy.AllSceneItems[key].Visible ? SOURCE_SELECTED : SOURCE_UNSELECTED);
        }

        private void ResetParameters(Boolean readContent)
        {
            this.RemoveAllParameters();

            if (readContent)
            {
                this.Plugin.Log.Info($"Adding {ObsStudioPlugin.Proxy.AllSceneItems?.Count} sources");

                foreach (var item in ObsStudioPlugin.Proxy.AllSceneItems)
                {
                    this.AddSceneItemParameter(item.Value.SceneName, item.Value.SourceName, item.Value.SourceId);
                }
            }

            this.ParametersChanged();
            this.ActionImageChanged();
        }
    }
}
