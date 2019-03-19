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

using System.Collections.Generic;
using dnlib.DotNet;

namespace dnSpy.Decompiler {
	readonly struct FormatterMethodInfo {
		public readonly ModuleDef ModuleDef;
		public readonly IList<TypeSig> TypeGenericParams;
		public readonly IList<TypeSig> MethodGenericParams;
		public readonly MethodDef MethodDef;
		public readonly MethodSig MethodSig;
		public readonly bool RetTypeIsLastArgType;
		public readonly bool IncludeReturnTypeInArgsList;

		public FormatterMethodInfo(IMethod method, bool retTypeIsLastArgType = false, bool includeReturnTypeInArgsList = false) {
			ModuleDef = method.Module;
			TypeGenericParams = null;
			MethodGenericParams = null;
			MethodSig = method.MethodSig ?? new MethodSig(CallingConvention.Default);
			RetTypeIsLastArgType = retTypeIsLastArgType;
			IncludeReturnTypeInArgsList = includeReturnTypeInArgsList;

			MethodDef = method as MethodDef;
			var ms = method as MethodSpec;
			var mr = method as MemberRef;
			if (ms != null) {
				var ts = ms.Method == null ? null : ms.Method.DeclaringType as TypeSpec;
				if (ts != null) {
					if (ts.TypeSig.RemovePinnedAndModifiers() is GenericInstSig gp)
						TypeGenericParams = gp.GenericArguments;
				}

				var gsSig = ms.GenericInstMethodSig;
				if (gsSig != null)
					MethodGenericParams = gsSig.GenericArguments;

				MethodDef = ms.Method.ResolveMethodDef();
			}
			else if (mr != null) {
				if (mr.DeclaringType is TypeSpec ts) {
					if (ts.TypeSig.RemovePinnedAndModifiers() is GenericInstSig gp)
						TypeGenericParams = gp.GenericArguments;
				}

				MethodDef = mr.ResolveMethod();
			}
		}
	}
}
