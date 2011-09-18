// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// This code is distributed under the MS-PL (for details please see \doc\MS-PL.txt)

using System;
using System.Linq;
using ICSharpCode.ILSpy;
using Mono.Cecil;
using Ricciolo.StylesExplorer.MarkupReflection;

namespace ILSpy.BamlDecompiler
{
	public class CecilType : IType
	{
		internal readonly TypeDefinition type;
		
		public CecilType(TypeDefinition type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			this.type = type;
		}
		
		public string AssemblyQualifiedName {
			get {
				return type.FullName +
					", " + type.Module.Assembly.FullName;
			}
		}
		
		public bool IsSubclassOf(IType type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			if (!(type is CecilType))
				return false;
			
			CecilType ct = (CecilType)type;
			
			var t = this.type;
			
			if (t == ct.type)
				return false;
			
			while (t != null) {
				if (t == ct.type)
					return true;
				foreach (var @interface in t.Interfaces) {
					var resolved = @interface.Resolve();
					if (resolved == ct.type)
						return true;
				}
				if (t.BaseType == null)
					break;
				
				t = t.BaseType.Resolve();
			}
			
			return false;
		}
		
		public bool Equals(IType type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			if (!(type is CecilType))
				return false;
			
			return this.type == ((CecilType)type).type;
		}
		
		public override string ToString()
		{
			return string.Format("[CecilType Type={0}]", type);
		}
		
		public IType BaseType {
			get {
				TypeDefinition td = type.BaseType.Resolve();
				if (td == null)
					throw new Exception("could not resolve '" + type.BaseType.FullName + "'!");
				
				return new CecilType(td);
			}
		}
	}
}
