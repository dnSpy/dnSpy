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

// See coreclr/src/vm/siginfo.cpp

using System;
using System.Diagnostics;

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// <c>System.Runtime.InteropServices.TypeIdentifierAttribute</c> helper code used by <see cref="DmdSigComparer"/>
	/// </summary>
	static class TIAHelper {
		readonly struct Info {
			public readonly string Scope;
			public readonly string Identifier;

			public Info(string scope, string identifier) {
				Scope = scope;
				Identifier = identifier;
			}

			public bool Equals(Info other) => StringComparer.OrdinalIgnoreCase.Equals(Scope, other.Scope) && Identifier == other.Identifier;
		}

		static Info? GetInfo(DmdType td) {
			if ((object)td == null)
				return null;
			if (td.IsWindowsRuntime)
				return null;

			string scope = null, identifier = null;
			var tia = td.FindCustomAttribute("System.Runtime.InteropServices.TypeIdentifierAttribute", inherit: false);
			if (tia != null) {
				if (tia.ConstructorArguments.Count >= 2) {
					if (tia.ConstructorArguments[0].ArgumentType != td.AppDomain.System_String)
						return null;
					if (tia.ConstructorArguments[1].ArgumentType != td.AppDomain.System_String)
						return null;
					scope = tia.ConstructorArguments[0].Value as string;
					identifier = tia.ConstructorArguments[1].Value as string;
				}
			}
			else {
				bool isTypeLib = td.Module.Assembly.IsDefined("System.Runtime.InteropServices.ImportedFromTypeLibAttribute", inherit: false) ||
								td.Module.Assembly.IsDefined("System.Runtime.InteropServices.PrimaryInteropAssemblyAttribute", inherit: false);
				if (!isTypeLib)
					return null;
			}

			if (identifier == null) {
				DmdCustomAttributeData gca;
				if (td.IsInterface && td.IsImport)
					gca = td.FindCustomAttribute("System.Runtime.InteropServices.GuidAttribute", inherit: false);
				else
					gca = td.Module.Assembly.FindCustomAttribute("System.Runtime.InteropServices.GuidAttribute", inherit: false);
				if (gca == null)
					return null;
				if (gca.ConstructorArguments.Count < 1)
					return null;
				if (gca.ConstructorArguments[0].ArgumentType != td.AppDomain.System_String)
					return null;
				scope = gca.ConstructorArguments[0].Value as string;
				var ns = td.MetadataNamespace;
				var name = td.MetadataName;
				if (string.IsNullOrEmpty(ns))
					identifier = name;
				else
					identifier = ns + "." + name;
			}
			return new Info(scope, identifier);
		}

		static bool CheckEquivalent(DmdType td) {
			Debug.Assert((object)td != null);

			for (int i = 0; (object)td != null && i < 1000; i++) {
				if (i != 0) {
					var info = GetInfo(td);
					if (info == null)
						return false;
				}

				bool f;
				if (td.IsInterface)
					f = td.IsImport || td.IsDefined("System.Runtime.InteropServices.ComEventInterfaceAttribute", inherit: false);
				else
					f = td.IsValueType || IsDelegate(td);
				if (!f)
					return false;
				if (td.IsGenericTypeDefinition)
					return false;

				var declType = td.DeclaringType;
				if ((object)declType == null)
					return td.IsPublic;

				if (!td.IsNestedPublic)
					return false;
				td = declType;
			}

			return false;
		}

		public static bool IsTypeDefEquivalent(DmdType td) {
			Debug.Assert((object)td != null);
			if (GetInfo(td) == null)
				return false;
			return CheckEquivalent(td);
		}

		static bool IsDelegate(DmdType td) => td.BaseType == td.AppDomain.System_MulticastDelegate;

		public static bool Equivalent(DmdType td1, DmdType td2) {
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
					if ((object)bt1 == null || (object)bt2 == null)
						return false;
					if (IsDelegate(td1)) {
						if (!IsDelegate(td2))
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
				if ((object)td1 == null && (object)td2 == null)
					break;
				if ((object)td1 == null || (object)td2 == null)
					return false;
			}

			return true;
		}

		static bool DelegateEquals(DmdType td1, DmdType td2) {
			var invoke1 = td1.GetMethod("Invoke");
			var invoke2 = td2.GetMethod("Invoke");
			if ((object)invoke1 == null || (object)invoke2 == null)
				return false;

			//TODO: Compare method signatures. Prevent infinite recursion...

			return true;
		}

		static bool ValueTypeEquals(DmdType td1, DmdType td2, bool isEnum) {
			if (td1.DeclaredMethods.Count != 0 || td2.DeclaredMethods.Count != 0)
				return false;

			//TODO: Compare the fields. Prevent infinite recursion...

			return true;
		}
	}
}
