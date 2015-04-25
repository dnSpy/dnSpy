// 
// BaseRefactoringContext.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin <http://xamarin.com>
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
using System.ComponentModel.Design;
using ICSharpCode.NRefactory.CSharp.Analysis;
using ICSharpCode.NRefactory.Utils;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Analysis;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	public abstract class BaseRefactoringContext : IServiceProvider
	{
		readonly CSharpAstResolver resolver;
		readonly CancellationToken cancellationToken;
		
		public virtual bool Supports(Version version)
		{
			return true;
		}

		/// <summary>
		/// Gets the default namespace which should be defined in this file.
		/// </summary>
		public abstract string DefaultNamespace {
			get;
		}

		/// <summary>
		/// Gets a value indicating if 'var' keyword should be used or explicit types.
		/// </summary>
		public virtual bool UseExplicitTypes {
			get;
			set;
		}
		
		public CancellationToken CancellationToken {
			get { return cancellationToken; }
		}
		
		public virtual AstNode RootNode {
			get {
				return resolver.RootNode;
			}
		}

		public CSharpAstResolver Resolver {
			get {
				return resolver;
			}
		}

		public virtual CSharpUnresolvedFile UnresolvedFile {
			get {
				return resolver.UnresolvedFile;
			}
		}

		public ICompilation Compilation {
			get { return resolver.Compilation; }
		}

		/// <summary>
		/// Gets the type graph for the current compilation.
		/// </summary>
		public virtual TypeGraph TypeGraph {
			get { return new TypeGraph(Compilation.Assemblies); }
		}
		
		public BaseRefactoringContext (ICSharpCode.NRefactory.CSharp.Resolver.CSharpAstResolver resolver, System.Threading.CancellationToken cancellationToken)
		{
			this.resolver = resolver;
			this.cancellationToken = cancellationToken;
			this.referenceFinder = new LocalReferenceFinder(resolver);
		}


		#region Resolving
		public ResolveResult Resolve (AstNode node)
		{
			return resolver.Resolve (node, cancellationToken);
		}
		
		public CSharpResolver GetResolverStateBefore(AstNode node)
		{
			return resolver.GetResolverStateBefore (node, cancellationToken);
		}
		
		public CSharpResolver GetResolverStateAfter(AstNode node)
		{
			return resolver.GetResolverStateAfter (node, cancellationToken);
		}

		public IType ResolveType (AstType type)
		{
			return resolver.Resolve (type, cancellationToken).Type;
		}
		
		public IType GetExpectedType (Expression expression)
		{
			return resolver.GetExpectedType(expression, cancellationToken);
		}
		
		public Conversion GetConversion (Expression expression)
		{
			return resolver.GetConversion(expression, cancellationToken);
		}
		
		public TypeSystemAstBuilder CreateTypeSystemAstBuilder(AstNode node)
		{
			var csResolver = resolver.GetResolverStateBefore(node);
			return new TypeSystemAstBuilder(csResolver);
		}
		#endregion

		#region Code Analyzation
		/// <summary>
		/// Creates a new definite assignment analysis object with a given root statement.
		/// </summary>
		/// <returns>
		/// The definite assignment analysis object.
		/// </returns>
		/// <param name='root'>
		/// The root statement.
		/// </param>
		public DefiniteAssignmentAnalysis CreateDefiniteAssignmentAnalysis (Statement root)
		{
			return new DefiniteAssignmentAnalysis (root, resolver, CancellationToken);
		}

		/// <summary>
		/// Creates a new reachability analysis object with a given statement.
		/// </summary>
		/// <param name="statement">
		/// The statement to start the analysis.
		/// </param>
		/// <param name="recursiveDetectorVisitor">
		/// TODO.
		/// </param>
		/// <returns>
		/// The reachability analysis object.
		/// </returns>
		public ReachabilityAnalysis CreateReachabilityAnalysis (Statement statement, ReachabilityAnalysis.RecursiveDetectorVisitor recursiveDetectorVisitor = null)
		{
			return ReachabilityAnalysis.Create (statement, resolver, recursiveDetectorVisitor, CancellationToken);
		}

		/// <summary>
		/// Parses a composite format string.
		/// </summary>
		/// <returns>
		/// The format string parsing result.
		/// </returns>
		public virtual FormatStringParseResult ParseFormatString(string source)
		{
			return new CompositeFormatStringParser().Parse(source);
		}

		LocalReferenceFinder referenceFinder;

		public IList<ReferenceResult> FindReferences(AstNode rootNode, IVariable variable)
		{
			return referenceFinder.FindReferences(rootNode, variable);
		}

		#endregion


		#region Naming
		public virtual string GetNameProposal (string name, TextLocation loc, bool camelCase = true)
		{
			string baseName = (camelCase ? char.ToLower (name [0]) : char.ToUpper (name [0])) + name.Substring (1);

			var type = RootNode.GetNodeAt<TypeDeclaration>(loc);
			if (type == null)
				return baseName;

			int number = -1;
			string proposedName;
			do {
				proposedName = AppendNumberToName (baseName, number++);
			} while (type.Members.Select (m => m.GetChildByRole (Roles.Identifier)).Any (n => n.Name == proposedName));
			return proposedName;
		}
		
		public virtual string GetLocalNameProposal (string name, TextLocation loc, bool camelCase = true)
		{
			string baseName = (camelCase ? char.ToLower (name [0]) : char.ToUpper (name [0])) + name.Substring (1);
			var node = RootNode.GetNodeAt(loc);
			if (node == null)
				return baseName;
			
			var context = GetResolverStateBefore (node);
			int number = -1;
			string proposedName;
			do {
				proposedName = AppendNumberToName (baseName, number++);
			} while (!(context.ResolveSimpleName (proposedName, EmptyList<IType>.Instance) is UnknownIdentifierResolveResult));
			return proposedName;
		}

		static string AppendNumberToName (string baseName, int number)
		{
			return baseName + (number > 0 ? (number + 1).ToString () : "");
		}
		#endregion

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


		/// <summary>
		/// Translates the english input string to the context language.
		/// </summary>
		/// <returns>
		/// The translated string.
		/// </returns>
		public virtual string TranslateString(string str)
		{
			return str;
		}

		#region IServiceProvider implementation
		IServiceContainer services = new ServiceContainer();
		
		/// <summary>
		/// Gets a service container used to associate services with this context.
		/// </summary>
		public IServiceContainer Services {
			get { return services; }
			protected set { services = value; }
		}
		
		/// <summary>
		/// Retrieves a service from the refactoring context.
		/// If the service is not found in the <see cref="Services"/> container.
		/// </summary>
		public object GetService(Type serviceType)
		{
			return services.GetService(serviceType);
		}
		#endregion
	}
	
}
