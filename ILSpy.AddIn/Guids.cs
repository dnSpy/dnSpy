// Guids.cs
// MUST match guids.h
using System;

namespace ICSharpCode.ILSpy.AddIn
{
	static class GuidList
	{
		public const string guidILSpyAddInPkgString = "E423C8E4-E730-47FE-B943-5D0E0E5C7CEB";
		public const string guidILSpyAddInCmdSetString = "116541AF-26D5-49C0-B5BD-9683B27A18AC";

		public static readonly Guid guidILSpyAddInCmdSet = new Guid(guidILSpyAddInCmdSetString);
	};
}