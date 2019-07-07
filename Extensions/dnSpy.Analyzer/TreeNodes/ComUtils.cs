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

using System;
using dnlib.DotNet;

namespace dnSpy.Analyzer.TreeNodes {
	static class ComUtils {
		public static bool IsComType(TypeDef type, out Guid guid) {
			guid = default;

			// type.IsImport (== ComImportAttribute) isn't checked since the CLR doesn't require
			// it to be set. If it has a GuidAttribute, it's probably a COM type.

			if (!type.IsInterface)
				return false;

			var ca = type.CustomAttributes.Find("System.Runtime.InteropServices.GuidAttribute");
			if (ca is null || ca.ConstructorArguments.Count != 1)
				return false;
			if (!(ca.ConstructorArguments[0].Value is UTF8String guidStr) || !Guid.TryParse(guidStr, out guid))
				return false;

			return true;
		}

		public static bool ComEquals(TypeDef type, ref Guid guid) {
			// type.IsImport (== ComImportAttribute) isn't checked since the CLR doesn't require
			// it to be set. If it has a GuidAttribute, it's probably a COM type.

			if (!type.IsInterface)
				return false;

			var ca = type.CustomAttributes.Find("System.Runtime.InteropServices.GuidAttribute");
			if (ca is null || ca.ConstructorArguments.Count != 1)
				return false;
			if (!(ca.ConstructorArguments[0].Value is UTF8String guidStr) || !Guid.TryParse(guidStr, out var g))
				return false;

			return g == guid;
		}

		static bool IsVtblGap(MethodDef method) => IsVtblGap(method, out _);

		public static bool IsVtblGap(MethodDef method, out int count) {
			count = 0;
			if (!(method.IsVirtual || method.IsAbstract))
				return false;
			if (!method.IsRuntimeSpecialName)
				return false;
			string name = method.Name;
			const string vtblGapPrefix = "_VtblGap";
			if (name is null || !name.StartsWith(vtblGapPrefix))
				return false;
			int i = vtblGapPrefix.Length;
			while (i < name.Length) {
				var c = name[i];
				if (!('0' <= c && c <= '9'))
					break;
				i++;
			}
			if (i == name.Length)
				count = 1;
			else {
				if (name[i++] != '_')
					return false;
				if (i == name.Length)
					return false;
				while (i < name.Length) {
					var c = name[i];
					if (!('0' <= c && c <= '9'))
						break;
					count = (ushort)((uint)count * 10 + c - '0');
					i++;
				}
				if (i != name.Length)
					return false;
			}
			return true;
		}

		static int GetVtblIndex(MethodDef method) {
			int vtblIndex = 0;
			var methods = method.DeclaringType.Methods;
			for (int i = 0; i < methods.Count; i++) {
				var m = methods[i];
				if (!(m.IsVirtual || m.IsAbstract))
					continue;
				if (m == method)
					return vtblIndex;
				if (IsVtblGap(m, out var count))
					vtblIndex += count;
				else
					vtblIndex++;
			}
			return -1;
		}

		public static void GetMemberInfo(MethodDef method, out bool isComType, out Guid comGuid, out int vtblIndex) {
			comGuid = default;
			isComType = (method.IsVirtual || method.IsAbstract) && !IsVtblGap(method) && IsComType(method.DeclaringType, out comGuid);
			if (isComType)
				vtblIndex = GetVtblIndex(method);
			else
				vtblIndex = -1;
		}

		public static MethodDef? GetMethod(TypeDef type, int vtblIndex) {
			int currentVtblIndex = 0;
			var methods = type.Methods;
			for (int i = 0; i < methods.Count; i++) {
				var method = methods[i];
				if (!(method.IsVirtual || method.IsAbstract))
					continue;
				if (!IsVtblGap(method, out int count)) {
					if (currentVtblIndex == vtblIndex)
						return method;
					count = 1;
				}
				currentVtblIndex += count;
				if (currentVtblIndex > vtblIndex)
					break;
			}
			return null;
		}
	}
}
