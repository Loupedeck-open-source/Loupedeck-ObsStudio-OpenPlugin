namespace Loupedeck.GenStreamPlugin
{
    using System;
    using System.Collections.Generic;
    using OBSWebsocketDotNet;


    public class GenStreamProxy : OBSWebsocketDotNet.OBSWebsocket
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

        //
        // RECORDING 
        //
        public event EventHandler<EventArgs> AppEvtRecordingOn;
        public event EventHandler<EventArgs> AppEvtRecordingOff;

        //FIXME: Provide customized images for starting/started... -- For that, create special event handler on Action side. 
        private void OnObsRecordingStateChange(OBSWebsocket sender, OBSWebsocketDotNet.Types.OutputState newState)
        {
            if ((newState == OBSWebsocketDotNet.Types.OutputState.Started) || (newState == OBSWebsocketDotNet.Types.OutputState.Starting))
            {
                this.AppEvtRecordingOn?.Invoke(this, new EventArgs());
            }
            else
            {
                this.AppEvtRecordingOff?.Invoke(this, new EventArgs());
            }
        }
        public void AppToggleRecording()
        {
            if (this.IsAppConnected)
            {
                Helpers.TryExecuteSafe(() => this.ToggleRecording());
            }
        }

        #region STREAMING TOGGLE
        public event EventHandler<EventArgs> AppEvtStreamingOn;
        public event EventHandler<EventArgs> AppEvtStreamingOff;

        private void OnObsStreamingStateChange(OBSWebsocket sender, OBSWebsocketDotNet.Types.OutputState newState)
        {
            if ((newState == OBSWebsocketDotNet.Types.OutputState.Started) || (newState == OBSWebsocketDotNet.Types.OutputState.Starting))
            {
                this.AppEvtStreamingOn?.Invoke(this, new EventArgs());
            }
            else
            {
                this.AppEvtStreamingOff?.Invoke(this, new EventArgs());
            }
        }
        public void AppToggleStreaming()
        {
            if (this.IsAppConnected)
            {
                Helpers.TryExecuteSafe(() => this.ToggleStreaming());
            }
        }
        #endregion

        #region VIRTUAL CAM TOGGLE
        public event EventHandler<EventArgs> AppEvtVirtualCamOn;
        public event EventHandler<EventArgs> AppEvtVirtualCamOff;

        private void OnObsVirtualCameraStarted(Object sender, EventArgs e) => this.AppEvtVirtualCamOn?.Invoke(this, new EventArgs());
        private void OnObsVirtualCameraStopped(Object sender, EventArgs e) => this.AppEvtVirtualCamOff?.Invoke(this, new EventArgs());

        public void AppToggleVirtualCam()
        {
            if (this.IsAppConnected)
            {
                Helpers.TryExecuteSafe(() => this.ToggleVirtualCam());
            }
        }
        #endregion

        #region STUDIO MODE TOGGLE
        public event EventHandler<EventArgs> AppEvtStudioModeOn;
        public event EventHandler<EventArgs> AppEvtStudioModeOff;

        private void OnObsStudioModeStateChange(OBSWebsocket sender, Boolean enabled)
        {
            if (enabled)
            {
                this.AppEvtStudioModeOn?.Invoke(this, new EventArgs());
            }
            else
            {
                this.AppEvtStudioModeOff?.Invoke(this, new EventArgs());
            }
        }
        public void AppToggleStudioMode()
        {
            if (this.IsAppConnected)
            {
                Helpers.TryExecuteSafe(() => this.ToggleStudioMode());
            }
        }
        #endregion

        #region REPLAY BUFFER TOGGLE
        public event EventHandler<EventArgs> AppEvtReplayBufferOn;
        public event EventHandler<EventArgs> AppEvtReplayBufferOff;
        private void OnObsReplayBufferStateChange(OBSWebsocket sender, OBSWebsocketDotNet.Types.OutputState newState)
        {
            if ((newState == OBSWebsocketDotNet.Types.OutputState.Started) || (newState == OBSWebsocketDotNet.Types.OutputState.Starting))
            {
                this.AppEvtReplayBufferOn?.Invoke(this, new EventArgs());
            }
            else
            {
                this.AppEvtReplayBufferOff?.Invoke(this, new EventArgs());
            }
        }

        public void AppToggleReplayBuffer()
        {
            if (this.IsAppConnected)
            {
                Helpers.TryExecuteSafe(() => this.ToggleReplayBuffer());
            }
        }
        #endregion

        #region SCENE COLLECTIONS

        public event EventHandler<EventArgs> AppEvtSceneCollectionsChanged;
        public event EventHandler<EventArgs> AppEvtCurrentSceneCollectionChanged;


        public List<String> SceneCollections { get; private set; }

        public String CurrentSceneCollection {  get; private set; }

        void OnObsSceneCollectionListChanged(Object sender, EventArgs e)
        {
            if( Helpers.TryExecuteSafe(() => { this.SceneCollections = this.ListSceneCollections(); }) )
            { 
                this.AppEvtSceneCollectionsChanged?.Invoke(sender, e);
            }
        }

        void OnObsSceneCollectionChanged(Object sender, EventArgs e)
        {
            if( Helpers.TryExecuteSafe(() => { this.CurrentSceneCollection = this.GetCurrentSceneCollection(); }) )
            {
                this.AppEvtCurrentSceneCollectionChanged?.Invoke(sender, e);
            }
        }

        public void CycleSceneCollections()
        {
            if(this.IsAppConnected && this.SceneCollections != null && this.CurrentSceneCollection!="")
            {
                var index = this.SceneCollections.IndexOf(this.CurrentSceneCollection);
                index = index != this.SceneCollections.Count - 1 ? index + 1 : 0;
                this.AppSwitchToScenCollection(this.SceneCollections[index]);
            }
        }

        public void AppSwitchToScenCollection(String newCollection)
        {
            if (this.IsAppConnected && this.SceneCollections.Contains(newCollection) && this.CurrentSceneCollection != newCollection)
            {
                Helpers.TryExecuteSafe(() => this.SetCurrentSceneCollection(newCollection));
            }

        }
        #endregion
        // ----------------------------------
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


            this.EvtAppConnected?.Invoke(sender, e);

            //Setting correct states
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
                    this.OnObsVirtualCameraStarted(this, e);
                } 
                else
                {
                    this.OnObsVirtualCameraStopped(this, e);
                }

                this.OnObsStudioModeStateChange(this, studioModeStatus);

                this.OnObsSceneCollectionListChanged(this, e);

                this.OnObsSceneCollectionChanged(this, e);
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

            //Note, invoking 
            this.EvtAppDisconnected?.Invoke(sender, e);
        }
    }
}
