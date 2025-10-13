# Get the latest .NET SDK version from Microsoft download page
param(
    [Parameter(Mandatory=$true)]
    [string]$VersionPrefix
)

# Clean up version prefix by trimming trailing dots and x's
$cleanedPrefix = $VersionPrefix.TrimEnd('.', 'x', 'X')

# Parse version prefix to determine major.minor and optional feature band
if ($cleanedPrefix -match '^(\d+\.\d+)\.?(\d*)$') {
    $majorMinor = $matches[1]
    $featureBand = $matches[2]
} else {
    Write-Error "Invalid version prefix format. Use format like '8.0', '9.0', '8.0.1xxx', '8.0.1xx', etc."
    exit 1
}

$url = "https://dotnet.microsoft.com/en-us/download/dotnet/$majorMinor"

# Retry logic for web request with random delays
$maxRetries = 5
$attempt = 0
$response = $null

do {
    $attempt++
    try {
        $response = Invoke-WebRequest -Uri $url -UseBasicParsing
        break
    }
    catch {
        Write-Warning "Web request failed on attempt $attempt of $maxRetries`: $($_.Exception.Message)"
        
        if ($attempt -lt $maxRetries) {
            $delay = Get-Random -Minimum 5 -Maximum 11
            Write-Warning "Retrying in $delay seconds..."
            Start-Sleep -Seconds $delay
        }
        else {
            throw "All $maxRetries attempts failed. Last error: $($_.Exception.Message)"
        }
    }
} while ($attempt -lt $maxRetries)

$content = $response.Content

# Create more comprehensive regex pattern to capture full version strings including build numbers
if ($featureBand) {
    # Match specific feature band with full build numbers (e.g., 8.0.1xx-rc.1.25451.107)
    $pattern = "\b($majorMinor\.$featureBand\d{2}(?:-[a-zA-Z]+(?:\.\d+)*)?)\b"
} else {
    # Match any version in major.minor with full build numbers (e.g., 8.0.xxx-rc.1.25451.107)
    $pattern = "\b($majorMinor\.\d{3}(?:-[a-zA-Z]+(?:\.\d+)*)?)\b"
}

$regexMatches = [regex]::Matches($content, $pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)

$versions = @()
foreach ($match in $regexMatches) {
    $version = $match.Groups[1].Value
    # Validate it's a proper version format (allow full version with multiple build number segments)
    if ($version -match '^\d+\.\d+\.\d+(?:-[a-zA-Z]+(?:\.\d+)*)?$') {
        $versions += $version
    }
}

if ($versions.Count -eq 0) {
    Write-Error "No .NET SDK versions found matching pattern '$VersionPrefix'"
    exit 1
}

# Sort versions properly (stable versions come after pre-release)
$latestVersion = $versions | Sort-Object -Unique | Sort-Object {
    if ($_ -match '^(\d+)\.(\d+)\.(\d+)(?:-(.+))?$') {
        $major = [int]$matches[1]
        $minor = [int]$matches[2] 
        $build = [int]$matches[3]
        $prerelease = $matches[4]
        
        # Sort stable versions (no prerelease) last, then by prerelease type
        $prereleaseWeight = if (-not $prerelease) { 999 } 
                           elseif ($prerelease -like "rc*") { 100 }
                           else { 0 }  # preview versions
        
        return @($major, $minor, $build, $prereleaseWeight)
    }
    return @(0, 0, 0, 0)
} | Select-Object -Last 1

Write-Output $latestVersion