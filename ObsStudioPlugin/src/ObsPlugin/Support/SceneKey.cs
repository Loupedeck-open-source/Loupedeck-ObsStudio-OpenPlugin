namespace Loupedeck.ObsStudioPlugin
{
    using System;

    internal class SceneKey
    {
        public const String FieldSeparator = "||~~(%)~~||";
        public const String NothingField = "NONE";
        public String Scene;

        // Synonym, since sources are used collection-wide
        public String Source => this.Scene;

        public String Collection;

        public SceneKey(String collection, String scene)
        {
            this.Scene = scene ?? NothingField;
            this.Collection = collection ?? NothingField;
        }

        private String Stringize()
        {
            var a = new String[] { this.Collection, this.Scene };
            return String.Join(FieldSeparator, a);
        }

        public static Boolean TryParse(String inp, out SceneKey key)
        {
            key = SceneKey.FromString(inp);
            return key != null;
        }

        public static SceneKey FromString(String inp)
        {
            if (Helpers.TryExecuteFunc(
                () =>
                {
                    var parts = inp.Split(FieldSeparator, StringSplitOptions.RemoveEmptyEntries);
                    return (parts as String[])?.Length == 2 ? new SceneKey(parts[0], parts[1]) : null;
                }, out var x))
            {
                return x;
            }
            Tracer.Error($"Cannot decode string param {inp}");
            return null;
        }

        public static String Encode(String coll, String scene) => new SceneKey(coll, scene).Stringize();
    }

}
