namespace Loupedeck.ObsStudioPlugin
{
    using System;

    internal class SceneItemKey: SceneKey
    {
        public Int32 SourceId;
        public String SourceName;

        //Implement simple copy constructor
        public SceneItemKey(SceneItemKey key) : base(key.Collection, key.Scene)
        {
            this.SourceId = key.SourceId;
            this.SourceName = key.SourceName;
        }

        public SceneItemKey(String coll, String scene, Int32 sourceId, String sourceName = "" )
            : base(coll, scene)
        {
            if (sourceId == -1)
            {
                if (sourceName == "" || !ObsStudioPlugin.Proxy.TryGetSceneItemByName(coll, scene, sourceName, out var item))
                {
                    ObsStudioPlugin.Proxy.Plugin.Log.Error($"SceneItemKey:  Key \"{coll} | {scene} | {sourceId}\": Cannot create key with empty sourceName and sourceId = -1");
                    throw new ArgumentException("Cannot create key with empty sourceName and sourceId = -1");
                }
               
                this.SourceId = item.SourceId;
            }
            else
            {
                this.SourceId = sourceId;
            }

            this.SourceName = (sourceName != String.Empty) ? sourceName: ObsStudioPlugin.Proxy.GetSceneItemNameById(coll, scene, sourceId);
           
        }

        public new String Stringize()
        {
            var a = new String[] { this.Collection, this.Scene, this.SourceId.ToString(),this.SourceName };
            return String.Join(FieldSeparator, a);
        }

        public static Boolean TryParse(String inp, out SceneItemKey key)
        {
            var parts = inp.Split(FieldSeparator, StringSplitOptions.RemoveEmptyEntries);
            var plength = (parts as String[])?.Length;
            key = null;

            if ( (plength < 3) || parts[0].Length == 0 || parts[1].Length == 0 || parts[2].Length == 0)
            {
                ObsStudioPlugin.Proxy.Plugin.Log.Error($"SceneItemKey:  Key \"{inp}\": Need at least 3 fields, got {plength}");
                return false;
            }

            var sceneItemId = -1;
            var sceneItemName = String.Empty;

            if (plength == 4)
            {
                sceneItemName = parts[3];
            }

            if(!Int32.TryParse(parts[2], out sceneItemId) || sceneItemId == -1) 
            {
                //Initial version of OBS plugins were storing sourceName in parts[2] instead of sourceId

                sceneItemName = parts[2];
                foreach (var item in ObsStudioPlugin.Proxy.AllSceneItems)
                {
                    if (item.Value.CollectionName == parts[0] &&
                        item.Value.SceneName == parts[1] &&
                        item.Value.SourceName == parts[2])
                    {
                        sceneItemId = item.Value.SourceId;

                        ObsStudioPlugin.Proxy.Plugin.Log.Info($"SceneItemKey: Legacy item Id \"{parts[2]}\" - Found Item Id = {sceneItemId} ");
                        break;
                    }
                }

                if (sceneItemId == -1)
                {
                    ObsStudioPlugin.Proxy.Plugin.Log.Error($"SceneItemKey:  Key \"{inp}\": Cannot find scene by name {sceneItemName}");
                    return false;
                }
            }

            if (sceneItemName == String.Empty)
            {
                sceneItemName = ObsStudioPlugin.Proxy.GetSceneItemNameById(parts[0], parts[1], sceneItemId);

                if (sceneItemName == String.Empty)
                {
                    //This is true for removed scene items
                    //ObsStudioPlugin.Proxy.Plugin.Log.Error($"SceneItemKey:  Key \"{inp}\": Cannot find scene name by id {sceneItemId}");
                    return false;
                }
            }

            key = new SceneItemKey(parts[0], parts[1], sceneItemId, sceneItemName);
            return key != null;
        }

        public static String Encode(String coll, String scene, Int32 sourceId, String sceneItemName = "") => new SceneItemKey(coll, scene, sourceId, sceneItemName).Stringize();
    }
}
