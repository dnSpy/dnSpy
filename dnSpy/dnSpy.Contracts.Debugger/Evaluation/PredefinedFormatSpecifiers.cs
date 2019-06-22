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

using System.Collections.ObjectModel;
using System.Diagnostics;

namespace dnSpy.Contracts.Debugger.Evaluation {
	/// <summary>
	/// Format specifiers used by variables windows
	/// 
	/// https://docs.microsoft.com/en-us/visualstudio/debugger/format-specifiers-in-csharp
	/// </summary>
	public static class PredefinedFormatSpecifiers {
		/// <summary>
		/// Use decimal
		/// </summary>
		public const string Decimal = "d";

		/// <summary>
		/// Use hexadecimal
		/// </summary>
		public const string Hexadecimal = "h";

		/// <summary>
		/// No quotes, just show the raw string
		/// </summary>
		public const string NoQuotes = "nq";

		/// <summary>
		/// Raw view only
		/// </summary>
		public const string RawView = "raw";

		/// <summary>
		/// Results view only
		/// </summary>
		public const string ResultsView = "results";

		/// <summary>
		/// Dynamic view only
		/// </summary>
		public const string DynamicView = "dynamic";

		/// <summary>
		/// Show all members, even non-public ones
		/// </summary>
		public const string ShowAllMembers = "hidden";

		/// <summary>
		/// Emulate the code without real func-eval
		/// </summary>
		public const string Emulator = "emulator";

		/// <summary>
		/// No side effects, it implies <see cref="Emulator"/>
		/// </summary>
		public const string NoSideEffects = "nse";

		/// <summary>
		/// Allow func-eval even if <see cref="NoSideEffects"/> is used and expression causes side effects
		/// (ac = always calculate)
		/// </summary>
		public const string AllowFuncEval = "ac";

		// The following are dnSpy extensions

		/// <summary>
		/// Digit separators
		/// </summary>
		public const string DigitSeparators = "ds";

		/// <summary>
		/// No digit seaparators
		/// </summary>
		public const string NoDigitSeparators = "nds";

		/// <summary>
		/// Use the same expression that's shown in the edit text box
		/// </summary>
		public const string EditExpression = "edit";

		/// <summary>
		/// Use ToString() if available to format the value
		/// </summary>
		public new const string ToString = "ts";

		/// <summary>
		/// Don't use ToString() to format the value
		/// </summary>
		public const string NoToString = "nts";

		/// <summary>
		/// Use <see cref="DebuggerDisplayAttribute"/> if available
		/// </summary>
		public const string DebuggerDisplay = "dda";

		/// <summary>
		/// Don't use <see cref="DebuggerDisplayAttribute"/>
		/// </summary>
		public const string NoDebuggerDisplay = "ndda";

		/// <summary>
		/// Show the full string value even if it's a very long string
		/// </summary>
		public const string FullString = "fs";

		/// <summary>
		/// Don't show the full string value if it's a very long string
		/// </summary>
		public const string NoFullString = "nfs";

		/// <summary>
		/// Show namespaces
		/// </summary>
		public const string Namespaces = "ns";

		/// <summary>
		/// Don't show namespaces
		/// </summary>
		public const string NoNamespaces = "nns";

		/// <summary>
		/// Show intrinsic type keywords (eg. int instead of Int32)
		/// </summary>
		public const string Intrinsics = "intrinsics";

		/// <summary>
		/// Don't show intrinsic type keywords (eg. int instead of Int32)
		/// </summary>
		public const string NoIntrinsics = "nointrinsics";

		/// <summary>
		/// Show tokens
		/// </summary>
		public const string Tokens = "tokens";

		/// <summary>
		/// Don't show tokens
		/// </summary>
		public const string NoTokens = "notokens";

		/// <summary>
		/// Show compiler generated members
		/// </summary>
		public const string ShowCompilerGeneratedMembers = "cgm";

		/// <summary>
		/// Don't show compiler generated members
		/// </summary>
		public const string NoShowCompilerGeneratedMembers = "ncgm";

		/// <summary>
		/// Respect attributes that can hide a member, eg. <see cref="DebuggerBrowsableAttribute"/> and <see cref="DebuggerBrowsableState.Never"/>
		/// </summary>
		public const string RespectHideMemberAttributes = "hma";

		/// <summary>
		/// Don't respect attributes that can hide a member, eg. <see cref="DebuggerBrowsableAttribute"/> and <see cref="DebuggerBrowsableState.Never"/>
		/// </summary>
		public const string NoRespectHideMemberAttributes = "nhma";

