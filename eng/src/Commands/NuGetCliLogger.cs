// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using NuGet.Common;
using PostSharp.Engineering.BuildTools.Utilities;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace BuildMetalamaTestsDotNetSdk.Commands;

internal class NuGetCliLogger(ConsoleHelper console) : ILogger
{
    private static string FormatMessage( string level, string data, string? time = null, string? warningLevel = null )
        =>
            $"{time ?? DateTime.Now.ToString( CultureInfo.CurrentCulture )} NuGet {level}{(warningLevel == null ? "" : $" {warningLevel}")}: {data}";

    private static string FormatMessage( ILogMessage message )
        => FormatMessage( message.Level.ToString().ToUpperInvariant(), message.Message,
            message.Time.ToString( CultureInfo.CurrentCulture ),
            message.WarningLevel.ToString().ToUpperInvariant() );
    
    public void LogDebug( string data ) => console.WriteMessage( FormatMessage( "DEBUG", data ) );

    public void LogVerbose( string data ) => console.WriteMessage( FormatMessage( "VERBOSE", data ) );

    public void LogInformation( string data ) => console.WriteMessage( FormatMessage( "INFO", data ) );

    public void LogMinimal( string data ) => console.WriteMessage( FormatMessage( "MINIMAL", data ) );

    public void LogWarning( string data ) => console.WriteWarning( FormatMessage( "WARNING", data ) );

    public void LogError( string data ) => console.WriteError( FormatMessage( "ERROR", data ) );

    public void LogInformationSummary( string data ) => console.WriteMessage( FormatMessage( "SUMMARY", data ) );

    public void Log( LogLevel level, string data )
    {
        switch ( level )
        {
            case LogLevel.Debug:
                this.LogDebug( data );
                break;
            case LogLevel.Verbose:
                this.LogVerbose( data );
                break;
            case LogLevel.Information:
                this.LogInformation( data );
                break;
            case LogLevel.Minimal:
                this.LogMinimal( data );
                break;
            case LogLevel.Warning:
                this.LogWarning( data );
                break;
            case LogLevel.Error:
                this.LogError( data );
                break;
            default:
                throw new ArgumentOutOfRangeException( nameof( level ), level, null );
        }
    }

    public Task LogAsync( LogLevel level, string data )
    {
        this.Log( level, data );
        
        return Task.CompletedTask;
    }

    public void Log( ILogMessage message )
    {
        switch ( message.Level )
        {
            case LogLevel.Debug:
                console.WriteMessage( FormatMessage( message ) );
                break;
            case LogLevel.Verbose:
                console.WriteMessage( FormatMessage( message ) );
                break;
            case LogLevel.Information:
                console.WriteMessage( FormatMessage( message ) );
                break;
            case LogLevel.Minimal:
                console.WriteMessage( FormatMessage( message ) );
                break;
            case LogLevel.Warning:
                console.WriteWarning( FormatMessage( message ) );
                break;
            case LogLevel.Error:
                console.WriteError( FormatMessage( message ) );
                break;
            default:
                throw new ArgumentOutOfRangeException( nameof( message.Level ), message.Level, null );
        }
    }

    public Task LogAsync( ILogMessage message )
    {
        this.Log( message );

        return Task.CompletedTask;
    }
}