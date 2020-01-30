#!/bin/bash

dotnet build && dotnet test --no-build -v normal /p:CollectCoverage=true /p:CoverletOutput=TestResults/ /p:CoverletOutputFormat=lcov && reportgenerator -reports:TestResults/coverage.info -targetdir:reports

