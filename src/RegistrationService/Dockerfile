FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/RegistrationService/RegistrationService.csproj src/RegistrationService/
RUN dotnet restore src/RegistrationService/RegistrationService.csproj
COPY src/RegistrationService/ src/RegistrationService/
RUN dotnet publish src/RegistrationService/RegistrationService.csproj -c Release -o /app/publish --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RegistrationService.dll"]
