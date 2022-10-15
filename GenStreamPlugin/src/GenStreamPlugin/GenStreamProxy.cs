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
            //this.OBSExit -= this.OnObsExit;
        }

        ~GenStreamProxy()
        {
            this.Connected -= this.OnAppConnected;
            this.Disconnected -= this.OnAppDisconnected;
        }

        //Event forwarded from App.
        // Commands

        //
        // RECORDING 
        //
        //Event forwarded from App.
        // Recording Toggled on in target application
        public event EventHandler<EventArgs> AppEvtRecordingOn;
        // Recording Toggled off in target application
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
        //Event forwarded from App.
        // Recording Toggled on in target application
        public event EventHandler<EventArgs> AppEvtStreamingOn;
        // Streaming Toggled off in target application
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
        //Event forwarded from App.
        // Recording Toggled on in target application
        public event EventHandler<EventArgs> AppEvtVirtualCamOn;
        // VirtualCam Toggled off in target application
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
        //Event forwarded from App.
        // Recording Toggled on in target application
        public event EventHandler<EventArgs> AppEvtStudioModeOn;
        // StudioMode Toggled off in target application
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
            // Fetching initial states for controls
            // Notifying all subscribers on App Connected

            this.RecordingStateChanged += this.OnObsRecordingStateChange;
            this.StreamingStateChanged += this.OnObsStreamingStateChange;
            this.VirtualCameraStarted  += this.OnObsVirtualCameraStarted;
            this.VirtualCameraStopped  += this.OnObsVirtualCameraStopped;
            this.StudioModeSwitched += this.OnObsStudioModeStateChange;


            this.EvtAppConnected.Invoke(sender, e);

            //Setting correct states
            Helpers.TryExecuteSafe(() =>
            {
                var status = this.GetStreamingStatus();
                if (status != null)
                {
                    this.OnObsRecordingStateChange(this, status.IsRecording ? OBSWebsocketDotNet.Types.OutputState.Started : OBSWebsocketDotNet.Types.OutputState.Stopped);
                    this.OnObsStreamingStateChange(this, status.IsStreaming ? OBSWebsocketDotNet.Types.OutputState.Started : OBSWebsocketDotNet.Types.OutputState.Stopped);
                }
            });

            Helpers.TryExecuteSafe(() =>
            {
                var status = this.GetVirtualCamStatus();
                if( status != null && status.IsActive )
                {
                    this.OnObsVirtualCameraStarted(this, e);
                } 
                else
                {
                    this.OnObsVirtualCameraStopped(this, e);
                }
            });

            Helpers.TryExecuteSafe(() =>
            {
                this.OnObsStudioModeStateChange(this, this.StudioModeEnabled());
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
