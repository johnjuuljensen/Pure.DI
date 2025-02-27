namespace Pure.DI.Core;

internal interface ICannotResolveExceptionFactory
{
    HandledException Create(IBindingMetadata binding, ExpressionSyntax? tag, CodeError[] errors);
}