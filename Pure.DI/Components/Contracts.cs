﻿// ReSharper disable UnusedParameter.Global
// ReSharper disable UnusedParameter.Global
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable NotAccessedField.Global
// ReSharper disable StructCanBeMadeReadOnly
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ArrangeNamespaceBody
#pragma warning disable 0436
#pragma warning disable 8625
namespace NS35EBD81B
{
    using System;

    /// <summary>
    /// Binding lifetimes.
    /// </summary>
    internal enum Lifetime
    {
        /// <summary>
        /// Creates a new instance of the requested type every time. To dispose <see cref="IDisposable"/> instances subscribe on <c>OnDisposable</c> event, store them and and dispose these instances at appropriate time manually.
        /// <example>
        /// <code>
        /// Composer.OnDisposable += e => disposables.Add(e.Disposable);
        /// </code>
        /// </example>
        /// </summary>
        Transient,

        /// <summary>
        /// Creates an instance first time and then provides the same instance each time. To dispose <see cref="IDisposable"/> instances use the method <c>Composer.FinalDispose();</c>
        /// </summary>
        Singleton,

        /// <summary>
        /// The per resolve lifetime is similar to the <see cref="Lifetime.Transient"/>, but it reuses the same instance in the recursive object graph. To manage disposable instances subscribe on <c>Composer.OnDisposable</c> event, store them and dispose instances at appropriate time manually.
        /// <example>
        /// <code>
        /// Composer.OnDisposable += e => disposables.Add(e.Disposable);
        /// </code>
        /// </example>
        /// </summary>
        PerResolve,

        /// <summary>
        /// This lifetime is applicable for integration with Microsoft Dependency Injection. Specifies that a single instance of the service will be created.
        /// </summary>
        ContainerSingleton,

        /// <summary>
        /// This lifetime is applicable for integration with Microsoft Dependency Injection. Specifies that a new instance of the service will be created for each scope.
        /// </summary>
        Scoped
    }

    /// <summary>
    /// Represents the generic type arguments marker. It allows creating custom generic type arguments marker like <see cref="TTS"/>, <see cref="TTDictionary{TKey,TValue}"/> and etc. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct)]
    internal sealed class GenericTypeArgumentAttribute : Attribute { }
    
    /// <summary>
    /// Represents an order attribute overriding an injection order.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field)]
    internal class OrderAttribute : Attribute
    {
        // ReSharper disable once MemberCanBePrivate.Global
        /// <summary>
        /// The injection order.
        /// </summary>
        public readonly int Order;

        /// <summary>
        /// Creates an attribute instance.
        /// </summary>
        /// <param name="order">The injection order.</param>
        public OrderAttribute(int order)
        {
            Order = order;
        }
    }

    /// <summary>
    /// Represents a tag attribute overriding an injection tag.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field)]
    internal class TagAttribute : Attribute
    {
        // ReSharper disable once MemberCanBePrivate.Global
        /// <summary>
        /// The injection tag.
        /// </summary>
        public readonly object Tag;

        /// <summary>
        /// Creates an attribute instance.
        /// </summary>
        /// <param name="tag">The injection tag. See also <see cref="IBinding.Tags"/></param>.
        public TagAttribute(object tag)
        {
            Tag = tag;
        }
    }

    /// <summary>
    /// Represents a dependency type attribute overriding an injection type. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field)]
    internal class TypeAttribute : Attribute
    {
        // ReSharper disable once MemberCanBePrivate.Global
        /// <summary>
        /// The injection type.
        /// </summary>
        public readonly Type Type;

        /// <summary>
        /// Creates an attribute instance.
        /// </summary>
        /// <param name="type">The injection type. See also <see cref="IConfiguration.Bind{T}"/> and <see cref="IBinding.Bind{T}"/>.</param>
        public TypeAttribute(Type type)
        {
            Type = type;
        }
    }

