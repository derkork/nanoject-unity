namespace AncientLightStudios.Nanoject
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using JetBrains.Annotations;
    using UnityEngine;

    public sealed class DependencyContext
    {
        private readonly HolderByTypeAndQualifier<object> _resolvedComponents =
            new HolderByTypeAndQualifier<object>();

        private readonly HolderByTypeAndQualifier<DependencySet> _collections =
            new HolderByTypeAndQualifier<DependencySet>();

        private readonly HolderByTypeAndQualifier<UnresolvedComponent> _unresolvedComponents =
            new HolderByTypeAndQualifier<UnresolvedComponent>();

        public bool IsResolved => _unresolvedComponents.Count == 0;

        public DependencyContext()
        {
            Declare(this);
        }

        public void DeclareQualified<T>(string qualifier, T instance = default)
        {
            DeclareInternal(typeof(T), instance, qualifier);
        }

        public void Declare<T>(T instance = default)
        {
            DeclareInternal(typeof(T), instance);
        }


        public void DeclareQualified(Type type, string qualifier, object instance = default)
        {
            DeclareInternal(type, instance, qualifier);
        }

        public void Declare(Type type, object instance = default)
        {
            DeclareInternal(type, instance);
        }


        private void DeclareInternal(Type type, object instance = default, string qualifier = "")
        {
            var unresolvedDependency =
                instance == default ? new UnresolvedComponent(type) : new UnresolvedComponent(instance);
            _unresolvedComponents.Put(type, qualifier, unresolvedDependency);
        }

        public T Get<T>(string qualifier = "")
        {
            if (!IsResolved)
            {
                throw new InvalidOperationException("This dependency context is not resolved.");
            }

            if (_resolvedComponents.TryGetUniqueValue(typeof(T), qualifier, out var result))
            {
                return (T) result;
            }

            throw new InvalidOperationException(
                $"No unique dependency of type {typeof(T)} with qualifier '{qualifier}'.");
        }

        public List<T> GetAll<T>()
        {
            return _resolvedComponents.Where((type, qualifier) => typeof(T).IsAssignableFrom(type)).Cast<T>().ToList();
        }


        public void Resolve()
        {
            bool ResolveUnique(Type type, string qualifier, out object result)
            {
                if (_resolvedComponents
                    .Where((itsType, itsQualifier) => type.IsAssignableFrom(itsType) && itsQualifier == qualifier)
                    .UniqueMatch(out result))
                {
                    return true;
                }

                if (Utils.IsCollectionType(type, out var collectionContentType))
                {
                    // first check if there are any unresolved components of the given collection content type
                    // or an assignable type. we need to do this to make sure we don't inject a half-finished collection
                    // into a newly created dependency. We can only create collections after all components that
                    // make up the collection are resolved.
                    if (!_unresolvedComponents.Where((itsType, _) => collectionContentType.IsAssignableFrom(itsType))
                        .Any())
                    {
                        // if we already built a collection for this type and qualifier, use this.
                        if (!_collections.Where((itsType, itsQualifier) =>
                            collectionContentType == itsType && qualifier == itsQualifier).UniqueMatch(out var set))
                        {
                            // otherwise build a new one
                            set = Utils.MakeDependencySet(collectionContentType);
                            _collections.Put(type, qualifier, set);
                        }

                        // make sure the set contains all known matching components. Since it is a set, it doesn't
                        // matter if we add a component more than once.
                        foreach (var item in _resolvedComponents.Where((itsType, itsQualifier) =>
                            collectionContentType.IsAssignableFrom(itsType)
                            && (qualifier == "" || qualifier == itsQualifier))
                        )
                        {
                            set.Add(item);
                        }

                        result = set;
                        return true;
                    }
                }

                result = default;
                return false;
            }

            bool hasProgress;
            do
            {
                hasProgress = false;

                // try a round of resolving stuff.
                _unresolvedComponents.ForEach((unresolvedType, unresolvedQualifier, unresolvedComponent) =>
                {
                    if (!unresolvedComponent.TryResolve(ResolveUnique, out var instance))
                    {
                        return;
                    }

                    // we have resolved it, so we can add it to the known dependencies
                    _resolvedComponents.Put(unresolvedType, unresolvedQualifier, instance);

                    // and remove it from the unresolved dependencies
                    _unresolvedComponents.Remove(unresolvedType, unresolvedQualifier, unresolvedComponent);
                    hasProgress = true;
                });
            } while (hasProgress);

            if (_unresolvedComponents.Count <= 0)
            {
                return;
            }

            _unresolvedComponents.ForEach((itsType, itsQualifier, item) =>
            {
                var actualQualifier = itsQualifier == "" ? "<no qualifier>" : itsQualifier;
                Debug.Log($"[{actualQualifier}] {itsType.FullName}\n{item}");
            });

            throw new InvalidOperationException("There are unresolved dependencies left!");
        }
    }

    internal abstract class DependencySet
    {
        public abstract void Add(object toAdd);
    }

    internal class TypedDependencySet<T> : DependencySet, IReadOnlyCollection<T>
    {
        private readonly HashSet<T> _set = new HashSet<T>();

        public override void Add(object toAdd)
        {
            _set.Add((T) toAdd);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _set.GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _set.GetEnumerator();
        }

        public int Count => _set.Count;
    }

    /// <summary>
    /// Various helper functions that don't fit anywhere else.
    /// </summary>
    internal static class Utils
    {
        /// <summary>
        /// The supported collection type.
        /// </summary>
        private static readonly Type CollectionType = typeof(IReadOnlyCollection<>);

        /// <summary>
        /// An empty list, so we don't need to create unnecessary instances.
        /// </summary>
        public static readonly ICollection<string> EmptyList = new string[0];

        /// <summary>
        /// Checks if the given type is a supported collection type for injection. Supported is any <see cref="IReadOnlyCollection{T}"/>.
        /// </summary>
        /// <param name="type">the type argument</param>
        /// <param name="collectionContentType">the content type of the collection (e.g. what is in the collection).</param>
        /// <returns></returns>
        public static bool IsCollectionType(Type type, out Type collectionContentType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == CollectionType)
            {
                collectionContentType = type.GetGenericArguments()[0];
                return true;
            }

            collectionContentType = default;
            return false;
        }

        /// <summary>
        /// Create a set to hold dependencies of the given type.
        /// </summary>
        /// <param name="t">The type that the set should hold.</param>
        /// <returns>a DependencySet that can hold dependencies of the given type.</returns>
        public static DependencySet MakeDependencySet(Type t)
        {
            return (DependencySet) Activator.CreateInstance(typeof(TypedDependencySet<>).MakeGenericType(t));
        }

        /// <summary>
        /// Returns true if the given enumerable only contains one element, false otherwise. Returns the single element
        /// inside the enumerable.
        /// </summary>
        public static bool UniqueMatch<T>(this IEnumerable<T> input, out T result)
        {
            var list = input.ToList();
            if (list.Count != 1)
            {
                result = default;
                return false;
            }

            result = list[0];
            return true;
        }
    }


    /// <summary>
    /// Helper class for our nested dictionary structures to avoid some typing.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class HolderByTypeAndQualifier<T>
    {
        private readonly Dictionary<Type, Dictionary<string, HashSet<T>>> _value =
            new Dictionary<Type, Dictionary<string, HashSet<T>>>();

        public int Count => _value.Count;

        public IEnumerable<T> Where(Func<Type, string, bool> filter)
        {
            return _value.SelectMany(outer => outer.Value, (outer, inner) => new {outer, inner})
                .Where(t => filter(t.outer.Key, t.inner.Key))
                .SelectMany(t => t.inner.Value);
        }

        public void ForEach(Action<Type, string, T> handler)
        {
            foreach (var outer in _value.ToList())
            {
                foreach (var inner in outer.Value.ToList())
                {
                    foreach (var item in inner.Value.ToList())
                    {
                        handler(outer.Key, inner.Key, item);
                    }
                }
            }
        }

        public void Put(Type type, string qualifier, T value)
        {
            if (!_value.TryGetValue(type, out var byQualifier))
            {
                byQualifier = new Dictionary<string, HashSet<T>>();
                _value[type] = byQualifier;
            }

            if (!byQualifier.TryGetValue(qualifier, out var set))
            {
                set = new HashSet<T>();
                byQualifier[qualifier] = set;
            }

            set.Add(value);
        }

        public bool TryGetValue(Type type, string qualifier, out HashSet<T> result)
        {
            if (_value.TryGetValue(type, out var byQualifier))
            {
                return byQualifier.TryGetValue(qualifier, out result);
            }

            result = default;
            return false;
        }

        public bool TryGetUniqueValue(Type type, string qualifier, out T result)
        {
            if (TryGetValue(type, qualifier, out var set))
            {
                if (set.Count == 1)
                {
                    result = set.First();
                    return true;
                }
            }

            result = default;
            return false;
        }

        public void Remove(Type type, string qualifier, T value)
        {
            if (!_value.TryGetValue(type, out var byQualifier))
            {
                return;
            }

            if (!byQualifier.TryGetValue(qualifier, out var set))
            {
                return;
            }

            set.Remove(value);

            if (set.Count == 0)
            {
                byQualifier.Remove(qualifier);
            }

            if (byQualifier.Count == 0)
            {
                _value.Remove(type);
            }
        }
    }

    /// <summary>
    /// Class representing a component that still needs to be resolved. Using this class allows to cache introspection
    /// results and do a gradual resolve while saving intermediate resolution steps. This makes the whole process faster
    /// and also allows for better debugging.
    /// </summary>
    internal class UnresolvedComponent
    {
        public delegate bool UniqueResolver(Type type, string qualifier, out object result);

        /// <summary>
        /// The existing instance of the dependency. Only set for objects with [LateInit] initializers.
        /// </summary>
        [CanBeNull] private readonly object _existingInstance;

        /// <summary>
        /// The initializer that should be used to initialize the instance (either a constructor or a [LateInit] method).
        /// For dependencies with existing instances this can be null if the dependency has no [LateInit] method.
        /// </summary>
        [CanBeNull] private readonly MethodBase _initializer;

        /// <summary>
        /// The types of all parameters to be resolved.
        /// </summary>
        private readonly Type[] _types;

        /// <summary>
        /// The qualifiers of all parameters to be resolved.
        /// </summary>
        private readonly string[] _qualifiers;

        /// <summary>
        /// All currently resolved parameters.
        /// </summary>
        private readonly object[] _resolvedDependencies;

        /// <summary>
        /// Flags indicating if a certain parameter is already resolved.
        /// </summary>
        private readonly bool[] _isResolved;

        /// <summary>
        /// Safety flag to avoid resolving this component more than once.
        /// </summary>
        private bool _wasResolved;


        /// <summary>
        /// Constructor for [LateInit] components.
        /// </summary>
        /// <param name="existingInstance">the existing instance</param>
        public UnresolvedComponent(object existingInstance)
        {
            var methods = existingInstance
                .GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(it => it.GetCustomAttribute(typeof(LateInitAttribute)) != null)
                .ToArray();

            if (methods.Length > 1)
            {
                throw new ArgumentException(
                    $"Type {existingInstance.GetType().Name} has more than one [LateInit] annotated method. Only one method can be annotated.");
            }

            _existingInstance = existingInstance;
            if (methods.Length == 0)
            {
                _initializer = null;
                _types = new Type[0];
                _qualifiers = new string[0];
                _resolvedDependencies = new object[0];
                _isResolved = new bool[0];
                return;
            }

            _initializer = methods[0];
            ReadMethodInfo(_initializer, out _types, out _qualifiers, out _resolvedDependencies, out _isResolved);
        }

        /// <summary>
        /// Constructor for regular components.
        /// </summary>
        /// <param name="type">the type of the component</param>
        public UnresolvedComponent(Type type)
        {
            var constructors = type.GetConstructors();

            if (constructors.Length == 0)
            {
                throw new ArgumentException(
                    $"Type {type.Name} has no public constructor. Please create a public constructor to be used for initialization.");
            }

            if (constructors.Length > 1)
            {
                constructors = constructors
                    .Where(it => it.GetCustomAttribute(typeof(ConstructorAttribute)) != null)
                    .ToArray();
                if (constructors.Length > 1)
                {
                    throw new ArgumentException(
                        $"Type {type.Name} has more than one constructor annotated with [Constructor]. Only one constructor can be annotated.");
                }

                if (constructors.Length == 0)
                {
                    throw new ArgumentException(
                        $"Type {type.Name} has more than one constructor but none is annotated with [Constructor]. Please annotate exactly one with [Constructor].");
                }
            }


            _initializer = constructors[0];
            ReadMethodInfo(_initializer, out _types, out _qualifiers, out _resolvedDependencies, out _isResolved);
        }

        /// <summary>
        /// Helper function which reads information about a method through C# reflection and puts the results into
        /// the given data structures.
        /// </summary>
        /// <param name="method">The method instance to read.</param>
        /// <param name="types">Array containing the types of the parameters of the method.</param>
        /// <param name="qualifiers">Array containing the qualifier of each parameter of the method.</param>
        /// <param name="resolvedDependencies">Array with an empty space for each parameter of the method.
        /// This is later filled when dependency resolution takes place.</param>
        /// <param name="isResolved">Array with markers indicating if a parameter is already resolved. This will contain
        /// one space for each parameter, all initialized to <c>false</c></param>
        private static void ReadMethodInfo(MethodBase method,
            out Type[] types,
            out string[] qualifiers,
            out object[] resolvedDependencies,
            out bool[] isResolved)
        {
            var parameters = method.GetParameters();
            var numberOfParameters = parameters.Length;

            resolvedDependencies = new object[numberOfParameters];
            types = new Type[numberOfParameters];
            qualifiers = new string[numberOfParameters];
            isResolved = new bool[numberOfParameters];

            for (var index = 0; index < numberOfParameters; index++)
            {
                var parameter = parameters[index];
                var qualifierAttribute = parameter.GetCustomAttribute<QualifierAttribute>();
                var qualifier = "";
                if (qualifierAttribute != null)
                {
                    qualifier = qualifierAttribute.Name;
                }

                qualifiers[index] = qualifier;
                types[index] = parameter.ParameterType;
                isResolved[index] = false;
            }
        }

        /// <summary>
        /// Tries to resolve this component using the given resolver method.
        /// </summary>
        /// <param name="resolver">The resolver method to use.</param>
        /// <param name="result">the resolved object. Only filled if resolution was successful.</param>
        /// <returns><c>true</c> if the resolution was successful, <c>false</c> otherwise</returns>
        /// <exception cref="InvalidOperationException">if this method is called after this component already has been
        /// resolved.</exception>
        public bool TryResolve(UniqueResolver resolver, out object result)
        {
            if (_wasResolved)
            {
                throw new InvalidOperationException(
                    "This component has already been resolved. This is almost certainly a bug.");
            }

            if (_initializer == null)
            {
                // we have an existing object and no [LateInit] method, so this is completely resolved.
                result = _existingInstance;
                _wasResolved = true;
                return true;
            }

            var hasUnresolvedValues = false;

            // walk over all dependencies and check if something can be resolved.
            for (var i = 0; i < _resolvedDependencies.Length; i++)
            {
                if (_isResolved[i])
                {
                    continue;
                }

                if (resolver(_types[i], _qualifiers[i], out var value))
                {
                    _resolvedDependencies[i] = value;
                    _isResolved[i] = true;
                    continue;
                }

                hasUnresolvedValues = true;
            }

            // if all dependencies could be resolved call the constructor or [LateInit] method.
            if (!hasUnresolvedValues)
            {
                if (_initializer is ConstructorInfo constructorInfo)
                {
                    result = constructorInfo.Invoke(_resolvedDependencies);
                }
                else
                {
                    _initializer.Invoke(_existingInstance, _resolvedDependencies);
                    result = _existingInstance;
                }

                _wasResolved = true;
                return true;
            }

            result = default;
            return false;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("Initializer: ");
            sb.Append(_initializer);
            sb.Append("\nUnresolved parameters (no match or no unique match):\n");

            for (var i = 0; i < _isResolved.Length; i++)
            {
                if (_isResolved[i])
                {
                    continue;
                }

                sb.Append(" - Parameter ")
                    .Append(i)
                    .Append(": ")
                    .Append(_types[i].Name);
                if (_qualifiers[i].Length > 0)
                {
                    sb.Append(" (Qualifier: ")
                        .Append(_qualifiers[i])
                        .Append(")");
                }
            }

            return sb.ToString();
        }
    }
}
