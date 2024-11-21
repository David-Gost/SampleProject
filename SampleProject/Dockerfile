FROM --platform=linux/amd64  mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM --platform=linux/amd64  mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SampleProject.csproj", "SampleProject/"]
RUN dotnet restore "SampleProject/SampleProject.csproj"

WORKDIR "/src/SampleProject"
COPY . .
RUN dotnet build "SampleProject.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
RUN dotnet publish "SampleProject.csproj" -c $BUILD_CONFIGURATION  --no-restore -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SampleProject.dll"]
