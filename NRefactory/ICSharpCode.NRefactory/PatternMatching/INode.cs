// Copyright (c) 2011-2013 AlphaSierraPapa for the SharpDevelop Team
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

namespace ICSharpCode.NRefactory.PatternMatching
{
	/// <summary>
	/// AST node that supports pattern matching.
	/// </summary>
	public interface INode
	{
		Role Role { get; }
		INode FirstChild { get; }
		INode NextSibling { get; }
		bool IsNull { get; }
		
		bool DoMatch(INode other, Match match);
		bool DoMatchCollection(Role role, INode pos, Match match, BacktrackingInfo backtrackingInfo);
	}
	
	public static class PatternExtensions
	{
		/// <summary>
		/// Performs a pattern matching operation.
		/// <c>this</c> is the pattern, <paramref name="other"/> is the AST that is being matched.
		/// </summary>
		/// <returns>
		/// A match object. Check <see cref="PatternMatching.Match.Success"/> to see whether the match was successful.
		/// </returns>
		/// <remarks>
		/// Patterns are ASTs that contain special pattern nodes (from the PatternMatching namespace).
		/// However, it is also possible to match two ASTs without any pattern nodes -
		/// doing so will produce a successful match if the two ASTs are structurally identical.
		/// </remarks>
		public static Match Match(this INode pattern, INode other)
		{
			if (pattern == null)
				throw new ArgumentNullException("pattern");
			Match match = PatternMatching.Match.CreateNew();
			if (pattern.DoMatch(other, match))
				return match;
			else
				return default(PatternMatching.Match);
		}
		
		public static bool IsMatch(this INode pattern, INode other)
		{
			if (pattern == null)
				throw new ArgumentNullException("pattern");
			return pattern.DoMatch(other, PatternMatching.Match.CreateNew());
		}
	}
}
