namespace Loupedeck.ObsStudioPlugin.Actions
{
    using System;

    internal class SourceFilterCommand : PluginMultistateDynamicCommand
    {
        public const String IMGEnabled = "FilterOn.png";
        public const String IMGDisabled = "FilterOff.png";
        public const String IMGInaccessible = "FilterDisabled.png";
        public const String NameUnknown = "UnknownFilter";

        private const Int16 STATE_DISABLED = 0;
        private const Int16 STATE_ENABLED = 1;

        public SourceFilterCommand()
        {
            this.Description = "Enables/Disables Source Filter";
            this.GroupName = "5. Filters";
            _ = this.AddState("Disabled", "Filter disabled");
            _ = this.AddState("Enabled", "Filter enabled");
        }

        protected override Boolean OnLoad()
        {
            this.IsEnabled = false;

            ObsStudioPlugin.Proxy.AppConnected += this.OnAppConnected;
            ObsStudioPlugin.Proxy.AppDisconnected += this.OnAppDisconnected;

            ObsStudioPlugin.Proxy.AppEvtSceneListChanged += this.OnSceneListChanged;
            ObsStudioPlugin.Proxy.AppEvtCurrentSceneChanged += this.OnCurrentSceneChanged;
            ObsStudioPlugin.Proxy.AppEvtSceneNameChanged += this.OnSceneListChanged; //Note using same handler since we just re-generate params
            ObsStudioPlugin.Proxy.AppSceneItemRenamed += this.OnSceneListChanged;

            ObsStudioPlugin.Proxy.AppEvtSceneItemVisibilityChanged += this.OnSceneItemVisibilityChanged; //All filters go to 'off' if item is not visible
            
            ObsStudioPlugin.Proxy.AppEvtSourceFilterCreated += this.OnSourceFilterCreated;
            ObsStudioPlugin.Proxy.AppEvtSourceFilterRemoved += this.OnSourceFilterRemoved;
            ObsStudioPlugin.Proxy.AppEvtSourceFilterRenamed += this.OnSourceFilterRenamed;
            ObsStudioPlugin.Proxy.AppEvtSourceFilterEnableStateChanged += this.OnSourceFilterEnableStateChanged;


            //ObsStudioPlugin.Proxy.AppEvtSceneItemAdded += this.OnSceneItemAdded;
            ObsStudioPlugin.Proxy.AppEvtSceneItemRemoved += this.OnSceneItemRemoved;

            //ObsStudioPlugin.Proxy.AppInputRenamed += this.OnSourceRenamed;


            this.OnAppDisconnected(this, null);

            return true;
        }

        protected override Boolean OnUnload()
        {
            ObsStudioPlugin.Proxy.AppConnected -= this.OnAppConnected;
            ObsStudioPlugin.Proxy.AppDisconnected -= this.OnAppDisconnected;

            ObsStudioPlugin.Proxy.AppEvtSceneListChanged -= this.OnSceneListChanged;
            ObsStudioPlugin.Proxy.AppEvtCurrentSceneChanged -= this.OnCurrentSceneChanged;
            ObsStudioPlugin.Proxy.AppEvtSceneNameChanged -= this.OnSceneListChanged;
            ObsStudioPlugin.Proxy.AppSceneItemRenamed -= this.OnSceneListChanged;

            ObsStudioPlugin.Proxy.AppEvtSceneItemVisibilityChanged -= this.OnSceneItemVisibilityChanged;

            ObsStudioPlugin.Proxy.AppEvtSourceFilterCreated -= this.OnSourceFilterCreated;
            ObsStudioPlugin.Proxy.AppEvtSourceFilterRemoved -= this.OnSourceFilterRemoved;
            ObsStudioPlugin.Proxy.AppEvtSourceFilterRenamed -= this.OnSourceFilterRenamed;
            ObsStudioPlugin.Proxy.AppEvtSourceFilterEnableStateChanged -= this.OnSourceFilterEnableStateChanged;

            //ObsStudioPlugin.Proxy.AppEvtSceneItemAdded -= this.OnSceneItemAdded;
            ObsStudioPlugin.Proxy.AppEvtSceneItemRemoved -= this.OnSceneItemRemoved;

            //ObsStudioPlugin.Proxy.AppInputRenamed -= this.OnSourceRenamed;


            return true;
        }

