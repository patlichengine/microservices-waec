FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/NotificationWorker/NotificationWorker.csproj src/NotificationWorker/
RUN dotnet restore src/NotificationWorker/NotificationWorker.csproj
COPY src/NotificationWorker/ src/NotificationWorker/
RUN dotnet publish src/NotificationWorker/NotificationWorker.csproj -c Release -o /app/publish --no-restore
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NotificationWorker.dll"]
