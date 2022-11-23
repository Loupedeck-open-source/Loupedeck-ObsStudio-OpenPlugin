namespace Loupedeck.ObsStudioPlugin.Actions
{
    using System;

    public class SceneSelectCommand : PluginMultistateDynamicCommand
    {
        private ObsAppProxy Proxy => (this.Plugin as ObsStudioPlugin).Proxy;

        public const String IMGSceneSelected = "SceneOn.png";
        public const String IMGSceneUnselected = "SceneOff.png";
        public const String IMGSceneInaccessible = "SceneOff.png";
        public const String SceneNameUnknown = "Offline";

        public SceneSelectCommand()
        {
            this.Name = "SceneSelect";
            this.Description = "Switches to a specific scene in OBS Studio";
            this.GroupName = "1. Scenes";
            _ = this.AddState("Unselected", "Scene unselected");
            _ = this.AddState("Selected", "Scene selected");
        }

        protected override Boolean OnLoad()
        {
            this.IsEnabled = false;

            this.Proxy.AppConnected += this.OnAppConnected;
            this.Proxy.AppDisconnected += this.OnAppDisconnected;

            this.Proxy.AppEvtSceneListChanged += this.OnSceneListChanged;
            this.Proxy.AppEvtCurrentSceneChanged += this.OnCurrentSceneChanged;

            this.OnAppDisconnected(this, null);

            return true;
        }

        protected override Boolean OnUnload()
        {
            this.Proxy.AppConnected -= this.OnAppConnected;
            this.Proxy.AppDisconnected -= this.OnAppDisconnected;

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
                ObsStudioPlugin.Trace($"Adding {this.Proxy.Scenes?.Count} scene items");
                foreach (var scene in this.Proxy.Scenes)
                {
                    var key = SceneKey.Encode(this.Proxy.CurrentSceneCollection, scene.Name);
                    this.AddParameter(key, scene.Name, this.GroupName);
                    _ = this.SetCurrentState(key, scene.Name.Equals(this.Proxy.CurrentScene?.Name) ? 1 : 0);
                }
            }

            this.ParametersChanged();
        }

        private void OnSceneListChanged(Object sender, EventArgs e) =>
            this.ResetParameters(true);

        private void OnCurrentSceneChanged(Object sender, EventArgs e)
        {
            var arg = e as ObsAppProxy.OldNewStringChangeEventArgs;
            var oldPar = SceneKey.Encode(this.Proxy.CurrentSceneCollection, arg.Old);
            var newPar = SceneKey.Encode(this.Proxy.CurrentSceneCollection, arg.New);

            //unselecting old and selecting new
            this.SetCurrentState(oldPar, 0);
            this.SetCurrentState(newPar, 1);

            this.ActionImageChanged(oldPar);
            this.ActionImageChanged(newPar);

            this.ParametersChanged();
        }

        private void OnAppConnected(Object sender, EventArgs e) => this.IsEnabled = true;

        private void OnAppDisconnected(Object sender, EventArgs e)
        {
            this.IsEnabled = false;
            this.ResetParameters(false);
            this.ActionImageChanged();
        }

        protected override BitmapImage GetCommandImage(String actionParameter, Int32 stateIndex, PluginImageSize imageSize)
        {
            var imageName = IMGSceneInaccessible;
            var sceneName = SceneNameUnknown;

            if (SceneKey.TryParse(actionParameter, out var parsed))
            {
                sceneName = parsed.Scene;

                if( this.Proxy.TryGetSceneByName(parsed.Scene, out var _) )
                {
                    imageName = sceneName.Equals(this.Proxy.CurrentScene?.Name) ? IMGSceneSelected : IMGSceneUnselected;
                }
            }            
            return (this.Plugin as ObsStudioPlugin).GetPluginCommandImage(imageSize, imageName, sceneName, imageName == IMGSceneSelected);
        }
    }
}
