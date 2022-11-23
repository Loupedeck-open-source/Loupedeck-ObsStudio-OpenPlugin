namespace Loupedeck.ObsStudioPlugin
{
    using System;
    using System.IO;
    using System.Linq;

    using Loupedeck.ObsStudioPlugin.Actions;

    using Newtonsoft.Json.Linq;

    public class DynamicSceneCols : PluginDynamicCommand
    {
        private ObsAppProxy Proxy => (this.Plugin as ObsStudioPlugin).Proxy;

        //Note DeviceTypeNone -- so that actions is not visible in the UI' action tree.
        public DynamicSceneCols()
            : base(displayName: "LegacyCollectionsAction",
                   description: "",
                   groupName: "",
                   DeviceType.None) => this.Name = "DynamicSceneCols";

        private readonly StringDictionaryNoCase _collection_guid_name_map = new StringDictionaryNoCase();

        private void ReadLegacyMapping()
        {
            /* Sample data file, Stored in Plugin data/Data/sc_cols.json
{
"897c424e-06be-4fe8-9105-45a7cf7d08e6": "Scene collection 1",
"91d36ef5-8c9e-4509-a798-8da6a48e5810": "Small",
"37ef40b1-7a93-4357-b391-f8e2c16cbd20": "Tournament",
"4f368e42-cc63-415b-8a43-f83b77a86cc0": "Was Wunderbar"
}       
*/

            //here we need to read a json file from app dir that might have the association between scene collection
            //GUID and scene collection name
            var filepath = Path.Combine(this.Plugin.GetPluginDataDirectory(), "Data", "sc_cols.json");
            if (File.Exists(filepath))
            {
                this._collection_guid_name_map.Clear();
                try
                {
                    var items = JObject.Parse(File.ReadAllText(filepath)).Properties().ToList();
                    foreach (var item in items)
                    {
                        this._collection_guid_name_map.Add(item.Name, item.Value.ToString());
                    }
                }
                catch (Exception ex)
                {
                    ObsStudioPlugin.Trace($"Exception {ex} reading legacy strings file");
                }
            }

        }

        protected override Boolean OnLoad()
        {
            this.Proxy.AppEvtCurrentSceneCollectionChanged += this.OnCurrentSceneCollectionChanged;
            this.Proxy.AppConnected += this.OnAppConnected;
            this.Proxy.AppDisconnected += this.OnAppDisconnected;
            this.IsEnabled = false;

            this.ReadLegacyMapping();
            return true;
        }

        protected override Boolean OnUnload()
        {
            this.Proxy.AppEvtCurrentSceneCollectionChanged -= this.OnCurrentSceneCollectionChanged;
            this.Proxy.AppConnected -= this.OnAppConnected;
            this.Proxy.AppDisconnected -= this.OnAppDisconnected;
            return true;
        }

        private void OnAppConnected(Object sender, EventArgs e) => this.IsEnabled = true;

        private void OnAppDisconnected(Object sender, EventArgs e) => this.IsEnabled = false;

        private void OnCurrentSceneCollectionChanged(Object sender, EventArgs e)
        {
            
            var arg = e as ObsAppProxy.OldNewStringChangeEventArgs;
            //unselecting old and selecting new
            if (!String.IsNullOrEmpty(arg.Old))
            {
               
                this.ActionImageChanged(arg.Old);
            }

            if (!String.IsNullOrEmpty(arg.New))
            {
                this.ActionImageChanged(arg.New);
            }

            this.ActionImageChanged();

            ObsStudioPlugin.Trace($"DynamicSceneCols: OnCurrentSceneCollectionChanged: old {arg.Old}, new {arg.New} ");
        }

        internal String GetSceneCollectionName(String actionParameter) => 
                !String.IsNullOrEmpty(actionParameter) && this._collection_guid_name_map.ContainsKey(actionParameter) ? this._collection_guid_name_map[actionParameter] : "Unknown";

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var imageName = SceneCollectionSelectCommand.IMGCollectionUnselected;
            var collection = this.GetSceneCollectionName(actionParameter);

            if (this.Proxy.SceneCollections.Contains(collection))
            {
                imageName = collection.Equals(this.Proxy.CurrentSceneCollection) ? SceneCollectionSelectCommand.IMGCollectionSelected : SceneCollectionSelectCommand.IMGCollectionUnselected;
            }

            ObsStudioPlugin.Trace($"DynamicSceneCols:Collection {collection}, Selected? { imageName == SceneCollectionSelectCommand.IMGCollectionSelected }");

            return (this.Plugin as ObsStudioPlugin).GetPluginCommandImage(imageSize, imageName, collection, imageName == SceneCollectionSelectCommand.IMGCollectionSelected);
        }

        protected override void RunCommand(String actionParameter) =>  this.Proxy.AppSwitchToSceneCollection(this.GetSceneCollectionName(actionParameter));

    }
}
