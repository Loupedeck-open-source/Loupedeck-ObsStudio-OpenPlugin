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
            this.DisplayName = "OBS Source Visibility ";
            this.Description = "Ensures specific source is visible or hidden. This is particularly useful to ensure that source is on or off in custom Multi-Action.";
            this.GroupName = "";

            this.ActionEditor.AddControl(
                new ActionEditorListbox(name: ControlIsSourceVisible, labelText: "Visibility:", "Controls, what state source needs to be in"));
            this.ActionEditor.AddControl(
                new ActionEditorListbox(name: ControlSceneSelector, labelText: "Scene:","Select Scene name"));
            this.ActionEditor.AddControl(
                new ActionEditorListbox(name: ControlSourceSelector, labelText: "Source:", "Select Source name"));
            
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
            //FIXMEFIXME:  Check how to prevent this from failing when scene collection updates!

            if (e.ControlName.EqualsNoCase(ControlIsSourceVisible))
            {
                e.AddItem(this.Visibility_Show, this.Visibility_Show, $"Ensures source is visible");
                e.AddItem(this.Visibility_Hide, this.Visibility_Hide, $"Ensures source is hidden");
                
            } 
            else if ( !ObsStudioPlugin.Proxy.IsAppConnected )
            {
                //Both scenes and sources drop down
                e.AddItem("N/A", "No data","Not connected to OBS");
                return;
            }
            else if (e.ControlName.EqualsNoCase(ControlSceneSelector))
            {
                var scenes = new HashSet<String>();

                //Unique scenes (those that have sources)
                foreach (var item in ObsStudioPlugin.Proxy.AllSceneItems)
                {
                    scenes.Add(item.Value.SceneName);
                }

                foreach (var scene in scenes)
                {
                    e.AddItem(scene, scene, $"Scene {scene}");
                }

                e.ActionEditorState.SetValue(ControlSceneSelector, e.Items[0].Name);
                this.ActionEditor.ListboxItemsChanged(ControlSourceSelector);
            }
            else if (e.ControlName.EqualsNoCase(ControlSourceSelector))
            {
                var selectedScene = e.ActionEditorState.GetControlValue(ControlSceneSelector);

                var firstKey = "";

                foreach (var item in ObsStudioPlugin.Proxy.AllSceneItems)
                {
                    if(firstKey.IsNullOrEmpty())
                    {
                        firstKey = item.Key;
                    }

                    if(item.Value.SceneName == selectedScene )
                    {
                        //The 'Value' for sources drop down is the same Key (collection-scene-item) we are using in SourceVisibilityCommand
                        e.AddItem(item.Key, item.Value.SourceName, $"Source {item.Value.SourceName}");
                    }
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
