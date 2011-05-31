// Copyright (c) Cristian Civera (cristian@aspitalia.com)
// This code is distributed under the MS-PL (for details please see \doc\MS-PL.txt)

using System;

namespace Ricciolo.StylesExplorer.MarkupReflection
{
	internal class TypeDeclaration
	{
		private readonly XmlBamlReader reader;

		private readonly short _assemblyId;
		private readonly bool _isKnown;
		private readonly string _name;
		private readonly string _namespaceName;
		private readonly bool _isExtension;
		private IType _type;
		private bool _typeLoaded;
		private readonly ITypeResolver resolver;

		public TypeDeclaration(ITypeResolver resolver, string name, string namespaceName, short assemblyId)
			: this(null, resolver, name, namespaceName, assemblyId, true)
		{

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
			this._name = name;
			this._namespaceName = namespaceName;
			this._assemblyId = assemblyId;
			this._isKnown = isKnown;

			if (!_isExtension)
				_isExtension = name.EndsWith("Extension");
		}

		public override string ToString()
		{
			return this._name;
		}

		public bool IsExtension
		{
			get { return _isExtension; }
		}

		public string Assembly
		{
			get
			{
				if (reader != null)
					return this.reader.GetAssembly(this.AssemblyId);
				else
					return KnownInfo.KnownAssemblyTable[this.AssemblyId];
			}
		}

		public short AssemblyId
		{
			get { return _assemblyId; }
		}

		public string Name
		{
			get
			{
				return this._name;
			}
		}

		public bool IsKnown
		{
			get { return _isKnown; }
		}

		//public Type DotNetType
		//{
		//    get
		//    {
		//        if (!_typeLoaded)
		//        {
		//            _type = Type.GetType(String.Format("{0}.{1}, {2}", this.Namespace, this.Name, this.Assembly), false, true);
		//            _typeLoaded = true;
		//        }

		//        return _type;
		//    }
		//}

		public IType Type
		{
			get
			{
				if (!_typeLoaded)
				{
					if (this.Name.Length > 0)
						_type = resolver.GetTypeByAssemblyQualifiedName(String.Format("{0}.{1}, {2}", this.Namespace, this.Name, this.Assembly));
					_typeLoaded = true;
				}

				return _type;
			}
		}

		public string Namespace
		{
			get
			{
				return this._namespaceName;
			}
		}

		public override bool Equals(object obj)
		{
			TypeDeclaration td = obj as TypeDeclaration;
			if (td != null)
				return (this.Name == td.Name && this.Namespace == td.Namespace && this.AssemblyId == td.AssemblyId);
			else
				return false;
		}
	}

}