		/// <summary>
		/// Gets value formatter options
		/// </summary>
		/// <param name="formatSpecifiers">Format specifiers or null</param>
		/// <param name="options">Default options</param>
		/// <returns></returns>
		public static DbgValueFormatterOptions GetValueFormatterOptions(ReadOnlyCollection<string>? formatSpecifiers, DbgValueFormatterOptions options) {
			if (!(formatSpecifiers is null)) {
				for (int i = 0; i < formatSpecifiers.Count; i++) {
					switch (formatSpecifiers[i]) {
					case Decimal:
						options |= DbgValueFormatterOptions.Decimal;
						break;
					case Hexadecimal:
						options &= ~DbgValueFormatterOptions.Decimal;
						break;
					case NoQuotes:
						options |= DbgValueFormatterOptions.NoStringQuotes;
						break;
					case DigitSeparators:
						options |= DbgValueFormatterOptions.DigitSeparators;
						break;
					case NoDigitSeparators:
						options &= ~DbgValueFormatterOptions.DigitSeparators;
						break;
					case EditExpression:
						options |= DbgValueFormatterOptions.Edit;
						break;
					case ToString:
						options |= DbgValueFormatterOptions.ToString;
						break;
					case NoToString:
						options &= ~DbgValueFormatterOptions.ToString;
						break;
					case DebuggerDisplay:
						options &= ~DbgValueFormatterOptions.NoDebuggerDisplay;
						break;
					case NoDebuggerDisplay:
						options |= DbgValueFormatterOptions.NoDebuggerDisplay;
						break;
					case FullString:
						options |= DbgValueFormatterOptions.FullString;
						break;
					case NoFullString:
						options &= ~DbgValueFormatterOptions.FullString;
						break;
					case Namespaces:
						options |= DbgValueFormatterOptions.Namespaces;
						break;
					case NoNamespaces:
						options &= ~DbgValueFormatterOptions.Namespaces;
						break;
					case Intrinsics:
						options |= DbgValueFormatterOptions.IntrinsicTypeKeywords;
						break;
					case NoIntrinsics:
						options &= ~DbgValueFormatterOptions.IntrinsicTypeKeywords;
						break;
					case Tokens:
						options |= DbgValueFormatterOptions.Tokens;
						break;
					case NoTokens:
						options &= ~DbgValueFormatterOptions.Tokens;
						break;
					}
				}
			}
			return options;
		}

		/// <summary>
		/// Gets value formatter type options
		/// </summary>
		/// <param name="formatSpecifiers">Format specifiers or null</param>
		/// <param name="options">Default options</param>
		/// <returns></returns>
		public static DbgValueFormatterTypeOptions GetValueFormatterTypeOptions(ReadOnlyCollection<string>? formatSpecifiers, DbgValueFormatterTypeOptions options) {
			if (!(formatSpecifiers is null)) {
				for (int i = 0; i < formatSpecifiers.Count; i++) {
					switch (formatSpecifiers[i]) {
					case Decimal:
						options |= DbgValueFormatterTypeOptions.Decimal;
						break;
					case Hexadecimal:
						options &= ~DbgValueFormatterTypeOptions.Decimal;
						break;
					case DigitSeparators:
						options |= DbgValueFormatterTypeOptions.DigitSeparators;
						break;
					case NoDigitSeparators:
						options &= ~DbgValueFormatterTypeOptions.DigitSeparators;
						break;
					case Namespaces:
						options |= DbgValueFormatterTypeOptions.Namespaces;
						break;
					case NoNamespaces:
						options &= ~DbgValueFormatterTypeOptions.Namespaces;
						break;
					case Intrinsics:
						options |= DbgValueFormatterTypeOptions.IntrinsicTypeKeywords;
						break;
					case NoIntrinsics:
						options &= ~DbgValueFormatterTypeOptions.IntrinsicTypeKeywords;
						break;
					case Tokens:
						options |= DbgValueFormatterTypeOptions.Tokens;
						break;
					case NoTokens:
						options &= ~DbgValueFormatterTypeOptions.Tokens;
						break;
					}
				}
			}
			return options;
		}

		/// <summary>
		/// Gets value node evaluation options
		/// </summary>
		/// <param name="formatSpecifiers">Format specifiers or null</param>
		/// <param name="options">Default options</param>
		/// <returns></returns>
		public static DbgValueNodeEvaluationOptions GetValueNodeEvaluationOptions(ReadOnlyCollection<string>? formatSpecifiers, DbgValueNodeEvaluationOptions options) {
			if (!(formatSpecifiers is null)) {
				for (int i = 0; i < formatSpecifiers.Count; i++) {
					switch (formatSpecifiers[i]) {
					case DynamicView:
						options |= DbgValueNodeEvaluationOptions.DynamicView;
						break;
					case ResultsView:
						options |= DbgValueNodeEvaluationOptions.ResultsView;
						break;
					case RawView:
						options |= DbgValueNodeEvaluationOptions.RawView;
						break;
					case ShowAllMembers:
						options &= ~DbgValueNodeEvaluationOptions.PublicMembers;
						break;
					case ShowCompilerGeneratedMembers:
						options &= ~DbgValueNodeEvaluationOptions.HideCompilerGeneratedMembers;
						break;
					case NoShowCompilerGeneratedMembers:
						options |= DbgValueNodeEvaluationOptions.HideCompilerGeneratedMembers;
						break;
					case RespectHideMemberAttributes:
						options |= DbgValueNodeEvaluationOptions.RespectHideMemberAttributes;
						break;
					case NoRespectHideMemberAttributes:
						options &= ~DbgValueNodeEvaluationOptions.RespectHideMemberAttributes;
						break;
					}
				}
			}
			return options;
		}
	}
}
