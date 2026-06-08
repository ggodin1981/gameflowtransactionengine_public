FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props GameFlow.TransactionEngine.sln ./
COPY src/GameFlow.Shared ./src/GameFlow.Shared
COPY src/GameFlow.Api ./src/GameFlow.Api

RUN dotnet restore ./src/GameFlow.Api/GameFlow.Api.csproj
RUN dotnet publish ./src/GameFlow.Api/GameFlow.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "GameFlow.Api.dll"]
