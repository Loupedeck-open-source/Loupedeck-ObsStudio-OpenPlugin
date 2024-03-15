namespace Loupedeck.ObsStudioPlugin
{
    using System;
    using System.Collections.Generic;
    
    using OBSWebsocketDotNet;
    using OBSWebsocketDotNet.Types.Events;

    /// <summary>
    /// Proxy to OBS websocket server, for API reference see
    /// https://github.com/obsproject/obs-websocket/blob/4.x-compat/docs/generated/protocol.md
    /// </summary>
    internal partial class ObsAppProxy
    {
        public event EventHandler<EventArgs> AppEvtSceneCollectionsChanged;

        public event EventHandler<OldNewStringChangeEventArgs> AppEvtCurrentSceneCollectionChanged;
        public List<String> SceneCollections { get; private set; } = new List<String>();
        public String CurrentSceneCollection { get; private set; } = "";

        public void AppSwitchToSceneCollection(String newCollection)
        {
            if (this.IsAppConnected && this.SceneCollections.Contains(newCollection) && this.CurrentSceneCollection != newCollection)
            {
                // NOTE NOTE: After we issue Switch Scene Collection command, there will be lots of events coming from OBS
                // BEFORE we get SceneCollectionChanged event
                //  SINCE OUR INTERNAL DATA STRUCTURES ARE REGENERATED FROM THE LATTER, we temporarily set the 'suspend events' flag

                this.Plugin.Log.Info($"Switching to Scene Collection {newCollection}");

                this.UnsubscribeFromSceneCollectionEvents();

                Helpers.TryExecuteSafe(() => this.SetCurrentSceneCollection(newCollection));
            }
        }

        private void OnObsSceneCollectionChanging(Object sender, CurrentSceneCollectionChangingEventArgs e)
        {
            this.Plugin.Log.Info("OBS OnObsSceneCollectionChanging");
            //Unsubscribing from the scene collection events
            this.UnsubscribeFromSceneCollectionEvents();
            //Unselect currently selected scene
            this.AppEvtCurrentSceneChanged?.Invoke(this, new OldNewStringChangeEventArgs(this.CurrentSceneName, ""));   
        }

        private void OnObsSceneCollectionListChanged(Object sender, SceneCollectionListChangedEventArgs args)
        {
            this.Plugin.Log.Info($"OBS SceneCollectionList changed");
          
            this.SceneCollections = args.SceneCollections;
     
            this.AppEvtSceneCollectionsChanged?.Invoke(sender, args);
        }

        private void OnObsSceneCollectionChanged(Object sender, CurrentSceneCollectionChangedEventArgs e)
        {
            //If sender == null, we came from initialization routine
            try
            {             
                this.Plugin.Log.Info($"OnObsSceneCollectionChanged: Fetching current collection");

                var newSceneCollection = e.SceneCollectionName;
                if (sender == null || newSceneCollection != this.CurrentSceneCollection)
                {
                    var args = new OldNewStringChangeEventArgs(sender == null ? null : this.CurrentSceneCollection, newSceneCollection);
                    this.Plugin.Log.Info($" Current Scene collection changing from {args.Old} to {args.New}");
                    this.CurrentSceneCollection = newSceneCollection;

                    var newSceneCollections = this.GetSceneCollectionList();
                    //Todo: We can probably compare the lists and avoid extra 'onChange' event

                    this.OnObsSceneCollectionListChanged(sender, new SceneCollectionListChangedEventArgs(newSceneCollections));
                    
                    // Regenerating all internal structures
                    this.OnObsSceneListChanged(sender, e);
                    this.AppEvtCurrentSceneCollectionChanged?.Invoke(sender, args);

                    // SEE THE AppSwitchToSceneCollection
                    this.SubscribeToSceneCollectionEvents();
                }
                else
                {
                    this.Plugin.Log.Info($"OBS Current Collection not changed!");
                }
            }
            catch (Exception ex)
            {
                this.Plugin.Log.Error(ex, "Exception retreiving GetCurrentSceneCollection");
                if (ex.InnerException != null)
                {
                    this.Plugin.Log.Error(ex.InnerException, "Inner exception retreiving GetCurrentSceneCollection");
                }
            }
        }

        private Boolean TryFetchSceneItems(Scene scene)
        {
            if (!Helpers.TryExecuteFunc(() => this.GetSceneItemList(scene.Name), out var sceneDetailsList))
            {
                this.Plugin.Log.Warning($"Cannot get SceneList for scene {scene.Name}");
                return false;
            }

            foreach (var item in sceneDetailsList)
            {
                var sourceDictItem = SceneItemDescriptor.CreateSourceDictItem(this.CurrentSceneCollection, scene.Name, item, this);
                if (sourceDictItem != null)
                {
                    var key = SceneItemKey.Encode(this.CurrentSceneCollection, scene.Name, item.ItemId, item.SourceName);
                    this.RetreiveSourceFilters(sourceDictItem);
                    this.AllSceneItems[key] = sourceDictItem;
                }
                else
                {
                    this.Plugin.Log.Warning($"Cannot get CreateSourceDictItem for scene {scene.Name}, item {item.SourceName}");
                }
            }

            return true; 
        }
     
    }
}
