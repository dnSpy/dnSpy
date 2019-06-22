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
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Debugger.Formatters {
	readonly struct ToStringFormatter {
		readonly DbgEvaluationInfo evalInfo;

		public ToStringFormatter(DbgEvaluationInfo evalInfo) => this.evalInfo = evalInfo ?? throw new ArgumentNullException(nameof(evalInfo));

		sealed class ToStringState {
			public readonly DmdMethodInfo? ToStringMethod;
			public ToStringState(DmdMethodInfo? toStringMethod) => ToStringMethod = toStringMethod;
		}

		ToStringState GetToStringState(DmdType type) {
			if (type.TryGetData(out ToStringState? state))
				return state;
			return CreateToStringState(type);

			ToStringState CreateToStringState(DmdType type2) {
				var appDomain = type2.AppDomain;
				var method = type2.GetMethod(nameof(object.ToString), DmdSignatureCallingConvention.Default | DmdSignatureCallingConvention.HasThis, 0, appDomain.System_String, Array.Empty<DmdType>(), throwOnError: false) as DmdMethodInfo;
				if (!(method is null)) {
					if (method.DeclaringType == appDomain.System_Object || method.DeclaringType == appDomain.System_ValueType || method.DeclaringType == appDomain.System_Enum)
						method = null;
				}
				return type2.GetOrCreateData(() => new ToStringState(method));
			}
		}

		public string? GetToStringValue(DbgDotNetValue value) {
			if (value.IsNull)
				return null;

			var state = GetToStringState(value.Type);
			if (state.ToStringMethod is null)
				return null;

			var runtime = evalInfo.Runtime.GetDotNetRuntime();
			var res = runtime.Call(evalInfo, value, state.ToStringMethod, Array.Empty<object>(), DbgDotNetInvokeOptions.None);
			if (res.HasError || res.ValueIsException)
				return null;

			var rawValue = res.Value!.GetRawValue();
			if (!rawValue.HasRawValue)
				return null;
			return rawValue.RawValue as string;
		}
	}
}
