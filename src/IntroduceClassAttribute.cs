using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

[assembly: IntroduceClass]

internal class IntroduceClassAttribute : CompilationAspect
{
    public override void BuildAspect(IAspectBuilder<ICompilation> builder)
    {
        builder.With( builder.Target.GlobalNamespace ).IntroduceClass("MetalamaIntroducedClass");
    }
}