namespace AncientLightStudios.Nanoject
{
    using System;
    using JetBrains.Annotations;

    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Class)]
    public class DependencyComponentAttribute : Attribute
    {
    }
}
