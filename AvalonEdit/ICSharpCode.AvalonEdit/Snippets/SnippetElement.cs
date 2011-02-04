// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Windows.Documents;

using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Snippets
{
	/// <summary>
	/// An element inside a snippet.
	/// </summary>
	[Serializable]
	public abstract class SnippetElement
	{
		/// <summary>
		/// Performs insertion of the snippet.
		/// </summary>
		public abstract void Insert(InsertionContext context);
		
		/// <summary>
		/// Converts the snippet to text, with replaceable fields in italic.
		/// </summary>
		public virtual Inline ToTextRun()
		{
			return null;
		}
	}
}
