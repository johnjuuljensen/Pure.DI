// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

using NS35EBD81B;

internal class MetadataSyntaxFilter : ISyntaxFilter
{
    private static readonly ISet<string> Names = new HashSet<string>
    {
        nameof(DI.Setup),
        nameof(IConfiguration<Unit>.DependsOn),
        nameof(IConfiguration<Unit>.Bind),
        nameof(IConfiguration<Unit>.Default),
        nameof(IConfiguration<Unit>.OrderAttribute),
        nameof(IConfiguration<Unit>.TagAttribute),
        nameof(IConfiguration<Unit>.TypeAttribute),
        nameof(IBinding<Unit>.As),
        nameof(IBinding<Unit>.Bind),
        nameof(IBinding<Unit>.Tags),
        nameof(IBinding<Unit>.To),
        nameof(IBinding<Unit>.AnyTag),
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