//
// CSharpFormatter.cs
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
using ICSharpCode.NRefactory.Editor;
using System.Threading;
using System.Linq;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.TypeSystem;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// The formatting changes are used to format a specific region inside a document and apply a minimal formatting
	/// changeset to a given document. This is useful for a text editor environment.
	/// </summary>
	public class FormattingChanges
	{
		readonly IDocument document;
		readonly internal List<TextReplaceAction> changes = new List<TextReplaceAction> ();

		internal FormattingChanges (IDocument document)
		{
			if (document == null)
				throw new ArgumentNullException("document");
			this.document = document;
		}

		public int Count {
			get {
				return changes.Count;
			}
		}

		/// <summary>
		/// Applies the changes to the input document.
		/// </summary>
		public void ApplyChanges()
		{
			ApplyChanges(0, document.TextLength, document.Replace, (o, l, v) => document.GetText(o, l) == v);
		}

		public void ApplyChanges(int startOffset, int length)
		{
			ApplyChanges(startOffset, length, document.Replace, (o, l, v) => document.GetText(o, l) == v);
		}

		/// <summary>
		/// Applies the changes to the given Script instance.
		/// </summary>
		public void ApplyChanges(Script script)
		{
			ApplyChanges(0, document.TextLength, script.Replace);
		}

		public void ApplyChanges(int startOffset, int length, Script script)
		{
			ApplyChanges(startOffset, length, script.Replace);
		}

		public void ApplyChanges(int startOffset, int length, Action<int, int, string> documentReplace, Func<int, int, string, bool> filter = null)
		{
			int endOffset = startOffset + length;
			//			Console.WriteLine ("apply:"+ startOffset + "->" + endOffset);
			//			Console.WriteLine (document.Text.Substring (0, startOffset) + new string ('x',length) + document.Text.Substring (startOffset+ length));

			TextReplaceAction previousChange = null;
			int delta = 0;
			var depChanges = new List<TextReplaceAction> ();
			foreach (var change in changes.OrderBy(c => c.Offset)) {
				if (previousChange != null) {
					if (change.Equals(previousChange)) {
						// ignore duplicate changes
						continue;
					}
					if (change.Offset < previousChange.Offset + previousChange.RemovalLength) {
						throw new InvalidOperationException ("Detected overlapping changes " + change + "/" + previousChange);
					}
				}
				previousChange = change;
				bool skipChange = change.Offset + change.RemovalLength < startOffset || change.Offset > endOffset;
				skipChange |= filter != null && filter(change.Offset + delta, change.RemovalLength, change.NewText);
				skipChange &= !depChanges.Contains(change);
				if (!skipChange) {
					documentReplace(change.Offset + delta, change.RemovalLength, change.NewText);
					delta += change.NewText.Length - change.RemovalLength;
					if (change.DependsOn != null) {
						depChanges.Add(change.DependsOn);
					}
				}
			}
			changes.Clear();
		}

		internal TextReplaceAction AddChange(int offset, int removedChars, string insertedText)
		{
			if (offset < 0)
				throw new ArgumentOutOfRangeException("offset", "Should be >= 0");
			if (offset >= document.TextLength)
				throw new ArgumentOutOfRangeException("offset", "Should be < document.TextLength");
			if (removedChars < 0)
				throw new ArgumentOutOfRangeException("removedChars", "Should be >= 0");
			if (removedChars > offset + document.TextLength)
				throw new ArgumentOutOfRangeException("removedChars", "Tried to remove beyond end of text");
			if (removedChars == 0 && string.IsNullOrEmpty (insertedText))
				return null;
			var action = new TextReplaceAction (offset, removedChars, insertedText);
			changes.Add(action);
			return action;
		}
		
		internal sealed class TextReplaceAction
		{
			internal readonly int Offset;
			internal readonly int RemovalLength;
			internal readonly string NewText;
			internal TextReplaceAction DependsOn;

			public TextReplaceAction (int offset, int removalLength, string newText)
			{
				this.Offset = offset;
				this.RemovalLength = removalLength;
				this.NewText = newText ?? string.Empty;
			}

			public override bool Equals(object obj)
			{
				TextReplaceAction other = obj as TextReplaceAction;
				if (other == null) {
					return false;
				}
				return this.Offset == other.Offset && this.RemovalLength == other.RemovalLength && this.NewText == other.NewText;
			}

			public override int GetHashCode()
			{
				return 0;
			}

			public override string ToString()
			{
				return string.Format("[TextReplaceAction: Offset={0}, RemovalLength={1}, NewText={2}]", Offset, RemovalLength, NewText);
			}
		}
	}
}
