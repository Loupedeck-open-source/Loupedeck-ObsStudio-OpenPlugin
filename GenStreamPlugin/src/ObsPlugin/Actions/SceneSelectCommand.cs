namespace Loupedeck.ObsPlugin.Actions
{
    using System;

    public class SceneSelectCommand : PluginMultistateDynamicCommand
    {
        private ObsAppProxy Proxy => (this.Plugin as ObsPlugin).Proxy;

        private const String IMGSceneSelected = "Loupedeck.ObsPlugin.icons.SceneOn.png";
        private const String IMGSceneUnselected = "Loupedeck.ObsPlugin.icons.SceneOff.png";
        private const String IMGSceneInaccessible = "Loupedeck.ObsPlugin.icons.CloseDesktop.png";
        private const String IMGOffline = "Loupedeck.ObsPlugin.icons.SoftwareNotFound.png";
        private const String SceneNameUnknown = "Offline";

        public SceneSelectCommand()
        {
            this.Name = "Scenes";
            this.Description = "Activates Scene";
            this.GroupName = "Scenes";

            this.AddState("Unselected", "Scene unselected");
            this.AddState("Selected", "Scene selected");
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
            if (SceneKey.TryParse(actionParameter, out var key))
            {
                this.Proxy.AppSwitchToScene(key.Scene);
            }
        }

        private void ResetParameters(Boolean readContent)
        {
            this.RemoveAllParameters();

            if (readContent)
            {
                this.Proxy.Trace($"Adding {this.Proxy.Scenes?.Count} scene items");
                foreach (var scene in this.Proxy.Scenes)
                {
                    var key = SceneKey.Encode(this.Proxy.CurrentSceneCollection, scene.Name);
                    this.AddParameter(key, scene.Name, this.GroupName);
                    this.SetCurrentState(key, scene.Name.Equals(this.Proxy.CurrentScene?.Name) ? 1 : 0);
                }
            }

            this.ParametersChanged();
        }

        private void OnSceneListChanged(Object sender, EventArgs e) =>
            this.ResetParameters(true);

        private void OnCurrentSceneChanged(Object sender, EventArgs e)
        {
            // resetting selection
            foreach (var par in this.GetParameters())
            {
                this.SetCurrentState(par.Name, 0);
            }

            if (!String.IsNullOrEmpty(this.Proxy.CurrentScene?.Name))
            {
                this.SetCurrentState(SceneKey.Encode(this.Proxy.CurrentSceneCollection, this.Proxy.CurrentScene?.Name), 1);
            }

            this.ActionImageChanged();
        }

        private void OnAppConnected(Object sender, EventArgs e)
        {
            // We expect to get SceneCollectionChange so doin' nothin' here.
        }

        private void OnAppDisconnected(Object sender, EventArgs e)
        {
            this.ResetParameters(false);
            this.ActionImageChanged();
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var imageName = IMGOffline;
            var sceneName = SceneNameUnknown;
            if (SceneKey.TryParse(actionParameter, out var parsed) && this.TryGetCurrentStateIndex(actionParameter, out var currentState))
            {
                sceneName = parsed.Scene;

                imageName = parsed.Collection != this.Proxy.CurrentSceneCollection
                    ? IMGSceneInaccessible
                    : currentState == 1 ? IMGSceneSelected : IMGSceneUnselected;
            }

            return ObsPlugin.NameOverBitmap(imageSize, imageName, sceneName);
        }
    }
}
