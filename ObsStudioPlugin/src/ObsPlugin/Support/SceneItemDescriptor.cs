namespace Loupedeck.ObsStudioPlugin
{
    using System;

    internal class SceneItemDescriptor
    {
        public String CollectionName;
        public String SceneName;

        public String SourceName { get; private set; }

        //It's not "private set" because the visibility will be triggered by an event handler
        public Boolean Visible { get; set; } 

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
        public static SceneItemDescriptor CreateSourceDictItem(String in_collection, String in_sceneName, LDSceneItem in_sceneItem, OBSWebsocketDotNet.OBSWebsocket obs, OBSWebsocketDotNet.Types.SceneItemDetails in_details = null)
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

                var source = new SceneItemDescriptor(in_collection, in_sceneName, 
                                        details != null ? details.SourceName : "Invalid_Source_Name", 
                                        props != null && props.Visible);

                return source;
            }
            catch (Exception ex)
            {
                Tracer.Error($"Warning: Exception: {ex.Message} in creating source item for item '{in_sceneItem.SourceName}' of scene '{in_sceneName}'");
            }

            return null;
        }

        protected SceneItemDescriptor(String coll, String scene, String sourceName, Boolean visible)
        {
            this.CollectionName = coll;
            this.SceneName = scene;
            this.SourceName = sourceName;
            this.Visible = visible; 
        }
    }


}
