(& dotnet nuget locals http-cache -c) | Out-Null

Push-Location -Path "$PSScriptRoot\eng\src"
$exitCode = -1;

try {
    # We run the command in the engineering directory, so the global.json from that directory is used.
    # Builds are then execured in the project/solution directory using the --project-dir-as-working-dir, so the possibly modified global.json in the repository root is used.
    & dotnet run --project "BuildMetalamaTestsDotNetSdk.csproj" -- $args --project-dir-as-working-dir
    $exitCode = $LASTEXITCODE
} finally {
    Pop-Location
}

exit $exitCode