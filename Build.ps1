$logtimestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$logfilename = "$PSScriptRoot/Transcript_$logtimestamp.log"

Push-Location -Path "$PSScriptRoot/eng/src"
$exitCode = -1;

try {
    # We run the commands in the engineering directory, so the global.json from that directory is used.
    # Builds are then execured in the project/solution directory using the --project-dir-as-working-dir, so the possibly modified global.json in the repository root is used.
    (& dotnet nuget locals http-cache -c) | Out-Null
    & dotnet run --project "BuildMetalamaTestsDotNetSdk.csproj" -- $args --project-dir-as-working-dir > $logfilename 2>&1
    $exitCode = $LASTEXITCODE
} finally {
    Pop-Location
    [Console]::WriteLine([System.IO.File]::ReadAllText($logfilename))
}

exit $exitCode