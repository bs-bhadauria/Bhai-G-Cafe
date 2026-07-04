FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY server/BhaiGCafe.Api.csproj server/
RUN dotnet restore server/BhaiGCafe.Api.csproj

COPY server/ server/
RUN dotnet publish server/BhaiGCafe.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

# Render provides PORT dynamically; default to 8080 for local/container runs.
CMD ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080} dotnet BhaiGCafe.Api.dll"]
