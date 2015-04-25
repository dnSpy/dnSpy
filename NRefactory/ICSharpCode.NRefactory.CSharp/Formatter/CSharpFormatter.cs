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
	public enum FormattingMode {
		OnTheFly,
		Intrusive
	}

	/// <summary>
	/// The C# Formatter generates a set of text replace actions to format a region in a C# document.
	/// </summary>
	public class CSharpFormatter
	{
		readonly CSharpFormattingOptions policy;
		readonly TextEditorOptions options;

		/// <summary>
		/// Gets the formatting policy the formatter uses.
		/// </summary>
		public CSharpFormattingOptions Policy {
			get {
				return policy;
			}
		}

		/// <summary>
		/// Gets the text editor options the formatter uses.
		/// Note: If none was specified TextEditorOptions.Default gets used.
		/// </summary>
		public TextEditorOptions TextEditorOptions {
			get {
				return options;
			}
		}

		List<DomRegion> formattingRegions = new List<DomRegion> ();
		internal TextLocation lastFormattingLocation = new TextLocation(int.MaxValue, int.MaxValue);

		/// <summary>
		/// Gets the formatting regions. NOTE: Will get changed to IReadOnlyList.
		/// </summary>
		public IList<DomRegion> FormattingRegions {
			get {
				return formattingRegions;
			}
		}

		/// <summary>
		/// Gets or sets the formatting mode. For on the fly formatting a lightweight formatting mode
		/// gives better results.
		/// </summary>
		public FormattingMode FormattingMode {
			get;
			set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ICSharpCode.NRefactory.CSharp.CSharpFormatter"/> class.
		/// </summary>
		/// <param name="policy">The formatting policy to use.</param>
		/// <param name="options">The text editor options (optional). Default is: TextEditorOptions.Default</param>
		public CSharpFormatter(CSharpFormattingOptions policy, TextEditorOptions options = null)
		{
			if (policy == null)
				throw new ArgumentNullException("policy");
			this.policy = policy;
			this.options = options ?? TextEditorOptions.Default;
		}

		/// <summary>
		/// Format the specified document and gives back the formatted text as result.
		/// </summary>
		public string Format(IDocument document)
		{
			return InternalFormat (new StringBuilderDocument (document.Text));
		}

		/// <summary>
		/// Format the specified text and gives back the formatted text as result.
		/// </summary>
		public string Format(string text)
		{
			return InternalFormat (new StringBuilderDocument (text));
		}

		string InternalFormat(IDocument document)
		{
			var syntaxTree = SyntaxTree.Parse (document, document.FileName);
			var changes = AnalyzeFormatting(document, syntaxTree);
			changes.ApplyChanges();
			return document.Text;
		}

		/// <summary>
		/// Analyzes the formatting of a given document and syntax tree.
		/// </summary>
		/// <param name="document">Document.</param>
		/// <param name="syntaxTree">Syntax tree.</param>
		/// <param name="token">The cancellation token.</param>
		public FormattingChanges AnalyzeFormatting(IDocument document, SyntaxTree syntaxTree, CancellationToken token = default (CancellationToken))
		{
			if (document == null)
				throw new ArgumentNullException("document");
			if (syntaxTree == null)
				throw new ArgumentNullException("syntaxTree");
			var result = new FormattingChanges(document);
			var visitor = new FormattingVisitor(this, document, result, token);
			syntaxTree.AcceptVisitor(visitor);
			return result;
		}

		/// <summary>
		/// Adds a region in the document that should be formatted.
		/// </summary>
		public void AddFormattingRegion (DomRegion region)
		{
			formattingRegions.Add(region);
			if (formattingRegions.Count == 1) {
				lastFormattingLocation = region.End;
			} else {
				lastFormattingLocation = lastFormattingLocation < region.End ? region.End : lastFormattingLocation;
			}
		}

	}
}

