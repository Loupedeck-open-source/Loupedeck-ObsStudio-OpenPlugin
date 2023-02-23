namespace Loupedeck.ObsStudioPlugin.Actions
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Design;
    using System.Linq;

    internal class SourceVisibilitySwitch : ActionEditorCommand
    {


        private const String ControlSceneSelector = "sceneSelector";
        private const String ControlSourceSelector = "sourceSelector";
        private const String ControlIsSourceVisible  = "switchDirection";
        private readonly String Visibility_Show = "Show";
        private readonly String Visibility_Hide = "Hide";

        public SourceVisibilitySwitch()
        {
            this.DisplayName = "Source Visibility ";
            this.Description = "Ensures specific source is visible or hidden. This is particularly useful to ensure that source is on or off in custom Multi-Action.";
            this.GroupName = "";

            this.ActionEditor.AddControl(
                new ActionEditorListbox(name: ControlSceneSelector, labelText: "Scene:"/*,"Select Scene name"*/)
                    .SetRequired()
                );
            this.ActionEditor.AddControl(
                new ActionEditorListbox(name: ControlSourceSelector, labelText: "Source:"/*, "Select Source name"*/)
                .SetRequired()
                );
            this.ActionEditor.AddControl(
                new ActionEditorListbox(name: ControlIsSourceVisible, labelText: "Visibility:"/*, "Controls, what state source needs to be in"*/)
                .SetRequired()
                );

            this.ActionEditor.ListboxItemsRequested += this.OnActionEditorListboxItemsRequested;
            this.ActionEditor.ControlValueChanged += this.OnActionEditorControlValueChanged;
        }

        protected override Boolean OnLoad()
        {
            ObsStudioPlugin.Proxy.AppConnected += this.OnAppConnected;
            ObsStudioPlugin.Proxy.AppDisconnected += this.OnAppDisconnected;
            //ObsStudioPlugin.Proxy.AppEvtSceneListChanged += this.OnSceneListChanged;

            return true;
        }

        protected override Boolean OnUnload()
        {
            ObsStudioPlugin.Proxy.AppConnected -= this.OnAppConnected;
            ObsStudioPlugin.Proxy.AppDisconnected -= this.OnAppDisconnected;
            //ObsStudioPlugin.Proxy.AppEvtSceneListChanged -= this.OnSceneListChanged;

            return true;
        }

        private void OnSceneListChanged(Object sender, EventArgs e) => this.ActionEditor.ListboxItemsChanged(ControlSceneSelector);

        private void OnActionEditorControlValueChanged(Object sender, ActionEditorControlValueChangedEventArgs e)
        {

            if (e.ControlName.EqualsNoCase(ControlSceneSelector))
            {
                this.ActionEditor.ListboxItemsChanged(ControlSourceSelector);
                this.Plugin.Log.Info($"Regenerating sources list");
            }
            //We set display name ONLY if all controls are set!
            else if( e.ControlName.EqualsNoCase(ControlSourceSelector)
                   || (e.ControlName.EqualsNoCase(ControlIsSourceVisible)
                   &&
                    !(e.ActionEditorState.GetControlValue(ControlSceneSelector).IsNullOrEmpty() 
                   || e.ActionEditorState.GetControlValue(ControlSourceSelector).IsNullOrEmpty()))
                   )
            {
                var visible = e.ActionEditorState.GetControlValue(ControlIsSourceVisible) == this.Visibility_Show ? "Show" : "Hide";
                var sourceName = "Unknown";
                var sceneName = "Unknown"; 
                if (SceneItemKey.TryParse(e.ActionEditorState.GetControlValue(ControlSourceSelector), out var parsed))
                {
                    sourceName = parsed.Source;
                    sceneName = parsed.Scene;
                }
                  
                e.ActionEditorState.SetDisplayName($"{visible} {sourceName} ({sceneName})");
            }
        }

        private void OnActionEditorListboxItemsRequested(Object sender, ActionEditorListboxItemsRequestedEventArgs e)
        {
            /*
             * This does not work (yet)
             * e.ActionEditorState.SetEnabled(ControlSceneSelector, ObsStudioPlugin.Proxy.IsAppConnected);
             * e.ActionEditorState.SetEnabled(ControlSourceSelector, ObsStudioPlugin.Proxy.IsAppConnected);
             * e.ActionEditorState.SetEnabled(ControlIsSourceVisible, ObsStudioPlugin.Proxy.IsAppConnected);
            */

            if (e.ControlName.EqualsNoCase(ControlIsSourceVisible))
            {
                e.AddItem(this.Visibility_Show, this.Visibility_Show, $"Ensures source is visible");
                e.AddItem(this.Visibility_Hide, this.Visibility_Hide, $"Ensures source is hidden");
            }
            else if (e.ControlName.EqualsNoCase(ControlSceneSelector))
            {
                if (!ObsStudioPlugin.Proxy.IsAppConnected)
                {
                    e.AddItem("N/A", "No data", "Not connected to OBS");
                }
                else
                {
                    this.Plugin.Log.Info($"SVS: Adding scenes to {ControlSceneSelector} control");
                    //Unique scenes (those that have sources)
                    foreach (var item in ObsStudioPlugin.Proxy.ScenesWithItems)
                    {
                        e.AddItem(item.Key, item.Key, $"Scene {item.Key}");
                    }
                }
            }
            else if (e.ControlName.EqualsNoCase(ControlSourceSelector))
            {

                if (!ObsStudioPlugin.Proxy.IsAppConnected)
                {
                    //To ensure sources list is empty so that control cannot be saved.
                    return; 
                }

                var selectedScene = e.ActionEditorState.GetControlValue(ControlSceneSelector);

                if (!String.IsNullOrEmpty(selectedScene))
                {
                    this.Plugin.Log.Info($"SVS: Adding sources for {selectedScene}");

                    if (ObsStudioPlugin.Proxy.ScenesWithItems.TryGetValue(selectedScene, out var sceneItemList))
                    {
                        var firstKey = "";
                        foreach (var item in sceneItemList)
                        {
                            if (firstKey.IsNullOrEmpty())
                            {
                                firstKey = item.Item1;
                            }
                            //The 'Value' for sources drop down is the same Key (collection-scene-item) we are using in SourceVisibilityCommand
                            e.AddItem(item.Item1, item.Item2, $"Source {item.Item2}");
                        }
                    }
                }
                else
                {
                    e.AddItem("N/A", "No Scene Selected", "Select Scene first");
                }
            }
            else
            {
                this.Plugin.Log.Error($"Unexpected control name '{e.ControlName}'");
            }
        }

        private void OnAppConnected(Object sender, EventArgs e)
        {
        }

        private void OnAppDisconnected(Object sender, EventArgs e)
        {
        }

        protected override BitmapImage GetCommandImage(ActionEditorActionParameters actionParameters, Int32 imageWidth, Int32 imageHeight)
        {
            var sourceName = SourceVisibilityCommand.SourceNameUnknown;
            var imageName = SourceVisibilityCommand.IMGSceneInaccessible;

            if (actionParameters.TryGetString(ControlSourceSelector, out var key) && SceneItemKey.TryParse(key, out var parsed))
            {
                sourceName = parsed.Source;
                var sourceVisible = actionParameters.TryGetString(ControlIsSourceVisible, out var vis) && vis == this.Visibility_Show;

                imageName = parsed.Collection != ObsStudioPlugin.Proxy.CurrentSceneCollection
                    ? SourceVisibilityCommand.IMGSceneInaccessible
                    : sourceVisible ? SourceVisibilityCommand.IMGSceneSelected : SourceVisibilityCommand.IMGSceneUnselected;
            }
            else
            {
                this.Plugin.Log.Warning($"Cannot retreive selected source name from '{actionParameters}'");
            }

            return (this.Plugin as ObsStudioPlugin).GetPluginCommandImage(imageWidth >=80 ? PluginImageSize.Width90: PluginImageSize.Width60, imageName, sourceName, imageName == SourceVisibilityCommand.IMGSceneSelected);
        }

        protected override Boolean RunCommand(ActionEditorActionParameters actionParameters)
        {
            if (actionParameters.TryGetString(ControlSourceSelector, out var key))
            {
                var setVisible = actionParameters.TryGetString(ControlIsSourceVisible, out var vis) && vis == this.Visibility_Show;

                ObsStudioPlugin.Proxy.AppSceneItemVisibilityToggle(key, true, setVisible);

                return true;
            }
            else
            {
                this.Plugin.Log.Warning($"Run: Cannot retreive selected source name from '{actionParameters}'");
                return false;
            }
        }
    }
}
