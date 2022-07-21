#!/bin/bash

REPO_ROOT=$(dirname $0)/../

echo "${REPO_ROOT}"

VERSION_PREFIX=$(yq -p xml .Project.PropertyGroup.VersionPrefix ${REPO_ROOT}/Directory.Build.props)

SHORT_HASH=$(git rev-parse --short HEAD)
CURRENT_BRANCH=$(git rev-parse --abbrev-ref HEAD)

NUGET_REPO_NAME="github-georgevella"


rm -rf ./packages

dotnet clean
dotnet build -c Release --version-suffix "${CURRENT_BRANCH}.${SHORT_HASH}"
dotnet pack -c Release --version-suffix "${CURRENT_BRANCH}.${SHORT_HASH}" -o ./packages

dotnet nuget push ./packages/*.nupkg --skip-duplicate -s ${NUGET_REPO_NAME}