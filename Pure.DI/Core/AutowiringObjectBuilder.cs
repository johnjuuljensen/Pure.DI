﻿namespace Pure.DI.Core;

using NS35EBD81B;

// ReSharper disable once ClassNeverInstantiated.Global
internal class AutowiringObjectBuilder : IObjectBuilder
{
    private const string InstanceVarName = "instance";
    private readonly ILog<AutowiringObjectBuilder> _log;
    private readonly IDiagnostic _diagnostic;
    private readonly IBuildContext _buildContext;
    private readonly IAttributesService _attributesService;
    private readonly ITracer _tracer;
    private readonly IStringTools _stringTools;

    public AutowiringObjectBuilder(
        ILog<AutowiringObjectBuilder> log,
        IDiagnostic diagnostic,
        IBuildContext buildContext,
        IAttributesService attributesService,
        ITracer tracer,
        IStringTools stringTools)
    {
        _log = log;
        _diagnostic = diagnostic;
        _buildContext = buildContext;
        _attributesService = attributesService;
        _tracer = tracer;
        _stringTools = stringTools;
    }

    [SuppressMessage("ReSharper", "InvertIf")]
    public Optional<ExpressionSyntax> TryBuild(IBuildStrategy buildStrategy, Dependency dependency)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var ctorInfos = (
            from ctor in
                dependency.Implementation.Type is INamedTypeSymbol implementationType
                    ? implementationType.Constructors
                    : Enumerable.Empty<IMethodSymbol>()
            where !_buildContext.IsCancellationRequested
            where ctor.DeclaredAccessibility is Accessibility.Internal or Accessibility.Public or Accessibility.Friend
            let order = GetOrder(ctor) ?? int.MaxValue
            let parameters = ResolveMethodParameters($"constructor {ctor} argument", _buildContext.TypeResolver, buildStrategy, dependency, ctor, true).ToArray()
                let description = new StringBuilder(ctor.ToString())
                let resolvedParams = parameters.Where(i => i.Expression.HasValue).ToArray()
                let isResolved = parameters.Length == resolvedParams.Length
                let paramsResolvedCount = resolvedParams.Count(i => i.DefaultType == DefaultType.ResolvedValue)
                let paramsResolveWeight = resolvedParams.Sum(i => (int)i.ResolveType)
                let paramsTypeWeight = resolvedParams.Sum(i => (int)i.DefaultType)
                select (ctor, resolvedParams, parameters, order, paramsResolvedCount, paramsResolveWeight, paramsTypeWeight, isResolved))
            // By an Order attribute
            .OrderBy(i => i.order)
            // By a number of resolved parameters
            .ThenByDescending(i => i.paramsResolvedCount)
            // By an access modifier
            .ThenByDescending(i => i.ctor.DeclaredAccessibility)
            // By resolving type
            .ThenByDescending(i => i.paramsResolveWeight)
            // By param type
            .ThenByDescending(i => i.paramsTypeWeight)
            .ToArray();

        stopwatch.Stop();

        var ctorInfo = ctorInfos.FirstOrDefault(i => i.isResolved);
        if (ctorInfo == default || _buildContext.IsCancellationRequested)
        {
            var errors = (
                    from ctorResolve in ctorInfos
                    from paramResolve in ctorResolve.parameters
                    let parameterDescription = paramResolve.Expression.HasValue ? string.Empty : $"constructor {ctorResolve.ctor} argument {GetParameterDisplayName(paramResolve.Target)}"
                    where !string.IsNullOrWhiteSpace(parameterDescription)
                    select new [] {new CodeError(dependency, Diagnostics.Error.CannotResolve, parameterDescription, paramResolve.Target.Locations.ToArray())}.Concat(paramResolve.Expression.Errors)
                )
                .SelectMany(i => i)
                .DefaultIfEmpty(new CodeError(dependency, Diagnostics.Error.CannotResolve, "Cannot found a constructor", dependency.Implementation.Type.Locations.ToArray()))
                .ToArray();
            
            var reasons = string.Join("; ", errors.Select(i => i.Description));
            _log.Trace(() => new[] { $"[{stopwatch.ElapsedMilliseconds}] Cannot find a constructor for {dependency}: {reasons}." });
            _tracer.Save();
            return Optional<ExpressionSyntax>.CreateEmpty(errors);
        }
        
