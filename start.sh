#!/bin/bash
git pull origin master
git submodule update --init --recursive
dotnet build DiscordLevelsBot.sln --configuration Release -o ./bin/live_release
screen -dmS levelsbot dotnet bin/live_release/DiscordLevelsBot.dll $1
