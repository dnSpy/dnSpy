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
using dnSpy.Contracts.Debugger.DotNet.Evaluation;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine.Interpreter {
	sealed class UnboxAddressILValue : AddressILValue {
		readonly DbgDotNetValue dnValue;

		public UnboxAddressILValue(DebuggerRuntimeImpl runtime, DbgDotNetValue dnValue)
			: base(runtime, dnValue.Type) {
			this.dnValue = dnValue;
		}

		protected override DbgDotNetValue? ReadValue() => dnValue;
		protected override void WriteValue(object? value) => throw new NotImplementedException();

		public override bool Equals(AddressILValue other) =>
			other is UnboxAddressILValue addr &&
			runtime.Equals(dnValue, addr.dnValue) == true;
	}
}
