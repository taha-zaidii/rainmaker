using System;

namespace Digi.Shared.Filters
{
    /// <summary>
    /// Opt-out attribute for EnforceSelfScopeFilter.
    /// Use on endpoints where non-admin users are allowed to query other user/employee IDs.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class SkipSelfScopeAttribute : Attribute
    {
    }
}


