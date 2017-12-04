Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$config = "Release"

if(Test-Path .\dist) {
	Remove-Item -Path .\dist -Recurse -Force -ErrorAction SilentlyContinue
}

dotnet restore .\Hangfire.MaximumConcurrentExecutions\Hangfire.MaximumConcurrentExecutions.csproj
dotnet restore .\SampleApp\SampleApp.csproj

dotnet build .\SampleApp\SampleApp.csproj -c $config
dotnet build .\Hangfire.MaximumConcurrentExecutions\Hangfire.MaximumConcurrentExecutions.csproj -c $config -o .\..\dist

# push the package - assume default key has been set on this machine
dotnet nuget push  .\dist\*.nupkg  -s https://www.nuget.org/api/v2/package 