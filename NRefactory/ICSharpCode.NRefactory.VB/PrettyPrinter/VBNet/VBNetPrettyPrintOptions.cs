// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.VB.PrettyPrinter
{
	/// <summary>
	/// Description of VBNetPrettyPrintOptions.
	/// </summary>
	public class VBNetPrettyPrintOptions : AbstractPrettyPrintOptions
	{
		/// <summary>
		/// Gets/Sets if the optional "ByVal" modifier should be written.
		/// </summary>
		public bool OutputByValModifier { get; set; }
	}
}
