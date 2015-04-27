// 
// LovalVariableDeclarationSpace.cs
//  
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
// 
// Copyright (c) 2013 Simon Lindgren
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
using ICSharpCode.NRefactory.Utils;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ICSharpCode.NRefactory.CSharp.Analysis
{
	/// <summary>
	/// Represents a declaration space. (§3.3)
	/// </summary>
	public class LocalDeclarationSpace
	{
		/// <summary>
		/// Maps from variable name to the declarations in this declaration space.
		/// </summary>
		/// <remarks>
		/// This maps from variable name
		/// </remarks>
		MultiDictionary<string, AstNode> declarations = new MultiDictionary<string, AstNode> ();

		public LocalDeclarationSpace()
		{
			Children = new List<LocalDeclarationSpace> ();
		}

		/// <summary>
		/// The child declaration spaces.
		/// </summary>
		public IList<LocalDeclarationSpace> Children {
			get;
			private set;
		}

		/// <summary>
		/// The parent declaration space.
		/// </summary>
		/// <value>The parent.</value>
		public LocalDeclarationSpace Parent {
			get;
			private set;
		}

		/// <summary>
		/// The names declared in this declaration space, excluding child spaces.
		/// </summary>
		/// <value>The declared names.</value>
		public ICollection<string> DeclaredNames {
			get {
				return declarations.Keys;
			}
		}

		/// <summary>
		/// Get all nodes declaring the name specified in <paramref name="name"/>.
		/// </summary>
		/// <returns>The declaring nodes.</returns>
		/// <param name="name">The declaration name.</param>
		public IEnumerable<AstNode> GetNameDeclarations(string name)
		{
			return declarations [name].Concat(Children.SelectMany(child => child.GetNameDeclarations(name)));
		}

		/// <summary>
		/// Adds a child declaration space.
		/// </summary>
		/// <param name="child">The <see cref="LocalDeclarationSpace"/> to add.</param>
		public void AddChildSpace(LocalDeclarationSpace child)
		{
			if (child == null)
				throw new ArgumentNullException("child");
			if (Children.Contains(child))
				throw new InvalidOperationException("the child was already added");

			Children.Add(child);
			child.Parent = this;
		}

		/// <summary>
		/// Adds a new declaration to the declaration space.
		/// </summary>
		/// <param name="name">The name of the declared variable.</param>
		/// <param name="node">A node associated with the declaration.</param>
		public void AddDeclaration(string name, AstNode node)
		{
			if (name == null)
				throw new ArgumentNullException("name");
			if (node == null)
				throw new ArgumentNullException("node");
			declarations.Add(name, node);
		}

		/// <summary>
		/// Determines if the name exists in the this declaration space.
		/// </summary>
		/// <returns><c>true</c>, if the name specified in <paramref name="name"/> is used in this variable declaration space, <c>false</c> otherwise.</returns>
		/// <param name="name">The name to look for.</param>
		/// <param name="includeChildren">When <c>true</c>, child declaration spaces are included in the search.</param>
		public bool ContainsName(string name, bool includeChildren)
		{
			if (name == null)
				throw new ArgumentNullException("name");

			if (declarations.Keys.Contains(name)) 
				return true;
			return includeChildren && Children.Any(child => child.ContainsName(name, true));
		}

		/// <summary>
		/// Determines whether the name specified in <paramref name="name"/> is used in surrouding code.
		/// </summary>
		/// <returns><c>true</c> if the name is used, <c>false</c> otherwise.</returns>
		/// <param name="name">The name to check.</param>
		/// <remarks>
		/// Contrary to <see cref="ContainsName"/>, this method also checks parent declaration spaces
		/// for name conflicts. Typically, this will be the right method to use when determining if a name can be used.
		/// </remarks>
		public bool IsNameUsed(string name)
		{
			if (name == null)
				throw new ArgumentNullException("name");

			return IsNameUsedBySelfOrParent(name) || Children.Any(child => child.ContainsName(name, true));
		}

		bool IsNameUsedBySelfOrParent(string name)
		{
			if (declarations.Keys.Contains(name))
				return true;
			return Parent != null && Parent.IsNameUsedBySelfOrParent(name);
		}
	}
}