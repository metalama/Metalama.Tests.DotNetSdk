# Get the latest .NET SDK version from GitHub API
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

# GitHub API URL for .NET SDK releases
$url = "https://api.github.com/repos/dotnet/sdk/releases"

# Fetch .NET SDK releases from GitHub API with retry logic
Write-Host "::notice::Fetching .NET SDK releases from GitHub API..."

# Retry logic for GitHub API request with exponential backoff
$maxRetries = 5
$baseDelay = 1
$maxDelay = 30
# Calculate exponential factor for shorter delays with GitHub API
$exponentialFactor = [Math]::Exp([Math]::Log($maxDelay / $baseDelay) / ($maxRetries - 1))
$attempt = 0
$releases = $null

do {
    $attempt++
    try {
        # Add User-Agent header for GitHub API (required)
        $headers = @{
            'User-Agent' = 'PowerShell-Script'
            'Accept' = 'application/vnd.github.v3+json'
        }
        
        # Add Authorization header if GITHUB_TOKEN environment variable is available
        if ($env:GITHUB_TOKEN) {
            $headers['Authorization'] = "Bearer $env:GITHUB_TOKEN"
            Write-Host "::notice::Using GITHUB_TOKEN for authentication"
        } else {
            Write-Host "::notice::No GITHUB_TOKEN found, using unauthenticated requests (subject to rate limits)"
        }
        
        $releases = Invoke-RestMethod -Uri $url -Headers $headers -UseBasicParsing
        break
    }
    catch {
        Write-Warning "GitHub API request failed on attempt $attempt of $maxRetries`: $($_.Exception.Message)"
        
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

# Filter releases based on version prefix
$matchingVersions = @()

foreach ($release in $releases) {
    $releaseVersion = $release.tag_name -replace '^v', ''  # Remove 'v' prefix if present
    
    # Check if this release matches our version prefix criteria
    if ($featureBand) {
        # Match specific feature band (e.g., 8.0.1xx matches 8.0.1xx series)
        # Extract the first digit of feature band for matching (1xx -> 1, 4xx -> 4)
        $featureBandPrefix = $featureBand.Substring(0, 1)
        if ($releaseVersion -match "^$majorMinor\.$featureBandPrefix\d{2}") {
            $matchingVersions += @{
                Version = $releaseVersion
                IsPrerelease = $release.prerelease
                PublishedAt = $release.published_at
            }
        }
    } else {
        # Match any version in major.minor (e.g., 8.0 matches 8.0.100, 8.0.200, etc.)
        if ($releaseVersion -match "^$majorMinor\.") {
            $matchingVersions += @{
                Version = $releaseVersion
                IsPrerelease = $release.prerelease
                PublishedAt = $release.published_at
            }
        }
    }
}

if ($matchingVersions.Count -eq 0) {
    Write-Error "No .NET SDK versions found matching pattern '$VersionPrefix' in GitHub releases"
    exit 1
}

Write-Host "::notice::Found $($matchingVersions.Count) matching .NET SDK releases"

# Sort versions to get the latest (stable versions preferred over prereleases)
$latestVersion = $matchingVersions | Sort-Object -Property @(
    @{ Expression = { $_.IsPrerelease }; Ascending = $true },  # Stable (false) comes before prerelease (true)
    @{ Expression = { 
        # Parse version for proper semantic sorting
        if ($_.Version -match '^(\d+)\.(\d+)\.(\d+)(?:-(.+))?$') {
            $major = [int]$matches[1]
            $minor = [int]$matches[2] 
            $build = [int]$matches[3]
            return [Version]"$major.$minor.$build"
        }
        return [Version]"0.0.0"
    }; Ascending = $false },  # Highest version first
    @{ Expression = { [DateTime]$_.PublishedAt }; Ascending = $false }  # Most recent first
) | Select-Object -First 1

Write-Output $latestVersion.Version