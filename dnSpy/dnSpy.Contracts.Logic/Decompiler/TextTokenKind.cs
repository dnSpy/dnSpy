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

using System;
using System.Diagnostics;
using dnlib.DotNet;

#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
namespace dnSpy.Contracts.Decompiler {
	public enum TextTokenKind : byte {
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

		// If you add a new one, also update ColorType

		/// <summary>
		/// Must be last
		/// </summary>
		Last,
	}

	public static class BoxedTextTokenKind {
		public static readonly object Text = TextTokenKind.Text;
		public static readonly object Operator = TextTokenKind.Operator;
		public static readonly object Punctuation = TextTokenKind.Punctuation;
		public static readonly object Number = TextTokenKind.Number;
		public static readonly object Comment = TextTokenKind.Comment;
		public static readonly object Keyword = TextTokenKind.Keyword;
		public static readonly object String = TextTokenKind.String;
		public static readonly object VerbatimString = TextTokenKind.VerbatimString;
		public static readonly object Char = TextTokenKind.Char;
		public static readonly object Namespace = TextTokenKind.Namespace;
		public static readonly object Type = TextTokenKind.Type;
		public static readonly object SealedType = TextTokenKind.SealedType;
		public static readonly object StaticType = TextTokenKind.StaticType;
		public static readonly object Delegate = TextTokenKind.Delegate;
		public static readonly object Enum = TextTokenKind.Enum;
		public static readonly object Interface = TextTokenKind.Interface;
		public static readonly object ValueType = TextTokenKind.ValueType;
		public static readonly object TypeGenericParameter = TextTokenKind.TypeGenericParameter;
		public static readonly object MethodGenericParameter = TextTokenKind.MethodGenericParameter;
		public static readonly object InstanceMethod = TextTokenKind.InstanceMethod;
		public static readonly object StaticMethod = TextTokenKind.StaticMethod;
		public static readonly object ExtensionMethod = TextTokenKind.ExtensionMethod;
		public static readonly object InstanceField = TextTokenKind.InstanceField;
		public static readonly object EnumField = TextTokenKind.EnumField;
		public static readonly object LiteralField = TextTokenKind.LiteralField;
		public static readonly object StaticField = TextTokenKind.StaticField;
		public static readonly object InstanceEvent = TextTokenKind.InstanceEvent;
		public static readonly object StaticEvent = TextTokenKind.StaticEvent;
		public static readonly object InstanceProperty = TextTokenKind.InstanceProperty;
		public static readonly object StaticProperty = TextTokenKind.StaticProperty;
		public static readonly object Local = TextTokenKind.Local;
		public static readonly object Parameter = TextTokenKind.Parameter;
		public static readonly object PreprocessorKeyword = TextTokenKind.PreprocessorKeyword;
		public static readonly object PreprocessorText = TextTokenKind.PreprocessorText;
		public static readonly object Label = TextTokenKind.Label;
		public static readonly object OpCode = TextTokenKind.OpCode;
		public static readonly object ILDirective = TextTokenKind.ILDirective;
		public static readonly object ILModule = TextTokenKind.ILModule;
		public static readonly object ExcludedCode = TextTokenKind.ExcludedCode;
		public static readonly object XmlDocCommentAttributeName = TextTokenKind.XmlDocCommentAttributeName;
		public static readonly object XmlDocCommentAttributeQuotes = TextTokenKind.XmlDocCommentAttributeQuotes;
		public static readonly object XmlDocCommentAttributeValue = TextTokenKind.XmlDocCommentAttributeValue;
		public static readonly object XmlDocCommentCDataSection = TextTokenKind.XmlDocCommentCDataSection;
		public static readonly object XmlDocCommentComment = TextTokenKind.XmlDocCommentComment;
		public static readonly object XmlDocCommentDelimiter = TextTokenKind.XmlDocCommentDelimiter;
		public static readonly object XmlDocCommentEntityReference = TextTokenKind.XmlDocCommentEntityReference;
		public static readonly object XmlDocCommentName = TextTokenKind.XmlDocCommentName;
		public static readonly object XmlDocCommentProcessingInstruction = TextTokenKind.XmlDocCommentProcessingInstruction;
		public static readonly object XmlDocCommentText = TextTokenKind.XmlDocCommentText;
		public static readonly object XmlLiteralAttributeName = TextTokenKind.XmlLiteralAttributeName;
		public static readonly object XmlLiteralAttributeQuotes = TextTokenKind.XmlLiteralAttributeQuotes;
		public static readonly object XmlLiteralAttributeValue = TextTokenKind.XmlLiteralAttributeValue;
		public static readonly object XmlLiteralCDataSection = TextTokenKind.XmlLiteralCDataSection;
		public static readonly object XmlLiteralComment = TextTokenKind.XmlLiteralComment;
		public static readonly object XmlLiteralDelimiter = TextTokenKind.XmlLiteralDelimiter;
		public static readonly object XmlLiteralEmbeddedExpression = TextTokenKind.XmlLiteralEmbeddedExpression;
		public static readonly object XmlLiteralEntityReference = TextTokenKind.XmlLiteralEntityReference;
		public static readonly object XmlLiteralName = TextTokenKind.XmlLiteralName;
		public static readonly object XmlLiteralProcessingInstruction = TextTokenKind.XmlLiteralProcessingInstruction;
		public static readonly object XmlLiteralText = TextTokenKind.XmlLiteralText;
		public static readonly object XmlAttributeName = TextTokenKind.XmlAttributeName;
		public static readonly object XmlAttributeQuotes = TextTokenKind.XmlAttributeQuotes;
		public static readonly object XmlAttributeValue = TextTokenKind.XmlAttributeValue;
		public static readonly object XmlCDataSection = TextTokenKind.XmlCDataSection;
		public static readonly object XmlComment = TextTokenKind.XmlComment;
		public static readonly object XmlDelimiter = TextTokenKind.XmlDelimiter;
		public static readonly object XmlKeyword = TextTokenKind.XmlKeyword;
		public static readonly object XmlName = TextTokenKind.XmlName;
		public static readonly object XmlProcessingInstruction = TextTokenKind.XmlProcessingInstruction;
		public static readonly object XmlText = TextTokenKind.XmlText;
		public static readonly object XmlDocToolTipColon = TextTokenKind.XmlDocToolTipColon;
		public static readonly object XmlDocToolTipExample = TextTokenKind.XmlDocToolTipExample;
		public static readonly object XmlDocToolTipExceptionCref = TextTokenKind.XmlDocToolTipExceptionCref;
		public static readonly object XmlDocToolTipReturns = TextTokenKind.XmlDocToolTipReturns;
		public static readonly object XmlDocToolTipSeeCref = TextTokenKind.XmlDocToolTipSeeCref;
		public static readonly object XmlDocToolTipSeeLangword = TextTokenKind.XmlDocToolTipSeeLangword;
		public static readonly object XmlDocToolTipSeeAlso = TextTokenKind.XmlDocToolTipSeeAlso;
		public static readonly object XmlDocToolTipSeeAlsoCref = TextTokenKind.XmlDocToolTipSeeAlsoCref;
		public static readonly object XmlDocToolTipParamRefName = TextTokenKind.XmlDocToolTipParamRefName;
		public static readonly object XmlDocToolTipParamName = TextTokenKind.XmlDocToolTipParamName;
		public static readonly object XmlDocToolTipTypeParamName = TextTokenKind.XmlDocToolTipTypeParamName;
		public static readonly object XmlDocToolTipValue = TextTokenKind.XmlDocToolTipValue;
		public static readonly object XmlDocToolTipSummary = TextTokenKind.XmlDocToolTipSummary;
		public static readonly object XmlDocToolTipText = TextTokenKind.XmlDocToolTipText;
		public static readonly object Assembly = TextTokenKind.Assembly;
		public static readonly object AssemblyExe = TextTokenKind.AssemblyExe;
		public static readonly object Module = TextTokenKind.Module;
		public static readonly object DirectoryPart = TextTokenKind.DirectoryPart;
		public static readonly object FileNameNoExtension = TextTokenKind.FileNameNoExtension;
		public static readonly object FileExtension = TextTokenKind.FileExtension;
		public static readonly object Error = TextTokenKind.Error;
		public static readonly object ToStringEval = TextTokenKind.ToStringEval;
		public static readonly object ReplPrompt1 = TextTokenKind.ReplPrompt1;
		public static readonly object ReplPrompt2 = TextTokenKind.ReplPrompt2;
		public static readonly object ReplOutputText = TextTokenKind.ReplOutputText;
		public static readonly object ReplScriptOutputText = TextTokenKind.ReplScriptOutputText;
		public static readonly object Black = TextTokenKind.Black;
		public static readonly object Blue = TextTokenKind.Blue;
		public static readonly object Cyan = TextTokenKind.Cyan;
		public static readonly object DarkBlue = TextTokenKind.DarkBlue;
		public static readonly object DarkCyan = TextTokenKind.DarkCyan;
		public static readonly object DarkGray = TextTokenKind.DarkGray;
		public static readonly object DarkGreen = TextTokenKind.DarkGreen;
		public static readonly object DarkMagenta = TextTokenKind.DarkMagenta;
		public static readonly object DarkRed = TextTokenKind.DarkRed;
		public static readonly object DarkYellow = TextTokenKind.DarkYellow;
		public static readonly object Gray = TextTokenKind.Gray;
		public static readonly object Green = TextTokenKind.Green;
		public static readonly object Magenta = TextTokenKind.Magenta;
		public static readonly object Red = TextTokenKind.Red;
		public static readonly object White = TextTokenKind.White;
		public static readonly object Yellow = TextTokenKind.Yellow;
		public static readonly object InvBlack = TextTokenKind.InvBlack;
		public static readonly object InvBlue = TextTokenKind.InvBlue;
		public static readonly object InvCyan = TextTokenKind.InvCyan;
		public static readonly object InvDarkBlue = TextTokenKind.InvDarkBlue;
		public static readonly object InvDarkCyan = TextTokenKind.InvDarkCyan;
		public static readonly object InvDarkGray = TextTokenKind.InvDarkGray;
		public static readonly object InvDarkGreen = TextTokenKind.InvDarkGreen;
		public static readonly object InvDarkMagenta = TextTokenKind.InvDarkMagenta;
		public static readonly object InvDarkRed = TextTokenKind.InvDarkRed;
		public static readonly object InvDarkYellow = TextTokenKind.InvDarkYellow;
		public static readonly object InvGray = TextTokenKind.InvGray;
		public static readonly object InvGreen = TextTokenKind.InvGreen;
		public static readonly object InvMagenta = TextTokenKind.InvMagenta;
		public static readonly object InvRed = TextTokenKind.InvRed;
		public static readonly object InvWhite = TextTokenKind.InvWhite;
		public static readonly object InvYellow = TextTokenKind.InvYellow;
		public static readonly object DebugLogExceptionHandled = TextTokenKind.DebugLogExceptionHandled;
		public static readonly object DebugLogExceptionUnhandled = TextTokenKind.DebugLogExceptionUnhandled;
		public static readonly object DebugLogStepFiltering = TextTokenKind.DebugLogStepFiltering;
		public static readonly object DebugLogLoadModule = TextTokenKind.DebugLogLoadModule;
		public static readonly object DebugLogUnloadModule = TextTokenKind.DebugLogUnloadModule;
		public static readonly object DebugLogExitProcess = TextTokenKind.DebugLogExitProcess;
		public static readonly object DebugLogExitThread = TextTokenKind.DebugLogExitThread;
		public static readonly object DebugLogProgramOutput = TextTokenKind.DebugLogProgramOutput;
		public static readonly object DebugLogMDA = TextTokenKind.DebugLogMDA;
		public static readonly object DebugLogTimestamp = TextTokenKind.DebugLogTimestamp;
		public static readonly object LineNumber = TextTokenKind.LineNumber;
		public static readonly object Link = TextTokenKind.Link;
		public static readonly object VisibleWhitespace = TextTokenKind.VisibleWhitespace;
		public static readonly object SelectedText = TextTokenKind.SelectedText;
		public static readonly object InactiveSelectedText = TextTokenKind.InactiveSelectedText;
		public static readonly object HighlightedReference = TextTokenKind.HighlightedReference;
		public static readonly object HighlightedWrittenReference = TextTokenKind.HighlightedWrittenReference;
		public static readonly object HighlightedDefinition = TextTokenKind.HighlightedDefinition;
		public static readonly object CurrentStatement = TextTokenKind.CurrentStatement;
		public static readonly object ReturnStatement = TextTokenKind.ReturnStatement;
		public static readonly object SelectedReturnStatement = TextTokenKind.SelectedReturnStatement;
		public static readonly object BreakpointStatement = TextTokenKind.BreakpointStatement;
		public static readonly object DisabledBreakpointStatement = TextTokenKind.DisabledBreakpointStatement;
		public static readonly object SpecialCharacterBox = TextTokenKind.SpecialCharacterBox;
		public static readonly object SearchResultMarker = TextTokenKind.SearchResultMarker;
		public static readonly object CurrentLine = TextTokenKind.CurrentLine;
		public static readonly object CurrentLineNoFocus = TextTokenKind.CurrentLineNoFocus;
		public static readonly object HexText = TextTokenKind.HexText;
		public static readonly object HexOffset = TextTokenKind.HexOffset;
		public static readonly object HexByte0 = TextTokenKind.HexByte0;
		public static readonly object HexByte1 = TextTokenKind.HexByte1;
		public static readonly object HexByteError = TextTokenKind.HexByteError;
		public static readonly object HexAscii = TextTokenKind.HexAscii;
		public static readonly object HexCaret = TextTokenKind.HexCaret;
		public static readonly object HexInactiveCaret = TextTokenKind.HexInactiveCaret;
		public static readonly object HexSelection = TextTokenKind.HexSelection;
		public static readonly object GlyphMargin = TextTokenKind.GlyphMargin;

