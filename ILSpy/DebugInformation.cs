// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using dnlib.DotNet;
using dnSpy.Debugger;

namespace ICSharpCode.ILSpy.Debugger {
	/// <summary>
	/// Contains the data important for debugger from the main application.
	/// </summary>
	public static class DebugInformation
	{
		/// <summary>
		/// Gets or sets the current method key, IL offset and member reference. Used for step in/out.
		/// </summary>
		public static Tuple<MethodKey, int, IMemberRef> DebugStepInformation { get; set; }

		/// <summary>
		/// true if we must call JumpToReference() due to new stack frame
		/// </summary>
		public static bool MustJumpToReference { get; set; }
	}
}
