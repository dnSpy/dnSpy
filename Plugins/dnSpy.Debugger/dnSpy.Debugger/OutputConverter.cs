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
using dndbg.Engine;
using dnSpy.Contracts.Highlighting;
using dnSpy.Decompiler.Shared;

namespace dnSpy.Debugger {
	sealed class OutputConverter : ITypeOutput {
		readonly ISyntaxHighlightOutput output;

		public OutputConverter(ISyntaxHighlightOutput output) {
			this.output = output;
		}

		public void Write(string s, TypeColor type) => output.Write(s, Convert(type));

		public static object Convert(TypeColor color) {
			switch (color) {
			case TypeColor.Unknown:				return BoxedTextTokenKind.Text;
			case TypeColor.Space:				return BoxedTextTokenKind.Text;
			case TypeColor.IPType:				return BoxedTextTokenKind.Text;
			case TypeColor.Operator:			return BoxedTextTokenKind.Operator;
			case TypeColor.Punctuation:			return BoxedTextTokenKind.Punctuation;
			case TypeColor.NativeFrame:			return BoxedTextTokenKind.Text;
			case TypeColor.InternalFrame:		return BoxedTextTokenKind.Text;
			case TypeColor.UnknownFrame:		return BoxedTextTokenKind.Text;
			case TypeColor.Number:				return BoxedTextTokenKind.Number;
			case TypeColor.Error:				return BoxedTextTokenKind.Error;
			case TypeColor.Module:				return BoxedTextTokenKind.Module;
			case TypeColor.Token:				return BoxedTextTokenKind.Number;
			case TypeColor.Namespace:			return BoxedTextTokenKind.Namespace;
			case TypeColor.InstanceProperty:	return BoxedTextTokenKind.InstanceProperty;
			case TypeColor.StaticProperty:		return BoxedTextTokenKind.StaticProperty;
			case TypeColor.InstanceEvent:		return BoxedTextTokenKind.InstanceEvent;
			case TypeColor.StaticEvent:			return BoxedTextTokenKind.StaticEvent;
			case TypeColor.Type:				return BoxedTextTokenKind.Type;
			case TypeColor.SealedType:			return BoxedTextTokenKind.SealedType;
			case TypeColor.StaticType:			return BoxedTextTokenKind.StaticType;
			case TypeColor.Delegate:			return BoxedTextTokenKind.Delegate;
			case TypeColor.Enum:				return BoxedTextTokenKind.Enum;
			case TypeColor.Interface:			return BoxedTextTokenKind.Interface;
			case TypeColor.ValueType:			return BoxedTextTokenKind.ValueType;
			case TypeColor.Comment:				return BoxedTextTokenKind.Comment;
			case TypeColor.StaticMethod:		return BoxedTextTokenKind.StaticMethod;
			case TypeColor.ExtensionMethod:		return BoxedTextTokenKind.ExtensionMethod;
			case TypeColor.InstanceMethod:		return BoxedTextTokenKind.InstanceMethod;
			case TypeColor.TypeKeyword:			return BoxedTextTokenKind.Keyword;
			case TypeColor.TypeGenericParameter:return BoxedTextTokenKind.TypeGenericParameter;
			case TypeColor.MethodGenericParameter:return BoxedTextTokenKind.MethodGenericParameter;
			case TypeColor.Keyword:				return BoxedTextTokenKind.Keyword;
			case TypeColor.Parameter:			return BoxedTextTokenKind.Parameter;
			case TypeColor.String:				return BoxedTextTokenKind.String;
			case TypeColor.Char:				return BoxedTextTokenKind.Char;
			case TypeColor.InstanceField:		return BoxedTextTokenKind.InstanceField;
			case TypeColor.EnumField:			return BoxedTextTokenKind.EnumField;
			case TypeColor.LiteralField:		return BoxedTextTokenKind.LiteralField;
			case TypeColor.StaticField:			return BoxedTextTokenKind.StaticField;
			case TypeColor.TypeStringBrace:		return BoxedTextTokenKind.Error;
			case TypeColor.ToStringBrace:		return BoxedTextTokenKind.ToStringEval;
			case TypeColor.ToStringResult:		return BoxedTextTokenKind.ToStringEval;
			default:
				Debug.Fail(string.Format("Unknown color: {0}", color));
				return BoxedTextTokenKind.Text;
			}
		}
	}
}
