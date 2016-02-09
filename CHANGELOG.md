#Eve 4.0

### Events, a CHANGELOG

 - Added a changelog. I think I should've done this a while ago.

 - Eve now utilises an event-based system by which plugins can subscribe to, documentation for this is coming soon. For now, just look at the source of Core.cs, it's a very simple plugin (doesn't do anything!)

 - Introduced `Writer` class. For general outputting to console, use `Writer.Log()` (note: this also writes to the log file). For writing to the stream use `Writer.SendData()` or `Writer.Privmsg()`.

 - `IModule` interface is now `IPlugin` interface. What's being added to Eve now is much closer to plugins than to modules, as the plugins have a lot of flexibility now.

 - `IrcBot` class is now in two files; `IrcBot.cs` is the primary file, which contains declarations and the `Runtime()` method. `IrcBot.Extended.cs` is a rest-of-the-methods collection.