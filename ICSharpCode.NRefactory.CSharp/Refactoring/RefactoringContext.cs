// 
// RefactoringContext.cs
//
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Mike Krüger <mkrueger@novell.com>
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

using System;
using System.Linq;
using System.Threading;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.Editor;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	public abstract class RefactoringContext : BaseRefactoringContext
	{
		public RefactoringContext(CSharpAstResolver resolver, CancellationToken cancellationToken) : base  (resolver, cancellationToken)
		{
		}

		public abstract TextLocation Location { get; }
		
		public TypeSystemAstBuilder CreateTypeSytemAstBuilder()
		{
			var csResolver = Resolver.GetResolverStateBefore(GetNode());
			return new TypeSystemAstBuilder(csResolver);
		}
		
		public virtual AstType CreateShortType (IType fullType)
		{
			var builder = CreateTypeSytemAstBuilder();
			return builder.ConvertType(fullType);
		}
		
		public virtual AstType CreateShortType(string ns, string name, int typeParameterCount = 0)
		{
			var builder = CreateTypeSytemAstBuilder();
			return builder.ConvertType(ns, name, typeParameterCount);
		}

		public virtual IEnumerable<AstNode> GetSelectedNodes()
		{
			if (!IsSomethingSelected) {
				return Enumerable.Empty<AstNode> ();
			}
			
			return RootNode.GetNodesBetween(SelectionStart, SelectionEnd);
		}

		public AstNode GetNode ()
		{
			return RootNode.GetNodeAt (Location);
		}
		
		public AstNode GetNode (Predicate<AstNode> pred)
		{
			return RootNode.GetNodeAt (Location, pred);
		}
		
		public T GetNode<T> () where T : AstNode
		{
			return RootNode.GetNodeAt<T> (Location);
		}
		
		#region Text stuff
		public virtual TextEditorOptions TextEditorOptions {
			get {
				return TextEditorOptions.Default;
			}
		}
		
		public virtual bool IsSomethingSelected {
			get {
				return SelectionStart != TextLocation.Empty;
			}
		}
		
		public virtual string SelectedText {
			get { return string.Empty; }
		}
		
		public virtual TextLocation SelectionStart {
			get {
				return TextLocation.Empty;
			}
		}
		
		public virtual TextLocation SelectionEnd {
			get {
				return TextLocation.Empty;
			}
		}

		public abstract int GetOffset (TextLocation location);

		public abstract IDocumentLine GetLineByOffset (int offset);
		
		public int GetOffset (int line, int col)
		{
			return GetOffset (new TextLocation (line, col));
		}

		public abstract TextLocation GetLocation (int offset);

		public abstract string GetText (int offset, int length);

		public abstract string GetText (ISegment segment);
		#endregion

		#region Naming
		public virtual string GetNameProposal (string name, bool camelCase = true)
		{
			string baseName = (camelCase ? char.ToLower (name [0]) : char.ToUpper (name [0])) + name.Substring (1);
			
			var type = GetNode<TypeDeclaration> ();
			if (type == null)
				return baseName;
			
			int number = -1;
			string proposedName;
			do {
				proposedName = AppendNumberToName (baseName, number++);
			} while (type.Members.Select (m => m.GetChildByRole (Roles.Identifier)).Any (n => n.Name == proposedName));
			return proposedName;
		}
		
		static string AppendNumberToName (string baseName, int number)
		{
			return baseName + (number > 0 ? (number + 1).ToString () : "");
		}
		#endregion
	}
}

