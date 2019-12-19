#!/usr/bin/env bash

cd WrenSharp.Tests
dotnet test
dotnet reportgenerator -reports:coverage/coverage.opencover.xml -targetdir:coverage

if [[ $1 = "open" || $1 = "-o" || $1 = "--open" ]]; then
  # Fail the script if open command isn't found
  set -e
  OPEN_COMMAND=`command -v xdg-open || command -v open`

  # Open coverage results in default browser
  $OPEN_COMMAND coverage/index.htm
fi
