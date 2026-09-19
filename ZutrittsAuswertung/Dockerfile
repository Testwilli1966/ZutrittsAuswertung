# Build-Stage: kompiliert die App (nur während des Add-on-Builds benötigt)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish ZutrittsAuswertung.csproj -c Release -o /app

# Laufzeit-Stage: nur die ASP.NET-Core-Runtime, kein SDK -> kleines, schlankes Image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_ENVIRONMENT=Production
# Workstation-GC statt Server-GC: geringerer Speicherbedarf, passend für den Pi
ENV DOTNET_gcServer=0

EXPOSE 5080

ENTRYPOINT ["dotnet", "ZutrittsAuswertung.dll"]
