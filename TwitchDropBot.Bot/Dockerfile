FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TwitchDropBot.Bot/TwitchDropBot.Bot.csproj", "TwitchDropBot.Bot/"]
COPY ["TwitchDropBot.Service/TwitchDropBot.Service.csproj", "TwitchDropBot.Service/"]
COPY ["TwitchDropBot.Data/TwitchDropBot.Data.csproj", "TwitchDropBot.Data/"]
RUN dotnet restore "TwitchDropBot.Bot/TwitchDropBot.Bot.csproj"
COPY . .
WORKDIR "/src/TwitchDropBot.Bot"
RUN dotnet build "./TwitchDropBot.Bot.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TwitchDropBot.Bot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TwitchDropBot.Bot.dll"]
