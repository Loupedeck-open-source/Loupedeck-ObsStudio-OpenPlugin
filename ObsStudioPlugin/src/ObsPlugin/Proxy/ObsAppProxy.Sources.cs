namespace Loupedeck.ObsStudioPlugin
{
    using System;
    using System.Collections.Generic;
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
        public event EventHandler<OldNewStringChangeEventArgs> AppSceneItemRenamed;
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

        public String GetSceneItemNameById(String collection, String scene, Int32 itemId)
        {
            foreach (var item in ObsStudioPlugin.Proxy.AllSceneItems)
            {
                if (item.Value.CollectionName == collection &&
                    item.Value.SceneName == scene &&
                    item.Value.SourceId == itemId)
                {
                    return item.Value.SourceName;
                    
                }
            }
            return String.Empty;
        }

        public Boolean TryGetSceneItemByName(String collection, String scene, String sourceName, out SceneItemDescriptor descriptor)
        {
            //Search in AllSceneItems
            foreach (var item in ObsStudioPlugin.Proxy.AllSceneItems)
            {
                if (item.Value.CollectionName == this.CurrentSceneCollection &&
                    item.Value.SceneName == this.CurrentSceneName &&
                    item.Value.SourceName == sourceName)
                {
                    descriptor = item.Value;
                    return true;
                }
            }
            descriptor = null;
            return false;
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
        

        public void TransformSoure(String key, String action){
            if (this.IsAppConnected && this.AllSceneItems.ContainsKey(key)) {

                var originalItem = this.AllSceneItems[key];
                var currentTransform = this.GetSceneItemTransform(originalItem.SceneName, originalItem.SourceId);
                currentTransform.BoundsWidth = currentTransform.BoundsWidth >= 1 ? currentTransform.BoundsWidth : 1;
                currentTransform.BoundsHeight = currentTransform.BoundsHeight >= 1 ? currentTransform.BoundsHeight : 1;
                
                this.Plugin.Log.Info($"the old transform. boundWidth:{currentTransform.BoundsWidth}, boundType:{currentTransform.BoundsType}, alignment: {currentTransform.Alignnment}");
                try
                {
                    var zoomFactor = 5;  // Equivalent to zoom_factor in Python
                    var moveDistance = 5;  // Equivalent to move_distance in Python
                    
                    var originalScaleX = currentTransform.ScaleX;
                    var originalScaleY = currentTransform.ScaleY;
                    var posX = currentTransform.X;
                    var posY = currentTransform.Y;

                    // Adjust position or scale based on mode1600
                    switch (action.ToLower())
                    {
                        case "up":
                            posY -= moveDistance;
                            break;
                        case "down":
                            posY += moveDistance;
                            break;
                        case "left":
                            posX -= moveDistance;
                            break;
                        case "right":
                            posX += moveDistance;
                            break;
                        case "zoom_in":
                        case "zoom_out":
                            var newScaleX = action == "zoom_in" ? 
                                originalScaleX + zoomFactor : originalScaleX - zoomFactor;
                                
                            var newScaleY = action == "zoom_in" ? 
                                originalScaleY + zoomFactor : originalScaleY - zoomFactor;
                                

                            // Calculate position shift to keep zoom centered
                            var sourceWidth = currentTransform.Width;
                            var sourceHeight = currentTransform.Height;
                            
                            var deltaX = sourceWidth * (newScaleX - originalScaleX) / 2;
                            var deltaY = sourceHeight * (newScaleY - originalScaleY) / 2;

                            posX -= deltaX;
                            posY -= deltaY;

                            currentTransform.ScaleX = newScaleX;
                            currentTransform.ScaleY = newScaleY;
                            break;
                    }

                    currentTransform.X = posX;
                    currentTransform.Y = posY;
                    this.Plugin.Log.Info($"the new transform. boundWidth:{currentTransform.BoundsWidth}, boundType:{currentTransform.BoundsType}, alignment: {currentTransform.Alignnment}");
                    this.Plugin.Log.Info($"New position of '{originalItem.SourceName}': x={posX}, y={posY}, scale_x={currentTransform.ScaleX}, scale_y={currentTransform.ScaleY}");
                   this.SetSceneItemTransform(originalItem.SceneName, originalItem.SourceId, currentTransform);

                }
                catch (Exception ex)
                {
                    this.Plugin.Log.Error($"Exception {ex.Message} when adjusting source {key}");
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

            var key = SceneItemKey.Encode(this.CurrentSceneCollection, args.SceneName, args.SceneItemId,args.SourceName);
            this.AllSceneItems.Add(key, sourceDictItem);
            //this.Plugin.Log.Info($"OnObsSceneItemAdded: Adding item key: {key}");
           // NB: We assume that filters are added explicitly this.RetreiveSourceFilters(this.AllSceneItems[key]);
            this.AppEvtSceneItemAdded?.Invoke(this, new SceneItemArgs(args.SceneName, args.SourceName, args.SceneItemId));
        }

        private void OnObsSceneItemRemoved(Object sender, SceneItemRemovedEventArgs args)
        {
            this.Plugin.Log.Info($"OBS: Scene Item {args.SourceName} removed from scene {args.SceneName}");

            var key = SceneItemKey.Encode(this.CurrentSceneCollection, args.SceneName, args.SceneItemId, args.SourceName);
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

        private void OnObsSourceNameChanged(Object _, InputNameChangedEventArgs e)
        {
            var audioInputRenamed = false;
            var sourceRenamed = false;
            //this.Plugin.Log.Info($"Entering {MethodBase.GetCurrentMethod().Name}. {e.OldInputName} -> {e.InputName}");
            if (this.CurrentAudioSources.ContainsKey(e.OldInputName))
            {
                var source = this.CurrentAudioSources[e.OldInputName];
                this.CurrentAudioSources.Remove(e.OldInputName);
                this.CurrentAudioSources.Add(e.InputName, source);
                audioInputRenamed = true;
            }

            //Assumption: Source is unique in the AllSceneItems
            foreach (var key in this.AllSceneItems.Keys)
            {
                if (this.AllSceneItems[key].SourceName == e.OldInputName)
                {
                    var dtor = this.AllSceneItems[key];
                    dtor.SourceName = e.InputName;
                    var newKey = SceneItemKey.Encode(dtor.CollectionName, dtor.SceneName, dtor.SourceId, e.InputName);
                    this.AllSceneItems.Add(newKey, dtor);
                    this.AllSceneItems.Remove(key);
                    sourceRenamed = true;
                    break;
                }
            }

            if(audioInputRenamed)
            {
                this.AppInputRenamed?.Invoke(this, new OldNewStringChangeEventArgs(e.OldInputName, e.InputName));
            }

            if(sourceRenamed)
            {
                this.AppSceneItemRenamed?.Invoke(this, new OldNewStringChangeEventArgs(e.OldInputName, e.InputName));
            }

        }
    }
}
