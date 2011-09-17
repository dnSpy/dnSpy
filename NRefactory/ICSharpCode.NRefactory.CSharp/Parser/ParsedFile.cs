// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
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
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// Represents a file that was parsed and converted for the type system.
	/// </summary>
	[Serializable]
	public sealed class CSharpParsedFile : AbstractFreezable, IParsedFile
	{
		readonly string fileName;
		readonly UsingScope rootUsingScope;
		IList<ITypeDefinition> topLevelTypeDefinitions = new List<ITypeDefinition>();
		IList<IAttribute> assemblyAttributes = new List<IAttribute>();
		IList<IAttribute> moduleAttributes = new List<IAttribute>();
		IList<UsingScope> usingScopes = new List<UsingScope>();
		IList<Error> errors = new List<Error> ();
		
		protected override void FreezeInternal()
		{
			base.FreezeInternal();
			rootUsingScope.Freeze();
			topLevelTypeDefinitions = FreezeList(topLevelTypeDefinitions);
			assemblyAttributes = FreezeList(assemblyAttributes);
			moduleAttributes = FreezeList(moduleAttributes);
			usingScopes = FreezeList(usingScopes);
		}
		
		public CSharpParsedFile(string fileName, UsingScope rootUsingScope)
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
		
		DateTime lastWriteTime = DateTime.UtcNow;
		
		public DateTime LastWriteTime {
			get { return lastWriteTime; }
			set {
				CheckBeforeMutation();
				lastWriteTime = value;
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
		
		public IList<IAttribute> ModuleAttributes {
			get { return moduleAttributes; }
		}
		
		public UsingScope GetUsingScope(TextLocation location)
		{
			foreach (UsingScope scope in usingScopes) {
				if (scope.Region.IsInside(location.Line, location.Column))
					return scope;
			}
			return rootUsingScope;
		}
		
		public ITypeDefinition GetTopLevelTypeDefinition(TextLocation location)
		{
			return FindEntity(topLevelTypeDefinitions, location);
		}
		
		public ITypeDefinition GetInnermostTypeDefinition(TextLocation location)
		{
			ITypeDefinition parent = null;
			ITypeDefinition type = GetTopLevelTypeDefinition(location);
			while (type != null) {
				parent = type;
				type = FindEntity(parent.NestedTypes, location);
			}
			return parent;
		}
		
		public IMember GetMember(TextLocation location)
		{
			ITypeDefinition type = GetInnermostTypeDefinition(location);
			if (type == null)
				return null;
			return FindEntity(type.Methods, location)
				?? FindEntity(type.Fields, location)
				?? FindEntity(type.Properties, location)
				?? (IMember)FindEntity(type.Events, location);
		}
		
		static T FindEntity<T>(IList<T> list, TextLocation location) where T : class, IEntity
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
