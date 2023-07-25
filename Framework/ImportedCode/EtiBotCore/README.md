# EtiBotCore
## WARNING: This is not production ready, and may have severe bugs or issues in it that prevent it from functioning properly.
A Discord Bot Framework in C# that experiments with a more idiomatic design.

# Introduction
*EtiBotCore* was initially designed for the [official Ori Discord Server](https://discord.gg/orithegame) as a completely home-brewed framework for the server's bot to run on top of. The main payoff, at the cost of a *lot* of work, was that I would have low level network control of the bot -- I can dictate **exactly** how it works.

In designing this system, I saw a great opportunity to test something that - to my knowledge - hasn't been done before. *EtiBotCore* focuses on having an **idiomatic design**. Unlike other frameworks, where to modify an object you may need to call a special method to alter an object, *EtiBotCore* allows you to directly edit the object just as you might if the program were entirely local and not networked. Want to change a role's name? `role.Name = "Your Desired Name Here"` is the way to do it.

Technically, you could argue I just *moved* the method definitions. In order to avoid spamming Discord with requests, you must call a `BeginChanges` method before modifying an object, and finish it by calling an `ApplyChanges` method, which takes in an optional reason or reasons as a dictionary (intended for each property).

Overall, I think this method of editing objects boils down to personal preference. I prefer the idiomatic style where I can just modify my object in-line and it works out of the box. I think it feels more natural, and makes the management of various things around Discord feel really straightforward.

# Plans
The only big change I want to make right away is to add events to objects themselves. A `Guild` should provide the guild events, and those events should only fire if Discord sends one aimed at that guild. This should extend to child objects, for instance, instead of `Guild` having an event for a member changing, each individual member should instead have its own events that fire for that member object alone. Of course, these events will be offered globally, which is practically a requirement for certain interactions.
