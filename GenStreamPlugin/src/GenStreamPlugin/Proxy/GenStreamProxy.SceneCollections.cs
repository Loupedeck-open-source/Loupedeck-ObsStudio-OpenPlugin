namespace Loupedeck.GenStreamPlugin
{
    using System;
    using System.Collections.Generic;
    using OBSWebsocketDotNet;

    /// <summary>
    /// Proxy to OBS websocket server, for API reference see
    /// https://github.com/obsproject/obs-websocket/blob/4.x-compat/docs/generated/protocol.md
    /// </summary>
    public partial class GenStreamProxy 
    {

        public event EventHandler<EventArgs> AppEvtSceneCollectionsChanged;
        public event EventHandler<EventArgs> AppEvtCurrentSceneCollectionChanged;

        public List<String> SceneCollections { get; private set; }

        public String CurrentSceneCollection { get; private set; }

        void OnObsSceneCollectionListChanged(Object sender, EventArgs e)
        {
            if (Helpers.TryExecuteSafe(() => { this.SceneCollections = this.ListSceneCollections(); }))
            {
                this.AppEvtSceneCollectionsChanged?.Invoke(sender, e);
            }
        }

        void OnObsSceneCollectionChanged(Object sender, EventArgs e)
        {
            if (Helpers.TryExecuteSafe(() => { this.CurrentSceneCollection = this.GetCurrentSceneCollection(); }))
            {
                this.AppEvtCurrentSceneCollectionChanged?.Invoke(sender, e);
            }
        }

        public void CycleSceneCollections()
        {
            if (this.IsAppConnected && this.SceneCollections != null && this.CurrentSceneCollection != "")
            {
                var index = this.SceneCollections.IndexOf(this.CurrentSceneCollection);
                index = index != this.SceneCollections.Count - 1 ? index + 1 : 0;
                this.AppSwitchToSceneCollection(this.SceneCollections[index]);
            }
        }

        public void AppSwitchToSceneCollection(String newCollection)
        {
            if (this.IsAppConnected && this.SceneCollections.Contains(newCollection) && this.CurrentSceneCollection != newCollection)
            {
                Helpers.TryExecuteSafe(() => this.SetCurrentSceneCollection(newCollection));
            }

        }
    }
}
