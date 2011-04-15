// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.NRefactory.PatternMatching
{
	/// <summary>
	/// Represents the result of a pattern matching operation.
	/// </summary>
	public struct Match
	{
		// TODO: maybe we should add an implicit Match->bool conversion? (implicit operator bool(Match m) { return m != null; })
		
		List<KeyValuePair<string, INode>> results;
		
		public bool Success {
			get { return results != null; }
		}
		
		internal static Match CreateNew()
		{
			Match m;
			m.results = new List<KeyValuePair<string, INode>>();
			return m;
		}
		
		internal int CheckPoint()
		{
			return results.Count;
		}
		
		internal void RestoreCheckPoint(int checkPoint)
		{
			results.RemoveRange(checkPoint, results.Count - checkPoint);
		}
		
		public IEnumerable<INode> Get(string groupName)
		{
			if (results == null)
				yield break;
			foreach (var pair in results) {
				if (pair.Key == groupName)
					yield return pair.Value;
			}
		}
		
		public IEnumerable<T> Get<T>(string groupName) where T : INode
		{
			if (results == null)
				yield break;
			foreach (var pair in results) {
				if (pair.Key == groupName)
					yield return (T)pair.Value;
			}
		}
		
		public bool Has(string groupName)
		{
			if (results == null)
				return false;
			foreach (var pair in results) {
				if (pair.Key == groupName)
					return true;
			}
			return false;
		}
		
		internal void Add(string groupName, INode node)
		{
			if (groupName != null && node != null) {
				results.Add(new KeyValuePair<string, INode>(groupName, node));
			}
		}
	}
}
