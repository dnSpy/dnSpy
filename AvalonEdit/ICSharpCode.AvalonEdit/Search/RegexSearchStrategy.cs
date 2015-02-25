// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.NRefactory.Editor;

namespace ICSharpCode.AvalonEdit.Search
{
	class RegexSearchStrategy : ISearchStrategy
	{
		readonly Regex searchPattern;
		readonly bool matchWholeWords;
		
		public RegexSearchStrategy(Regex searchPattern, bool matchWholeWords)
		{
			if (searchPattern == null)
				throw new ArgumentNullException("searchPattern");
			this.searchPattern = searchPattern;
			this.matchWholeWords = matchWholeWords;
		}
		
		public IEnumerable<ISearchResult> FindAll(ITextSource document, int offset, int length)
		{
			int endOffset = offset + length;
			foreach (Match result in searchPattern.Matches(document.Text)) {
				int resultEndOffset = result.Length + result.Index;
				if (offset > result.Index || endOffset < resultEndOffset)
					continue;
				if (matchWholeWords && (!IsWordBorder(document, result.Index) || !IsWordBorder(document, resultEndOffset)))
					continue;
				yield return new SearchResult { StartOffset = result.Index, Length = result.Length, Data = result };
			}
		}
		
		static bool IsWordBorder(ITextSource document, int offset)
		{
			return TextUtilities.GetNextCaretPosition(document, offset - 1, LogicalDirection.Forward, CaretPositioningMode.WordBorder) == offset;
		}
		
		public ISearchResult FindNext(ITextSource document, int offset, int length)
		{
			return FindAll(document, offset, length).FirstOrDefault();
		}
		
		public bool Equals(ISearchStrategy other)
		{
			var strategy = other as RegexSearchStrategy;
			return strategy != null &&
				strategy.searchPattern.ToString() == searchPattern.ToString() &&
				strategy.searchPattern.Options == searchPattern.Options &&
				strategy.searchPattern.RightToLeft == searchPattern.RightToLeft;
		}
	}
	
	class SearchResult : TextSegment, ISearchResult
	{
		public Match Data { get; set; }
		
		public string ReplaceWith(string replacement)
		{
			return Data.Result(replacement);
		}
	}
}
