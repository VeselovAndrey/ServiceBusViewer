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

$archiveStream = $null
$archive = $null
$failure = $null

try {
    Add-Type -AssemblyName System.IO.Compression
    $bundleRoot = [System.IO.Path]::GetFileName($bundlePath.TrimEnd([char[]]@('\', '/')))
    $archiveStream = [System.IO.File]::Open(
        $destinationPath,
        [System.IO.FileMode]::Create,
        [System.IO.FileAccess]::Write,
        [System.IO.FileShare]::None
    )
    $archive = [System.IO.Compression.ZipArchive]::new(
        $archiveStream,
        [System.IO.Compression.ZipArchiveMode]::Create,
        $false
    )

    foreach ($item in Get-ChildItem -LiteralPath $bundlePath -Force -Recurse) {
        $relativePath = $item.FullName.Substring($bundlePath.Length).TrimStart([char[]]@('\', '/'))
        $entryName = "$bundleRoot/$($relativePath.Replace('\', '/'))"

        if ($item.PSIsContainer) {
            $null = $archive.CreateEntry("$entryName/")
            continue
        }

        $zipEntry = $archive.CreateEntry($entryName, [System.IO.Compression.CompressionLevel]::Optimal)
        $inputStream = [System.IO.File]::OpenRead($item.FullName)
        $entryStream = $null
        try {
            $entryStream = $zipEntry.Open()
            $inputStream.CopyTo($entryStream)
        }
        finally {
            if ($null -ne $entryStream) {
                $entryStream.Dispose()
            }
            $inputStream.Dispose()
        }
    }
}
catch {
    $failure = $_.Exception.Message
}
finally {
    if ($null -ne $archive) {
        $archive.Dispose()
    }
    elseif ($null -ne $archiveStream) {
        $archiveStream.Dispose()
    }
}

if ($null -ne $failure) {
    Remove-Item -LiteralPath $destinationPath -Force -ErrorAction SilentlyContinue
    [Console]::Error.WriteLine($failure)
    exit 1
}
