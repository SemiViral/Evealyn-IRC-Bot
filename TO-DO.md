1. Allow plugins to access User and Channel lists/static fields
2. Restructure a bit more to increase scalability
- ~~Get simple commands structure functioning~~
- Implement plugin unloading
- Implement custom exceptions for more accurate error reporting
- ~~Add handlers for each message type (PRIVMSG, JOIN, PART, 376, etc.)~~
- ~~Create Enum types for each handler type~~ abandoned enums
- ~~Create TranslateHandler method to translate methods into their respective enums, and returning them~~
- ~~Possibly insert the handlers into a custom(?) or dictionary generic for referencing quicker~~
- ~~Modify ChannelMessage object's `string Type` to `HandlerType Type`~~ used IrcPtotocol
- ~~Create a new namespace for sending data (Eve.DataWriting, Eve.Messaging, something like that)~~
- ~~Place module interface and module loading methods into new namespace, Eve.Modules~~
- ~~Upgrade current system to load new modules into an AppDomain, so that they can be unloaded/reloaded~~
