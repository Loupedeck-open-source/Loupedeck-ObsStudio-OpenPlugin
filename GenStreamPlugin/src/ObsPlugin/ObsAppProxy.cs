namespace Loupedeck.ObsStudioPlugin
{
    // TODO:

    // TEST: Scene item added / removed

    // CONVERTER/Check compatibility with existing OBS plugin

    // OnObsSourceRename
    // HOW TO STOP SHOWING SOURCES THAT ARE NOT VISIBLE IN MIXER

    // Simple actions
    // *  Transition
    // *  Save replay buffer

    // Toggle
    // *  Recording pause/resume
    // Special
    //  Universal toggle (Tree)
    // CPU
    // Useful command -- RefreshBrowserSource
    //  Add Multistate-Parameter Profile
    // NB for sources -- TakeSourceScreenshot
    // ? source action?

    // For all actions,
    //  - Handle added/deleted/created/destroyed signals (editing)
    // --> For later, upgrade to new OBS websocket and see if port/password can be parsed from the Ini file in C:\Users\[User]\AppData\Roaming\obs-studio

    using System;

    /// <summary>
    /// Proxy to OBS websocket server, for API reference see
    /// https://github.com/obsproject/obs-websocket/blob/4.x-compat/docs/generated/protocol.md
    /// </summary>
    public partial class ObsAppProxy : OBSWebsocketDotNet.OBSWebsocket
    {
        // Our 'own' events
        public event EventHandler<EventArgs> AppConnected;

        public event EventHandler<EventArgs> AppDisconnected;

        // Properties
        public Boolean IsAppConnected => this.IsConnected;

        public ObsAppProxy()
        {
            // OBS Websocket events
            this.Connected += this.OnAppConnected;
            this.Disconnected += this.OnAppDisconnected;
        }

        ~ObsAppProxy()
        {
            this.Connected -= this.OnAppConnected;
            this.Disconnected -= this.OnAppDisconnected;
        }

        private Boolean _scene_collection_events_subscribed = false;

        private void UnsubscribeFromSceneCollectionEvents()
        {
            if (!this._scene_collection_events_subscribed)
            {
                this.SceneListChanged -= this.OnObsSceneListChanged;
                this.SceneChanged -= this.OnObsSceneChanged;

                this.SceneItemVisibilityChanged -= this.OnObsSceneItemVisibilityChanged;
                this.SceneItemAdded -= this.OnObsSceneItemAdded;
                this.SceneItemRemoved -= this.OnObsSceneItemRemoved;

                this.SourceMuteStateChanged -= this.OnObsSourceMuteStateChanged;
                this.SourceVolumeChanged -= this.OnObsSourceVolumeChanged;

                this.SourceCreated -= this.OnObsSourceCreated;
                this.SourceDestroyed -= this.OnObsSourceDestroyed;

                this.SourceAudioActivated -= this.OnObsSourceAudioActivated;
                this.SourceAudioDeactivated -= this.OnObsSourceAudioDeactivated;
                this._scene_collection_events_subscribed = true;
            }
        }

        private void SubscribeToSceneCollectionEvents()
        {
            if (this._scene_collection_events_subscribed)
            {
                this.SceneListChanged += this.OnObsSceneListChanged;
                this.SceneChanged += this.OnObsSceneChanged;

                this.SceneItemVisibilityChanged += this.OnObsSceneItemVisibilityChanged;
                this.SceneItemAdded += this.OnObsSceneItemAdded;
                this.SceneItemRemoved += this.OnObsSceneItemRemoved;

                this.SourceMuteStateChanged += this.OnObsSourceMuteStateChanged;
                this.SourceVolumeChanged += this.OnObsSourceVolumeChanged;

                this.SourceCreated += this.OnObsSourceCreated;
                this.SourceDestroyed += this.OnObsSourceDestroyed;

                this.SourceAudioActivated += this.OnObsSourceAudioActivated;
                this.SourceAudioDeactivated += this.OnObsSourceAudioDeactivated;
                this._scene_collection_events_subscribed = false;
            }
        }

        internal void InitializeObsData(Object sender, EventArgs e)
        {
            // NOTE: This can throw! Exception handling is done OUTSIDE of this method
            var streamingStatus = this.GetStreamingStatus();
            var vcamstatus = this.GetVirtualCamStatus();
            var studioModeStatus = this.StudioModeEnabled();

            // Retreiving Audio types.
            this.OnAppConnected_RetreiveSourceTypes();

            if (streamingStatus != null)
            {
                this.OnObsRecordingStateChange(this, streamingStatus.IsRecording ? OBSWebsocketDotNet.Types.OutputState.Started : OBSWebsocketDotNet.Types.OutputState.Stopped);
                this.OnObsStreamingStateChange(this, streamingStatus.IsStreaming ? OBSWebsocketDotNet.Types.OutputState.Started : OBSWebsocketDotNet.Types.OutputState.Stopped);
            }

            if (vcamstatus != null && vcamstatus.IsActive)
            {
                this.OnObsVirtualCameraStarted(sender, e);
            }
            else
            {
                this.OnObsVirtualCameraStopped(sender, e);
            }

            this.OnObsStudioModeStateChange(sender, studioModeStatus);

            this.OnObsSceneCollectionListChanged(sender, e);
            this.OnObsSceneCollectionChanged(sender, e);
        }

        private void OnAppConnected(Object sender, EventArgs e)
        {
            ObsStudioPlugin.Trace("Entering AppConnected");

            // Subscribing to App events
            // Notifying all subscribers on App Connected
            // Fetching initial states for controls
            this.RecordingStateChanged += this.OnObsRecordingStateChange;
            this.RecordingPaused += this.OnObsRecordPaused;
            this.RecordingResumed += this.OnObsRecordResumed;
            this.StreamingStateChanged += this.OnObsStreamingStateChange;
            this.VirtualCameraStarted += this.OnObsVirtualCameraStarted;
            this.VirtualCameraStopped += this.OnObsVirtualCameraStopped;
            this.StudioModeSwitched += this.OnObsStudioModeStateChange;
            this.ReplayBufferStateChanged += this.OnObsReplayBufferStateChange;

            this.SceneCollectionListChanged += this.OnObsSceneCollectionListChanged;
            this.SceneCollectionChanged += this.OnObsSceneCollectionChanged;

            this.AppConnected?.Invoke(sender, e);

            ObsStudioPlugin.Trace("AppConnected: Initializing data");
            _ = Helpers.TryExecuteSafe(() =>
            {
                this.InitializeObsData(sender, e);
            });

            // Subscribing to all the events that are depenendent on Scene Collection change
            this._scene_collection_events_subscribed = true;
            this.SubscribeToSceneCollectionEvents();
        }

        private void OnAppDisconnected(Object sender, EventArgs e)
        {
            ObsStudioPlugin.Trace("Entering AppDisconnected");

            // Unsubscribing from App events here
            this.RecordingStateChanged -= this.OnObsRecordingStateChange;
            this.RecordingPaused -= this.OnObsRecordPaused;
            this.RecordingResumed -= this.OnObsRecordResumed;

            this.StreamingStateChanged -= this.OnObsStreamingStateChange;
            this.VirtualCameraStarted -= this.OnObsVirtualCameraStarted;
            this.VirtualCameraStopped -= this.OnObsVirtualCameraStopped;
            this.StudioModeSwitched -= this.OnObsStudioModeStateChange;

            this.SceneCollectionListChanged -= this.OnObsSceneCollectionListChanged;
            this.SceneCollectionChanged -= this.OnObsSceneCollectionChanged;

            // Unsubscribing from all the events that are depenendent on Scene Collection change
            this._scene_collection_events_subscribed = false;
            this.UnsubscribeFromSceneCollectionEvents();

            this.AppDisconnected?.Invoke(sender, e);
        }
    }
}
