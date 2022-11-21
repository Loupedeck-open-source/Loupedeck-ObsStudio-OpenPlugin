namespace Loupedeck.ObsStudioPlugin
{
    using System;
    using System.Collections.Generic;

    // NOTE: FOR RELEASE BUILD, 'optimization needs to be turned  off in the Build tab of project properties
    // In order to make ReadEmbedded images properly working!

    // This class contains the plugin-level logic of the Loupedeck plugin.

    public partial class ObsStudioPlugin : Plugin
    {
        public readonly ObsAppProxy Proxy;

        // Gets a value indicating whether this is an Universal plugin or an Application plugin.
        public override Boolean UsesApplicationApiOnly => true;

        // Gets a value indicating whether this is an API-only plugin.
        public override Boolean HasNoApplication => true;

        private readonly ObsConnector _connector;

        public ObsStudioPlugin()
        {
            //FIXME FIXME: REMOVE THAT ONCE BUG WITH SERVICE IS FIXED
            this.SupportedDevices = DeviceType.LoupedeckCtFamily;

            this.Proxy = new ObsAppProxy();
            this._connector = new ObsConnector(this.Proxy, this.GetPluginDataDirectory(),
                                () => this.OnPluginStatusChanged(Loupedeck.PluginStatus.Warning, this.Localization.GetString("Connecting to OBS"), "https://support.loupedeck.com/obs-guide", ""));
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

            this.Proxy.AppConnected += this.OnAppConnStatusChange;
            this.Proxy.AppDisconnected += this.OnAppConnStatusChange;

            this._connector.Start();

            this.Update_PluginStatus();
        }

        // Unload is called once when plugin is being unloaded.
        public override void Unload()
        {
            this._connector.Stop();

            this.OnApplicationStopped(this, null);

            this.ClientApplication.ApplicationStarted -= this.OnApplicationStarted;
            this.ClientApplication.ApplicationStopped -= this.OnApplicationStopped;

            this.ClientApplication.ApplicationInstanceStarted -= this.OnApplicationStarted;
            this.ClientApplication.ApplicationInstanceStopped -= this.OnApplicationStopped;

            this.Proxy.AppConnected -= this.OnAppConnStatusChange;
            this.Proxy.AppDisconnected -= this.OnAppConnStatusChange;

            // this.Proxy = null;
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
            else if (this.Proxy != null && this.Proxy.IsAppConnected)
            {
                this.OnPluginStatusChanged(Loupedeck.PluginStatus.Normal, "");
            }
            else
            {
                this.OnPluginStatusChanged(Loupedeck.PluginStatus.Warning, "Not connected to OBS", "https://support.loupedeck.com/obs-guide", "more details");
            }
        }

        internal static readonly BitmapColor BitmapColorPink = new BitmapColor(255, 192, 203);

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
                Trace($"Cannot load image {imageName}, exception {ex}");
            }

            if (!String.IsNullOrEmpty(text))
            {
                var x1 = bitmapBuilder.Width * 0.1;
                var w = bitmapBuilder.Width * 0.8;
                var y1 = bitmapBuilder.Height * 0.65;
                var h = bitmapBuilder.Height * 0.3;

                bitmapBuilder.DrawText(text, (Int32)x1, (Int32)y1, (Int32)w, (Int32)h, selected ? BitmapColor.Black : BitmapColorPink, 13, 12, 1);
            }

            return bitmapBuilder;
        }

#if IMG_CACHE_ENABLED
        internal String MakeCacheKey(PluginImageSize imageSize, String imageName, String text, Boolean selected) => String.IsNullOrEmpty(text) ? $"S-{imageSize}-N{imageName}-T-NONE-S{selected}" : $"S-{imageSize}-N{imageName}-T{text}- S{selected}";
        private readonly Dictionary<String, Byte[]> _localImageCache = new Dictionary<String, Byte[]>();
#endif
        // Loupedeck.ObsPlugin.icons.
        public static readonly String ImageResPrefix = "Loupedeck.ObsStudioPlugin.Icons.";

        /// <summary>
        ///  Draws text over the bitmap. Bad location but in absence of the better components, put it here.
        /// </summary>
        /// <param name="imageSize">size of the image</param>
        /// <param name="imagePath">Image file name</param>
        /// <param name="text">text to render</param>
        /// <param name="textSelected">If true, darker color of text is chosen</param>
        /// <returns>bitmap with text rendered</returns>
        public BitmapImage GetPluginCommandImage(PluginImageSize imageSize, String imagePath, String text = null, Boolean textSelected = false)
        {
            var imageName = ImageResPrefix + imagePath;
#if IMG_CACHE_ENABLED
            var key = this.MakeCacheKey(imageSize, imageName, text, textSelected);
            if (!this._localImageCache.ContainsKey(key))
            {
                var builder = this.BuildImage(imageSize, imageName, text, textSelected);
                this._localImageCache.Add(key, builder.ToArray()); // return bitmapBuilder.ToImage();
            }
            return this._localImageCache[key].ToImage();
#else
            return this.BuildImage(imageSize, imageName, text, textSelected).ToImage();
#endif
        }

        public static void Trace(String line) => Tracer.Trace("GSP:" + line); /*System.Diagnostics.Debug.WriteLine(*/
    }
}
