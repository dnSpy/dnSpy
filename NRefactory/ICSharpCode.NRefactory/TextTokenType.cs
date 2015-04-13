/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using dnlib.DotNet;

namespace ICSharpCode.NRefactory
{
	public enum TextTokenType : byte
	{
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

		// If you add a new one, also update DnColorType

		/// <summary>
		/// Must be last
		/// </summary>
		Last,
	}

	[CLSCompliant(false)]
	public static class TextTokenHelper
	{
		public static TextTokenType GetTextTokenType(TypeDef td)
		{
			if (td == null)
				return TextTokenType.Text;

			if (td.IsInterface)
				return TextTokenType.Interface;
			if (td.IsEnum)
				return TextTokenType.Enum;
			if (td.IsValueType)
				return TextTokenType.ValueType;

			var bt = td.BaseType;
			if (bt != null && bt.DefinitionAssembly.IsCorLib() && bt.FullName == "System.MulticastDelegate")
				return TextTokenType.Delegate;

			if (td.IsSealed && td.IsAbstract && td.BaseType != null && td.BaseType.FullName == "System.Object")
				return TextTokenType.StaticType;

			return TextTokenType.Type;
		}

		public static TextTokenType GetTextTokenType(TypeRef tr)
		{
			if (tr == null)
				return TextTokenType.Text;

			var td = tr.Resolve();
			if (td != null)
				return GetTextTokenType(td);

			return TextTokenType.Type;
		}

		public static TextTokenType GetTextTokenType(IMemberRef r)
		{
			if (r == null)
				return TextTokenType.Text;

			if (r.IsField) {
				var fd = ((IField)r).ResolveFieldDef();
				if (fd == null)
					return TextTokenType.InstanceField;
				if (fd.DeclaringType.IsEnum)
					return TextTokenType.EnumField;
				if (fd.IsLiteral)
					return TextTokenType.LiteralField;
				if (fd.IsStatic)
					return TextTokenType.StaticField;
				return TextTokenType.InstanceField;
			}
			if (r.IsMethod) {
				var mr = (IMethod)r;
				if (mr.MethodSig == null)
					return TextTokenType.InstanceMethod;
				if (!mr.MethodSig.HasThis) {
					var md = mr.ResolveMethodDef();
					if (md != null && md.CustomAttributes.Find("System.Runtime.CompilerServices.ExtensionAttribute") != null)
						return TextTokenType.ExtensionMethod;
					return TextTokenType.StaticMethod;
				}
				return TextTokenType.InstanceMethod;
			}
			if (r.IsPropertyDef) {
				var p = (PropertyDef)r;
				return GetTextTokenType(p.GetMethod ?? p.SetMethod, TextTokenType.StaticProperty, TextTokenType.InstanceProperty);
			}
			if (r.IsEventDef) {
				var e = (EventDef)r;
				return GetTextTokenType(e.AddMethod ?? e.RemoveMethod ?? e.InvokeMethod, TextTokenType.StaticEvent, TextTokenType.InstanceEvent);
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
				return TextTokenType.Type;
			}

			var gp = r as GenericParam;
			if (gp != null)
				return GetTextTokenType(gp);

			// It can be a MemberRef if it doesn't have a field or method sig (invalid metadata)
			if (r.IsMemberRef)
				return TextTokenType.Text;

			return TextTokenType.Text;
		}

		public static TextTokenType GetTextTokenType(GenericSig sig)
		{
			if (sig == null)
				return TextTokenType.Text;

			return sig.IsMethodVar ? TextTokenType.MethodGenericParameter : TextTokenType.TypeGenericParameter;
		}

		public static TextTokenType GetTextTokenType(GenericParam gp)
		{
			if (gp == null)
				return TextTokenType.Text;

			if (gp.DeclaringType != null)
				return TextTokenType.TypeGenericParameter;

			if (gp.DeclaringMethod != null)
				return TextTokenType.MethodGenericParameter;

			return TextTokenType.TypeGenericParameter;
		}

		static TextTokenType GetTextTokenType(MethodDef method, TextTokenType staticValue, TextTokenType instanceValue)
		{
			if (method == null)
				return instanceValue;
			if (method.IsStatic)
				return staticValue;
			return instanceValue;
		}

		public static TextTokenType GetTextTokenType(ExportedType et)
		{
			if (et == null)
				return TextTokenType.Text;

			return GetTextTokenType(et.ToTypeRef());
		}

		public static TextTokenType GetTextTokenType(TypeSig ts)
		{
			ts = ts.RemovePinnedAndModifiers();
			if (ts == null)
				return TextTokenType.Text;

			var tdr = ts as TypeDefOrRefSig;
			if (tdr != null)
				return GetTextTokenType(tdr.TypeDefOrRef);

			var gsig = ts as GenericSig;
			if (gsig != null)
				return GetTextTokenType(gsig);

			return TextTokenType.Text;
		}

		public static TextTokenType GetTextTokenType(object op)
		{
			if (op == null)
				return TextTokenType.Text;

			if (op is byte || op is sbyte ||
				op is ushort || op is short ||
				op is uint || op is int ||
				op is ulong || op is long ||
				op is UIntPtr || op is IntPtr)
				return TextTokenType.Number;

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

			if (op is TextTokenType)
				return (TextTokenType)op;

			if (op is Parameter)
				return TextTokenType.Parameter;

			if (op is dnlib.DotNet.Emit.Local)
				return TextTokenType.Local;

			if (op is MethodSig)
				return TextTokenType.Text;//TODO:

			if (op.GetType().ToString() == "ICSharpCode.Decompiler.ILAst.ILVariable")
				return TextTokenType.Local;

			if (op is string)
				return TextTokenType.String;

			return TextTokenType.Text;
		}

		public static TextTokenType GetTextTokenTypeFromLangToken(this string text)
		{
			if (string.IsNullOrEmpty(text))
				return TextTokenType.Text;
			if (char.IsLetter(text[0]))
				return TextTokenType.Keyword;
			if (text == "{" || text == "}")
				return TextTokenType.Brace;
			return TextTokenType.Operator;
		}

		public static TextTokenType GetTextTokenType(Type type)
		{
			if (type == null)
				return TextTokenType.Text;

			if (type.IsInterface)
				return TextTokenType.Interface;
			if (type.IsEnum)
				return TextTokenType.Enum;
			if (type.IsValueType)
				return TextTokenType.ValueType;

			var bt = type.BaseType;
			if (bt != null && bt.Assembly == typeof(object).Assembly && bt.FullName == "System.MulticastDelegate")
				return TextTokenType.Delegate;

			return TextTokenType.Type;
		}
	}
}
