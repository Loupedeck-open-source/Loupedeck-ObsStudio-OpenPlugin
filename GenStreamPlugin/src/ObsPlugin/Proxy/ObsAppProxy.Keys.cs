namespace Loupedeck.ObsPlugin
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

    internal class SceneItemKey : SceneKey
    {
        public new String Source;

        public SceneItemKey(String coll, String scene, String source)
            : base(coll, scene) => this.Source = source ?? NothingField;

        public String Stringize()
        {
            var a = new String[] { this.Collection, this.Scene, this.Source };
            return String.Join(FieldSeparator, a);
        }

        public static Boolean TryParse(String inp, out SceneItemKey key)
        {
            key = SceneItemKey.FromString(inp);
            return key != null;
        }

        public new static SceneItemKey FromString(String inp)
        {
            if (Helpers.TryExecuteFunc(
                () =>
            {
                var parts = inp.Split(FieldSeparator, StringSplitOptions.RemoveEmptyEntries);
                return (parts as String[])?.Length == 3 ? new SceneItemKey(parts[0], parts[1], parts[2]) : null;
            }, out var x))
            {
                return x;
            }
            Tracer.Error($"Cannot decode string param {inp}");
            return null;
        }

        public static String Encode(String coll, String scene, String source) => new SceneItemKey(coll, scene, source).Stringize();
    }
}