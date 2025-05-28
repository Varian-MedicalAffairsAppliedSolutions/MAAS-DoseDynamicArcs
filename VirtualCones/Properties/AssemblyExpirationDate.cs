using System;

[AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
internal class AssemblyExpirationDate : Attribute
{
    public string ExpirationDate { get; private set; }

    public AssemblyExpirationDate(string expirationDate)
    {
        ExpirationDate = expirationDate;
    }
} 