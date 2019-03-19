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
using System.Collections.Generic;

namespace dnSpy.Roslyn.Debugger.Formatters.VisualBasic {
	static class Operators {
		static readonly Dictionary<string, string[]> nameToOperatorName = new Dictionary<string, string[]>(StringComparer.Ordinal) {
			{ "op_UnaryPlus", "Operator +".Split(' ') },
			{ "op_UnaryNegation", "Operator -".Split(' ') },
			{ "op_False", "Operator IsFalse".Split(' ') },
			{ "op_True", "Operator IsTrue".Split(' ') },
			{ "op_OnesComplement", "Operator Not".Split(' ') },
			{ "op_Addition", "Operator +".Split(' ') },
			{ "op_Subtraction", "Operator -".Split(' ') },
			{ "op_Multiply", "Operator *".Split(' ') },
			{ "op_Division", "Operator /".Split(' ') },
			{ "op_IntegerDivision", @"Operator \".Split(' ') },
			{ "op_Concatenate", "Operator &".Split(' ') },
			{ "op_Exponent", "Operator ^".Split(' ') },
			{ "op_RightShift", "Operator >>".Split(' ') },
			{ "op_LeftShift", "Operator <<".Split(' ') },
			{ "op_Equality", "Operator =".Split(' ') },
			{ "op_Inequality", "Operator <>".Split(' ') },
			{ "op_GreaterThan", "Operator >".Split(' ') },
			{ "op_GreaterThanOrEqual", "Operator >=".Split(' ') },
			{ "op_LessThan", "Operator <".Split(' ') },
			{ "op_LessThanOrEqual", "Operator <=".Split(' ') },
			{ "op_BitwiseAnd", "Operator And".Split(' ') },
			{ "op_Like", "Operator Like".Split(' ') },
			{ "op_Modulus", "Operator Mod".Split(' ') },
			{ "op_BitwiseOr", "Operator Or".Split(' ') },
			{ "op_ExclusiveOr", "Operator Xor".Split(' ') },
			{ "op_Implicit", "Widening Operator CType".Split(' ') },
			{ "op_Explicit", "Narrowing Operator CType".Split(' ') },
		};

		public static string[] TryGetOperatorInfo(string name) {
			nameToOperatorName.TryGetValue(name, out var list);
			return list;
		}
	}
}
