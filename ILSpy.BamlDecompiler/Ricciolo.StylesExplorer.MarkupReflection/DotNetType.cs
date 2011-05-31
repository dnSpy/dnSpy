// Copyright (c) Cristian Civera (cristian@aspitalia.com)
// This code is distributed under the MS-PL (for details please see \doc\MS-PL.txt)

using System;
using System.Collections.Generic;
using System.Text;

namespace Ricciolo.StylesExplorer.MarkupReflection
{
	public class DotNetType : MarshalByRefObject, IType
	{
		private readonly string _assemblyQualifiedName;
		private Type _type;

		public DotNetType(string assemblyQualifiedName)
		{
			if (assemblyQualifiedName == null) throw new ArgumentNullException("assemblyQualifiedName");

			_assemblyQualifiedName = assemblyQualifiedName;
			_type = Type.GetType(assemblyQualifiedName, false, true);
		}

		#region IType Members

		public string AssemblyQualifiedName
		{
			get { return _assemblyQualifiedName; }
		}

		public bool IsSubclassOf(IType type)
		{
			if (type == null) throw new ArgumentNullException("type");
			if (!(type is DotNetType)) throw new ArgumentException("type");
			if (_type == null) return false;
			return this._type.IsSubclassOf(((DotNetType)type).Type);
		}

		public bool Equals(IType type)
		{
			if (type == null) throw new ArgumentNullException("type");
			if (!(type is DotNetType)) throw new ArgumentException("type");
			if (_type == null) return false;
			return this._type.Equals(((DotNetType)type).Type);
		}
		
		public IType BaseType {
			get {
				return new DotNetType(this._type.BaseType.AssemblyQualifiedName);
			}
		}

		#endregion

		public Type Type
		{
			get { return _type; }
		}

		public override object InitializeLifetimeService()
		{
			return null;
		}
	}
}
