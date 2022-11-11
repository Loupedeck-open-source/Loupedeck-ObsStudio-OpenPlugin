namespace Loupedeck.ObsPlugin
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
            this.Trace("OBS SceneCollectionList changed");

            if (Helpers.TryExecuteSafe(() => { this.SceneCollections = this.ListSceneCollections(); }))
            {
                this.Trace($"Retreived list of {this.SceneCollections.Count} collections");

                this.AppEvtSceneCollectionsChanged?.Invoke(sender, e);
            }
        }

        private void OnObsSceneCollectionChanged(Object sender, EventArgs e)
        {
            var oldSceneCollection = this.CurrentSceneCollection;
            if (Helpers.TryExecuteSafe(() => { this.CurrentSceneCollection = this.GetCurrentSceneCollection(); }))
            {
                this.Trace($"OBS Current Scene collection changed from {oldSceneCollection} to {this.CurrentSceneCollection}");

                // Regenerating all internal structures
                this.OnObsSceneListChanged(sender, e);
                this.AppEvtCurrentSceneCollectionChanged?.Invoke(sender, e);
            }
            else
            {
                this.Trace($"OBS Warning: cannot handle Collection Changed");
            }
        }

        public void AppSwitchToSceneCollection(String newCollection)
        {
            if (this.IsAppConnected && this.SceneCollections.Contains(newCollection) && this.CurrentSceneCollection != newCollection)
            {
                this.Trace($"Switching to Scene Collection {newCollection}");
                Helpers.TryExecuteSafe(() => this.SetCurrentSceneCollection(newCollection));
            }
        }
    }
}
