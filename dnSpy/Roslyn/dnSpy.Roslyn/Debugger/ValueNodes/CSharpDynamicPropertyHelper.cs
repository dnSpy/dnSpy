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

using System.Diagnostics;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Debugger.ValueNodes {
	static class CSharpDynamicPropertyHelper {
		public static bool IsCSharpDynamicProperty(DmdType type) =>
			type.MetadataNamespace is null &&
			type.MetadataName == "DynamicProperty" &&
			type.DeclaringType is DmdType declType &&
			declType.MetadataNamespace == "Microsoft.CSharp.RuntimeBinder" &&
			declType.MetadataName == "DynamicMetaObjectProviderDebugView" &&
			declType.DeclaringType is null;

		sealed class CSharpDynamicPropertyState {
			public bool Initialized;
			public DmdFieldInfo? NameField;
			public DmdFieldInfo? ValueField;
		}

		public static (string name, DbgDotNetValue value, DmdFieldInfo valueField) GetRealValue(DbgEvaluationInfo evalInfo, DbgDotNetValue propValue) {
			var type = propValue.Type;
			Debug.Assert(IsCSharpDynamicProperty(type));
			var state = type.GetOrCreateData<CSharpDynamicPropertyState>();
			if (!state.Initialized) {
				state.Initialized = true;
				state.NameField = type.GetField(KnownMemberNames.DynamicProperty_Name_FieldName, type.AppDomain.System_String, throwOnError: false);
				state.ValueField = type.GetField(KnownMemberNames.DynamicProperty_Value_FieldName, type.AppDomain.System_Object, throwOnError: false);
			}
			if (state.NameField is null || state.ValueField is null)
				return default;

			DbgDotNetValueResult nameValue = default;
			DbgDotNetValueResult valueValue = default;
			bool error = true;
			try {
				var runtime = evalInfo.Runtime.GetDotNetRuntime();
				nameValue = runtime.LoadField(evalInfo, propValue, state.NameField);
				if (!nameValue.IsNormalResult)
					return default;
				valueValue = runtime.LoadField(evalInfo, propValue, state.ValueField);
				if (!valueValue.IsNormalResult)
					return default;
				var rawValue = nameValue.Value!.GetRawValue();
				if (!rawValue.HasRawValue || rawValue.ValueType != DbgSimpleValueType.StringUtf16 || !(rawValue.RawValue is string realName))
					return default;

				error = false;
				return (realName, valueValue.Value!, state.ValueField);
			}
			finally {
				nameValue.Value?.Dispose();
				if (error)
					valueValue.Value?.Dispose();
			}
		}
	}
}
