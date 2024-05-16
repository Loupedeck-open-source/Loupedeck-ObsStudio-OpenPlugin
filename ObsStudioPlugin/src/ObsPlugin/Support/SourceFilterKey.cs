namespace Loupedeck.ObsStudioPlugin
{
    using System;

    internal class SourceFilterKey : SceneItemKey
    {
        private const String FilterFieldSeparator = "|~|~|";
        public String FilterName;

        public SourceFilterKey(SceneItemKey key, String filterName) : base(key) => this.FilterName = filterName;

        public SourceFilterKey(String coll, String scene, Int32 sourceId, String sourceName, String filterName) :
            base(coll, scene, sourceId, sourceName) => this.FilterName = filterName;

        public String StringizeAsItemKey() => base.Stringize();

        new public String Stringize()
        {
            var a = new String[] { base.Stringize(), this.FilterName };
            return String.Join(FilterFieldSeparator, a);
        }

        public static Boolean TryParse(String inp, out SourceFilterKey key)
        {
            var parts = inp.Split(FilterFieldSeparator, StringSplitOptions.RemoveEmptyEntries);
            var plength = (parts as String[])?.Length;
            key = null;

            if (plength < 2)
            {
                ObsStudioPlugin.Proxy.Plugin.Log.Error($"SourceFilterKey:  Key \"{inp}\": Need at least 2 fields, got {plength}");
                return false;
            }

            if (!SceneItemKey.TryParse(parts[0], out var sceneItemKey))
            {
                ObsStudioPlugin.Proxy.Plugin.Log.Error($"SourceFilterKey:  Key \"{inp}\": Cannot parse SceneItemKey");
                return false;
            }

            key = new SourceFilterKey(sceneItemKey, parts[1]);
            return key != null;
        }
        public static String Encode(String coll, String scene, Int32 sourceId, String sceneItemName, String filterName) => 
                new SourceFilterKey(coll, scene, sourceId, sceneItemName, filterName).Stringize();

    }

}
