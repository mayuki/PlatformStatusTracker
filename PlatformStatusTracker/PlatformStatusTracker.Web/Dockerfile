FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY ["PlatformStatusTracker.Web/PlatformStatusTracker.Web.csproj", "PlatformStatusTracker.Web/"]
COPY ["PlatformStatusTracker.Core/PlatformStatusTracker.Core.csproj", "PlatformStatusTracker.Core/"]
RUN dotnet restore "PlatformStatusTracker.Web/PlatformStatusTracker.Web.csproj"
COPY . .
WORKDIR "/src/PlatformStatusTracker.Web"
RUN dotnet build "PlatformStatusTracker.Web.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "PlatformStatusTracker.Web.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "PlatformStatusTracker.Web.dll"]