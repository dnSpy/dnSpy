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
using System.Runtime.Serialization;
using ICSharpCode.NRefactory.Editor;
using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Search
{
	/// <summary>
	/// Basic interface for search algorithms.
	/// </summary>
	public interface ISearchStrategy : IEquatable<ISearchStrategy>
	{
		/// <summary>
		/// Finds all matches in the given ITextSource and the given range.
		/// </summary>
		/// <remarks>
		/// This method must be implemented thread-safe.
		/// All segments in the result must be within the given range, and they must be returned in order
		/// (e.g. if two results are returned, EndOffset of first result must be less than or equal StartOffset of second result).
		/// </remarks>
		IEnumerable<ISearchResult> FindAll(ITextSource document, int offset, int length);
		
		/// <summary>
		/// Finds the next match in the given ITextSource and the given range.
		/// </summary>
		/// <remarks>This method must be implemented thread-safe.</remarks>
		ISearchResult FindNext(ITextSource document, int offset, int length);
	}
	
	/// <summary>
	/// Represents a search result.
	/// </summary>
	public interface ISearchResult : ISegment
	{
		/// <summary>
		/// Replaces parts of the replacement string with parts from the match. (e.g. $1)
		/// </summary>
		string ReplaceWith(string replacement);
	}
	
	/// <summary>
	/// Defines supported search modes.
	/// </summary>
	public enum SearchMode
	{
		/// <summary>
		/// Standard search
		/// </summary>
		Normal,
		/// <summary>
		/// RegEx search
		/// </summary>
		RegEx,
		/// <summary>
		/// Wildcard search
		/// </summary>
		Wildcard
	}
	
	/// <inheritdoc/>
	public class SearchPatternException : Exception, ISerializable
	{
		/// <inheritdoc/>
		public SearchPatternException()
		{
		}
		
		/// <inheritdoc/>
		public SearchPatternException(string message) : base(message)
		{
		}
		
		/// <inheritdoc/>
		public SearchPatternException(string message, Exception innerException) : base(message, innerException)
		{
		}

		// This constructor is needed for serialization.
		/// <inheritdoc/>
		protected SearchPatternException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}
