
namespace Loupedeck.ObsStudioPlugin
{
    using System;
    using System.IO;
    using System.Text.Json;
    using System.Text.Json.Nodes;


    /*
     JSON file state:
        Server enabled  (E)
        Server disabled (D)

    OBS state:
        Running (R)
        NOT running (N)

    OBS installation state:
        Installed (I)
        NOT installed OR portable (U)


    if R and E ->  Connect

    NOTE: During installation, if OBS is I, we try fixing JSON FILE
    if R and D ->  Monitor ApplicationClose and fix the JSON file when OBS is closed  . Set appropriate status ("Please restart OBS"). 
    if N and I ->  IF D, fix JSON FILE

    if R and U and D -> Portable mode -> Detect JSON File location and monitor AppClose. Set appropriate status. 
    if N and U -> EIther not there or portable mode -> Set appropriate status. ("Cannot detect OBS installation")
     */

    class OBSWebSocketServerJSON
    {

        private const String DEFAULT_PASSWORD = "NeverUseThis";
        private const Int32 DEFAULT_PORT = 4455;
        public ObsStudioPlugin Plugin { get; private set; }

        public String ServerPassword { get; private set; } = DEFAULT_PASSWORD;
        public Int32 ServerPort { get; private set; } = DEFAULT_PORT;

        // True if jsonFileExists
        public Boolean jsonFileExists { get; private set; } = false;

        // True if we can get all settings from WebServer Json file. 
        public Boolean jsonFileGood { get; private set; } = false;

        private String __jsonFilePath = null;
        private String _jsonFilePath
        { 
            get => this.__jsonFilePath;

            set
            {
                this.__jsonFilePath = value; 
                this.ReadJsonFile();
            }
        }

        public void SetPortableJsonPath(String processPath)
        {
            //FIXME: Different for MAC
            // Get the directory of the file
            var directoryInfo = new DirectoryInfo(Path.GetDirectoryName(processPath));

            // Move up two levels
            if (directoryInfo.Parent != null && directoryInfo.Parent.Parent != null)
            {
                var twoLevelsUp = directoryInfo.Parent.Parent;
                //this._iniFilePath  = $"{twoLevelsUp.FullName}\\config\\obs-studio\\global.ini";
                this._jsonFilePath = $"{twoLevelsUp.FullName}\\config\\obs-studio\\plugin_config\\obs-websocket\\config.json";
                this.Plugin.Log.Info($"Found a path for portable installation, {this._jsonFilePath}");
            }
            else
            {
                this.Plugin.Log.Error($"Cannot derive WebServer Json file path from portable process path {processPath}");
            }
        }

        public OBSWebSocketServerJSON(ObsStudioPlugin parent)
        {
            this.Plugin = parent;  
            
            this._jsonFilePath = Helpers.IsWindows() 
                            ? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\obs-studio\\plugin_config\\obs-websocket\\config.json"
                            : Environment.GetEnvironmentVariable("HOME") + "/Library/Application Support/obs-studio/plugin_config/obs-websocket/config.json";
            this.Plugin.Log.Info($"Will try OBS Ini at {this._jsonFilePath}");
        }

        public void ReadJsonFile()
        {
            this.jsonFileGood = false;
            this.jsonFileExists = File.Exists(this._jsonFilePath);
            if (!this.jsonFileExists)
            {
                return;
            }

            var jsonString = File.ReadAllText(this._jsonFilePath);
            using (var document = JsonDocument.Parse(jsonString))
            {
                JsonElement root = document.RootElement;

                var authRequired = root.GetProperty("auth_required").GetBoolean();
                var serverEnabled = root.GetProperty("server_enabled").GetBoolean();
                this.ServerPassword = root.GetProperty("server_password").GetString();
                this.ServerPort = root.GetProperty("server_port").GetInt32();

                this.Plugin.Log.Info($"Read init file: serverEnabled:{serverEnabled}, authRequired:{authRequired}");
                this.jsonFileGood = serverEnabled && authRequired /*Hypothetically we need to ensure password is non-zero*/;
            }
        }

        private readonly Random random = new Random();
        //Generated 20 characters long random string
        private String GenerateServerPassword()
        {
            const Int32 length = 20;
            var s = "";

            for (var i = 0; i < length; i++)
            {
                s += (Char)this.random.Next(32, 127);
            }

            return s;
        }

        public Boolean FixJsonFile()
        {
            if (!this.jsonFileExists)
            {
                this.Plugin.Log.Error($"Cannot fix OBS WebServer Json file: File not exist ");
                return false;
            }

            if (this.jsonFileGood)
            {
                this.Plugin.Log.Info($"File is already good, no need to fix");
                return true;
            }

            this.Plugin.Log.Info($"Fixing WebServer Json file");

            try
            {
                // Read the entire JSON file into a string
                var jsonString = File.ReadAllText(this._jsonFilePath);

                // Parse the JSON string into a JsonNode for easier manipulation
                var rootNode = JsonNode.Parse(jsonString)!;

                var serverPassword = "";
                if (rootNode["server_password"] != null)
                {
                    serverPassword = rootNode["server_password"]!.GetValue<String>();
                }
                if (serverPassword.IsNullOrEmpty())
                {
                    rootNode["server_password"] = this.GenerateServerPassword();
                }

                var serverPort = 0;
                if (rootNode["server_port"] != null)
                {
                    serverPort = rootNode["server_port"]!.GetValue<Int32>();
                }
                if (serverPort == 0)
                {
                    rootNode["server_port"] = DEFAULT_PORT;
                }

                rootNode["auth_required"] = true;
                rootNode["server_enabled"] = true;

                File.WriteAllText(this._jsonFilePath, rootNode.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                this.ReadJsonFile();
                return true;
            }
            catch (Exception ex)
            {
                this.Plugin.Log.Error($"An error occurred: {ex.Message}");
                return false;
            }
        }
    }
}
