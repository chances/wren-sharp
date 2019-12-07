# wren-sharp

C# bindings to the [Wren](http://wren.io/) scripting language

## Testing

Test coverage via [ReportGenerator](https://danielpalme.github.io/ReportGenerator/usage.html).

```shell
cd WrenSharp.Tests
dotnet test /p:CollectCoverage=true, /p:CoverletOutputFormat="lcov,opencover" /p:CoverletOutput=coverage/,
dotnet reportgenerator "-reports:coverage/coverage.opencover.xml" "-targetdir:coverage"
```

## TODO

- Reference the [libuv nuget package](https://www.nuget.org/packages/Libuv/) for bundling native libraries
- [Loading Native Libararies](https://dev.to/jeikabu/loading-native-libraries-in-c-fh6), too?
