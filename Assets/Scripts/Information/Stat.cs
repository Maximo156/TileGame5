using System;

[AttributeUsage(AttributeTargets.Field)]
public class Stat : Attribute
{
    public readonly string ValueOverride;
    public readonly string Name;

    public Stat(string Name, string ValueOverride = null)
    {
        this.Name = Name;
        this.ValueOverride = ValueOverride;
    }
}
