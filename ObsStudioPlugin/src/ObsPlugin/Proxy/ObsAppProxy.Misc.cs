﻿namespace Loupedeck.ObsStudioPlugin
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
        public static String ScreenshotsSavingPath { get; private set; } = "";

        public void AppTakeScreenshot()
        {
            var currentScene = this.CurrentSceneName;

            if (this.IsAppConnected && !String.IsNullOrEmpty(currentScene))
            {
                try
                {

                    //Generate unique filename 
                    var filename = System.IO.Path.Combine(ObsAppProxy.ScreenshotsSavingPath, "Screenshot-" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".png");

                    this.SaveSourceScreenshot((String)currentScene,"png" , filename, -1, -1);

                    this.Plugin.Log.Info($"Screenshot taken and saved to {filename}");
                }
                catch (Exception ex)
                {
                    this.Plugin.Log.Error($"Exception {ex.Message} taking screenshot");
                }
            }
        }

    }
}