        private void OnSourceFilterCreated(Object sender, SourceFilterEventArgs args)
        {
            if (ObsStudioPlugin.Proxy.CurrentAudioSources.ContainsKey(args.SourceName))
            {
                //Not ours, handled by GlobalAudioFilterCommand
                return;
            }

            var key = new SourceFilterKey(ObsStudioPlugin.Proxy.CurrentSceneCollection, ObsStudioPlugin.Proxy.CurrentSceneName, -1, args.SourceName, args.FilterName);

            if (ObsStudioPlugin.Proxy.AllSceneItems.ContainsKey(key.StringizeAsItemKey()))
            {
                //We assume that corresponding Filters element is already in
                this.AddSourceFilterParameter(ObsStudioPlugin.Proxy.CurrentSceneName, -1, args.SourceName, args.FilterName);
                this.ParametersChanged();
            }
            else
            {
                this.Plugin.Log.Warning($"OnGlobalSourceFilterCreated: Cannot find Scene item  by key {key.StringizeAsItemKey()} ");
            }
        }

        private void OnSourceFilterRemoved(Object sender, SourceFilterEventArgs args)
        {
            if (ObsStudioPlugin.Proxy.CurrentAudioSources.ContainsKey(args.SourceName))
            {
                //Not ours, handled by GlobalAudioFilterCommand
                return;
            }

            var sfk = new SourceFilterKey(ObsStudioPlugin.Proxy.CurrentSceneCollection, ObsStudioPlugin.Proxy.CurrentSceneName, -1, args.SourceName, args.FilterName);

            if (ObsStudioPlugin.Proxy.AllSceneItems.ContainsKey(sfk.StringizeAsItemKey()))
            {
                this.RemoveParameter(sfk.Stringize());
                this.ParametersChanged();
            }
        }

        private void OnSourceFilterRenamed(Object sender, SourceFilterRenamedArgs args)
        {
            if (ObsStudioPlugin.Proxy.CurrentAudioSources.ContainsKey(args.SourceName))
            {
                //Not ours, handled by GlobalAudioFilterCommand
                return;
            }

            //arg.FilterName is the old name
            var sfk = new SourceFilterKey(ObsStudioPlugin.Proxy.CurrentSceneCollection, ObsStudioPlugin.Proxy.CurrentSceneName, -1, args.SourceName, args.FilterName);
            if (ObsStudioPlugin.Proxy.AllSceneItems.ContainsKey(sfk.StringizeAsItemKey()))
            {
                this.Plugin.Log.Info($"OnSourceFilterRenamed: Removing {args.FilterName} and adding {args.NewFilterName}");

                this.RemoveParameter(sfk.Stringize());
                this.AddSourceFilterParameter(ObsStudioPlugin.Proxy.CurrentSceneName, -1, args.SourceName, args.NewFilterName);
                this.ParametersChanged();
            }
        }

        private void OnSourceFilterEnableStateChanged(Object sender, SourceFilterEventArgs args)
        {
            if (ObsStudioPlugin.Proxy.CurrentAudioSources.ContainsKey(args.SourceName))
            {
                //Not ours, handled by GlobalAudioFilterCommand
                return;
            }

            var sfk = new SourceFilterKey(ObsStudioPlugin.Proxy.CurrentSceneCollection, ObsStudioPlugin.Proxy.CurrentSceneName, -1, args.SourceName, args.FilterName);

            if (ObsStudioPlugin.Proxy.AllSceneItems.ContainsKey(sfk.StringizeAsItemKey()))
            {
                //The state is already in the values
                this.SetCurrentState(sfk.Stringize(), ObsStudioPlugin.Proxy.AllSceneItems[sfk.StringizeAsItemKey()].Filters[args.FilterName].Enabled  ? STATE_ENABLED : STATE_DISABLED);
            }
            else
            {
                this.Plugin.Log.Info($"OnSourceFilterEnableStateChanged key not found ");
            }
        }

        protected override void RunCommand(String actionParameter)
        {
            ObsStudioPlugin.Proxy.AppSourceFilterEnableToggle(actionParameter);
        } 

        private void OnSceneListChanged(Object sender, EventArgs e) => this.ResetParameters(true);

        private void OnCurrentSceneChanged(Object sender, EventArgs e) => this.ActionImageChanged();

