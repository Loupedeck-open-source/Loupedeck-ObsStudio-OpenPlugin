namespace Loupedeck.GenStreamPlugin.Actions
{
    using System;
    using System.Collections.Generic;

    class SceneCommand : PluginDynamicCommand
    {
        internal class SceneCollectionTuple
        {
            private const String separator = "-||~~(%)~~||-";
            public String SceneName { get; private set; }
            public String SceneCollectionName { get; private set; }
            protected SceneCollectionTuple(String scene, String collection)
            {
                this.SceneName = scene;
                this.SceneCollectionName = collection;
            }

            public override String ToString() => separator + this.SceneCollectionName + separator + this.SceneName;

            public static String Encode(String collection, String scene) => new SceneCollectionTuple(collection, scene).ToString();

            public static SceneCollectionTuple Decode(String inp)
            {
                try
                {
                    var parts = inp.Split(separator, StringSplitOptions.RemoveEmptyEntries);

                    if ((parts as String[])?.Length == 2)
                    {
                        return new SceneCollectionTuple(parts[0], parts[1]);
                    }
                }
                catch (Exception ex)
                {
                    Tracer.Error($"Exception {ex.InnerException.Message}: Cannot decode string param {inp}");
                }

                Tracer.Warning($"Cannot parse Collection-Scene from {inp}");
                return null;
            }
        }

        private GenStreamProxy Proxy => (this.Plugin as GenStreamPlugin).Proxy;
    
        private const String IMG_SceneSelected = "Loupedeck.GenStreamPlugin.icons.SourceOn.png";
        private const String IMG_SceneUnselected = "Loupedeck.GenStreamPlugin.icons.SourceOff.png";
        private const String IMG_SceneInaccessible = "Loupedeck.GenStreamPlugin.icons.CloseDesktop.png";
        private const String IMG_Offline = "Loupedeck.GenStreamPlugin.icons.SoftwareNotFound.png";

        public SceneCommand()
        {
            this.Name = "Scenes";
            this.Description = "Activates Scene";
            this.GroupName = "Scenes in current collection";
        }

        protected override Boolean OnLoad()
        {

            this.Proxy.EvtAppConnected += this.OnAppConnected;
            this.Proxy.EvtAppDisconnected += this.OnAppDisconnected;

            this.Proxy.AppEvtSceneListChanged += this.OnSceneListChanged;
            this.Proxy.AppEvtCurrentSceneChanged += this.OnCurrentSceneChanged;

            this.OnAppDisconnected(this, null);

            return true;
        }

        protected override Boolean OnUnload()
        {
            this.Proxy.EvtAppConnected -= this.OnAppConnected;
            this.Proxy.EvtAppDisconnected -= this.OnAppDisconnected;

            this.Proxy.AppEvtSceneListChanged -= this.OnSceneListChanged;
            this.Proxy.AppEvtCurrentSceneChanged -= this.OnCurrentSceneChanged;

            return true;
        }

        protected override void RunCommand(String actionParameter)
        {
            var tuple = SceneCollectionTuple.Decode(actionParameter);
            if (tuple != null)
            {
                this.Proxy.AppSwitchToScene(tuple.SceneName);
            }
        }
        
        private void ResetParameters(Boolean readScenes)
        {
            this.RemoveAllParameters();

            if(readScenes)
            {
                foreach (var scene in this.Proxy.Scenes)
                {
                    this.AddParameter(SceneCollectionTuple.Encode(this.Proxy.CurrentSceneCollection, scene.Name), scene.Name, this.GroupName);
                }
            }
            this.ParametersChanged();
        }

        private void OnSceneListChanged(Object sender, EventArgs e) =>
            this.ResetParameters(true);

        private void OnCurrentSceneChanged(Object sender, EventArgs e) => this.ActionImageChanged();

        private void OnAppConnected(Object sender, EventArgs e)
        { 
            //We expect to get SceneCollectionChange so doin' nothin' here. 
        }

        private void OnAppDisconnected(Object sender, EventArgs e)
        {
            this.ResetParameters(false);
            this.ActionImageChanged();
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var tuple = SceneCollectionTuple.Decode(actionParameter);

            return (tuple != null) && this.Proxy.IsAppConnected
                ? this.Proxy.CurrentScene.Name == tuple.SceneName
                    ? EmbeddedResources.ReadImage(IMG_SceneSelected)
                    : this.Proxy.SceneInCurrentCollection(tuple.SceneName)
                        ? EmbeddedResources.ReadImage(IMG_SceneUnselected)
                        : EmbeddedResources.ReadImage(IMG_SceneInaccessible)
                : EmbeddedResources.ReadImage(IMG_Offline);
        }
    }
}
