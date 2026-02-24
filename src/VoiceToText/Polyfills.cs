#if !NET5_0_OR_GREATER
// Polyfills for C# features used with netstandard2.1 target.
// These types are built into .NET 5+ but must be defined manually for older targets.

// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices
{
    using System.ComponentModel;

    /// <summary>Allows 'init' accessors in netstandard2.1.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit;

    /// <summary>Allows 'required' keyword in netstandard2.1.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    internal sealed class RequiredMemberAttribute : Attribute;

    /// <summary>Allows 'required' keyword in netstandard2.1.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    internal sealed class CompilerFeatureRequiredAttribute : Attribute
    {
        public CompilerFeatureRequiredAttribute(string featureName) => FeatureName = featureName;
        public string FeatureName { get; }
        public bool IsOptional { get; init; }
    }
}
#endif
