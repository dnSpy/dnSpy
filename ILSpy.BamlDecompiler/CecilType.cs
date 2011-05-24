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
				throw new ArgumentException("type has to be a CecilType");
			
			CecilType ct = (CecilType)type;
			
			var t = ct.type;
			
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
				throw new ArgumentException("type has to be a CecilType");
			
			return this.type == ((CecilType)type).type;
		}
		
		public override string ToString()
		{
			return string.Format("[CecilType Type={0}]", type);
		}
	}
}
