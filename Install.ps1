[System.Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "Stop"

# 检查管理员权限
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
$isAdmin = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator"
    Write-Host "Please right-click PowerShell and select 'Run as Administrator', then run this script again."
    exit 1
}

function Write-ColorOutput($ForegroundColor) {
    $fc = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $ForegroundColor
    if ($args) {
        Write-Output $args
    }
    $host.UI.RawUI.ForegroundColor = $fc
}

function Show-Status($message) {
    Write-ColorOutput Green "==> $message"
}

function Show-Error($message) {
    Write-ColorOutput Red "ERROR: $message"
    exit 1
}

# Check if running as Administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Show-Error "This script must be run as Administrator"
}

Show-Status "Checking required tools..."
try {
    dotnet --version | Out-Null
    Show-Status "√ .NET SDK installed"
} catch {
    Show-Error ".NET SDK not installed, please install .NET 9.0 SDK first"
}

# Install required tools
Show-Status "Installing required tools..."
dotnet tool install --global dotnet-ef
dotnet add ZhihuClone.API package Microsoft.EntityFrameworkCore.Design

# Create bundleconfig.json if not exists
if (!(Test-Path "ZhihuClone.Web\bundleconfig.json")) {
    @'
{
  "minify": {
    "enabled": true
  },
  "sourceMap": false
}
'@ | Set-Content "ZhihuClone.Web\bundleconfig.json"
}

$env:ASPNETCORE_ENVIRONMENT = "Production"

# Ensure IIS is installed
Show-Status "Checking IIS installation..."
try {
    $iisInstalled = (Get-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole).State -eq "Enabled"
    if (-not $iisInstalled) {
        Show-Status "Installing IIS..."
        Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole, IIS-WebServer, IIS-CommonHttpFeatures, IIS-ManagementConsole, IIS-HttpErrors, IIS-HttpRedirect, IIS-WindowsAuthentication, IIS-StaticContent, IIS-DefaultDocument, IIS-HttpCompressionStatic, IIS-DirectoryBrowsing, IIS-ApplicationInit, IIS-ASPNET45, IIS-ISAPIExtensions, IIS-ISAPIFilter, IIS-WebSockets -All
    }
    Import-Module WebAdministration -ErrorAction Stop
} catch {
    Show-Error "Failed to configure IIS. Please ensure IIS is installed properly: $_"
}

# Check ASP.NET Core Hosting Bundle
Show-Status "Checking ASP.NET Core Hosting Bundle..."
$aspNetCoreModulePath = "HKLM:\SOFTWARE\Microsoft\IIS Extensions\IIS AspNetCore Module V2"
if (-not (Test-Path $aspNetCoreModulePath)) {
    Show-Status "Installing ASP.NET Core Hosting Bundle..."
    
    # 定义多个下载链接
    $hostingBundleUrls = @(
        "https://download.visualstudio.microsoft.com/download/pr/85c42055-f67d-4f4f-a2aa-1a865ddf2946/52a726a89592f9e61a57c488c23f6949/dotnet-hosting-9.0.1-win.exe",
        "https://aka.ms/dotnet/9.0/dotnet-hosting-win.exe",
        "https://aka.ms/aspnetcore-9.0-windows-hosting-bundle-installer"
    )
    
    $downloadSuccess = $false
    $hostingBundlePath = "$env:TEMP\dotnet-hosting-9.0.1-win.exe"
    
    foreach ($url in $hostingBundleUrls) {
        try {
            Show-Status "Trying to download from: $url"
            Invoke-WebRequest -Uri $url -OutFile $hostingBundlePath -UseBasicParsing -ErrorAction Stop
            $downloadSuccess = $true
            break
        }
        catch {
            Show-Status "Failed to download from $url, trying next mirror..."
            Start-Sleep -Seconds 2
        }
    }
    
    if (-not $downloadSuccess) {
        Show-Status "Unable to download ASP.NET Core Hosting Bundle automatically."
        Show-Status "Please download it manually from: https://dotnet.microsoft.com/download/dotnet/9.0"
        Show-Status "Install it, then press any key to continue..."
        $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    }
    else {
        try {
            Show-Status "Installing ASP.NET Core Hosting Bundle..."
            $process = Start-Process -FilePath $hostingBundlePath -ArgumentList "/install", "/quiet", "/norestart" -Wait -PassThru
            if ($process.ExitCode -ne 0) {
                throw "Installation failed with exit code: $($process.ExitCode)"
            }
            Remove-Item $hostingBundlePath -ErrorAction SilentlyContinue
        }
        catch {
            Show-Error "Failed to install ASP.NET Core Hosting Bundle: $_"
        }
    }
    
    # 重启 IIS 以应用更改
    Show-Status "Restarting IIS..."
    try {
        iisreset /restart
    }
    catch {
        Show-Status "Failed to restart IIS automatically. Please restart IIS manually."
        Show-Status "Press any key to continue..."
        $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    }
}

