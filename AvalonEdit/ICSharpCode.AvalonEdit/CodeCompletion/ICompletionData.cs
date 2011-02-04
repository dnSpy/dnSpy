// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace ICSharpCode.AvalonEdit.CodeCompletion
{
	/// <summary>
	/// Describes an entry in the <see cref="CompletionList"/>.
	/// </summary>
	public interface ICompletionData
	{
		/// <summary>
		/// Gets the image.
		/// </summary>
		ImageSource Image { get; }
		
		/// <summary>
		/// Gets the text. This property is used to filter the list of visible elements.
		/// </summary>
		string Text { get; }
		
		/// <summary>
		/// The displayed content. This can be the same as 'Text', or a WPF UIElement if
		/// you want to display rich content.
		/// </summary>
		object Content { get; }
		
		/// <summary>
		/// Gets the description.
		/// </summary>
		object Description { get; }
		
		/// <summary>
		/// Gets the priority. This property is used in the selection logic. You can use it to prefer selecting those items
		/// which the user is accessing most frequently.
		/// </summary>
		double Priority { get; }
		
		/// <summary>
		/// Perform the completion.
		/// </summary>
		/// <param name="textArea">The text area on which completion is performed.</param>
		/// <param name="completionSegment">The text segment that was used by the completion window if
		/// the user types (segment between CompletionWindow.StartOffset and CompletionWindow.EndOffset).</param>
		/// <param name="insertionRequestEventArgs">The EventArgs used for the insertion request.
		/// These can be TextCompositionEventArgs, KeyEventArgs, MouseEventArgs, depending on how
		/// the insertion was triggered.</param>
		void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs);
	}
}
