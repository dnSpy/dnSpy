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

using System.Reflection.Emit;

namespace dnSpy.Debugger.Native {
	static unsafe class Memset {
		delegate void MemsetDelegate(void* destination, int value, int size);
		static MemsetDelegate memsetDelegate;

		static Memset() {
			var dm = new DynamicMethod("memset.NET", typeof(void), new[] { typeof(void*), typeof(int), typeof(int) }, typeof(Memset), true);
			var ilg = dm.GetILGenerator();
			ilg.Emit(OpCodes.Ldarg_0);
			ilg.Emit(OpCodes.Ldarg_1);
			ilg.Emit(OpCodes.Ldarg_2);
			ilg.Emit(OpCodes.Initblk);
			ilg.Emit(OpCodes.Ret);
			memsetDelegate = (MemsetDelegate)dm.CreateDelegate(typeof(MemsetDelegate));
		}

		public static void Clear(void* destination, byte value, int size) => memsetDelegate(destination, value, size);
	}
}
