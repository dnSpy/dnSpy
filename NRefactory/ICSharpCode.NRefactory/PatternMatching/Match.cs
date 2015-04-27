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
		
		public void Add(string groupName, INode node)
		{
			if (groupName != null && node != null) {
				results.Add(new KeyValuePair<string, INode>(groupName, node));
			}
		}

		internal void AddNull (string groupName)
		{
			if (groupName != null) {
				results.Add(new KeyValuePair<string, INode>(groupName, null));
			}
		}
	}
}
