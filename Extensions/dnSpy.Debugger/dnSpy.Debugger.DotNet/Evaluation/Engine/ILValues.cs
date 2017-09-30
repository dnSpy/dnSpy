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
using dnSpy.Debugger.DotNet.Interpreter;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	interface IDebuggerRuntimeILValue {
		DbgDotNetValue GetDotNetValue();
	}

	sealed class ByRefILValueImpl : ByRefILValue, IDebuggerRuntimeILValue {
		readonly DbgDotNetValue value;
		public ByRefILValueImpl(DbgDotNetValue value) => this.value = value ?? throw new ArgumentNullException(nameof(value));
		public override DmdType Type => value.Type;
		DbgDotNetValue IDebuggerRuntimeILValue.GetDotNetValue() => value;
	}

	sealed class ObjectRefILValueImpl : ObjectRefILValue, IDebuggerRuntimeILValue {
		readonly DbgDotNetValue value;
		public ObjectRefILValueImpl(DbgDotNetValue value) => this.value = value ?? throw new ArgumentNullException(nameof(value));
		public override DmdType Type => value.Type;
		DbgDotNetValue IDebuggerRuntimeILValue.GetDotNetValue() => value;
	}

	sealed class ValueTypeILValueImpl : ValueTypeILValue, IDebuggerRuntimeILValue {
		readonly DbgDotNetValue value;
		public ValueTypeILValueImpl(DbgDotNetValue value) => this.value = value ?? throw new ArgumentNullException(nameof(value));
		public override ILValue Clone() => this;//TODO:
		public override DmdType Type => value.Type;
		DbgDotNetValue IDebuggerRuntimeILValue.GetDotNetValue() => value;
	}

	sealed class NullObjectRefILValueImpl : NullObjectRefILValue, IDebuggerRuntimeILValue {
		readonly DbgDotNetValue value;
		public NullObjectRefILValueImpl(DbgDotNetValue value) => this.value = value ?? throw new ArgumentNullException(nameof(value));
		public override DmdType Type => value.Type;
		DbgDotNetValue IDebuggerRuntimeILValue.GetDotNetValue() => value;
	}

	sealed class ConstantInt32ILValueImpl : ConstantInt32ILValue, IDebuggerRuntimeILValue {
		readonly DbgDotNetValue value;
		public ConstantInt32ILValueImpl(DbgDotNetValue value, int v) : base(value.Type, v) => this.value = value ?? throw new ArgumentNullException(nameof(value));
		DbgDotNetValue IDebuggerRuntimeILValue.GetDotNetValue() => value;
	}

	sealed class ConstantInt64ILValueImpl : ConstantInt64ILValue, IDebuggerRuntimeILValue {
		readonly DbgDotNetValue value;
		public ConstantInt64ILValueImpl(DbgDotNetValue value, long v) : base(value.Type, v) => this.value = value ?? throw new ArgumentNullException(nameof(value));
		DbgDotNetValue IDebuggerRuntimeILValue.GetDotNetValue() => value;
	}

	sealed class ConstantStringILValueImpl : ConstantStringILValue, IDebuggerRuntimeILValue {
		readonly DbgDotNetValue value;
		public ConstantStringILValueImpl(DbgDotNetValue value, string s) : base(value.Type.AppDomain, s) => this.value = value ?? throw new ArgumentNullException(nameof(value));
		DbgDotNetValue IDebuggerRuntimeILValue.GetDotNetValue() => value;
	}

	sealed class ConstantFloatILValueImpl : ConstantFloatILValue, IDebuggerRuntimeILValue {
		readonly DbgDotNetValue value;
		public ConstantFloatILValueImpl(DbgDotNetValue value, double v) : base(value.Type, v) => this.value = value ?? throw new ArgumentNullException(nameof(value));
		DbgDotNetValue IDebuggerRuntimeILValue.GetDotNetValue() => value;
	}

	sealed class ConstantNativeIntILValueImpl : ConstantNativeIntILValue, IDebuggerRuntimeILValue {
		readonly DbgDotNetValue value;
		public static ConstantNativeIntILValueImpl Create32(DbgDotNetValue value, int v) => new ConstantNativeIntILValueImpl(value, v);
		public static ConstantNativeIntILValueImpl Create64(DbgDotNetValue value, long v) => new ConstantNativeIntILValueImpl(value, v);
		ConstantNativeIntILValueImpl(DbgDotNetValue value, int v) : base(value.Type, v) => this.value = value ?? throw new ArgumentNullException(nameof(value));
		ConstantNativeIntILValueImpl(DbgDotNetValue value, long v) : base(value.Type, v) => this.value = value ?? throw new ArgumentNullException(nameof(value));
		DbgDotNetValue IDebuggerRuntimeILValue.GetDotNetValue() => value;
	}
}
