namespace Loupedeck.ObsStudioPlugin
{
    using System;

    /// <summary>
    /// Proxy to OBS websocket server, for API reference see
    /// https://github.com/obsproject/obs-websocket/blob/4.x-compat/docs/generated/protocol.md
    /// </summary>
    internal partial class ObsAppProxy
    {
        public event EventHandler<EventArgs> AppEvtVirtualCamOn;

        public event EventHandler<EventArgs> AppEvtVirtualCamOff;

        private void OnObsVirtualCameraStarted(Object sender, EventArgs e)
        {
            ObsStudioPlugin.Trace("Obs Virtual camera started");
            this.AppEvtVirtualCamOn?.Invoke(this, new EventArgs());
        }

        private void OnObsVirtualCameraStopped(Object sender, EventArgs e)
        {
            ObsStudioPlugin.Trace("Obs Virtual camera stopped");
            this.AppEvtVirtualCamOff?.Invoke(this, new EventArgs());
        }

        public void AppToggleVirtualCam()
        {
            if (this.IsAppConnected)
            {
                _ = Helpers.TryExecuteSafe(() => this.ToggleVirtualCam());
            }
        }
    }
}
