namespace Loupedeck.GenStreamPlugin.Actions
{
    using System;
    using System.Collections.Generic;

    class SceneCollectionCommand : PluginMultistateDynamicCommand
    {
        private GenStreamProxy Proxy => (this.Plugin as GenStreamPlugin).Proxy;
    
        private const String IMG_CollectionSelected = "Loupedeck.GenStreamPlugin.icons.SourceOn.png";
        private const String IMG_CollectionUnselected = "Loupedeck.GenStreamPlugin.icons.SourceOff.png";
        private const String IMG_Offline = "Loupedeck.GenStreamPlugin.icons.SoftwareNotFound.png";
        private const String CollectionNameUnknown = "Offline";

        public SceneCollectionCommand()
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
            if(readScenes)
            {
                foreach (var coll in this.Proxy.SceneCollections)
                {
                    this.AddParameter(coll, coll, this.GroupName);
                    this.SetCurrentState(coll, 0);
                }
                if(!String.IsNullOrEmpty(this.Proxy.CurrentSceneCollection))
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
            foreach(var par in this.GetParameters())
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
            //We expect to get SceneCollectionChange so doin' nothin' here. 
        }

        private void OnAppDisconnected(Object sender, EventArgs e)
        {
            this.ResetParameters(false);
            this.ActionImageChanged();
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var imageName = IMG_Offline;
            var collName = CollectionNameUnknown;

            if ( !String.IsNullOrEmpty(actionParameter) && this.Proxy.IsAppConnected)
            {

                imageName = actionParameter == this.Proxy.CurrentSceneCollection ? IMG_CollectionSelected : IMG_CollectionUnselected;
            }

            return GenStreamPlugin.NameOverBitmap(imageSize, imageName, collName);
        }
    }
}
