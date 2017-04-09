#region usings

using System;
using System.Collections.Generic;
using System.Diagnostics;

#endregion

namespace Eve.Plugin {
    internal class PluginWrapper : MarshalByRefObject {
        internal EventHandler<CommandRegistrarEventArgs> CommandRegistrarEventArgsCallback;

        internal EventHandler TerminateBotEvent;

        public PluginHost PluginHost;

        private void PluginsCallback(object source, PluginEventArgs e) {
            switch (e.MessageType) {
                case PluginEventMessageType.Message:
                    if (e.Result is PluginReturnMessage response) {
                        Writer.SendData(response.Protocol, $"{response.Target} {response.Args}");
                        break;
                    }

                    Writer.Log(e.Result.ToString(), IrcLogEntryType.Message);
                    break;
                case PluginEventMessageType.EventLog:
                    break;
                case PluginEventMessageType.Action:
                    switch (e.ActionType) {
                        case PluginActionType.AddCommand:
                            try {
                                CommandRegistrarCallback(this,
                                    new CommandRegistrarEventArgs((KeyValuePair<string, string>)e.Result));
                            } catch (InvalidCastException c) {
                                Writer.Log(c.Message, IrcLogEntryType.Error);
                            }
                            break;
                        case PluginActionType.Load:
                            PluginHost.LoadPluginDomain();
                            break;
                        case PluginActionType.Unload:
                            PluginHost.UnloadPluginDomain();
                            break;
                        case PluginActionType.None:
                            break;
                        case PluginActionType.RunProcess:
                            break;
                        case PluginActionType.SignalTerminate:
                            PluginHost.UnloadPluginDomain();
                            TerminateBotEvent?.Invoke(this, EventArgs.Empty);
                            break;
                        case PluginActionType.UpdatePlugin:
                            break;
                        case PluginActionType.SendMessage:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void CommandRegistrarCallback(object source, CommandRegistrarEventArgs e) {
            CommandRegistrarEventArgsCallback?.Invoke(this, e);
        }

        public void Start(EventHandler<CommandRegistrarEventArgs> commandRegistrar) {
            if (PluginHost != null) return;

            PluginHost = new PluginHost();
            CommandRegistrarEventArgsCallback += commandRegistrar;
            PluginHost.PluginCallback += PluginsCallback;
            PluginHost.LoadPluginDomain();
            PluginHost.StartPlugins();
        }
    }
}