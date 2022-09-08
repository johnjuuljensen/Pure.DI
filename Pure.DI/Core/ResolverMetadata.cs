namespace Pure.DI.Core;

internal record ResolverMetadata(SyntaxNode SetupNode, string ComposerTypeName, ClassDeclarationSyntax? Owner, TypeSyntax ContextArgType )
{
    public readonly string ComposerTypeName = ComposerTypeName;
    public readonly ClassDeclarationSyntax? Owner = Owner;
    public readonly SyntaxNode SetupNode = SetupNode;
    public readonly ICollection<IBindingMetadata> Bindings = new List<IBindingMetadata>();
    public readonly ICollection<string> DependsOn = new HashSet<string>
    {
        "DefaultFeature",
        "AspNetFeature"
    };
    public readonly ICollection<AttributeMetadata> Attributes = new List<AttributeMetadata>();
    public readonly IDictionary<Setting, string> Settings = new Dictionary<Setting, string>();
    public readonly TypeSyntax ContextArgType = ContextArgType;

    public void Merge(ResolverMetadata dependency)
    {
        foreach (var dependsOn in dependency.DependsOn)
        {
            DependsOn.Add(dependsOn);
        }

        foreach (var binding in dependency.Bindings)
        {
            Bindings.Add(binding);
        }

        foreach (var attribute in dependency.Attributes)
        {
            Attributes.Add(attribute);
        }

        foreach (var setting in dependency.Settings)
        {
            Settings[setting.Key] = setting.Value;
        }

        if (!ContextArgType.IsEquivalentTo(dependency.ContextArgType)) {
            throw new NotSupportedException( "ContextArgType must match dependency" );
        }
    }
}