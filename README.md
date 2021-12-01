DiscordLevelsBot
----------------

A simple bot for activity tracking / leveling on Discord. Inspired by Mee6. Designed for more customization (for my own needs at least) and to not have all the unrelated Mee6 features in the way (especially not the whole 'premium' thing yknow).

### Setup

- 0: Before setup: This is intended to run on a Linux server, with `git`, `screen`, and `dotnet` (6.0) installed. If you're not in this environment... you're on your own for making it work. Should be easy, but I'm only documenting my own use case here.
- 1: Clone this repo with `git clone`
- 2: Make sure to checkout submodules as well: `git submodule update --init --recursive` (the `start.sh` will automatically do this for you)
- 2: create folder `config` at top level
- 3: You need to have a Discord bot already - check [this guide](https://discordpy.readthedocs.io/en/stable/discord.html) if you don't know how to get one
- 4: within `config` create file `token.txt` with contents being your Discord bot's token
- 5: `./start.sh`. Will run in a screen which you can attach to with `screen -r levelsbot`

### Licensing pre-note:

This is an open source project, provided entirely freely, for everyone to use and contribute to.

If you make any changes that could benefit the community as a whole, please contribute upstream.

### The short of the license is:

You can do basically whatever you want (as long as you give credit), except you may not hold any developer liable for what you do with the software.

### The long version of the license follows:

The MIT License (MIT)

Copyright (c) 2021 Alex "mcmonkey" Goodwin

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