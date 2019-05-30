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

using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine.Interpreter {
	sealed class RuntimeTypeHandleILValue : TypeILValueImpl {
		readonly DmdType type;

		public RuntimeTypeHandleILValue(DebuggerRuntimeImpl runtime, DmdType type)
			: base(runtime, type.AppDomain.GetWellKnownType(DmdWellKnownType.System_RuntimeTypeHandle)) {
			this.type = type;
		}

		protected override DbgDotNetValue? CreateObjValue() => runtime.CreateRuntimeTypeHandleCore(type);
	}

	sealed class RuntimeFieldHandleILValue : TypeILValueImpl {
		public DmdFieldInfo Field { get; }

		public RuntimeFieldHandleILValue(DebuggerRuntimeImpl runtime, DmdFieldInfo field)
			: base(runtime, field.AppDomain.GetWellKnownType(DmdWellKnownType.System_RuntimeFieldHandle)) {
			Field = field;
		}

		protected override DbgDotNetValue? CreateObjValue() => runtime.CreateRuntimeFieldHandleCore(Field);
	}

	sealed class RuntimeMethodHandleILValue : TypeILValueImpl {
		readonly DmdMethodBase method;

		public RuntimeMethodHandleILValue(DebuggerRuntimeImpl runtime, DmdMethodBase method)
			: base(runtime, method.AppDomain.GetWellKnownType(DmdWellKnownType.System_RuntimeMethodHandle)) {
			this.method = method;
		}

		protected override DbgDotNetValue? CreateObjValue() => runtime.CreateRuntimeMethodHandleCore(method);
	}
}
