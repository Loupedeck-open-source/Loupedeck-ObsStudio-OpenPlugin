namespace Loupedeck.ObsStudioPlugin
{
    using System;

    using OBSWebsocketDotNet.Types.Events;

    /// <summary>
    /// Proxy to OBS websocket server, for API reference see
    /// https://github.com/obsproject/obs-websocket/blob/4.x-compat/docs/generated/protocol.md
    /// </summary>
    internal partial class ObsAppProxy
    {
        public event EventHandler<EventArgs> AppEvtVirtualCamOn;

        public event EventHandler<EventArgs> AppEvtVirtualCamOff;

        public void AppToggleVirtualCam() => this.SafeRunConnected(() => this.ToggleVirtualCam(), "Cannot toggle virtual cam");

        public void AppStartVirtualCam() => this.SafeRunConnected(() => this.StartVirtualCam(), "Cannot start virtual cam");

        public void AppStopVirtualCam() => this.SafeRunConnected(() => this.StopVirtualCam(), "Cannot stop virtual cam");

        private void OnObsVirtualCameraStateChanged(Object _, VirtualcamStateChangedEventArgs args)
        {
            this.Plugin.Log.Info($"OBS VirtualCam state change, new state is {args.OutputState.IsActive}");

            switch (args.OutputState.IsActive)
            {
                case true:
                    this.AppEvtVirtualCamOn?.Invoke(this, new EventArgs());
                    break;
                case false:
                    this.AppEvtVirtualCamOff?.Invoke(this, new EventArgs());
                    break;
            }
        }
    }
}
