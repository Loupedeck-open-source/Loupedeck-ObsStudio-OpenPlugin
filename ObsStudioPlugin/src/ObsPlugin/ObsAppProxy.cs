namespace Loupedeck.ObsStudioPlugin
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    using OBSWebsocketDotNet.Communication;
    using OBSWebsocketDotNet.Types.Events;

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
                this.CurrentProgramSceneChanged -= this.OnObsSceneChanged;
                this.CurrentPreviewSceneChanged -= this.OnObsPreviewSceneChanged;
        
                this.SceneItemEnableStateChanged  -= this.OnObsSceneItemVisibilityChanged;
                this.SceneItemCreated -= this.OnObsSceneItemAdded;
                this.SceneItemRemoved -= this.OnObsSceneItemRemoved;

                this.InputCreated -= this.OnObsSourceCreated;
                this.InputRemoved -= this.OnObsSourceDestroyed;

                this.InputMuteStateChanged -= this.OnObsInputMuteStateChanged;
                this.InputVolumeChanged -= this.OnObsInputVolumeChanged;
                
#if false
                this.SourceAudioActivated -= this.OnObsSourceAudioActivated;
                this.SourceAudioDeactivated -= this.OnObsSourceAudioDeactivated;
#endif
                this._scene_collection_events_subscribed = true;
            }
        }

        private void SubscribeToSceneCollectionEvents()
        {
            if (this._scene_collection_events_subscribed)
            {
                this.SceneListChanged += this.OnObsSceneListChanged;
                this.CurrentProgramSceneChanged += this.OnObsSceneChanged;
                this.CurrentPreviewSceneChanged += this.OnObsPreviewSceneChanged;

                this.SceneItemEnableStateChanged += this.OnObsSceneItemVisibilityChanged;

                this.SceneItemCreated += this.OnObsSceneItemAdded;
                this.SceneItemRemoved += this.OnObsSceneItemRemoved;


                this.InputCreated += this.OnObsSourceCreated;
                this.InputRemoved += this.OnObsSourceDestroyed;

                this.InputMuteStateChanged += this.OnObsInputMuteStateChanged;
                this.InputVolumeChanged += this.OnObsInputVolumeChanged;



                this.InputNameChanged += this.OnObsInputNameChanged;
                //this.InputSettingsChanged += this.OnObsInputSettingsChanged;
                this.InputActiveStateChanged += this.OnObsInputActiveStateChanged;
                this.InputShowStateChanged += this.OnObsInputShowStateChanged;

                this.InputAudioBalanceChanged += this.OnObsInputAudioBalanceChanged;
                this.InputAudioSyncOffsetChanged += this.OnObsinputAudioSyncOffsetChanged;
                this.InputAudioTracksChanged += this.OnObsInputAudioTracksChanged;
                this.InputAudioMonitorTypeChanged += this.OnObsInputAudioMonitorTypeChanged;
                this.InputVolumeMeters += this.OnObsInputVolumeMeters;



#if false
                this.SourceAudioActivated += this.OnObsSourceAudioActivated;
                this.SourceAudioDeactivated += this.OnObsSourceAudioDeactivated;
#endif
                this._scene_collection_events_subscribed = false;
            }
        }
        private void OnObsInputNameChanged(object sender, InputNameChangedEventArgs e)
        {
            this.Plugin.Log.Info($"Entering {MethodBase.GetCurrentMethod().Name}");
        }

        private void OnObsInputActiveStateChanged(object sender, InputActiveStateChangedEventArgs e)
        {
            this.Plugin.Log.Info($"Entering {MethodBase.GetCurrentMethod().Name}");
        }

        private void OnObsInputShowStateChanged(object sender, InputShowStateChangedEventArgs e)
        {
            this.Plugin.Log.Info($"Entering {MethodBase.GetCurrentMethod().Name}");
        }

        private void OnObsInputAudioBalanceChanged(object sender, InputAudioBalanceChangedEventArgs e)
        {
            this.Plugin.Log.Info($"Entering {MethodBase.GetCurrentMethod().Name}");
        }

        private void OnObsinputAudioSyncOffsetChanged(object sender, InputAudioSyncOffsetChangedEventArgs e)
        {
            this.Plugin.Log.Info($"Entering {MethodBase.GetCurrentMethod().Name}");
        }

        private void OnObsInputAudioTracksChanged(object sender, InputAudioTracksChangedEventArgs e)
        {
            this.Plugin.Log.Info($"Entering {MethodBase.GetCurrentMethod().Name}");
        }

        private void OnObsInputAudioMonitorTypeChanged(object sender, InputAudioMonitorTypeChangedEventArgs e)
        {

            this.Plugin.Log.Info($"Entering {MethodBase.GetCurrentMethod().Name}");
        }

        private void OnObsInputVolumeMeters(object sender, InputVolumeMetersEventArgs e)
        {
            this.Plugin.Log.Info($"Entering {MethodBase.GetCurrentMethod().Name}");
        }


        internal void InitializeObsData(Object sender, EventArgs e)
        {
            // NOTE: This can throw! Exception handling is done OUTSIDE of this method
            var streamingStatus = this.GetStreamStatus();
            var recordStatus = this.GetRecordStatus();
            var vcamstatus = this.GetVirtualCamStatus();
            var studioModeStatus = this.GetStudioModeEnabled();

            // Retreiving Audio types.
            this.OnAppConnected_RetreiveSourceTypes();

            if (streamingStatus != null)
            {
                this._currentStreamingState = streamingStatus.IsActive
                    ? OBSWebsocketDotNet.Types.OutputState.OBS_WEBSOCKET_OUTPUT_STARTED
                    : OBSWebsocketDotNet.Types.OutputState.OBS_WEBSOCKET_OUTPUT_STOPPED;

                this.OnObsRecordingStateChange(this, recordStatus.IsRecording 
                    ? OBSWebsocketDotNet.Types.OutputState.OBS_WEBSOCKET_OUTPUT_STARTED
                    : OBSWebsocketDotNet.Types.OutputState.OBS_WEBSOCKET_OUTPUT_STOPPED);

                this.OnObsStreamingStateChange(this, streamingStatus.IsActive
                    ? OBSWebsocketDotNet.Types.OutputState.OBS_WEBSOCKET_OUTPUT_STARTED
                    : OBSWebsocketDotNet.Types.OutputState.OBS_WEBSOCKET_OUTPUT_STOPPED);
            }

            if (vcamstatus != null)
            {
                var arg = new OBSWebsocketDotNet.Types.Events.VirtualcamStateChangedEventArgs(new OBSWebsocketDotNet.Types.OutputStateChanged());
                arg.OutputState.IsActive = vcamstatus.IsActive;
                this.OnObsVirtualCameraStateChanged(sender, arg);
            }

            this.OnObsStudioModeStateChanged(sender, studioModeStatus);

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
            this.RecordStateChanged += this.OnObsRecordingStateChange;
            this.StreamStateChanged += this.OnObsStreamingStateChange;
            this.VirtualcamStateChanged += this.OnObsVirtualCameraStateChanged;
            this.StudioModeStateChanged += this.OnObsStudioModeStateChanged;
            this.ReplayBufferStateChanged += this.OnObsReplayBufferStateChange;

            this.SceneCollectionListChanged += this.OnObsSceneCollectionListChanged;
            this.CurrentSceneCollectionChanged += this.OnObsSceneCollectionChanged;

            //this.SceneTransitionEnded += this.OnObsTransitionEnd;

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

        private void OnAppDisconnected(Object sender, ObsDisconnectionInfo arg)
        {
            this.Plugin.Log.Info($"Entering AppDisconnected. Disconnect reason:\"{arg.DisconnectReason}\"");

            // Unsubscribing from App events here
            this.RecordStateChanged -= this.OnObsRecordingStateChange;

            this.StreamStateChanged -= this.OnObsStreamingStateChange;
            this.StudioModeStateChanged -= this.OnObsStudioModeStateChanged;

            this.SceneCollectionListChanged -= this.OnObsSceneCollectionListChanged;
            this.CurrentSceneCollectionChanged -= this.OnObsSceneCollectionChanged;

            //this.TransitionEnd -= this.OnObsTransitionEnd;

            // Unsubscribing from all the events that are depenendent on Scene Collection change
            this._scene_collection_events_subscribed = false;
            this.UnsubscribeFromSceneCollectionEvents();

            this.AppDisconnected?.Invoke(sender, new System.EventArgs() );
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
