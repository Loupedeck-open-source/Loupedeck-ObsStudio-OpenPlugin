namespace Loupedeck.ObsStudioPlugin
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using OBSWebsocketDotNet;
    using OBSWebsocketDotNet.Types.Events;

    /// <summary>
    /// Proxy to OBS websocket server, for API reference see
    /// https://github.com/obsproject/obs-websocket/blob/4.x-compat/docs/generated/protocol.md
    /// </summary>
    internal partial class ObsAppProxy
    {
        public event EventHandler<SourceFilterEventArgs> AppEvtSourceFilterCreated;
        public event EventHandler<SourceFilterEventArgs> AppEvtSourceFilterRemoved;
        public event EventHandler<SourceFilterRenamedArgs> AppEvtSourceFilterRenamed;
        public event EventHandler<SourceFilterEventArgs> AppEvtSourceFilterEnableStateChanged;

        private void RetreiveSourceFilters(SceneItemDescriptor dtor)
        {
            if (Helpers.TryExecuteFunc(() => this.GetSourceFilterList(dtor.SourceName), out var filters))
            {
                foreach (var filter in filters)
                {
                    this.Plugin.Log.Info($"Adding filters {filter.Name} to source {dtor.SourceName} of scene {dtor.SceneName}");

                    dtor.Filters.Add(filter.Name, new SourceFilter(filter.Name, filter.IsEnabled));
                }
            }
        }

        private void RetreiveSourceFilters(String SourceName, AudioSourceDescriptor dtor)
        {
            if (Helpers.TryExecuteFunc(() => this.GetSourceFilterList(SourceName), out var filters))
            {
                foreach (var filter in filters)
                {
                    this.Plugin.Log.Info($"Adding filters {filter.Name} to audio source source {SourceName}");

                    dtor.Filters.Add(filter.Name, new SourceFilter(filter.Name, filter.IsEnabled));
                }
            }
        }

        ///     <summary>
        ///  Controls source filter enablement 
        /// </summary>
        /// <param name="key">Key for source filter</param>
        /// <param name="forceState">if True, force specific state to the scene item, otherwise toggle. </param>
        /// <param name="newState">state to force</param>
        /// <param name="applyToAllScenes">Apply to all scenes in collection where this source exists</param>
        public void AppGlobalAudioFilterEnableToggle(String filterKey, Boolean forceState = false, Boolean newState = false)
        {

            if(this.IsAppConnected 
                && GlobalFilterKey.TryParse(filterKey, out var key) 
                && this.CurrentAudioSources.ContainsKey(key.Source) 
                && this.CurrentAudioSources[key.Source].Filters.ContainsKey(key.FilterName))
            {
                this.Plugin.Log.Info($"AppGlobalAudioFilterEnableToggle: Key {filterKey}");
                this.CurrentAudioSources[key.Source].Filters[key.FilterName].Enabled = forceState ? newState : !this.CurrentAudioSources[key.Source].Filters[key.FilterName].Enabled;

                Helpers.TryExecuteAction(() => this.SetSourceFilterEnabled(key.Source, key.FilterName, this.CurrentAudioSources[key.Source].Filters[key.FilterName].Enabled));
            }
            else
            {
                this.Plugin.Log.Warning($"AppGlobalAudioFilterEnableToggle: Filterkey \"{filterKey}\" not actionable");
            }
        }

        ///     <summary>
        ///  Controls source filter enablement 
        /// </summary>
        /// <param name="key">Key for source filter</param>
        /// <param name="forceState">if True, force specific state to the scene item, otherwise toggle. </param>
        /// <param name="newState">state to force</param>
        /// <param name="applyToAllScenes">Apply to all scenes in collection where this source exists</param>
        public void AppSourceFilterEnableToggle(String filterKey, Boolean forceState = false, Boolean newState = false)
        {

            SourceFilterKey fKey = SourceFilterKey.TryParse(filterKey, out var parsedkey) ? parsedkey : null;
            var key = ((SceneItemKey)fKey).Stringize();

            this.Plugin.Log.Info($"AppSourceFilterEnableToggle: Key {key}");

            if (this.IsAppConnected && key != null && this.AllSceneItems.ContainsKey(key))
            {
                var item = this.AllSceneItems[key];
                if (item.Filters.ContainsKey(fKey.FilterName))
                {
                    item.Filters[fKey.FilterName].Enabled = forceState ? newState : !item.Filters[fKey.FilterName].Enabled;
                    Helpers.TryExecuteAction(() => this.SetSourceFilterEnabled(item.SourceName, fKey.FilterName, item.Filters[fKey.FilterName].Enabled));
                    //this.SetSourceFilterEnabled(item.SourceName, fKey.FilterName, item.Filters[fKey.FilterName].Enabled);
                    //TEST: should we manually invoke onStateChanged event? 
                }
                else
                {
                    this.Plugin.Log.Warning($"Filter {fKey.FilterName} is not found in source {item.SourceName}");
                }
            }
        }

        private void OnObsSourceFilterCreated(Object sender, SourceFilterCreatedEventArgs e)
        {
            try
            {
                this.Plugin.Log.Info($"OnObsSourceFilterCreated: SourceName:{e.SourceName}. FilterName:{e.FilterName}. FilterKind:{e.FilterKind}");
                
                if( this.CurrentAudioSources.ContainsKey(e.SourceName) ) //Global Audio filter
                { 
                    this.Plugin.Log.Info($"OnObsSourceFilterCreated:  Filter for audio sources Audio Source {e.SourceName}");
                    this.CurrentAudioSources[e.SourceName].Filters.Add(e.FilterName, new SourceFilter(e.FilterName, false));

                }
                else
                {
                    var key = SceneItemKey.Encode(this.CurrentSceneCollection, this.CurrentSceneName, -1, e.SourceName);
                    if (!this.AllSceneItems.ContainsKey(key))
                    {
                        this.Plugin.Log.Error($"WARNING: KEY \"{key}\" NOT FOUND");
                        return;
                    }
                    this.AllSceneItems[key].Filters.Add(e.FilterName, new SourceFilter(e.FilterName, false));
                }

                this.AppEvtSourceFilterCreated?.Invoke(sender, new SourceFilterEventArgs(e.SourceName, e.FilterName));
            }
            catch (Exception ex)
            {
                this.Plugin.Log.Error($"Error adding filter: {ex.Message}");
            }

            this.Plugin.Log.Info($"OnObsSourceFilterCreated: source \"{e.SourceName}\" Filter \"{e.FilterName}\"");
        }

        private void OnObsSourceFilterRemoved(Object sender, SourceFilterRemovedEventArgs e)
        {
            this.Plugin.Log.Info($"OnObsSourceFilterRemoved: SourceName:{e.SourceName}. FilterName:{e.FilterName}");
            
            var itemRemoved = false;
            if (this.CurrentAudioSources.ContainsKey(e.SourceName) && this.CurrentAudioSources[e.SourceName].Filters.ContainsKey(e.FilterName)) //Global Audio filter
            {
                itemRemoved = !this.CurrentAudioSources[e.SourceName].Filters.Remove(e.FilterName);
            }
            else if (this.TryGetSceneItemByName(this.CurrentSceneCollection, this.CurrentSceneName, e.SourceName, out var item) && item.Filters.ContainsKey(e.FilterName))
            {
                itemRemoved = item.Filters.Remove(e.FilterName);
            }
            else
            {
                this.Plugin.Log.Warning($"Cannot remove filter: Scene Item {e.SourceName} does note have filter {e.FilterName}");
                return;
            }
            
            if(!itemRemoved)
            {
                this.Plugin.Log.Warning($"Cannot remove filter: Scene Item {e.SourceName} does note have filter {e.FilterName}");
            }

            this.AppEvtSourceFilterRemoved?.Invoke(sender, new SourceFilterEventArgs(e.SourceName, e.FilterName));
            
        }

        private void OnObsSourceFilterEnableStateChanged(Object sender, SourceFilterEnableStateChangedEventArgs e)
        {
            this.Plugin.Log.Info($"OnObsSourceFilterEnableStateChanged: source \"{e.SourceName}\" Filter \"{e.FilterName}\", Enabled state changed to {e.FilterEnabled} ");

            if (this.CurrentAudioSources.ContainsKey(e.SourceName) && this.CurrentAudioSources[e.SourceName].Filters.ContainsKey(e.FilterName)) //Global Audio filter
            {
                this.CurrentAudioSources[e.SourceName].Filters[e.FilterName].Enabled = e.FilterEnabled;
            }
            else if (this.TryGetSceneItemByName(this.CurrentSceneCollection, this.CurrentSceneName, e.SourceName, out var item)
                && item.Filters.ContainsKey(e.FilterName))
            {
                item.Filters[e.FilterName].Enabled = e.FilterEnabled;
            }
            else
            {
                this.Plugin.Log.Warning($"Cannot find filter {e.FilterName} in source {e.SourceName}");
                return;
            }

            this.AppEvtSourceFilterEnableStateChanged?.Invoke(sender, new SourceFilterEventArgs(e.SourceName, e.FilterName));

        }

        private void OnObsSourceFilterNameChanged(Object sender, SourceFilterNameChangedEventArgs e)
        {
            this.Plugin.Log.Info($"OnObsSourceFilterNameChanged: source \"{e.SourceName}\" Filter name changed to \"{e.FilterName}\" from {e.OldFilterName} ");

            if (this.CurrentAudioSources.ContainsKey(e.SourceName) && this.CurrentAudioSources[e.SourceName].Filters.ContainsKey(e.OldFilterName)) //Global Audio filter
            {
                var f = this.CurrentAudioSources[e.SourceName].Filters[e.OldFilterName];
                this.CurrentAudioSources[e.SourceName].Filters.Remove(e.OldFilterName);
                f.FilterName = e.FilterName;
                this.CurrentAudioSources[e.SourceName].Filters.Add(e.FilterName, f);
            }
            else if (this.TryGetSceneItemByName(this.CurrentSceneCollection, this.CurrentSceneName, e.SourceName, out var item)
                && item.Filters.ContainsKey(e.OldFilterName))
            {
                var f = item.Filters[e.OldFilterName];
                item.Filters.Remove(e.OldFilterName);
                f.FilterName = e.FilterName;
                item.Filters.Add(e.FilterName, f);
            }
            else
            {
                this.Plugin.Log.Warning($"Cannot find source {e.SourceName} in scene {this.CurrentSceneName}");
                return;        
            }

            this.AppEvtSourceFilterRenamed?.Invoke(sender, new SourceFilterRenamedArgs(e.SourceName, e.OldFilterName, e.FilterName));
        }

    }
}
