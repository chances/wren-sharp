#!/usr/bin/env bash
  
if [[ -z "$1" ]]; then
  echo "Please give namespace, class name, or test name to filter tests"
  echo
  echo "i.e. \`dotnet test --filter DisplayName~<given_input>\`"
  exit 1
else
  dotnet test --filter DisplayName~$1
fi
