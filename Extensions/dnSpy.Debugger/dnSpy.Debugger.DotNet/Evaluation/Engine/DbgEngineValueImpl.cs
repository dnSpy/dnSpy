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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	sealed class DbgEngineValueImpl : DbgEngineValue {
		public override DbgSimpleValueType ValueType => DbgSimpleValueType.OtherReferenceType;//TODO:
		public override bool HasRawValue => false;//TODO:
		public override object RawValue => null;//TODO:
		internal DbgDotNetValue DotNetValue => value;

		readonly DbgDotNetValue value;

		public DbgEngineValueImpl(DbgDotNetValue value) => this.value = value ?? throw new ArgumentNullException(nameof(value));

		public override DbgRawAddressValue? GetRawAddressValue(bool onlyDataAddress) {
			return null;//TODO:
		}

		protected override void CloseCore(DbgDispatcher dispatcher) => value.Dispose();
	}
}
