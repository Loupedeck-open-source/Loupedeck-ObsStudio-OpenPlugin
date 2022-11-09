namespace Loupedeck.GenStreamPlugin.Actions
{
    using System;

    class SourceVisibilityCommand : PluginMultistateDynamicCommand
    {

        private GenStreamProxy Proxy => (this.Plugin as GenStreamPlugin).Proxy;
    
        private const String IMG_SceneSelected = "Loupedeck.GenStreamPlugin.icons.SourceOn.png";
        private const String IMG_SceneUnselected = "Loupedeck.GenStreamPlugin.icons.SourceOff.png";
        private const String IMG_SceneInaccessible = "Loupedeck.GenStreamPlugin.icons.CloseDesktop.png";
        private const String IMG_Offline = "Loupedeck.GenStreamPlugin.icons.SoftwareNotFound.png";
        private const String SourceNameUnknown = "Offline";

        public SourceVisibilityCommand()
        {
            this.Name = "Source Visibility";
            this.Description = "Shows/Hides a Source";
            this.GroupName = "Current Sources";

            this.AddState("Hidden", "Source hidden");
            this.AddState("Visible","Source visible");
        }

        protected override Boolean OnLoad()
        {
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

        private void OnAppConnected(Object sender, EventArgs e)
        { 
            //We expect to get SceneCollectionChange so doin' nothin' here. 
        }

        private void OnAppDisconnected(Object sender, EventArgs e)
        {
            this.ResetParameters(false);
            this.ActionImageChanged();
        }
        protected void OnSceneItemVisibilityChanged(OBSWebsocketDotNet.OBSWebsocket sender, String sceneName, String itemName, Boolean isVisible)
        {
            var actionParameter = SceneItemKey.Encode(this.Proxy?.CurrentSceneCollection, sceneName, itemName);
            this.SetCurrentState(actionParameter, isVisible ? 1 : 0);
            this.ActionImageChanged();
        }
        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var sourceName = SourceNameUnknown;
            var imageName = IMG_Offline;
            if ( SceneItemKey.TryParse(actionParameter, out var parsed) && this.TryGetCurrentStateIndex(actionParameter, out var currentState))
            {
                sourceName = parsed.Source; 

                imageName = parsed.Collection != this.Proxy.CurrentSceneCollection
                    ? IMG_SceneInaccessible
                    : currentState == 1 ? IMG_SceneSelected : IMG_SceneUnselected;
            }

            //FIXME: We need to learn to cache bitmaps. Here the key can be same 3 items: image name, state # and sourceName text
            return GenStreamPlugin.NameOverBitmap(imageSize, imageName, sourceName);
        }

        internal void AddSceneItemParameter(String sceneName, String itemName)
        {
            var key = SceneItemKey.Encode(this.Proxy.CurrentSceneCollection, sceneName, itemName);
            this.AddParameter(key, $"{itemName} ({sceneName})", this.GroupName);
            this.SetCurrentState(key, this.Proxy.allSceneItems[key].Visible ? 1 : 0);
        }

        internal void ResetParameters(Boolean readContent)
        {
            this.RemoveAllParameters();

            if (readContent)
            {
                this.Proxy.Trace($"Adding {this.Proxy.allSceneItems?.Count} sources");

                foreach (var item in this.Proxy.allSceneItems)
                {
                    this.AddSceneItemParameter(item.Value.SceneName, item.Value.SourceName);
                }
            }

            this.ParametersChanged();
        }

    }
}
