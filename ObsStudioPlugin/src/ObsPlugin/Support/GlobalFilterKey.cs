namespace Loupedeck.ObsStudioPlugin
{
    using System;


    //Key consists of SceneKey + FilterName
    internal class GlobalFilterKey : SceneKey
    {
        private const String FilterFieldSeparator = "|~|~|";
        public String FilterName;

        public GlobalFilterKey(SceneKey key, String filterName) : base(key.Collection, key.Scene) => this.FilterName = filterName;

        public GlobalFilterKey(String coll, String sourceName, String filterName) :
            base(coll, sourceName) => this.FilterName = filterName;

        public String StringizeAsItemKey() => base.Stringize();

        new public String Stringize()
        {
            var a = new String[] { base.Stringize(), this.FilterName };
            return String.Join(FilterFieldSeparator, a);
        }

        public static Boolean TryParse(String inp, out GlobalFilterKey key)
        {
            var parts = inp.Split(FilterFieldSeparator, StringSplitOptions.RemoveEmptyEntries);
            var plength = (parts as String[])?.Length;
            key = null;

            if (plength < 2)
            {
                ObsStudioPlugin.Proxy.Plugin.Log.Error($"GlobalFilterKey:  Key \"{inp}\": Need at least 2 fields, got {plength}");
                return false;
            }

            if (!SceneKey.TryParse(parts[0], out var sceneKey))
            {
                ObsStudioPlugin.Proxy.Plugin.Log.Error($"GlobalFilterKey:  Key \"{inp}\": Cannot parse SceneKey");
                return false;
            }

            key = new GlobalFilterKey(sceneKey, parts[1]);
            return key != null;
        }
        public static String Encode(String coll, String scene, String filterName) => 
                new GlobalFilterKey(coll, scene, filterName).Stringize();

    }

}
