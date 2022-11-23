namespace Loupedeck.ObsStudioPlugin
{
    using System;
    using System.Collections.Generic;
    using OBSWebsocketDotNet.Types;

    /// <summary>
    /// Proxy to OBS websocket server, for API reference see
    /// https://github.com/obsproject/obs-websocket/blob/4.x-compat/docs/generated/protocol.md
    /// </summary>
    public partial class ObsAppProxy
    {
        // Retreives all scene items for all scenes in current collection
        private void OnObsSceneCollectionChange_FetchSceneItems()
        {
            this.AllSceneItems.Clear();

            ObsStudioPlugin.Trace("Adding scene items");

            // sources
            foreach (var scene in this.Scenes)
            {
                if (!Helpers.TryExecuteFunc(() => this.GetSceneItemList(scene.Name), out var sceneDetailsList))
                {
                    ObsStudioPlugin.Trace($"Warning: Cannot get SceneList for scene {scene.Name}");
                    continue;
                }

                foreach (var item in sceneDetailsList)
                {
                    var sceneItem = scene.Items.Find(x => x.SourceName == item.SourceName);
                    if (sceneItem != null)
                    {
                        var sourceDictItem = SceneItemDescriptor.CreateSourceDictItem(this.CurrentSceneCollection, scene.Name, sceneItem, this,item);
                        if (sourceDictItem != null)
                        {
                            var key = SceneItemKey.Encode(this.CurrentSceneCollection, scene.Name, item.SourceName); 
                            this.AllSceneItems.Add(key, sourceDictItem);
                        }
                        else
                        {
                            ObsStudioPlugin.Trace($"Warning: Cannot get CreateSourceDictItem for scene {scene.Name}, item {sceneItem.SourceName}");
                        }
                    }
                    else
                    {
                        ObsStudioPlugin.Trace($"Warning: Cannot get SceneItemList for scene {scene.Name}");
                    }
                }
            }
        }

        /// <summary>
        /// Our own dictionary of scene items of all scenes in current collection, with all properties
        /// Note: Scene item is an instance of the source in particular scene.  Most of the source
        /// properties are shared among scenes with just a few (like visibility) being scene-specif
        ///
        /// </summary>
         // 'Main' dictionary, with Scene-Item ID being a key
        // Dictionary
        public Dictionary<String, SceneItemDescriptor> AllSceneItems = new Dictionary<String, SceneItemDescriptor>();

        public class SceneItemDescriptor
        {
            public String CollectionName;

            public String SceneName;

            public String SceneItemName => this._sceneItemProps != null ? this._sceneItemProps.ItemName : "Invalid_Item_Name";

            public String SourceName => this._sceneItemDetails != null ? this._sceneItemDetails.SourceName : "Invalid_Source_Name";

            public Boolean Visible { get => this._sceneItemProps.Visible; set => this._sceneItemProps.Visible = value; }

            // private readonly volumeinfo;
            // private readonly OBSWebsocketDotNet.Types.SceneItem _sceneItem;
            private readonly SceneItemDetails _sceneItemDetails;
            private readonly SceneItemProperties _sceneItemProps;

            /// <summary>
            /// Creates a single Source Dictionary item, optionally feching SceneItemProperties  and SceneItemDetails
            /// </summary>
            /// <param name="in_collection">Collection</param>
            /// <param name="in_sceneName">SceneName</param>
            /// <param name="in_sceneItem">SceneItem descriptor</param>
            /// <param name="obs">OBS Websocket</param>
            /// <param name="in_props">properties</param>
            /// <param name="in_details">details</param>
            /// <returns></returns>
            public static SceneItemDescriptor CreateSourceDictItem(String in_collection, String in_sceneName, SceneItem in_sceneItem, OBSWebsocketDotNet.OBSWebsocket obs, SceneItemDetails in_details = null)
            {
                try
                {
                    var props = obs.GetSceneItemProperties(in_sceneItem.SourceName, in_sceneName);
                    
                    var details = in_details;

                    if (details == null)
                    {
                        //Getting information about all scene' sources from OBS and selecting ours
                        var sceneItemList = obs.GetSceneItemList(in_sceneName);
                        
                        foreach (var sceneItem in sceneItemList)
                        {
                            if (sceneItem.SourceName == in_sceneItem.SourceName)
                            {
                                details = sceneItem;
                                break;
                            }
                        }
                    }

                    if (details == null)
                    {
                        throw new Exception("Cannot find details for source");
                    }

                    var source = new SceneItemDescriptor(in_collection, in_sceneName, in_sceneItem, details, props);

                    return source;
                }
                catch (Exception ex)
                {
                    ObsStudioPlugin.Trace($"Warning: Exception: {ex.Message} in creating source item for item '{in_sceneItem.SourceName}' of scene '{in_sceneName}'");
                }

                return null;
            }

            protected SceneItemDescriptor(String coll, String scene, SceneItem item, SceneItemDetails details, SceneItemProperties props)
            {
                this.CollectionName = coll;
                this.SceneName = scene;
                this._sceneItemDetails = details;
                this._sceneItemProps = props;
            }
        }
    }
}


/// Stripped version of all Scene Item-related classes of OBS
////
#if FALSE
    public class SceneItem
    {
        public string SourceName;
        public string InternalType;
        public float AudioVolume;
        public float XPos;
        public float YPos;
        public int SourceWidth;
        public int SourceHeight;
        public float Width;
        public float Height;
        public bool Locked { set; get; }
        public bool Render { set; get; }
        public int ID { set; get; }
        public string ParentGroupName { set; get; }
        public List<SceneItem> GroupChildren { set; get; }
}

   public class SceneItemProperties
    {
        public SceneItemCropInfo Crop { set; get; }
        public SceneItemBoundsInfo Bounds { set; get; }
        public SceneItemPointInfo Scale { set; get; }
        public SceneItemPositionInfo Position { set; get; }
        public string ItemName { set; get; }
        public string Item { set; get; }
        public double Height { set; get; }
        public double Width { set; get; }
        public bool Locked { set; get; }
        public bool Visible { set; get; }
        public int SourceHeight { set; get; }
        public int SourceWidth { set; get; }
        public double Rotation { set; get; }

    }

 public class SceneItemDetails
    {
        public int ItemId { set; get; }
        public string SourceKind { set; get; }
        public string SourceName { set; get; }
        public SceneItemSourceType SourceType { set; get; }
    }

#endif

