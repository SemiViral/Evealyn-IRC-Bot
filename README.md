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
- User databasing, access values, and a message timeout feature that will ignore messages from a user after so many queries.
- Modules: any Eve.*.dll file that implements the IModule interface and is placed anywhere inside the Modules folder will be type-loaded as an assembly.
- An advanced calculator: the calculator takes literal expressions such as 2+2 or 2(2), rather than 2 2 + or another form of it. It processes sine, tan, cos, asine, atan, acos, mod, and factorials.

# Implementing the IModule interface
Implementation of the IModule interface is fairly simple. Declare a method called OnChannelMessage, and that will be called on every channel message that meets base params in the Core assembly. You also have to declare a generic 'Dictionary<string, string> def', format being: <name of command or how it is referenced in use, usage and description of command>.

Example: `Dictionary<string, string> def => new Dictionary<string, string> { ["join"] = "(<channel>) - joins specified channel." };`

That is all. Obviously, your whole class (that implements IModule interface) will be loaded, and not single methods. If you don't understand Types and how classes/methods/fields work, you should learn that before trying to change things in this solution or adding modules to it.

It is reccomended (by myself, at least) that you make a different class for each seperate command, as it tends to make everything run smoother (and I don't know if it will mess anything up, but it shouldn't). It's also a lot quicker than a large SWITCH statement.

# Making use of the users database and good practice
This project makes use of [System.Data.Sqlite](https://system.data.sqlite.org/) for its database and querying.

To make use of the users database that is automatigically generated, simply follow a syntax similar to this:
	IrcBot.QueryDefaultDatabase("query here");

And if you would like to get the max ID value of the table, just use `IrcBot.GetLastDatabaseId();`. This will return an `Int32`.

# Ending note
I'm quite bad at doing comprehensive ReadMe's, so if I've missed anything feel free to e-mail me at semiviral@gmail.com, or I can be found on IRC at: irc.foonetic.net/#ministryofsillywalks
