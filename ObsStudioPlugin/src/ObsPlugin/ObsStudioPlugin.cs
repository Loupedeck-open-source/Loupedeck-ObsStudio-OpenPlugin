namespace Loupedeck.ObsStudioPlugin
{
    using System;
    using System.Collections.Generic;

    // NOTE: FOR RELEASE BUILD, 'optimization needs to be turned  off in the Build tab of project properties
    // In order to make ReadEmbedded images properly working!

    // This class contains the plugin-level logic of the Loupedeck plugin.

     partial class ObsStudioPlugin : Plugin
    {
        private readonly String SupportPageUrl = "https://support.logi.com/hc/articles/25522063648407-Other-Plugins-MX-Creative-Console#h_01J4V10DCG2M17YD4STPM3NXFS";
        internal static ObsAppProxy Proxy { get; private set; } 
        private ObsIniFile IniFile = null;
        // Gets a value indicating whether this is an Universal plugin or an Application plugin.
        public override Boolean UsesApplicationApiOnly => true;

        // Gets a value indicating whether this is an API-only plugin.
        public override Boolean HasNoApplication => true;

        //private readonly ObsConnector _connector;
        private const String PluginSettingServerURI = "PluginSettingServerURI";
        public String OBSServerURI { get; set; } = null;
        private const String PluginSettingServerPassword = "PluginSettingServerPassword";
        public String OBSServerPassword { get; set; } = null;
        private const String PluginSettingServerPort = "PluginSettingServerPort";
        public Int32 OBSServerPort { get; set; } = 0;
        public Boolean IsPluginConfigured { get; private set; } = false;
        public String OBSWebsocketServerLastDisconnectReason { get; set; } = "";


        public ObsStudioPlugin()
        {
            ObsStudioPlugin.Proxy = new ObsAppProxy(this);
            

            
        }
        public void UpdatePluginSettings(String serverUri, Int32 port, String password)
        {
            this.OBSServerURI = serverUri;
            this.OBSServerPort = port;
            this.OBSServerPassword = password;
            this.IsPluginConfigured = true;

            this.SetPluginSetting(PluginSettingServerURI, serverUri);
            this.SetPluginSetting(PluginSettingServerPort, port.ToString());
            this.SetPluginSetting(PluginSettingServerPassword, password);
            this.Log.Info($"UpdatePluginSettings: URI:{serverUri}, Port:{port}, Password.substring(0,5){password.Substring(0,4)}******");

            if(
                this.TryGetPluginSetting(PluginSettingServerURI, out var serverUri2) && 
                this.TryGetPluginSetting(PluginSettingServerPort, out var portValue2) && 
                this.TryGetPluginSetting(PluginSettingServerPassword, out var passwordValue2))
            {
                this.OBSServerURI = serverUri2;
                this.OBSServerPort = Int32.Parse(portValue2);
                this.OBSServerPassword =  passwordValue2;
                this.IsPluginConfigured = true;
                //this.Log.Info($"UpdatePluginSettings New Settings from Store: URI:{serverUri2}, Port:{portValue2}, Password.substring(0,5){passwordValue2.Substring(0,5)}");

            }
        }

        // Load is called once as plugin is being initialized during service start.
        public override void Load()
        {
                        

            if(
                this.TryGetPluginSetting(PluginSettingServerURI, out var serverUri) && 
                this.TryGetPluginSetting(PluginSettingServerPort, out var portValue) && 
                this.TryGetPluginSetting(PluginSettingServerPassword, out var passwordValue))
            {
                this.OBSServerURI = serverUri;
                this.OBSServerPort = Int32.Parse(portValue);
                this.OBSServerPassword =  passwordValue;
                this.IsPluginConfigured = true;


                this.Log.Info($"Load Settings from Store: URI:{serverUri}, Port:{portValue}, Password:{passwordValue.Substring(0,4)}******");
            }

            this.IniFile = new ObsIniFile(this);   

            var appActive = this.ClientApplication.IsActive() || this.ClientApplication.IsRunning();
            this.Info.Icon256x256 = EmbeddedResources.ReadImage("Loupedeck.ObsStudioPlugin.metadata.Icon256x256.png");

            this.ClientApplication.ApplicationStarted += this.OnApplicationStarted;
            this.ClientApplication.ApplicationStopped += this.OnApplicationStopped;

            this.ClientApplication.ApplicationInstanceStarted += this.OnApplicationStarted;
            this.ClientApplication.ApplicationInstanceStopped += this.OnApplicationStopped;

        
            ObsStudioPlugin.Proxy.AppConnected += this.OnAppConnected;
            ObsStudioPlugin.Proxy.AppDisconnected += this.OnAppDisconected;

            ObsStudioPlugin.Proxy.RegisterAppEvents();

            if (appActive)
            {
                this.OnApplicationStarted(this, null);
            }
            else if (this.ClientApplication.GetApplicationStatus() == ClientApplicationStatus.Installed
                && !this.IsPluginConfigured
                && this.IniFile.iniFileExists
                && !this.IniFile.iniFileGood
                )
            {
                this.Log.Info("Loupedeck is not yet configured. OBS Installed but INI file has no WebSocketServer settings");

                //Attempting to fix ini file if it is not good. Can only be done when app is not running (for Portable app we need to know its location first
                this.IniFile.FixIniFile();
            }

            this.Update_PluginStatus();
        }

        // Unload is called once when plugin is being unloaded.
        public override void Unload()
        {
            ObsStudioPlugin.Proxy.UnregisterAppEvents();

            this.OnApplicationStopped(this, null);

            this.ClientApplication.ApplicationStarted -= this.OnApplicationStarted;
            this.ClientApplication.ApplicationStopped -= this.OnApplicationStopped;

            this.ClientApplication.ApplicationInstanceStarted -= this.OnApplicationStarted;
            this.ClientApplication.ApplicationInstanceStopped -= this.OnApplicationStopped;

            ObsStudioPlugin.Proxy.AppConnected -= this.OnAppConnected;
            ObsStudioPlugin.Proxy.AppDisconnected -= this.OnAppDisconected;

        }

        private void OnAppConnected(Object sender, EventArgs e)
        {
            this.Log.Info("OnAppConnected");
            this.Update_PluginStatus();
        }

        private void OnAppDisconected(Object sender, EventArgs e)
        {
            this.Log.Info("OnAppDisconnected");
            //We'll re-read the ini file to see whether it is good or not
            this.IniFile.ReadIniFile();
            this.Update_PluginStatus();
        }

        
        private void OnApplicationStarted(Object sender, EventArgs e)
        {
            //Main entry point for the plugin's connectivity
            var status = this.ClientApplication.GetApplicationStatus();

            this.Log.Info($"OnApplicationStarted. Installation status:{status != ClientApplicationStatus.Installed}");
            this.Log.Info($"PluginConfig: this.IsPluginConfigured={this.IsPluginConfigured} PluginSettingServerURI={(this.TryGetPluginSetting(PluginSettingServerURI, out var settingValue) ? settingValue : "no PluginSettings available")} Ini exists/good: {this.IniFile.iniFileExists}/{this.IniFile.iniFileGood}  ");

            if (!this.IniFile.iniFileGood && status != ClientApplicationStatus.Installed)
            {
                this.Log.Info("Portable mode detected");

                //FIXME: There needs to be more sophisticated logic
                this.IniFile.SetPortableIniPath(this.ClientApplication.GetRunningProcessName());
            }

            //Here we can detect if application is running in the portable mode (runnign but not installed) and adjust ini file accordingly 

            if (this.IniFile.iniFileGood || this.IsPluginConfigured)
            {
                this.Log.Info($"Connecting using the data from IniFile (port {this.IniFile.ServerPort})");
                const UInt32 MAX_ATTEMPTS = 20;
                var attempt = 0;
                //Oftentimes we receive 'application started' notification too soon for the target app to be capable of accepting connections
                // Firstly we need to wait for the port to be listening
                while( Loupedeck.NetworkHelpers.IsTcpPortFree(this.IniFile.ServerPort) )
                {
                    if (attempt ++ > MAX_ATTEMPTS)
                    {
                        this.Log.Error($"Port is not listening after {MAX_ATTEMPTS} attempts, giving up");
                        break;
                    }
                    this.Log.Info($"Port is not yet listening, waiting for 1s");
                    System.Threading.Thread.Sleep(1000);
                }
                //And we sleep some more to make sure that the app is ready to accept connections
                System.Threading.Thread.Sleep(2000);

                //
                if (!Helpers.TryExecuteAction(() => Proxy.ConnectAsync($"ws://{this.OBSServerURI}:{this.OBSServerPort}", this.OBSServerPassword)))
                {
                    this.Log.Error("OBS: Error connecting to OBS");
                }
            }
            else if (this.IniFile.iniFileExists) //Means that ini file is bad 
            {
                this.Update_PluginStatus();
            }
        }

        private void OnApplicationStopped(Object sender, EventArgs e)
        {
            this.Log.Info("OnApplicationStopped");
            if (!this.IsPluginConfigured && !this.IniFile.iniFileGood && this.IniFile.iniFileExists)
            {
                this.Log.Info("Fixing Ini file");
                this.IniFile.FixIniFile();
            }
        }

        private void Update_PluginStatus()
        {
            if (!this.IsApplicationInstalled() && !this.ClientApplication.IsRunning())
            {
                this.OnPluginStatusChanged(Loupedeck.PluginStatus.Error, "OBS Studio is not installed", this.SupportPageUrl, "more details");
            }
            else if (ObsStudioPlugin.Proxy.IsAppConnected)
            {
                this.OnPluginStatusChanged(Loupedeck.PluginStatus.Normal, "");
            }
            else if (this.IsPluginConfigured && !ObsStudioPlugin.Proxy.IsAppConnected)
            {
                if(this.OBSWebsocketServerLastDisconnectReason == "Authentication failed.")
                {
                    this.IniFile.FixIniFile();
                    this.OnPluginStatusChanged(Loupedeck.PluginStatus.Warning, $"We've reset your OBS Websocket Server Settings in the OBS Ini, please restart OBS Studio.");
                }
            }
            else if (this.IniFile.iniFileExists && !this.IniFile.iniFileGood) 
            {
                //If ini is not good we can set up the 'on app stopped' watch to modify file
                this.OnPluginStatusChanged(Loupedeck.PluginStatus.Error, $"Cannot connect to OBS Studio. You might try restarting OBS Studio and trying again.", this.SupportPageUrl, "more details");
            }
            else
            {
                // FIXME: We need more elaborate explanation here. 
                this.OnPluginStatusChanged(Loupedeck.PluginStatus.Warning, "Not connected to OBS", this.SupportPageUrl, "more details");
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
    }
}
