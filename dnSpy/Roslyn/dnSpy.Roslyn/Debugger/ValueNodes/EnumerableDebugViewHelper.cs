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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Debugger.ValueNodes {
	static class EnumerableDebugViewHelper {
		/// <summary>
		/// Returns an enumerable debug view. These types are located in System.Core / System.Linq. If <paramref name="enumerableType"/>
		/// is <see cref="System.Collections.IEnumerable"/>, then <c>System.Linq.SystemCore_EnumerableDebugView</c>'s constructor
		/// is returned, else if <paramref name="enumerableType"/> is <see cref="IEnumerable{T}"/>, then
		/// <c>System.Linq.SystemCore_EnumerableDebugView`1</c>'s constructor is returned.
		/// </summary>
		/// <param name="enumerableType">Enumerable type, must be one of <see cref="System.Collections.IEnumerable"/>, <see cref="IEnumerable{T}"/></param>
		/// <returns></returns>
		public static DmdConstructorInfo GetEnumerableDebugViewConstructor(DmdType enumerableType) {
			Debug.Assert(enumerableType == enumerableType.AppDomain.System_Collections_IEnumerable ||
				(enumerableType.IsConstructedGenericType && enumerableType.GetGenericTypeDefinition() == enumerableType.AppDomain.System_Collections_Generic_IEnumerable_T));

			var appDomain = enumerableType.AppDomain;

			var wellKnownType = enumerableType.IsConstructedGenericType ?
				DmdWellKnownType.System_Linq_SystemCore_EnumerableDebugView_T :
				DmdWellKnownType.System_Linq_SystemCore_EnumerableDebugView;
			var debugViewType = appDomain.GetWellKnownType(wellKnownType, isOptional: true);
			// If this fails, System.Core (.NET Framework) / System.Linq (.NET Core) hasn't been loaded yet
			// or the type doesn't exist in the assembly (Unity)
			if ((object)debugViewType == null)
				return null;

			if (enumerableType.IsConstructedGenericType) {
				var genericArgs = enumerableType.GetGenericArguments();
				if (debugViewType.GetGenericArguments().Count != genericArgs.Count)
					return null;
				debugViewType = debugViewType.MakeGenericType(genericArgs);
			}

			var ctor = debugViewType.GetConstructor(DmdBindingFlags.Public | DmdBindingFlags.NonPublic | DmdBindingFlags.Instance, DmdCallingConventions.Standard | DmdCallingConventions.HasThis, new[] { enumerableType });
			Debug.Assert((object)ctor != null);
			return ctor;
		}

		public static string GetDebugViewTypeDisplayName(DmdType enumerableType) =>
			enumerableType.IsConstructedGenericType ? "System.Linq.SystemCore_EnumerableDebugView<T>" : "System.Linq.SystemCore_EnumerableDebugView";

		public static (DmdConstructorInfo ctor, DmdMethodInfo toArrayMethod) GetListEnumerableMethods(DmdType objType, DmdType enumerableType) {
			Debug.Assert(enumerableType == enumerableType.AppDomain.System_Collections_IEnumerable ||
				(enumerableType.IsConstructedGenericType && enumerableType.GetGenericTypeDefinition() == enumerableType.AppDomain.System_Collections_Generic_IEnumerable_T));

			var appDomain = enumerableType.AppDomain;
			if (enumerableType.IsConstructedGenericType) {
				var genArgs = enumerableType.GetGenericArguments();
				Debug.Assert(genArgs.Count == 1);
				if (genArgs.Count != 1)
					return default;
				var type = appDomain.CorLib.GetType("System.Collections.Generic.List`1");
				Debug.Assert(type?.IsGenericTypeDefinition == true);
				if (type?.IsGenericTypeDefinition != true)
					return default;
				type = type.MakeGenericType(genArgs);
				var ctor = type.GetMethod(DmdConstructorInfo.ConstructorName, DmdSignatureCallingConvention.HasThis,
					0, appDomain.System_Void, new[] { enumerableType }, throwOnError: false) as DmdConstructorInfo;
				var toArrayMethd = type.GetMethod(nameof(List<int>.ToArray), DmdSignatureCallingConvention.HasThis,
					0, genArgs[0].MakeArrayType(), Array.Empty<DmdType>(), throwOnError: false) as DmdMethodInfo;

				Debug.Assert(((object)ctor != null) == ((object)toArrayMethd != null));
				if ((object)ctor == null || (object)toArrayMethd == null)
					return default;
				return (ctor, toArrayMethd);
			}
			else {
				var icollectionType = appDomain.GetWellKnownType(DmdWellKnownType.System_Collections_ICollection, isOptional: true);
				if (!objType.CanCastTo(icollectionType))
					return default;

				var type = appDomain.CorLib.GetType("System.Collections." + nameof(ArrayList));
				var ctor = type?.GetMethod(DmdConstructorInfo.ConstructorName, DmdSignatureCallingConvention.HasThis,
					0, appDomain.System_Void, new[] { icollectionType }, throwOnError: false) as DmdConstructorInfo;
				var toArrayMethd = type?.GetMethod(nameof(ArrayList.ToArray), DmdSignatureCallingConvention.HasThis,
					0, appDomain.System_Object.MakeArrayType(), Array.Empty<DmdType>(), throwOnError: false) as DmdMethodInfo;

				Debug.Assert(((object)ctor != null) == ((object)toArrayMethd != null));
				if ((object)ctor == null || (object)toArrayMethd == null)
					return default;
				return (ctor, toArrayMethd);
			}
		}
	}
}
