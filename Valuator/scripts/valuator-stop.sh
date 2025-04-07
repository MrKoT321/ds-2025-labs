#!/usr/bin/env bash

echo Stopping Valuator on port 5001
echo Searching for a process using port 5001...
for /F "tokens=5" %%A in ('netstat -ano ^| find "5001"') do (
    echo Stopping web application on port 5001 with PID: %%A...
    taskkill /PID %%A /F > nul 2>&1
)
echo Valuator on port 5001 stopped.

echo Stopping Valuator on port 5002
echo Searching for a process using port 5002...
for /F "tokens=5" %%A in ('netstat -ano ^| find "5002"') do (
    echo Stopping Valuator on port 5002 with PID: %%A...
    taskkill /PID %%A /F > nul 2>&1
)
echo Valuator on port 5002 stopped.

echo Stopping Nginx using docker-compose...
cd ..\nginx\conf
docker-compose down
echo Nginx stopped.

echo Stopping Redis using docker-compose...
cd ..\..\Valuator
docker-compose down
echo Redis stopped.

cd ..\scripts

echo All components successfully stopped.