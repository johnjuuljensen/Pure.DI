// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

using NS35EBD81B;

internal class MicrosoftDependencyInjectionLifetimeStrategy : ILifetimeStrategy
{
    private readonly IBuildContext _buildContext;
    private readonly ICannotResolveExceptionFactory _cannotResolveExceptionFactory;
    private readonly Func<IBuildStrategy> _buildStrategy;
    private readonly IWrapperStrategy _wrapperStrategy;

    public MicrosoftDependencyInjectionLifetimeStrategy(
        IBuildContext buildContext,
        ICannotResolveExceptionFactory cannotResolveExceptionFactory,
        Func<IBuildStrategy> buildStrategy,
        IWrapperStrategy wrapperStrategy)
    {
        _buildContext = buildContext;
        _cannotResolveExceptionFactory = cannotResolveExceptionFactory;
        _buildStrategy = buildStrategy;
        _wrapperStrategy = wrapperStrategy;
    }

    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
    public Lifetime? Lifetime { get; private set; }

    public MicrosoftDependencyInjectionLifetimeStrategy Initialize(Lifetime lifetime)
    {
        Lifetime = lifetime;
        return this;
    }

    public ExpressionSyntax Build(SemanticType resolvingType, Dependency dependency, ExpressionSyntax objectBuildExpression)
    {
        var type = dependency.Binding.Dependencies.FirstOrDefault() ?? dependency.Implementation;
        var serviceProviderInstance = new SemanticType(dependency.Implementation.SemanticModel.Compilation.GetTypeByMetadataName(typeof(ServiceProviderInstance<>).FullName.ReplaceNamespace())!, dependency.Implementation.SemanticModel);
        var instanceType = serviceProviderInstance.Construct(type);
        var serviceProviderDependency = _buildContext.TypeResolver.Resolve(instanceType, dependency.Tag);
        var serviceProvider = _buildStrategy().TryBuild(serviceProviderDependency, instanceType);
        if (!serviceProvider.HasValue)
        {
            throw _cannotResolveExceptionFactory.Create(serviceProviderDependency.Binding, serviceProviderDependency.Tag, serviceProvider.Errors);
        }

        var instanceExpression = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            serviceProvider.Value,
            SyntaxFactory.IdentifierName(nameof(ServiceProviderInstance<object>.Value)));

        return _wrapperStrategy.Build(resolvingType, dependency, instanceExpression);
    }
}