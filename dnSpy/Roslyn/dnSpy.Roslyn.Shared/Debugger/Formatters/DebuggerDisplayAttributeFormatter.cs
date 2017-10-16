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
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.Formatters;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Shared.Debugger.Formatters {
	struct DebuggerDisplayAttributeFormatter {
		readonly DbgEvaluationContext context;
		readonly DbgStackFrame frame;
		readonly LanguageFormatter languageFormatter;
		readonly ITextColorWriter output;
		readonly DbgValueFormatterOptions options;
		readonly CultureInfo cultureInfo;
		readonly CancellationToken cancellationToken;

		public DebuggerDisplayAttributeFormatter(DbgEvaluationContext context, DbgStackFrame frame, LanguageFormatter languageFormatter, ITextColorWriter output, DbgValueFormatterOptions options, CultureInfo cultureInfo, CancellationToken cancellationToken) {
			this.context = context;
			this.frame = frame;
			this.languageFormatter = languageFormatter;
			this.output = output;
			this.options = options;
			this.cultureInfo = cultureInfo;
			this.cancellationToken = cancellationToken;
		}

		[Flags]
		enum DisplayPartFlags : byte {
			None					= 0,
			EvaluateText			= 0x01,
			Decimal					= 0x02,
			Hexadecimal				= 0x04,
			NoQuotes				= 0x08,
		}

		struct DisplayPart {
			public readonly DisplayPartFlags Flags;
			public readonly string Text;
			DisplayPart(DisplayPartFlags flags, string text) {
				Flags = flags;
				Text = text ?? throw new ArgumentNullException(nameof(text));
			}
			public static DisplayPart CreateText(string text) => new DisplayPart(DisplayPartFlags.None, text);
			public static DisplayPart CreateEvaluate(string text, string[] formatSpecifiers) {
				var flags = DisplayPartFlags.EvaluateText;
				// https://docs.microsoft.com/en-us/visualstudio/debugger/format-specifiers-in-csharp
				foreach (var fs in formatSpecifiers) {
					switch (fs) {
					case "d":
						flags = (flags & ~DisplayPartFlags.Hexadecimal) | DisplayPartFlags.Decimal;
						break;
					case "h":
						flags = (flags & ~DisplayPartFlags.Decimal) | DisplayPartFlags.Hexadecimal;
						break;
					case "nq":
						flags |= DisplayPartFlags.NoQuotes;
						break;
					}
				}
				return new DisplayPart(flags, text);
			}
			public override string ToString() => $"{Flags}: '{Text}'";
		}

		sealed class TypeState : IDisposable {
			public static readonly TypeState Empty = new TypeState(null, Array.Empty<DisplayPart>());
			public readonly DbgEvaluationContext TypeContext;
			public readonly DisplayPart[] DisplayParts;
			public TypeState(DbgEvaluationContext typeContext, DisplayPart[] displayParts) {
				TypeContext = typeContext;
				DisplayParts = displayParts ?? throw new ArgumentNullException(nameof(displayParts));
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

		public bool Format(DbgDotNetValue value) {
			var typeState = GetOrCreateTypeState(value.Type, context.Language);
			if (typeState.DisplayParts.Length == 0)
				return false;

			var evaluator = context.GetDebuggerDisplayAttributeEvaluator();
			foreach (var part in typeState.DisplayParts) {
				if ((part.Flags & DisplayPartFlags.EvaluateText) == 0)
					output.Write(BoxedTextColor.DebuggerDisplayAttributeEval, part.Text);
				else {
					object eeState = typeState.GetExpressionEvaluatorState(context.Language.ExpressionEvaluator, part.Text);
					DbgDotNetEvalResult evalRes = default;
					try {
						evalRes = evaluator.Evaluate(typeState.TypeContext, frame, value, part.Text, DbgEvaluationOptions.Expression, eeState, cancellationToken);
						if (evalRes.Error != null) {
							output.Write(BoxedTextColor.Error, "<<<");
							output.Write(BoxedTextColor.Error, evalRes.Error);
							output.Write(BoxedTextColor.Error, ">>>");
						}
						else {
							// Prevent recursive calls by disabling func-eval
							var options = this.options & ~(DbgValueFormatterOptions.FuncEval | DbgValueFormatterOptions.ToString);
							if ((part.Flags & DisplayPartFlags.Decimal) != 0)
								options |= DbgValueFormatterOptions.Decimal;
							else if ((part.Flags & DisplayPartFlags.Hexadecimal) != 0)
								options &= ~DbgValueFormatterOptions.Decimal;
							if ((part.Flags & DisplayPartFlags.NoQuotes) != 0)
								options |= DbgValueFormatterOptions.NoStringQuotes;
							else
								options &= ~DbgValueFormatterOptions.NoStringQuotes;

							languageFormatter.FormatValue(context, output, frame, evalRes.Value, options, cultureInfo, cancellationToken);
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
			var parts = GetDisplayParts(type) ?? Array.Empty<DisplayPart>();
			if (parts.Length == 0)
				return TypeState.Empty;

			var context = language.CreateContext(frame.Runtime, null, cancellationToken: cancellationToken);
			var state = new TypeState(context, parts);
			context.Runtime.CloseOnExit(state);
			return state;
		}

		DisplayPart[] GetDisplayParts(DmdType type) {
			var ddaType = type.AppDomain.GetWellKnownType(DmdWellKnownType.System_Diagnostics_DebuggerDisplayAttribute, isOptional: true);
			Debug.Assert((object)ddaType != null);
			if ((object)ddaType == null)
				return null;

			string debuggerDisplayString;
			var attr = type.FindCustomAttribute(ddaType, inherit: true);
			if (attr == null) {
				if (type.CanCastTo(type.AppDomain.System_Type)) {
					// Show the same thing VS shows
					debuggerDisplayString = @"\{Name = {Name} FullName = {FullName}\}";
				}
				else
					return null;
			}
			else {
				if (attr.ConstructorArguments.Count == 1)
					debuggerDisplayString = attr.ConstructorArguments[0].Value as string;
				else
					debuggerDisplayString = null;
			}
			if (string.IsNullOrEmpty(debuggerDisplayString))
				return null;

			return CreateDisplayParts(debuggerDisplayString);
		}

		static DisplayPart[] CreateDisplayParts(string debuggerDisplayString) {
			var list = ListCache<DisplayPart>.AllocList();

			var sb = ObjectCache.AllocStringBuilder();
			int pos = 0;
			for (;;) {
				sb.Clear();
				var text = ReadText(debuggerDisplayString, ref pos, sb);
				if (text.Length != 0)
					list.Add(DisplayPart.CreateText(text));

				sb.Clear();
				var evalInfo = ReadEvalText(debuggerDisplayString, ref pos, sb);
				if (evalInfo.expression.Length != 0)
					list.Add(DisplayPart.CreateEvaluate(evalInfo.expression, evalInfo.formatSpecifiers));

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
						sb.Append(c);
						break;

					default:
						sb.Append('\\');
						sb.Append(c);
						break;
					}
				}
				else
					sb.Append(c);
			}
			return sb.ToString();
		}

		static (string expression, string[] formatSpecifiers) ReadEvalText(string s, ref int pos, StringBuilder sb) {
			bool seenComma = false;
			while (pos < s.Length) {
				var c = s[pos++];
				if (c == '}')
					break;
				seenComma |= c == ',';
				sb.Append(c);
			}
			if (!seenComma)
				return (sb.ToString(), Array.Empty<string>());
			return FormatSpecifiersUtils.GetFormatSpecifiers(sb);
		}
	}
}
