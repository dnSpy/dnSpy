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

namespace dnSpy.Decompiler.Shared {
	public enum TextTokenKind : byte {
		/// <summary>
		/// default text (in text editor)
		/// </summary>
		Text,

		/// <summary>
		/// {}
		/// </summary>
		Brace,

		/// <summary>
		/// +-/etc and other special chars like ,; etc
		/// </summary>
		Operator,

		/// <summary>
		/// numbers
		/// </summary>
		Number,

		/// <summary>
		/// code comments
		/// </summary>
		Comment,

		/// <summary>
		/// XML tag in XML doc comment. This includes the "///" (C#) or "'''" (VB) part
		/// </summary>
		XmlDocTag,

		/// <summary>
		/// XML attribute in an XML doc tag
		/// </summary>
		XmlDocAttribute,

		/// <summary>
		/// XML doc comments. Whatever is not an XML doc tag / attribute
		/// </summary>
		XmlDocComment,

		/// <summary>
		/// keywords
		/// </summary>
		Keyword,

		/// <summary>
		/// "strings"
		/// </summary>
		String,

		/// <summary>
		/// chars ('a', 'b')
		/// </summary>
		Char,

		/// <summary>
		/// Any part of a namespace
		/// </summary>
		NamespacePart,

		/// <summary>
		/// classes (not keyword-classes eg "int")
		/// </summary>
		Type,

		/// <summary>
		/// static types
		/// </summary>
		StaticType,

		/// <summary>
		/// delegates
		/// </summary>
		Delegate,

		/// <summary>
		/// enums
		/// </summary>
		Enum,

		/// <summary>
		/// interfaces
		/// </summary>
		Interface,

		/// <summary>
		/// value types
		/// </summary>
		ValueType,

		/// <summary>
		/// type generic parameters
		/// </summary>
		TypeGenericParameter,

		/// <summary>
		/// method generic parameters
		/// </summary>
		MethodGenericParameter,

		/// <summary>
		/// instance methods
		/// </summary>
		InstanceMethod,

		/// <summary>
		/// static methods
		/// </summary>
		StaticMethod,

		/// <summary>
		/// extension methods
		/// </summary>
		ExtensionMethod,

		/// <summary>
		/// instance fields
		/// </summary>
		InstanceField,

		/// <summary>
		/// enum fields
		/// </summary>
		EnumField,

		/// <summary>
		/// constant fields (not enum fields)
		/// </summary>
		LiteralField,

		/// <summary>
		/// static fields
		/// </summary>
		StaticField,

		/// <summary>
		/// instance events
		/// </summary>
		InstanceEvent,

		/// <summary>
		/// static events
		/// </summary>
		StaticEvent,

		/// <summary>
		/// instance properties
		/// </summary>
		InstanceProperty,

		/// <summary>
		/// static properties
		/// </summary>
		StaticProperty,

		/// <summary>
		/// method locals
		/// </summary>
		Local,

		/// <summary>
		/// method parameters
		/// </summary>
		Parameter,

		/// <summary>
		/// labels
		/// </summary>
		Label,

		/// <summary>
		/// opcodes
		/// </summary>
		OpCode,

		/// <summary>
		/// IL directive (.sometext)
		/// </summary>
		ILDirective,

		/// <summary>
		/// IL module names, eg. [module]SomeClass
		/// </summary>
		ILModule,

		/// <summary>
		/// ":" string
		/// </summary>
		XmlDocToolTipColon,

		/// <summary>
		/// "Example" string
		/// </summary>
		XmlDocToolTipExample,

		/// <summary>
		/// cref attribute in an exception tag
		/// </summary>
		XmlDocToolTipExceptionCref,

		/// <summary>
		/// "Returns" string
		/// </summary>
		XmlDocToolTipReturns,

		/// <summary>
		/// cref attribute in a see tag
		/// </summary>
		XmlDocToolTipSeeCref,

		/// <summary>
		/// langword attribute in a see tag
		/// </summary>
		XmlDocToolTipSeeLangword,

		/// <summary>
		/// "See also" string
		/// </summary>
		XmlDocToolTipSeeAlso,

		/// <summary>
		/// cref attribute in a seealso tag
		/// </summary>
		XmlDocToolTipSeeAlsoCref,

		/// <summary>
		/// name attribute in a paramref tag
		/// </summary>
		XmlDocToolTipParamRefName,

		/// <summary>
		/// name attribute in a param tag
		/// </summary>
		XmlDocToolTipParamName,

