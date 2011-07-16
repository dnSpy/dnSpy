// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Globalization;

namespace ICSharpCode.NRefactory.VB.Parser
{
	/// <summary>
	/// This class implements a keyword map. It implements a digital search trees (tries) to find
	/// a word.
	/// </summary>
	internal class LookupTable
	{
		Node root = new Node(-1, null);
		bool casesensitive;
		int  length;
		
		/// <value>
		/// The number of elements in the table
		/// </value>
		public int Count {
			get {
				return length;
			}
		}
		
		/// <summary>
		/// Inserts an int in the tree, under keyword
		/// </summary>
		public int this[string keyword] {
			get {
				Node next = root;
				
				if (!casesensitive) {
					keyword = keyword.ToUpper(CultureInfo.InvariantCulture);
				}
				
				for (int i = 0; i < keyword.Length; ++i) {
					int index = ((int)keyword[i]) % 256;
					next = next.leaf[index];
					
					if (next == null) {
						return -1;
					}
					
					if (keyword == next.word) {
						return next.val;
					}
				}
				return -1;
			}
			set {
				Node node = root;
				Node next = root;
				
				if (!casesensitive) {
					keyword = keyword.ToUpper(CultureInfo.InvariantCulture);
				}
				
				++length;
				
				// insert word into the tree
				for (int i = 0; i < keyword.Length; ++i) {
					int index = ((int)keyword[i]) % 256; // index of curchar
					bool d = keyword[i] == '\\';
					
					next = next.leaf[index];             // get node to this index
					
					if (next == null) { // no node created -> insert word here
						node.leaf[index] = new Node(value, keyword);
						break;
					}
					
					if (next.word != null && next.word.Length != i) { // node there, take node content and insert them again
						string tmpword  = next.word;                  // this word will be inserted 1 level deeper (better, don't need too much 
						int    tmpval = next.val;                 // string comparisons for finding.)
						next.val = -1;
						next.word = null;
						this[tmpword] = tmpval;
					}
					
					if (i == keyword.Length - 1) { // end of keyword reached, insert node there, if a node was here it was
						next.word = keyword;       // reinserted, if it has the same length (keyword EQUALS this word) it will be overwritten
						next.val = value;
						break;
					}
					
					node = next;
				}
			}
		}
		
		/// <summary>
		/// Creates a new instance of <see cref="LookupTable"/>
		/// </summary>
		public LookupTable(bool casesensitive)
		{
			this.casesensitive = casesensitive;
		}
		
		class Node
		{
			public Node(int val, string word)
			{
				this.word  = word;
				this.val   = val;
			}
			
			public string word;
			public int    val;
			
			public Node[] leaf = new Node[256];
		}
	}
}
