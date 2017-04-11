
# Eve

## 4.x.x

### 4.3.0 -- New Plugins system, new Core features, cleaner code

 - Code is once again cleaner, hopefully more readable.

 - The plguins system has been revised, and I think this iteration is much more versatile. It allows for a method to subscribe to any `Protocols` type, which is a boon in and of itself.

 - Config file is now deserialised directly into a `BotConfig`. This got rid of a lot of boilerplate code.

 - API can now be added to the config file and referenced.

   * ChannelMessage now inherits MarshalByRefObj. This will probably change, as it is only a temporary fix for a minor bug.

### 4.2.0 -- Plugins fully functional, cleaned up code and encapsulation

 - * a note: I'm horrible at changelogging

 - Plugin events now execute correctly

 - User/ChannelOverlord no longer exist. In place the functions and lists are now in `IrcBot` class

 - `IrcBot` class is now one file.

 - Commands list is now held in `IrcBot` class as well

 - A couple of commands have been added to `Core.cs`, much closer to finishing this off

####4.2.2 -- Minor code cleaning, optimising

 - `PluginController` and `PluginHost` have been cleaned up.

 - 'Quit' command implemented to the Core plugin, shutting the bot down.
 
 - Implemented new log type, `IrcLogEntryType`, now used in place of `EventLogEntryType`.

####4.2.3 -- Vigorously cleaned various classes

 - `Controller.cs` should be much more readable now.

 - added 'YouTube' functionality

### 4.1.2 -- Plugins and commands list functional, more resutructuring/refactoring

 - The `Result` field of a `PluginEventArgs` is now of type `object`
 
 - Plugins can now send back a response class type, called `PluginChannelMessageResponse` (working on the name)
 
 - Command list is now working, and is held inside of `PluginWrapper` as a static dictionary
 
 - VariablesManager/PassableMutableObject/Whatever else I have named it is now gone. In it's place is a `Database.cs` file. The methods related to the User and Channel lists/classes have been moved to their respective classes in GeneralClasses (will restructure this too).
 
 - Utilities no longer exists in the Eve namespace. User/Channel static extension methods have been moved to their respective classes while `CaseEquals` for strings and `AddFrom` for dictionaries has been completely removed. The Utilities class has been moved to the `Core` project, and will be employed when the define and lookup commands are functional.
 
 - `CalcPlugin` and `Core` have been merged into one plugin.
 
 - Split `Runtime()` method into two pieces, `ListenToStream()` and `ExecuteRuntime()`

### 4.0.0 -- Events, a CHANGELOG

 - Added a changelog. I think I should've done this a while ago.

 - Eve now utilises an event-based system by which plugins can subscribe to, documentation for this is coming soon. For now, just look at the source of Core.cs, it's a very simple plugin (doesn't do anything!)

 - Introduced `Writer` class. For general outputting to console, use `Writer.Log()` (note: this also writes to the log file). For writing to the stream use `Writer.SendData()` or `Writer.Privmsg()`.

 - `IModule` interface is now `IPlugin` interface. What's being added to Eve now is much closer to plugins than to modules, as the plugins have a lot of flexibility now.

 - `IrcBot` class is now in two files; `IrcBot.cs` is the primary file, which contains declarations and the `Runtime()` method. `IrcBot.Extended.cs` is a rest-of-the-methods collection.
