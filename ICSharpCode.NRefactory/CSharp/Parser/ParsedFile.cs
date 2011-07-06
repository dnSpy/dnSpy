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
	public sealed class ParsedFile : AbstractFreezable, IParsedFile
	{
		readonly string fileName;
		readonly UsingScope rootUsingScope;
		IList<ITypeDefinition> topLevelTypeDefinitions = new List<ITypeDefinition>();
		IList<IAttribute> assemblyAttributes = new List<IAttribute>();
		IList<UsingScope> usingScopes = new List<UsingScope>();
		IList<Error> errors = new List<Error> ();
		
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
		
		DateTime parseTime = DateTime.Now;
		public DateTime ParseTime {
			get {
				return parseTime;
			}
		}
		
		public UsingScope RootUsingScope {
			get { return rootUsingScope; }
		}
		
		public IList<Error> Errors {
			get { return errors; }
			internal set { errors = (List<Error>)value; }
		}
		
		public IList<UsingScope> UsingScopes {
			get { return usingScopes; }
		}
		
		public IProjectContent ProjectContent {
			get { return rootUsingScope.ProjectContent; }
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
			return FindEntity(topLevelTypeDefinitions, location);
		}
		
		public ITypeDefinition GetTypeDefinition(AstLocation location)
		{
			ITypeDefinition parent = null;
			ITypeDefinition type = GetTopLevelTypeDefinition(location);
			while (type != null) {
				parent = type;
				type = FindEntity(parent.NestedTypes, location);
			}
			return parent;
		}
		
		public IMember GetMember(AstLocation location)
		{
			ITypeDefinition type = GetTypeDefinition(location);
			if (type == null)
				return null;
			return FindEntity(type.Methods, location)
				?? FindEntity(type.Fields, location)
				?? FindEntity(type.Properties, location)
				?? (IMember)FindEntity(type.Events, location);
		}
		
		static T FindEntity<T>(IList<T> list, AstLocation location) where T : class, IEntity
		{
			// This could be improved using a binary search
			foreach (T entity in list) {
				if (entity.Region.IsInside(location.Line, location.Column))
					return entity;
			}
			return null;
		}
	}
}
