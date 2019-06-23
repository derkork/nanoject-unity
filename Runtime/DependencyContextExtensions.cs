namespace AncientLightStudios.Nanoject
{
    using System;
    using System.Linq;
    using System.Reflection;

    public static class DependencyContextExtensions
    {
        public static void ScanForComponents(this DependencyContext context, Predicate<Type> typeFilter = null)
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