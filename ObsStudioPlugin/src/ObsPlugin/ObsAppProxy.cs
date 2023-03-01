namespace Loupedeck.ObsStudioPlugin
{
    using System;
    using System.Collections.Generic;
    using System.IO;

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

        // Folders to select from when we try saving screenshots
        public static readonly Environment.SpecialFolder[] ScreenshotFolders =
            {                
                Environment.SpecialFolder.MyPictures,
                Environment.SpecialFolder.MyDocuments,
                Environment.SpecialFolder.Personal,
                Environment.SpecialFolder.CommonPictures
            };

        public ObsAppProxy(Plugin _plugin)
        {
            this.Plugin = _plugin;

            // Trying to set screenshot save-to path
            for(var i=0; (i< ScreenshotFolders.Length) && String.IsNullOrEmpty(ObsAppProxy.ScreenshotsSavingPath); i++)
            {
                var folder = Environment.GetFolderPath(ScreenshotFolders[i]);
                if (Directory.Exists(folder))
                {
                    ObsAppProxy.ScreenshotsSavingPath = folder;
                }
            }
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
                this.PreviewSceneChanged -= this.OnObsPreviewSceneChanged;
        
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
                this.PreviewSceneChanged += this.OnObsPreviewSceneChanged;

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
                this._currentStreamingState = streamingStatus.IsStreaming ? OBSWebsocketDotNet.Types.OutputState.Started : OBSWebsocketDotNet.Types.OutputState.Stopped;

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

            this.Plugin.Log.Info("Init: OnObsSceneCollectionListChanged");

            this.OnObsSceneCollectionListChanged(sender, new OldNewStringChangeEventArgs("",""));

            this.Plugin.Log.Info("Init: OnObsSceneCollectionChanged");
            // This should initiate retreiving of all data
            // to indicate that we need to force rescan of all scenes and all first parameter is null 
            this.OnObsSceneCollectionChanged(null , e);
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

            this.TransitionEnd += this.OnObsTransitionEnd;

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

            this.TransitionEnd -= this.OnObsTransitionEnd;

            // Unsubscribing from all the events that are depenendent on Scene Collection change
            this._scene_collection_events_subscribed = false;
            this.UnsubscribeFromSceneCollectionEvents();

            this.AppDisconnected?.Invoke(sender, e);
        }

        private void SafeRunConnected(Action action, String warning) 
        {
            if (this.IsAppConnected)
            {
                if (!Helpers.TryExecuteSafe(action))
                {
                    this.Plugin.Log.Warning(warning);
                }
            }
        }
    }
}
