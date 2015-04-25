// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
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
using ICSharpCode.NRefactory.Documentation;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Implementation of <see cref="IEntity"/> that resolves an unresolved entity.
	/// </summary>
	public abstract class AbstractResolvedEntity : IEntity
	{
		protected readonly IUnresolvedEntity unresolved;
		protected readonly ITypeResolveContext parentContext;
		
		protected AbstractResolvedEntity(IUnresolvedEntity unresolved, ITypeResolveContext parentContext)
		{
			if (unresolved == null)
				throw new ArgumentNullException("unresolved");
			if (parentContext == null)
				throw new ArgumentNullException("parentContext");
			this.unresolved = unresolved;
			this.parentContext = parentContext;
			this.Attributes = unresolved.Attributes.CreateResolvedAttributes(parentContext);
		}
		
		public SymbolKind SymbolKind {
			get { return unresolved.SymbolKind; }
		}
		
		[Obsolete("Use the SymbolKind property instead.")]
		public EntityType EntityType {
			get { return (EntityType)unresolved.SymbolKind; }
		}
		
		public DomRegion Region {
			get { return unresolved.Region; }
		}
		
		public DomRegion BodyRegion {
			get { return unresolved.BodyRegion; }
		}
		
		public ITypeDefinition DeclaringTypeDefinition {
			get { return parentContext.CurrentTypeDefinition; }
		}
		
		public virtual IType DeclaringType {
			get { return parentContext.CurrentTypeDefinition ?? (IType)SpecialType.UnknownType; }
		}
		
		public IAssembly ParentAssembly {
			get { return parentContext.CurrentAssembly; }
		}
		
		public IList<IAttribute> Attributes { get; protected set; }
		
		public virtual DocumentationComment Documentation {
			get {
				IDocumentationProvider provider = FindDocumentation(parentContext);
				if (provider != null)
					return provider.GetDocumentation(this);
				else
					return null;
			}
		}
		
		internal static IDocumentationProvider FindDocumentation(ITypeResolveContext context)
		{
			IAssembly asm = context.CurrentAssembly;
			if (asm != null)
				return asm.UnresolvedAssembly as IDocumentationProvider;
			else
				return null;
		}

		public abstract ISymbolReference ToReference();
		
		public bool IsStatic { get { return unresolved.IsStatic; } }
		public bool IsAbstract { get { return unresolved.IsAbstract; } }
		public bool IsSealed { get { return unresolved.IsSealed; } }
		public bool IsShadowing { get { return unresolved.IsShadowing; } }
		public bool IsSynthetic { get { return unresolved.IsSynthetic; } }
		
		public ICompilation Compilation {
			get { return parentContext.Compilation; }
		}
		
		public string FullName { get { return unresolved.FullName; } }
		public string Name { get { return unresolved.Name; } }
		public string ReflectionName { get { return unresolved.ReflectionName; } }
		public string Namespace { get { return unresolved.Namespace; } }
		
		public Accessibility Accessibility { get { return unresolved.Accessibility; } }
		public bool IsPrivate { get { return unresolved.IsPrivate; } }
		public bool IsPublic { get { return unresolved.IsPublic; } }
		public bool IsProtected { get { return unresolved.IsProtected; } }
		public bool IsInternal { get { return unresolved.IsInternal; } }
		public bool IsProtectedOrInternal { get { return unresolved.IsProtectedOrInternal; } }
		public bool IsProtectedAndInternal { get { return unresolved.IsProtectedAndInternal; } }
		
		public override string ToString()
		{
			return "[" + this.SymbolKind.ToString() + " " + this.ReflectionName + "]";
		}
	}
}
