// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.Editor
{
	/// <summary>
	/// The TextAnchor class references an offset (a position between two characters).
	/// It automatically updates the offset when text is inserted/removed in front of the anchor.
	/// </summary>
	/// <remarks>
	/// <para>Use the <see cref="ITextAnchor.Offset"/> property to get the offset from a text anchor.
	/// Use the <see cref="IDocument.CreateAnchor"/> method to create an anchor from an offset.
	/// </para>
	/// <para>
	/// The document will automatically update all text anchors; and because it uses weak references to do so,
	/// the garbage collector can simply collect the anchor object when you don't need it anymore.
	/// </para>
	/// <para>Moreover, the document is able to efficiently update a large number of anchors without having to look
	/// at each anchor object individually. Updating the offsets of all anchors usually only takes time logarithmic
	/// to the number of anchors. Retrieving the <see cref="ITextAnchor.Offset"/> property also runs in O(lg N).</para>
	/// </remarks>
	/// <example>
	/// Usage:
	/// <code>TextAnchor anchor = document.CreateAnchor(offset);
	/// ChangeMyDocument();
	/// int newOffset = anchor.Offset;
	/// </code>
	/// </example>
	public interface ITextAnchor
	{
		/// <summary>
		/// Gets the text location of this anchor.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown when trying to get the Offset from a deleted anchor.</exception>
		TextLocation Location { get; }
		
		/// <summary>
		/// Gets the offset of the text anchor.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown when trying to get the Offset from a deleted anchor.</exception>
		int Offset { get; }
		
		/// <summary>
		/// Controls how the anchor moves.
		/// </summary>
		AnchorMovementType MovementType { get; set; }
		
		/// <summary>
		/// Specifies whether the anchor survives deletion of the text containing it.
		/// <c>false</c>: The anchor is deleted when the a selection that includes the anchor is deleted.
		/// <c>true</c>: The anchor is not deleted.
		/// </summary>
		bool SurviveDeletion { get; set; }
		
		/// <summary>
		/// Gets whether the anchor was deleted.
		/// </summary>
		bool IsDeleted { get; }
		
		/// <summary>
		/// Occurs after the anchor was deleted.
		/// </summary>
		event EventHandler Deleted;
		
		/// <summary>
		/// Gets the line number of the anchor.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown when trying to get the Offset from a deleted anchor.</exception>
		int Line { get; }
		
		/// <summary>
		/// Gets the column number of this anchor.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown when trying to get the Offset from a deleted anchor.</exception>
		int Column { get; }
	}
	
	/// <summary>
	/// Defines how a text anchor moves.
	/// </summary>
	public enum AnchorMovementType
	{
		/// <summary>
		/// When text is inserted at the anchor position, the type of the insertion
		/// determines where the caret moves to. For normal insertions, the anchor will stay
		/// behind the inserted text.
		/// </summary>
		Default,
		/// <summary>
		/// Behaves like a start marker - when text is inserted at the anchor position, the anchor will stay
		/// before the inserted text.
		/// </summary>
		BeforeInsertion,
		/// <summary>
		/// Behave like an end marker - when text is insered at the anchor position, the anchor will move
		/// after the inserted text.
		/// </summary>
		AfterInsertion
	}
}
