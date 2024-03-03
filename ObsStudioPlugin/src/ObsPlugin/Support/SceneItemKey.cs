namespace Loupedeck.ObsStudioPlugin
{
    using System;

    internal class SceneItemKey: SceneKey
    {
        public Int32 SourceId;

        public SceneItemKey(String coll, String scene, Int32 sourceId)
            : base(coll, scene) => this.SourceId = sourceId;

        public String Stringize()
        {
            var a = new String[] { this.Collection, this.Scene, this.SourceId.ToString() };
            return String.Join(FieldSeparator, a);
        }

        public static Boolean TryParse(String inp, out SceneItemKey key)
        {
            var parts = inp.Split(FieldSeparator, StringSplitOptions.RemoveEmptyEntries);
            key = null; 
            if ((parts as String[])?.Length == 3)
            {
                var sceneItemId = -1;
                if (!Int32.TryParse(parts[2], out sceneItemId) && parts[2].Length > 0)
                {

                    //Old version of OBS Plugin has Source Name as a 3rd parameter, so we can try to find the source by name in current sources and get it's id
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
                    
                }

                if (sceneItemId != -1)
                {
                    key = new SceneItemKey(parts[0], parts[1], sceneItemId);
                }
                else
                {
                    ObsStudioPlugin.Proxy.Plugin.Log.Warning($"SceneItemKey: Legacy item Id \"{parts[2]}\" - Item Id not found");
                }

            }
            return key != null;
        }

        public static String Encode(String coll, String scene, Int32 sourceId) => new SceneItemKey(coll, scene, sourceId).Stringize();
    }

}
