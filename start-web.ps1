Get-Process E-learningProject.Web -ErrorAction SilentlyContinue | Stop-Process -Force

$env:DOTNET_ROLL_FORWARD = 'Major'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = 'http://localhost:5230'

$hostName = 'localhost'
$port = '5432'
$database = 'MicroLmsDb'
$username = 'postgres'

if ([string]::IsNullOrWhiteSpace($env:MICROLMS_CONNECTION_STRING)) {
	# Lire depuis les variables d'environnement utilisateur/système (registre Windows)
	$dbPassword = [System.Environment]::GetEnvironmentVariable('DB_PASSWORD', 'User')
	if ([string]::IsNullOrWhiteSpace($dbPassword)) { $dbPassword = [System.Environment]::GetEnvironmentVariable('DB_PASSWORD', 'Machine') }
	if ([string]::IsNullOrWhiteSpace($dbPassword)) { $dbPassword = $env:DB_PASSWORD }
	if ([string]::IsNullOrWhiteSpace($dbPassword)) { $dbPassword = 'postgres' }

	$dbUser = [System.Environment]::GetEnvironmentVariable('DB_USERNAME', 'User')
	if ([string]::IsNullOrWhiteSpace($dbUser)) { $dbUser = [System.Environment]::GetEnvironmentVariable('DB_USERNAME', 'Machine') }
	if ([string]::IsNullOrWhiteSpace($dbUser)) { $dbUser = $env:DB_USERNAME }
	if ([string]::IsNullOrWhiteSpace($dbUser)) { $dbUser = $username }

	$env:MICROLMS_CONNECTION_STRING = "Host=$hostName;Port=$port;Database=$database;Username=$dbUser;Password=$dbPassword;Search Path=public,lms;Include Error Detail=true"
}

Write-Host "Demarrage de l'application web avec PostgreSQL..." -ForegroundColor Cyan
dotnet run --project .\E-learningProject.Web\E-learningProject.Web.csproj --no-launch-profile
