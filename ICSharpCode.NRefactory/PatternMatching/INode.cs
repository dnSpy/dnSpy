// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

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
		/// A match object. Check <see cref="Match.Success"/> to see whether the match was successful.
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
