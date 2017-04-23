dotnet restore
dotnet build /p:VersionSuffix="CI$env:BuildCounter"
dotnet test ./tests/Devsense.PHP.Phar.Tests/Devsense.PHP.Phar.Tests.csproj