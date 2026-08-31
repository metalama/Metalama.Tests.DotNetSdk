// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Observability;
using System.ComponentModel;

// Happy path for Metalama.Patterns.Observability: [Observable] must implement
// INotifyPropertyChanged and raise PropertyChanged when a property is assigned.

[Observable]
internal class Person
{
    public string? Name { get; set; }
}

internal static class Program
{
    public static int Main()
    {
        var person = new Person();

        PackageTestCheck.That( person is INotifyPropertyChanged, "[Observable] implements INotifyPropertyChanged" );

        var raised = new List<string?>();
        ((INotifyPropertyChanged) person).PropertyChanged += ( _, e ) => raised.Add( e.PropertyName );

        person.Name = "Alice";

        PackageTestCheck.That( raised.Contains( nameof(Person.Name) ), "[Observable] raises PropertyChanged on assignment" );

        return PackageTestCheck.Report( "Metalama.Patterns.Observability" );
    }
}
