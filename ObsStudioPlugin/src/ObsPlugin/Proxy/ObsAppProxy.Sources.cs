namespace Loupedeck.ObsStudioPlugin
{
    using System;
    using System.Collections.Generic;
    
    using OBSWebsocketDotNet;

    /// <summary>
    /// Proxy to OBS websocket server, for API reference see
    /// https://github.com/obsproject/obs-websocket/blob/4.x-compat/docs/generated/protocol.md
    /// </summary>
    internal partial class ObsAppProxy
    {
        public event EventHandler<TwoStringArgs> AppEvtSceneItemAdded;
        public event EventHandler<TwoStringArgs> AppEvtSceneItemRemoved;
        public event EventHandler<SceneItemVisibilityChangedArgs> AppEvtSceneItemVisibilityChanged;

        /// <summary>
        /// Our own dictionary of scene items of all scenes in current collection, with all properties
        /// Note: Scene item is an instance of the source in particular scene.  Most of the source
        /// properties are shared among scenes with just a few (like visibility) being scene-specif
        ///
        /// </summary>
        // 'Main' dictionary, with Scene-Item ID being a key
        // Dictionary
        public Dictionary<String, SceneItemDescriptor> AllSceneItems = new Dictionary<String, SceneItemDescriptor>();


        /// <summary>
        ///  Controls scene item visibilty. 
        /// </summary>
        /// <param name="key">Key for scene item</param>
        /// <param name="forceState">if True, force specific state to the scene item, otherwise toggle. </param>
        /// <param name="newState">state to force</param>
        public void AppSceneItemVisibilityToggle(String key, Boolean forceState = false, Boolean newState = false)
        {
            if (this.IsAppConnected && this.AllSceneItems.ContainsKey(key))
            {
                try
                {
                    var item = this.AllSceneItems[key];
                    this.SetSourceRender(item.SourceName, forceState ? newState : !item.Visible, item.SceneName);
                }
                catch (Exception ex)
                {
                    this.Plugin.Log.Error($"Exception {ex.Message} when toggling visibility to {key}");
                }
            }
        }
        private void OnObsSceneItemVisibilityChanged(OBSWebsocket sender, String sceneName, String itemName, Boolean isVisible)
        {
            var key = SceneItemKey.Encode(this.CurrentSceneCollection, sceneName, itemName);
            if (this.AllSceneItems.ContainsKey(key))
            {
                this.AllSceneItems[key].Visible = isVisible;
            }
            else
            {
                this.Plugin.Log.Warning($"Cannot update visiblity: item {itemName} scene {sceneName} not in dictionary");
            }

            this.AppEvtSceneItemVisibilityChanged?.Invoke(sender, new SceneItemVisibilityChangedArgs(sceneName, itemName, isVisible));
        }

        private void OnObsSceneItemAdded(OBSWebsocket sender, String sceneName, String itemName)
        {
            this.Plugin.Log.Info($"OBS: OnObsSceneItemAdded: Item '{itemName}' scene '{sceneName}'");

            if (sceneName != this.CurrentScene.Name)
            {
                this.Plugin.Log.Warning($"OnObsSceneItemAdded received non-current scene '{sceneName}'. Current is '{this.CurrentScene.Name}'. Ignoring");
                return;
            }

            // Re-reading current scene, since this.CurrentScene does not contain the item
            if (!Helpers.TryExecuteFunc(() => this.GetCurrentScene(), out var obsCurrentScene))
            {
                this.Plugin.Log.Warning($"Cannot get current scene from OBS");
                return;
            }

            if (obsCurrentScene.Name != this.CurrentScene.Name)
            {
                this.Plugin.Log.Warning($"Current scene changed to '{obsCurrentScene}' mid-way.");
                return;
            }

            this.CurrentScene = new Scene(obsCurrentScene);

            var itemIndex = this.CurrentScene.Items.FindIndex(x => x.SourceName == itemName);

            if (itemIndex == -1)
            {
                this.Plugin.Log.Warning($"Cannot find item '{itemName}' among current scene items.");
                return;
            }

            var item = this.CurrentScene.Items[itemIndex];

            //Creating item and fetching all missing data for it from OBS
            var sourceDictItem = SceneItemDescriptor.CreateSourceDictItem(this.CurrentSceneCollection, sceneName, item, this, null);

            if (sourceDictItem == null)
            {
                this.Plugin.Log.Warning($"Cannot get properties for item '{itemName}'.");
                return;
            }

            this.AllSceneItems[SceneItemKey.Encode(this.CurrentSceneCollection, sceneName, item.SourceName)] = sourceDictItem;

            this.AppEvtSceneItemAdded?.Invoke(this, new TwoStringArgs(sceneName, itemName));
        }

        private void OnObsSceneItemRemoved(OBSWebsocket sender, String sceneName, String itemName)
        {
            this.Plugin.Log.Info($"OBS: Scene Item {itemName} removed from scene {sceneName}");

            var key = SceneItemKey.Encode(this.CurrentSceneCollection, sceneName, itemName);
            if (this.AllSceneItems.ContainsKey(key))
            {
                this.AllSceneItems.Remove(key);

                this.AppEvtSceneItemRemoved?.Invoke(this, new TwoStringArgs(sceneName, itemName));
            }
            else
            {
                this.Plugin.Log.Warning($"Cannot find item {itemName} in scene {sceneName}");
            }
        }

    }
}
