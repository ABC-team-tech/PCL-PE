<#
.SYNOPSIS
    从 Release 构建产物生成可直接上传到 GitHub Release 的发布包。

.DESCRIPTION
    依次完成：清理旧产物 → Release 构建整个解决方案 → 输出发布文件到 release\。
    运行前请确保启动器已完全退出，否则 bin\ 下的 DLL 被占用会导致构建失败（MSB3027）。

.PARAMETER SkipBuild
    跳过编译，直接使用现有的 bin 产物打包。

.PARAMETER OutputDir
    发布输出目录，默认为仓库根目录下的 release\。
#>
[CmdletBinding()]
param(
    [switch]$SkipBuild,
    [string]$OutputDir
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
if (-not $OutputDir) { $OutputDir = Join-Path $root 'release' }

# ---- 定位 MSBuild：优先使用 Visual Studio 实例，回退到已知默认路径 ----
$msbuildCandidates = @()
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (Test-Path $vswhere) {
    $instances = & $vswhere -products * -requires Microsoft.Component.MSBuild -property installationPath
    foreach ($inst in $instances) {
        $msbuildCandidates += Join-Path $inst 'MSBuild\Current\Bin\amd64\MSBuild.exe'
    }
}
$msbuildCandidates += Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\amd64\MSBuild.exe'
$msbuild = $msbuildCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $msbuild) { throw '未找到 MSBuild.exe，请先安装 Visual Studio 2022（含 .NET 桌面开发工作负载）。' }

# ---- 读取版本号，与程序内检查更新使用的版本保持一致 ----
$modBase = Join-Path $root 'PCLPro\Modules\Base\ModBase.vb'
$versionMatch = Select-String -Path $modBase -Pattern 'VersionBaseName\s+As\s+String\s*=\s*"([^"]+)"' | Select-Object -First 1
if (-not $versionMatch) { throw '无法从 ModBase.vb 读取 VersionBaseName。' }
$version = $versionMatch.Matches[0].Groups[1].Value
$baseName = "PCL-Professional-Edition-$version"
Write-Host "版本: $version" -ForegroundColor Cyan

# ---- 检查启动器是否仍在运行（会锁定 bin 下的 DLL）----
$running = Get-Process | Where-Object {
    $_.ProcessName -eq 'PCL Professional Edition' -or $_.ProcessName -like 'PCL PE*'
}
if ($running) {
    Write-Warning "检测到启动器仍在运行（PID: $($running.Id -join ', ')），请先关闭后再构建。"
    if (-not $SkipBuild) { throw '启动器未关闭，构建会因文件被占用而失败。' }
}

# ---- 构建 ----
$exePath = Join-Path $root 'PCLPro\bin\PCL Professional Edition.exe'
if (-not $SkipBuild) {
    $cleanDirs = @(
        'PCLPro\obj', 'PCLPro\bin',
        'PCLCS\bin', 'PCLCS\obj',
        'MeloongCore\Shared\bin', 'MeloongCore\Shared\obj',
        'MeloongCore\Wpf\bin', 'MeloongCore\Wpf\obj'
    ) | ForEach-Object { Join-Path $root $_ }
    foreach ($d in $cleanDirs) {
        if (Test-Path $d) { Remove-Item -LiteralPath $d -Recurse -Force }
    }

    Write-Host "构建 Release…" -ForegroundColor Cyan
    $log = Join-Path $env:TEMP "pcl-release-$(Get-Date -Format yyyyMMddHHmmss).log"
    & $msbuild (Join-Path $root 'PCLPro.sln') -restore -p:Configuration=Release -nologo -m |
        Tee-Object -FilePath $log | Out-Null
    $errors = Select-String -Path $log -Pattern ':\s*error\s+[A-Z]+\d+' | Select-Object -First 5
    if ($errors) {
        $errors | ForEach-Object { Write-Host $_.Line.Trim() -ForegroundColor Red }
        throw "构建失败，完整日志: $log"
    }
}

if (-not (Test-Path $exePath)) { throw "未找到构建产物: $exePath" }

# ---- 组装发布包 ----
if (-not (Test-Path $OutputDir)) { New-Item -ItemType Directory -Path $OutputDir | Out-Null }
Get-ChildItem $OutputDir -File | Remove-Item -Force

$releaseExe = Join-Path $OutputDir "$baseName.exe"
Copy-Item -LiteralPath $exePath -Destination $releaseExe

$releaseZip = Join-Path $OutputDir "$baseName.zip"
Compress-Archive -LiteralPath $releaseExe -DestinationPath $releaseZip -CompressionLevel Optimal

$sums = @()
foreach ($f in @($releaseExe, $releaseZip)) {
    $hash = (Get-FileHash -LiteralPath $f -Algorithm SHA256).Hash
    $sums += "$hash  $(Split-Path $f -Leaf)"
}
$sums | Set-Content -Path (Join-Path $OutputDir 'SHA256SUMS.txt') -Encoding ASCII

# ---- 输出结果 ----
Write-Host "`n发布包已生成:" -ForegroundColor Green
Get-ChildItem $OutputDir -File | ForEach-Object {
    "{0,-45} {1,8:N2} MB" -f $_.Name, ($_.Length / 1MB)
}
Write-Host "`n上传到: https://github.com/ABC-team-tech/PCL-PE-Unofficial/releases/new" -ForegroundColor Cyan
Write-Host "Tag 建议: v$version" -ForegroundColor Cyan
Write-Host "附件请上传 .exe（文件名不要包含 setup / install，否则会被判定为安装器）" -ForegroundColor Yellow
