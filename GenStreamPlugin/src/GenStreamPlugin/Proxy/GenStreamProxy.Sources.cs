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

        //Dictionary 
         // Collection-Scene-Source key - boolean visibility
         // Fetching initial during startup

        void OnObsSceneItemVisibilityChanged(OBSWebsocket sender, String sceneName, String itemName, Boolean isVisible)
        {
            var key = SceneItemKey.Encode(this.CurrentSceneCollection, sceneName, itemName);
            if (!Helpers.TryExecuteSafe(() => this.currentSources[key].Visible = isVisible))
            {
                this.Trace($"WARNING: Cannot update visiblity for item {itemName} scene {sceneName} from dictionary");
            }

            this.AppEvtSceneItemVisibilityChanged?.Invoke(sender, sceneName, itemName, isVisible);
        }

        private Boolean AddCurrentSourceItem(String sceneName, OBSWebsocketDotNet.Types.SceneItemDetails item)
        {

            if (Helpers.TryExecuteFunc(() => { return this.GetSceneItemProperties(item.SourceName, sceneName); }, out var props))
            {
                if (!Helpers.TryExecuteAction(() =>
                {
                    this.currentSources.Add(SceneItemKey.Encode(this.CurrentSceneCollection, sceneName, item.SourceName),
                                                  new SourceDictItem(this.CurrentSceneCollection, sceneName, item, props));
                }))
                {
                    this.Trace($"Cannot add item {item.SourceName} of scene {sceneName}");
                    return false;
                }
            }
            else
            {
                this.Trace($"Cannot get properties for item {item.SourceName} of scene {sceneName}");
                return false;
            }

            return true;
        }

        private void OnObsSceneItemAdded(OBSWebsocket sender, String sceneName, String itemName)
        {
            //FIXME: Make sure all data structures are updated
            this.AppEvtSceneItemAdded?.Invoke(this, sceneName, itemName);
        }

        void OnObsSceneItemRemoved(OBSWebsocket sender, String sceneName, String itemName)
        {
            //FIXME: Make sure all data structures are updated
            this.AppEvtSceneItemRemoved?.Invoke(this, sceneName, itemName);

        }

        //'Main' dictionary, with Scene-Item ID being a key
        public Dictionary<String, SourceDictItem> currentSources;
        //Retreives all items for current collection
        private void RetreiveAllSceneItemProps()
        {
            this.currentSources.Clear();

            this.Trace("Adding sources");

            //sources 
            foreach (var scene in this.Scenes)
            {
                if (Helpers.TryExecuteFunc(() => { return this.GetSceneItemList(scene.Name); }, out var items))
                {
                    foreach (var item in items)
                    {
                        this.AddCurrentSourceItem(scene.Name, item);
                        this.Trace($"adding sourceName {item.SourceName} to scene {scene.Name}");
                    }
                }
                else
                {
                    this.Trace($"Warning: Cannot get SceneItemList for scene {scene.Name}");
                }
            }
        }
        public void ToggleSourceVisiblity(String sourceName)
        {
            var item = this.currentSources[sourceName];

            if( this.IsAppConnected )
            { 
                if(!Helpers.TryExecuteAction(() =>
                    {
                        this.SetSourceRender(item.SourceName, !item.Visible, item.SceneName);
                    }))
                {
                    this.Trace($"Warning: Cannot set visibility to source {sourceName}");
                }
            }
        }

    }
}
