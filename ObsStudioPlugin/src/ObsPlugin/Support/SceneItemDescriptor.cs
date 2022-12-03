namespace Loupedeck.ObsStudioPlugin
{
    using System;

    using OBSWebsocketDotNet.Types;

    internal class SceneItemDescriptor
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
                Tracer.Error($"Warning: Exception: {ex.Message} in creating source item for item '{in_sceneItem.SourceName}' of scene '{in_sceneName}'");
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
