// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.AvalonEdit.Highlighting.Xshd
{
	/// <summary>
	/// An element in a XSHD rule set.
	/// </summary>
	[Serializable]
	public abstract class XshdElement
	{
		/// <summary>
		/// Gets the line number in the .xshd file.
		/// </summary>
		public int LineNumber { get; set; }
		
		/// <summary>
		/// Gets the column number in the .xshd file.
		/// </summary>
		public int ColumnNumber { get; set; }
		
		/// <summary>
		/// Applies the visitor to this element.
		/// </summary>
		public abstract object AcceptVisitor(IXshdVisitor visitor);
	}
}
