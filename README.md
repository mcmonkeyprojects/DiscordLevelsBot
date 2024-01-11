DiscordLevelsBot
----------------

A simple bot for activity tracking / leveling on Discord. Inspired by Mee6. Designed for more customization (for my own needs at least) and to not have all the unrelated Mee6 features in the way (especially not the whole 'premium' thing yknow).

By default, when a user posts a message (at least one minute after their last message), they get a random bit of XP from 15 to 25 points. This XP is used to obtain levels, that are incrementally harder to reach (reaching level 1 only takes about 5 messages, but level 100 takes millions).

View an example web leaderboard [Here](https://levels.mcmonkey.org/leaderboard/315163488085475337).

### Want To Add The Public Instance?

- Just [click here](https://discord.com/api/oauth2/authorize?client_id=915501392519651358&permissions=2415922176&scope=bot%20applications.commands).

### Admin Configuration

- For now, the admin configuration panel is very cheap and simple: type `@LevelsBot admin-configure` for info.
- You can do for example `@LevelsBot admin-configure restrict_channel 1234` to prevent that channel from being used to gain XP.
- You can configure channel restrictions, level role rewards (role auto-granted when a user hits a level), XP per tick minimum and maximum, time between messages before XP should be allowed to tick again, minimum level before "level up!" notifications show.

### Setup Your Own Instance

- 0: Before setup: This is intended to run on a Linux server, with `git`, `screen`, and `dotnet-6-sdk` installed. If you're not in this environment... you're on your own for making it work. Should be easy, but I'm only documenting my own use case here.
- 1: Clone this repo with `git clone`
- 2: Make sure to checkout submodules as well: `git submodule update --init --recursive` (the `start.sh` will automatically do this for you)
- 3: create folder `config` at top level
- 4: You need to have a Discord bot already - check [this guide](https://discordpy.readthedocs.io/en/stable/discord.html) if you don't know how to get one. Requires messages intent, and slash commands grant. Make sure to add the bot to your server(s).
- 5: within `config` create file `token.txt` with contents being your Discord bot's token
- 6: if you want, create `config.fds` and configure it according to config reference below
- 7: `./start.sh`. Will run in a screen which you can attach to with `screen -r levelsbot`

### Config File Reference

Reference format for `config.fds`:

```yml
webpage:
    # If true, will enable the leaderboard webpage host
    enable: false
    # If enabled, you can configure the listen address
    # It is strongly recommended you bind an internal port and use a reverse proxy like nginx or apache2.
    listen: http://127.0.0.1:8099/
    # The external address of the website, for generated links
    address: https://example.com/
```

### Licensing pre-note:

This is an open source project, provided entirely freely, for everyone to use and contribute to.

If you make any changes that could benefit the community as a whole, please contribute upstream.

### The short of the license is:

You can do basically whatever you want (as long as you give credit), except you may not hold any developer liable for what you do with the software.

### The long version of the license follows:

The MIT License (MIT)

Copyright (c) 2021-2024 Alex "mcmonkey" Goodwin

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.