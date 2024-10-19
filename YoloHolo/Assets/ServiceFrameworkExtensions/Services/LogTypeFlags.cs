using System;

namespace ServiceFrameworkExtensions.Services
{
    [Flags]
    public enum LogTypeFlags
    {
        Error     = 1,
        Assert    = 1 << 1,
        Warning   = 1 << 2,
        Log       = 1 << 3,
        Exception = 1 << 4
    }
}