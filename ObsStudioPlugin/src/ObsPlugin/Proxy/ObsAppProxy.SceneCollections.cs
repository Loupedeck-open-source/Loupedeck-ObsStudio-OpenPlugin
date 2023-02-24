namespace Loupedeck.ObsStudioPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Web.UI.WebControls;

    using OBSWebsocketDotNet;

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

        //While AllSceneItems is a flat list indexed y string key, this one is the list of lists for scene, 
        //To be used in the SourceVisibility processing 
        public Dictionary<String, List<Tuple<String, String>>> ScenesWithItems { get; private set; } = new Dictionary<String, List<Tuple<String, String>>>();
        private void OnObsSceneCollectionListChanged(Object sender, EventArgs args)
        {
            this.Plugin.Log.Info("OBS SceneCollectionList changed");

            if (Helpers.TryExecuteSafe(() => this.SceneCollections = this.ListSceneCollections()))
            {
                this.Plugin.Log.Info($"Retreived list of {this.SceneCollections.Count} collections");

                this.AppEvtSceneCollectionsChanged?.Invoke(sender, args);
            }
            else
            {
                this.Plugin.Log.Warning($"Cannot handle SceneCollectionList change");
            }
        }

        private void OnObsSceneCollectionChanged(Object sender, EventArgs e)
        {
            //If sender == null, we came from initialization routine
            try
            {
                this.Plugin.Log.Info($"OnObsSceneCollectionChanged: Fetching current collection");
                var newSceneCollection = this.GetCurrentSceneCollection();
                if (sender == null || newSceneCollection != this.CurrentSceneCollection)
                {
                    var args = new OldNewStringChangeEventArgs(sender == null ? null : this.CurrentSceneCollection, newSceneCollection);
                    this.Plugin.Log.Info($"OBS Current Scene collection changing from {args.Old} to {args.New}");
                    this.CurrentSceneCollection = newSceneCollection;

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

        // Retreives all scene items for all scenes in current collection
        private void OnObsSceneCollectionChange_FetchSceneItems()
        {
            this.AllSceneItems.Clear();
            this.ScenesWithItems.Clear();

            this.Plugin.Log.Info("Adding scene items");

            // sources
            foreach (var scene in this.Scenes)
            {
                if (!Helpers.TryExecuteFunc(() => this.GetSceneItemList(scene.Name), out var sceneDetailsList))
                {
                    this.Plugin.Log.Warning($"Cannot get SceneList for scene {scene.Name}");
                    continue;
                }

                var itemsList = new List<Tuple<String,String>>();

                foreach (var item in sceneDetailsList)
                {
                    var sceneItem = scene?.Items?.Find(x => x.SourceName == item.SourceName) ?? null;
                    if (sceneItem != null)
                    {
                        if(Helpers.TryExecuteFunc(()=> { return SceneItemDescriptor.CreateSourceDictItem(this.CurrentSceneCollection, scene.Name, sceneItem, this, item); }, out var sourceDictItem) 
                            && sourceDictItem != null)
                        {
                            var key = SceneItemKey.Encode(this.CurrentSceneCollection, scene.Name, item.SourceName);
                            this.AllSceneItems[key] = sourceDictItem;
                            itemsList.Add(Tuple.Create(key, item.SourceName));
                        }
                        else
                        {
                            this.Plugin.Log.Warning($"Cannot get CreateSourceDictItem for scene {scene.Name}, item {sceneItem.SourceName}");
                        }
                    }
                    else
                    {
                        this.Plugin.Log.Warning($"Cannot get SceneItemList for scene {scene.Name}");
                    }
                }

                if(itemsList.Count > 0)
                {
                    this.ScenesWithItems.Add(scene.Name, itemsList);
                }

            }
        }
    }
}
