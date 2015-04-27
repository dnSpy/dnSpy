// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;

namespace ICSharpCode.NRefactory.Editor
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
		/// <remarks>Anchor movement is ambiguous if text is inserted exactly at the anchor's location.
		/// Does the anchor stay before the inserted text, or does it move after it?
		/// The property <see cref="MovementType"/> will be used to determine which of these two options the anchor will choose.
		/// The default value is <see cref="AnchorMovementType.Default"/>.</remarks>
		AnchorMovementType MovementType { get; set; }
		
		/// <summary>
		/// <para>
		/// Specifies whether the anchor survives deletion of the text containing it.
		/// </para><para>
		/// <c>false</c>: The anchor is deleted when the a selection that includes the anchor is deleted.
		/// <c>true</c>: The anchor is not deleted.
		/// </para>
		/// </summary>
		/// <remarks><inheritdoc cref="IsDeleted" /></remarks>
		bool SurviveDeletion { get; set; }
		
		/// <summary>
		/// Gets whether the anchor was deleted.
		/// </summary>
		/// <remarks>
		/// <para>When a piece of text containing an anchor is removed, then that anchor will be deleted.
		/// First, the <see cref="IsDeleted"/> property is set to true on all deleted anchors,
		/// then the <see cref="Deleted"/> events are raised.
		/// You cannot retrieve the offset from an anchor that has been deleted.</para>
		/// <para>This deletion behavior might be useful when using anchors for building a bookmark feature,
		/// but in other cases you want to still be able to use the anchor. For those cases, set <c><see cref="SurviveDeletion"/> = true</c>.</para>
		/// </remarks>
		bool IsDeleted { get; }
		
		/// <summary>
		/// Occurs after the anchor was deleted.
		/// </summary>
		/// <remarks>
		/// <inheritdoc cref="IsDeleted" />
		/// <para>Due to the 'weak reference' nature of text anchors, you will receive
		/// the Deleted event only while your code holds a reference to the TextAnchor object.
		/// </para>
		/// </remarks>
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
		/// determines where the caret moves to. For normal insertions, the anchor will move
		/// after the inserted text.
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
