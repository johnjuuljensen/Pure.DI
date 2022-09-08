// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

using NS35EBD81B;

internal class MetadataSyntaxFilter : ISyntaxFilter
{
    private static readonly ISet<string> Names = new HashSet<string>
    {
        nameof(DI.Setup),
        nameof(IConfiguration<DI.Unit>.DependsOn),
        nameof(IConfiguration<DI.Unit>.Bind),
        nameof(IConfiguration<DI.Unit>.Default),
        nameof(IConfiguration<DI.Unit>.OrderAttribute),
        nameof(IConfiguration<DI.Unit>.TagAttribute),
        nameof(IConfiguration<DI.Unit>.TypeAttribute),
        nameof(IBinding<DI.Unit>.As),
        nameof(IBinding<DI.Unit>.Bind),
        nameof(IBinding<DI.Unit>.Tags),
        nameof(IBinding<DI.Unit>.To),
        nameof(IBinding<DI.Unit>.AnyTag),
        nameof(IBinding)
    };

    public bool Accept(SyntaxNode node)
    {
        if (node is not InvocationExpressionSyntax invocation)
        {
            return false;
        }

        var names = new HashSet<string>();
        foreach (var descendantNode in invocation.Expression.DescendantNodes())
        {
            switch (descendantNode)
            {
                case IdentifierNameSyntax identifierNameSyntax:
                    names.Add(identifierNameSyntax.Identifier.Text);
                    break;

                case GenericNameSyntax genericNameSyntax:
                    names.Add(genericNameSyntax.Identifier.Text);
                    break;
            }
        }

        return names.Overlaps(Names);
    }
}