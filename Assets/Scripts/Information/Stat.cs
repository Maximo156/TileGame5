using System;

[AttributeUsage(AttributeTargets.Field)]
public class Stat : Attribute
{
    public readonly string ValueOverride;
    public readonly string Name;
    public readonly int defaultValue;

    public Stat(string Name, string ValueOverride = null, int defaultValue = 0)
    {
        this.Name = Name;
        this.ValueOverride = ValueOverride;
        this.defaultValue = defaultValue;
    }
}
