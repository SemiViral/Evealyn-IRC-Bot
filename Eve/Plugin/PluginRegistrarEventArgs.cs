#region usings

using System;
using System.Collections.Generic;

#endregion

namespace Eve.Plugin {
    public class PluginRegistrarEventArgs : MarshalByRefObject {
        public readonly PluginMethodWrapper Method;

        public PluginRegistrarEventArgs(string protocolType, Action<object, ChannelMessage> pluginMethod, KeyValuePair<string, string> definition = default(KeyValuePair<string, string>)) {
            ProtocolType = protocolType;
            Method = new PluginMethodWrapper(pluginMethod);
            Definition = definition;
        }

        public string ProtocolType { get; }

        public KeyValuePair<string, string> Definition { get; }
    }

    public sealed class PluginMethodWrapper : MarshalByRefObject {
        private readonly Action<object, ChannelMessage> internalDelegate;

        public PluginMethodWrapper(Action<object, ChannelMessage> method) {
            internalDelegate = method;
        }

        public void Invoke(object source, ChannelMessage channelMessage) {
            internalDelegate.Invoke(source, channelMessage);
        }
    }
}