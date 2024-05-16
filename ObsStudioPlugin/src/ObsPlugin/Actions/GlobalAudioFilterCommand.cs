namespace Loupedeck.ObsStudioPlugin.Actions
{
    using System;
    using System.Collections.ObjectModel;

    //The same class as SourceFilterCommand but for Global audio sources
    internal class GlobalAudioFilterCommand : PluginMultistateDynamicCommand
    {
        public const String IMGEnabled = "FilterOn.png";
        public const String IMGDisabled = "FilterOff.png";
        public const String IMGInaccessible = "FilterDisabled.png";
        public const String NameUnknown = "UnknownFilter";

        private const Int16 STATE_DISABLED = 0;
        private const Int16 STATE_ENABLED = 1;

        public GlobalAudioFilterCommand()
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
            ObsStudioPlugin.Proxy.AppEvtSceneNameChanged += this.OnSceneListChanged; 
            ObsStudioPlugin.Proxy.AppSceneItemRenamed += this.OnSceneListChanged;

            ObsStudioPlugin.Proxy.AppEvtSourceFilterCreated += this.OnGlobalSourceFilterCreated;
            ObsStudioPlugin.Proxy.AppEvtSourceFilterRemoved += this.OnSourceFilterRemoved;
            ObsStudioPlugin.Proxy.AppEvtSourceFilterRenamed += this.OnSourceFilterRenamed;
            ObsStudioPlugin.Proxy.AppEvtSourceFilterEnableStateChanged += this.OnSourceFilterEnableStateChanged;

            ObsStudioPlugin.Proxy.AppInputRenamed += this.OnSourceRenamed;

            this.OnAppDisconnected(this, null);

            return true;
        }

        protected override Boolean OnUnload()
        {
            ObsStudioPlugin.Proxy.AppConnected -= this.OnAppConnected;
            ObsStudioPlugin.Proxy.AppDisconnected -= this.OnAppDisconnected;

            ObsStudioPlugin.Proxy.AppEvtSceneListChanged -= this.OnSceneListChanged;
            ObsStudioPlugin.Proxy.AppSceneItemRenamed -= this.OnSceneListChanged;

            ObsStudioPlugin.Proxy.AppEvtSourceFilterCreated -= this.OnGlobalSourceFilterCreated;
            ObsStudioPlugin.Proxy.AppEvtSourceFilterRemoved -= this.OnSourceFilterRemoved;
            ObsStudioPlugin.Proxy.AppEvtSourceFilterRenamed -= this.OnSourceFilterRenamed;
            ObsStudioPlugin.Proxy.AppEvtSourceFilterEnableStateChanged -= this.OnSourceFilterEnableStateChanged;

            ObsStudioPlugin.Proxy.AppInputRenamed -= this.OnSourceRenamed;

            return true;
        }

        private void OnGlobalSourceFilterCreated(Object sender, SourceFilterEventArgs args)
        {
            //Collection-source-filter
            var key = new GlobalFilterKey(ObsStudioPlugin.Proxy.CurrentSceneCollection, args.SourceName, args.FilterName);

            if (ObsStudioPlugin.Proxy.CurrentAudioSources.ContainsKey(args.SourceName))
            {
                //Note, the Global source Filters are always created in the current scene collection!
                this.AddFilter(args.SourceName, args.FilterName);
                this.ParametersChanged();
            }
            else
            {
                this.Plugin.Log.Warning($"OnGlobalSourceFilterCreated: Cannot find Source by name {args.SourceName} ");
            }
        }

        private void OnSourceFilterRemoved(Object sender, SourceFilterEventArgs args)
        {
            if( ObsStudioPlugin.Proxy.CurrentAudioSources.ContainsKey(args.SourceName) )
            {
                var key = new GlobalFilterKey(ObsStudioPlugin.Proxy.CurrentSceneCollection, args.SourceName, args.FilterName);
                this.RemoveParameter(key.Stringize());
                this.ParametersChanged();
            }
        }

        private void OnSourceFilterRenamed(Object sender, SourceFilterRenamedArgs args)
        {
            //arg.FilterName is the old name
            if (ObsStudioPlugin.Proxy.CurrentAudioSources.ContainsKey(args.SourceName))
            {
                var key = new GlobalFilterKey(ObsStudioPlugin.Proxy.CurrentSceneCollection, args.SourceName, args.FilterName);
                this.Plugin.Log.Info($"OnSourceFilterRenamed: Removing {args.FilterName} and adding {args.NewFilterName}");

                this.RemoveParameter(key.Stringize());
                this.AddFilter(args.SourceName, args.NewFilterName);
                this.ParametersChanged();
            }
        }

        private void OnSourceFilterEnableStateChanged(Object sender, SourceFilterEventArgs args)
        {
            var key = new GlobalFilterKey(ObsStudioPlugin.Proxy.CurrentSceneCollection, args.SourceName, args.FilterName);

            if (ObsStudioPlugin.Proxy.CurrentAudioSources.ContainsKey(args.SourceName) 
                && ObsStudioPlugin.Proxy.CurrentAudioSources[args.SourceName].Filters.ContainsKey(args.FilterName))
            {
                //The state is already in the values
                this.SetCurrentState(key.Stringize(), ObsStudioPlugin.Proxy.CurrentAudioSources[args.SourceName].Filters[args.FilterName].Enabled  ? STATE_ENABLED : STATE_DISABLED);
            }
            else
            {
                this.Plugin.Log.Info($"OnSourceFilterEnableStateChanged key not found ");
            }
        }

        protected override void RunCommand(String actionParameter)
        {
            ObsStudioPlugin.Proxy.AppGlobalAudioFilterEnableToggle(actionParameter);
        } 

        private void OnSceneListChanged(Object sender, EventArgs e) => this.ResetParameters(true);

        private void OnSourceRenamed(Object sender, OldNewStringChangeEventArgs args) => this.ResetParameters(true);

        private void OnAppConnected(Object sender, EventArgs e) => this.IsEnabled = true;

        private void OnAppDisconnected(Object sender, EventArgs e)
        {
            this.IsEnabled = false;

            this.ResetParameters(false);
            this.ActionImageChanged();
        }

        protected override BitmapImage GetCommandImage(String actionParameter, Int32 stateIndex, PluginImageSize imageSize)
        {
            var imageText = NameUnknown;
            var imageName = IMGInaccessible;
            
            if(GlobalFilterKey.TryParse(actionParameter, out var parsed))
            {
                imageText = $"{parsed.Source} - {parsed.FilterName}";
                imageName = 
                    parsed.Collection == ObsStudioPlugin.Proxy.CurrentSceneCollection
                    && ObsStudioPlugin.Proxy.CurrentAudioSources.ContainsKey(parsed.Source)
                    && ObsStudioPlugin.Proxy.CurrentAudioSources[parsed.Source].Filters.ContainsKey(parsed.FilterName)
                    ? stateIndex == STATE_ENABLED ? IMGEnabled : IMGDisabled
                    : IMGInaccessible;
            }
            //ObsStudioPlugin.Proxy.CurrentAudioSources[parsed.Source].Filters[parsed.FilterName]

            return (this.Plugin as ObsStudioPlugin).GetPluginCommandImage(imageSize, imageName, imageText, imageName == IMGEnabled);
        }

        private void AddFilter(String sourceName, String filterName)
        {
            var key = new GlobalFilterKey(ObsStudioPlugin.Proxy.CurrentSceneCollection, sourceName, filterName);
            var filterEnabled = ObsStudioPlugin.Proxy.CurrentAudioSources[sourceName].Filters[filterName].Enabled;

            //this.Plugin.Log.Info($"Adding filter {filterName}");

            this.AddParameter(key.Stringize(), 
                                $"{sourceName}(G)-{filterName}",
                                //$"{this.GroupName}{CommonStrings.SubgroupSeparator}{sceneName}").Description =
                                $"{this.GroupName}").Description = 
                                    (filterEnabled ? "Disable" : "Enable") 
                                        + $" filter \"{filterName}\" of source \"{sourceName}\"";

            this.SetCurrentState(key.Stringize(), filterEnabled ? STATE_ENABLED : STATE_DISABLED);
        }

        private void ResetParameters(Boolean readContent)
        {
            this.RemoveAllParameters();

            if (readContent)
            {
                this.Plugin.Log.Info($"Adding {ObsStudioPlugin.Proxy.CurrentAudioSources.Count} sources");

                foreach (var item in ObsStudioPlugin.Proxy.CurrentAudioSources)
                {
                    if (item.Value.Filters.Count > 0)
                    {
                        foreach (var filter in item.Value.Filters)
                        {
                            this.Plugin.Log.Info($"ResetParameters: Adding filter {filter.Value.FilterName} for item {item.Key} ");
                            this.AddFilter(item.Key, filter.Value.FilterName);
                        }
                    }
                }
            }

            this.ParametersChanged();
            this.ActionImageChanged();

        }
    }
}
