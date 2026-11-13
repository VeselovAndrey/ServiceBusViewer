param(
    [Parameter(Mandatory = $true)]
    [string] $Bundle,

    [Parameter(Mandatory = $true)]
    [string] $Destination
)

$ErrorActionPreference = 'Stop'
$bundlePath = [System.IO.Path]::GetFullPath($Bundle)
$destinationPath = [System.IO.Path]::GetFullPath($Destination)

if (-not [System.IO.Directory]::Exists($bundlePath)) {
    throw "Windows bundle directory does not exist: $bundlePath"
}

if (Test-Path -LiteralPath $destinationPath -PathType Leaf) {
    Remove-Item -LiteralPath $destinationPath -Force
}

try {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory(
        $bundlePath,
        $destinationPath,
        [System.IO.Compression.CompressionLevel]::Optimal,
        $true
    )
}
catch {
    Remove-Item -LiteralPath $destinationPath -Force -ErrorAction SilentlyContinue
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}
