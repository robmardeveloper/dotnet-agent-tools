Small projects broken on purpose, used by the tests. They are not part of the solution and are copied to a temporary
folder before each test builds them.

- `Clean`: builds with no error or warning. It has no `global.json`, so `dotnet test` would use VSTest.
- `CompileError`: one compiler error (CS0103) and one warning (CS8602).
- `MissingPackage`: references a NuGet package that does not exist (NU1101).
- `FailingTests`: xUnit v3 on Microsoft.Testing.Platform, with one passing, two failing and one skipped test.
