FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/StudentService/StudentService.csproj src/StudentService/
RUN dotnet restore src/StudentService/StudentService.csproj
COPY src/StudentService/ src/StudentService/
RUN dotnet publish src/StudentService/StudentService.csproj -c Release -o /app/publish --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "StudentService.dll"]
