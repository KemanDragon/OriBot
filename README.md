# Oribot v5.0.0 Public Repository | Project Scope

## What is Oribot?

A custom bot running on .NET 6 that is built from the ground up for the Ori the Game Discord, utilizing Discord.NET and NoSQL Database.
More Documentation can be found [here](https://github.com/SlamTheDragon/Oribot-v5.0.0-Obsidian.md-Project-Documentation).

## Project Scope  

- Moderation Features
- User Customization
- Passive Bot Interactions
- [...]

## Bot Infrastructure

- Logging
- Command Handling
  - Passive Commands
  - Slash Commands
  - Traditional Commands
- Storage
- Permission Access
- [...]

## Mandatory Features

- Must have a simple command structure. This means more commands, less subcommands.
  - Break up command trees into more easily digested, smaller commands with fewer subcommands
  - Test moving to slash commands
- Overhauled log system. Must be able to enter, view and delete entries.
  - Implement a feature for deleting log entries instead of simply hiding them
  - Must use JSON database for more reliable storage
  - Bot must DM the user any warns, mutes, bans etc.
- Should migrate anti-spam, Steam API, and Gallery Pins since they’re API-independent.
- Improve maintainability of the bot
  - Implement encapsulation
  - Should be accessible for multiple servers.
- Must be compatible with existing data.
  - Alternatively, write conversion script to convert existing data to the new format.
- Reimplement the existing profile system with identical features in JSON.
- Reimplement access controls #permission-access
  - Simplify with a tighter scale, rather than 0 to 128 like now
    - New User
    - Validated User
    - Moderator
    - Bot Admin
- Rework the ticket system
  - Allow use of ticket within the server for less confusion
- Add Bot-Commands responses

### Optional features

- Dice Rolls

### Libraries and Targets

- C#
- Discord.NET
- Ubuntu Server as Target Platform
- JSON NoSQL Database

## Potential (Slash) #Commands

`>> mute [User ID] [Reason] [Duration]`
 Mutes a user for the given duration, logs the mute, and DMs them with [Reason]. Moderator only.

`>> unmute [User ID] [Reason]`
 Unmutes a user, moderator only.

`>> warn [harsh, minor] [User ID] [Reason]`
 DMs the user with an automated warning, and logs it internally. Moderator only.

`>> warnlog [User ID] [Entry Index]`
 Allows for viewing a user's log, or view a specified entry. Moderator only.

`>> deletelog  [User ID] [Entry Index] [Reason]`
 Deletes the given log entry and logs a reason for deletion. Moderator only.

`>> note [User ID] [Note Content]`
 Logs a note about a potentially problematic user. Moderator only.

`>> ban [User ID] [Reason]`
 Bans the given user and DMs them with [Reason]. Moderator only.

`>> profile [User ID]`
 Brings up a user's profile, or your own if you don't include an ID.

`>> updatestatus [Status Text]`
 Updates the status entry of your profile.

`>> updatebio [Bio Text]`
 Updates the bio entry of your profile.
%%can perhaps add a subcommand under profile? Same goes to others (just in case)%%

`>> addbadge [User ID] [Badge Name]`
 Adds a badge to a user's profile. Moderator only.

`>> color [Color Name | List]`
 Adds a color role to you, or lists available colors.

`>> hug`
 Ori gives you a hug! (unless… :flushed:)

`>> restart [reason]`
 Forces the bot to restart. Bot Admin Only. [reason] is optional.

`>> shutdown [reason]`
 Shuts the bot down. Bot Admin Only. [reason] is optional.

`>> ticket`
 Opens a thread where moderators can address an issue.

`>> chw [channel-id]` (alias)
`>> channelWhitelist [channel-id]`
 Whitelists a channel for the bot to not monitor

`>> chs [channel-id] [bot | logs | art]` (alias)
`>> channelSettings [channel-id] [ bot | logs | art]`
 Sets a channel if it is either a bot-commands channel, a log channel, or an art-gallery channel.
