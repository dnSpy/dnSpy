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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	sealed class DbgEngineValueImpl : DbgEngineValue {
		public override object InternalValue => value;
		public override DbgSimpleValueType ValueType => value.GetRawValue().ValueType;
		public override bool HasRawValue => value.GetRawValue().HasRawValue;
		public override object RawValue => value.GetRawValue().RawValue;
		internal DbgDotNetValue DotNetValue => value;

		readonly DbgDotNetValue value;

		public DbgEngineValueImpl(DbgDotNetValue value) => this.value = value ?? throw new ArgumentNullException(nameof(value));

		public override DbgRawAddressValue? GetRawAddressValue(bool onlyDataAddress) => value.GetRawAddressValue(onlyDataAddress);
		protected override void CloseCore(DbgDispatcher dispatcher) => value.Dispose();
	}
}