    /// <summary>
    /// Represents an attribute of including regular expression filter for implementation types processing by <c>IFactory</c>. Is used together with <see cref="IFactory"/>.
    /// <example>
    /// <code>
    /// [Include("MyClass.+")]
    /// public class MyFactory : IFactory
    /// {
    /// }
    /// </code>
    /// </example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    internal class IncludeAttribute : Attribute
    {
        /// <summary>
        /// The regular expression to include full implementation type names.
        /// </summary>
        public readonly string ImplementationTypeNameRegularExpression;

        /// <summary>
        /// Creates an attribute instance.
        /// </summary>
        /// <param name="implementationTypeNameRegularExpression">The regular expression to include full implementation type names.</param>
        public IncludeAttribute(string implementationTypeNameRegularExpression)
        {
            ImplementationTypeNameRegularExpression = implementationTypeNameRegularExpression;
        }
    }
    
    /// <summary>
    /// Represents an attribute of excluding regular expression filter for implementation types processing by <c>IFactory</c>. Is used together with <see cref="IFactory"/>.
    /// <example>
    /// <code>
    /// [Exclude("Logger.+")]
    /// public class MyFactory : IFactory
    /// {
    /// }
    /// </code>
    /// </example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    internal class ExcludeAttribute : Attribute
    {
        /// <summary>
        /// The regular expression to exclude full implementation type names.
        /// </summary>
        public readonly string ImplementationTypeNameRegularExpression;

        /// <summary>
        /// Creates an attribute instance.
        /// </summary>
        /// <param name="implementationTypeNameRegularExpression">The regular expression to exclude full implementation type names.</param>
        public ExcludeAttribute(string implementationTypeNameRegularExpression)
        {
            ImplementationTypeNameRegularExpression = implementationTypeNameRegularExpression;
        }
    }
    
    /// <summary>
    /// Provides API to configure a DI composer.
    /// <example>
    /// <code>
    /// static partial class Composer
    /// {
    ///   private static readonly Random Indeterminacy = new();
    ///   static Composer() =&gt; DI.Setup()
    ///     .Bind&lt;State&gt;().To(_ =&gt; (State)Indeterminacy.Next(2))
    ///     .Bind&lt;ICat&gt;().To&lt;ShroedingersCat&gt;()
    ///     .Bind&lt;IBox&lt;TT&gt;&gt;().To&gt;CardboardBox&lt;TT&gt;&gt;()
    ///     .Bind&lt;Program&gt;().As(Lifetime.Singleton).To&lt;Program&gt;();
    /// }
    /// </code>
    /// </example>
    /// </summary>
    internal static class DI
    {
        internal class Unit { }

        /// <summary>
        /// Starts a new or continues an existing DI configuration chain.
        /// <example>
        /// <code>
        /// static partial class Composer
        /// {
        ///   static Composer() =&gt; DI.Setup("MyComposer");
        /// }
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="composerTypeName">The optional argument specifying a custom DI composer type name to generate. By default, it is a name of an owner class if the owner class is <c>static partial class</c> otherwise, it is a name of an owner plus the "DI" postfix. /// <param name="composerTypeName">The optional argument specifying a custom DI composer type name to generate. By default, it is a name of an owner class if the owner class is <c>static partial class</c> otherwise, it is a name of an owner plus the "DI" postfix. For a top level statements application the name is <c>Composer</c> by default.</param></param>
        /// <returns>DI configuration API.</returns>
        internal static IConfiguration<TContextArgs> Setup<TContextArgs>( string composerTypeName = "")
        {
            return Configuration<TContextArgs>.Shared;
        }

        internal static IConfiguration<Unit> Setup( string composerTypeName = "" ) => 
            Setup<Unit>(composerTypeName);

        private class Configuration<TContextArgs>: IConfiguration<TContextArgs>
        {
            public static readonly IConfiguration<TContextArgs> Shared = new Configuration<TContextArgs>();

            /// <inheritdoc />
            public IBinding<TContextArgs> Bind<T>(params object[] tags)
            {
                return new Binding<TContextArgs>( this);
            }