		public static object Box(this TextTokenKind textTokenKind) {
			Debug.Assert(0 <= textTokenKind && textTokenKind < TextTokenKind.Last);
			int index = (int)textTokenKind;
			if ((uint)index < (uint)boxedColors.Length)
				return boxedColors[index];
			return Text;
		}

		static readonly object[] boxedColors = new object[(int)TextTokenKind.Last] {
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
	}

	public static class TextTokenKindUtils {
		public static object GetTextTokenKind(TypeDef td) {
			if (td == null)
				return BoxedTextTokenKind.Text;

			if (td.IsInterface)
				return BoxedTextTokenKind.Interface;
			if (td.IsEnum)
				return BoxedTextTokenKind.Enum;
			if (td.IsValueType)
				return BoxedTextTokenKind.ValueType;

			if (td.IsDelegate)
				return BoxedTextTokenKind.Delegate;

			if (td.IsSealed && td.IsAbstract) {
				var bt = td.BaseType;
				if (bt != null && bt.DefinitionAssembly.IsCorLib()) {
					var baseTr = bt as TypeRef;
					if (baseTr != null) {
						if (baseTr.Namespace == systemString && baseTr.Name == objectString)
							return BoxedTextTokenKind.StaticType;
					}
					else {
						var baseTd = bt as TypeDef;
						if (baseTd.Namespace == systemString && baseTd.Name == objectString)
							return BoxedTextTokenKind.StaticType;
					}
				}
			}

