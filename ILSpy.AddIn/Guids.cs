// Guids.cs
// MUST match guids.h
using System;

namespace ICSharpCode.ILSpy.AddIn
{
	static class GuidList
	{
		public const string guidILSpyAddInPkgString = "a9120dbe-164a-4891-842f-fb7829273838";
		public const string guidILSpyAddInCmdSetString = "85ddb8ca-a842-4b1c-ba1a-94141fdf19d0";

		public static readonly Guid guidILSpyAddInCmdSet = new Guid(guidILSpyAddInCmdSetString);
	};
}