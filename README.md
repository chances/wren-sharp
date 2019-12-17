# wren-sharp [![WrenSharp CI](https://github.com/chances/wren-sharp/workflows/WrenSharp%20CI/badge.svg)](https://github.com/chances/wren-sharp/actions)

C# bindings to the [Wren](http://wren.io/) scripting language.

## Testing

[![Code Coverage](https://codecov.io/gh/chances/wren-sharp/branch/master/graph/badge.svg)](https://codecov.io/gh/chances/wren-sharp)

Test coverage via [ReportGenerator](https://danielpalme.github.io/ReportGenerator/usage.html).

```shell
cd WrenSharp.Tests
dotnet test /p:CollectCoverage=true, /p:CoverletOutputFormat="lcov,opencover" /p:CoverletOutput=coverage/,
dotnet reportgenerator "-reports:coverage/coverage.opencover.xml" "-targetdir:coverage"
```

## TODO

- Reference the [libuv nuget package](https://www.nuget.org/packages/Libuv/) for bundling native libraries
- [Loading Native Libararies](https://dev.to/jeikabu/loading-native-libraries-in-c-fh6), too?
