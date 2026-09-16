<#
.SYNOPSIS
    DeskSeek 本地一键打包与安装程序构建脚本
.DESCRIPTION
    1. 自动执行 dotnet publish 生成 Self-Contained 单文件独立版
    2. 自动打包 Portable 便携免安装 ZIP 压缩包
    3. 检查 Inno Setup 编译器（若无则提示一键通过 winget 安装）并构建 Setup.exe 安装包
#>

param(
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$PublishDir = Join-Path $ProjectRoot "publish\self-contained"
$DistDir = Join-Path $ProjectRoot "dist"
$IssScript = Join-Path $ProjectRoot "installer\DeskSeek.iss"

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "   DeskSeek 打包程序构建工具 v$Version   " -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

# 1. 创建目标目录
if (Test-Path $DistDir) {
    Remove-Item $DistDir -Recurse -Force
}
New-Item -ItemType Directory -Path $DistDir -Force | Out-Null

# 2. 编译发布自包含单文件版
Write-Host "`n[1/3] 正在编译 Self-Contained 独立运行版本 (win-x64)..." -ForegroundColor Yellow
dotnet publish (Join-Path $ProjectRoot "DeskSeek.csproj") `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $PublishDir

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish 编译失败！"
    exit 1
}

# 3. 打包便携版 ZIP
Write-Host "`n[2/3] 正在打包便携版 ZIP (Portable)..." -ForegroundColor Yellow
$ZipPath = Join-Path $DistDir "DeskSeek-v$Version-Portable.zip"
Compress-Archive -Path "$PublishDir\*" -DestinationPath $ZipPath -Force
Write-Host "  -> 已生成便携包: $ZipPath" -ForegroundColor Green

# 4. 寻找 Inno Setup 并构建安装包
Write-Host "`n[3/3] 正在检查 Inno Setup 编译器 (ISCC.exe)..." -ForegroundColor Yellow

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
    Write-Host "（GitHub Actions 自动化流水线已自动集成，无需本地安装即可由 GitHub 生成）" -ForegroundColor Gray
} else {
    Write-Host "  找到编译器: $IsccPath" -ForegroundColor Gray
    Write-Host "  正在构建 Windows 安装包 Setup.exe..." -ForegroundColor Yellow
    & $IsccPath "/DAppVersion=$Version" $IssScript
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  -> 已成功生成安装程序于 dist 目录！" -ForegroundColor Green
    } else {
        Write-Warning "Inno Setup 编译失败，请检查报错信息。"
    }
}

Write-Host "`n=========================================" -ForegroundColor Green
Write-Host "构建完成！输出文件请查看目录:" -ForegroundColor Green
Write-Host "  $DistDir" -ForegroundColor White
Write-Host "=========================================" -ForegroundColor Green