            /// <inheritdoc />
            public IConfiguration<TContextArgs> DependsOn(string baseConfigurationName)
            {
                return this;
            }

            /// <inheritdoc />
            public IConfiguration<TContextArgs> TypeAttribute<T>(int typeArgumentPosition = 0) where T : Attribute
            {
                return this;
            }

            /// <inheritdoc />
            public IConfiguration<TContextArgs> TagAttribute<T>(int tagArgumentPosition = 0) where T : Attribute
            {
                return this;
            }

            /// <inheritdoc />
            public IConfiguration<TContextArgs> OrderAttribute<T>(int orderArgumentPosition = 0) where T : Attribute
            {
                return this;
            }

            /// <inheritdoc />
            public IConfiguration<TContextArgs> Default(Lifetime lifetime)
            {
                return this;
            }
        }

        private class Binding<TContextArgs>: IBinding<TContextArgs> {
            private readonly IConfiguration<TContextArgs> _configuration;

            public Binding(IConfiguration<TContextArgs> configuration )
            {
                _configuration = configuration;
            }

            /// <inheritdoc />
            public IBinding<TContextArgs> Bind<T>(params object[] tags)
            {
                return this;
            }

            /// <inheritdoc />
            public IBinding<TContextArgs> As(Lifetime lifetime)
            {
                return this;
            }

            /// <inheritdoc />
            public IBinding<TContextArgs> Tags(params object[] tags)
            {
                return this;
            }

            /// <inheritdoc />
            public IBinding<TContextArgs> AnyTag()
            {
                return this;
            }

            /// <inheritdoc />
            public IConfiguration<TContextArgs> To<T>()
            {
                return _configuration;
            }

            /// <inheritdoc />
            public IConfiguration<TContextArgs> To<T>(Func<IContext<TContextArgs>, T> factory)
            {
                return _configuration;
            }
        }
    }

    /// <summary>
    /// API to configure DI.
    /// </summary>
    internal interface IConfiguration { }
    internal interface IConfiguration<TContextArgs> : IConfiguration
    {
        /// <summary>
        /// Starts a binding.
        /// <example>
        /// <code>
        /// static partial class Composer
        /// {
        ///   static Composer() =&gt; DI.Setup()
        ///     .Bind&lt;IBox&lt;TT&gt;&gt;().To&gt;CardboardBox&lt;TT&gt;&gt;()
        /// }
        /// </code>
        /// </example>
        /// </summary>
        /// <typeparam name="T">The type of dependency to bind. Also supports generic type markers like <see cref="TT"/>, <see cref="TTList{T}"/> and others.</typeparam>
        /// <param name="tags">The optional argument specifying the tags for the specific dependency type of binding.</param>
        /// <returns>Binding configuration API.</returns>
        IBinding<TContextArgs> Bind<T>(params object[] tags);

        /// <summary>
        /// Use some DI configuration as a base by its name.
        /// <example>
        /// <code>
        /// static partial class Composer
        /// {
        ///   static Composer() =&gt; DI.Setup()
        ///     .DependsOn("MyBaseComposer");
        /// }
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="baseConfigurationName">The name of a base DI configuration.</param>
        /// <returns>DI configuration API.</returns>
        IConfiguration<TContextArgs> DependsOn(string baseConfigurationName);

        /// <summary>
        /// Determines a custom attribute overriding an injection type.
        /// <example>
        /// <code>
        /// [AttributeUsage(
        ///   AttributeTargets.Parameter
        ///   | AttributeTargets.Property
        ///   | AttributeTargets.Field)]
        /// public class MyTypeAttribute : Attribute
        /// {
        ///   public readonly Type Type;
        ///   public MyTypeAttribute(Type type) => Type = type;
        /// }
        /// static partial class Composer
        /// {
        ///   static Composer() =&gt; DI.Setup()
        ///     .TypeAttribute&lt;MyTypeAttribute&gt;();
        /// }
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="typeArgumentPosition">The optional position of a type parameter in the attribute constructor. See the predefined <see cref="TypeAttribute{T}"/> attribute.</param>
        /// <typeparam name="T">The attribute type.</typeparam>
        /// <returns>DI configuration API.</returns>
        IConfiguration<TContextArgs> TypeAttribute<T>(int typeArgumentPosition = 0) where T : Attribute;

