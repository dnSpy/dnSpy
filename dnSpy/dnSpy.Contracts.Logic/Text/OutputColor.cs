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

using System.Diagnostics;

namespace dnSpy.Contracts.Text {
	/// <summary>
	/// Output color
	/// </summary>
	public enum OutputColor {
		// IMPORTANT: The order must match dnSpy.Contracts.Themes.ColorType

#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		Text,
		Operator,
		Punctuation,
		Number,
		Comment,
		Keyword,
		String,
		VerbatimString,
		Char,
		Namespace,
		Type,
		SealedType,
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
		ExcludedCode,
		XmlDocCommentAttributeName,
		XmlDocCommentAttributeQuotes,
		XmlDocCommentAttributeValue,
		XmlDocCommentCDataSection,
		XmlDocCommentComment,
		XmlDocCommentDelimiter,
		XmlDocCommentEntityReference,
		XmlDocCommentName,
		XmlDocCommentProcessingInstruction,
		XmlDocCommentText,
		XmlLiteralAttributeName,
		XmlLiteralAttributeQuotes,
		XmlLiteralAttributeValue,
		XmlLiteralCDataSection,
		XmlLiteralComment,
		XmlLiteralDelimiter,
		XmlLiteralEmbeddedExpression,
		XmlLiteralEntityReference,
		XmlLiteralName,
		XmlLiteralProcessingInstruction,
		XmlLiteralText,
		XmlAttributeName,
		XmlAttributeQuotes,
		XmlAttributeValue,
		XmlCDataSection,
		XmlComment,
		XmlDelimiter,
		XmlKeyword,
		XmlName,
		XmlProcessingInstruction,
		XmlText,
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
		XmlDocToolTipSummary,
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
		LineNumber,
		Link,
		VisibleWhitespace,
		SelectedText,
		InactiveSelectedText,
		HighlightedReference,
		HighlightedWrittenReference,
		HighlightedDefinition,
		CurrentStatement,
		ReturnStatement,
		SelectedReturnStatement,
		BreakpointStatement,
		DisabledBreakpointStatement,
		SpecialCharacterBox,
		SearchResultMarker,
		CurrentLine,
		CurrentLineNoFocus,
		HexText,
		HexOffset,
		HexByte0,
		HexByte1,
		HexByteError,
		HexAscii,
		HexCaret,
		HexInactiveCaret,
		HexSelection,
		GlyphMargin,

		/// <summary>
		/// Must be last
		/// </summary>
		Last,
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}

