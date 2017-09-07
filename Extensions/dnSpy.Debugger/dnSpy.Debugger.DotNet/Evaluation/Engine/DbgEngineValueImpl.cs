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
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	sealed class DbgEngineValueImpl : DbgEngineValue {
		readonly DbgDotNetValue value;

		public DbgEngineValueImpl(DbgDotNetValue value) => this.value = value ?? throw new ArgumentNullException(nameof(value));

		public override DbgSimpleValueType ValueType => throw new NotImplementedException();//TODO:
		public override bool HasRawValue => throw new NotImplementedException();//TODO:
		public override object RawValue => throw new NotImplementedException();//TODO:

		public override DbgRawAddressValue? GetRawAddressValue(bool onlyDataAddress) {
			throw new NotImplementedException();//TODO:
		}

		protected override void CloseCore() => value.Dispose();
	}
}
