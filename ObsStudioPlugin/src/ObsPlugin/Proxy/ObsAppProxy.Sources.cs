namespace Loupedeck.ObsStudioPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Web.UI.WebControls;

    using OBSWebsocketDotNet;
    using OBSWebsocketDotNet.Types;
    using OBSWebsocketDotNet.Types.Events;

    /// <summary>
    /// Proxy to OBS websocket server, for API reference see
    /// https://github.com/obsproject/obs-websocket/blob/4.x-compat/docs/generated/protocol.md
    /// </summary>
    internal partial class ObsAppProxy
    {
        public event EventHandler<SceneItemArgs> AppEvtSceneItemAdded;
        public event EventHandler<SceneItemArgs> AppEvtSceneItemRemoved;
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

        public String GetSceneItemName(String collection, String scene, Int32 itemId)
        {
            var key = SceneItemKey.Encode(collection, scene, itemId);
            return this.AllSceneItems.ContainsKey(key) ? this.AllSceneItems[key].SourceName : "";
        }

        ///     <summary>
        ///  Controls scene item visibilty. 
        /// </summary>
        /// <param name="key">Key for scene item</param>
        /// <param name="forceState">if True, force specific state to the scene item, otherwise toggle. </param>
        /// <param name="newState">state to force</param>
        /// <param name="applyToAllScenes">Apply to all scenes in collection where this source exists</param>
        public void AppSceneItemVisibilityToggle(String key, Boolean forceState = false, Boolean newState = false, Boolean applyToAllScenes = false)
        {
            //this.Plugin.Log.Info($"AppSceneItemVisibilityToggle: Key {key} applyToAllScenes {applyToAllScenes}");

            if (this.IsAppConnected && this.AllSceneItems.ContainsKey(key))
            {
                try
                {
                    if (!SceneItemKey.TryParse(key, out var parsedkey))
                    {
                        throw new ArgumentException($"Cannot parse a key \"{key}\"");
                    }

                    var items = new List<SceneItemDescriptor>();
                    var originalItem = this.AllSceneItems[key];

                    items.Add(this.AllSceneItems[key]);

                    if (applyToAllScenes)
                    {
                        //We go thru all scenes and add only those items where source with the same name is present
                        foreach (var newkey in this.AllSceneItems.Keys)
                        {
                            if ( newkey != key
                              && (this.AllSceneItems[newkey].CollectionName == originalItem.CollectionName)
                              && (this.AllSceneItems[newkey].SourceName == originalItem.SourceName)
                            )
                            {
                                items.Add(this.AllSceneItems[newkey]);
                            }   
                        }
                    }

                    foreach (var item in items)
                    {
                        //this.Plugin.Log.Info($"AppSceneItemVisibilityToggle: settings vis {newState} to {item.SceneName}");
                        this.SetSceneItemEnabled(item.SceneName, item.SourceId, forceState ? newState : !item.Visible);
                    }
                }
                catch (Exception ex)
                {
                    this.Plugin.Log.Error($"Exception {ex.Message} when toggling visibility to {key}");
                }
            }
        }
        
        private void OnObsSceneItemVisibilityChanged(Object sender, SceneItemEnableStateChangedEventArgs arg)
        {

            var key = SceneItemKey.Encode(this.CurrentSceneCollection, arg.SceneName, arg.SceneItemId);

            if (this.AllSceneItems.ContainsKey(key))
            {
                this.AllSceneItems[key].Visible = arg.SceneItemEnabled;
            }
            else
            {
                this.Plugin.Log.Warning($"Cannot update visiblity: item {arg.SceneItemId} scene {arg.SceneName} not in dictionary");
            }

            this.AppEvtSceneItemVisibilityChanged?.Invoke(sender, new SceneItemVisibilityChangedArgs(arg.SceneName,"", arg.SceneItemId, arg.SceneItemEnabled));
        }

        private void OnObsSceneItemAdded(Object sender, SceneItemCreatedEventArgs args)
        {

            this.Plugin.Log.Info($"OBS: OnObsSceneItemAdded: Item '{args.SourceName}' scene '{args.SceneName}' ItemId {args.SceneItemId} ");

            if (args.SceneName != this.CurrentSceneName)
            {
                this.Plugin.Log.Warning($"OnObsSceneItemAdded received non-current scene '{args.SceneName}'. Current is '{this.CurrentSceneName}'. Ignoring");
                return;
            }
// "Check if in new API we get sceneItemAdded for non-current"
#if false

            // Re-reading current scene, since this.CurrentSceneName does not contain the item
            if (!Helpers.TryExecuteFunc(() => this.GetCurrentProgramScene(), out var obsCurrentScene))
            {
                this.Plugin.Log.Warning($"Cannot get current scene from OBS");
                return;
            }

            if (obsCurrentScene != this.CurrentSceneName)
            {
                this.Plugin.Log.Warning($"Current scene changed to '{obsCurrentScene}' mid-way.");
                return;
            }

            this.CurrentSceneName = new Scene(obsCurrentScene);
#endif

            //NOTE: args.sceneItemIndex is an array index of 
            //Creating item and fetching all missing data for it from OBS
            //FIXME: The only detail fetched is 'visibiility' -> If we can validate that all items are 'visible' upon creation we can skip it
            //and create the descriptor manually

            var sourceDictItem = SceneItemDescriptor.CreateSourceDictItem( this.CurrentSceneCollection, args.SceneName, new SceneItemDetails() { SourceName = args.SourceName, ItemId = args.SceneItemId }, this, true);

            //Note, this should never happen because we don't fecth visibility for new items
            if (sourceDictItem == null)
            {
                this.Plugin.Log.Warning($"Cannot get properties for item '{args.SourceName}'.");
                return;
            }

            var key = SceneItemKey.Encode(this.CurrentSceneCollection, args.SceneName, args.SceneItemId); 

            this.AllSceneItems[key] = sourceDictItem;

            this.AppEvtSceneItemAdded?.Invoke(this, new SceneItemArgs(args.SceneName, args.SourceName, args.SceneItemId));

        }

        private void OnObsSceneItemRemoved(Object sender, SceneItemRemovedEventArgs args)
        {
            
            this.Plugin.Log.Info($"OBS: Scene Item {args.SourceName} removed from scene {args.SceneName}");

            var key = SceneItemKey.Encode(this.CurrentSceneCollection, args.SceneName, args.SceneItemId);
            if (this.AllSceneItems.ContainsKey(key))
            {
                this.AllSceneItems.Remove(key);

                this.AppEvtSceneItemRemoved?.Invoke(this, new SceneItemArgs(args.SceneName, args.SourceName, args.SceneItemId));
            }
            else
            {
                this.Plugin.Log.Warning($"Cannot find item {args.SourceName} in scene {args.SceneName}");
            }
        }
    }
}
