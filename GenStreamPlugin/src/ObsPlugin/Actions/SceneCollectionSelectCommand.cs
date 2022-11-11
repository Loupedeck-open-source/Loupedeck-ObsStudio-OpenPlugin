namespace Loupedeck.ObsPlugin.Actions
{
    using System;
    using System.Collections.Generic;

    public class SceneCollectionSelectCommand : PluginMultistateDynamicCommand
    {
        private ObsAppProxy Proxy => (this.Plugin as ObsPlugin).Proxy;

        private const String IMGCollectionSelected = "Loupedeck.ObsPlugin.icons.SourceOn.png";
        private const String IMGCollectionUnselected = "Loupedeck.ObsPlugin.icons.SourceOff.png";
        private const String IMGOffline = "Loupedeck.ObsPlugin.icons.SoftwareNotFound.png";
        private const String CollectionNameUnknown = "Offline";

        public SceneCollectionSelectCommand()
        {
            this.Name = "Scene Collections";
            this.Description = "Activates Scene collection";
            this.GroupName = "Scene Collections";

            this.AddState("Unselected", "Scene collection unselected");
            this.AddState("Selected", "Scene collection selected");
        }

        protected override Boolean OnLoad()
        {
            this.Proxy.EvtAppConnected += this.OnAppConnected;
            this.Proxy.EvtAppDisconnected += this.OnAppDisconnected;

            this.Proxy.AppEvtSceneCollectionsChanged += this.OnSceneCollectionsChanged;
            this.Proxy.AppEvtCurrentSceneCollectionChanged += this.OnCurrentSceneCollectionChanged;

            this.OnAppDisconnected(this, null);

            return true;
        }

        protected override Boolean OnUnload()
        {
            this.Proxy.EvtAppConnected -= this.OnAppConnected;
            this.Proxy.EvtAppDisconnected -= this.OnAppDisconnected;

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
                    this.SetCurrentState(coll, 0);
                }
                if (!String.IsNullOrEmpty(this.Proxy.CurrentSceneCollection))
                {
                    this.SetCurrentState(this.Proxy.CurrentSceneCollection, 1);
                }
            }

            this.ParametersChanged();
        }

        private void OnSceneCollectionsChanged(Object sender, EventArgs e) =>
            this.ResetParameters(true);

        private void OnCurrentSceneCollectionChanged(Object sender, EventArgs e)
        {
            foreach (var par in this.GetParameters())
            {
                this.SetCurrentState(par.Name, 0);
            }

            if (!String.IsNullOrEmpty(this.Proxy.CurrentSceneCollection))
            {
                this.SetCurrentState(this.Proxy.CurrentSceneCollection, 1);
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
            var collName = CollectionNameUnknown;

            if (!String.IsNullOrEmpty(actionParameter) && this.Proxy.IsAppConnected)
            {
                imageName = actionParameter == this.Proxy.CurrentSceneCollection ? IMGCollectionSelected : IMGCollectionUnselected;
            }

            return ObsPlugin.NameOverBitmap(imageSize, imageName, collName);
        }
    }
}
