namespace Loupedeck.ObsPlugin
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
            if (!Helpers.TryExecuteSafe(() => this.AllSceneItems[key].Visible = isVisible))
            {
                this.Trace($"WARNING: Cannot update visiblity for item {itemName} scene {sceneName} from dictionary");
            }

            this.AppEvtSceneItemVisibilityChanged?.Invoke(sender, sceneName, itemName, isVisible);
        }

        private void OnObsSceneItemAdded(OBSWebsocket sender, String sceneName, String itemName)
        {
            this.Trace($"OBS: Scene Item {itemName} added to scene {sceneName}");

            // Re-reading current scene
            if (Helpers.TryExecuteFunc(() => this.GetCurrentScene(), out var currscene))
            {
                // Re=reading current scene to make sure all items are there
                this.CurrentScene = currscene;
            }

            if (!this.AddSceneItemToDictionary(sceneName, itemName))
            {
                this.Trace($"Warning: Cannot add item {itemName} to scene {sceneName}");
            }
            else
            {
                this.AppEvtSceneItemAdded?.Invoke(this, sceneName, itemName);
            }
        }

        private void OnObsSceneItemRemoved(OBSWebsocket sender, String sceneName, String itemName)
        {
            this.Trace($"OBS: Scene Item {itemName} removed from scene {sceneName}");

            var key = SceneItemKey.Encode(this.CurrentSceneCollection, sceneName, itemName);
            if (this.AllSceneItems.ContainsKey(key))
            {
                this.AllSceneItems.Remove(key);
                this.AppEvtSceneItemRemoved?.Invoke(this, sceneName, itemName);
            }
            else
            {
                this.Trace($"Warning: Cannot find item {itemName} in scene {sceneName}");
            }
        }

        public void AppToggleSceneItemVisibility(String key)
        {
            if (this.IsAppConnected)
            {
                try
                {
                    var item = this.AllSceneItems[key];
                    this.SetSourceRender(item.SourceName, !item.Visible, item.SceneName);
                }
                catch (Exception ex)
                {
                    this.Trace($"Warning: Exception {ex.Message} when toggling visibility to {key}");
                }
            }
        }
    }
}
