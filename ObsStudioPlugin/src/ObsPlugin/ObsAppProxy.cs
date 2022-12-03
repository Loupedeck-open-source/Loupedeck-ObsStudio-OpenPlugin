namespace Loupedeck.ObsStudioPlugin
{
    // TODO:

    // Legacy actions and adustments
    // Source rename
    // FIXME: Reconnect on OBS crash? 

    // Universal toggle action
    // CPU action
    // 6. Profiles actions 
    // Use scene icon -- TakeSourceScreenshot
    
    // HOW TO STOP SHOWING SOURCES THAT ARE NOT VISIBLE IN MIXER

    // Useful command -- RefreshBrowserSource
    //  Add Multistate-Parameter Profile
    // ? source action?

    // For all actions,
    //  - Handle added/deleted/created/destroyed signals (editing)
    // --> For later, upgrade to new OBS websocket and see if port/password can be parsed from the Ini file in C:\Users\[User]\AppData\Roaming\obs-studio

    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Proxy to OBS websocket server, for API reference see
    /// https://github.com/obsproject/obs-websocket/blob/4.x-compat/docs/generated/protocol.md
    /// </summary>
    internal partial class ObsAppProxy : OBSWebsocketDotNet.OBSWebsocket
    {
        // Our 'own' events
        public event EventHandler<EventArgs> AppConnected;
        public event EventHandler<EventArgs> AppDisconnected;

        public Plugin Plugin { get; private set; }

        // Properties
        public Boolean IsAppConnected => this.IsConnected;

        public ObsAppProxy(Plugin _plugin)
        {
            this.CurrentScene = new OBSWebsocketDotNet.Types.OBSScene();
            this.Scenes = new List<OBSWebsocketDotNet.Types.OBSScene>();
            this.Plugin = _plugin;
        }
        public void RegisterAppEvents()
        {
            //Mapping OBS Websocket events to ours
            this.Connected += this.OnAppConnected;
            this.Disconnected += this.OnAppDisconnected;
        }

        public void UnregisterAppEvents()
        {
            //Unmapping OBS Websocket events 
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

            this.OnObsSceneCollectionListChanged(sender, new OldNewStringChangeEventArgs("",""));
            this.OnObsSceneCollectionChanged(sender, e);
        }

        private void OnAppConnected(Object sender, EventArgs e)
        {
            this.Plugin.Log.Info("Entering AppConnected");

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

            this.Plugin.Log.Info("AppConnected: Initializing data");
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
            this.Plugin.Log.Info("Entering AppDisconnected");

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

        internal Boolean TryConvertLegacyActionParamToKey(String actionParameter, out SceneItemKey key)
        {
            //Sample action parameter: 9|Background|BRB
            //TODO: Find right variable for the separator
            var FieldSeparator = "|";
            key = Helpers.TryExecuteFunc(
                () =>
                {
                    var parts = actionParameter.Split(FieldSeparator, StringSplitOptions.RemoveEmptyEntries);
                    return (parts as String[])?.Length > 2 ? new SceneItemKey(this.CurrentSceneCollection, parts[2], parts[1]) : null;
                }, out var x) ? x : null;

            return key != null;
        }

    }
}
