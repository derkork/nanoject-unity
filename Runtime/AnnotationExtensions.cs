namespace AncientLightStudios.Nanoject
{
    using System;
    using System.Linq;
    using System.Reflection;

    public static class AnnotationExtensions
    {
        /// <summary>
        /// Scans all assemblies in the current domain for classes annotated with <see cref="DependencyComponentAttribute"/>
        /// and declares them in the dependency context. If the components have a <see cref="QualifierAttribute"/> they
        /// will be declared as qualified component using <see cref="DependencyContext.DeclareQualified(Type,string)"/>
        /// </summary>
        /// <param name="context">the context into which the classes should be declared.</param>
        /// <param name="typeFilter">an optional predicate for filtering the scanned classes.</param>
        public static void DeclareAnnotatedComponents(this DependencyContext context, Predicate<Type> typeFilter = null)
        {
            if (typeFilter == null)
            {
                typeFilter = it => true;
            }
            
            foreach (var type in AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(it => it.GetTypes())
                .Where(it => typeFilter(it) && it.GetCustomAttribute(typeof(DependencyComponentAttribute)) != null))
            {
                var qualifier = "";
                 var attribute = type.GetCustomAttribute<QualifierAttribute>();
                if (attribute != null)
                {
                    qualifier = attribute.Name;
                }
                    
                context.DeclareQualified(type, qualifier);
            }
        }
        
    }
}