			if (td.IsSealed)
				return BoxedTextTokenKind.SealedType;
			return BoxedTextTokenKind.Type;
		}
		static readonly UTF8String systemString = new UTF8String("System");
		static readonly UTF8String objectString = new UTF8String("Object");

		public static object GetTextTokenKind(TypeRef tr) {
			if (tr == null)
				return BoxedTextTokenKind.Text;

			var td = tr.Resolve();
			if (td != null)
				return GetTextTokenKind(td);

			return BoxedTextTokenKind.Type;
		}

		static readonly UTF8String systemRuntimeCompilerServicesString = new UTF8String("System.Runtime.CompilerServices");
		static readonly UTF8String extensionAttributeString = new UTF8String("ExtensionAttribute");
		public static object GetTextTokenKind(IMemberRef r) {
			if (r == null)
				return BoxedTextTokenKind.Text;

			if (r.IsField) {
				var fd = ((IField)r).ResolveFieldDef();
				if (fd == null)
					return BoxedTextTokenKind.InstanceField;
				if (fd.DeclaringType.IsEnum)
					return BoxedTextTokenKind.EnumField;
				if (fd.IsLiteral)
					return BoxedTextTokenKind.LiteralField;
				if (fd.IsStatic)
					return BoxedTextTokenKind.StaticField;
				return BoxedTextTokenKind.InstanceField;
			}
			if (r.IsMethod) {
				var mr = (IMethod)r;
				if (mr.MethodSig == null)
					return BoxedTextTokenKind.InstanceMethod;
				var md = mr.ResolveMethodDef();
				if (md != null && md.IsConstructor)
					return GetTextTokenKind(md.DeclaringType);
				if (!mr.MethodSig.HasThis) {
					if (md != null && md.IsDefined(systemRuntimeCompilerServicesString, extensionAttributeString))
						return BoxedTextTokenKind.ExtensionMethod;
					return BoxedTextTokenKind.StaticMethod;
				}
				return BoxedTextTokenKind.InstanceMethod;
			}
			if (r.IsPropertyDef) {
				var p = (PropertyDef)r;
				return GetTextTokenKind(p.GetMethod ?? p.SetMethod, BoxedTextTokenKind.StaticProperty, BoxedTextTokenKind.InstanceProperty);
			}
			if (r.IsEventDef) {
				var e = (EventDef)r;
				return GetTextTokenKind(e.AddMethod ?? e.RemoveMethod ?? e.InvokeMethod, BoxedTextTokenKind.StaticEvent, BoxedTextTokenKind.InstanceEvent);
			}

