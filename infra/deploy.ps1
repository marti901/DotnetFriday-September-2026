<#
.SYNOPSIS
	Deploys the MonkeyBook infrastructure and the application to Azure.

.DESCRIPTION
	1. Creates the resource group if it does not exist.
	2. Deploys infra/main.bicep.
	3. Publishes and zip deploys the three .NET services to their app services.
	4. Builds the Vue frontend, writes config.json with the api urls and uploads it to the static web app.

.EXAMPLE
	./infra/deploy.ps1 -ResourceGroupName rg-monkeybook-dev -PostgresAdminPassword (Read-Host -AsSecureString)
#>
[CmdletBinding()]
param(
	[Parameter(Mandatory)]
	[string] $ResourceGroupName,

	[Parameter(Mandatory)]
	[securestring] $PostgresAdminPassword,

	[string] $Location = 'westeurope',

	[string] $EnvironmentName = 'dev',

	[string] $NamePrefix = 'monkeybook',

	[switch] $InfraOnly
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$codeRoot = Join-Path $repoRoot 'code'
$artifacts = Join-Path $repoRoot 'artifacts'

$plainPassword = [System.Net.NetworkCredential]::new('', $PostgresAdminPassword).Password

Write-Host "Creating resource group $ResourceGroupName..." -ForegroundColor Cyan
az group create --name $ResourceGroupName --location $Location --output none

Write-Host 'Deploying infrastructure...' -ForegroundColor Cyan
$deployment = az deployment group create `
	--resource-group $ResourceGroupName `
	--name 'monkeybook-infra' `
	--template-file (Join-Path $PSScriptRoot 'main.bicep') `
	--parameters (Join-Path $PSScriptRoot 'main.parameters.json') `
	--parameters namePrefix=$NamePrefix environmentName=$EnvironmentName location=$Location postgresAdminPassword=$plainPassword `
	--output json | ConvertFrom-Json

$outputs = $deployment.properties.outputs

Write-Host "Frontend  : $($outputs.frontendUrl.value)" -ForegroundColor Green
Write-Host "Feed api  : $($outputs.feedApiUrl.value)" -ForegroundColor Green
Write-Host "Posts api : $($outputs.postsApiUrl.value)" -ForegroundColor Green

if ($InfraOnly) {
	return
}

# --- .NET services -----------------------------------------------------------

$services = @(
	@{ Project = 'MonkeyBook.PostsApi'; AppName = $outputs.postsApiName.value },
	@{ Project = 'MonkeyBook.FeedApi'; AppName = $outputs.feedApiName.value },
	@{ Project = 'MoneyBook.BackgroundProcessor'; AppName = $outputs.backgroundProcessorName.value }
)

if (Test-Path $artifacts) {
	Remove-Item $artifacts -Recurse -Force
}

New-Item -ItemType Directory -Path $artifacts | Out-Null

foreach ($service in $services) {
	$publishDir = Join-Path $artifacts $service.Project
	$zipPath = Join-Path $artifacts "$($service.Project).zip"

	Write-Host "Publishing $($service.Project)..." -ForegroundColor Cyan
	dotnet publish (Join-Path $codeRoot "$($service.Project)/$($service.Project).csproj") `
		--configuration Release `
		--output $publishDir `
		--nologo

	Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $zipPath -Force

	Write-Host "Deploying $($service.Project) to $($service.AppName)..." -ForegroundColor Cyan
	az webapp deploy `
		--resource-group $ResourceGroupName `
		--name $service.AppName `
		--src-path $zipPath `
		--type zip `
		--output none
}

# --- Frontend ----------------------------------------------------------------

$frontendDir = Join-Path $codeRoot 'MonkeyBook.Frontend'

Write-Host 'Building the frontend...' -ForegroundColor Cyan
Push-Location $frontendDir
try {
	npm ci
	npm run build
}
finally {
	Pop-Location
}

# The spa reads these at startup, so the same build works for every environment.
$config = @{
	feedApiUrl  = $outputs.feedApiUrl.value
	postsApiUrl = $outputs.postsApiUrl.value
} | ConvertTo-Json

Set-Content -Path (Join-Path $frontendDir 'dist/config.json') -Value $config -Encoding utf8

Write-Host 'Deploying the frontend...' -ForegroundColor Cyan
$swaToken = az staticwebapp secrets list `
	--name $outputs.staticWebAppName.value `
	--resource-group $ResourceGroupName `
	--query 'properties.apiKey' `
	--output tsv

npx --yes @azure/static-web-apps-cli deploy (Join-Path $frontendDir 'dist') `
	--deployment-token $swaToken `
	--env production

Write-Host "Done. Open $($outputs.frontendUrl.value)" -ForegroundColor Green