        /// <summary>
        /// Determines a tag attribute overriding an injection tag.
        /// <example>
        /// <code>
        /// [AttributeUsage(
        ///   AttributeTargets.Parameter
        ///   | AttributeTargets.Property
        ///   | AttributeTargets.Field)]
        /// public class MyTagAttribute : Attribute
        /// {
        ///   public readonly object Tag;
        ///   public MyTagAttribute(object tag) => Tag = tag;
        /// }
        /// static partial class Composer
        /// {
        ///   static Composer() =&gt; DI.Setup()
        ///     .TagAttribute&lt;MyTagAttribute&gt;();
        /// }
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="tagArgumentPosition">The optional position of a tag parameter in the attribute constructor. See the predefined <see cref="TagAttribute{T}"/> attribute.</param>
        /// <typeparam name="T">The attribute type.</typeparam>
        /// <returns>DI configuration API.</returns>
        IConfiguration<TContextArgs> TagAttribute<T>(int tagArgumentPosition = 0) where T : Attribute;

        /// <summary>
        /// Determines a custom attribute overriding an injection order.
        /// <example>
        /// <code>
        /// [AttributeUsage(
        ///   AttributeTargets.Constructor
        ///   | AttributeTargets.Method
        ///   | AttributeTargets.Property
        ///   | AttributeTargets.Field)]
        /// public class MyOrderAttribute : Attribute
        /// {
        ///   public readonly int Order;
        ///   public MyOrderAttribute(int order) => Order = order;
        /// }
        /// static partial class Composer
        /// {
        ///   static Composer() =&gt; DI.Setup()
        ///     .OrderAttribute&lt;MyOrderAttribute&gt;();
        /// }
        /// </code>
        /// </example> 
        /// </summary>
        /// <param name="orderArgumentPosition">The optional position of an order parameter in the attribute constructor. 0 by default. See the predefined <see cref="OrderAttribute{T}"/> attribute.</param>
        /// <typeparam name="T">The attribute type.</typeparam>
        /// <returns>DI configuration API.</returns>
        IConfiguration<TContextArgs> OrderAttribute<T>(int orderArgumentPosition = 0) where T : Attribute;

        /// <summary>
        /// Overrides a default <see cref="Lifetime"/>. <see cref="Lifetime.Transient"/> is default lifetime.
        /// <example>
        /// <code>
        /// static partial class Composer
        /// {
        ///   static Composer() =&gt; DI.Setup()
        ///     .Default(Lifetime.Singleton)
        ///     .Bind&lt;ICat&gt;().To&lt;ShroedingersCat&gt;();
        /// }
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="lifetime">The new default lifetime.</param>
        /// <returns>DI configuration API.</returns>
        IConfiguration<TContextArgs> Default(Lifetime lifetime);
    }

    /// <summary>
    /// API to configure a binding.
    /// </summary>
    internal interface IBinding { }
    internal interface IBinding<TContextArgs> : IBinding
    {
        /// <summary>
        /// Continue a binding configuration chain, determining an additional dependency type.
        /// <example>
        /// <code>
        /// static partial class Composer
        /// {
        ///   static Composer() =&gt; DI.Setup()
        ///     .Bind&lt;ICat&gt;().To&lt;ShroedingersCat&gt;();
        /// }
        /// </code>
        /// </example>
        /// </summary>
        /// <typeparam name="T">The type of dependency to bind. Also supports generic type markers like <see cref="TT"/>, <see cref="TTList{T}"/> and others.</typeparam>
        /// <param name="tags">The optional argument specifying the tags for the specific dependency type of binding.</param>
        /// <returns>Binding configuration API.</returns>
        IBinding<TContextArgs> Bind<T>(params object[] tags);