		/// <summary>
		/// name attribute in a typeparam tag
		/// </summary>
		XmlDocToolTipTypeParamName,

		/// <summary>
		/// "Value" string
		/// </summary>
		XmlDocToolTipValue,

		/// <summary>
		/// Summary text
		/// </summary>
		XmlDocSummary,

		/// <summary>
		/// Any other XML doc text
		/// </summary>
		XmlDocToolTipText,

		/// <summary>
		/// Assembly
		/// </summary>
		Assembly,

		/// <summary>
		/// Assembly (executable)
		/// </summary>
		AssemblyExe,

		/// <summary>
		/// Module
		/// </summary>
		Module,

		/// <summary>
		/// Part of a directory
		/// </summary>
		DirectoryPart,

		/// <summary>
		/// Filename without extension
		/// </summary>
		FileNameNoExtension,

		/// <summary>
		/// File extension
		/// </summary>
		FileExtension,

		/// <summary>
		/// Error text
		/// </summary>
		Error,

		/// <summary>
		/// ToString() eval text
		/// </summary>
		ToStringEval,

		// If you add a new one, also update ColorType

		/// <summary>
		/// Must be last
		/// </summary>
		Last,
	}

	public static class TextTokenKindUtils {
		public static TextTokenKind GetTextTokenType(TypeDef td) {
			if (td == null)
				return TextTokenKind.Text;

			if (td.IsInterface)
				return TextTokenKind.Interface;
			if (td.IsEnum)
				return TextTokenKind.Enum;
			if (td.IsValueType)
				return TextTokenKind.ValueType;

			if (td.IsDelegate)
				return TextTokenKind.Delegate;

			if (td.IsSealed && td.IsAbstract) {
				var bt = td.BaseType;
				if (bt != null && bt.DefinitionAssembly.IsCorLib()) {
					var baseTr = bt as TypeRef;
					if (baseTr != null) {
						if (baseTr.Namespace == systemString && baseTr.Name == objectString)
							return TextTokenKind.StaticType;
					}
					else {
						var baseTd = bt as TypeDef;
						if (baseTd.Namespace == systemString && baseTd.Name == objectString)
							return TextTokenKind.StaticType;
					}
				}
			}

			return TextTokenKind.Type;
		}
		static readonly UTF8String systemString = new UTF8String("System");
		static readonly UTF8String objectString = new UTF8String("Object");

		public static TextTokenKind GetTextTokenType(TypeRef tr) {
			if (tr == null)
				return TextTokenKind.Text;

			var td = tr.Resolve();
			if (td != null)
				return GetTextTokenType(td);

			return TextTokenKind.Type;
		}

		static readonly UTF8String systemRuntimeCompilerServicesString = new UTF8String("System.Runtime.CompilerServices");
		static readonly UTF8String extensionAttributeString = new UTF8String("ExtensionAttribute");
		public static TextTokenKind GetTextTokenType(IMemberRef r) {
			if (r == null)
				return TextTokenKind.Text;

			if (r.IsField) {
				var fd = ((IField)r).ResolveFieldDef();
				if (fd == null)
					return TextTokenKind.InstanceField;
				if (fd.DeclaringType.IsEnum)
					return TextTokenKind.EnumField;
				if (fd.IsLiteral)
					return TextTokenKind.LiteralField;
				if (fd.IsStatic)
					return TextTokenKind.StaticField;
				return TextTokenKind.InstanceField;
			}
			if (r.IsMethod) {
				var mr = (IMethod)r;
				if (mr.MethodSig == null)
					return TextTokenKind.InstanceMethod;
				var md = mr.ResolveMethodDef();
				if (md != null && md.IsConstructor)
					return GetTextTokenType(md.DeclaringType);
				if (!mr.MethodSig.HasThis) {
					if (md != null && md.IsDefined(systemRuntimeCompilerServicesString, extensionAttributeString))
						return TextTokenKind.ExtensionMethod;
					return TextTokenKind.StaticMethod;
				}
				return TextTokenKind.InstanceMethod;
			}
			if (r.IsPropertyDef) {
				var p = (PropertyDef)r;
				return GetTextTokenType(p.GetMethod ?? p.SetMethod, TextTokenKind.StaticProperty, TextTokenKind.InstanceProperty);
			}
			if (r.IsEventDef) {
				var e = (EventDef)r;
				return GetTextTokenType(e.AddMethod ?? e.RemoveMethod ?? e.InvokeMethod, TextTokenKind.StaticEvent, TextTokenKind.InstanceEvent);
			}

			var td = r as TypeDef;
			if (td != null)
				return GetTextTokenType(td);

			var tr = r as TypeRef;
			if (tr != null)
				return GetTextTokenType(tr);

			var ts = r as TypeSpec;
			if (ts != null) {
				var gsig = ts.TypeSig as GenericSig;
				if (gsig != null)
					return GetTextTokenType(gsig);
				return TextTokenKind.Type;
			}

			var gp = r as GenericParam;
			if (gp != null)
				return GetTextTokenType(gp);

			// It can be a MemberRef if it doesn't have a field or method sig (invalid metadata)
			if (r.IsMemberRef)
				return TextTokenKind.Text;

			return TextTokenKind.Text;
		}

