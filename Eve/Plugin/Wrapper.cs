#region usings

using System;

#endregion

namespace Eve.Plugin {
    internal class PluginWrapper : MarshalByRefObject {
        internal EventHandler<LogEntry> LogEntryEventHandler;
        internal PluginHost PluginHost;
        internal EventHandler<PluginSimpleReturnMessage> SimpleMessageEventHandler;

        internal EventHandler TerminateBotEvent;

        private void PluginsCallback(object source, PluginReturnActionEventArgs e) {
            switch (e.ActionType) {
                case PluginActionType.Load:
                    PluginHost.LoadPluginDomain();
                    break;
                case PluginActionType.Unload:
                    break;
                case PluginActionType.None:
                    break;
                case PluginActionType.RunProcess:
                    break;
                case PluginActionType.SignalTerminate:
                    PluginHost.UnloadPluginDomain();
                    TerminateBotEvent?.Invoke(this, EventArgs.Empty);
                    break;
                case PluginActionType.RegisterMethod:
                    if (!(e.Result is PluginRegistrarEventArgs))
                        break;

                    PluginHost.RegisterMethod((PluginRegistrarEventArgs)e.Result);
                    break;
                case PluginActionType.SendMessage:
                    if (!(e.Result is PluginSimpleReturnMessage))
                        break;

                    SimpleMessageEventHandler.Invoke(this, (PluginSimpleReturnMessage)e.Result);
                    break;
                case PluginActionType.Log:
                    if (!(e.Result is LogEntry))
                        break;

                    LogEntryEventHandler.Invoke(this, (LogEntry)e.Result);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Start() {
            if (PluginHost != null)
                return;

            PluginHost = new PluginHost();
            PluginHost.PluginCallback += PluginsCallback;
            PluginHost.LoadPluginDomain();
            PluginHost.StartPlugins();
        }
    }
}