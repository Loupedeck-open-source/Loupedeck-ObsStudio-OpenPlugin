namespace Loupedeck.GenStreamPlugin
{
    //TODO: Transform regions into partial class files 
    //TODO: Move to the Vasily's switch class and remove 'disconnected' state
    //TODO: Use single (?) device offline image for all unavailable actions?
    using System;
    using System.Collections.Generic;
    using OBSWebsocketDotNet;

    /// <summary>
    /// Proxy to OBS websocket server, for API reference see
    /// https://github.com/obsproject/obs-websocket/blob/4.x-compat/docs/generated/protocol.md
    /// </summary>
    public partial class GenStreamProxy : OBSWebsocketDotNet.OBSWebsocket
    {
        // Our 'own' events       
        public event EventHandler<EventArgs> EvtAppConnected;
        public event EventHandler<EventArgs> EvtAppDisconnected;

        // Properties
        public Boolean IsAppConnected => this.IsConnected;

        public GenStreamProxy()
        {
            //OBS Websocket events
            this.Connected += this.OnAppConnected;
            this.Disconnected += this.OnAppDisconnected;
        }

        ~GenStreamProxy()
        {
            this.Connected -= this.OnAppConnected;
            this.Disconnected -= this.OnAppDisconnected;
        }

  
        private void OnAppConnected(Object sender, EventArgs e)
        {
            // Subscribing to App events
            // Notifying all subscribers on App Connected
            // Fetching initial states for controls
            this.RecordingStateChanged += this.OnObsRecordingStateChange;
            this.StreamingStateChanged += this.OnObsStreamingStateChange;
            this.VirtualCameraStarted  += this.OnObsVirtualCameraStarted;
            this.VirtualCameraStopped  += this.OnObsVirtualCameraStopped;
            this.StudioModeSwitched += this.OnObsStudioModeStateChange;
            this.ReplayBufferStateChanged += this.OnObsReplayBufferStateChange;

            this.SceneCollectionListChanged += this.OnObsSceneCollectionListChanged;
            this.SceneCollectionChanged += this.OnObsSceneCollectionChanged;

            this.SceneListChanged += this.OnObsSceneListChanged;
            this.SceneChanged += this.OnObsSceneChanged;

            this.EvtAppConnected?.Invoke(sender, e);

            Helpers.TryExecuteSafe(() =>
            {
                var streamingStatus = this.GetStreamingStatus();
                var vcamstatus = this.GetVirtualCamStatus();
                var studioModeStatus = this.StudioModeEnabled();

                if (streamingStatus != null)
                {
                    this.OnObsRecordingStateChange(this, streamingStatus.IsRecording ? OBSWebsocketDotNet.Types.OutputState.Started : OBSWebsocketDotNet.Types.OutputState.Stopped);
                    this.OnObsStreamingStateChange(this, streamingStatus.IsStreaming ? OBSWebsocketDotNet.Types.OutputState.Started : OBSWebsocketDotNet.Types.OutputState.Stopped);
                }
                
                if( vcamstatus != null && vcamstatus.IsActive )
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

                this.OnObsSceneListChanged(sender, e);
            });

        }
        private void OnAppDisconnected(Object sender, EventArgs e)
        {
            // Unsubscribing from App events here
            this.RecordingStateChanged -= this.OnObsRecordingStateChange;
            this.StreamingStateChanged -= this.OnObsStreamingStateChange;
            this.VirtualCameraStarted -= this.OnObsVirtualCameraStarted;
            this.VirtualCameraStopped -= this.OnObsVirtualCameraStopped;
            this.StudioModeSwitched -= this.OnObsStudioModeStateChange;

            this.SceneCollectionListChanged -= this.OnObsSceneCollectionListChanged;
            this.SceneCollectionChanged -= this.OnObsSceneCollectionChanged;

            this.SceneListChanged -= this.OnObsSceneListChanged;
            this.SceneChanged -= this.OnObsSceneChanged;

            this.EvtAppDisconnected?.Invoke(sender, e);
        }
    }
}