		public static TextTokenKind GetTextTokenType(GenericSig sig) {
			if (sig == null)
				return TextTokenKind.Text;

			return sig.IsMethodVar ? TextTokenKind.MethodGenericParameter : TextTokenKind.TypeGenericParameter;
		}

		public static TextTokenKind GetTextTokenType(GenericParam gp) {
			if (gp == null)
				return TextTokenKind.Text;

			if (gp.DeclaringType != null)
				return TextTokenKind.TypeGenericParameter;

			if (gp.DeclaringMethod != null)
				return TextTokenKind.MethodGenericParameter;

			return TextTokenKind.TypeGenericParameter;
		}

		static TextTokenKind GetTextTokenType(MethodDef method, TextTokenKind staticValue, TextTokenKind instanceValue) {
			if (method == null)
				return instanceValue;
			if (method.IsStatic)
				return staticValue;
			return instanceValue;
		}

		public static TextTokenKind GetTextTokenType(ExportedType et) {
			if (et == null)
				return TextTokenKind.Text;

			return GetTextTokenType(et.ToTypeRef());
		}

		public static TextTokenKind GetTextTokenType(TypeSig ts) {
			ts = ts.RemovePinnedAndModifiers();
			if (ts == null)
				return TextTokenKind.Text;

			var tdr = ts as TypeDefOrRefSig;
			if (tdr != null)
				return GetTextTokenType(tdr.TypeDefOrRef);

			var gsig = ts as GenericSig;
			if (gsig != null)
				return GetTextTokenType(gsig);

			return TextTokenKind.Text;
		}

		public static TextTokenKind GetTextTokenType(object op) {
			if (op == null)
				return TextTokenKind.Text;

			if (op is byte || op is sbyte ||
				op is ushort || op is short ||
				op is uint || op is int ||
				op is ulong || op is long ||
				op is UIntPtr || op is IntPtr)
				return TextTokenKind.Number;

			var r = op as IMemberRef;
			if (r != null)
				return GetTextTokenType(r);

			var et = op as ExportedType;
			if (et != null)
				return GetTextTokenType(et);

			var ts = op as TypeSig;
			if (ts != null)
				return GetTextTokenType(ts);

			var gp = op as GenericParam;
			if (gp != null)
				return GetTextTokenType(gp);

			if (op is TextTokenKind)
				return (TextTokenKind)op;

			if (op is Parameter)
				return TextTokenKind.Parameter;

			if (op is dnlib.DotNet.Emit.Local)
				return TextTokenKind.Local;

			if (op is MethodSig)
				return TextTokenKind.Text;//TODO:

			if (op is string)
				return TextTokenKind.String;

			Debug.Assert(op.GetType().ToString() != "ICSharpCode.Decompiler.ILAst.ILVariable", "Fix caller, there should be no special type checks here");
			if (op.GetType().ToString() == "ICSharpCode.Decompiler.ILAst.ILVariable")
				return TextTokenKind.Local;

			return TextTokenKind.Text;
		}

		public static TextTokenKind GetTextTokenTypeFromLangToken(this string text) {
			if (string.IsNullOrEmpty(text))
				return TextTokenKind.Text;
			if (char.IsLetter(text[0]))
				return TextTokenKind.Keyword;
			if (text == "{" || text == "}")
				return TextTokenKind.Brace;
			return TextTokenKind.Operator;
		}

		public static TextTokenKind GetTextTokenType(Type type) {
			if (type == null)
				return TextTokenKind.Text;

			if (type.IsInterface)
				return TextTokenKind.Interface;
			if (type.IsEnum)
				return TextTokenKind.Enum;
			if (type.IsValueType)
				return TextTokenKind.ValueType;

			if (type.BaseType == typeof(MulticastDelegate))
				return TextTokenKind.Delegate;

			return TextTokenKind.Type;
		}
	}
}
