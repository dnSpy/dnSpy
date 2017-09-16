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

using System;
using System.Diagnostics;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;

namespace dnSpy.Roslyn.Shared.Debugger.ValueNodes {
	struct ArrayObjectValue : IDisposable {
		public readonly DbgDotNetValue Value;
		readonly bool ownsValue;
		public ArrayObjectValue(DbgDotNetValue value) {
			Debug.Assert(!value.IsNullReference);
			if (value.IsReference) {
				Value = value.Dereference();
				ownsValue = true;
			}
			else {
				Value = value;
				ownsValue = false;
			}
			Debug.Assert(Value.IsArray);
		}

		public void Dispose() {
			if (ownsValue)
				Value.Dispose();
		}
	}
}
