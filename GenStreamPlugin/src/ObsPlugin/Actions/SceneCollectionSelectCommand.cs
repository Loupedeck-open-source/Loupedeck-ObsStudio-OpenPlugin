namespace Loupedeck.ObsStudioPlugin.Actions
{
    using System;
    using System.Collections.Generic;

    public class SceneCollectionSelectCommand : PluginMultistateDynamicCommand
    {
        private ObsAppProxy Proxy => (this.Plugin as ObsStudioPlugin).Proxy;

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
            this.Proxy.AppConnected += this.OnAppConnected;
            this.Proxy.AppDisconnected += this.OnAppDisconnected;

            this.Proxy.AppEvtSceneCollectionsChanged += this.OnSceneCollectionsChanged;
            this.Proxy.AppEvtCurrentSceneCollectionChanged += this.OnCurrentSceneCollectionChanged;

            this.OnAppDisconnected(this, null);

            return true;
        }

        protected override Boolean OnUnload()
        {
            this.Proxy.AppConnected -= this.OnAppConnected;
            this.Proxy.AppDisconnected -= this.OnAppDisconnected;

            this.Proxy.AppEvtSceneCollectionsChanged -= this.OnSceneCollectionsChanged;
            this.Proxy.AppEvtCurrentSceneCollectionChanged -= this.OnCurrentSceneCollectionChanged;

            return true;
        }

        protected override void RunCommand(String actionParameter)
        {
            if (actionParameter != null)
            {
                this.Proxy.AppSwitchToSceneCollection(actionParameter);
            }
        }

        private void ResetParameters(Boolean readScenes)
        {
            this.RemoveAllParameters();
            if (readScenes)
            {
                foreach (var coll in this.Proxy.SceneCollections)
                {
                    this.AddParameter(coll, coll, this.GroupName);
                    _ = this.SetCurrentState(coll, 0);
                }
                if (!String.IsNullOrEmpty(this.Proxy.CurrentSceneCollection))
                {
                    _ = this.SetCurrentState(this.Proxy.CurrentSceneCollection, 1);
                }
            }

            this.ParametersChanged();
        }

        private void OnSceneCollectionsChanged(Object sender, EventArgs e) =>
            this.ResetParameters(true);

        private void OnCurrentSceneCollectionChanged(Object sender, EventArgs e)
        {
            var arg = e as ObsAppProxy.OldNewStringChangeEventArgs;
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