        _log.Trace(() => new[]
        {
            $"[{stopwatch.ElapsedMilliseconds}] Found a constructor for {dependency}: {ctorInfo.ctor?.ToString() ?? "null"}."
        });

        var objectCreationExpression = CreateObject(
            ctorInfo.ctor,
            dependency,
            SyntaxFactory.SeparatedList(
                from parameter in ctorInfo.resolvedParams
                where parameter.Expression.HasValue
                select SyntaxFactory.Argument(parameter.Expression.Value)));

        var members = (
                from member in dependency.Implementation.Type.GetMembers()
                where member.MetadataName != ".ctor"
                let order = GetOrder(member)
                where order != default
                orderby order
                select member)
            .Distinct(SymbolEqualityComparer.Default);

        List<StatementSyntax> factoryStatements = new();
        foreach (var member in members)
        {
            if (member.IsStatic || member.DeclaredAccessibility != Accessibility.Public && member.DeclaredAccessibility != Accessibility.Internal)
            {
                var error = $"{member} is inaccessible in {dependency.Implementation}.";
                _diagnostic.Error(Diagnostics.Error.MemberIsInaccessible, error, member.Locations.ToArray());
                throw new HandledException(error);
            }

            switch (member)
            {
                case IMethodSymbol method:
                    var arguments =
                        from parameter in ResolveMethodParameters($"method {member} argument", _buildContext.TypeResolver, buildStrategy, dependency, method, false)
                        where parameter.Expression.HasValue
                        select SyntaxFactory.Argument(parameter.Expression.Value);

                    var call = SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.ParseName(InstanceVarName),
                                SyntaxFactory.Token(SyntaxKind.DotToken),
                                SyntaxFactory.IdentifierName(method.Name)))
                        .AddArgumentListArguments(arguments.ToArray());

                    factoryStatements.Add(SyntaxRepo.ExpressionStatement(call));
                    break;

                case IFieldSymbol filed:
                    if (!filed.IsReadOnly && !filed.IsStatic && !filed.IsConst)
                    {
                        var fieldValue = ResolveInstance("instance field", filed, dependency, filed.Type, _buildContext.TypeResolver, buildStrategy, filed.Locations, false);
                        if (fieldValue.Expression.HasValue)
                        {
                            var assignmentExpression = SyntaxFactory.AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName(InstanceVarName),
                                    SyntaxFactory.Token(SyntaxKind.DotToken),
                                    SyntaxFactory.IdentifierName(filed.Name)),
                                fieldValue.Expression.Value);

