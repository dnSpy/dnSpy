//
// NullIStateMachineIndentEngine.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// An empty IStateMachineIndentEngine implementation that does nothing.
	/// </summary>
	public sealed class NullIStateMachineIndentEngine : IStateMachineIndentEngine
	{
		readonly ICSharpCode.NRefactory.Editor.IDocument document;
		int offset;

		public NullIStateMachineIndentEngine(ICSharpCode.NRefactory.Editor.IDocument document)
		{
			if (document == null)
				throw new ArgumentNullException("document");
			this.document = document;
		}

		#region IStateMachineIndentEngine implementation
		public IStateMachineIndentEngine Clone()
		{
			return new NullIStateMachineIndentEngine(document) { offset = this.offset };
		}

		bool IStateMachineIndentEngine.IsInsidePreprocessorDirective {
			get {
				return false;
			}
		}

		bool IStateMachineIndentEngine.IsInsidePreprocessorComment {
			get {
				return false;
			}
		}

		bool IStateMachineIndentEngine.IsInsideStringLiteral {
			get {
				return false;
			}
		}

		bool IStateMachineIndentEngine.IsInsideVerbatimString {
			get {
				return false;
			}
		}

		bool IStateMachineIndentEngine.IsInsideCharacter {
			get {
				return false;
			}
		}

		bool IStateMachineIndentEngine.IsInsideString {
			get {
				return false;
			}
		}

		bool IStateMachineIndentEngine.IsInsideLineComment {
			get {
				return false;
			}
		}

		bool IStateMachineIndentEngine.IsInsideMultiLineComment {
			get {
				return false;
			}
		}

		bool IStateMachineIndentEngine.IsInsideDocLineComment {
			get {
				return false;
			}
		}

		bool IStateMachineIndentEngine.IsInsideComment {
			get {
				return false;
			}
		}

		bool IStateMachineIndentEngine.IsInsideOrdinaryComment {
			get {
				return false;
			}
		}

		bool IStateMachineIndentEngine.IsInsideOrdinaryCommentOrString {
			get {
				return false;
			}
		}

		bool IStateMachineIndentEngine.LineBeganInsideVerbatimString {
			get {
				return false;
			}
		}

		bool IStateMachineIndentEngine.LineBeganInsideMultiLineComment {
			get {
				return false;
			}
		}
		#endregion

		#region IDocumentIndentEngine implementation
		void IDocumentIndentEngine.Push(char ch)
		{
			offset++;
		}

		void IDocumentIndentEngine.Reset()
		{
			this.offset = 0;
		}

		void IDocumentIndentEngine.Update(int offset)
		{
			this.offset = offset;
		}

		IDocumentIndentEngine IDocumentIndentEngine.Clone()
		{
			return Clone();
		}

		ICSharpCode.NRefactory.Editor.IDocument IDocumentIndentEngine.Document {
			get {
				return document;
			}
		}

		string IDocumentIndentEngine.ThisLineIndent {
			get {
				return "";
			}
		}

		string IDocumentIndentEngine.NextLineIndent {
			get {
				return "";
			}
		}

		string IDocumentIndentEngine.CurrentIndent {
			get {
				return "";
			}
		}

		bool IDocumentIndentEngine.NeedsReindent {
			get {
				return false;
			}
		}

		int IDocumentIndentEngine.Offset {
			get {
				return offset;
			}
		}
		TextLocation IDocumentIndentEngine.Location {
			get {
				return TextLocation.Empty;
			}
		}

		/// <inheritdoc />
		public bool EnableCustomIndentLevels
		{
			get { return false; }
			set { }
		}

		#endregion

		#region ICloneable implementation
		object ICloneable.Clone()
		{
			return Clone();
		}
		#endregion
	}
}

