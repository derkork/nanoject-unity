namespace AncientLightStudios.Nanoject
{
    using System;

    public class QualifierAttribute : Attribute
    {
        public string Name { get; }

        public QualifierAttribute(string name)
        {
            Name = name;
        }
        
    }
}