Show-Status "Stopping IIS websites..."
try {
    if (Test-Path "IIS:\Sites\ZhihuClone.API") {
        Stop-Website -Name "ZhihuClone.API"
    }
    if (Test-Path "IIS:\Sites\ZhihuClone.Web") {
        Stop-Website -Name "ZhihuClone.Web"
    }
} catch {
    Show-Status "IIS websites do not exist, will create new ones"
}

Show-Status "Cleaning publish directory..."
$publishPath = ".\publish"
if (Test-Path $publishPath) {
    Remove-Item -Path $publishPath -Recurse -Force
}
New-Item -ItemType Directory -Path $publishPath | Out-Null

Show-Status "Restoring NuGet packages..."
dotnet restore ZhihuClone.sln

Show-Status "Building solution..."
dotnet build ZhihuClone.sln -c Release --no-restore

Show-Status "Running database migrations..."
try {
    dotnet ef database update --project ZhihuClone.Infrastructure --startup-project ZhihuClone.API
} catch {
    Show-Error "Database migration failed: $_"
}

Show-Status "Publishing API project..."
dotnet publish ZhihuClone.API -c Release -o "$publishPath\api" --no-restore

# 创建并设置日志目录权限
$apiLogPath = "$publishPath\api\logs"
if (!(Test-Path $apiLogPath)) {
    New-Item -ItemType Directory -Force -Path $apiLogPath | Out-Null
    Write-Host "Created logs directory at: $apiLogPath"
}

