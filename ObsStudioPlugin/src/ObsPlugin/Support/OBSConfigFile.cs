namespace Loupedeck.ObsStudioPlugin {
    using System.Collections.Generic;
    using System;
    using System.Dynamic;
    using System.IO;
    
    class OBSConfigFile {
        
        private const String DEFAULT_SERVER = "localhost";
        private const Int32 DEFAULT_PORT = 4455;
        private const String CONFIG_FILE_NAME = "config.json";
        private const String REPLACED_PASSWORD = "*****";
        public ObsStudioPlugin Plugin { get; private set; }

        public OBSConfigFile(ObsStudioPlugin parent) {
            this.Plugin = parent;
            this.LoadConfigFile();
            
        }

        private String GetConfigFilePath() => Path.Combine(this.Plugin.GetPluginDataDirectory(), CONFIG_FILE_NAME);

        public Boolean LoadConfigFile() {
            if (!IoHelpers.FileExists(this.GetConfigFilePath())) {
                this.CreateTemplateConfigFile();
                return false;
            }   
            String configFile = IoHelpers.ReadTextFile(this.GetConfigFilePath());
            dynamic config = new ExpandoObject();
            config = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(configFile);
            
            if (config != null) {
                if (config.serverPort is Int32 && config.server is String && config.password is String) {
                    if (config.password != REPLACED_PASSWORD) {
                    this.Plugin.UpdatePluginSettings(config.server, config.serverPort, config.password);
                    this.SaveConfigFile(config.server, config.serverPort, REPLACED_PASSWORD);
                    return true;
                    } else {
                       // config file is already loaded, maybe we should inform the user.
                    
                    }
                }else {
                    // config file is not valid, create a new one
                    this.Plugin.Log.Warning($"Config file ({CONFIG_FILE_NAME}) is not valid, creating a new one. Please update the settings. at: " + this.GetConfigFilePath());
                }
            } else {
                // no config file, create a new one
                this.Plugin.Log.Warning($"No {CONFIG_FILE_NAME} , creating a new one. Please update the settings. at: " + this.GetConfigFilePath());
            }
            this.CreateTemplateConfigFile();

            
            

            

            return false;
            
        }

        public Boolean SaveConfigFile(String server, Int32 port, String password) {
            dynamic config = new ExpandoObject();
            config.server = server;
            config.serverPort = port;
            config.password = password;
            String json = Newtonsoft.Json.JsonConvert.SerializeObject(config);
            IoHelpers.WriteTextFile(CONFIG_FILE_NAME, json);
            return true;
        }

        public Boolean CreateTemplateConfigFile() {
            if (!IoHelpers.FileExists(CONFIG_FILE_NAME)) {
                IoHelpers.CreateEmptyFile(CONFIG_FILE_NAME);
            
            }
            return IoHelpers.WriteTextFile(CONFIG_FILE_NAME, "{serverPort: 4455, server: \"localhost\", password: \"\"}");
            
        }
    }
}