#!/bin/bash

dotnet build && dotnet test --no-build -v normal /p:CollectCoverage=true

