dotnet restore
dotnet build DiscordLevelsBot.sln --configuration Release -o ./bin/live_release
dotnet bin\live_release\DiscordLevelsBot.dll
