FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5002

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/ConsensusService/ConsensusService.csproj", "src/ConsensusService/"]
RUN dotnet restore "src/ConsensusService/ConsensusService.csproj"

COPY . .
WORKDIR "/src/src/ConsensusService"
RUN dotnet build "ConsensusService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ConsensusService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ConsensusService.dll"]