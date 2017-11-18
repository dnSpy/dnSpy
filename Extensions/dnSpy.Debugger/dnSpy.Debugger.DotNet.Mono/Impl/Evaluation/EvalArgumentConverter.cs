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
using dnSpy.Debugger.DotNet.Metadata;
using Mono.Debugger.Soft;

namespace dnSpy.Debugger.DotNet.Mono.Impl.Evaluation {
	struct EvalArgumentResult {
		public string ErrorMessage { get; }
		public Value Value { get; }
		public EvalArgumentResult(string errorMessage) {
			ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
			Value = null;
		}
		public EvalArgumentResult(Value value) {
			ErrorMessage = null;
			Value = value ?? throw new ArgumentNullException(nameof(value));
		}
	}

	struct EvalArgumentConverter {
		readonly DbgEngineImpl engine;
		readonly AppDomainMirror appDomain;
		readonly DmdAppDomain reflectionAppDomain;

		public EvalArgumentConverter(DbgEngineImpl engine, AppDomainMirror appDomain, DmdAppDomain reflectionAppDomain) {
			this.engine = engine;
			this.appDomain = appDomain;
			this.reflectionAppDomain = reflectionAppDomain;
		}

		public unsafe EvalArgumentResult Convert(object value, DmdType defaultType, out DmdType type) {
			if (value == null) {
				type = defaultType;
				return new EvalArgumentResult(new PrimitiveValue(appDomain.VirtualMachine, ElementType.Object, null));
			}
			if (value is DbgValue dbgValue)
				value = dbgValue.InternalValue;
			if (value is DbgDotNetValueImpl dnValueImpl) {
				type = dnValueImpl.Type;
				return new EvalArgumentResult(dnValueImpl.Value);
			}
			if (value is DbgDotNetValue dnValue) {
				var rawValue = dnValue.GetRawValue();
				if (rawValue.HasRawValue) {
					value = rawValue.RawValue;
					if (value == null) {
						type = defaultType;
						return new EvalArgumentResult(new PrimitiveValue(appDomain.VirtualMachine, ElementType.Object, null));
					}
				}
			}
			if (value is string s) {
				type = reflectionAppDomain.System_String;
				return new EvalArgumentResult(appDomain.CreateString(s));
			}

			//TODO:
			type = defaultType;
			return new EvalArgumentResult(PredefinedEvaluationErrorMessages.InternalDebuggerError);
		}
	}
}