        /*
        we assume we get Create event through filters, so we don't need to listen to these events
        private void OnSceneItemAdded(Object sender, SceneItemArgs arg)
        {
            var s = SceneItemKey.Encode(ObsStudioPlugin.Proxy.CurrentSceneCollection, arg.SceneName, arg.ItemId, arg.ItemName);
            if (ObsStudioPlugin.Proxy.AllSceneItems.ContainsKey(s) && ObsStudioPlugin.Proxy.AllSceneItems[s].Filters.Count > 0)
            {
                this.AddFilter(arg.SceneName, arg.ItemName, arg.ItemId);
                this.ParametersChanged();
            }
        }
        */
        private void OnSceneItemRemoved(Object sender, SceneItemArgs arg)
        {

            var s = SceneItemKey.Encode(ObsStudioPlugin.Proxy.CurrentSceneCollection, arg.SceneName, arg.ItemId,arg.ItemName);
            if (ObsStudioPlugin.Proxy.AllSceneItems.ContainsKey(s) && ObsStudioPlugin.Proxy.AllSceneItems[s].Filters.Count > 0)
            {
                foreach(var filter in ObsStudioPlugin.Proxy.AllSceneItems[s].Filters)
                {
                    var sfk = SourceFilterKey.Encode(ObsStudioPlugin.Proxy.CurrentSceneCollection, arg.SceneName, arg.ItemId, arg.ItemName, filter.Value.FilterName);
                    this.RemoveParameter(sfk);
                }

                this.ParametersChanged();
            }
        }
        
        //Note: We can possibly do cherry-picking on the parameters but that require quite a bit of code. 
        private void OnSourceRenamed(Object sender, OldNewStringChangeEventArgs args) => this.ResetParameters(true);

        private void OnAppConnected(Object sender, EventArgs e) => this.IsEnabled = true;

        private void OnAppDisconnected(Object sender, EventArgs e)
        {
            this.IsEnabled = false;

            this.ResetParameters(false);
            this.ActionImageChanged();
        }

        protected void OnSceneItemVisibilityChanged(Object sender, SceneItemVisibilityChangedArgs arg)
        {
            var actionParameter = SceneItemKey.Encode(ObsStudioPlugin.Proxy.CurrentSceneCollection, arg.SceneName, arg.ItemId);
            _ = this.SetCurrentState(actionParameter, arg.Visible ? STATE_ENABLED : STATE_DISABLED);
        }

        protected override BitmapImage GetCommandImage(String actionParameter, Int32 stateIndex, PluginImageSize imageSize)
        {
            var imageText = NameUnknown;
            var imageName = IMGInaccessible;
            if(SourceFilterKey.TryParse(actionParameter, out var parsed))
            {
                imageText = $"{parsed.SourceName} - {parsed.FilterName}";
                imageName = 
                    parsed.Collection == ObsStudioPlugin.Proxy.CurrentSceneCollection
                    && parsed.Scene == ObsStudioPlugin.Proxy.CurrentSceneName
                    && ObsStudioPlugin.Proxy.AllSceneItems.ContainsKey(parsed.StringizeAsItemKey())
                    ? stateIndex == STATE_ENABLED ? IMGEnabled : IMGDisabled
                    : IMGInaccessible;
            }


            return (this.Plugin as ObsStudioPlugin).GetPluginCommandImage(imageSize, imageName, imageText, imageName == IMGEnabled);
        }

        private void AddSourceFilterParameter(String sceneName,Int32 itemId, String itemName, String filterName)
        {
            var sfk = new SourceFilterKey(ObsStudioPlugin.Proxy.CurrentSceneCollection, sceneName, itemId, itemName, filterName);
            var filterEnabled  = ObsStudioPlugin.Proxy.AllSceneItems[sfk.StringizeAsItemKey()].Filters[filterName].Enabled;

            this.AddParameter(sfk.Stringize(), 
                                $"{itemName}-{filterName}",  /*{CommonStrings.SubgroupSeparator}{sceneName}*/
                                $"{this.GroupName}{CommonStrings.SubgroupSeparator}{sceneName}").Description = 
                                    (filterEnabled ? "Disable" : "Enable") 
                                        + $" filter \"{filterName}\" of source \"{itemName}\", scene \"{sceneName}\"";

            this.SetCurrentState(sfk.Stringize(), filterEnabled ? STATE_ENABLED : STATE_DISABLED);
        }

        private void ResetParameters(Boolean readContent)
        {
            this.RemoveAllParameters();

            if (readContent)
            {
                this.Plugin.Log.Info($"ResetParameters:Adding Filters");

                foreach (var item in ObsStudioPlugin.Proxy.AllSceneItems)
                {
                    if(item.Value.Filters.Count > 0)
                    {
                        foreach (var filter in item.Value.Filters)
                        {
                            this.Plugin.Log.Info($"ResetParameters: Adding filter {filter.Value.FilterName} for item {item.Value.SourceName} ");
                            this.AddSourceFilterParameter(item.Value.SceneName, item.Value.SourceId, item.Value.SourceName, filter.Value.FilterName);
                        }
                        
                    }   
                }
            }

            this.ParametersChanged();
            this.ActionImageChanged();
        }
    }
}
