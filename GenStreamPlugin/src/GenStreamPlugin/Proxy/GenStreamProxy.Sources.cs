namespace Loupedeck.GenStreamPlugin
{
    using System;
    using System.Collections.Generic;
    using OBSWebsocketDotNet;

    /// <summary>
    /// Proxy to OBS websocket server, for API reference see
    /// https://github.com/obsproject/obs-websocket/blob/4.x-compat/docs/generated/protocol.md
    /// </summary>
    public partial class GenStreamProxy 
    {
        //        internal SceneItemProperties GetSceneItemProps(String itemName, String sceneName) => Helpers.TryExecuteSafe(()
        //        => this.Proxy.GetSceneItemProperties(itemName, sceneName), out var ret) ? ret : null;
        //SceneItemVisibilityChanged
        //    SceneItemRemoved
        //    SceneItemAdded
        //SetSourceRender
        //public event EventHandler<EventArgs> AppEvtSceneItemVisibilityChanged;

        public SceneItemUpdateCallback AppEvtSceneItemAdded;
        public SceneItemUpdateCallback AppEvtSceneItemRemoved;
        public SceneItemVisibilityChangedCallback AppEvtSceneItemVisibilityChanged;

        private void OnObsSceneItemVisibilityChanged(OBSWebsocket sender, String sceneName, String itemName, Boolean isVisible)
        {
            var key = SceneItemKey.Encode(this.CurrentSceneCollection, sceneName, itemName);
            if (!Helpers.TryExecuteSafe(() => this.allSceneItems[key].Visible = isVisible))
            {
                this.Trace($"WARNING: Cannot update visiblity for item {itemName} scene {sceneName} from dictionary");
            }

            this.AppEvtSceneItemVisibilityChanged?.Invoke(sender, sceneName, itemName, isVisible);
        }

        private void OnObsSceneItemAdded(OBSWebsocket sender, String sceneName, String itemName)
        {
            this.Trace($"OBS: Scene Item {itemName} added to scene {sceneName}");
            //Re-reading current scene
            if (Helpers.TryExecuteFunc(() => { return this.GetCurrentScene(); }, out var currscene))
            {
                //Re=reading current scene to make sure all items are there
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
            if (this.allSceneItems.ContainsKey(key))
            {
                this.allSceneItems.Remove(key);
                this.AppEvtSceneItemRemoved?.Invoke(this, sceneName, itemName);
            }
            else
            {
                this.Trace($"Warning: Cannot find item {itemName} in scene {sceneName}");
            }
        }


        public void AppToggleSceneItemVisibility(String key)
        {
            if( this.IsAppConnected) 
            { 
                if(!Helpers.TryExecuteAction(() =>
                    {
                        var item = this.allSceneItems[key];
                        this.SetSourceRender(item.SourceName, !item.Visible, item.SceneName);
                    }))
                {
                    this.Trace($"Warning: Cannot set visibility to key {key}");
                }
            }
        }

    }
}
