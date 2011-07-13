// Copyright (c) Cristian Civera (cristian@aspitalia.com)
// This code is distributed under the MS-PL (for details please see \doc\MS-PL.txt)

using System;

namespace Ricciolo.StylesExplorer.MarkupReflection
{
	internal class TypeDeclaration
	{
		private readonly XmlBamlReader reader;
		private readonly bool _isExtension;
		private IType _type;
		private bool _typeLoaded;
		private readonly ITypeResolver resolver;

		public TypeDeclaration(ITypeResolver resolver, string name, string namespaceName, short assemblyId)
			: this(null, resolver, name, namespaceName, assemblyId, true)
		{
		}

		public TypeDeclaration(ITypeResolver resolver, string name, string enclosingTypeName, string namespaceName, short assemblyId)
			: this(null, resolver, name, namespaceName, assemblyId, true)
		{
			this.EnclosingTypeName = enclosingTypeName;
		}

		public TypeDeclaration(ITypeResolver resolver, string name, string namespaceName, short assemblyId, bool isExtension)
			: this(null, resolver, name, namespaceName, assemblyId, true)
		{
			_isExtension = isExtension;
		}

		public TypeDeclaration(XmlBamlReader reader, ITypeResolver resolver, string name, string namespaceName, short assemblyId)
			: this(reader, resolver, name, namespaceName, assemblyId, true)
		{
		}

		public TypeDeclaration(XmlBamlReader reader, ITypeResolver resolver, string name, string namespaceName, short assemblyId, bool isKnown)
		{
			this.reader = reader;
			this.resolver = resolver;
			this.Name = name;
			this.Namespace = namespaceName;
			this.AssemblyId = assemblyId;
			this.IsKnown = isKnown;

			if (!_isExtension)
				_isExtension = name.EndsWith("Extension");
		}

		public override string ToString()
		{
			return this.Name;
		}
		
		public string EnclosingTypeName { get; private set; }

		public bool IsExtension
		{
			get { return _isExtension; }
		}

		public string Assembly
		{
			get {
				if (reader != null)
					return this.reader.GetAssembly(this.AssemblyId);
				else
					return KnownInfo.KnownAssemblyTable[this.AssemblyId];
			}
		}

		public short AssemblyId { get; private set; }

		public string Name { get; private set; }

		public bool IsKnown { get; private set; }

		public IType Type {
			get
			{
				if (!_typeLoaded)
				{
					if (this.Name.Length > 0)
						_type = resolver.GetTypeByAssemblyQualifiedName(AssemblyQualifiedName);
					_typeLoaded = true;
				}

				return _type;
			}
		}

		public string Namespace { get; private set; }
		
		public string FullyQualifiedName {
			get { return EnclosingTypeName == null ? string.Format("{0}.{1}", Namespace, Name) : string.Format("{0}.{1}+{2}", Namespace, EnclosingTypeName, Name); }
		}
		
		public string AssemblyQualifiedName {
			get { return string.Format("{0}, {1}", FullyQualifiedName, Assembly); }
		}

		public override bool Equals(object obj)
		{
			TypeDeclaration td = obj as TypeDeclaration;
			if (td != null)
				return (this.Name == td.Name && this.EnclosingTypeName == td.EnclosingTypeName && this.Namespace == td.Namespace && this.AssemblyId == td.AssemblyId);
			else
				return false;
		}
		
		public override int GetHashCode()
		{
			return this.AssemblyId ^ this.Name.GetHashCode() ^ this.EnclosingTypeName.GetHashCode() ^ this.Namespace.GetHashCode();
		}
	}

}
