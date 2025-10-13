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

# GitHub Actions cache configuration
$cacheDir = ".dotnet-cache"
$cacheFile = Join-Path $cacheDir "dotnet-$($majorMinor -replace '\.', '-').html"
$cacheLifetimeHours = 2

# Create cache directory if it doesn't exist
if (-not (Test-Path $cacheDir)) {
    New-Item -ItemType Directory -Path $cacheDir -Force | Out-Null
}

$content = $null
$useCache = $false

# Check if cache file exists and is still valid (within 2 hours)
if (Test-Path $cacheFile) {
    $cacheAge = (Get-Date) - (Get-Item $cacheFile).LastWriteTime
    
    if ($cacheAge.TotalHours -lt $cacheLifetimeHours) {
        Write-Host "::notice::Using cached .NET $majorMinor download page (cached $([math]::Round($cacheAge.TotalMinutes, 1)) minutes ago)"
        $content = Get-Content $cacheFile -Raw -Encoding UTF8
        $useCache = $true
    } else {
        Write-Host "::notice::Cache expired for .NET $majorMinor (age: $([math]::Round($cacheAge.TotalHours, 1)) hours)"
    }
}

# If no valid cache, fetch from web with retry logic and cache the result
if (-not $useCache) {
    Write-Host "::notice::Fetching .NET $majorMinor download page from Microsoft..."
    
    # Retry logic for web request with exponential backoff
    $maxRetries = 10
    $baseDelay = 2
    $maxDelay = 300
    # Calculate exponential factor: factor = exp(ln(maxDelay/baseDelay) / (maxRetries-1))
    # For 300 = 2 × factor^9: factor = exp(ln(150) / 9)
    $exponentialFactor = [Math]::Exp([Math]::Log($maxDelay / $baseDelay) / ($maxRetries - 1))
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
                # Calculate exponential backoff delay, capped at maxDelay
                $calculatedDelay = $baseDelay * [Math]::Pow($exponentialFactor, $attempt - 1)
                $delay = [Math]::Min($calculatedDelay, $maxDelay)
                $delay = [Math]::Round($delay, 1)
                
                Write-Warning "Retrying in $delay seconds..."
                Start-Sleep -Seconds $delay
            }
            else {
                throw "All $maxRetries attempts failed. Last error: $($_.Exception.Message)"
            }
        }
    } while ($attempt -lt $maxRetries)

    $content = $response.Content
    
    # Save content to cache
    $content | Set-Content $cacheFile -Encoding UTF8
    Write-Host "::notice::Cached .NET $majorMinor download page (expires in $cacheLifetimeHours hours)"
}

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