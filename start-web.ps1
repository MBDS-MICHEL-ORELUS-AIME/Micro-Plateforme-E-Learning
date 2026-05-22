Get-Process E-learningProject.Web -ErrorAction SilentlyContinue | Stop-Process -Force

$env:DOTNET_ROLL_FORWARD = 'Major'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = 'http://localhost:5230'

$hostName = 'localhost'
$port = '5432'
$database = 'MicroLmsDb'
$username = 'postgres'

if ([string]::IsNullOrWhiteSpace($env:MICROLMS_CONNECTION_STRING)) {
	$password = if ([string]::IsNullOrWhiteSpace($env:POSTGRES_PASSWORD)) { 'postgres' } else { $env:POSTGRES_PASSWORD }
	$env:MICROLMS_CONNECTION_STRING = "Host=$hostName;Port=$port;Database=$database;Username=$username;Password=$password;Search Path=public,lms;Include Error Detail=true"
}

Write-Host "Demarrage de l'application web avec PostgreSQL..." -ForegroundColor Cyan
dotnet run --project .\E-learningProject.Web\E-learningProject.Web.csproj --no-launch-profile