	/// <summary>
	/// Boxed colors
	/// </summary>
	public static class BoxedOutputColor {
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		public static readonly object Text = OutputColor.Text;
		public static readonly object Operator = OutputColor.Operator;
		public static readonly object Punctuation = OutputColor.Punctuation;
		public static readonly object Number = OutputColor.Number;
		public static readonly object Comment = OutputColor.Comment;
		public static readonly object Keyword = OutputColor.Keyword;
		public static readonly object String = OutputColor.String;
		public static readonly object VerbatimString = OutputColor.VerbatimString;
		public static readonly object Char = OutputColor.Char;
		public static readonly object Namespace = OutputColor.Namespace;
		public static readonly object Type = OutputColor.Type;
		public static readonly object SealedType = OutputColor.SealedType;
		public static readonly object StaticType = OutputColor.StaticType;
		public static readonly object Delegate = OutputColor.Delegate;
		public static readonly object Enum = OutputColor.Enum;
		public static readonly object Interface = OutputColor.Interface;
		public static readonly object ValueType = OutputColor.ValueType;
		public static readonly object TypeGenericParameter = OutputColor.TypeGenericParameter;
		public static readonly object MethodGenericParameter = OutputColor.MethodGenericParameter;
		public static readonly object InstanceMethod = OutputColor.InstanceMethod;
		public static readonly object StaticMethod = OutputColor.StaticMethod;
		public static readonly object ExtensionMethod = OutputColor.ExtensionMethod;
		public static readonly object InstanceField = OutputColor.InstanceField;
		public static readonly object EnumField = OutputColor.EnumField;
		public static readonly object LiteralField = OutputColor.LiteralField;
		public static readonly object StaticField = OutputColor.StaticField;
		public static readonly object InstanceEvent = OutputColor.InstanceEvent;
		public static readonly object StaticEvent = OutputColor.StaticEvent;
		public static readonly object InstanceProperty = OutputColor.InstanceProperty;
		public static readonly object StaticProperty = OutputColor.StaticProperty;
		public static readonly object Local = OutputColor.Local;
		public static readonly object Parameter = OutputColor.Parameter;
		public static readonly object PreprocessorKeyword = OutputColor.PreprocessorKeyword;
		public static readonly object PreprocessorText = OutputColor.PreprocessorText;
		public static readonly object Label = OutputColor.Label;
		public static readonly object OpCode = OutputColor.OpCode;
		public static readonly object ILDirective = OutputColor.ILDirective;
		public static readonly object ILModule = OutputColor.ILModule;
		public static readonly object ExcludedCode = OutputColor.ExcludedCode;
		public static readonly object XmlDocCommentAttributeName = OutputColor.XmlDocCommentAttributeName;
		public static readonly object XmlDocCommentAttributeQuotes = OutputColor.XmlDocCommentAttributeQuotes;
		public static readonly object XmlDocCommentAttributeValue = OutputColor.XmlDocCommentAttributeValue;
		public static readonly object XmlDocCommentCDataSection = OutputColor.XmlDocCommentCDataSection;
		public static readonly object XmlDocCommentComment = OutputColor.XmlDocCommentComment;
		public static readonly object XmlDocCommentDelimiter = OutputColor.XmlDocCommentDelimiter;
		public static readonly object XmlDocCommentEntityReference = OutputColor.XmlDocCommentEntityReference;
		public static readonly object XmlDocCommentName = OutputColor.XmlDocCommentName;
		public static readonly object XmlDocCommentProcessingInstruction = OutputColor.XmlDocCommentProcessingInstruction;
		public static readonly object XmlDocCommentText = OutputColor.XmlDocCommentText;
		public static readonly object XmlLiteralAttributeName = OutputColor.XmlLiteralAttributeName;
		public static readonly object XmlLiteralAttributeQuotes = OutputColor.XmlLiteralAttributeQuotes;
		public static readonly object XmlLiteralAttributeValue = OutputColor.XmlLiteralAttributeValue;
		public static readonly object XmlLiteralCDataSection = OutputColor.XmlLiteralCDataSection;
		public static readonly object XmlLiteralComment = OutputColor.XmlLiteralComment;
		public static readonly object XmlLiteralDelimiter = OutputColor.XmlLiteralDelimiter;
		public static readonly object XmlLiteralEmbeddedExpression = OutputColor.XmlLiteralEmbeddedExpression;
		public static readonly object XmlLiteralEntityReference = OutputColor.XmlLiteralEntityReference;
		public static readonly object XmlLiteralName = OutputColor.XmlLiteralName;
		public static readonly object XmlLiteralProcessingInstruction = OutputColor.XmlLiteralProcessingInstruction;
		public static readonly object XmlLiteralText = OutputColor.XmlLiteralText;
		public static readonly object XmlAttributeName = OutputColor.XmlAttributeName;
		public static readonly object XmlAttributeQuotes = OutputColor.XmlAttributeQuotes;
		public static readonly object XmlAttributeValue = OutputColor.XmlAttributeValue;
		public static readonly object XmlCDataSection = OutputColor.XmlCDataSection;
		public static readonly object XmlComment = OutputColor.XmlComment;
		public static readonly object XmlDelimiter = OutputColor.XmlDelimiter;
		public static readonly object XmlKeyword = OutputColor.XmlKeyword;
		public static readonly object XmlName = OutputColor.XmlName;
		public static readonly object XmlProcessingInstruction = OutputColor.XmlProcessingInstruction;
		public static readonly object XmlText = OutputColor.XmlText;
		public static readonly object XmlDocToolTipColon = OutputColor.XmlDocToolTipColon;
		public static readonly object XmlDocToolTipExample = OutputColor.XmlDocToolTipExample;
		public static readonly object XmlDocToolTipExceptionCref = OutputColor.XmlDocToolTipExceptionCref;
		public static readonly object XmlDocToolTipReturns = OutputColor.XmlDocToolTipReturns;
		public static readonly object XmlDocToolTipSeeCref = OutputColor.XmlDocToolTipSeeCref;
		public static readonly object XmlDocToolTipSeeLangword = OutputColor.XmlDocToolTipSeeLangword;
		public static readonly object XmlDocToolTipSeeAlso = OutputColor.XmlDocToolTipSeeAlso;
		public static readonly object XmlDocToolTipSeeAlsoCref = OutputColor.XmlDocToolTipSeeAlsoCref;
		public static readonly object XmlDocToolTipParamRefName = OutputColor.XmlDocToolTipParamRefName;
		public static readonly object XmlDocToolTipParamName = OutputColor.XmlDocToolTipParamName;
		public static readonly object XmlDocToolTipTypeParamName = OutputColor.XmlDocToolTipTypeParamName;
		public static readonly object XmlDocToolTipValue = OutputColor.XmlDocToolTipValue;
		public static readonly object XmlDocToolTipSummary = OutputColor.XmlDocToolTipSummary;
		public static readonly object XmlDocToolTipText = OutputColor.XmlDocToolTipText;
		public static readonly object Assembly = OutputColor.Assembly;
		public static readonly object AssemblyExe = OutputColor.AssemblyExe;
		public static readonly object Module = OutputColor.Module;
		public static readonly object DirectoryPart = OutputColor.DirectoryPart;
		public static readonly object FileNameNoExtension = OutputColor.FileNameNoExtension;
		public static readonly object FileExtension = OutputColor.FileExtension;
		public static readonly object Error = OutputColor.Error;
		public static readonly object ToStringEval = OutputColor.ToStringEval;
		public static readonly object ReplPrompt1 = OutputColor.ReplPrompt1;
		public static readonly object ReplPrompt2 = OutputColor.ReplPrompt2;
		public static readonly object ReplOutputText = OutputColor.ReplOutputText;
		public static readonly object ReplScriptOutputText = OutputColor.ReplScriptOutputText;
		public static readonly object Black = OutputColor.Black;
		public static readonly object Blue = OutputColor.Blue;
		public static readonly object Cyan = OutputColor.Cyan;
		public static readonly object DarkBlue = OutputColor.DarkBlue;
		public static readonly object DarkCyan = OutputColor.DarkCyan;
		public static readonly object DarkGray = OutputColor.DarkGray;
		public static readonly object DarkGreen = OutputColor.DarkGreen;
		public static readonly object DarkMagenta = OutputColor.DarkMagenta;
		public static readonly object DarkRed = OutputColor.DarkRed;
		public static readonly object DarkYellow = OutputColor.DarkYellow;
		public static readonly object Gray = OutputColor.Gray;
		public static readonly object Green = OutputColor.Green;
		public static readonly object Magenta = OutputColor.Magenta;
		public static readonly object Red = OutputColor.Red;
		public static readonly object White = OutputColor.White;
		public static readonly object Yellow = OutputColor.Yellow;
		public static readonly object InvBlack = OutputColor.InvBlack;
		public static readonly object InvBlue = OutputColor.InvBlue;
		public static readonly object InvCyan = OutputColor.InvCyan;
		public static readonly object InvDarkBlue = OutputColor.InvDarkBlue;
		public static readonly object InvDarkCyan = OutputColor.InvDarkCyan;
		public static readonly object InvDarkGray = OutputColor.InvDarkGray;
		public static readonly object InvDarkGreen = OutputColor.InvDarkGreen;
		public static readonly object InvDarkMagenta = OutputColor.InvDarkMagenta;
		public static readonly object InvDarkRed = OutputColor.InvDarkRed;
		public static readonly object InvDarkYellow = OutputColor.InvDarkYellow;
		public static readonly object InvGray = OutputColor.InvGray;
		public static readonly object InvGreen = OutputColor.InvGreen;
		public static readonly object InvMagenta = OutputColor.InvMagenta;
		public static readonly object InvRed = OutputColor.InvRed;
		public static readonly object InvWhite = OutputColor.InvWhite;
		public static readonly object InvYellow = OutputColor.InvYellow;
		public static readonly object DebugLogExceptionHandled = OutputColor.DebugLogExceptionHandled;
		public static readonly object DebugLogExceptionUnhandled = OutputColor.DebugLogExceptionUnhandled;
		public static readonly object DebugLogStepFiltering = OutputColor.DebugLogStepFiltering;
		public static readonly object DebugLogLoadModule = OutputColor.DebugLogLoadModule;
		public static readonly object DebugLogUnloadModule = OutputColor.DebugLogUnloadModule;
		public static readonly object DebugLogExitProcess = OutputColor.DebugLogExitProcess;
		public static readonly object DebugLogExitThread = OutputColor.DebugLogExitThread;
		public static readonly object DebugLogProgramOutput = OutputColor.DebugLogProgramOutput;
		public static readonly object DebugLogMDA = OutputColor.DebugLogMDA;
		public static readonly object DebugLogTimestamp = OutputColor.DebugLogTimestamp;
		public static readonly object LineNumber = OutputColor.LineNumber;
		public static readonly object Link = OutputColor.Link;
		public static readonly object VisibleWhitespace = OutputColor.VisibleWhitespace;
		public static readonly object SelectedText = OutputColor.SelectedText;
		public static readonly object InactiveSelectedText = OutputColor.InactiveSelectedText;
		public static readonly object HighlightedReference = OutputColor.HighlightedReference;
		public static readonly object HighlightedWrittenReference = OutputColor.HighlightedWrittenReference;
		public static readonly object HighlightedDefinition = OutputColor.HighlightedDefinition;
		public static readonly object CurrentStatement = OutputColor.CurrentStatement;
		public static readonly object ReturnStatement = OutputColor.ReturnStatement;
		public static readonly object SelectedReturnStatement = OutputColor.SelectedReturnStatement;
		public static readonly object BreakpointStatement = OutputColor.BreakpointStatement;
		public static readonly object DisabledBreakpointStatement = OutputColor.DisabledBreakpointStatement;
		public static readonly object SpecialCharacterBox = OutputColor.SpecialCharacterBox;
		public static readonly object SearchResultMarker = OutputColor.SearchResultMarker;
		public static readonly object CurrentLine = OutputColor.CurrentLine;
		public static readonly object CurrentLineNoFocus = OutputColor.CurrentLineNoFocus;
		public static readonly object HexText = OutputColor.HexText;
		public static readonly object HexOffset = OutputColor.HexOffset;
		public static readonly object HexByte0 = OutputColor.HexByte0;
		public static readonly object HexByte1 = OutputColor.HexByte1;
		public static readonly object HexByteError = OutputColor.HexByteError;
		public static readonly object HexAscii = OutputColor.HexAscii;
		public static readonly object HexCaret = OutputColor.HexCaret;
		public static readonly object HexInactiveCaret = OutputColor.HexInactiveCaret;
		public static readonly object HexSelection = OutputColor.HexSelection;
		public static readonly object GlyphMargin = OutputColor.GlyphMargin;

