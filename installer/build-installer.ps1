<#
.SYNOPSIS
    DeskSeek 本地一键多架构打包与安装程序构建脚本
.DESCRIPTION
    1. 自动执行 dotnet publish 生成 Self-Contained 单文件独立版 (x64 / x86 / arm64)
    2. 自动打包 Portable 便携免安装 ZIP 压缩包
    3. 检查 Inno Setup 编译器（若无则提示一键通过 winget 安装）并构建 Setup.exe 安装包
#>

param(
    [string]$Version = "1.0.0",
    [ValidateSet("x64", "x86", "arm64", "all")]
    [string]$Arch = "x64",
    [switch]$SelfContained = $false
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$DistDir = Join-Path $ProjectRoot "dist"
$IssScript = Join-Path $ProjectRoot "installer\DeskSeek.iss"

$TargetArchs = if ($Arch -eq "all") { @("x64", "x86", "arm64") } else { @($Arch) }
$scStr = if ($SelfContained) { "true" } else { "false" }

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "   DeskSeek 打包程序构建工具 v$Version   " -ForegroundColor Cyan
Write-Host "   目标架构: $($TargetArchs -join ', ')  " -ForegroundColor Cyan
Write-Host "   自包含模式: $scStr                   " -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

# 1. 准备目标输出目录
if (-not (Test-Path $DistDir)) {
    New-Item -ItemType Directory -Path $DistDir -Force | Out-Null
}

# 2. 查找 Inno Setup 编译器 (ISCC.exe)
$IsccPath = $null
$CommonPaths = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    (Get-Command iscc.exe -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -ErrorAction SilentlyContinue)
)

foreach ($p in $CommonPaths) {
    if ($p -and (Test-Path $p)) {
        $IsccPath = $p
        break
    }
}

if (-not $IsccPath) {
    Write-Host "提示: 未检测到 Inno Setup 6 编译器。" -ForegroundColor Yellow
    Write-Host "如需在本地生成安装包 Setup.exe，可通过以下命令一键安装 Inno Setup:" -ForegroundColor White
    Write-Host "    winget install JRSoftware.InnoSetup" -ForegroundColor Cyan
    Write-Host "（GitHub Actions 自动化流水线已自动集成，无需本地安装即可由 GitHub 生成）`n" -ForegroundColor Gray
} else {
    Write-Host "找到 Inno Setup 编译器: $IsccPath`n" -ForegroundColor Gray
}

# 3. 循环构建指定架构
foreach ($target in $TargetArchs) {
    $rid = "win-$target"
    $publishDir = Join-Path $ProjectRoot "publish\$rid"

    Write-Host "-----------------------------------------" -ForegroundColor DarkCyan
    Write-Host ">>> 开始构建架构: $target (RID: $rid) ..." -ForegroundColor Cyan
    Write-Host "-----------------------------------------" -ForegroundColor DarkCyan

    # A. 编译发布单文件版
    Write-Host "[1/3] 正在执行 dotnet publish ($rid, self-contained: $scStr)..." -ForegroundColor Yellow
    $projectPath = Join-Path $ProjectRoot "DeskSeek.csproj"
    $dotnetArgs = @(
        "publish",
        $projectPath,
        "-c", "Release",
        "-r", $rid,
        "--self-contained", $scStr,
        "-p:PublishSingleFile=true",
        "-p:Version=$Version",
        "-o", $publishDir
    )
    dotnet @dotnetArgs

    if ($LASTEXITCODE -ne 0) {
        Write-Error "架构 $target 的 dotnet publish 编译失败！"
        exit 1
    }

    # 复制 Assets 资源到发布目录以确保 Portable 解压版包含图标
    $assetsDest = Join-Path $publishDir "Assets"
    New-Item -ItemType Directory -Path $assetsDest -Force | Out-Null
    Copy-Item -Path (Join-Path $ProjectRoot "Assets\*") -Destination $assetsDest -Recurse -Force

    # B. 打包便携版 ZIP (Portable)
    Write-Host "[2/3] 正在打包便携版 ZIP (Portable)..." -ForegroundColor Yellow
    $zipPath = Join-Path $DistDir "DeskSeek-v$Version-$rid-Portable.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    Compress-Archive -Path "$publishDir\*" -DestinationPath $zipPath -Force
    Write-Host "  -> 已生成便携包: $zipPath" -ForegroundColor Green

    # C. 构建 Inno Setup 安装包
    if ($IsccPath) {
        Write-Host "[3/3] 正在生成 Inno Setup 安装程序..." -ForegroundColor Yellow
        $isccArgs = @(
            "/DAppVersion=$Version",
            "/DAppArch=$target",
            "/DSourceDir=$publishDir",
            $IssScript
        )
        & $IsccPath @isccArgs
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  -> 已成功生成安装程序 ($target) 于 dist 目录！" -ForegroundColor Green
        }
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "架构 $target 的 Inno Setup 编译失败，请检查报错。"
        }
    }
    if (-not $IsccPath) {
        Write-Host "[3/3] 跳过安装程序构建 (未安装 Inno Setup)" -ForegroundColor Gray
    }
}

Write-Host "`n=========================================" -ForegroundColor Green
Write-Host "全部构建完成！输出文件请查看目录:" -ForegroundColor Green
Write-Host "  $DistDir" -ForegroundColor White
Get-ChildItem -Path $DistDir | Select-Object Name, Length | Format-Table -AutoSize
Write-Host "=========================================" -ForegroundColor Green
