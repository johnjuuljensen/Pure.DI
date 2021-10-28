﻿namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class MicrosoftDependencyInjectionBuilder : IMembersBuilder
    {
        private readonly ResolverMetadata _metadata;
        private readonly IBuildContext _buildContext;
        private readonly ITypeResolver _typeResolver;
        private readonly IBuildStrategy _buildStrategy;
        private readonly IDiagnostic _diagnostic;
        private readonly Log<MicrosoftDependencyInjectionBuilder> _log;
        private readonly ITracer _tracer;

        public MicrosoftDependencyInjectionBuilder(
            ResolverMetadata metadata,
            IBuildContext buildContext,
            ITypeResolver typeResolver,
            IBuildStrategy buildStrategy,
            IDiagnostic diagnostic,
            Log<MicrosoftDependencyInjectionBuilder> log,
            ITracer tracer)
        {
            _metadata = metadata;
            _buildContext = buildContext;
            _typeResolver = typeResolver;
            _buildStrategy = buildStrategy;
            _diagnostic = diagnostic;
            _log = log;
            _tracer = tracer;
        }

        public int Order => 1;

        public IEnumerable<MemberDeclarationSyntax> BuildMembers(SemanticModel semanticModel)
        {
            var dependencies = (
                from binding in _metadata.Bindings.Concat(_buildContext.AdditionalBindings)
                from dependency in binding.Dependencies
                let tags = binding.GetTags(dependency)
                where dependency.Type.IsReferenceType && !dependency.IsComposedGenericTypeMarker
                group (binding, tags) by dependency
                into groups
                let item = groups.First()
                select (dependency: groups.Key, lifetime: item.binding.Lifetime, item.binding, item.tags)).ToArray();

            var serviceProviderType = semanticModel.Compilation.GetTypeByMetadataName(typeof(System.IServiceProvider).FullName);
            var serviceCollectionType = semanticModel.Compilation.GetTypeByMetadataName("Microsoft.Extensions.DependencyInjection.IServiceCollection");
            var serviceDescriptorType = semanticModel.Compilation.GetTypeByMetadataName("Microsoft.Extensions.DependencyInjection.ServiceDescriptor");
            var serviceCollectionDescriptorExtensionsType = semanticModel.Compilation.GetTypeByMetadataName("Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions");
            var serviceCollectionServiceExtensionsType = semanticModel.Compilation.GetTypeByMetadataName("Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions");

            if (
                serviceProviderType == null
                || serviceCollectionType == null
                || serviceDescriptorType == null
                || serviceCollectionDescriptorExtensionsType == null
                || serviceCollectionServiceExtensionsType == null)
            {
                foreach (var (dependency, lifetime, binding, _) in dependencies.Where(i => i.lifetime is Lifetime.Scoped or Lifetime.ContainerSingleton))
                {
                    var error = $"Impossible to use the lifetime {lifetime} for {dependency} outside an ASP.NET context.";
                    _diagnostic.Error(Diagnostics.Error.Unsupported, error, binding.Location);
                }
            }
            else
            {
                var extensionMethodName = $"Add{_metadata.ComposerTypeName}";
                if (dependencies.Any(i => i.tags.Any()))
                {
                    _diagnostic.Information(Diagnostics.Information.Unsupported, $"The IServiceCollection extension method \"{extensionMethodName}\" was not generated due to it is not supported for a tagged binding.");
                    yield break;
                }

                var extensionKey = new MemberKey(extensionMethodName, GetType());
                yield return _buildContext.GetOrAddMember(extensionKey, () =>
                {
                    var serviceProvider = new SemanticType(serviceProviderType, semanticModel);
                    var serviceCollection = new SemanticType(serviceCollectionType, semanticModel);
                    var serviceDescriptor = new SemanticType(serviceDescriptorType, semanticModel);
                    var serviceCollectionDescriptorExtensions = new SemanticType(serviceCollectionDescriptorExtensionsType, semanticModel);
                    var serviceCollectionServiceExtensions = new SemanticType(serviceCollectionServiceExtensionsType, semanticModel);

                    var thisParameter = SyntaxFactory.Parameter(
                        SyntaxFactory.List<AttributeListSyntax>(),
                        SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ThisKeyword)),
                        serviceCollection,
                        SyntaxFactory.Identifier("services"),
                        null);

                    var method = SyntaxFactory.MethodDeclaration(serviceCollection, $"Add{_metadata.ComposerTypeName}")
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.InternalKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                        .AddParameterListParameters(thisParameter);

                    var mvcBuilderType = semanticModel.Compilation.GetTypeByMetadataName("Microsoft.Extensions.DependencyInjection.IMvcBuilder");
                    var mvcServiceCollectionExtensionsType = semanticModel.Compilation.GetTypeByMetadataName("Microsoft.Extensions.DependencyInjection.MvcServiceCollectionExtensions");
                    var controllerFeatureType = semanticModel.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.Controllers.ControllerFeature");
                    var controllerActivatorType = semanticModel.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.Controllers.IControllerActivator");
                    var serviceBasedControllerActivatorType = semanticModel.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.Controllers.ServiceBasedControllerActivator");

                    if (mvcBuilderType != null
                        && mvcServiceCollectionExtensionsType != null
                        && controllerFeatureType != null
                        && controllerActivatorType != null
                        && serviceBasedControllerActivatorType != null)
                    {
                        var mvcBuilder = new SemanticType(mvcBuilderType, semanticModel);
                        var mvcServiceCollectionExtensions = new SemanticType(mvcServiceCollectionExtensionsType, semanticModel);
                        var controllerFeature = new SemanticType(controllerFeatureType, semanticModel);
                        var controllerActivator = new SemanticType(controllerActivatorType, semanticModel);
                        var serviceBasedControllerActivator = new SemanticType(serviceBasedControllerActivatorType, semanticModel);

                        var getBuilder = SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    mvcServiceCollectionExtensions,
                                    SyntaxFactory.IdentifierName("AddControllers")))
                            .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("services")));

                        var declareBuilder = SyntaxFactory.LocalDeclarationStatement(
                            SyntaxFactory.VariableDeclaration(mvcBuilder)
                                .AddVariables(
                                    SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("builder"))
                                        .WithInitializer(SyntaxFactory.EqualsValueClause(getBuilder))));

                        method = method.AddBodyStatements(declareBuilder);

                        var populateFeature =
                            SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.IdentifierName("builder"),
                                            SyntaxFactory.IdentifierName("PartManager")),
                                        SyntaxFactory.IdentifierName("PopulateFeature")))
                                .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.ObjectCreationExpression(controllerFeature).AddArgumentListArguments()));

                        method = method.AddBodyStatements(SyntaxFactory.ExpressionStatement(populateFeature));

                        var transient = SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    serviceDescriptor,
                                    SyntaxFactory.GenericName("Transient")
                                        .AddTypeArgumentListArguments(
                                            controllerActivator, serviceBasedControllerActivator)))
                            .AddArgumentListArguments();

                        var bindControllerActivator = SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    serviceCollectionDescriptorExtensions,
                                    SyntaxFactory.IdentifierName("Replace")))
                            .AddArgumentListArguments(
                                SyntaxFactory.Argument(
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName("builder"),
                                        SyntaxFactory.IdentifierName("Services"))
                                ),
                                SyntaxFactory.Argument(transient)
                            );

                        method = method.AddBodyStatements(SyntaxFactory.ExpressionStatement(bindControllerActivator));
                    }

                    foreach (var (dependency, lifetime, _, _) in dependencies)
                    {
                        if (dependency.Equals(serviceProvider))
                        {
                            // Skip IServiceProvider
                            continue;
                        }

                        if (_buildContext.IsCancellationRequested)
                        {
                            _log.Trace(() => new[]
                            {
                                "Build canceled"
                            });

                            continue;
                        }

                        string lifetimeName = lifetime switch
                        {
                            Lifetime.ContainerSingleton => "AddSingleton",
                            Lifetime.Scoped => "AddScoped",
                            _ => "AddTransient"
                        };

                        var curDependency = _typeResolver.Resolve(dependency, null);

                        var objectExpression = curDependency.ObjectBuilder.TryBuild(_buildStrategy, curDependency);
                        if (objectExpression == null)
                        {
                            _tracer.Save();
                            continue;
                        }

                        var resolve = SyntaxFactory.CastExpression(dependency, objectExpression);

                        var serviceProviderInstance = new SemanticType(semanticModel.Compilation.GetTypeByMetadataName(typeof(ServiceProviderInstance).ToString())!, semanticModel);

                        var serviceProviderValue = SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            serviceProviderInstance,
                            SyntaxFactory.IdentifierName(nameof(ServiceProviderInstance.ServiceProvider)));

                        var tryBlock = SyntaxFactory.Block()
                            .AddStatements()
                            .AddStatements(SyntaxFactory.ExpressionStatement(
                                SyntaxFactory.AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression, serviceProviderValue, SyntaxFactory.IdentifierName("serviceProvider"))))
                            .AddStatements(SyntaxFactory.ReturnStatement(resolve));

                        var finallyBlock = SyntaxFactory.Block()
                            .AddStatements(SyntaxFactory.ExpressionStatement(
                                SyntaxFactory.AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression, serviceProviderValue, SyntaxFactory.IdentifierName("prevServiceProvider"))));

                        var lambdaBlock =
                            SyntaxFactory.Block()
                                .AddStatements(
                                    SyntaxFactory.LocalDeclarationStatement(
                                        SyntaxFactory.VariableDeclaration(serviceProvider)
                                            .AddVariables(
                                                SyntaxFactory.VariableDeclarator("prevServiceProvider")
                                                    .WithInitializer(SyntaxFactory.EqualsValueClause(serviceProviderValue))
                                            )
                                    )
                                )
                                .AddStatements(
                                    SyntaxFactory.TryStatement(
                                        tryBlock,
                                        SyntaxFactory.List<CatchClauseSyntax>(),
                                        SyntaxFactory.FinallyClause(finallyBlock)));

                        var resolveLambda = SyntaxFactory.SimpleLambdaExpression(SyntaxFactory.Parameter(SyntaxFactory.Identifier("serviceProvider")), lambdaBlock);

                        var bind =
                            SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        serviceCollectionServiceExtensions,
                                        SyntaxFactory.IdentifierName(lifetimeName)))
                                .AddArgumentListArguments(
                                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName("services")),
                                    SyntaxFactory.Argument(resolveLambda)
                                );

                        method = method.AddBodyStatements(SyntaxFactory.ExpressionStatement(bind));
                    }

                    return method.AddBodyStatements(SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName("services")));
                });
            }
        }
    }
}