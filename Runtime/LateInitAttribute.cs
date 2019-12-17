namespace AncientLightStudios.Nanoject
{
    using System;
    using JetBrains.Annotations;

    /// <summary>
    /// 
    /// </summary>
    [MeansImplicitUse]
    [AttributeUsage(validOn:AttributeTargets.Method)]
    public class LateInitAttribute : Attribute
    {
    }
}
