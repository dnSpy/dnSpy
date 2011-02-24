// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.PatternMatching
{
	/// <summary>
	/// Represents the result of a pattern matching operation.
	/// </summary>
	public sealed class Match
	{
		List<KeyValuePair<string, AstNode>> results = new List<KeyValuePair<string, AstNode>>();
		
		internal int CheckPoint()
		{
			return results.Count;
		}
		
		internal void RestoreCheckPoint(int checkPoint)
		{
			results.RemoveRange(checkPoint, results.Count - checkPoint);
		}
		
		public IEnumerable<AstNode> Get(string groupName)
		{
			foreach (var pair in results) {
				if (pair.Key == groupName)
					yield return pair.Value;
			}
		}
		
		public IEnumerable<T> Get<T>(string groupName) where T : AstNode
		{
			foreach (var pair in results) {
				if (pair.Key == groupName)
					yield return (T)pair.Value;
			}
		}
		
		public bool Has(string groupName)
		{
			foreach (var pair in results) {
				if (pair.Key == groupName)
					return true;
			}
			return false;
		}
		
		public void Add(string groupName, AstNode node)
		{
			if (groupName != null && node != null) {
				results.Add(new KeyValuePair<string, AstNode>(groupName, node));
			}
		}
	}
}
