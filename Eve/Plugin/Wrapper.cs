#region usings

using System;
using System.Collections.Generic;
using System.Diagnostics;

#endregion

namespace Eve.Plugin {
    internal class PluginWrapper : MarshalByRefObject {
        internal EventHandler<CommandRegistrarEventArgs> CommandRegistrarEventArgsCallback;

        public PluginHost PluginHost;

        private void PluginsCallback(object source, PluginEventArgs e) {
            switch (e.MessageType) {
                case PluginEventMessageType.Message:
                    if (e.Result is PluginReturnMessage response) {
                        Writer.SendData(response.Protocol, $"{response.Target} {response.Args}");

                        //if (response.Protocol != Protocols.PRIVMSG &&
                        //    !string.IsNullOrEmpty(response.Message))
                        //{
                        //    if (response.Message.StartsWith("#"))
                        //    {
                        //        Writer.Privmsg(response.Target);
                        //    }
                        //}
                        break;
                    }

                    Writer.Log(e.Result.ToString(), EventLogEntryType.Information);
                    break;
                case PluginEventMessageType.EventLog:
                    break;
                case PluginEventMessageType.Action:
                    if (!(e.Result is KeyValuePair<string, string>) &&
                        !(e.Result is PluginAssemblyType)) break;

                    switch (e.ActionType) {
                        case PluginActionType.AddCommand:
                            CommandRegistrarCallback(this,
                                new CommandRegistrarEventArgs((KeyValuePair<string, string>)e.Result));
                            break;
                        case PluginActionType.Load:
                            PluginHost.LoadDomain((PluginAssemblyType)e.Result);
                            break;
                        case PluginActionType.Unload:
                            PluginHost.UnloadDomain((PluginAssemblyType)e.Result);
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
            PluginHost.LoadAllDomains();
            PluginHost.StartAllPlugins();
        }
    }
}