﻿namespace Loupedeck.ObsPlugin.Actions
{
    using System;

    public abstract class GenericOnOffSwitch : PluginTwoStateDynamicCommand
    {
    
        private ObsAppProxy Proxy => (this.Plugin as ObsPlugin).Proxy;

        public GenericOnOffSwitch(String displayName, String description, String groupName, 
                                 String offStateName, String onStateName,
                                 String offStateImage, String onStateImage)
        {
            this.DisplayName = displayName;
            this.Description = description;
            this.GroupName = groupName;

            this.AddToggleCommand(displayName, 
                    EmbeddedResources.ReadImage(onStateImage), 
                    EmbeddedResources.ReadImage(offStateImage));

            this.SetOffStateDisplayName(offStateName);
            this.SetOnStateDisplayName(onStateName);

        }

        protected override Boolean OnLoad()
        {
            this.Proxy.EvtAppConnected += this.OnAppConnected;
            this.Proxy.EvtAppDisconnected += this.OnAppDisconnected;
            
            this.IsEnabled = false;
            this.ConnectAppEvents(this.AppEvtTurnedOn, this.AppEvtTurnedOff);

            return true;
        }

        protected override Boolean OnUnload()
        {
            this.Proxy.EvtAppConnected -= this.OnAppConnected;
            this.Proxy.EvtAppDisconnected -= this.OnAppDisconnected;

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

        private void AppEvtTurnedOff(Object sender, EventArgs e)
        {
            ObsPlugin.Trace($"Action {this.Name}: Setting state to OFF");
            this.TurnOff();
        }

        private void AppEvtTurnedOn(Object sender, EventArgs e)
        {
            ObsPlugin.Trace($"Action {this.Name}: Setting state to ON");
            this.TurnOn();
        }

        /// <summary>
        /// executes toggle at the application side. 
        /// </summary>
        /// <param name="currentState">Index of current state of the control: 0 - off, 1 - on </param>
        protected abstract void RunToggle();
        protected override void RunCommand(TwoStateCommand command)
        {
            switch (command)
            {
                case TwoStateCommand.TurnOff:
                    //Unimplemented
           
                case TwoStateCommand.TurnOn:
                    ObsPlugin.Trace($"Action {this.Name}: On and Off direct switches not implemented");
                    break;

                case TwoStateCommand.Toggle:
                    this.RunToggle();
                    break;
            }
        }

    }
}
