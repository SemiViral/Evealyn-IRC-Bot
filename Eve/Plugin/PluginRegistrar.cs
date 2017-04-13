#region usings

using System;
using System.Collections.Generic;
using Eve.Types.Irc;

#endregion

namespace Eve.Plugin {
    public class PluginRegistrar : MarshalByRefObject {
        public readonly PluginMethodWrapper Method;

        /// <summary>
        /// This is the constructor for a PluginRegistrarEventArgs
        /// </summary>
        /// <param name="commandType">This represents the IRC command the method is triggered by</param>
        /// <param name="pluginMethod">The method instance itself</param>
        /// <param name="definition">This is an optional <see cref="KeyValuePair{TKey,TValue}"/> that is added to the commands index</param>
        public PluginRegistrar(string commandType, Action<object, ChannelMessage> pluginMethod, KeyValuePair<string, string> definition = default(KeyValuePair<string, string>)) {
            CommandType = commandType;
            Method = new PluginMethodWrapper(pluginMethod);
            Definition = definition;
        }

        public string CommandType { get; }

        public KeyValuePair<string, string> Definition { get; }
    }

    /// <summary>
    /// This class is used to wrap the plugin method instances in an object type that can be marshalled across the app-domain boundary.
    /// This is necessary due to <see cref="List{T}"/>
    /// </summary>
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