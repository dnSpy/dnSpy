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
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.Formatters;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Debugger.Formatters {
	readonly struct DebuggerDisplayAttributeFormatter {
		readonly DbgEvaluationInfo evalInfo;
		readonly LanguageFormatter languageFormatter;
		readonly IDbgTextWriter output;
		readonly DbgValueFormatterOptions options;
		readonly CultureInfo cultureInfo;

		public DebuggerDisplayAttributeFormatter(DbgEvaluationInfo evalInfo, LanguageFormatter languageFormatter, IDbgTextWriter output, DbgValueFormatterOptions options, CultureInfo cultureInfo) {
			Debug.Assert((options & DbgValueFormatterOptions.NoDebuggerDisplay) == 0);
			this.evalInfo = evalInfo;
			this.languageFormatter = languageFormatter;
			this.output = output;
			this.options = options;
			this.cultureInfo = cultureInfo;
		}

		[Flags]
		enum DisplayPartFlags : byte {
			None					= 0,
			EvaluateText			= 0x01,
		}

		readonly struct DisplayPart {
			public readonly DisplayPartFlags Flags;
			public readonly string Text;
			DisplayPart(DisplayPartFlags flags, string text) {
				Flags = flags;
				Text = text ?? throw new ArgumentNullException(nameof(text));
			}
			public static DisplayPart CreateText(string text) => new DisplayPart(DisplayPartFlags.None, text);
			public static DisplayPart CreateEvaluate(string text) => new DisplayPart(DisplayPartFlags.EvaluateText, text);
			public override string ToString() => $"{Flags}: '{Text}'";
		}

		sealed class TypeState : IDisposable {
			public static readonly TypeState Empty = new TypeState(null, Array.Empty<DisplayPart>(), Array.Empty<DisplayPart>(), Array.Empty<DisplayPart>());
			public readonly DbgEvaluationContext TypeContext;
			public readonly DisplayPart[] NameParts;
			public readonly DisplayPart[] ValueParts;
			public readonly DisplayPart[] TypeParts;
			public TypeState(DbgEvaluationContext typeContext, DisplayPart[] nameParts, DisplayPart[] valueParts, DisplayPart[] typeParts) {
				TypeContext = typeContext;
				NameParts = nameParts ?? throw new ArgumentNullException(nameof(nameParts));
				ValueParts = valueParts ?? throw new ArgumentNullException(nameof(valueParts));
				TypeParts = typeParts ?? throw new ArgumentNullException(nameof(typeParts));
				if (typeContext != null) {
					lockObj = new object();
					eeStates = new Dictionary<string, object>(StringComparer.Ordinal);
				}
			}

			readonly object lockObj;
			readonly Dictionary<string, object> eeStates;

			public object GetExpressionEvaluatorState(DbgExpressionEvaluator expressionEvaluator, string expression) {
				lock (lockObj) {
					if (eeStates.TryGetValue(expression, out var state))
						return state;
					state = expressionEvaluator.CreateExpressionEvaluatorState();
					eeStates.Add(expression, state);
					return state;
				}
			}

			public void Dispose() {
				TypeContext?.Close();
				eeStates?.Clear();
			}
		}

		public bool FormatName(DbgDotNetValue value) {
			var typeState = GetOrCreateTypeState(value.Type, evalInfo.Context.Language);
			return Format(value, typeState, typeState.NameParts);
		}

		public bool FormatValue(DbgDotNetValue value) {
			var typeState = GetOrCreateTypeState(value.Type, evalInfo.Context.Language);
			return Format(value, typeState, typeState.ValueParts);
		}

		public bool FormatType(DbgDotNetValue value) {
			var typeState = GetOrCreateTypeState(value.Type, evalInfo.Context.Language);
			return Format(value, typeState, typeState.TypeParts);
		}

		bool Format(DbgDotNetValue value, TypeState typeState, DisplayPart[] displayParts) {
			if (displayParts.Length == 0)
				return false;

			var evaluator = evalInfo.Context.GetDebuggerDisplayAttributeEvaluator();
			foreach (var part in displayParts) {
				if ((part.Flags & DisplayPartFlags.EvaluateText) == 0)
					output.Write(DbgTextColor.DebuggerDisplayAttributeEval, part.Text);
				else {
					object eeState = typeState.GetExpressionEvaluatorState(evalInfo.Context.Language.ExpressionEvaluator, part.Text);
					DbgDotNetEvalResult evalRes = default;
					try {
						var evalInfo2 = new DbgEvaluationInfo(typeState.TypeContext, evalInfo.Frame, evalInfo.CancellationToken);
						evalRes = evaluator.Evaluate(evalInfo2, value, part.Text, DbgEvaluationOptions.Expression, eeState);
						if (evalRes.Error != null) {
							output.Write(DbgTextColor.Error, "<<<");
							output.Write(DbgTextColor.Error, evalRes.Error);
							output.Write(DbgTextColor.Error, ">>>");
						}
						else {
							// Prevent recursive calls
							var options = this.options | DbgValueFormatterOptions.NoDebuggerDisplay;
							options &= ~DbgValueFormatterOptions.NoStringQuotes;
							options = PredefinedFormatSpecifiers.GetValueFormatterOptions(evalRes.FormatSpecifiers, options);
							languageFormatter.FormatValue(evalInfo, output, evalRes.Value, options, cultureInfo);
						}
					}
					finally {
						if (evalRes.Value != value)
							evalRes.Value?.Dispose();
					}
				}
			}

			return true;
		}

		TypeState GetOrCreateTypeState(DmdType type, DbgLanguage language) {
			var state = StateWithKey<TypeState>.TryGet(type, language);
			if (state != null)
				return state;
			return GetOrCreateTypeStateCore(type, language);
		}

		TypeState GetOrCreateTypeStateCore(DmdType type, DbgLanguage language) {
			var state = CreateTypeState(type, language);
			return StateWithKey<TypeState>.GetOrCreate(type, language, () => state);
		}

		TypeState CreateTypeState(DmdType type, DbgLanguage language) {
			var info = GetDisplayParts(type);
			if (info.nameParts.Length == 0 && info.valueParts.Length == 0 && info.typeParts.Length == 0)
				return TypeState.Empty;

			var context = language.CreateContext(evalInfo.Runtime, null);
			var state = new TypeState(context, info.nameParts, info.valueParts, info.typeParts);
			context.Runtime.CloseOnExit(state);
			return state;
		}

		static bool ShouldIgnoreDebuggerDisplayAttribute(DmdType type) {
			// We have special support for formatting KeyValuePair<K, V> and DictionaryEntry, so ignore all DebuggerDisplayAttributes.
			// (Only Unity and older Mono versions have a DebuggerDisplayAttribute on them)
			if (type.IsConstructedGenericType) {
				if (type.MetadataName == "KeyValuePair`2" && type.MetadataNamespace == "System.Collections.Generic")
					return type.GetGenericTypeDefinition() == type.AppDomain.GetWellKnownType(DmdWellKnownType.System_Collections_Generic_KeyValuePair_T2, isOptional: true);
				return false;
			}
			else {
				if (type.MetadataName == "DictionaryEntry" && type.MetadataNamespace == "System.Collections")
					return type == type.AppDomain.GetWellKnownType(DmdWellKnownType.System_Collections_DictionaryEntry, isOptional: true);
				return false;
			}
		}

		(DisplayPart[] nameParts, DisplayPart[] valueParts, DisplayPart[] typeParts) GetDisplayParts(DmdType type) {
			var ddaType = type.AppDomain.GetWellKnownType(DmdWellKnownType.System_Diagnostics_DebuggerDisplayAttribute, isOptional: true);
			Debug.Assert((object)ddaType != null);

			bool forceNoAttr = ShouldIgnoreDebuggerDisplayAttribute(type);
			string nameDisplayString = null, valueDisplayString = null, typeDisplayString = null;
			if (!forceNoAttr && (object)ddaType != null) {
				var attr = type.FindCustomAttribute(ddaType, inherit: true);
				if (attr == null) {
					if (type.CanCastTo(type.AppDomain.System_Type)) {
						// Show the same thing VS shows
						valueDisplayString = @"\{Name = {Name} FullName = {FullName}\}";
					}
				}
				else {
					if (attr.ConstructorArguments.Count == 1)
						valueDisplayString = attr.ConstructorArguments[0].Value as string;
					nameDisplayString = GetString(attr, nameof(DebuggerDisplayAttribute.Name));
					typeDisplayString = GetString(attr, nameof(DebuggerDisplayAttribute.Type));
				}
			}

			var nameParts = CreateDisplayParts(nameDisplayString);
			var valueParts = CreateDisplayParts(valueDisplayString);
			var typeParts = CreateDisplayParts(typeDisplayString);
			return (nameParts, valueParts, typeParts);
		}

		static string GetString(DmdCustomAttributeData ca, string propertyName) {
			if (ca == null)
				return null;
			foreach (var arg in ca.NamedArguments) {
				if (arg.IsField)
					continue;
				if (arg.MemberName != propertyName)
					continue;
				if (arg.TypedValue.ArgumentType != arg.TypedValue.ArgumentType.AppDomain.System_String)
					continue;
				return arg.TypedValue.Value as string;
			}
			return null;
		}

		static DisplayPart[] CreateDisplayParts(string debuggerDisplayString) {
			if (debuggerDisplayString == null)
				return Array.Empty<DisplayPart>();
			if (debuggerDisplayString.Length == 0)
				return new[] { DisplayPart.CreateText(string.Empty) };

			var list = ListCache<DisplayPart>.AllocList();

			var sb = ObjectCache.AllocStringBuilder();
			int pos = 0;
			for (;;) {
				sb.Clear();
				var text = ReadText(debuggerDisplayString, ref pos, sb);
				if (text.Length != 0)
					list.Add(DisplayPart.CreateText(text));

				sb.Clear();
				var expression = ReadEvalText(debuggerDisplayString, ref pos, sb);
				if (expression.Length != 0)
					list.Add(DisplayPart.CreateEvaluate(expression));

				if (pos >= debuggerDisplayString.Length)
					break;
			}

			ObjectCache.Free(ref sb);
			return ListCache<DisplayPart>.FreeAndToArray(ref list);
		}

		static string ReadText(string s, ref int pos, StringBuilder sb) {
			while (pos < s.Length) {
				var c = s[pos++];
				if (c == '{')
					break;
				if (c == '\\' && pos < s.Length) {
					c = s[pos++];
					switch (c) {
					case '{':
					case '}':
					case '\\':
						break;

					default:
						sb.Append('\\');
						break;
					}
				}
				sb.Append(c);
			}
			return sb.ToString();
		}

		static string ReadEvalText(string s, ref int pos, StringBuilder sb) {
			int braceCount = 1;
			while (pos < s.Length) {
				var c = s[pos++];
				if (c == '}') {
					if (braceCount <= 1)
						break;
					braceCount--;
				}
				else if (c == '{')
					braceCount++;
				sb.Append(c);
			}
			return sb.ToString();
		}
	}
}
