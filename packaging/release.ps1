<#
.SYNOPSIS
    Membuat paket rilis Bertahan dari Windows.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File packaging\release.ps1
    powershell -ExecutionPolicy Bypass -File packaging\release.ps1 -Rids win-x64 -Version 1.2.0

.NOTES
    Hasil di dist\:
      Bertahan-<v>-windows-x64.zip dan -setup.exe (installer, bila Inno Setup 6 terpasang)
      Bertahan-<v>-linux-x64.tar.gz
      Bertahan-<v>-macos-<arch>.tar.gz (Bertahan.app; .dmg hanya bisa dibuat di macOS)
      SHA256SUMS.txt
#>
param(
    [string]$Version = "",
    [string[]]$Rids = @("win-x64", "linux-x64", "osx-x64", "osx-arm64")
)

$ErrorActionPreference = "Stop"
# "-Rids a,b" through powershell -File arrives as one string
$Rids = $Rids | ForEach-Object { $_ -split "," } | Where-Object { $_ }
$Root = Split-Path -Parent $PSScriptRoot
$Project = Join-Path $Root "src\Bertahan\Bertahan.csproj"
$Pkg = Join-Path $Root "packaging"
$Dist = Join-Path $Root "dist"
$Stage = Join-Path $Dist "stage"
$Work = Join-Path $Dist "pkg"

if (-not $Version) {
    $Version = ([xml](Get-Content $Project)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
}
Write-Host "== Bertahan $Version : $($Rids -join ', ')"
New-Item -ItemType Directory -Force $Dist | Out-Null

function Reset-Dir([string]$Path) {
    if (Test-Path $Path) { Remove-Item -Recurse -Force $Path }
    New-Item -ItemType Directory -Force $Path | Out-Null
}

# Windows files carry no Unix permissions, so the archive is built from an mtree manifest
# (read by the bsdtar that ships with Windows) that marks the executables 0755.
function New-TarGz([string]$Parent, [string]$Name, [string]$Archive, [string[]]$Executables) {
    $lines = [Collections.Generic.List[string]]::new()
    $lines.Add("#mtree")
    $lines.Add("./$Name type=dir mode=0755 uname=root gname=root")
    Get-ChildItem -Recurse (Join-Path $Parent $Name) | ForEach-Object {
        $rel = $_.FullName.Substring($Parent.Length + 1).Replace("\", "/")
        $esc = ($rel -replace " ", "\040")
        if ($_.PSIsContainer) {
            $lines.Add("./$esc type=dir mode=0755 uname=root gname=root")
        }
        else {
            $mode = if ($Executables -contains $rel) { "0755" } else { "0644" }
            $src = ($_.FullName.Replace("\", "/") -replace " ", "\040")
            $lines.Add("./$esc type=file mode=$mode uname=root gname=root contents=$src")
        }
    }
    $manifest = Join-Path $Parent "manifest.mtree"
    [IO.File]::WriteAllLines($manifest, $lines, (New-Object Text.UTF8Encoding $false))
    & "$env:SystemRoot\System32\tar.exe" -czf $Archive -C $Parent "@$manifest"
    if ($LASTEXITCODE -ne 0) { throw "tar gagal: $Archive" }
    Remove-Item $manifest
}

function Find-Iscc {
    $cmd = Get-Command iscc -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    foreach ($p in @("${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "$env:ProgramFiles\Inno Setup 6\ISCC.exe", "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe")) {
        if (Test-Path $p) { return $p }
    }
    return $null
}

foreach ($rid in $Rids) {
    $out = Join-Path $Stage $rid
    Write-Host "== ${rid}: publish"
    if (Test-Path $out) { Remove-Item -Recurse -Force $out }
    dotnet publish $Project -c Release -r $rid --self-contained true "-p:Version=$Version" -p:DebugType=None -p:DebugSymbols=false -o $out
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish gagal untuk $rid" }

    Write-Host "== ${rid}: paket"
    Reset-Dir $Work
    switch -Wildcard ($rid) {
        "win-*" {
            $dir = Join-Path $Work "Bertahan"
            Copy-Item -Recurse $out $dir
            $zip = Join-Path $Dist "Bertahan-$Version-windows-x64.zip"
            if (Test-Path $zip) { Remove-Item $zip }
            # bsdtar, not Compress-Archive: PowerShell 5.1 writes backslashes into zip entry names
            & "$env:SystemRoot\System32\tar.exe" -a -cf $zip -C $Work "Bertahan"
            if ($LASTEXITCODE -ne 0) { throw "zip gagal" }
            $iscc = Find-Iscc
            if ($iscc) {
                & $iscc /Q "/DAppVersion=$Version" "/DSourceDir=$out" "/DOutputDir=$Dist" (Join-Path $Pkg "windows\bertahan.iss")
                if ($LASTEXITCODE -ne 0) { throw "Inno Setup gagal" }
            }
            else {
                Write-Host "   (Inno Setup 6 tidak ditemukan: installer .exe dilewati, hanya .zip. Pasang: winget install JRSoftware.InnoSetup)"
            }
        }
        "linux-*" {
            $name = "bertahan-$Version"
            $dir = Join-Path $Work $name
            New-Item -ItemType Directory -Force $dir | Out-Null
            Copy-Item -Recurse $out (Join-Path $dir "app")
            Copy-Item (Join-Path $Pkg "linux\install.sh"), (Join-Path $Pkg "linux\uninstall.sh"), (Join-Path $Pkg "linux\bertahan.desktop"), (Join-Path $Root "LICENSE") $dir
            Copy-Item (Join-Path $Pkg "icons\bertahan-256.png") (Join-Path $dir "bertahan.png")
            New-TarGz $Work $name (Join-Path $Dist "Bertahan-$Version-linux-x64.tar.gz") @("$name/install.sh", "$name/uninstall.sh", "$name/app/Bertahan")
        }
        "osx-*" {
            $arch = $rid.Substring(4)
            $app = Join-Path $Work "Bertahan.app"
            New-Item -ItemType Directory -Force (Join-Path $app "Contents\Resources") | Out-Null
            Copy-Item -Recurse $out (Join-Path $app "Contents\MacOS")
            $plist = (Get-Content (Join-Path $Pkg "macos\Info.plist") -Raw).Replace("@VERSION@", $Version)
            [IO.File]::WriteAllText((Join-Path $app "Contents\Info.plist"), $plist, (New-Object Text.UTF8Encoding $false))
            Copy-Item (Join-Path $Pkg "icons\bertahan.icns") (Join-Path $app "Contents\Resources")
            New-TarGz $Work "Bertahan.app" (Join-Path $Dist "Bertahan-$Version-macos-$arch.tar.gz") @("Bertahan.app/Contents/MacOS/Bertahan")
        }
        default { throw "RID tidak didukung: $rid" }
    }
}
if (Test-Path $Work) { Remove-Item -Recurse -Force $Work }

Get-ChildItem $Dist -File -Filter "Bertahan-*" | ForEach-Object {
    "{0}  {1}" -f (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLower(), $_.Name
} | Set-Content -Encoding ascii (Join-Path $Dist "SHA256SUMS.txt")

Write-Host "== Selesai:"
Get-ChildItem $Dist -File | Format-Table Name, @{ n = "MB"; e = { [math]::Round($_.Length / 1MB, 1) } } -AutoSize
