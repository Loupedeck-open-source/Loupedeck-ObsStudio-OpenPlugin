﻿namespace Loupedeck.GenStreamPlugin
 {
    using System;
  
    internal class SceneKey
    {
        public const String FieldSeparator = "||~~(%)~~||";
        public const String NothingField = "NONE";
        public String Scene;
        public String Collection;
        public SceneKey(String coll, String scene)
        {
            this.Scene = scene ?? NothingField;
            this.Collection = coll ?? NothingField;
        }

        private String Stringize()
        {
            var a = new String[] { this.Collection, this.Scene };
            return String.Join(FieldSeparator, a);
        }

        public static Boolean TryParse(String inp, out SceneKey key )
        {
            key = SceneKey.FromString(inp);
            return key != null;
        }

        public static SceneKey FromString(String inp)
        {
            if (Helpers.TryExecuteFunc(() =>
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
        public String Source;
        public SceneItemKey(String coll, String scene, String Source) : base(coll, scene) => this.Source = Source ?? NothingField;
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
            if (Helpers.TryExecuteFunc(() =>
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
    public class SourceDictItem
    {
        public String CollectionName;
        public String SceneName;
        public String SceneNameProp => this.SceneItemProps.ItemName;
        public String SourceName => this.SceneItemDetails.SourceName;

        public Boolean Visible { get { return this.SceneItemProps.Visible; } set { this.SceneItemProps.Visible = value; } }

        private readonly OBSWebsocketDotNet.Types.SceneItemDetails SceneItemDetails;
        private readonly OBSWebsocketDotNet.Types.SceneItemProperties SceneItemProps;
        public SourceDictItem(String coll, String scene, OBSWebsocketDotNet.Types.SceneItemDetails item, OBSWebsocketDotNet.Types.SceneItemProperties props)
        {
            this.CollectionName = coll;
            this.SceneItemDetails = item;
            this.SceneItemProps = props;
            this.SceneName = scene;

            if (scene != this.SceneNameProp)
            {
                Tracer.Trace($"SourceDictItem ctor: Scene name {scene} Scene Name in details: { this.SceneNameProp}");
            }
        }
    }
}