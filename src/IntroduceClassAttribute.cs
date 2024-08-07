using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

[assembly: IntroduceClass]

internal class IntroduceClassAttribute : CompilationAspect
{
    public override void BuildAspect(IAspectBuilder<ICompilation> builder)
    {
        builder.Advice.IntroduceClass(builder.Target.GlobalNamespace, "MetalamaIntroducedClass");
    }
}