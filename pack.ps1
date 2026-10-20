$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$runtime = Join-Path $root "runtime"
$out = Join-Path $root "payload.bin"

if (-not (Test-Path -LiteralPath $runtime)) {
    throw "No existe runtime\: $runtime"
}

$files = New-Object System.Collections.Generic.List[object]

function Add-Rel([string]$rel) {
    $full = Join-Path $runtime $rel
    if (Test-Path -LiteralPath $full) {
        $files.Add(@{ Rel = $rel.Replace("\", "/"); Full = $full })
    }
}

Add-Rel "leechcore.dll"
Add-Rel "FTD3XX.dll"
Add-Rel "FTD3XXWU.dll"
Add-Rel "vcruntime140.dll"
Add-Rel "openFPGALoader\openFPGALoader.exe"
Add-Rel "openFPGALoader\libftdi1.dll"
Add-Rel "openFPGALoader\libgcc_s_seh-1.dll"
Add-Rel "openFPGALoader\libiconv-2.dll"
Add-Rel "openFPGALoader\libstdc++-6.dll"
Add-Rel "openFPGALoader\libusb-1.0.dll"
Add-Rel "openFPGALoader\libwinpthread-1.dll"
Add-Rel "openFPGALoader\zlib1.dll"
Add-Rel "openFPGALoader\LICENSE.openFPGALoader"
Add-Rel "openFPGALoader\spiOverJtag_xc7a35t.bit.gz"
Add-Rel "openFPGALoader\spiOverJtag_xc7a75t.bit.gz"
Add-Rel "openFPGALoader\spiOverJtag_xc7a100t.bit.gz"
Add-Rel "openFPGALoader\share\openFPGALoader\spiOverJtag_xc7a35t.bit.gz"
Add-Rel "openFPGALoader\share\openFPGALoader\spiOverJtag_xc7a75t.bit.gz"
Add-Rel "openFPGALoader\share\openFPGALoader\spiOverJtag_xc7a100t.bit.gz"
Add-Rel "drivers\rs232\zadig-2.9.exe"
Add-Rel "drivers\wch\CH343SER.INF"
Add-Rel "drivers\wch\CH343SER.CAT"
Add-Rel "drivers\wch\CH343SER.SYS"
Add-Rel "drivers\wch\CH343S64.SYS"
Add-Rel "drivers\wch\CH343M64.SYS"
Add-Rel "drivers\wch\CH343PT.DLL"
Add-Rel "drivers\wch\CH343PTA64.DLL"
Add-Rel "drivers\wch\CH343PORTS.dll"
Add-Rel "drivers\wch\CH343PORTSA64.dll"
Add-Rel "drivers\wch\SETUP.EXE"
Add-Rel "drivers\ftdi\ftdibus3.Inf"
Add-Rel "drivers\ftdi\ftdibus3.cat"
Add-Rel "drivers\ftdi\ftdibus3.Sys"

if ($files.Count -lt 10) {
    throw "Payload demasiado pequeno: $($files.Count) archivos"
}

$fs = [System.IO.File]::Create($out)
try {
    $bw = New-Object System.IO.BinaryWriter($fs, [System.Text.Encoding]::UTF8)
    $bw.Write([byte[]](0x46, 0x43, 0x30, 0x31))
    $bw.Write([int32]2)
    $bw.Write([int32]$files.Count)
    foreach ($f in $files) {
        $nameBytes = [System.Text.Encoding]::UTF8.GetBytes($f.Rel)
        $data = [System.IO.File]::ReadAllBytes($f.Full)
        $bw.Write([int32]$nameBytes.Length)
        $bw.Write($nameBytes)
        $bw.Write([int32]$data.Length)
        $bw.Write($data)
    }
    $bw.Flush()
}
finally {
    $fs.Close()
}

Write-Host ("payload.bin: {0} archivos, {1} bytes" -f $files.Count, (Get-Item -LiteralPath $out).Length)
