namespace Loupedeck.GenStreamPlugin
{
    using System;
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

        private void OnObsRecordingStateChange(OBSWebsocket sender, OBSWebsocketDotNet.Types.OutputState newState)
        {
            if( (newState == OBSWebsocketDotNet.Types.OutputState.Started )  || (newState == OBSWebsocketDotNet.Types.OutputState.Starting) )
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


        //
        // STREAMING
        //
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

        //
        // VIRTUAL CAM
        //
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

        //
        // STUDIO MODE
        //
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

            this.EvtAppConnected.Invoke(sender, e);

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

            //Note, invoking 
            this.EvtAppDisconnected?.Invoke(sender, e);
        }
    }
}
