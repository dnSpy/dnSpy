// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// Allows <see cref="VisualLineElementGenerator"/>s, <see cref="IVisualLineTransformer"/>s and
	/// <see cref="IBackgroundRenderer"/>s to be notified when they are added or removed from a text view.
	/// </summary>
	public interface ITextViewConnect
	{
		/// <summary>
		/// Called when added to a text view.
		/// </summary>
		void AddToTextView(TextView textView);
		
		/// <summary>
		/// Called when removed from a text view.
		/// </summary>
		void RemoveFromTextView(TextView textView);
	}
}
