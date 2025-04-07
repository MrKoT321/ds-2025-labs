#!/usr/bin/env bash

set -e

cd ../Valuator
dotnet run --urls "http://0.0.0.0:5001" >> /dev/null 
echo "Valuator (port 5001) started."

dotnet run --urls "http://0.0.0.0:5002" >> /dev/null 
echo "Valuator (port 5002) started."

cd ../
docker-compose up -d >> /dev/null 
echo "Nginx (port 8080) started."

cd nginx\conf
docker-compose up -d >> /dev/null 
echo "Redis (port 6379) started."

echo "All components successfully started."