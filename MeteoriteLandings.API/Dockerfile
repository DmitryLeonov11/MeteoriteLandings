FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["MeteoriteLandings.API/MeteoriteLandings.API.csproj", "MeteoriteLandings.API/"]
COPY ["MeteoriteLandings.Application/MeteoriteLandings.Application.csproj", "MeteoriteLandings.Application/"]
COPY ["MeteoriteLandings.Domain/MeteoriteLandings.Domain.csproj", "MeteoriteLandings.Domain/"]
COPY ["MeteoriteLandings.Infrastructure/MeteoriteLandings.Infrastructure.csproj", "MeteoriteLandings.Infrastructure/"]

RUN dotnet restore "MeteoriteLandings.API/MeteoriteLandings.API.csproj"

COPY . .
WORKDIR "/src/MeteoriteLandings.API"

RUN dotnet publish "MeteoriteLandings.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "MeteoriteLandings.API.dll"]