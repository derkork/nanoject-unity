namespace AncientLightStudios.Nanoject
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public delegate object Factory(Type type, object[] constructorArguments);


    public sealed class DependencyContext
    {
        private readonly Dictionary<Type, Dictionary<string, object>> _dependencies =
            new Dictionary<Type, Dictionary<string, object>>();

        private readonly Dictionary<Type, Dictionary<string, object>> _unresolvedDependencies =
            new Dictionary<Type, Dictionary<string, object>>();

        private readonly Dictionary<Type, Factory> _factories = new Dictionary<Type, Factory>();

        public DependencyContext()
        {
            Declare(this);
        }


        public void DeclareFactory<T>(Factory factory)
        {
            _factories[typeof(T)] = factory;
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
            if (!_unresolvedDependencies.TryGetValue(type, out var values))
            {
                values = new Dictionary<string, object>();
                _unresolvedDependencies[type] = values;
            }

            values[qualifier] = instance;
        }


        public T Get<T>(string qualifier = "")
        {
            if (_unresolvedDependencies.Count > 0)
            {
                throw new InvalidOperationException("This dependency context is not resolved.");
            }

            var type = typeof(T);
            if (_dependencies.TryGetValue(type, out var dependenciesByQualifier))
            {
                if (dependenciesByQualifier.TryGetValue(qualifier, out var result))
                {
                    return (T) result;
                }
            }

            throw new InvalidOperationException($"No dependency of type {type} with qualifier '{qualifier}'.");
        }

        public List<T> GetAll<T>()
        {
            var result = new List<T>();
            var resultType = typeof(T);

            var types = new List<Type>(_dependencies.Keys);
            foreach (var type in types)
            {
                if (resultType.IsAssignableFrom(type))
                {
                    foreach (var value in _dependencies[type].Values)
                    {
                        result.Add((T) value);
                    }
                }
            }

            return result;
        }

        public void Resolve()
        {
            bool hasProgress;

            do
            {
                hasProgress = false;

                var unresolvedTypes = new List<Type>(_unresolvedDependencies.Keys);
                foreach (var unresolvedType in unresolvedTypes)
                {
                    var unresolvedByQualifier = _unresolvedDependencies[unresolvedType];
                    var unresolvedQualifiers = new List<string>(unresolvedByQualifier.Keys);

                    foreach (var unresolvedQualifier in unresolvedQualifiers)
                    {
                        // if this object has been given in advance, so we just use it
                        var instance = unresolvedByQualifier[unresolvedQualifier] ?? TryConstruct(unresolvedType);

                        if (instance == default)
                        {
                            continue; // we cannot resolve it right now, so skip it for next round
                        }

                        // try to resolve late init method, if it exists
                        instance = TryResolveLateInitializer(unresolvedType, instance);
                        
                        if (instance == default)
                        {
                            continue; // we cannot resolve it right now, so skip it for next round
                        }
                        
                        // we have resolved it, so we can add it to the known dependencies
                        if (!_dependencies.TryGetValue(unresolvedType, out var values))
                        {
                            values = new Dictionary<string, object>();
                            _dependencies[unresolvedType] = values;
                        }
                        values[unresolvedQualifier] = instance;

                        hasProgress = true;

                        // remove it from the nested qualifiers dictionary
                        unresolvedByQualifier.Remove(unresolvedQualifier);

                        // and remove the whole qualifiers dictionary if it is empty.
                        if (unresolvedByQualifier.Count == 0)
                        {
                            _unresolvedDependencies.Remove(unresolvedType);
                        }
                    }
                }
            } while (hasProgress);

            if (_unresolvedDependencies.Count <= 0)
            {
                return;
            }
            
            var unresolved = "";
            foreach (var keyValuePair in _unresolvedDependencies)
            {
                unresolved +=  $"Unresolved dependency of type {keyValuePair.Key.FullName}\n";
            }

            throw new InvalidOperationException($"There are unresolved dependencies left!: \n{unresolved}");
        }

        private object TryResolveLateInitializer(Type type, object instance)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(it => it.GetCustomAttribute(typeof(LateInitAttribute)) != null)
                .ToArray();

            if (methods.Length == 0)
            {
                // no more missing dependencies,
                return instance;
            }
            
            foreach (var method in methods)
            {
                if (!ResolveMethodDependencies(method, out var resolvedDependencies))
                {
                    continue;
                }
                
                // we have a resolved initializer
                method.Invoke(instance, resolvedDependencies);
                return instance;
            }

            return default;
        }

        private object TryConstruct(Type type)
        {
            // only public constructors are used for dependency injection
            var constructors = type.GetConstructors();

            foreach (var constructor in constructors)
            {
                if (!ResolveMethodDependencies(constructor, out var resolvedDependencies))
                {
                    continue;
                }

                // all dependencies of this constructor have been resolved,
                // ReSharper disable once ConvertIfStatementToReturnStatement
                var factoryType = FindSuperType(_factories.Keys, type);

                if (factoryType != null)
                {
                    return _factories[factoryType](type, resolvedDependencies);
                }

                // no factory, then just invoke the constructor
                return constructor.Invoke(resolvedDependencies);
            }

            // no constructor could be resolved, so we give up.
            return null;
        }

        private bool ResolveMethodDependencies(MethodBase method, out object[] dependencies)
        {
            var parameters = method.GetParameters();
            var resolvedDependencies = new object[parameters.Length];
            for (var index = 0; index < parameters.Length; index++)
            {
                var parameter = parameters[index];
                var qualifierAttribute = parameter.GetCustomAttribute<QualifierAttribute>();
                var qualifier = "";
                if (qualifierAttribute != null)
                {
                    qualifier = qualifierAttribute.Name;
                }

                var parameterType = parameter.ParameterType;
                if (_dependencies.TryGetValue(parameterType, out var dependenciesByQualifier))
                {
                    if (dependenciesByQualifier.TryGetValue(qualifier, out var value))
                    {
                        resolvedDependencies[index] = value;
                        continue;
                    }
                }

                // if we have no matching dependency, we have to give up.
                dependencies = default;
                return false;
            }

            dependencies = resolvedDependencies;
            return true;
        }

        private static Type FindSuperType(IEnumerable<Type> searchSpace, Type toFind)
        {
            var matches = new List<Type>();

            foreach (var type in searchSpace)
            {
                if (type == toFind)
                {
                    return toFind;
                }

                if (type.IsAssignableFrom(toFind))
                {
                    matches.Add(type);
                }
            }

            return matches.FirstOrDefault();
        }
    }
}