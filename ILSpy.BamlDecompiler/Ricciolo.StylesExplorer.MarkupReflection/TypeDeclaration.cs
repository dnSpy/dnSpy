// Copyright (c) Cristian Civera (cristian@aspitalia.com)
// This code is distributed under the MS-PL (for details please see \doc\MS-PL.txt)

using System;

namespace Ricciolo.StylesExplorer.MarkupReflection
{
	class TypeDeclaration
	{
		readonly XmlBamlReader reader;
		readonly bool _isExtension;
		IType _type;
		bool _typeLoaded;
		readonly ITypeResolver resolver;
		
		protected TypeDeclaration(ITypeResolver resolver)
		{
			this.resolver = resolver;
		}

		public TypeDeclaration(ITypeResolver resolver, string name, string namespaceName, short assemblyId)
			: this(null, resolver, name, namespaceName, assemblyId)
		{
		}

		public TypeDeclaration(ITypeResolver resolver, string name, string enclosingTypeName, string namespaceName, short assemblyId)
			: this(null, resolver, name, namespaceName, assemblyId)
		{
			this.EnclosingTypeName = enclosingTypeName;
		}

		public TypeDeclaration(ITypeResolver resolver, string name, string namespaceName, short assemblyId, bool isExtension)
			: this(null, resolver, name, namespaceName, assemblyId)
		{
			_isExtension = isExtension;
		}

		public TypeDeclaration(XmlBamlReader reader, ITypeResolver resolver, string name, string namespaceName, short assemblyId)
		{
			this.reader = reader;
			this.resolver = resolver;
			this.Name = name;
			this.Namespace = namespaceName;
			this.AssemblyId = assemblyId;

			if (!_isExtension)
				_isExtension = name.EndsWith("Extension");
		}

		public override string ToString()
		{
			return this.Name;
		}
		
		protected virtual string EnclosingTypeName { get; set; }

		public bool IsExtension
		{
			get { return _isExtension; }
		}

		public virtual string Assembly
		{
			get {
				if (reader != null)
					return this.reader.GetAssembly(this.AssemblyId);
				else
					return KnownInfo.KnownAssemblyTable[this.AssemblyId];
			}
		}

		public virtual short AssemblyId { get; protected set; }

		public virtual string Name { get; protected set; }

		public IType Type {
			get {
				if (!_typeLoaded) {
					if (this.Name.Length > 0)
						_type = resolver.GetTypeByAssemblyQualifiedName(AssemblyQualifiedName);
					_typeLoaded = true;
				}

				return _type;
			}
		}

		public virtual string Namespace { get; protected set; }
		
		public string FullyQualifiedName {
			get { return EnclosingTypeName == null ? string.Format("{0}.{1}", Namespace, Name) : string.Format("{0}.{1}+{2}", Namespace, EnclosingTypeName, Name); }
		}
		
		public string AssemblyQualifiedName {
			get { return string.Format("{0}, {1}", FullyQualifiedName, Assembly); }
		}

		public override bool Equals(object obj)
		{
			TypeDeclaration td = obj as TypeDeclaration;
			if (td != null && !(obj is ResolverTypeDeclaration))
				return (this.Name == td.Name && this.EnclosingTypeName == td.EnclosingTypeName && this.Namespace == td.Namespace && this.AssemblyId == td.AssemblyId);
			
			return false;
		}
		
		public override int GetHashCode()
		{
			return this.AssemblyId ^ this.Name.GetHashCode() ^ this.EnclosingTypeName.GetHashCode() ^ this.Namespace.GetHashCode();
		}
	}

	class ResolverTypeDeclaration : TypeDeclaration
	{
		string assembly;
		
		public override short AssemblyId {
			get { throw new NotSupportedException(); }
			protected set { throw new NotSupportedException(); }
		}
		
		public ResolverTypeDeclaration(ITypeResolver resolver, string assemblyQualifiedName)
			: base(resolver)
		{
			string name, @namespace, assembly;
			ParseName(assemblyQualifiedName, out name, out @namespace, out assembly);
			Name = name;
			Namespace = @namespace;
			this.assembly = assembly;
		}
		
		void ParseName(string assemblyQualifiedName, out string name, out string @namespace, out string assembly)
		{
			int bracket = assemblyQualifiedName.LastIndexOf(']');
			int commaSeparator = bracket > -1 ? assemblyQualifiedName.IndexOf(", ", bracket) : assemblyQualifiedName.IndexOf(", ");
			assembly = "";
			if (commaSeparator >= 0) {
				assembly = assemblyQualifiedName.Substring(commaSeparator + 2);
				assemblyQualifiedName = assemblyQualifiedName.Remove(commaSeparator);
			}
			int namespaceSeparator = assemblyQualifiedName.LastIndexOf('.');
			@namespace = "";
			if (namespaceSeparator >= 0) {
				@namespace = assemblyQualifiedName.Substring(0, namespaceSeparator);
			}
			name = assemblyQualifiedName.Substring(namespaceSeparator + 1);
		}
		
		public override string Assembly {
			get { return assembly; }
		}
		
		public override bool Equals(object obj)
		{
			ResolverTypeDeclaration td = obj as ResolverTypeDeclaration;
			if (td != null)
				return (this.Name == td.Name && this.EnclosingTypeName == td.EnclosingTypeName && this.Namespace == td.Namespace);
			
			return false;
		}
		
		public override int GetHashCode()
		{
			return this.Name.GetHashCode() ^ this.EnclosingTypeName.GetHashCode() ^ this.Namespace.GetHashCode();
		}
	}
}
