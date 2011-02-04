// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// Contains information relevant for text run creation.
	/// </summary>
	public interface ITextRunConstructionContext
	{
		/// <summary>
		/// Gets the text document.
		/// </summary>
		TextDocument Document { get; }
		
		/// <summary>
		/// Gets the text view for which the construction runs.
		/// </summary>
		TextView TextView { get; }
		
		/// <summary>
		/// Gets the visual line that is currently being constructed.
		/// </summary>
		VisualLine VisualLine { get; }
		
		/// <summary>
		/// Gets the global text run properties.
		/// </summary>
		TextRunProperties GlobalTextRunProperties { get; }
	}
}
