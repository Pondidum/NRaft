#! /bin/sh

# First parameter is build mode, defaults to Debug
MODE=${1:-Debug}

# Find the solution file in the root take it's name
NAME=$(basename $(ls *.sln | head -n 1) .sln)

dotnet restore
dotnet build --configuration $MODE

find ./src -iname "*.Tests.csproj" -type f -exec dotnet test --no-build "{}" --configuration $MODE \;

dotnet pack --no-build --configuration $MODE --output ../../.build