		public static object Box(this OutputColor OutputColor) {
			Debug.Assert(0 <= OutputColor && OutputColor < OutputColor.Last);
			int index = (int)OutputColor;
			if ((uint)index < (uint)boxedColors.Length)
				return boxedColors[index];
			return Text;
		}

		static readonly object[] boxedColors = new object[(int)OutputColor.Last] {
			Text,
			Operator,
			Punctuation,
			Number,
			Comment,
			Keyword,
			String,
			VerbatimString,
			Char,
			Namespace,
			Type,
			SealedType,
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
			ExcludedCode,
			XmlDocCommentAttributeName,
			XmlDocCommentAttributeQuotes,
			XmlDocCommentAttributeValue,
			XmlDocCommentCDataSection,
			XmlDocCommentComment,
			XmlDocCommentDelimiter,
			XmlDocCommentEntityReference,
			XmlDocCommentName,
			XmlDocCommentProcessingInstruction,
			XmlDocCommentText,
			XmlLiteralAttributeName,
			XmlLiteralAttributeQuotes,
			XmlLiteralAttributeValue,
			XmlLiteralCDataSection,
			XmlLiteralComment,
			XmlLiteralDelimiter,
			XmlLiteralEmbeddedExpression,
			XmlLiteralEntityReference,
			XmlLiteralName,
			XmlLiteralProcessingInstruction,
			XmlLiteralText,
			XmlAttributeName,
			XmlAttributeQuotes,
			XmlAttributeValue,
			XmlCDataSection,
			XmlComment,
			XmlDelimiter,
			XmlKeyword,
			XmlName,
			XmlProcessingInstruction,
			XmlText,
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
			XmlDocToolTipSummary,
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
			LineNumber,
			Link,
			VisibleWhitespace,
			SelectedText,
			InactiveSelectedText,
			HighlightedReference,
			HighlightedWrittenReference,
			HighlightedDefinition,
			CurrentStatement,
			ReturnStatement,
			SelectedReturnStatement,
			BreakpointStatement,
			DisabledBreakpointStatement,
			SpecialCharacterBox,
			SearchResultMarker,
			CurrentLine,
			CurrentLineNoFocus,
			HexText,
			HexOffset,
			HexByte0,
			HexByte1,
			HexByteError,
			HexAscii,
			HexCaret,
			HexInactiveCaret,
			HexSelection,
			GlyphMargin,
		};
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}
}