			var td = r as TypeDef;
			if (td != null)
				return GetTextTokenKind(td);

			var tr = r as TypeRef;
			if (tr != null)
				return GetTextTokenKind(tr);

			var ts = r as TypeSpec;
			if (ts != null) {
				var gsig = ts.TypeSig as GenericSig;
				if (gsig != null)
					return GetTextTokenKind(gsig);
				return BoxedTextTokenKind.Type;
			}

			var gp = r as GenericParam;
			if (gp != null)
				return GetTextTokenKind(gp);

			// It can be a MemberRef if it doesn't have a field or method sig (invalid metadata)
			if (r.IsMemberRef)
				return BoxedTextTokenKind.Text;

			return BoxedTextTokenKind.Text;
		}

		public static object GetTextTokenKind(GenericSig sig) {
			if (sig == null)
				return BoxedTextTokenKind.Text;

			return sig.IsMethodVar ? BoxedTextTokenKind.MethodGenericParameter : BoxedTextTokenKind.TypeGenericParameter;
		}

		public static object GetTextTokenKind(GenericParam gp) {
			if (gp == null)
				return BoxedTextTokenKind.Text;

			if (gp.DeclaringType != null)
				return BoxedTextTokenKind.TypeGenericParameter;

			if (gp.DeclaringMethod != null)
				return BoxedTextTokenKind.MethodGenericParameter;

