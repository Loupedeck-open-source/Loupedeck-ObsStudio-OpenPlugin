namespace Loupedeck.ObsStudioPlugin
{
    using System;
    using System.Collections.Generic;

    using OBSWebsocketDotNet;

    /// <summary>
    /// Proxy to OBS websocket server, for API reference see
    /// https://github.com/obsproject/obs-websocket/blob/4.x-compat/docs/generated/protocol.md
    /// </summary>
    internal partial class ObsAppProxy
    {
        //TODO: This kind of stuff needs to be in plugin settings
        static internal String DefaultScreenshotPath = "";

        public void AppTakeScreenshot()
        {
            var currentScene = this.CurrentScene?.Name;

            if (this.IsAppConnected && !String.IsNullOrEmpty(currentScene))
            {
                if (ObsAppProxy.DefaultScreenshotPath == "")
                {
                    ObsAppProxy.DefaultScreenshotPath = Environment.ExpandEnvironmentVariables("%USERPROFILE%\\Videos\\");
                }

                try
                {
                    //Generate unique filename 
                    var filename = ObsAppProxy.DefaultScreenshotPath + "Screenshot-" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".png";
                    var resp = this.TakeSourceScreenshot((String)currentScene, null, filename, -1, -1);
                    this.Plugin.Log.Info($"Screenshot taken and saved to {resp.ImageFile}");
                }
                catch (Exception ex)
                {
                    this.Plugin.Log.Error($"Exception {ex.Message} taking screenshot");
                }
            }
        }

    }
}
