# Evealyn, an C# IRC bot
Evealyn is an IRC bot that I decided to start as a way to get known with the programming language C#.

If you're looking to take this code and learn from it, be wary. It may house many mistakes. As I become more proficient in C# I will try to come back and improve or refactor the code, but I cannot promise the integrity of her code. As it stands, I'd take Evealyn as a rough guideline for something that you can aim for when creating your own IRC bot, which I highly recommend.

# Implementing the IPlugin interface
Implementation of the IPlugin interface is fairly simple; use `Core.cs` as a guideline for this.

Example: `Dictionary<string, string> def => new Dictionary<string, string> { ["join"] = "(<channel>) - joins specified channel." };`

That is all. Obviously, your whole class (that implements IPlugin interface) will be loaded, and not single methods. If you don't understand types and how classes/methods/fields work, you should learn that before trying to change things in this solution or adding plugins to it.

It is reccomended (by myself, at least) that you not make a different class for each command. The cleanest method of implemention for your methods would as seen in `Core.cs`.

# Note on databasing and dependencies
This project makes use of [System.Data.Sqlite](https://system.data.sqlite.org/) for its database and querying.

For the configuation template, [Json.Net](http://www.newtonsoft.com/json) is used.

# Ending note
I'm quite bad at doing comprehensive READMEs, or documentation in general, so if I've missed anything feel free to e-mail me at semiviral@gmail.com.
