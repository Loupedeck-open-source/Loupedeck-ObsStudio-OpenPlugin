namespace Loupedeck.ObsStudioPlugin
{
    using System;
    using System.Collections.Generic;

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
                var key = SceneItemKey.Encode(this.CurrentSceneCollection, this.CurrentSceneName, -1, e.SourceName);
                if (!this.AllSceneItems.ContainsKey(key))
                {
                    this.Plugin.Log.Error($"WARNING: KEY \"{key}\" NOT FOUND");
                    return;
                }
                this.AllSceneItems[key].Filters.Add(e.FilterName, new SourceFilter(e.FilterName, false));
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
            if (this.TryGetSceneItemByName(this.CurrentSceneCollection, this.CurrentSceneName, e.SourceName, out var item))
            {
                if (item.Filters.ContainsKey(e.FilterName))
                {
                    item.Filters.Remove(e.FilterName);

                    this.AppEvtSourceFilterRemoved?.Invoke(sender, new SourceFilterEventArgs(e.SourceName, e.FilterName));
                }
                else
                {
                    this.Plugin.Log.Warning($"Cannot remove filter: Scene Item {e.SourceName} does note have filter {e.FilterName}");
                }
            }
            else
            {
                this.Plugin.Log.Warning($"Cannot find source {e.SourceName} in scene {this.CurrentSceneName}");
            }

            this.Plugin.Log.Info($"OnObsSourceFilterRemoved: source \"{e.SourceName}\" Filter \"{e.FilterName}\"");
        }

        private Boolean TryGetSourceFilter(String sourceName, String filterName, out SourceFilter filter)
        {
            filter = null;
            //Search in AllSceneItems
            foreach (var item in ObsStudioPlugin.Proxy.AllSceneItems)
            {
                if (item.Value.CollectionName == this.CurrentSceneCollection &&
                    item.Value.SceneName == this.CurrentSceneName &&
                    item.Value.SourceName == sourceName)
                {
                    if (item.Value.Filters.ContainsKey(filterName))
                    {
                        filter = item.Value.Filters[filterName];
                        return true;
                    }
                }
            }
            return false;
        }

        private void OnObsSourceFilterEnableStateChanged(Object sender, SourceFilterEnableStateChangedEventArgs e)
        {
            if (this.TryGetSceneItemByName(this.CurrentSceneCollection, this.CurrentSceneName, e.SourceName, out var item)
                && item.Filters.ContainsKey(e.FilterName))
            {
                item.Filters[e.FilterName].Enabled = e.FilterEnabled;
                this.AppEvtSourceFilterEnableStateChanged?.Invoke(sender, new SourceFilterEventArgs(e.SourceName, e.FilterName));
            }
            else
            {
                this.Plugin.Log.Warning($"Cannot find filter {e.FilterName} in source {e.SourceName}");
            }

            this.Plugin.Log.Info($"OnObsSourceFilterEnableStateChanged: source \"{e.SourceName}\" Filter \"{e.FilterName}\", Enabled state changed to {e.FilterEnabled} ");
        }

        private void OnObsSourceFilterNameChanged(Object sender, SourceFilterNameChangedEventArgs e)
        {
            var key = new SourceFilterKey(this.CurrentSceneCollection, this.CurrentSceneName, -1, e.SourceName, e.FilterName);
            if (this.AllSceneItems.ContainsKey(key.StringizeAsItemKey()))
            {
                if (this.AllSceneItems[key.StringizeAsItemKey()].Filters.ContainsKey(e.OldFilterName))
                {
                    var f = this.AllSceneItems[key.StringizeAsItemKey()].Filters[e.OldFilterName];
                    this.AllSceneItems[key.StringizeAsItemKey()].Filters.Remove(e.OldFilterName);
                    f.FilterName = e.FilterName;
                    this.AllSceneItems[key.StringizeAsItemKey()].Filters.Add(e.FilterName, f);
                }
            }
            else
            {
                this.Plugin.Log.Warning($"Cannot find source {e.SourceName} in scene {this.CurrentSceneName}");
            }

            this.AppEvtSourceFilterRenamed?.Invoke(sender, new SourceFilterRenamedArgs(e.SourceName, e.OldFilterName, e.FilterName));
            this.Plugin.Log.Info($"OnObsSourceFilterNameChanged: source \"{e.SourceName}\" Filter name changed to \"{e.FilterName}\" from {e.OldFilterName} ");

        }

    }
}
