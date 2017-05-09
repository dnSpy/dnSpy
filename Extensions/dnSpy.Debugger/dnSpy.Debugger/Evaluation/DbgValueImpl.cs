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
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation.Engine;

namespace dnSpy.Debugger.Evaluation {
	sealed class DbgValueImpl : DbgValue {
		public override DbgThread Thread { get; }
		public override DbgSimpleValueType ValueType => EngineValue.ValueType;
		public override bool HasRawValue => EngineValue.HasRawValue;
		public override object RawValue => EngineValue.RawValue;

		internal DbgEngineValue EngineValue { get; }

		public DbgValueImpl(DbgThread thread, DbgEngineValue engineValue) {
			Thread = thread ?? throw new ArgumentNullException(nameof(thread));
			EngineValue = engineValue ?? throw new ArgumentNullException(nameof(engineValue));
		}

		public override DbgRawAddressValue? GetRawAddressValue(bool onlyDataAddress) => EngineValue.GetRawAddressValue(onlyDataAddress);
		public override void Close() => Process.DbgManager.Close(this);
		protected override void CloseCore() => EngineValue.Close(Process.DbgManager.Dispatcher);
	}
}
