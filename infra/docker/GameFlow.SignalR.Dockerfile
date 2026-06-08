FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props GameFlow.TransactionEngine.sln ./
COPY src/GameFlow.Shared ./src/GameFlow.Shared
COPY src/GameFlow.SignalR ./src/GameFlow.SignalR

RUN dotnet restore ./src/GameFlow.SignalR/GameFlow.SignalR.csproj
RUN dotnet publish ./src/GameFlow.SignalR/GameFlow.SignalR.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "GameFlow.SignalR.dll"]
