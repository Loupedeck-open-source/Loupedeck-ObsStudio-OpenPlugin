namespace Loupedeck.ObsStudioPlugin.Actions
{
    using System;

    internal class SceneSelectCommand : PluginMultistateDynamicCommand
    {
        public const String IMGSceneSelected = "SceneOn.png";
        public const String IMGSceneUnselected = "SceneOff.png";
        public const String IMGSceneInaccessible = "SceneInaccessible.png";
        public const String SceneNameUnknown = "Offline";
        private const Int16 SCENE_UNSELECTED = 0;
        private const Int16 SCENE_SELECTED = 1;

        public SceneSelectCommand()
        {
            this.Description = "Switches to a specific scene in OBS Studio";
            this.GroupName = "1. Scenes";
            _ = this.AddState("Unselected", "Scene unselected");
            _ = this.AddState("Selected", "Scene selected");
        }

        protected override Boolean OnLoad()
        {
            this.IsEnabled = false;

            ObsStudioPlugin.Proxy.AppConnected += this.OnAppConnected;
            ObsStudioPlugin.Proxy.AppDisconnected += this.OnAppDisconnected;

            ObsStudioPlugin.Proxy.AppEvtSceneListChanged += this.OnSceneListChanged;
            ObsStudioPlugin.Proxy.AppEvtCurrentSceneChanged += this.OnCurrentSceneChanged;

            ObsStudioPlugin.Proxy.AppEvtSceneNameChanged += this.OnSceneListChanged;

            this.OnAppDisconnected(this, null);

            return true;
        }

        protected override Boolean OnUnload()
        {
            ObsStudioPlugin.Proxy.AppConnected -= this.OnAppConnected;
            ObsStudioPlugin.Proxy.AppDisconnected -= this.OnAppDisconnected;

            ObsStudioPlugin.Proxy.AppEvtSceneListChanged -= this.OnSceneListChanged;
            ObsStudioPlugin.Proxy.AppEvtCurrentSceneChanged -= this.OnCurrentSceneChanged;

            ObsStudioPlugin.Proxy.AppEvtSceneNameChanged -= this.OnSceneListChanged;

            return true;
        }

        protected override void RunCommand(String actionParameter)
        {
            if (SceneKey.TryParse(actionParameter, out var key))
            {
                ObsStudioPlugin.Proxy.AppSwitchToScene(key.Scene);
            }
        }

        private void ResetParameters(Boolean readContent)
        {
            this.RemoveAllParameters();

            if (readContent)
            {
                this.Plugin.Log.Info($"Adding {ObsStudioPlugin.Proxy.Scenes?.Count} scenes");
                foreach (var scene in ObsStudioPlugin.Proxy.Scenes)
                {
                    var key = SceneKey.Encode(ObsStudioPlugin.Proxy.CurrentSceneCollection, scene.Name);
                    this.AddParameter(key, scene.Name, this.GroupName).Description=$"Switch to scene \"{scene.Name}\"";
                    this.SetCurrentState(key, scene.Name.Equals(ObsStudioPlugin.Proxy.CurrentSceneName) ? SCENE_SELECTED : SCENE_UNSELECTED);
                }
            }

            this.ParametersChanged();
        }

        private void OnSceneListChanged(Object sender, EventArgs e) =>
            this.ResetParameters(true);
        
        private void OnCurrentSceneChanged(Object sender, OldNewStringChangeEventArgs arg)
        {
            this.Plugin.Log.Info($"Scene changed from {arg.Old} to {arg.New}");
            //FIXME: We need to make sure this selection change works well
            //Note that the old scene can be from the diffrent scene collection

            //unselecting old (if it'ss still there) and selecting new
            if (ObsStudioPlugin.Proxy.TryGetSceneByName(arg.Old, out var _))
            {
                var param = SceneKey.Encode(ObsStudioPlugin.Proxy.CurrentSceneCollection, arg.Old);
                this.SetCurrentState(param, SCENE_UNSELECTED);

            }
            else
            {
                this.Plugin.Log.Warning($"OnCurrentSceneChanged: Old Scene is not in current collection {arg.Old}. Current collection: {ObsStudioPlugin.Proxy.CurrentSceneCollection}");
            }

            if ( !String.IsNullOrEmpty(arg.New))
            {
                var param = SceneKey.Encode(ObsStudioPlugin.Proxy.CurrentSceneCollection, arg.New);
                this.SetCurrentState(param, SCENE_SELECTED);
            }

            this.ActionImageChanged();
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

                if ( parsed.Collection != ObsStudioPlugin.Proxy.CurrentSceneCollection )
                {
                    imageName = IMGSceneInaccessible;
                }
                else if( ObsStudioPlugin.Proxy.TryGetSceneByName(parsed.Scene, out var _) )
                {
                    imageName = stateIndex == SCENE_SELECTED ? IMGSceneSelected : IMGSceneUnselected;
                }
                else
                {
                    //Note: see if this produces too much logging output
                    this.Plugin.Log.Info($"Cannot find scene \"{parsed.Scene}\" in current collection. Was it renamed or deleted?");
                }
            }            
            return (this.Plugin as ObsStudioPlugin).GetPluginCommandImage(imageSize, imageName, sceneName, imageName == IMGSceneSelected);
        }
    }
}