        /// <summary>
        /// Determines a binding <see cref="Lifetime"/>.
        /// <example>
        /// <code>
        /// static partial class Composer
        /// {
        ///   static Composer() =&gt; DI.Setup()
        ///     .Bind&lt;ICat&gt;().As(Lifetime.Singleton).To&lt;ShroedingersCat&gt;();
        /// }
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="lifetime">The binding <see cref="Lifetime"/>.</param>
        /// <returns>Binding configuration API.</returns>
        IBinding<TContextArgs> As(Lifetime lifetime);

        /// <summary>
        /// Determines a binding tag.
        /// <example>
        /// <code>
        /// static partial class Composer
        /// {
        ///   static Composer() =&gt; DI.Setup()
        ///     .Bind&lt;ICat&gt;().Tag("MyCat").To&lt;ShroedingersCat&gt;();
        /// }
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="tags">Tags for all dependency types of binding.</param>
        /// <returns>Binding configuration API.</returns>
        IBinding<TContextArgs> Tags(params object[] tags);

        /// <summary>
        /// Determines a binding suitable for any tag.
        /// <example>
        /// <code>
        /// static partial class Composer
        /// {
        ///   static Composer() =&gt; DI.Setup()
        ///     .Bind&lt;ICat&gt;().AnyTag().To&lt;ShroedingersCat&gt;();
        /// }
        /// </code>
        /// </example>
        /// </summary>
        /// <returns>Binding configuration API.</returns>
        IBinding<TContextArgs> AnyTag();

        /// <summary>
        /// Finish a binding configuration chain by determining a binding implementation.
        /// <example>
        /// <code>
        /// static partial class Composer
        /// {
        ///   static Composer() =&gt; DI.Setup()
        ///     .Bind&lt;ICat&gt;().To&lt;ShroedingersCat&gt;();
        /// }
        /// </code>
        /// </example>
        /// </summary>
        /// <typeparam name="T">The type of binding implementation. Also supports generic type markers like <see cref="TT"/>, <see cref="TTList{T}"/> and others.</typeparam>
        /// <returns>DI configuration API.</returns>
        IConfiguration<TContextArgs> To<T>();

        /// <summary>
        /// Finish a binding configuration chain by determining a binding implementation using a factory method. It allows to resole an instance manually, invoke required methods, initialize properties, fields and etc.
        /// <example>
        /// <code>
        /// DI.Setup()
        ///  .Bind&lt;IDependency&gt;().To&lt;Dependency&gt;()
        ///  .Bind&lt;INamedService&gt;().To(
        ///    ctx =&gt;
        ///    {
        ///      var service = new InitializingNamedService(ctx.Resolve&lt;IDependency&gt;());
        ///      service.Initialize("Initialized!", ctx.Resolve&lt;IDependency&gt;());
        ///      return service;
        ///    });
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="factory">The method providing an dependency implementation.</param>
        /// <typeparam name="T">The type of binding implementation. Also supports generic type markers like <see cref="TT"/>, <see cref="TTList{T}"/> and others.</typeparam>
        /// <returns>DI configuration.</returns>
        IConfiguration<TContextArgs> To<T>(Func<IContext<TContextArgs>, T> factory);
    }

    /// <summary>
    /// The abstraction to resolve a DI dependency via <see cref="IBinding.To{T}(System.Func{IContext,T})"/>.
    /// </summary>
    internal interface IContext<TArgs>
    {
        TArgs Args { get; }

        /// <summary>
        /// Resolves a composition root.
        /// <example>
        /// <code>
        /// DI.Setup()
        ///  .Bind&lt;IDependency&gt;().To&lt;Dependency&gt;()
        ///  .Bind&lt;INamedService&gt;().To(
        ///    ctx =&gt;
        ///    {
        ///      var service = new InitializingNamedService(ctx.Resolve&lt;IDependency&gt;());
        ///      service.Initialize("Initialized!", ctx.Resolve&lt;IDependency&gt;());
        ///      return service;
        ///    });
        /// </code>
        /// </example>
        /// </summary>
        /// <typeparam name="T">The type of a root.</typeparam>
        /// <returns>A resolved dependency.</returns>
        T Resolve<T>();

