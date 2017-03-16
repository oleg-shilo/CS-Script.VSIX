// Guids.cs
// MUST match guids.h
using System;

namespace OlegShilo.CSScript
{
    static class GuidList
    {
        public const string guidCSScriptPkgString = "7660d863-76ee-4d81-afab-a6510429a0fd";
        public const string guidCSScriptCmdSetString = "60ad10de-9844-423b-8d12-c5e357ab5e98";
        public const string guidToolWindowPersistanceString = "fd0da38f-47b9-4072-b27d-01629b580799";

        public static readonly Guid guidCSScriptCmdSet = new Guid(guidCSScriptCmdSetString);
    };
}