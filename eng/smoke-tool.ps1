param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$artifacts = Join-Path $repoRoot ".artifacts"
$packageDir = Join-Path $artifacts "packages"
$toolPath = Join-Path $artifacts "tool-smoke"
$repoPath = Join-Path $artifacts "tool-smoke-repo"
$storagePath = Join-Path $artifacts "tool-smoke-storage"
$nugetConfigPath = Join-Path $artifacts "tool-smoke.nuget.config"

Remove-Item -Recurse -Force $packageDir, $toolPath, $repoPath, $storagePath -ErrorAction SilentlyContinue
Remove-Item -Force $nugetConfigPath -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $packageDir, $toolPath, $repoPath, $storagePath | Out-Null

@"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <AssemblyName>Smoke.Project</AssemblyName>
  </PropertyGroup>
</Project>
"@ | Set-Content -Encoding UTF8 (Join-Path $repoPath "Smoke.Project.csproj")

New-Item -ItemType Directory -Force (Join-Path $repoPath "src") | Out-Null
@"
namespace Smoke.Project;

public class Worker
{
    public void Run()
    {
        Helper();
    }

    private void Helper()
    {
    }
}
"@ | Set-Content -Encoding UTF8 (Join-Path $repoPath "src/Worker.cs")

dotnet pack (Join-Path $repoRoot "src/SharpMemory.Tool/SharpMemory.Tool.csproj") `
    --configuration $Configuration `
    --no-build `
    --output $packageDir

@"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$packageDir" />
  </packageSources>
</configuration>
"@ | Set-Content -Encoding UTF8 $nugetConfigPath

dotnet tool install sharp-memory `
    --tool-path $toolPath `
    --configfile $nugetConfigPath `
    --version 0.1.0

$port = Get-Random -Minimum 20000 -Maximum 50000
$baseAddress = "http://127.0.0.1:$port"
$toolExe = Join-Path $toolPath "sharp-memory"
if ($IsWindows) {
    $toolExe = "$toolExe.exe"
}

$outLog = Join-Path $storagePath "sharp-memory.out.log"
$errLog = Join-Path $storagePath "sharp-memory.err.log"
$env:ASPNETCORE_URLS = $baseAddress
$startArgs = @{
    FilePath = $toolExe
    ArgumentList = @("run", "--repo", $repoPath)
    WorkingDirectory = $storagePath
    RedirectStandardOutput = $outLog
    RedirectStandardError = $errLog
    PassThru = $true
}
if ($IsWindows) {
    $startArgs.WindowStyle = "Hidden"
}

$process = Start-Process @startArgs

try {
    $deadline = (Get-Date).AddSeconds(30)
    do {
        if ($process.HasExited) {
            throw "sharp-memory exited with code $($process.ExitCode).`n$(Get-Content $outLog -Raw)`n$(Get-Content $errLog -Raw)"
        }

        try {
            $health = Invoke-WebRequest -Uri "$baseAddress/health" -UseBasicParsing -TimeoutSec 2
            if ($health.StatusCode -eq 200) {
                break
            }
        }
        catch {
            Start-Sleep -Milliseconds 250
        }
    } while ((Get-Date) -lt $deadline)

    if ((Get-Date) -ge $deadline) {
        throw "Timed out waiting for sharp-memory health endpoint."
    }

    $repositories = @(Invoke-RestMethod -Uri "$baseAddress/api/memory/repositories" -TimeoutSec 10)
    if ($repositories.Count -ne 1) {
        throw "Expected exactly one repository, got $($repositories.Count)."
    }
}
finally {
    if (-not $process.HasExited) {
        Stop-Process -Id $process.Id -Force
        $process.WaitForExit()
    }

    Remove-Item Env:\ASPNETCORE_URLS -ErrorAction SilentlyContinue
}

$stdioStart = [System.Diagnostics.ProcessStartInfo]::new()
$stdioStart.FileName = $toolExe
$stdioStart.WorkingDirectory = $storagePath
$stdioStart.UseShellExecute = $false
$stdioStart.RedirectStandardInput = $true
$stdioStart.RedirectStandardOutput = $true
$stdioStart.RedirectStandardError = $true
$stdioStart.CreateNoWindow = $true
$escapedRepoPath = $repoPath.Replace('"', '\"')
$stdioStart.Arguments = "run --repo `"$escapedRepoPath`" --stdio"

$stdioProcess = [System.Diagnostics.Process]::Start($stdioStart)
try {
    Start-Sleep -Seconds 2
    $stdioProcess.Refresh()
    if ($stdioProcess.HasExited) {
        throw "sharp-memory --stdio exited with code $($stdioProcess.ExitCode)."
    }
}
finally {
    if (-not $stdioProcess.HasExited) {
        Stop-Process -Id $stdioProcess.Id -Force
        $stdioProcess.WaitForExit()
    }
}
