FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/SchoolService/SchoolService.csproj src/SchoolService/
RUN dotnet restore src/SchoolService/SchoolService.csproj
COPY src/SchoolService/ src/SchoolService/
RUN dotnet publish src/SchoolService/SchoolService.csproj -c Release -o /app/publish --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SchoolService.dll"]
