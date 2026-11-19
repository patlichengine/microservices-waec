FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/ExamGateway/ExamGateway.csproj src/ExamGateway/
RUN dotnet restore src/ExamGateway/ExamGateway.csproj
COPY src/ExamGateway/ src/ExamGateway/
RUN dotnet publish src/ExamGateway/ExamGateway.csproj -c Release -o /app/publish --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "ExamGateway.dll"]
