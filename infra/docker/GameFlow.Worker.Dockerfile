FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props GameFlow.TransactionEngine.sln ./
COPY src/GameFlow.Shared ./src/GameFlow.Shared
COPY src/GameFlow.Worker ./src/GameFlow.Worker

RUN dotnet restore ./src/GameFlow.Worker/GameFlow.Worker.csproj
RUN dotnet publish ./src/GameFlow.Worker/GameFlow.Worker.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GameFlow.Worker.dll"]
