namespace Loupedeck.ObsStudioPlugin.Actions
{
    using System;
    using System.Collections.Generic;

    internal class SceneCollectionSelectCommand : PluginMultistateDynamicCommand
    {
        public const String IMGCollectionSelected = "SceneOn.png";
        public const String IMGCollectionUnselected = "SceneOff.png";

        public SceneCollectionSelectCommand()
        {
            this.Description = "Switches to a specific Scene Collection in OBS Studio";
            this.GroupName = "4. Scene Collections";

            _ = this.AddState("Unselected", "Scene collection unselected");
            _ = this.AddState("Selected", "Scene collection selected");
        }

        protected override Boolean OnLoad()
        {
            this.IsEnabled = false;
            ObsStudioPlugin.Proxy.AppConnected += this.OnAppConnected;
            ObsStudioPlugin.Proxy.AppDisconnected += this.OnAppDisconnected;

            ObsStudioPlugin.Proxy.AppEvtSceneCollectionsChanged += this.OnSceneCollectionsChanged;
            ObsStudioPlugin.Proxy.AppEvtCurrentSceneCollectionChanged += this.OnCurrentSceneCollectionChanged;

            this.OnAppDisconnected(this, null);

            return true;
        }

        protected override Boolean OnUnload()
        {
            ObsStudioPlugin.Proxy.AppConnected -= this.OnAppConnected;
            ObsStudioPlugin.Proxy.AppDisconnected -= this.OnAppDisconnected;

            ObsStudioPlugin.Proxy.AppEvtSceneCollectionsChanged -= this.OnSceneCollectionsChanged;
            ObsStudioPlugin.Proxy.AppEvtCurrentSceneCollectionChanged -= this.OnCurrentSceneCollectionChanged;

            return true;
        }

        protected override void RunCommand(String actionParameter)
        {
            if (actionParameter != null)
            {
                ObsStudioPlugin.Proxy.AppSwitchToSceneCollection(actionParameter);
            }
        }

        private void ResetParameters(Boolean readScenes)
        {
            this.RemoveAllParameters();
            if (readScenes)
            {
                foreach (var coll in ObsStudioPlugin.Proxy.SceneCollections)
                {
                    this.AddParameter(coll, coll, this.GroupName).Description = $"Switch to scene collection \"{coll}\"";
                    this.SetCurrentState(coll, 0);
                }
                if (!String.IsNullOrEmpty(ObsStudioPlugin.Proxy.CurrentSceneCollection))
                {
                    this.SetCurrentState(ObsStudioPlugin.Proxy.CurrentSceneCollection, 1);
                }
            }

            this.ParametersChanged();
        }

        private void OnSceneCollectionsChanged(Object sender, EventArgs e) =>
            this.ResetParameters(true);

        private void OnCurrentSceneCollectionChanged(Object sender, OldNewStringChangeEventArgs arg)
        {
            
            //unselecting old and selecting new
            if (!String.IsNullOrEmpty(arg.Old))
            {
                this.SetCurrentState(arg.Old, 0);
                this.ActionImageChanged(arg.Old);
            }
            if (!String.IsNullOrEmpty(arg.New))
            {
                this.SetCurrentState(arg.New, 1);
                this.ActionImageChanged(arg.New);
            }

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
            var imageName = stateIndex == 1 ? IMGCollectionSelected : IMGCollectionUnselected;

            return (this.Plugin as ObsStudioPlugin).GetPluginCommandImage(imageSize, imageName, String.IsNullOrEmpty(actionParameter) ? "Offline" : actionParameter, stateIndex == 1);
        }
    }
}
