#!/bin/bash

dotnet build && dotnet test --no-build -v normal /p:CollectCoverage=true /p:CoverletOutput=TestResults/ /p:CoverletOutputFormat=lcov /p:Exclude=[TauManager.Views]* /p:exclude-by-attribute=CompilerGeneratedAttribute /p:ExcludeByFile=\"../TauManager/Areas/Identity/*.cs\" && reportgenerator -reports:TestResults/coverage.info -targetdir:reports

