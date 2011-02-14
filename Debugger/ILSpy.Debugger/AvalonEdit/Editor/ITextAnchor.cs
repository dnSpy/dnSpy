// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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
using ICSharpCode.NRefactory.CSharp;

namespace ILSpy.Debugger.AvalonEdit.Editor
{
	/// <summary>
	/// Represents an anchored location inside an <see cref="IDocument"/>.
	/// </summary>
	public interface ITextAnchor
	{
		/// <summary>
		/// Gets the text location of this anchor.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown when trying to get the Offset from a deleted anchor.</exception>
		AstLocation Location { get; }
		
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
		Default = ICSharpCode.AvalonEdit.Document.AnchorMovementType.Default,
		/// <summary>
		/// Behaves like a start marker - when text is inserted at the anchor position, the anchor will stay
		/// before the inserted text.
		/// </summary>
		BeforeInsertion = ICSharpCode.AvalonEdit.Document.AnchorMovementType.BeforeInsertion,
		/// <summary>
		/// Behave like an end marker - when text is insered at the anchor position, the anchor will move
		/// after the inserted text.
		/// </summary>
		AfterInsertion = ICSharpCode.AvalonEdit.Document.AnchorMovementType.AfterInsertion
	}
}
