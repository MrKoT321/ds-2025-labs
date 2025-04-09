FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /source

COPY *.sln . 
COPY Valuator/*.csproj ./Valuator/
COPY Valuator.Tests/*.csproj ./Valuator.Tests/

RUN dotnet restore

COPY Valuator/. ./Valuator/
COPY Valuator.Tests/. ./Valuator.Tests/

RUN dotnet publish -c release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

RUN apt-get update && \
    apt-get install -y redis-tools && \
    rm -rf /var/lib/apt/lists/* 

COPY /scripts/init-redis.sh /app/init-redis.sh
RUN chmod +x /app/init-redis.sh

COPY --from=build /app ./

EXPOSE 8080
ENTRYPOINT ["/bin/bash", "-c", "/app/init-redis.sh && dotnet Valuator.dll"]
