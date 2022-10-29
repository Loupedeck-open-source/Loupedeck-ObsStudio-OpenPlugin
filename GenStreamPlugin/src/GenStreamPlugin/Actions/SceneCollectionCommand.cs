namespace Loupedeck.GenStreamPlugin.Actions
{
    using System;
    using System.Collections.Generic;

    class SceneCollectionCommand : PluginDynamicCommand
    {
        private GenStreamProxy Proxy => (this.Plugin as GenStreamPlugin).Proxy;
    
        private const String CycleActionName = "DUMMY ACTION NAME";
        private const String CycleActionDescription = "Cycle Scene Collection";
        private const String IMG_CycleAction = "Loupedeck.GenStreamPlugin.icons.Workspaces.png";
        private const String IMG_CollectionSelected = "Loupedeck.GenStreamPlugin.icons.SourceOn.png";
        private const String IMG_CollectionUnselected = "Loupedeck.GenStreamPlugin.icons.SourceOff.png";
        private const String IMG_CollectionInaccessible = "Loupedeck.GenStreamPlugin.icons.CloseDesktop.png";
        private const String IMG_Offline = "Loupedeck.GenStreamPlugin.icons.SoftwareNotFound.png";

        public SceneCollectionCommand()
        {
            this.Name = "Scene Collections";
            this.Description = "Activates Scene collection";
            this.GroupName = "Scene Collections";
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
            if (actionParameter == CycleActionName)
            {
                this.Proxy.CycleSceneCollections();
            }
            else if (actionParameter != null)
            {
                this.Proxy.AppSwitchToSceneCollection(actionParameter);
            }
        }

        private void ResetParameters(Boolean readScenes)
        {
            this.RemoveAllParameters();
            this.AddParameter(CycleActionName, CycleActionDescription, this.GroupName);
            if(readScenes)
            {
                foreach (var coll in this.Proxy.SceneCollections)
                {
                    this.AddParameter(coll, coll, this.GroupName);
                }
            }
            this.ParametersChanged();
        }

        private void OnSceneCollectionsChanged(Object sender, EventArgs e) =>
            this.ResetParameters(true);

        private void OnCurrentSceneCollectionChanged(Object sender, EventArgs e) => this.ActionImageChanged();

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
            if (actionParameter == CycleActionName)
            {
                return EmbeddedResources.ReadImage(IMG_CycleAction);
            } 
            else if ( (actionParameter != null) && this.Proxy.IsAppConnected )
            {
                return this.Proxy.CurrentSceneCollection == actionParameter
                    ? EmbeddedResources.ReadImage(IMG_CollectionSelected)
                    : this.Proxy.SceneCollections.Contains(actionParameter)
                        ? EmbeddedResources.ReadImage(IMG_CollectionUnselected)
                        : EmbeddedResources.ReadImage(IMG_CollectionInaccessible);

            }

            return EmbeddedResources.ReadImage(IMG_Offline );
        }
    }
}
