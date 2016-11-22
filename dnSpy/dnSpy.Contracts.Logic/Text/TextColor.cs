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
	/// Text color
	/// </summary>
	public enum TextColor {
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
		Module,
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
		XmlAttribute,
		XmlAttributeQuotes,
		XmlAttributeValue,
		XmlCDataSection,
		XmlComment,
		XmlDelimiter,
		XmlKeyword,
		XmlName,
		XmlProcessingInstruction,
		XmlText,
		XamlAttribute,
		XamlAttributeQuotes,
		XamlAttributeValue,
		XamlCDataSection,
		XamlComment,
		XamlDelimiter,
		XamlKeyword,
		XamlMarkupExtensionClass,
		XamlMarkupExtensionParameterName,
		XamlMarkupExtensionParameterValue,
		XamlName,
		XamlProcessingInstruction,
		XamlText,
		XmlDocToolTipHeader,
		Assembly,
		AssemblyExe,
		AssemblyModule,
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
		ReplLineNumberInput1,
		ReplLineNumberInput2,
		ReplLineNumberOutput,
		VisibleWhitespace,
		SelectedText,
		InactiveSelectedText,
		HighlightedReference,
		HighlightedWrittenReference,
		HighlightedDefinition,
		CurrentStatement,
		CurrentStatementMarker,
		CallReturn,
		CallReturnMarker,
		ActiveStatementMarker,
		BreakpointStatement,
		BreakpointStatementMarker,
		SelectedBreakpointStatementMarker,
		DisabledBreakpointStatementMarker,
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
		BraceMatching,
		LineSeparator,
		FindMatchHighlightMarker,
		BlockStructureNamespace,
		BlockStructureType,
		BlockStructureModule,
		BlockStructureValueType,
		BlockStructureInterface,
		BlockStructureMethod,
		BlockStructureAccessor,
		BlockStructureAnonymousMethod,
		BlockStructureConstructor,
		BlockStructureDestructor,
		BlockStructureOperator,
		BlockStructureConditional,
		BlockStructureLoop,
		BlockStructureProperty,
		BlockStructureEvent,
		BlockStructureTry,
		BlockStructureCatch,
		BlockStructureFilter,
		BlockStructureFinally,
		BlockStructureFault,
		BlockStructureLock,
		BlockStructureUsing,
		BlockStructureFixed,
		BlockStructureSwitch,
		BlockStructureCase,
		BlockStructureLocalFunction,
		BlockStructureOther,
		BlockStructureXml,
		BlockStructureXaml,
		CompletionMatchHighlight,
		CompletionSuffix,
		SignatureHelpDocumentation,
		SignatureHelpCurrentParameter,
		SignatureHelpParameter,
		SignatureHelpParameterDocumentation,
		Url,
		HexPeDosHeader,
		HexPeFileHeader,
		HexPeOptionalHeader32,
		HexPeOptionalHeader64,
		HexPeSection,
		HexPeSectionName,
		HexCor20Header,
		HexStorageSignature,
		HexStorageHeader,
		HexStorageStream,
		HexStorageStreamName,
		HexStorageStreamNameInvalid,
		HexTablesStream,
		HexTableName,
		DocumentListMatchHighlight,
		GacMatchHighlight,
		AppSettingsTreeViewNodeMatchHighlight,
		AppSettingsTextMatchHighlight,
		HexCurrentLine,
		HexCurrentLineNoFocus,
		HexInactiveSelectedText,

		/// <summary>
		/// Must be last
		/// </summary>
		Last,
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}

	/// <summary>
	/// Boxed colors
	/// </summary>
	public static class BoxedTextColor {
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		public static readonly object Text = TextColor.Text;
		public static readonly object Operator = TextColor.Operator;
		public static readonly object Punctuation = TextColor.Punctuation;
		public static readonly object Number = TextColor.Number;
		public static readonly object Comment = TextColor.Comment;
		public static readonly object Keyword = TextColor.Keyword;
		public static readonly object String = TextColor.String;
		public static readonly object VerbatimString = TextColor.VerbatimString;
		public static readonly object Char = TextColor.Char;
		public static readonly object Namespace = TextColor.Namespace;
		public static readonly object Type = TextColor.Type;
		public static readonly object SealedType = TextColor.SealedType;
		public static readonly object StaticType = TextColor.StaticType;
		public static readonly object Delegate = TextColor.Delegate;
		public static readonly object Enum = TextColor.Enum;
		public static readonly object Interface = TextColor.Interface;
		public static readonly object ValueType = TextColor.ValueType;
		public static readonly object Module = TextColor.Module;
		public static readonly object TypeGenericParameter = TextColor.TypeGenericParameter;
		public static readonly object MethodGenericParameter = TextColor.MethodGenericParameter;
		public static readonly object InstanceMethod = TextColor.InstanceMethod;
		public static readonly object StaticMethod = TextColor.StaticMethod;
		public static readonly object ExtensionMethod = TextColor.ExtensionMethod;
		public static readonly object InstanceField = TextColor.InstanceField;
		public static readonly object EnumField = TextColor.EnumField;
		public static readonly object LiteralField = TextColor.LiteralField;
		public static readonly object StaticField = TextColor.StaticField;
		public static readonly object InstanceEvent = TextColor.InstanceEvent;
		public static readonly object StaticEvent = TextColor.StaticEvent;
		public static readonly object InstanceProperty = TextColor.InstanceProperty;
		public static readonly object StaticProperty = TextColor.StaticProperty;
		public static readonly object Local = TextColor.Local;
		public static readonly object Parameter = TextColor.Parameter;
		public static readonly object PreprocessorKeyword = TextColor.PreprocessorKeyword;
		public static readonly object PreprocessorText = TextColor.PreprocessorText;
		public static readonly object Label = TextColor.Label;
		public static readonly object OpCode = TextColor.OpCode;
		public static readonly object ILDirective = TextColor.ILDirective;
		public static readonly object ILModule = TextColor.ILModule;
		public static readonly object ExcludedCode = TextColor.ExcludedCode;
		public static readonly object XmlDocCommentAttributeName = TextColor.XmlDocCommentAttributeName;
		public static readonly object XmlDocCommentAttributeQuotes = TextColor.XmlDocCommentAttributeQuotes;
		public static readonly object XmlDocCommentAttributeValue = TextColor.XmlDocCommentAttributeValue;
		public static readonly object XmlDocCommentCDataSection = TextColor.XmlDocCommentCDataSection;
		public static readonly object XmlDocCommentComment = TextColor.XmlDocCommentComment;
		public static readonly object XmlDocCommentDelimiter = TextColor.XmlDocCommentDelimiter;
		public static readonly object XmlDocCommentEntityReference = TextColor.XmlDocCommentEntityReference;
		public static readonly object XmlDocCommentName = TextColor.XmlDocCommentName;
		public static readonly object XmlDocCommentProcessingInstruction = TextColor.XmlDocCommentProcessingInstruction;
		public static readonly object XmlDocCommentText = TextColor.XmlDocCommentText;
		public static readonly object XmlLiteralAttributeName = TextColor.XmlLiteralAttributeName;
		public static readonly object XmlLiteralAttributeQuotes = TextColor.XmlLiteralAttributeQuotes;
		public static readonly object XmlLiteralAttributeValue = TextColor.XmlLiteralAttributeValue;
		public static readonly object XmlLiteralCDataSection = TextColor.XmlLiteralCDataSection;
		public static readonly object XmlLiteralComment = TextColor.XmlLiteralComment;
		public static readonly object XmlLiteralDelimiter = TextColor.XmlLiteralDelimiter;
		public static readonly object XmlLiteralEmbeddedExpression = TextColor.XmlLiteralEmbeddedExpression;
		public static readonly object XmlLiteralEntityReference = TextColor.XmlLiteralEntityReference;
		public static readonly object XmlLiteralName = TextColor.XmlLiteralName;
		public static readonly object XmlLiteralProcessingInstruction = TextColor.XmlLiteralProcessingInstruction;
		public static readonly object XmlLiteralText = TextColor.XmlLiteralText;
		public static readonly object XmlAttribute = TextColor.XmlAttribute;
		public static readonly object XmlAttributeQuotes = TextColor.XmlAttributeQuotes;
		public static readonly object XmlAttributeValue = TextColor.XmlAttributeValue;
		public static readonly object XmlCDataSection = TextColor.XmlCDataSection;
		public static readonly object XmlComment = TextColor.XmlComment;
		public static readonly object XmlDelimiter = TextColor.XmlDelimiter;
		public static readonly object XmlKeyword = TextColor.XmlKeyword;
		public static readonly object XmlName = TextColor.XmlName;
		public static readonly object XmlProcessingInstruction = TextColor.XmlProcessingInstruction;
		public static readonly object XmlText = TextColor.XmlText;
		public static readonly object XamlAttribute = TextColor.XamlAttribute;
		public static readonly object XamlAttributeQuotes = TextColor.XamlAttributeQuotes;
		public static readonly object XamlAttributeValue = TextColor.XamlAttributeValue;
		public static readonly object XamlCDataSection = TextColor.XamlCDataSection;
		public static readonly object XamlComment = TextColor.XamlComment;
		public static readonly object XamlDelimiter = TextColor.XamlDelimiter;
		public static readonly object XamlKeyword = TextColor.XamlKeyword;
		public static readonly object XamlMarkupExtensionClass = TextColor.XamlMarkupExtensionClass;
		public static readonly object XamlMarkupExtensionParameterName = TextColor.XamlMarkupExtensionParameterName;
		public static readonly object XamlMarkupExtensionParameterValue = TextColor.XamlMarkupExtensionParameterValue;
		public static readonly object XamlName = TextColor.XamlName;
		public static readonly object XamlProcessingInstruction = TextColor.XamlProcessingInstruction;
		public static readonly object XamlText = TextColor.XamlText;
		public static readonly object XmlDocToolTipHeader = TextColor.XmlDocToolTipHeader;
		public static readonly object Assembly = TextColor.Assembly;
		public static readonly object AssemblyExe = TextColor.AssemblyExe;
		public static readonly object AssemblyModule = TextColor.AssemblyModule;
		public static readonly object DirectoryPart = TextColor.DirectoryPart;
		public static readonly object FileNameNoExtension = TextColor.FileNameNoExtension;
		public static readonly object FileExtension = TextColor.FileExtension;
		public static readonly object Error = TextColor.Error;
		public static readonly object ToStringEval = TextColor.ToStringEval;
		public static readonly object ReplPrompt1 = TextColor.ReplPrompt1;
		public static readonly object ReplPrompt2 = TextColor.ReplPrompt2;
		public static readonly object ReplOutputText = TextColor.ReplOutputText;
		public static readonly object ReplScriptOutputText = TextColor.ReplScriptOutputText;
		public static readonly object Black = TextColor.Black;
		public static readonly object Blue = TextColor.Blue;
		public static readonly object Cyan = TextColor.Cyan;
		public static readonly object DarkBlue = TextColor.DarkBlue;
		public static readonly object DarkCyan = TextColor.DarkCyan;
		public static readonly object DarkGray = TextColor.DarkGray;
		public static readonly object DarkGreen = TextColor.DarkGreen;
		public static readonly object DarkMagenta = TextColor.DarkMagenta;
		public static readonly object DarkRed = TextColor.DarkRed;
		public static readonly object DarkYellow = TextColor.DarkYellow;
		public static readonly object Gray = TextColor.Gray;
		public static readonly object Green = TextColor.Green;
		public static readonly object Magenta = TextColor.Magenta;
		public static readonly object Red = TextColor.Red;
		public static readonly object White = TextColor.White;
		public static readonly object Yellow = TextColor.Yellow;
		public static readonly object InvBlack = TextColor.InvBlack;
		public static readonly object InvBlue = TextColor.InvBlue;
		public static readonly object InvCyan = TextColor.InvCyan;
		public static readonly object InvDarkBlue = TextColor.InvDarkBlue;
		public static readonly object InvDarkCyan = TextColor.InvDarkCyan;
		public static readonly object InvDarkGray = TextColor.InvDarkGray;
		public static readonly object InvDarkGreen = TextColor.InvDarkGreen;
		public static readonly object InvDarkMagenta = TextColor.InvDarkMagenta;
		public static readonly object InvDarkRed = TextColor.InvDarkRed;
		public static readonly object InvDarkYellow = TextColor.InvDarkYellow;
		public static readonly object InvGray = TextColor.InvGray;
		public static readonly object InvGreen = TextColor.InvGreen;
		public static readonly object InvMagenta = TextColor.InvMagenta;
		public static readonly object InvRed = TextColor.InvRed;
		public static readonly object InvWhite = TextColor.InvWhite;
		public static readonly object InvYellow = TextColor.InvYellow;
		public static readonly object DebugLogExceptionHandled = TextColor.DebugLogExceptionHandled;
		public static readonly object DebugLogExceptionUnhandled = TextColor.DebugLogExceptionUnhandled;
		public static readonly object DebugLogStepFiltering = TextColor.DebugLogStepFiltering;
		public static readonly object DebugLogLoadModule = TextColor.DebugLogLoadModule;
		public static readonly object DebugLogUnloadModule = TextColor.DebugLogUnloadModule;
		public static readonly object DebugLogExitProcess = TextColor.DebugLogExitProcess;
		public static readonly object DebugLogExitThread = TextColor.DebugLogExitThread;
		public static readonly object DebugLogProgramOutput = TextColor.DebugLogProgramOutput;
		public static readonly object DebugLogMDA = TextColor.DebugLogMDA;
		public static readonly object DebugLogTimestamp = TextColor.DebugLogTimestamp;
		public static readonly object LineNumber = TextColor.LineNumber;
		public static readonly object ReplLineNumberInput1 = TextColor.ReplLineNumberInput1;
		public static readonly object ReplLineNumberInput2 = TextColor.ReplLineNumberInput2;
		public static readonly object ReplLineNumberOutput = TextColor.ReplLineNumberOutput;
		public static readonly object VisibleWhitespace = TextColor.VisibleWhitespace;
		public static readonly object SelectedText = TextColor.SelectedText;
		public static readonly object InactiveSelectedText = TextColor.InactiveSelectedText;
		public static readonly object HighlightedReference = TextColor.HighlightedReference;
		public static readonly object HighlightedWrittenReference = TextColor.HighlightedWrittenReference;
		public static readonly object HighlightedDefinition = TextColor.HighlightedDefinition;
		public static readonly object CurrentStatement = TextColor.CurrentStatement;
		public static readonly object CurrentStatementMarker = TextColor.CurrentStatementMarker;
		public static readonly object CallReturn = TextColor.CallReturn;
		public static readonly object CallReturnMarker = TextColor.CallReturnMarker;
		public static readonly object ActiveStatementMarker = TextColor.ActiveStatementMarker;
		public static readonly object BreakpointStatement = TextColor.BreakpointStatement;
		public static readonly object BreakpointStatementMarker = TextColor.BreakpointStatementMarker;
		public static readonly object SelectedBreakpointStatementMarker = TextColor.SelectedBreakpointStatementMarker;
		public static readonly object DisabledBreakpointStatementMarker = TextColor.DisabledBreakpointStatementMarker;
		public static readonly object CurrentLine = TextColor.CurrentLine;
		public static readonly object CurrentLineNoFocus = TextColor.CurrentLineNoFocus;
		public static readonly object HexText = TextColor.HexText;
		public static readonly object HexOffset = TextColor.HexOffset;
		public static readonly object HexByte0 = TextColor.HexByte0;
		public static readonly object HexByte1 = TextColor.HexByte1;
		public static readonly object HexByteError = TextColor.HexByteError;
		public static readonly object HexAscii = TextColor.HexAscii;
		public static readonly object HexCaret = TextColor.HexCaret;
		public static readonly object HexInactiveCaret = TextColor.HexInactiveCaret;
		public static readonly object HexSelection = TextColor.HexSelection;
		public static readonly object GlyphMargin = TextColor.GlyphMargin;
		public static readonly object BraceMatching = TextColor.BraceMatching;
		public static readonly object LineSeparator = TextColor.LineSeparator;
		public static readonly object FindMatchHighlightMarker = TextColor.FindMatchHighlightMarker;
		public static readonly object BlockStructureNamespace = TextColor.BlockStructureNamespace;
		public static readonly object BlockStructureType = TextColor.BlockStructureType;
		public static readonly object BlockStructureModule = TextColor.BlockStructureModule;
		public static readonly object BlockStructureValueType = TextColor.BlockStructureValueType;
		public static readonly object BlockStructureInterface = TextColor.BlockStructureInterface;
		public static readonly object BlockStructureMethod = TextColor.BlockStructureMethod;
		public static readonly object BlockStructureAccessor = TextColor.BlockStructureAccessor;
		public static readonly object BlockStructureAnonymousMethod = TextColor.BlockStructureAnonymousMethod;
		public static readonly object BlockStructureConstructor = TextColor.BlockStructureConstructor;
		public static readonly object BlockStructureDestructor = TextColor.BlockStructureDestructor;
		public static readonly object BlockStructureOperator = TextColor.BlockStructureOperator;
		public static readonly object BlockStructureConditional = TextColor.BlockStructureConditional;
		public static readonly object BlockStructureLoop = TextColor.BlockStructureLoop;
		public static readonly object BlockStructureProperty = TextColor.BlockStructureProperty;
		public static readonly object BlockStructureEvent = TextColor.BlockStructureEvent;
		public static readonly object BlockStructureTry = TextColor.BlockStructureTry;
		public static readonly object BlockStructureCatch = TextColor.BlockStructureCatch;
		public static readonly object BlockStructureFilter = TextColor.BlockStructureFilter;
		public static readonly object BlockStructureFinally = TextColor.BlockStructureFinally;
		public static readonly object BlockStructureFault = TextColor.BlockStructureFault;
		public static readonly object BlockStructureLock = TextColor.BlockStructureLock;
		public static readonly object BlockStructureUsing = TextColor.BlockStructureUsing;
		public static readonly object BlockStructureFixed = TextColor.BlockStructureFixed;
		public static readonly object BlockStructureSwitch = TextColor.BlockStructureSwitch;
		public static readonly object BlockStructureCase = TextColor.BlockStructureCase;
		public static readonly object BlockStructureLocalFunction = TextColor.BlockStructureLocalFunction;
		public static readonly object BlockStructureOther = TextColor.BlockStructureOther;
		public static readonly object BlockStructureXml = TextColor.BlockStructureXml;
		public static readonly object BlockStructureXaml = TextColor.BlockStructureXaml;
		public static readonly object CompletionMatchHighlight = TextColor.CompletionMatchHighlight;
		public static readonly object CompletionSuffix = TextColor.CompletionSuffix;
		public static readonly object SignatureHelpDocumentation = TextColor.SignatureHelpDocumentation;
		public static readonly object SignatureHelpCurrentParameter = TextColor.SignatureHelpCurrentParameter;
		public static readonly object SignatureHelpParameter = TextColor.SignatureHelpParameter;
		public static readonly object SignatureHelpParameterDocumentation = TextColor.SignatureHelpParameterDocumentation;
		public static readonly object Url = TextColor.Url;
		public static readonly object HexPeDosHeader = TextColor.HexPeDosHeader;
		public static readonly object HexPeFileHeader = TextColor.HexPeFileHeader;
		public static readonly object HexPeOptionalHeader32 = TextColor.HexPeOptionalHeader32;
		public static readonly object HexPeOptionalHeader64 = TextColor.HexPeOptionalHeader64;
		public static readonly object HexPeSection = TextColor.HexPeSection;
		public static readonly object HexPeSectionName = TextColor.HexPeSectionName;
		public static readonly object HexCor20Header = TextColor.HexCor20Header;
		public static readonly object HexStorageSignature = TextColor.HexStorageSignature;
		public static readonly object HexStorageHeader = TextColor.HexStorageHeader;
		public static readonly object HexStorageStream = TextColor.HexStorageStream;
		public static readonly object HexStorageStreamName = TextColor.HexStorageStreamName;
		public static readonly object HexStorageStreamNameInvalid = TextColor.HexStorageStreamNameInvalid;
		public static readonly object HexTablesStream = TextColor.HexTablesStream;
		public static readonly object HexTableName = TextColor.HexTableName;
		public static readonly object DocumentListMatchHighlight = TextColor.DocumentListMatchHighlight;
		public static readonly object GacMatchHighlight = TextColor.GacMatchHighlight;
		public static readonly object AppSettingsTreeViewNodeMatchHighlight = TextColor.AppSettingsTreeViewNodeMatchHighlight;
		public static readonly object AppSettingsTextMatchHighlight = TextColor.AppSettingsTextMatchHighlight;
		public static readonly object HexCurrentLine = TextColor.HexCurrentLine;
		public static readonly object HexCurrentLineNoFocus = TextColor.HexCurrentLineNoFocus;
		public static readonly object HexInactiveSelectedText = TextColor.HexInactiveSelectedText;

		/// <summary>
		/// Boxes <paramref name="color"/>
		/// </summary>
		/// <param name="color">Color to box</param>
		/// <returns></returns>
		public static object Box(this TextColor color) {
			Debug.Assert(0 <= color && color < TextColor.Last);
			int index = (int)color;
			if ((uint)index < (uint)boxedColors.Length)
				return boxedColors[index];
			return Text;
		}

		static readonly object[] boxedColors = new object[(int)TextColor.Last] {
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
			Module,
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
			XmlAttribute,
			XmlAttributeQuotes,
			XmlAttributeValue,
			XmlCDataSection,
			XmlComment,
			XmlDelimiter,
			XmlKeyword,
			XmlName,
			XmlProcessingInstruction,
			XmlText,
			XamlAttribute,
			XamlAttributeQuotes,
			XamlAttributeValue,
			XamlCDataSection,
			XamlComment,
			XamlDelimiter,
			XamlKeyword,
			XamlMarkupExtensionClass,
			XamlMarkupExtensionParameterName,
			XamlMarkupExtensionParameterValue,
			XamlName,
			XamlProcessingInstruction,
			XamlText,
			XmlDocToolTipHeader,
			Assembly,
			AssemblyExe,
			AssemblyModule,
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
			ReplLineNumberInput1,
			ReplLineNumberInput2,
			ReplLineNumberOutput,
			VisibleWhitespace,
			SelectedText,
			InactiveSelectedText,
			HighlightedReference,
			HighlightedWrittenReference,
			HighlightedDefinition,
			CurrentStatement,
			CurrentStatementMarker,
			CallReturn,
			CallReturnMarker,
			ActiveStatementMarker,
			BreakpointStatement,
			BreakpointStatementMarker,
			SelectedBreakpointStatementMarker,
			DisabledBreakpointStatementMarker,
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
			BraceMatching,
			LineSeparator,
			FindMatchHighlightMarker,
			BlockStructureNamespace,
			BlockStructureType,
			BlockStructureModule,
			BlockStructureValueType,
			BlockStructureInterface,
			BlockStructureMethod,
			BlockStructureAccessor,
			BlockStructureAnonymousMethod,
			BlockStructureConstructor,
			BlockStructureDestructor,
			BlockStructureOperator,
			BlockStructureConditional,
			BlockStructureLoop,
			BlockStructureProperty,
			BlockStructureEvent,
			BlockStructureTry,
			BlockStructureCatch,
			BlockStructureFilter,
			BlockStructureFinally,
			BlockStructureFault,
			BlockStructureLock,
			BlockStructureUsing,
			BlockStructureFixed,
			BlockStructureSwitch,
			BlockStructureCase,
			BlockStructureLocalFunction,
			BlockStructureOther,
			BlockStructureXml,
			BlockStructureXaml,
			CompletionMatchHighlight,
			CompletionSuffix,
			SignatureHelpDocumentation,
			SignatureHelpCurrentParameter,
			SignatureHelpParameter,
			SignatureHelpParameterDocumentation,
			Url,
			HexPeDosHeader,
			HexPeFileHeader,
			HexPeOptionalHeader32,
			HexPeOptionalHeader64,
			HexPeSection,
			HexPeSectionName,
			HexCor20Header,
			HexStorageSignature,
			HexStorageHeader,
			HexStorageStream,
			HexStorageStreamName,
			HexStorageStreamNameInvalid,
			HexTablesStream,
			HexTableName,
			DocumentListMatchHighlight,
			GacMatchHighlight,
			AppSettingsTreeViewNodeMatchHighlight,
			AppSettingsTextMatchHighlight,
			HexCurrentLine,
			HexCurrentLineNoFocus,
			HexInactiveSelectedText,
		};
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}
}
