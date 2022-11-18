namespace Loupedeck.ObsStudioPlugin
{
    using System;

    // NOTE: FOR RELEASE BUILD, 'optimization needs to be turned  off in the Build tab of project properties
    // In order to make ReadEmbedded images properly working!

    // This class contains the plugin-level logic of the Loupedeck plugin.

    public class ObsStudioPlugin : Plugin
    {
        public readonly ObsAppProxy Proxy;

        // Gets a value indicating whether this is an Universal plugin or an Application plugin.
        public override Boolean UsesApplicationApiOnly => true;

        // Gets a value indicating whether this is an API-only plugin.
        public override Boolean HasNoApplication => true;

        private readonly ObsConnector _connector;

        public ObsStudioPlugin()
        {
            this.Proxy = new ObsAppProxy();
            this._connector = new ObsConnector(this.Proxy, this.GetPluginDataDirectory() + "\\..\\ObsStudio",
                                () => this.OnPluginStatusChanged(Loupedeck.PluginStatus.Warning, this.Localization.GetString("Connecting to OBS"), "https://support.loupedeck.com/obs-guide", ""));
        }

        // Load is called once as plugin is being initialized during service start.
        public override void Load()
        {
            this.ClientApplication.ApplicationStarted += this.OnApplicationStarted;
            this.ClientApplication.ApplicationStopped += this.OnApplicationStopped;

            this.ClientApplication.ApplicationInstanceStarted += this.OnApplicationStarted;
            this.ClientApplication.ApplicationInstanceStopped += this.OnApplicationStopped;

            this.Proxy.EvtAppConnected += this.OnAppConnStatusChange;
            this.Proxy.EvtAppDisconnected += this.OnAppConnStatusChange;

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

            this.Proxy.EvtAppConnected -= this.OnAppConnStatusChange;
            this.Proxy.EvtAppDisconnected -= this.OnAppConnStatusChange;

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

        static readonly BitmapColor BitmapColorPink = new BitmapColor(255, 192, 203);

        /// <summary>
        ///  Draws text over the bitmap. Bad location but in absence of the better components, put it here.
        /// </summary>
        /// <param name="imageSize">size of the image</param>
        /// <param name="imageName">Image file name</param>
        /// <param name="text">text to render</param>
        /// <returns>bitmap with text rendered</returns>
        internal BitmapImage NameOverBitmap(PluginImageSize imageSize, String imageName, String text, Boolean selected=false)
        {
            
            using (var bitmapBuilder = new BitmapBuilder(imageSize))
            {
                bitmapBuilder.DrawImage(EmbeddedResources.ReadImage(imageName));

                if (!String.IsNullOrEmpty(text))
                {
                    var x1 = bitmapBuilder.Width * 0.1;
                    var w  = bitmapBuilder.Width * 0.8;
                    var y1 = bitmapBuilder.Height * 0.65;
                    var h  = bitmapBuilder.Height * 0.3;

                    bitmapBuilder.DrawText(text, (Int32)x1, (Int32)y1, (Int32)w, (Int32)h,selected ? BitmapColor.Black : BitmapColorPink,13,12,1) ;
                }

                return bitmapBuilder.ToImage();
            }
        }

        //Loupedeck.ObsPlugin.icons.
        public static readonly String imageResPrefix = "Loupedeck.ObsPlugin.icons.";

        //TODO IMPLEMENT CACHE HERE
        public BitmapImage GetPluginCommandImage(PluginImageSize imageSize, String imageName, String text = null, Boolean textSelected = false) => text != null ? this.NameOverBitmap(imageSize, imageResPrefix + imageName, text, textSelected) : EmbeddedResources.ReadImage(imageResPrefix + imageName);

        public static void Trace(String line) => Tracer.Trace("GSP:" + line); /*System.Diagnostics.Debug.WriteLine(*/
    }
}
