/*
    Copyright (C) 2014-2019 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

// Copied from dnlib

// See coreclr/src/vm/siginfo.cpp

using System;
using System.Diagnostics;
using dnlib.DotNet;

namespace dnSpy.AsmEditor.Compiler {
	/// <summary>
	/// <c>System.Runtime.InteropServices.TypeIdentifierAttribute</c> helper code used by <see cref="SigComparer"/>
	/// </summary>
	static class TIAHelper {
		struct Info : IEquatable<Info> {
			public readonly UTF8String Scope;
			public readonly UTF8String Identifier;

			public Info(UTF8String scope, UTF8String identifier) {
				this.Scope = scope;
				this.Identifier = identifier;
			}

			public bool Equals(Info other) {
				return stricmp(Scope, other.Scope) &&
					UTF8String.Equals(Identifier, other.Identifier);
			}

			static bool stricmp(UTF8String a, UTF8String b) {
				var da = (object)a == null ? null : a.Data;
				var db = (object)b == null ? null : b.Data;
				if (da == db)
					return true;
				if (da == null || db == null)
					return false;
				if (da.Length != db.Length)
					return false;
				for (int i = 0; i < da.Length; i++) {
					byte ba = da[i], bb = db[i];
					if ((byte)'A' <= ba && ba <= (byte)'Z')
						ba = (byte)(ba - 'A' + 'a');
					if ((byte)'A' <= bb && bb <= (byte)'Z')
						bb = (byte)(bb - 'A' + 'a');
					if (ba != bb)
						return false;
				}
				return true;
			}
		}

		static Info? GetInfo(TypeDef td) {
			if (td == null)
				return null;
			if (td.IsWindowsRuntime)
				return null;

			UTF8String scope = null, identifier = null;
			var tia = td.CustomAttributes.Find("System.Runtime.InteropServices.TypeIdentifierAttribute");
			if (tia != null) {
				if (tia.ConstructorArguments.Count >= 2) {
					if (tia.ConstructorArguments[0].Type.GetElementType() != ElementType.String)
						return null;
					if (tia.ConstructorArguments[1].Type.GetElementType() != ElementType.String)
						return null;
					scope = tia.ConstructorArguments[0].Value as UTF8String ?? tia.ConstructorArguments[0].Value as string;
					identifier = tia.ConstructorArguments[1].Value as UTF8String ?? tia.ConstructorArguments[1].Value as string;
				}
			}
			else {
				var mod = td.Module;
				var asm = mod == null ? null : mod.Assembly;
				if (asm == null)
					return null;
				bool isTypeLib = asm.CustomAttributes.IsDefined("System.Runtime.InteropServices.ImportedFromTypeLibAttribute") ||
								asm.CustomAttributes.IsDefined("System.Runtime.InteropServices.PrimaryInteropAssemblyAttribute");
				if (!isTypeLib)
					return null;
			}

			if (UTF8String.IsNull(identifier)) {
				CustomAttribute gca;
				if (td.IsInterface && td.IsImport)
					gca = td.CustomAttributes.Find("System.Runtime.InteropServices.GuidAttribute");
				else {
					var mod = td.Module;
					var asm = mod == null ? null : mod.Assembly;
					if (asm == null)
						return null;
					gca = asm.CustomAttributes.Find("System.Runtime.InteropServices.GuidAttribute");
				}
				if (gca == null)
					return null;
				if (gca.ConstructorArguments.Count < 1)
					return null;
				if (gca.ConstructorArguments[0].Type.GetElementType() != ElementType.String)
					return null;
				scope = gca.ConstructorArguments[0].Value as UTF8String ?? gca.ConstructorArguments[0].Value as string;
				var ns = td.Namespace;
				var name = td.Name;
				if (UTF8String.IsNullOrEmpty(ns))
					identifier = name;
				else if (UTF8String.IsNullOrEmpty(name))
					identifier = new UTF8String(Concat(ns.Data, (byte)'.', empty));
				else
					identifier = new UTF8String(Concat(ns.Data, (byte)'.', name.Data));
			}
			return new Info(scope, identifier);
		}
		static readonly byte[] empty = new byte[0];

		static byte[] Concat(byte[] a, byte b, byte[] c) {
			var data = new byte[a.Length + 1 + c.Length];
			for (int i = 0; i < a.Length; i++)
				data[i] = a[i];
			data[a.Length] = b;
			for (int i = 0, j = a.Length + 1; i < c.Length; i++, j++)
				data[j] = c[i];
			return data;
		}

		static bool CheckEquivalent(TypeDef td) {
			Debug.Assert(td != null);

			for (int i = 0; td != null && i < 1000; i++) {
				if (i != 0) {
					var info = GetInfo(td);
					if (info == null)
						return false;
				}

				bool f;
				if (td.IsInterface)
					f = td.IsImport || td.CustomAttributes.IsDefined("System.Runtime.InteropServices.ComEventInterfaceAttribute");
				else
					f = td.IsValueType || td.IsDelegate;
				if (!f)
					return false;
				if (td.GenericParameters.Count > 0)
					return false;

				var declType = td.DeclaringType;
				if (declType == null)
					return td.IsPublic;

				if (!td.IsNestedPublic)
					return false;
				td = declType;
			}

			return false;
		}

		public static bool Equivalent(TypeDef td1, TypeDef td2) {
			var info1 = GetInfo(td1);
			if (info1 == null)
				return false;
			var info2 = GetInfo(td2);
			if (info2 == null)
				return false;
			if (!CheckEquivalent(td1) || !CheckEquivalent(td2))
				return false;
			if (!info1.Value.Equals(info2.Value))
				return false;

			// Caller has already compared names of the types and any declaring types

			for (int i = 0; i < 1000; i++) {
				if (td1.IsInterface) {
					if (!td2.IsInterface)
						return false;
				}
				else {
					var bt1 = td1.BaseType;
					var bt2 = td2.BaseType;
					if (bt1 == null || bt2 == null)
						return false;
					if (td1.IsDelegate) {
						if (!td2.IsDelegate)
							return false;
						if (!DelegateEquals(td1, td2))
							return false;
					}
					else if (td1.IsValueType) {
						if (td1.IsEnum != td2.IsEnum)
							return false;
						if (!td2.IsValueType)
							return false;
						if (!ValueTypeEquals(td1, td2, td1.IsEnum))
							return false;
					}
					else
						return false;
				}

				td1 = td1.DeclaringType;
				td2 = td2.DeclaringType;
				if (td1 == null && td2 == null)
					break;
				if (td1 == null || td2 == null)
					return false;
			}

			return true;
		}

		static bool DelegateEquals(TypeDef td1, TypeDef td2) {
			var invoke1 = td1.FindMethod(InvokeString);
			var invoke2 = td2.FindMethod(InvokeString);
			if (invoke1 == null || invoke2 == null)
				return false;

			//TODO: Compare method signatures. Prevent infinite recursion...

			return true;
		}
		static readonly UTF8String InvokeString = new UTF8String("Invoke");

		static bool ValueTypeEquals(TypeDef td1, TypeDef td2, bool isEnum) {
			if (td1.Methods.Count != 0 || td2.Methods.Count != 0)
				return false;

			//TODO: Compare the fields. Prevent infinite recursion...

			return true;
		}

		public static bool IsTypeDefEquivalent(TypeDef td) {
			if (TIAHelper.GetInfo(td) == null)
				return false;
			return TIAHelper.CheckEquivalent(td);
		}
	}
}
