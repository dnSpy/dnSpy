// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// Represents a file that was parsed and converted for the type system.
	/// </summary>
	public sealed class ParsedFile : AbstractFreezable
	{
		readonly string fileName;
		readonly UsingScope rootUsingScope;
		IList<ITypeDefinition> topLevelTypeDefinitions = new List<ITypeDefinition>();
		IList<IAttribute> assemblyAttributes = new List<IAttribute>();
		IList<UsingScope> usingScopes = new List<UsingScope>();
		
		protected override void FreezeInternal()
		{
			base.FreezeInternal();
			rootUsingScope.Freeze();
			topLevelTypeDefinitions = FreezeList(topLevelTypeDefinitions);
			assemblyAttributes = FreezeList(assemblyAttributes);
			usingScopes = FreezeList(usingScopes);
		}
		
		public ParsedFile(string fileName, UsingScope rootUsingScope)
		{
			if (fileName == null)
				throw new ArgumentNullException("fileName");
			if (rootUsingScope == null)
				throw new ArgumentNullException("rootUsingScope");
			this.fileName = fileName;
			this.rootUsingScope = rootUsingScope;
		}
		
		public string FileName {
			get { return fileName; }
		}
		
		public UsingScope RootUsingScope {
			get { return rootUsingScope; }
		}
		
		public IList<UsingScope> UsingScopes {
			get { return usingScopes; }
		}
		
		public IList<ITypeDefinition> TopLevelTypeDefinitions {
			get { return topLevelTypeDefinitions; }
		}
		
		public IList<IAttribute> AssemblyAttributes {
			get { return assemblyAttributes; }
		}
		
		public UsingScope GetUsingScope(AstLocation location)
		{
			foreach (UsingScope scope in usingScopes) {
				if (scope.Region.IsInside(location.Line, location.Column))
					return scope;
			}
			return rootUsingScope;
		}
		
		public ITypeDefinition GetTopLevelTypeDefinition(AstLocation location)
		{
			foreach (ITypeDefinition typeDef in topLevelTypeDefinitions) {
				if (typeDef.Region.IsInside(location.Line, location.Column))
					return typeDef;
			}
			return null;
		}
	}
}
