namespace Loupedeck.ObsStudioPlugin
{
    using System;

    using OBSWebsocketDotNet;

    /// <summary>
    /// Proxy to OBS websocket server, for API reference see
    /// https://github.com/obsproject/obs-websocket/blob/4.x-compat/docs/generated/protocol.md
    /// </summary>
    public partial class ObsAppProxy
    {
        public SceneItemUpdateCallback AppEvtSceneItemAdded;
        public SceneItemUpdateCallback AppEvtSceneItemRemoved;
        public SceneItemVisibilityChangedCallback AppEvtSceneItemVisibilityChanged;

        private void OnObsSceneItemVisibilityChanged(OBSWebsocket sender, String sceneName, String itemName, Boolean isVisible)
        {
            var key = SceneItemKey.Encode(this.CurrentSceneCollection, sceneName, itemName);
            if (this.AllSceneItems.ContainsKey(key))
            {
                this.AllSceneItems[key].Visible = isVisible;
            }
            else
            {
                ObsStudioPlugin.Trace($"WARNING: Cannot update visiblity: item {itemName} scene {sceneName} not in dictionary");
            }

            this.AppEvtSceneItemVisibilityChanged?.Invoke(sender, sceneName, itemName, isVisible);
        }

        private void OnObsSceneItemAdded(OBSWebsocket sender, String sceneName, String itemName)
        {
            ObsStudioPlugin.Trace($"OBS: OnObsSceneItemAdded: Item '{itemName}' scene '{sceneName}'");

            if ( sceneName!= this.CurrentScene.Name ) 
            {
                ObsStudioPlugin.Trace($"Warning: OnObsSceneItemAdded received non-current scene '{sceneName}'. Current is '{this.CurrentScene.Name}'. Ignoring");
                return;
            }

            // Re-reading current scene, since this.CurrentScene does not contain the item
            if(!Helpers.TryExecuteFunc(() => this.GetCurrentScene(), out var obsCurrentScene))
            {
                ObsStudioPlugin.Trace($"Warning:Cannot get current scene from OBS");
                return;
            }

            if( obsCurrentScene.Name != this.CurrentScene.Name )
            {
                ObsStudioPlugin.Trace($"Warning: Current scene changed to '{obsCurrentScene}' mid-way.");
                return;
            }

            this.CurrentScene = obsCurrentScene;

            var itemIndex = this.CurrentScene.Items.FindIndex(x => x.SourceName == itemName);
            
            if (itemIndex == -1)
            {
                ObsStudioPlugin.Trace($"Warning: Cannot find item '{itemName}' among current scene items.");
                return;
            }

            var item = this.CurrentScene.Items[itemIndex];

            //Creating item and fetching all missing data for it from OBS
            var sourceDictItem = SceneItemDescriptor.CreateSourceDictItem(this.CurrentSceneCollection, sceneName, item, this,null);

            if (sourceDictItem == null)
            {
                ObsStudioPlugin.Trace($"Warning: Cannot get properties for item '{itemName}'.");
                return;
            }

            this.AllSceneItems.Add( SceneItemKey.Encode(this.CurrentSceneCollection, sceneName, item.SourceName), sourceDictItem);

            this.AppEvtSceneItemAdded?.Invoke(this, sceneName, itemName);
        }

        private void OnObsSceneItemRemoved(OBSWebsocket sender, String sceneName, String itemName)
        {
            ObsStudioPlugin.Trace($"OBS: Scene Item {itemName} removed from scene {sceneName}");

            var key = SceneItemKey.Encode(this.CurrentSceneCollection, sceneName, itemName);
            if (this.AllSceneItems.ContainsKey(key))
            {
                _ = this.AllSceneItems.Remove(key);
                this.AppEvtSceneItemRemoved?.Invoke(this, sceneName, itemName);
            }
            else
            {
                ObsStudioPlugin.Trace($"Warning: Cannot find item {itemName} in scene {sceneName}");
            }
        }

        public void AppToggleSceneItemVisibility(String key)
        {
            if (this.IsAppConnected && this.AllSceneItems.ContainsKey(key))
            {
                try
                {
                    var item = this.AllSceneItems[key];
                    this.SetSourceRender(item.SourceName, !item.Visible, item.SceneName);
                }
                catch (Exception ex)
                {
                    ObsStudioPlugin.Trace($"Warning: Exception {ex.Message} when toggling visibility to {key}");
                }
            }
        }
    }
}
