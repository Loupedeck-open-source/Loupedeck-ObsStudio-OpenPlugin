namespace Loupedeck.ObsStudioPlugin
{
    using System;
    using System.Collections.Generic;

    // NOTE: FOR RELEASE BUILD, 'optimization needs to be turned  off in the Build tab of project properties
    // In order to make ReadEmbedded images properly working!

    // This class contains the plugin-level logic of the Loupedeck plugin.

     partial class ObsStudioPlugin : Plugin
    {
        internal static ObsAppProxy Proxy { get; private set; } 

        // Gets a value indicating whether this is an Universal plugin or an Application plugin.
        public override Boolean UsesApplicationApiOnly => true;

        // Gets a value indicating whether this is an API-only plugin.
        public override Boolean HasNoApplication => true;

        private readonly ObsConnector _connector;

        public ObsStudioPlugin()
        {
            ObsStudioPlugin.Proxy = new ObsAppProxy(this);
            this._connector = new ObsConnector(ObsStudioPlugin.Proxy, this.GetPluginDataDirectory(),
                                (Object sender, EventArgs e) => this.OnPluginStatusChanged(Loupedeck.PluginStatus.Warning, this.Localization.GetString("Connecting to OBS"), "https://support.loupedeck.com/obs-guide", ""));
        }

        // Load is called once as plugin is being initialized during service start.
        public override void Load()
        {
            this.Info.Icon16x16 = EmbeddedResources.ReadImage("Loupedeck.ObsStudioPlugin.metadata.Icon16x16.png");
            this.Info.Icon32x32 = EmbeddedResources.ReadImage("Loupedeck.ObsStudioPlugin.metadata.Icon32x32.png");
            this.Info.Icon48x48 = EmbeddedResources.ReadImage("Loupedeck.ObsStudioPlugin.metadata.Icon48x48.png");
            this.Info.Icon256x256 = EmbeddedResources.ReadImage("Loupedeck.ObsStudioPlugin.metadata.Icon256x256.png");

            this.ClientApplication.ApplicationStarted += this.OnApplicationStarted;
            this.ClientApplication.ApplicationStopped += this.OnApplicationStopped;

            this.ClientApplication.ApplicationInstanceStarted += this.OnApplicationStarted;
            this.ClientApplication.ApplicationInstanceStopped += this.OnApplicationStopped;

            ObsStudioPlugin.Proxy.AppConnected += this.OnAppConnStatusChange;
            ObsStudioPlugin.Proxy.AppDisconnected += this.OnAppConnStatusChange;

            ObsStudioPlugin.Proxy.RegisterAppEvents(); 

            this._connector.Start();

            this.Update_PluginStatus();
        }

        // Unload is called once when plugin is being unloaded.
        public override void Unload()
        {
            this._connector.Stop();
            ObsStudioPlugin.Proxy.UnregisterAppEvents();

            this.OnApplicationStopped(this, null);

            this.ClientApplication.ApplicationStarted -= this.OnApplicationStarted;
            this.ClientApplication.ApplicationStopped -= this.OnApplicationStopped;

            this.ClientApplication.ApplicationInstanceStarted -= this.OnApplicationStarted;
            this.ClientApplication.ApplicationInstanceStopped -= this.OnApplicationStopped;

            ObsStudioPlugin.Proxy.AppConnected -= this.OnAppConnStatusChange;
            ObsStudioPlugin.Proxy.AppDisconnected -= this.OnAppConnStatusChange;

        }

        private void OnAppConnStatusChange(Object sender, EventArgs e) => this.Update_PluginStatus();

        private void OnApplicationStarted(Object sender, EventArgs e)
        {
        }

        private void OnApplicationStopped(Object sender, EventArgs e)
        {
        }

        private void Update_PluginStatus()
        {
            if (!this.IsApplicationInstalled())
            {
                this.OnPluginStatusChanged(Loupedeck.PluginStatus.Error, "OBS Studio is not installed", "https://support.loupedeck.com/obs-guide", "more details");
            }
            else if (ObsStudioPlugin.Proxy.IsAppConnected)
            {
                this.OnPluginStatusChanged(Loupedeck.PluginStatus.Normal, "");
            }
            else
            {
                this.OnPluginStatusChanged(Loupedeck.PluginStatus.Warning, "Not connected to OBS", "https://support.loupedeck.com/obs-guide", "more details");
            }
        }

        private static readonly BitmapColor BitmapColorPink = new BitmapColor(255, 192, 203);

        internal BitmapBuilder BuildImage(PluginImageSize imageSize, String imageName, String text, Boolean selected)
        {
            var bitmapBuilder = new BitmapBuilder(imageSize);
            try
            {
                
                var image = EmbeddedResources.ReadImage(imageName);
                bitmapBuilder.DrawImage(image);
            }
            catch (Exception ex)
            {
                this.Log.Error($"Cannot load image {imageName}, exception {ex}");
            }

            if (!String.IsNullOrEmpty(text))
            {
                var x1 = bitmapBuilder.Width * 0.1;
                var w = bitmapBuilder.Width * 0.8;
                var y1 = bitmapBuilder.Height * 0.60;
                var h = bitmapBuilder.Height * 0.3;

                bitmapBuilder.DrawText(text, (Int32)x1, (Int32)y1, (Int32)w, (Int32)h, 
                                            selected ? BitmapColor.Black : BitmapColorPink, 
                                            imageSize == PluginImageSize.Width90 ? 13 : 9, 
                                            imageSize == PluginImageSize.Width90 ? 12 : 8, 1);
            }

            return bitmapBuilder;
        }

        // Loupedeck.ObsPlugin.icons.
        internal const String ImageResPrefix = "Loupedeck.ObsStudioPlugin.Icons.";

        /// <summary>
        ///  Draws text over the bitmap. Bad location but in absence of the better components, put it here.
        /// </summary>
        /// <param name="imageSize">size of the image</param>
        /// <param name="imagePath">Image file name</param>
        /// <param name="text">text to render</param>
        /// <param name="textSelected">If true, darker color of text is chosen</param>
        /// <returns>bitmap with text rendered</returns>
        internal BitmapImage GetPluginCommandImage(PluginImageSize imageSize, String imagePath, String text = null, Boolean textSelected = false) => 
            this.BuildImage(imageSize, ImageResPrefix + imagePath, text, textSelected).ToImage();

        public static void Trace(String line) => Tracer.Trace("GSP:" + line); /*System.Diagnostics.Debug.WriteLine(*/
    }
}
