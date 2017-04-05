dotnet restore
dotnet build /p:VersionSuffix="CI$env:BuildCounter"
dotnet test ./tests/PharPackage.Tests/PharPackage.Tests.csproj