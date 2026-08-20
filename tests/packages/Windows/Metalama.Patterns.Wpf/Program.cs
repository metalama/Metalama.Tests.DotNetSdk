// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Wpf;
using System.Windows;

// Happy path for Metalama.Patterns.Wpf: [DependencyProperty] must generate a real
// DependencyProperty behind the property, and [Command] must generate an ICommand.
// No window is shown -- the generated members are exercised directly.

internal partial class Gadget : DependencyObject
{
    [DependencyProperty]
    public string? Label { get; set; }

    public int ExecutionCount;

    [Command]
    private void ExecuteRefresh() => this.ExecutionCount++;
}

internal static class Program
{
    [STAThread]
    public static int Main()
    {
        var gadget = new Gadget();

        gadget.Label = "hello";

        PackageTestCheck.That( gadget.Label == "hello", "[DependencyProperty] round-trips a value" );
        PackageTestCheck.That(
            gadget.GetValue( Gadget.LabelProperty ) as string == "hello",
            "[DependencyProperty] generated a backing DependencyProperty" );

        PackageTestCheck.That( gadget.RefreshCommand != null, "[Command] generated an ICommand" );

        gadget.RefreshCommand.Execute( null );

        PackageTestCheck.That( gadget.ExecutionCount == 1, "[Command] invokes the method" );

        return PackageTestCheck.Report( "Metalama.Patterns.Wpf" );
    }
}
