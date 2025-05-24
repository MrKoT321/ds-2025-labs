FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /source

COPY *.sln ./
COPY ./RankCalculator/*.csproj ./RankCalculator/

RUN dotnet restore RankCalculator/RankCalculator.csproj

COPY ./RankCalculator/ ./RankCalculator/
COPY ./RankCalculator/appsettings*.json ./RankCalculator/

RUN dotnet publish RankCalculator/RankCalculator.csproj -c release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app .
COPY --from=build /source/RankCalculator/appsettings*.json .

ENTRYPOINT ["dotnet", "RankCalculator.dll"]