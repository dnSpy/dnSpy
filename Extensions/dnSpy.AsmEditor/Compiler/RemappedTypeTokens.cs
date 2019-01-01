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
using System.Diagnostics;
using dnlib.DotNet;

namespace dnSpy.AsmEditor.Compiler {
	sealed class RemappedTypeTokens {
		readonly Dictionary<uint, uint> dict;
		uint minNestedToken, maxNestedToken;
		bool isReadOnly;
		uint[] arrayDict;
		readonly uint enclosingTypeToken;
		uint enclosingTypeNewToken;

		public int Count => dict.Count;

		public RemappedTypeTokens(TypeDef enclosingTypeOrNull) {
			dict = new Dictionary<uint, uint>();
			minNestedToken = uint.MaxValue;
			maxNestedToken = uint.MinValue;
			enclosingTypeToken = enclosingTypeOrNull?.MDToken.Raw ?? uint.MaxValue;
			enclosingTypeNewToken = uint.MaxValue;
		}

		public void Add(uint token, uint newToken) {
			Debug.Assert(!isReadOnly);
			if (enclosingTypeToken != token) {
				minNestedToken = Math.Min(minNestedToken, token);
				maxNestedToken = Math.Max(maxNestedToken, token);
			}
			dict.Add(token, newToken);
		}

		public void SetReadOnly() {
			Debug.Assert(!isReadOnly);
			isReadOnly = true;
			int nestCount = minNestedToken == uint.MaxValue && maxNestedToken == uint.MinValue ? 0 : (int)(maxNestedToken - minNestedToken + 1);
			if (nestCount + 1 == dict.Count) {
				arrayDict = nestCount == 0 ? Array.Empty<uint>() : new uint[nestCount];
				foreach (var kv in dict) {
					if (kv.Key == enclosingTypeToken)
						enclosingTypeNewToken = kv.Value;
					else
						arrayDict[(int)(kv.Key - minNestedToken)] = kv.Value;
				}
			}
		}

		public bool TryGetValue(uint token, out uint newToken) {
			Debug.Assert(isReadOnly);
			var arrayDict = this.arrayDict;
			if (arrayDict != null) {
				// Most likely code path

				if (token == enclosingTypeToken) {
					newToken = enclosingTypeNewToken;
					return true;
				}
				int index = (int)(token - minNestedToken);
				if ((uint)index < (uint)arrayDict.Length) {
					newToken = arrayDict[index];
					return true;
				}
			}
			else {
				// Unlikely code path (if it's a Microsoft compiler, a very likely path if it's mcs)

				if (token == enclosingTypeToken || (minNestedToken <= token && token <= maxNestedToken))
					return dict.TryGetValue(token, out newToken);
			}

			newToken = 0;
			return false;
		}
	}
}
