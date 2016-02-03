To-Do list:
#Handlers
- ~~Add handlers for each message type (PRIVMSG, JOIN, PART, 376, etc.)~~
- ~~Create Enum types for each handler type~~ abandoned enums
- ~~Create TranslateHandler method to translate methods into their respective enums, and returning them~~
- ~~Possibly insert the handlers into a custom(?) or dictionary generic for referencing quicker~~
- ~~Modify ChannelMessage object's `string Type` to `HandlerType Type`~~ used IrcPtotocol

#DataWriting
- Create a new namespace for sending data (Eve.DataWriting, Eve.Messaging, something like that)

#Modules
- ~~Place module interface and module loading methods into new namespace, Eve.Modules~~
- Upgrade current system to load new modules into an AppDomain, so that they can be unloaded/reloaded
