namespace Loupedeck.ObsStudioPlugin
{
    using System;
    using System.Net.NetworkInformation;

    internal class SceneItemDescriptor
    {
        public String CollectionName;
        public String SceneName;

        public String SourceName { get; private set; }
        
        public Int32 SourceId { get; private set; }

        //It's not "private set" because the visibility will be triggered by an event handler
        public Boolean Visible { get; set; }

        /// <summary>
        /// Creates a single Source Dictionary item, optionally feching SceneItemProperties  and SceneItemDetails
        /// </summary>
        /// <param name="in_collection">Collection</param>
        /// <param name="in_sceneName">SceneName</param>
        /// <param name="in_details">SceneItem details</param>
        /// <param name="obs">OBS Websocket</param>
        /// <param name="visibleDefault">If set to true, we set visibility to 'On' without querying OBS</param>
        /// <returns></returns>
        public static SceneItemDescriptor CreateSourceDictItem(String in_collection, String in_sceneName, OBSWebsocketDotNet.Types.SceneItemDetails in_details, OBSWebsocketDotNet.OBSWebsocket obs, Boolean visibleDefault = false)
        {
            try
            {
                var isVisible = visibleDefault  ? true : obs.GetSceneItemEnabled(in_sceneName, in_details.ItemId);

                var source = new SceneItemDescriptor(in_collection, in_sceneName,
                                       in_details.SourceName, in_details.ItemId,
                                        isVisible);

                return source;
            }
            catch (Exception ex)
            {
                Tracer.Error($"Warning: Exception: {ex.Message} in creating source item for item '{in_details.SourceName}' of scene '{in_sceneName}'");
            }

            return null;
        }

        protected SceneItemDescriptor(String coll, String scene, String sourceName, Int32 sourceId, Boolean visible)
        {
            this.CollectionName = coll;
            this.SceneName = scene;
            this.SourceName = sourceName;
            this.SourceId = sourceId;
            this.Visible = visible; 
        }
    }


}