			return BoxedTextTokenKind.TypeGenericParameter;
		}

		static object GetTextTokenKind(MethodDef method, object staticValue, object instanceValue) {
			if (method == null)
				return instanceValue;
			if (method.IsStatic)
				return staticValue;
			return instanceValue;
		}

		public static object GetTextTokenKind(ExportedType et) {
			if (et == null)
				return BoxedTextTokenKind.Text;

			return GetTextTokenKind(et.ToTypeRef());
		}

		public static object GetTextTokenKind(TypeSig ts) {
			ts = ts.RemovePinnedAndModifiers();
			if (ts == null)
				return BoxedTextTokenKind.Text;

			var tdr = ts as TypeDefOrRefSig;
			if (tdr != null)
				return GetTextTokenKind(tdr.TypeDefOrRef);

			var gsig = ts as GenericSig;
			if (gsig != null)
				return GetTextTokenKind(gsig);

			return BoxedTextTokenKind.Text;
		}

		public static object GetTextTokenKind(object op) {
			if (op == null)
				return BoxedTextTokenKind.Text;

			if (op is byte || op is sbyte ||
				op is ushort || op is short ||
				op is uint || op is int ||
				op is ulong || op is long ||
				op is UIntPtr || op is IntPtr)
				return BoxedTextTokenKind.Number;

			var r = op as IMemberRef;
			if (r != null)
				return GetTextTokenKind(r);

			var et = op as ExportedType;
			if (et != null)
				return GetTextTokenKind(et);

			var ts = op as TypeSig;
			if (ts != null)
				return GetTextTokenKind(ts);

			var gp = op as GenericParam;
			if (gp != null)
				return GetTextTokenKind(gp);

			if (op is TextTokenKind)
				return op;

			if (op is Parameter)
				return BoxedTextTokenKind.Parameter;

			if (op is dnlib.DotNet.Emit.Local)
				return BoxedTextTokenKind.Local;

			if (op is MethodSig)
				return BoxedTextTokenKind.Text;//TODO:

			if (op is string)
				return BoxedTextTokenKind.String;

			Debug.Assert(op.GetType().ToString() != "ICSharpCode.Decompiler.ILAst.ILVariable", "Fix caller, there should be no special type checks here");
			if (op.GetType().ToString() == "ICSharpCode.Decompiler.ILAst.ILVariable")
				return BoxedTextTokenKind.Local;

			return BoxedTextTokenKind.Text;
		}

		public static object GetTextTokenKind(Type type) {
			if (type == null)
				return BoxedTextTokenKind.Text;

			if (type.IsInterface)
				return BoxedTextTokenKind.Interface;
			if (type.IsEnum)
				return BoxedTextTokenKind.Enum;
			if (type.IsValueType)
				return BoxedTextTokenKind.ValueType;

			if (type.BaseType == typeof(MulticastDelegate))
				return BoxedTextTokenKind.Delegate;

			if (type.IsSealed)
				return BoxedTextTokenKind.SealedType;
			return BoxedTextTokenKind.Type;
		}
	}
}
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