# 确保 IIS 应用程序池用户有写入权限
$acl = Get-Acl $apiLogPath
$identity = "IIS AppPool\ZhihuCloneAPI"
$fileSystemRights = "FullControl"
$type = "Allow"
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule($identity, $fileSystemRights, "ContainerInherit,ObjectInherit", "None", $type)
try {
    $acl.SetAccessRule($rule)
    Set-Acl $apiLogPath $acl
    Write-Host "Set permissions for $identity on $apiLogPath"
} catch {
    Write-Host "Warning: Could not set permissions. Error: $_"
    Write-Host "Trying alternative approach with NETWORK SERVICE..."
    $networkServiceRule = New-Object System.Security.AccessControl.FileSystemAccessRule("NETWORK SERVICE", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
    $acl.SetAccessRule($networkServiceRule)
    Set-Acl $apiLogPath $acl
}

# 创建一个测试日志文件
$testLogPath = Join-Path $apiLogPath "startup.log"
try {
    "$(Get-Date) - Deployment script created this test log file" | Out-File -FilePath $testLogPath -Append
    Write-Host "Created test log file at: $testLogPath"
} catch {
    Write-Host "Warning: Could not create test log file. Error: $_"
}

Show-Status "Publishing Web project..."
dotnet publish ZhihuClone.Web -c Release -o "$publishPath\web" --no-restore

# 配置 IIS
Write-Host "==> Configuring IIS..."

# 确保 IIS 功能已安装
$iisFeatures = @(
    "IIS-WebServerRole",
    "IIS-WebServer",
    "IIS-CommonHttpFeatures",
    "IIS-DefaultDocument",
    "IIS-DirectoryBrowsing",
    "IIS-HttpErrors",
    "IIS-StaticContent",
    "IIS-HttpRedirect",
    "IIS-HealthAndDiagnostics",
    "IIS-HttpLogging",
    "IIS-LoggingLibraries",
    "IIS-RequestMonitor",
    "IIS-HttpTracing",
    "IIS-Performance",
    "IIS-HttpCompressionStatic",
    "IIS-HttpCompressionDynamic",
    "IIS-Security",
    "IIS-BasicAuthentication",
    "IIS-WindowsAuthentication",
    "IIS-ApplicationDevelopment",
    "NetFx4Extended-ASPNET45",
    "IIS-NetFxExtensibility45",
    "IIS-ISAPIExtensions",
    "IIS-ISAPIFilter",
    "IIS-WebServerManagementTools",
    "IIS-ManagementConsole",
    "IIS-ApplicationInit"
)

foreach ($feature in $iisFeatures) {
    try {
        $featureState = Get-WindowsOptionalFeature -Online -FeatureName $feature -ErrorAction Stop
        if ($featureState.State -ne "Enabled") {
            Write-Host "Installing IIS feature: $feature"
            Enable-WindowsOptionalFeature -Online -FeatureName $feature -All -NoRestart
        }
    }
    catch {
        Write-Host "Warning: Failed to check/install feature $feature"
        Write-Host $_.Exception.Message
        continue
    }
}

# 检查 ASP.NET Core Hosting Bundle 是否已安装
$hostingBundle = Get-ItemProperty HKLM:\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\* | 
    Where-Object { $_.DisplayName -like "*ASP.NET Core*Hosting Bundle*" }
if (-not $hostingBundle) {
    Write-Host "Installing ASP.NET Core Hosting Bundle..."
    $hostingBundleUrl = "https://download.visualstudio.microsoft.com/download/pr/c5e0609f-1db5-4741-add0-a37e8371a714/1ad9c59b8a92aeb5d09782e686264537/dotnet-hosting-6.0.8-win.exe"
    $hostingBundleInstaller = ".\dotnet-hosting-bundle.exe"
    Invoke-WebRequest -Uri $hostingBundleUrl -OutFile $hostingBundleInstaller
    Start-Process -FilePath $hostingBundleInstaller -ArgumentList "/quiet" -Wait
    Remove-Item $hostingBundleInstaller
}

# 创建应用程序池
Import-Module WebAdministration
$apiAppPoolName = "ZhihuCloneAPI"
$webAppPoolName = "ZhihuCloneWeb"

if (!(Test-Path IIS:\AppPools\$apiAppPoolName)) {
    Write-Host "Creating API application pool..."
    New-WebAppPool -Name $apiAppPoolName
    Set-ItemProperty IIS:\AppPools\$apiAppPoolName -name "managedRuntimeVersion" -value ""
    Set-ItemProperty IIS:\AppPools\$apiAppPoolName -name "startMode" -value "AlwaysRunning"
    Set-ItemProperty IIS:\AppPools\$apiAppPoolName -name "processModel.idleTimeout" -value "00:00:00"
}

if (!(Test-Path IIS:\AppPools\$webAppPoolName)) {
    Write-Host "Creating Web application pool..."
    New-WebAppPool -Name $webAppPoolName
    Set-ItemProperty IIS:\AppPools\$webAppPoolName -name "managedRuntimeVersion" -value ""
    Set-ItemProperty IIS:\AppPools\$webAppPoolName -name "startMode" -value "AlwaysRunning"
    Set-ItemProperty IIS:\AppPools\$webAppPoolName -name "processModel.idleTimeout" -value "00:00:00"
}

# 创建网站
$apiSiteName = "ZhihuCloneAPI"
$webSiteName = "ZhihuCloneWeb"
$apiPhysicalPath = "$(Get-Location)\publish\api"
$webPhysicalPath = "$(Get-Location)\publish\web"

# 创建日志目录并设置权限
$apiLogPath = "$apiPhysicalPath\logs"
$webLogPath = "$webPhysicalPath\logs"

New-Item -ItemType Directory -Force -Path $apiLogPath | Out-Null
New-Item -ItemType Directory -Force -Path $webLogPath | Out-Null

# 设置日志目录权限
$acl = Get-Acl $apiLogPath
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS AppPool\$apiAppPoolName", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.SetAccessRule($rule)
Set-Acl $apiLogPath $acl

$acl = Get-Acl $webLogPath
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS AppPool\$webAppPoolName", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.SetAccessRule($rule)
Set-Acl $webLogPath $acl

# 设置应用程序目录权限
$acl = Get-Acl $apiPhysicalPath
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS AppPool\$apiAppPoolName", "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.SetAccessRule($rule)
Set-Acl $apiPhysicalPath $acl

$acl = Get-Acl $webPhysicalPath
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS AppPool\$webAppPoolName", "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.SetAccessRule($rule)
Set-Acl $webPhysicalPath $acl

if (!(Test-Path IIS:\Sites\$apiSiteName)) {
    Write-Host "Creating API website..."
    New-Website -Name $apiSiteName -PhysicalPath $apiPhysicalPath -Port 5237 -ApplicationPool $apiAppPoolName -Force
}

if (!(Test-Path IIS:\Sites\$webSiteName)) {
    Write-Host "Creating Web website..."
    New-Website -Name $webSiteName -PhysicalPath $webPhysicalPath -Port 5110 -ApplicationPool $webAppPoolName -Force
}

# 确保网站绑定正确
Set-WebBinding -Name $apiSiteName -BindingInformation "*:5237:" -PropertyName "Port" -Value "5237"
Set-WebBinding -Name $webSiteName -BindingInformation "*:5110:" -PropertyName "Port" -Value "5110"