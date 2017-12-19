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

using System.Diagnostics;
using System.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Shared.Debugger.ValueNodes {
	static class CSharpDynamicPropertyHelper {
		public static bool IsCSharpDynamicProperty(DmdType type) =>
			type.MetadataNamespace == null &&
			type.MetadataName == "DynamicProperty" &&
			type.DeclaringType is DmdType declType &&
			declType.MetadataNamespace == "Microsoft.CSharp.RuntimeBinder" &&
			declType.MetadataName == "DynamicMetaObjectProviderDebugView" &&
			(object)declType.DeclaringType == null;

		sealed class CSharpDynamicPropertyState {
			public bool Initialized;
			public DmdFieldInfo NameField;
			public DmdFieldInfo ValueField;
		}

		public static (string name, DbgDotNetValue value) GetRealValue(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetValue propValue, CancellationToken cancellationToken) {
			var type = propValue.Type;
			Debug.Assert(IsCSharpDynamicProperty(type));
			var state = type.GetOrCreateData<CSharpDynamicPropertyState>();
			if (!state.Initialized) {
				state.Initialized = true;
				state.NameField = type.GetField("name", type.AppDomain.System_String, throwOnError: false);
				state.ValueField = type.GetField("value", type.AppDomain.System_Object, throwOnError: false);
			}
			if ((object)state.NameField == null || (object)state.ValueField == null)
				return default;

			DbgDotNetValueResult nameValue = default;
			DbgDotNetValueResult valueValue = default;
			bool error = true;
			try {
				var runtime = context.Runtime.GetDotNetRuntime();
				nameValue = runtime.LoadField(context, frame, propValue, state.NameField, cancellationToken);
				if (!nameValue.IsNormalResult)
					return default;
				valueValue = runtime.LoadField(context, frame, propValue, state.ValueField, cancellationToken);
				if (!valueValue.IsNormalResult)
					return default;
				var rawValue = nameValue.Value.GetRawValue();
				if (!rawValue.HasRawValue || rawValue.ValueType != DbgSimpleValueType.StringUtf16 || !(rawValue.RawValue is string realName))
					return default;

				error = false;
				return (realName, valueValue.Value);
			}
			finally {
				nameValue.Value?.Dispose();
				if (error)
					valueValue.Value?.Dispose();
			}
		}
	}
}
