// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Search
{
	class RegexSearchStrategy : ISearchStrategy
	{
		readonly Regex searchPattern;
		
		public RegexSearchStrategy(Regex searchPattern)
		{
			if (searchPattern == null)
				throw new ArgumentNullException("searchPattern");
			this.searchPattern = searchPattern;
		}
		
		public IEnumerable<ISearchResult> FindAll(ITextSource document, int offset, int length)
		{
			int endOffset = offset + length;
			foreach (Match result in searchPattern.Matches(document.Text)) {
				if (offset <= result.Index && endOffset >= (result.Length + result.Index))
					yield return new SearchResult { StartOffset = result.Index, Length = result.Length, Data = result };
			}
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