                            factoryStatements.Add(SyntaxRepo.ExpressionStatement(assignmentExpression));
                        }
                    }

                    break;

                case IPropertySymbol property:
                    if (property.SetMethod != null && !property.IsReadOnly && !property.IsStatic)
                    {
                        var propertyValue = ResolveInstance("instance property", property, dependency, property.Type, _buildContext.TypeResolver, buildStrategy, property.Locations, false);
                        if (propertyValue.Expression.HasValue)
                        {
                            var assignmentExpression = SyntaxFactory.AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName(InstanceVarName),
                                    SyntaxFactory.Token(SyntaxKind.DotToken),
                                    SyntaxFactory.IdentifierName(property.Name)),
                                propertyValue.Expression.Value);

                            factoryStatements.Add(SyntaxRepo.ExpressionStatement(assignmentExpression));
                        }
                    }

                    break;
            }
        }

        if (factoryStatements.Any())
        {
            var memberKey = new MemberKey($"Create{_stringTools.ConvertToTitle(dependency.Binding.Implementation?.ToString() ?? string.Empty)}", dependency);
            var factoryName = _buildContext.NameService.FindName(memberKey);
            var type = dependency.Implementation.TypeSyntax;
            var factoryMethod = SyntaxRepo.MethodDeclaration(type, SyntaxFactory.Identifier(factoryName))
                .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(SyntaxRepo.AggressiveInliningAttr))
                .AddModifiers(SyntaxKind.PrivateKeyword.WithSpace(), SyntaxKind.StaticKeyword.WithSpace())
                .AddBodyStatements(
                    SyntaxFactory.LocalDeclarationStatement(
                        SyntaxFactory.VariableDeclaration(type)
                            .AddVariables(
                                SyntaxFactory.VariableDeclarator(InstanceVarName)
                                    .WithSpace()
                                    .WithInitializer(SyntaxFactory.EqualsValueClause(objectCreationExpression)))))
                .AddBodyStatements(factoryStatements.ToArray())
                .AddBodyStatements(
                    SyntaxRepo.ReturnStatement(
                        SyntaxFactory.IdentifierName(InstanceVarName)));

            _buildContext.GetOrAddMember(memberKey, () => factoryMethod);
            return SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(factoryName));
        }

        return objectCreationExpression;
    }

    private static string GetParameterDisplayName(ISymbol parameter) => 
        $"{parameter.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)} {parameter.Name}";

    private ResolveResult ResolveInstance(
        string parameterTypeDescription,
        ISymbol target,
        Dependency targetDependency,
        ITypeSymbol defaultType,
        ITypeResolver typeResolver,
        IBuildStrategy buildStrategy,
        ImmutableArray<Location> resolveLocations,
        bool probe)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        try
        {
            ExpressionSyntax? defaultValue = null;
            var defaultResolveType = DefaultType.ResolvedValue;
            if (target is IParameterSymbol parameter)
            {
                if (parameter.HasExplicitDefaultValue)
                {
                    defaultValue = parameter.ExplicitDefaultValue.ToLiteralExpression();
                    defaultResolveType = DefaultType.DefaultValue;
                }
                else
                {
                    if (parameter.Type.NullableAnnotation == NullableAnnotation.Annotated)
                    {
                        defaultValue = SyntaxFactory.DefaultExpression(new SemanticType(parameter.Type.WithNullableAnnotation(NullableAnnotation.None), targetDependency.Implementation).TypeSyntax);
                        defaultResolveType = DefaultType.NullableValue;
                    }
                }
            }

            var resolvingType = GetDependencyType(target, targetDependency.Implementation) ?? new SemanticType(defaultType, targetDependency.Implementation);
            var tag = (ExpressionSyntax?)_attributesService.GetAttributeArgumentExpressions(AttributeKind.Tag, target).FirstOrDefault();
            var dependency = typeResolver.Resolve(resolvingType, tag);
            if (!dependency.IsResolved && defaultValue != null)
            {
                return new ResolveResult(target, defaultValue, ResolveType.ResolvedUnbound, defaultResolveType);
            }

            switch (dependency.Implementation.Type)
            {
                case INamedTypeSymbol namedType:
                {
                    var type = new SemanticType(namedType, targetDependency.Implementation);
                    var constructedType = dependency.TypesMap.ConstructType(type);
                    if (!dependency.Implementation.Equals(constructedType))
                    {
                        _buildContext.AddBinding(new BindingMetadata(dependency.Binding, constructedType, null));
                    }

                    if (dependency.IsResolved)
                    {
                        try
                        {
                            return new ResolveResult(target, buildStrategy.TryBuild(dependency, resolvingType), ResolveType.Resolved, DefaultType.ResolvedValue);
                        }
                        catch (BuildException buildException)
                        {
                            if (!buildException.Dependency.Equals(targetDependency))
                            {
                                throw;
                            }

                            var parameterName = GetParameterDisplayName(target);
                            throw new BuildException(targetDependency, buildException.Id, $"{buildException.Message} when resolving {parameterTypeDescription} {parameterName}", target.Locations.ToArray());
                        }
                    }

                    if (probe)
                    {
                        return new ResolveResult(target, new Optional<ExpressionSyntax>(), ResolveType.NotResolved, DefaultType.DefaultValue);
                    }

                    var dependencyType = dependency.Implementation.TypeSyntax;
                    return new ResolveResult(target, SyntaxFactory.CastExpression(dependencyType,
                        SyntaxFactory.InvocationExpression(SyntaxFactory.ParseName(nameof(IContext<Unit>.Resolve)))
                            .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(dependencyType)))), ResolveType.ResolvedUnbound, DefaultType.ResolvedValue);
                }

                case IArrayTypeSymbol:
                {
                    return new ResolveResult(target, buildStrategy.TryBuild(dependency, dependency.Implementation), ResolveType.Resolved, DefaultType.ResolvedValue);
                }
            }

            if (!buildStrategy.TryBuild(dependency, resolvingType).HasValue)
            {
                return new ResolveResult(target, new Optional<ExpressionSyntax>(), ResolveType.NotResolved, DefaultType.DefaultValue);
            }

            var error = $"Unsupported type {dependency.Implementation}.";
            _diagnostic.Error(Diagnostics.Error.Unsupported, error, resolveLocations.ToArray());
            throw new HandledException(error);
        }
        finally
        {
            stopwatch.Stop();
            _log.Trace(() => new[]
            {
                $"[{stopwatch.ElapsedMilliseconds}] Instance resolved for {targetDependency}."
            });
        }
    }

    private IEnumerable<ResolveResult> ResolveMethodParameters(string parameterTypeDescription, ITypeResolver typeResolver, IBuildStrategy buildStrategy, Dependency dependency, IMethodSymbol method, bool probe) =>
        from parameter in method.Parameters
        select ResolveInstance(parameterTypeDescription, parameter, dependency, parameter.Type, typeResolver, buildStrategy, parameter.Locations, probe);

    private SemanticType? GetDependencyType(ISymbol type, SemanticModel semanticModel) =>
    (
        from typeExpression in _attributesService.GetAttributeArgumentExpressions(AttributeKind.Type, type)
        where typeExpression != null
        let typeSemanticModel = typeExpression.GetSemanticModel(semanticModel)
        let typeSymbol = typeSemanticModel.GetTypeInfo(typeExpression).Type
        where typeSymbol != null
        select new SemanticType(typeSymbol, typeSemanticModel)).FirstOrDefault();

    // ReSharper disable once SuggestBaseTypeForParameter
    private ExpressionSyntax CreateObject(IMethodSymbol ctor, Dependency dependency, SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        var typeSyntax = dependency.Implementation.TypeSyntax;
        if (typeSyntax.IsKind(SyntaxKind.TupleType))
        {
            return SyntaxFactory.TupleExpression()
                .WithArguments(arguments);
        }

        if (ctor.GetAttributes(typeof(ObsoleteAttribute), dependency.Implementation.SemanticModel).Any())
        {
            _diagnostic.Warning(Diagnostics.Warning.CtorIsObsoleted, $"The constructor {ctor} marked as obsoleted is used.", ctor.Locations.ToArray());
        }

        return SyntaxRepo.ObjectCreationExpression(typeSyntax)
            .WithArgumentList(SyntaxFactory.ArgumentList(arguments));
    }

    private IComparable? GetOrder(ISymbol method)
    {
        var orders =
            (from expression in _attributesService.GetAttributeArgumentExpressions(AttributeKind.Order, method)
                let order = ((expression as LiteralExpressionSyntax)?.Token)?.Value as IComparable
                select order).ToList();

        return orders.Any() ? orders.Max() : default;
    }

    private readonly struct ResolveResult
    {
        public readonly ISymbol Target;
        public readonly Optional<ExpressionSyntax> Expression;
        public readonly ResolveType ResolveType;
        public readonly DefaultType DefaultType;

        public ResolveResult(ISymbol target, Optional<ExpressionSyntax> expression, ResolveType resolveType, DefaultType defaultType)
        {
            Target = target;
            Expression = expression;
            ResolveType = resolveType;
            DefaultType = defaultType;
        }
    }

    private enum ResolveType
    {
        NotResolved = 0,
        ResolvedUnbound = 1,
        Resolved = 2
    }

    private enum DefaultType
    {
        NullableValue = 0,
        DefaultValue = 1,
        ResolvedValue = 2
    }
}