/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using dnlib.DotNet;
using dnSpy.Contracts.Scripting.Debugger;

namespace dnSpy.Debugger.Scripting {
	/// <summary>
	/// Simple type comparer
	/// </summary>
	struct TypeComparer {
		public new bool Equals(object a, object b) {
			return TypeToString(a) == TypeToString(b);
		}

		public bool ArgListsEquals(IList<TypeSig> a, object[] b) {
			if (a.Count != b.Length)
				return false;
			for (int i = 0; i < b.Length; i++) {
				if (!Equals(a[i], b[i]))
					return false;
			}
			return true;
		}

		public bool PropertySignatureEquals(IDebuggerProperty a, PropertyInfo b) {
			var ag = a.Getter;
			var bg = b.GetMethod;
			if ((ag == null) != (bg == null))
				return false;
			if (ag != null) {
				if (!MethodSigEquals(ag.MethodSig, bg))
					return false;
			}

			// Should be enough, even if there's no getter

			return true;
		}

		public bool MethodSigEquals(MethodBaseSig sig, MethodBase method) {
			if (sig == null || method == null)
				return false;
			if (sig.HasThis != !method.IsStatic)
				return false;
			if (sig.Generic != method.IsGenericMethod)
				return false;
			if (sig.Generic) {
				if (sig.GenParamCount != method.GetGenericArguments().Length)
					return false;
			}
			if (method.IsMethodSpec())
				method = method.Module.ResolveMethod(method.MetadataToken) ?? method;
			var mps = method.GetParameters();
			if (sig.Params.Count != mps.Length)
				return false;

			var minfo = method as MethodInfo;
			if (minfo != null) {
				if (!Equals(sig.RetType, minfo.ReturnType))
					return false;
			}
			else {
				if (sig.RetType.RemovePinnedAndModifiers().GetElementType() != ElementType.Void)
					return false;
			}

			for (int i = 0; i < mps.Length; i++) {
				if (!Equals(sig.Params[i], mps[i].ParameterType))
					return false;
			}

			return true;
		}

		static string TypeToString(object a) {
			if (a == null)
				return null;

			var st = a as Type;
			if (st != null)
				return SystemTypeToString(st);

			var dt = a as IType;
			if (dt != null)
				return DnLibTypeToString(dt);

			var s = a as string;
			if (s != null)
				return s;

			Debug.Fail("Unsupported type");
			return a.ToString();
		}

		static string SystemTypeToString(Type a) {
			if (a == null)
				return null;

			// PERF: No need to import since FullName prop will be identical to dnlib's FullName
			// property if it's just a normal non-nested type
			if (!a.IsGenericType && !a.HasElementType && a.DeclaringType == null) {
				Debug.Assert(DnLibTypeToString(new Importer(dummyModule).Import(a)) == a.FullName);
				return a.FullName;
			}

			return DnLibTypeToString(new Importer(dummyModule).Import(a));
		}
		static readonly ModuleDef dummyModule = new ModuleDefUser("dummy");

		static string DnLibTypeToString(IType dt) {
			if (dt == null)
				return null;
			return dt.FullName;
		}
	}
}
