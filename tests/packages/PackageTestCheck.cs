// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Collections.Generic;

/// <summary>
/// Minimal assertion helper shared by every package test.
/// </summary>
/// <remarks>
/// Deliberately not a test framework. These programs run in every matrix cell, under
/// four SDK versions, three SDK sources and four build tools -- a plain executable that
/// returns a non-zero exit code is the only shape that behaves identically everywhere.
/// </remarks>
internal static class PackageTestCheck
{
    private static readonly List<string> _failures = [];
    private static int _passed;

    public static void That( bool condition, string description )
    {
        if ( condition )
        {
            _passed++;
            Console.WriteLine( $"  PASS  {description}" );
        }
        else
        {
            _failures.Add( description );
            Console.WriteLine( $"  FAIL  {description}" );
        }
    }

    public static void Throws<T>( Action action, string description )
        where T : Exception
    {
        try
        {
            action();
            That( false, $"{description} (no exception was thrown)" );
        }
        catch ( T )
        {
            That( true, description );
        }
        catch ( Exception e )
        {
            That( false, $"{description} (expected {typeof(T).Name}, got {e.GetType().Name})" );
        }
    }

    /// <summary>
    /// Writes a summary and returns the process exit code.
    /// </summary>
    public static int Report( string packageName )
    {
        if ( _failures.Count == 0 )
        {
            Console.WriteLine( $"{packageName}: OK ({_passed} check(s) passed)." );

            return 0;
        }

        Console.WriteLine( $"{packageName}: FAILED ({_failures.Count} of {_failures.Count + _passed} check(s) failed):" );

        foreach ( var failure in _failures )
        {
            Console.WriteLine( $"  - {failure}" );
        }

        return 1;
    }
}
