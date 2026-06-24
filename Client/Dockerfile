FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["AlarmClient.csproj", "."]
RUN dotnet restore "./AlarmClient.csproj"
COPY . .
RUN dotnet publish "./AlarmClient.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .


ENV HUB_URL=http://host.docker.internal/hubs/alarms

ENTRYPOINT ["dotnet", "AlarmClient.dll"]
