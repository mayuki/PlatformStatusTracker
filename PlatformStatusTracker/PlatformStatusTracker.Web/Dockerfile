FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "PlatformStatusTracker/PlatformStatusTracker.Web/PlatformStatusTracker.Web.csproj"
RUN dotnet publish "PlatformStatusTracker/PlatformStatusTracker.Web/PlatformStatusTracker.Web.csproj" -c Release -o /app --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "PlatformStatusTracker.Web.dll"]