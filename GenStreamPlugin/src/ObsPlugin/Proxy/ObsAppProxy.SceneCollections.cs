namespace Loupedeck.ObsStudioPlugin
{
    using System;
    using System.Collections.Generic;

    using OBSWebsocketDotNet;

    /// <summary>
    /// Proxy to OBS websocket server, for API reference see
    /// https://github.com/obsproject/obs-websocket/blob/4.x-compat/docs/generated/protocol.md
    /// </summary>
    public partial class ObsAppProxy
    {
        public event EventHandler<EventArgs> AppEvtSceneCollectionsChanged;

        public event EventHandler<EventArgs> AppEvtCurrentSceneCollectionChanged;

        public List<String> SceneCollections { get; private set; }

        public String CurrentSceneCollection { get; private set; }

        private void OnObsSceneCollectionListChanged(Object sender, EventArgs e)
        {
            ObsStudioPlugin.Trace("OBS SceneCollectionList changed");

            if (Helpers.TryExecuteSafe(() => this.SceneCollections = this.ListSceneCollections()))
            {
                ObsStudioPlugin.Trace($"Retreived list of {this.SceneCollections.Count} collections");

                this.AppEvtSceneCollectionsChanged?.Invoke(sender, e);
            }
        }

        private void OnObsSceneCollectionChanged(Object sender, EventArgs e)
        {
            var args = new OldNewStringChangeEventArgs();
            args.Old = this.CurrentSceneCollection;

            if (Helpers.TryExecuteFunc(()=> this.GetCurrentSceneCollection(),out var newSceneCollection) && newSceneCollection != args.Old)
            {
                args.New = newSceneCollection;
                ObsStudioPlugin.Trace($"OBS Current Scene collection changing from {args.Old} to {newSceneCollection}");
                this.CurrentSceneCollection = newSceneCollection;

                // Regenerating all internal structures
                this.OnObsSceneListChanged(sender, e);
                this.AppEvtCurrentSceneCollectionChanged?.Invoke(sender, args);

                // SEE THE AppSwitchToSceneCollection
                this.SubscribeToSceneCollectionEvents();
            }
            else
            {
                ObsStudioPlugin.Trace($"OBS Warning: cannot handle Collection Changed");
            }
        }

        public void AppSwitchToSceneCollection(String newCollection)
        {
            if (this.IsAppConnected && this.SceneCollections.Contains(newCollection) && this.CurrentSceneCollection != newCollection)
            {
                // NOTE NOTE: After we issue Switch Scene Collection command, there will be lots of events coming from OBS
                // BEFORE we get SceneCollectionChanged event
                //  SINCE OUR INTERNAL DATA STRUCTURES ARE REGENERATED FROM THE LATTER, we temporarily set the 'suspend events' flag

                ObsStudioPlugin.Trace($"Switching to Scene Collection {newCollection}");

                this.UnsubscribeFromSceneCollectionEvents();

                _ = Helpers.TryExecuteSafe(() => this.SetCurrentSceneCollection(newCollection));
            }
        }
    }
}
