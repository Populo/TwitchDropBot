# TwitchDropBot

## What it does

Checks an api that lists current twitch drops and posts them to a discord channel(s)

## How to use

Written for docker. Just create a compose.yml file and run it with `docker compose up -d`

## docker compose

```yaml
services:
  twitchdropbot:
    image: docker.populo.dev/twitchdropbot:latest
    container_name: TwitchDropBot
    restart: unless-stopped
#    depends_on:
#      mssql:
#        condition: service_healthy
    environment:
      - ErrorChannelId=123456789876 # comma separated list
      - PostChannelId=123456789876 # comma separated list
      - BotAdminUsers=123456789876 # comma separated list
      - DbHost=<hostname of DB server>
      - MSSQL_SA_PASSWORD=<MSSQL Server SA Password>
      - DbName=db_twitchdrop
      - DbUser=user_twitchdrop
      - DbPassword=<DB account password>
      - BotToken=<Discord bot token>
#    networks:
#      - mssql
networks:
  mssql:
    driver: bridge
```

### Sample mssql server compose file

```yaml
services:
    server:
        environment:
            - ACCEPT_EULA=Y
            - MSSQL_SA_PASSWORD=<MSSQL Server SA Password>
        ports:
            - 1433:1433
        container_name: mssql2022
        hostname: mssql2022
        restart: unless-stopped
        networks:
            - mssql
        healthcheck:
          test: /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "<MSSQL Server SA Password>" -Q "SELECT 1" -b -No
          interval: 10s
          timeout: 3s
          retries: 10
          start_period: 40s
        volumes:
            - ./mssql-data:/var/opt/mssql
        image: mcr.microsoft.com/mssql/server:2022-latest
networks:
  mssql:
    driver: bridge
```
