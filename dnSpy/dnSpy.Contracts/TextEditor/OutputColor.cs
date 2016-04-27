/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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

using dnSpy.Decompiler.Shared;

namespace dnSpy.Contracts.TextEditor {
	/// <summary>
	/// Output color
	/// </summary>
	public enum OutputColor {
		// IMPORTANT: The order must match dnSpy.Contracts.Themes.ColorType

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		Text,
		Brace,
		Operator,
		Number,
		Comment,
		XmlDocTag,
		XmlDocAttribute,
		XmlDocComment,
		Keyword,
		String,
		Char,
		NamespacePart,
		Type,
		StaticType,
		Delegate,
		Enum,
		Interface,
		ValueType,
		TypeGenericParameter,
		MethodGenericParameter,
		InstanceMethod,
		StaticMethod,
		ExtensionMethod,
		InstanceField,
		EnumField,
		LiteralField,
		StaticField,
		InstanceEvent,
		StaticEvent,
		InstanceProperty,
		StaticProperty,
		Local,
		Parameter,
		PreprocessorKeyword,
		PreprocessorText,
		Label,
		OpCode,
		ILDirective,
		ILModule,
		XmlDocToolTipColon,
		XmlDocToolTipExample,
		XmlDocToolTipExceptionCref,
		XmlDocToolTipReturns,
		XmlDocToolTipSeeCref,
		XmlDocToolTipSeeLangword,
		XmlDocToolTipSeeAlso,
		XmlDocToolTipSeeAlsoCref,
		XmlDocToolTipParamRefName,
		XmlDocToolTipParamName,
		XmlDocToolTipTypeParamName,
		XmlDocToolTipValue,
		XmlDocSummary,
		XmlDocToolTipText,
		Assembly,
		AssemblyExe,
		Module,
		DirectoryPart,
		FileNameNoExtension,
		FileExtension,
		Error,
		ToStringEval,
		ReplPrompt1,
		ReplPrompt2,
		ReplOutputText,
		ReplScriptOutputText,
		Black,
		Blue,
		Cyan,
		DarkBlue,
		DarkCyan,
		DarkGray,
		DarkGreen,
		DarkMagenta,
		DarkRed,
		DarkYellow,
		Gray,
		Green,
		Magenta,
		Red,
		White,
		Yellow,
		InvBlack,
		InvBlue,
		InvCyan,
		InvDarkBlue,
		InvDarkCyan,
		InvDarkGray,
		InvDarkGreen,
		InvDarkMagenta,
		InvDarkRed,
		InvDarkYellow,
		InvGray,
		InvGreen,
		InvMagenta,
		InvRed,
		InvWhite,
		InvYellow,
		DebugLogExceptionHandled,
		DebugLogExceptionUnhandled,
		DebugLogStepFiltering,
		DebugLogLoadModule,
		DebugLogUnloadModule,
		DebugLogExitProcess,
		DebugLogExitThread,
		DebugLogProgramOutput,
		DebugLogMDA,
		DebugLogTimestamp,
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}

	/// <summary>
	/// Extension methods
	/// </summary>
	public static class OutputColorExtensions {
		/// <summary>
		/// Converts <paramref name="color"/> to a <see cref="TextTokenKind"/>
		/// </summary>
		/// <param name="color">Color</param>
		/// <returns></returns>
		public static TextTokenKind ToTextTokenKind(this OutputColor color) {
			return (TextTokenKind)color;
		}
	}
}