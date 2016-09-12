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
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace dnSpy.Roslyn.Shared.Utilities {
	/// <summary>
	/// Roslyn uses <see cref="ImmutableArray{T}"/> but other parts of dnSpy don't. Even if
	/// all of dnSpy did, some code need to process some byte arrays before use, eg. by making
	/// every type public by modifying the metadata. Roslyn's use of <see cref="ImmutableArray{T}"/>
	/// will then lead to extra memory being allocated, and this memory would most likely be
	/// in the LOH (Large Object Heap). This class creates new <see cref="ImmutableArray{T}"/>s
	/// and overwrites the internal array field with our data to save memory.
	/// </summary>
	static class ImmutableArrayUtilities<T> {
		static readonly Func<T[], ImmutableArray<T>> delegateToImmutableArray;

		static ImmutableArrayUtilities() {
			delegateToImmutableArray = CreateImmutableArrayDelegate();
		}

		static Func<T[], ImmutableArray<T>> CreateImmutableArrayDelegate() {
			var immutableArrayType = typeof(ImmutableArray<T>);
			var immutableArrayFieldInfo = immutableArrayType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(a => a.FieldType == typeof(T[])).First();
			var dm = new DynamicMethod($"ImmutableArray<{typeof(T).Name}>", immutableArrayType, new Type[] { typeof(T[]) }, restrictedSkipVisibility: true);
			var ilg = dm.GetILGenerator();
			var local = ilg.DeclareLocal(immutableArrayType);
			ilg.Emit(OpCodes.Ldloca_S, local);
			ilg.Emit(OpCodes.Initobj, immutableArrayType);
			ilg.Emit(OpCodes.Ldloca_S, local);
			ilg.Emit(OpCodes.Ldarg_0);
			ilg.Emit(OpCodes.Stfld, immutableArrayFieldInfo);
			ilg.Emit(OpCodes.Ldloc_0);
			ilg.Emit(OpCodes.Ret);
			return (Func<T[], ImmutableArray<T>>)dm.CreateDelegate(typeof(Func<T[], ImmutableArray<T>>));
		}

		/// <summary>
		/// Creates a new <see cref="ImmutableArray{T}"/> by copying <paramref name="data"/>
		/// to the <see cref="ImmutableArray{T}"/>'s internal field. It doesn't waste any
		/// extra memory by allocating a duplicate array.
		/// </summary>
		/// <param name="data">Data to use</param>
		/// <returns></returns>
		public static ImmutableArray<T> ToImmutableArray(T[] data) => delegateToImmutableArray(data);
	}
}
