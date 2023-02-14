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
        

        public SourceVisibilitySwitch()
        {
            this.DisplayName = "OBS Source Visibility ";
            this.Description = "Ensures specific source is visible or hidden. This is particularly useful to ensure that source is on or off in custom Multi-Action.";
            this.GroupName = "";
            this.ActionEditor.AddControl(
                        new ActionEditorCheckbox(name: ControlIsSourceVisible, labelText: "Show", description: "Checked = ensure source visible, unchecked=ensure source hidden")
                .SetDefaultValue(true));

            this.ActionEditor.AddControl(
                new ActionEditorListbox(name: ControlSceneSelector, labelText: "Scene:","Select Scene name"));
            this.ActionEditor.AddControl(
                new ActionEditorListbox(name: ControlSourceSelector, labelText: "Source:", "Select Source name"));
            
            this.ActionEditor.ListboxItemsRequested += this.OnActionEditorListboxItemsRequested;
            this.ActionEditor.ControlValueChanged += this.OnActionEditorControlValueChanged;
            this.ActionEditor.ControlsStateRequested += this.OnActionEditorControlsStateRequested;
        }

        protected override Boolean OnLoad()
        {
            ObsStudioPlugin.Proxy.AppConnected += this.OnAppConnected;
            ObsStudioPlugin.Proxy.AppDisconnected += this.OnAppDisconnected;
            ObsStudioPlugin.Proxy.AppEvtSceneListChanged += this.OnSceneListChanged;

            return true;
        }

        protected override Boolean OnUnload()
        {
            ObsStudioPlugin.Proxy.AppConnected -= this.OnAppConnected;
            ObsStudioPlugin.Proxy.AppDisconnected -= this.OnAppDisconnected;
            ObsStudioPlugin.Proxy.AppEvtSceneListChanged -= this.OnSceneListChanged;

            return true;
        }

        private void OnSceneListChanged(Object sender, EventArgs e) => this.ActionEditor.ListboxItemsChanged(ControlSceneSelector);

        private void OnActionEditorControlsStateRequested(Object sender, ActionEditorControlsStateRequestedEventArgs e)
        {
/*            var notValid = e.ActionEditorState.GetControlValue(ControlSceneSelector).IsNullOrEmpty() || e.ActionEditorState.GetControlValue(ControlSourceSelector).IsNullOrEmpty();
            e.ActionEditorState.SetValidity(ControlSourceSelector, !notValid, notValid ? "Source cannot be empty" : null);
*/
        }

        private void OnActionEditorControlValueChanged(Object sender, ActionEditorControlValueChangedEventArgs e)
        {
            /*ControlSceneSelector 
            ControlSourceSelector
            ControlIsSourceVisible
            */

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
                var visible =(Boolean.TryParse(e.ActionEditorState.GetControlValue(ControlIsSourceVisible), out var chkboxchecked) && chkboxchecked )
                             ? "Show" : "Hide";
                var sourceName = "Unknown";
                var sceneName = "Unknown"; 
                if (SceneItemKey.TryParse(e.ActionEditorState.GetControlValue(ControlSourceSelector), out var parsed))
                {
                    sourceName = parsed.Source;
                    sceneName = parsed.Scene;
                }
                  
                e.ActionEditorState.SetDisplayName($"{visible} {sourceName} (scene {sceneName})");
            }
        }

        private void OnActionEditorListboxItemsRequested(Object sender, ActionEditorListboxItemsRequestedEventArgs e)
        {
            /*ControlSceneSelector
            ControlSourceSelector
            ControlIsSourceVisible*/
            
            if(!ObsStudioPlugin.Proxy.IsAppConnected && e.ControlName.EqualsNoCase(ControlSceneSelector) )
            {
                e.AddItem("N/A", "No data","Not connected to OBS");
                e.ActionEditorState.SetValue(ControlSourceSelector, e.Items[0].Name);
            }

            if (e.ControlName.EqualsNoCase(ControlSceneSelector))
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
            else if (e.ControlName.EqualsNoCase(ControlSourceSelector) && ObsStudioPlugin.Proxy.IsAppConnected)
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
               
                e.ActionEditorState.SetValue(ControlSourceSelector, firstKey);
                //e.ActionEditorState.SetDisplayName( ObsStudioPlugin.Proxy.AllSceneItems[firstKey].SceneItemName);
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
                var sceneSelected = false;
                if (actionParameters.TryGetBoolean(ControlIsSourceVisible, out var sceneSelParam))
                {
                    sceneSelected = sceneSelParam;
                }

                imageName = parsed.Collection != ObsStudioPlugin.Proxy.CurrentSceneCollection
                    ? SourceVisibilityCommand.IMGSceneInaccessible
                    : sceneSelected ? SourceVisibilityCommand.IMGSceneSelected : SourceVisibilityCommand.IMGSceneUnselected;
            }
            else
            {
                this.Plugin.Log.Warning($"Cannot retreive selected source name from '{actionParameters}'");
            }

            return (this.Plugin as ObsStudioPlugin).GetPluginCommandImage(imageWidth >=80 ? PluginImageSize.Width90: PluginImageSize.Width60, imageName, sourceName, imageName == SourceVisibilityCommand.IMGSceneSelected);
        }

        protected override Boolean RunCommand(ActionEditorActionParameters actionParameters)
        {
            if(actionParameters.TryGetString(ControlSourceSelector, out var key))
            {
                ObsStudioPlugin.Proxy.AppToggleSceneItemVisibility(key);
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
