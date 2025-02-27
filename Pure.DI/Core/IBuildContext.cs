﻿namespace Pure.DI.Core;

internal interface IBuildContext
{
    int Id { get; }

    Compilation Compilation { get; }

    bool IsCancellationRequested { get; }

    ResolverMetadata Metadata { get; }

    void Initialize(int id, Compilation compilation, CancellationToken cancellationToken, ResolverMetadata metadata);

    INameService NameService { get; }

    ITypeResolver TypeResolver { get; }

    IEnumerable<IBindingMetadata> AdditionalBindings { get; }

    IEnumerable<MemberDeclarationSyntax> AdditionalMembers { get; }

    IEnumerable<StatementSyntax> FinalizationStatements { get; }

    IEnumerable<StatementSyntax> FinalDisposeStatements { get; }

    void AddBinding(IBindingMetadata binding);

    T GetOrAddMember<T>(MemberKey key, Func<T> additionalMemberFactory) where T : MemberDeclarationSyntax;

    void AddFinalizationStatements(IEnumerable<StatementSyntax> finalizationStatement);

    void AddFinalDisposeStatements(IEnumerable<StatementSyntax> releaseStatements);
}