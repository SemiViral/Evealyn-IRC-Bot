#This is grossly out-of-date, and will be updated eventually

# Evealyn, a C# IRC bot
Evealyn is an IRC bot that I decided to start as a way to get known with the programming language C#. I'd say she's taught me fairly well about various things, including but of course not limited to:
- Modularity
- Method management, when and when to not use them
- Variables and using property access
- Shorthands
- LINQ

If you're looking to take this code and learn from it, be wary. It may house many mistakes. As I become more proficient in C# I will try to come back and improve or refactor the code, but I can't make a big promise not knowing where things will lead. As it stands, I'd take Evealyn as a rough guideline for something that you can aim for when creating your own IRC bot.

# Features
Evealyn offers these various features:
- User databasing, dynamic plugin loading/unloading, prebuilt core of useful commands.

# Implementing the IPlugin interface
Implementation of the IPlugin interface is fairly simple. Declare a method called OnChannelMessage, and that will be called on every channel message that meets base params in the Core assembly. You also have to declare a generic 'Dictionary<string, string> def', format being: <name of command or how it is referenced in use, usage and description of command>.

Example: `Dictionary<string, string> def => new Dictionary<string, string> { ["join"] = "(<channel>) - joins specified channel." };`

That is all. Obviously, your whole class (that implements IPlugin interface) will be loaded, and not single methods. If you don't understand types and how classes/methods/fields work, you should learn that before trying to change things in this solution or adding modules to it.

It is reccomended (by myself, at least) that you not make a different class for each command. The most cleanly way to implement your methods would as seen in `Core.cs`

# Making use of the users database and good practice
This project makes use of [System.Data.Sqlite](https://system.data.sqlite.org/) for its database and querying.

To make use of the users database that is automatigically generated, simply follow a syntax similar to this:
`IrcBot.QueryDefaultDatabase("query here");`

And if you would like to get the max ID value of the table, just use `IrcBot.GetLastDatabaseId();`. This will return an `Int32`.

# Ending note
I'm quite bad at doing comprehensive 'ReadMe's, so if I've missed anything feel free to e-mail me at semiviral@gmail.com.
