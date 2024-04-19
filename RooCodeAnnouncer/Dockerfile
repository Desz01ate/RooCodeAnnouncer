FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["RooCodeAnnouncer/RooCodeAnnouncer.csproj", "RooCodeAnnouncer/"]
COPY ["RooCodeAnnouncer.Contracts/RooCodeAnnouncer.Contracts.csproj", "RooCodeAnnouncer.Contracts/"]
COPY ["RooCodeAnnouncer.Discord/RooCodeAnnouncer.Discord.csproj", "RooCodeAnnouncer.Discord/"]
RUN dotnet restore "RooCodeAnnouncer/RooCodeAnnouncer.csproj"
COPY . .
WORKDIR "/src/RooCodeAnnouncer"
RUN dotnet build "RooCodeAnnouncer.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "RooCodeAnnouncer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RooCodeAnnouncer.dll"]
