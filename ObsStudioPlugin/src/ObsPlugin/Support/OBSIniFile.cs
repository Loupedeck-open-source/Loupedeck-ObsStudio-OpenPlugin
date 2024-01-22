
namespace Loupedeck.ObsStudioPlugin
{
    using System.Collections.Generic;
    using System;
    using System.Dynamic;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.CodeDom;


    /*
     Ini file state:
        Server enabled  (E)
        Server disabled (D)

    OBS state:
        Running (R)
        NOT running (N)

    OBS installation state:
        Installed (I)
        NOT installed OR portable (U)


    if R and E ->  Connect

    NOTE: During installation, if OBS is I, we try fixing INI FILE
    if R and D ->  Monitor ApplicationClose and fix the INI file when OBS is closed  . Set appropriate status ("Please restart OBS"). 
    if N and I ->  IF D, fix INI FILE

    if R and U and D -> Portable mode -> Detect INI File location and monitor AppClose. Set appropriate status. 
    if N and U -> EIther not there or portable mode -> Set appropriate status. ("Cannot detect OBS installation")
             
     
     
     
     */

    class ObsIniFile
    {

        private const String DEFAULT_PASSWORD = "NeverUseThis";
        private const Int32 DEFAULT_PORT = 4444;
        private const String OBSINIKey= "OBSWebSocket";
        public ObsStudioPlugin Plugin { get; private set; }

        public String ServerPassword { get; private set; } = DEFAULT_PASSWORD;
        public Int32 ServerPort { get; private set; } = DEFAULT_PORT;

        // True if iniFileExists
        public Boolean iniFileExists { get; private set; } = false;

        // True if we can get all settings from Ini file. 
        public Boolean iniFileGood { get; private set; } = false;

        private String __iniFilePath = null;
        private String _iniFilePath { 
            get => this.__iniFilePath;

            set
            {
                this.__iniFilePath = value; 
                this.ReadIniFile();
            }
        }

        public void SetPortableIniPath(String processPath)
        {
            //FIXME: Different for MAC
            // Get the directory of the file
            var directoryInfo = new DirectoryInfo(Path.GetDirectoryName(processPath));

            // Move up two levels
            if (directoryInfo.Parent != null && directoryInfo.Parent.Parent != null)
            {
                var twoLevelsUp = directoryInfo.Parent.Parent;
                this._iniFilePath  = $"{twoLevelsUp.FullName}\\config\\obs-studio\\global.ini";
                this.Plugin.Log.Info($"Found a path for portable installation, {this._iniFilePath}");
            }
            else
            {
                this.Plugin.Log.Error($"Cannot derive ini file path from portable process path {processPath}");
            }

        }

        public ObsIniFile(ObsStudioPlugin parent)
        {
            this.Plugin = parent;  
            //Default path.  
            // WARNINIG: Different for MAC

            this._iniFilePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\obs-studio\\global.ini";

        }

        private void ReadIniFile()
        {
            this.iniFileGood = false;
            this.iniFileExists = File.Exists(this._iniFilePath);
            if (!this.iniFileExists)
            {
                return;
            }

            var ini = new IniFile(this._iniFilePath);
            this.ServerPassword = ini.GetValue(OBSINIKey, "ServerPassword", DEFAULT_PASSWORD);
            var port_s = $"{DEFAULT_PORT}";
            this.ServerPort = Int32.TryParse(ini.GetValue(OBSINIKey, "ServerPort", DEFAULT_PORT.ToString()), out var port) ? port : DEFAULT_PORT;

            var serverEnabled = ini.GetValue(OBSINIKey, "ServerEnabled", "false");
            var authRequired = ini.GetValue(OBSINIKey, "AuthRequired", "false");

            this.iniFileGood = serverEnabled.EqualsNoCase("true") && authRequired.EqualsNoCase("true") /*Hypothetically we need to ensure password is non-zero*/;

            /*FirstLoad = false
            ServerEnabled = true
            ServerPort = 4455
            AlertsEnabled = false
            AuthRequired = true
            ServerPassword = 111222333
            */
        }

        public void FixIniFile()
        {
            if (!this.iniFileExists)
            {
                this.Plugin.Log.Error($"Cannot fix OBS ini file: File not exist ");
                return;
            }

            //Here we try fixing the file and write new OBSINIKey section, generate password etc.
#if false
            var ini = new IniFile(this._iniFilePath);
            this.ServerPassword = ini.GetValue(OBSINIKey, "ServerPassword", DEFAULT_PASSWORD);
            
            var port_s = $"{DEFAULT_PORT}";
            this.ServerPort = Int32.TryParse(ini.GetValue(OBSINIKey, "ServerPort", DEFAULT_PORT.ToString()), out var port) ? port : DEFAULT_PORT;
#endif
        }


        class IniFile
        {

            private readonly Dictionary<String, Dictionary<String, String>> ini = new Dictionary<String, Dictionary<String, String>>(StringComparer.InvariantCultureIgnoreCase);

            public IniFile(String file)
            {
                var txt = File.ReadAllText(file);

                var currentSection = new Dictionary<String, String>(StringComparer.InvariantCultureIgnoreCase);

                this.ini[""] = currentSection;

                foreach (var l in txt.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var line = l.Trim();
                    if (line.StartsWith(";"))
                    {
                        continue;
                    }

                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        currentSection = new Dictionary<String, String>(StringComparer.InvariantCultureIgnoreCase);
                        this.ini[line.Substring(1, line.LastIndexOf("]") - 1)] = currentSection;
                        continue;
                    }

                    var idx = line.IndexOf("=");
                    if (idx == -1)
                    {
                        continue;
                    }

                    currentSection[line.Substring(0, idx).Trim()] = line.Substring(idx + 1).Trim();
                }
            }

            public String GetValue(String section,  String key = "",  String defaultValue = "") => this.ini.ContainsKey(section) && this.ini[section].ContainsKey(key) ? this.ini[section][key] : defaultValue;
        }
    }


}
