using System;

namespace Wren.Attributes
{
    /// <summary>
    /// Mark constructors and methods that shall be ignored when binding a C# class derived from
    /// <see cref="ForeignObject"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class WrenIgnoreAttribute : Attribute
    {
    }
}
