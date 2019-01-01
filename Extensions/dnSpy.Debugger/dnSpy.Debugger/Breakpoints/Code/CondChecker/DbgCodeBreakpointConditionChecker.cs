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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.Breakpoints.Code.CondChecker {
	abstract class DbgCodeBreakpointConditionChecker {
		public abstract DbgCodeBreakpointCheckResult ShouldBreak(DbgBoundCodeBreakpoint boundBreakpoint, DbgThread thread, DbgCodeBreakpointCondition condition);
	}

	[Export(typeof(DbgCodeBreakpointConditionChecker))]
	sealed class DbgCodeBreakpointConditionCheckerImpl : DbgCodeBreakpointConditionChecker {
		readonly DbgLanguageService dbgLanguageService;
		readonly DbgObjectIdService dbgObjectIdService;

		[ImportingConstructor]
		DbgCodeBreakpointConditionCheckerImpl(DbgLanguageService dbgLanguageService, DbgObjectIdService dbgObjectIdService) {
			this.dbgLanguageService = dbgLanguageService;
			this.dbgObjectIdService = dbgObjectIdService;
		}

		sealed class BreakpointState : IDisposable {
			public DbgLanguage Language;
			public DbgCodeBreakpointCondition Condition;

			public SavedValue SavedValue {
				get => savedValue;
				set {
					savedValue?.Dispose();
					savedValue = value;
				}
			}
			SavedValue savedValue;

			public DbgEvaluationContext Context {
				get => context;
				set {
					context?.Close();
					context = value;
				}
			}
			DbgEvaluationContext context;

			public object ExpressionEvaluatorState;

			public void Dispose() {
				Language = null;
				Condition = default;
				SavedValue = null;
				Context = null;
				ExpressionEvaluatorState = null;
			}
		}

		abstract class SavedValue {
			public abstract bool Equals(DbgEvaluationInfo evalInfo, SavedValue other);
			public abstract void Dispose();

			public static SavedValue TryCreateValue(DbgObjectIdService dbgObjectIdService, DbgValue value, string valueType) {
				switch (value.ValueType) {
				case DbgSimpleValueType.Other:
					if (value.HasRawValue && value.RawValue == null)
						return new SimpleSavedValue(value.ValueType, value.RawValue, valueType);
					var objectId = dbgObjectIdService.CreateObjectId(value, CreateObjectIdOptions.Hidden);
					if (objectId != null)
						return new ObjectIdSavedValue(dbgObjectIdService, objectId);
					var addr = value.GetRawAddressValue(onlyDataAddress: false);
					if (addr != null)
						return new AddressSavedValue(addr.Value, valueType);
					return null;

				case DbgSimpleValueType.Void:
				case DbgSimpleValueType.Boolean:
				case DbgSimpleValueType.Char1:
				case DbgSimpleValueType.CharUtf16:
				case DbgSimpleValueType.Int8:
				case DbgSimpleValueType.Int16:
				case DbgSimpleValueType.Int32:
				case DbgSimpleValueType.Int64:
				case DbgSimpleValueType.UInt8:
				case DbgSimpleValueType.UInt16:
				case DbgSimpleValueType.UInt32:
				case DbgSimpleValueType.UInt64:
				case DbgSimpleValueType.Float32:
				case DbgSimpleValueType.Float64:
				case DbgSimpleValueType.Decimal:
				case DbgSimpleValueType.Ptr32:
				case DbgSimpleValueType.Ptr64:
				case DbgSimpleValueType.StringUtf16:
				case DbgSimpleValueType.DateTime:
					return new SimpleSavedValue(value.ValueType, value.RawValue, valueType);

				default:
					Debug.Fail($"Unknown type: {value.ValueType}");
					return null;
				}
			}

			sealed class SimpleSavedValue : SavedValue {
				readonly DbgSimpleValueType type;
				readonly object value;
				readonly string valueType;

				public SimpleSavedValue(DbgSimpleValueType type, object value, string valueType) {
					this.type = type;
					this.value = value;
					// It's needed if it's an enum value since 'value' contains the underlying type value
					this.valueType = valueType;
				}

				public override bool Equals(DbgEvaluationInfo evalInfo, SavedValue other) {
					var obj = other as SimpleSavedValue;
					return obj != null &&
						obj.type == type &&
						Equals(obj.value, value) &&
						(value == null || obj.valueType == valueType);
				}

				public override void Dispose() { }
			}

			sealed class AddressSavedValue : SavedValue {
				readonly DbgRawAddressValue address;
				readonly string valueType;

				public AddressSavedValue(DbgRawAddressValue address, string valueType) {
					this.address = address;
					this.valueType = valueType;
				}

				public override bool Equals(DbgEvaluationInfo evalInfo, SavedValue other) =>
					other is AddressSavedValue obj &&
					obj.address.Address == address.Address &&
					obj.address.Length == address.Length &&
					obj.valueType == valueType;

				public override void Dispose() { }
			}

			sealed class ObjectIdSavedValue : SavedValue {
				readonly DbgObjectIdService dbgObjectIdService;
				readonly DbgObjectId objectId;

				public ObjectIdSavedValue(DbgObjectIdService dbgObjectIdService, DbgObjectId objectId) {
					this.dbgObjectIdService = dbgObjectIdService;
					this.objectId = objectId;
				}

				public override bool Equals(DbgEvaluationInfo evalInfo, SavedValue other) {
					var obj = other as ObjectIdSavedValue;
					if (obj == null)
						return false;
					var value = obj.objectId.GetValue(evalInfo);
					try {
						return dbgObjectIdService.Equals(objectId, value);
					}
					finally {
						value.Close();
					}
				}

				public override void Dispose() => objectId.Remove();
			}
		}

		public override DbgCodeBreakpointCheckResult ShouldBreak(DbgBoundCodeBreakpoint boundBreakpoint, DbgThread thread, DbgCodeBreakpointCondition condition) {
			var expression = condition.Condition;
			Debug.Assert(expression != null);
			if (expression == null)
				return new DbgCodeBreakpointCheckResult("Missing expression");

			DbgStackFrame frame = null;
			DbgValue value = null;
			try {
				frame = thread.GetTopStackFrame();
				if (frame == null)
					return new DbgCodeBreakpointCheckResult("Couldn't get the current stack frame");

				var language = dbgLanguageService.GetCurrentLanguage(thread.Runtime.RuntimeKindGuid);
				var cancellationToken = CancellationToken.None;
				var state = GetState(boundBreakpoint, language, frame, condition, cancellationToken);
				var evalInfo = new DbgEvaluationInfo(state.Context, frame, cancellationToken);
				var evalRes = language.ExpressionEvaluator.Evaluate(evalInfo, expression, DbgEvaluationOptions.Expression, state.ExpressionEvaluatorState);
				if (evalRes.Error != null)
					return new DbgCodeBreakpointCheckResult(evalRes.Error);
				value = evalRes.Value;

				switch (condition.Kind) {
				case DbgCodeBreakpointConditionKind.IsTrue:
					switch (value.ValueType) {
					case DbgSimpleValueType.Boolean:	return new DbgCodeBreakpointCheckResult((bool)value.RawValue);
					case DbgSimpleValueType.Char1:		return new DbgCodeBreakpointCheckResult((byte)value.RawValue != 0);
					case DbgSimpleValueType.CharUtf16:	return new DbgCodeBreakpointCheckResult((char)value.RawValue != 0);
					case DbgSimpleValueType.Int8:		return new DbgCodeBreakpointCheckResult((sbyte)value.RawValue != 0);
					case DbgSimpleValueType.Int16:		return new DbgCodeBreakpointCheckResult((short)value.RawValue != 0);
					case DbgSimpleValueType.Int32:		return new DbgCodeBreakpointCheckResult((int)value.RawValue != 0);
					case DbgSimpleValueType.Int64:		return new DbgCodeBreakpointCheckResult((long)value.RawValue != 0);
					case DbgSimpleValueType.UInt8:		return new DbgCodeBreakpointCheckResult((byte)value.RawValue != 0);
					case DbgSimpleValueType.UInt16:		return new DbgCodeBreakpointCheckResult((ushort)value.RawValue != 0);
					case DbgSimpleValueType.UInt32:		return new DbgCodeBreakpointCheckResult((uint)value.RawValue != 0);
					case DbgSimpleValueType.UInt64:		return new DbgCodeBreakpointCheckResult((ulong)value.RawValue != 0);
					case DbgSimpleValueType.Float32:	return new DbgCodeBreakpointCheckResult((float)value.RawValue != 0);
					case DbgSimpleValueType.Float64:	return new DbgCodeBreakpointCheckResult((double)value.RawValue != 0);
					case DbgSimpleValueType.Decimal:	return new DbgCodeBreakpointCheckResult((decimal)value.RawValue != 0);
					case DbgSimpleValueType.Ptr32:		return new DbgCodeBreakpointCheckResult((uint)value.RawValue != 0);
					case DbgSimpleValueType.Ptr64:		return new DbgCodeBreakpointCheckResult((ulong)value.RawValue != 0);

					case DbgSimpleValueType.Other:
					case DbgSimpleValueType.Void:
					case DbgSimpleValueType.StringUtf16:
					case DbgSimpleValueType.DateTime:
					default:
						break;
					}
					return new DbgCodeBreakpointCheckResult(dnSpy_Debugger_Resources.BreakpointExpressionMustBeABooleanExpression);

				case DbgCodeBreakpointConditionKind.WhenChanged:
					var newValue = SavedValue.TryCreateValue(dbgObjectIdService, value, GetType(evalInfo, language, value));
					if (newValue == null)
						return new DbgCodeBreakpointCheckResult(true);
					bool shouldBreak = !(state.SavedValue?.Equals(evalInfo, newValue) ?? true);
					state.SavedValue = newValue;
					return new DbgCodeBreakpointCheckResult(shouldBreak);

				default:
					return new DbgCodeBreakpointCheckResult($"Unknown kind: {condition.Kind}");
				}
			}
			finally {
				if (frame != null)
					thread.Process.DbgManager.Close(frame);
				if (value != null)
					thread.Process.DbgManager.Close(value);
			}
		}

		string GetType(DbgEvaluationInfo evalInfo, DbgLanguage language, DbgValue value) {
			const DbgValueFormatterTypeOptions options = DbgValueFormatterTypeOptions.IntrinsicTypeKeywords | DbgValueFormatterTypeOptions.Namespaces | DbgValueFormatterTypeOptions.Tokens;
			const CultureInfo cultureInfo = null;
			var sb = ObjectCache.AllocStringBuilder();
			var output = new DbgStringBuilderTextWriter(sb);
			language.Formatter.FormatType(evalInfo, output, value, options, cultureInfo);
			return ObjectCache.FreeAndToString(ref sb);
		}

		BreakpointState GetState(DbgBoundCodeBreakpoint boundBreakpoint, DbgLanguage language, DbgStackFrame frame, DbgCodeBreakpointCondition condition, CancellationToken cancellationToken) {
			var state = boundBreakpoint.GetOrCreateData<BreakpointState>();
			if (state.Language != language || state.Condition != condition) {
				state.Language = language;
				state.Condition = condition;
				state.Context = language.CreateContext(frame, cancellationToken: cancellationToken);
				state.ExpressionEvaluatorState = language.ExpressionEvaluator.CreateExpressionEvaluatorState();
			}
			return state;
		}
	}
}
