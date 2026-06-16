# --- Faza 1: Build ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Kopiramo samo .csproj prvo - omogucava Docker-u da kesira "dotnet restore"
# korak ako se samo kod promeni a ne i zavisnosti (brze rebuild-ove)
COPY NotificationService.csproj .
RUN dotnet restore

# Sad kopiramo sav ostali kod i build-ujemo
COPY . .
RUN dotnet publish -c Release -o /app/publish

# --- Faza 2: Runtime ---
# Koristimo "aspnet" runtime image (manji od SDK, sadrzi samo sta treba da se POKRENE app, ne i da se build-uje)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Kontejner ce slusati na portu 8080 (standardna konvencija za .NET kontejnere)
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "NotificationService.dll"]