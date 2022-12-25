﻿namespace Loupedeck.ObsStudioPlugin.Actions
{
    using System;

    public abstract class GenericOnOffSwitch : PluginTwoStateDynamicCommand
    {
        public Byte[] OnStateImage { get; private set; }
        public Byte[] OffStateImage { get; private set; }

        public String OnStateName { get; private set; }
        public String OffStateName { get; private set; }

        public void SwitchOn() => this.RunCommand(TwoStateCommand.TurnOff);
        public void SwitchOff() => this.RunCommand(TwoStateCommand.TurnOn);

        public GenericOnOffSwitch(
            String name,
            String displayName, String description, String groupName,
            String offStateName, String onStateName,
            String offStateImage, String onStateImage):base(displayName,description,groupName)
        {
            this.Name = name;
            this.OnStateImage = EmbeddedResources.ReadBinaryFile(ObsStudioPlugin.ImageResPrefix + onStateImage);
            this.OffStateImage = EmbeddedResources.ReadBinaryFile(ObsStudioPlugin.ImageResPrefix + offStateImage);
            this.OnStateName = onStateName;
            this.OffStateName = offStateName;

            var p = this.AddToggleCommand(
                displayName,
                this.OnStateImage.ToImage(),
                this.OffStateImage.ToImage());
            p.Description = description;
            
            this.SetOffStateDisplayName(this.OffStateName);
            this.SetOnStateDisplayName(this.OnStateName);

            UniversalStateSwitch.RegisterToggle(this);
        }

        protected override Boolean OnLoad()
        {
            ObsStudioPlugin.Proxy.AppConnected += this.OnAppConnected;
            ObsStudioPlugin.Proxy.AppDisconnected += this.OnAppDisconnected;

            this.IsEnabled = false;
            this.ConnectAppEvents(this.AppEvtTurnedOn, this.AppEvtTurnedOff);

            return true;
        }

        protected override Boolean OnUnload()
        {
            ObsStudioPlugin.Proxy.AppConnected -= this.OnAppConnected;
            ObsStudioPlugin.Proxy.AppDisconnected -= this.OnAppDisconnected;

            this.DisconnectAppEvents(this.AppEvtTurnedOn, this.AppEvtTurnedOff);

            return true;
        }

        /// <summary>
        /// Connects command's On and Off events to the source (application)
        /// </summary>
        /// <param name="eventSwitchedOff">Event fired AFTER switch is turned off</param>
        /// <param name="eventSwitchedOn">Event fired AFTER switch is turned on</param>
        protected abstract void ConnectAppEvents(EventHandler<EventArgs> eventSwitchedOff, EventHandler<EventArgs> eventSwitchedOn);

        /// <summary>
        /// Disconnects command's On and Off events to the source (application)
        /// </summary>
        /// <param name="eventSwitchedOff">Event fired AFTER switch is turned off</param>
        /// <param name="eventSwitchedOn">Event fired AFTER switch is turned on</param>
        protected abstract void DisconnectAppEvents(EventHandler<EventArgs> eventSwitchedOff, EventHandler<EventArgs> eventSwitchedOn);

        private void OnAppConnected(Object sender, EventArgs e)
        {
            this.IsEnabled = true;
            this.AppEvtTurnedOff(sender, e); // Setting off by default
        }

        private void OnAppDisconnected(Object sender, EventArgs e) => this.IsEnabled = false;

        private Boolean _legacyActionDetected = false; 

        private void AppEvtTurnedOff(Object sender, EventArgs e)
        {
            this.Plugin.Log.Info($"Action {this.Name}: Setting state to OFF");
            this.TurnOff();
            if(this._legacyActionDetected)
            {  
                //If there were legacy calls we'll update all images
                this.ActionImageChanged();
            }
        }

        private void AppEvtTurnedOn(Object sender, EventArgs e)
        {
            this.Plugin.Log.Info($"Action {this.Name}: Setting state to ON");
            this.TurnOn();
            if (this._legacyActionDetected)
            {
                //If there were legacy calls we'll update all images
                this.ActionImageChanged();
            }
        }

        protected override void RunCommand(String actionParameter)
        {
            if (String.IsNullOrEmpty(actionParameter))
            {
                this._legacyActionDetected = true;
                // Handling legacy (old OBS plugin) actions
                this.Plugin.Log.Info($"Legacy run for {this.Name}");
                this.RunCommand(TwoStateCommand.Toggle);
            }
            else
            {
                base.RunCommand(actionParameter);
            }
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            if (String.IsNullOrEmpty(actionParameter))
            {
                // Handling legacy (old OBS plugin) actions. For some strange reason TryGetStateIndex is not working...
                var stateIndex = this.IsTurnedOn ? 1 : 0;
                this._legacyActionDetected = true;
                this.Plugin.Log.Info($"Legacy GCM for {this.Name}, state index {stateIndex}");
                return this.GetCommandImage($"{(Int32)TwoStateCommand.Toggle}", stateIndex, imageSize);
            }
            else
            {
                return base.GetCommandImage(actionParameter, imageSize);
            }
        }
    }
}
