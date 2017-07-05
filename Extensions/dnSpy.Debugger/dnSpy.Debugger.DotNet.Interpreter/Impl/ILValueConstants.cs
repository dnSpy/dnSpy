/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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

using System.Runtime.CompilerServices;

namespace dnSpy.Debugger.DotNet.Interpreter.Impl {
	static class ILValueConstants {
		static ILValueConstants() {
			constantInt32Values = new ConstantInt32ILValue[sbyte.MaxValue - sbyte.MinValue + 1];
			for (int i = sbyte.MinValue; i <= sbyte.MaxValue; i++)
				constantInt32Values[i - sbyte.MinValue] = new ConstantInt32ILValue(i);
		}
		static readonly ConstantInt32ILValue[] constantInt32Values;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ConstantInt32ILValue GetInt32Constant(int value) {
			if (sbyte.MinValue <= value && value <= sbyte.MaxValue)
				return constantInt32Values[value - sbyte.MinValue];
			return new ConstantInt32ILValue(value);
		}
	}
}
