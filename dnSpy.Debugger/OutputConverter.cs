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

		public void Write(string s, TypeColor type) {
			output.Write(s, Convert(type));
		}

		static TextTokenKind Convert(TypeColor color) {
			switch (color) {
			case TypeColor.Unknown:				return TextTokenKind.Text;
			case TypeColor.Space:				return TextTokenKind.Text;
			case TypeColor.IPType:				return TextTokenKind.Text;
			case TypeColor.Operator:			return TextTokenKind.Operator;
			case TypeColor.NativeFrame:			return TextTokenKind.Text;
			case TypeColor.InternalFrame:		return TextTokenKind.Text;
			case TypeColor.UnknownFrame:		return TextTokenKind.Text;
			case TypeColor.Number:				return TextTokenKind.Number;
			case TypeColor.Error:				return TextTokenKind.Error;
			case TypeColor.Module:				return TextTokenKind.Module;
			case TypeColor.Token:				return TextTokenKind.Number;
			case TypeColor.NamespacePart:		return TextTokenKind.NamespacePart;
			case TypeColor.InstanceProperty:	return TextTokenKind.InstanceProperty;
			case TypeColor.StaticProperty:		return TextTokenKind.StaticProperty;
			case TypeColor.InstanceEvent:		return TextTokenKind.InstanceEvent;
			case TypeColor.StaticEvent:			return TextTokenKind.StaticEvent;
			case TypeColor.Type:				return TextTokenKind.Type;
			case TypeColor.StaticType:			return TextTokenKind.StaticType;
			case TypeColor.Delegate:			return TextTokenKind.Delegate;
			case TypeColor.Enum:				return TextTokenKind.Enum;
			case TypeColor.Interface:			return TextTokenKind.Interface;
			case TypeColor.ValueType:			return TextTokenKind.ValueType;
			case TypeColor.Comment:				return TextTokenKind.Comment;
			case TypeColor.StaticMethod:		return TextTokenKind.StaticMethod;
			case TypeColor.ExtensionMethod:		return TextTokenKind.ExtensionMethod;
			case TypeColor.InstanceMethod:		return TextTokenKind.InstanceMethod;
			case TypeColor.TypeKeyword:			return TextTokenKind.Keyword;
			case TypeColor.TypeGenericParameter:return TextTokenKind.TypeGenericParameter;
			case TypeColor.MethodGenericParameter:return TextTokenKind.MethodGenericParameter;
			case TypeColor.Keyword:				return TextTokenKind.Keyword;
			case TypeColor.Parameter:			return TextTokenKind.Parameter;
			case TypeColor.String:				return TextTokenKind.String;
			case TypeColor.Char:				return TextTokenKind.Char;
			case TypeColor.InstanceField:		return TextTokenKind.InstanceField;
			case TypeColor.EnumField:			return TextTokenKind.EnumField;
			case TypeColor.LiteralField:		return TextTokenKind.LiteralField;
			case TypeColor.StaticField:			return TextTokenKind.StaticField;
			case TypeColor.TypeStringBrace:		return TextTokenKind.Error;
			case TypeColor.ToStringBrace:		return TextTokenKind.ToStringEval;
			case TypeColor.ToStringResult:		return TextTokenKind.ToStringEval;
			default:
				Debug.Fail(string.Format("Unknown color: {0}", color));
				return TextTokenKind.Text;
			}
		}
	}
}
