namespace Loupedeck.GenStreamPlugin
{
    //TODO: 

    //TEST: Scene item added / removed

    // Multistate-Parameter actions
    //  * Volume Mixer Mute 
    //  * Volume Mixer
    //  * General Audio Mute 
    //  * General Audio

    // OnObsSourceRename
    // HOW TO STOP SHOWING SOURCES THAT ARE NOT VISIBLE IN MIXER

    // Simple actions
    //  Transition 
    //  Save replay buffer

    // Toggle
    //  Recording pause/resume


    //Special
    //  Universal toggle (Tree)
    // CPU
    //  Add Multistate-Parameter Profile
    // Useful command -- RefreshBrowserSource
    // NB for sources -- TakeSourceScreenshot

    // ? Visualiue source action?

    // For all actions,
    //  - Handle added/deleted/created/destroyed signals (editing)

    // --> FOr later, upgrade to new OBS websocket and see if port/password can be parsed from the Ini file in C:\Users\[User]\AppData\Roaming\obs-studio


    using System;
    using System.Collections.Generic;
    
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
            this.allSceneItems = new Dictionary<String, SceneItemDescriptor>();
        }

        ~GenStreamProxy()
        {
            this.Connected -= this.OnAppConnected;
            this.Disconnected -= this.OnAppDisconnected;
        }

        public void Trace(String s) => Tracer.Trace("GSP:"+s);
        private void OnAppConnected(Object sender, EventArgs e)
        {
            this.Trace("Entering AppConnected");

            this.OnAppConnected_RetreiveSourceTypes();
            this.OnAppConnected_RetreiveSpecialSources();

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
            this.SceneItemVisibilityChanged += this.OnObsSceneItemVisibilityChanged;
            this.SceneItemAdded += this.OnObsSceneItemAdded;
            this.SceneItemRemoved += this.OnObsSceneItemRemoved;
            
            this.SourceMuteStateChanged += this.OnObsSourceMuteStateChanged;
            this.SourceVolumeChanged += this.OnObsSourceVolumeChanged;

            this.SourceCreated += this.OnObsSourceCreated;
            this.SourceDestroyed += this.OnObsSourceDestroyed;

            this.EvtAppConnected?.Invoke(sender, e);

            this.Trace("AppConnected: Initializing data");

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

                // this.OnObsSceneListChanged(sender, e) is called form SceneCollectionChanged
                

            });

        }
        private void OnAppDisconnected(Object sender, EventArgs e)
        {
            this.Trace("Entering AppDisconnected");
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

            this.SceneItemVisibilityChanged -= this.OnObsSceneItemVisibilityChanged;
            this.SceneItemAdded -= this.OnObsSceneItemAdded;
            this.SceneItemRemoved -= this.OnObsSceneItemRemoved;

            this.SourceCreated -= this.OnObsSourceCreated;
            this.SourceDestroyed -= this.OnObsSourceDestroyed;

            this.SourceMuteStateChanged -= this.OnObsSourceMuteStateChanged;

            this.EvtAppDisconnected?.Invoke(sender, e);
        }
    }
}