        /// <summary>
        /// Resolves a composition root marked with a tag. See also <see cref="IBinding.Tags"/>./>
        /// <example>
        /// <code>
        /// DI.Setup()
        ///  .Bind&lt;IDependency&gt;().Tag("MyDependency").To&lt;Dependency&gt;()
        ///  .Bind&lt;INamedService&gt;().To(
        ///    ctx =&gt;
        ///    {
        ///      var service = new InitializingNamedService(ctx.Resolve&lt;IDependency&gt;("MyDependency"));
        ///      service.Initialize("Initialized!", ctx.Resolve&lt;IDependency&gt;());
        ///      return service;
        ///    });
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="tag">The tag of of a composition root instance.</param>
        /// <typeparam name="T">The type of a composition root instance.</typeparam>
        /// <returns>A resolved dependency.</returns>
        T Resolve<T>(object tag);
    }

    /// <summary>
    /// The abstraction to intercept a resolving of a dependency of the type T. Besides that, it could be used to create a custom <see cref="Lifetime"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal interface IFactory<T>
    {
        /// <summary>
        /// Provides an instance.
        /// </summary>
        /// <param name="factory">The method resolving an instance of dependency.</param>
        /// <param name="implementationType">The implementation type.</param>
        /// <param name="tag">The dependency tag.</param>
        /// <typeparam name="T">The dependency type.</typeparam>
        /// <returns>A resolved instance.</returns>
        T Create(Func<T> factory, Type implementationType, object tag);
    }

    /// <summary>
    /// The abstraction to intercept a resolving of a dependency of types defined by <see cref="IncludeAttribute"/> and <see cref="ExcludeAttribute"/>. Besides that, it could be used to create a custom <see cref="Lifetime"/>.
    /// </summary>
    internal interface IFactory
    {
        /// <summary>
        /// Intercepts a resolving of a dependency of the type T using a method <paramref name="factory"/> for a <paramref name="tag"/>.
        /// </summary>
        /// <param name="factory">The method resolving an instance of dependency.</param>
        /// <param name="implementationType">The implementation type.</param>
        /// <param name="tag">The dependency tag.</param>
        /// <typeparam name="T">The dependency type.</typeparam>
        /// <returns>A resolved instance.</returns>
        T Create<T>(Func<T> factory, Type implementationType, object tag);
    }

    /// <summary>
    /// Represents an event rising after an instance of the <see cref="IDisposable"/> type is created during a DI composition. Use this event to manage <see cref="IDisposable"/> instances.
    /// <example>
    /// <code>
    /// Composer.OnDisposable += e => disposables.Add(e.Disposable);
    /// </code>
    /// </example>.
    /// </summary>
    internal struct RegisterDisposableEvent
    {
        /// <summary>
        /// The <see cref="IDisposable"/> instance.
        /// </summary>
        public readonly IDisposable Disposable;

        /// <summary>
        /// A binding <see cref="Lifetime"/> relating to a disposable instance.
        /// </summary>
        public readonly Lifetime Lifetime;

        /// <summary>
        /// Creates an event instance. 
        /// </summary>
        /// <param name="disposable">The disposable instance.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> relating to a disposable instance.</param>
        public RegisterDisposableEvent(IDisposable disposable, Lifetime lifetime)
        {
            Disposable = disposable;
            Lifetime = lifetime;
        }
    }

    /// <summary>
    /// Represents a delegate of the <see cref="RegisterDisposableEvent"/> event rising after an instance of the <see cref="IDisposable"/> type is created during a DI composition.
    /// </summary>
    internal delegate void RegisterDisposable(RegisterDisposableEvent registerDisposableEvent);
}
#pragma warning restore 0436
#pragma warning restore 8625