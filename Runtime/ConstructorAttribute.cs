namespace AncientLightStudios.Nanoject
{
    using System;
    using JetBrains.Annotations;

    /// <summary>
    /// Marker attribute that can be used to mark the constructor that should be used to construct the component. Only
    /// required if you have more than one constructor.
    /// </summary>
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Constructor)]
    public class ConstructorAttribute : Attribute
    {
    }
}
