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
using dnSpy.Contracts.Text;

namespace dnSpy.Debugger {
	sealed class OutputConverter : ITypeOutput {
		readonly ITextColorWriter output;

		public OutputConverter(ITextColorWriter output) {
			this.output = output;
		}

		public void Write(string s, TypeColor type) => output.Write(Convert(type), s);

		public static object Convert(TypeColor color) {
			switch (color) {
			case TypeColor.Unknown:				return BoxedTextColor.Text;
			case TypeColor.Space:				return BoxedTextColor.Text;
			case TypeColor.IPType:				return BoxedTextColor.Text;
			case TypeColor.Operator:			return BoxedTextColor.Operator;
			case TypeColor.Punctuation:			return BoxedTextColor.Punctuation;
			case TypeColor.NativeFrame:			return BoxedTextColor.Text;
			case TypeColor.InternalFrame:		return BoxedTextColor.Text;
			case TypeColor.UnknownFrame:		return BoxedTextColor.Text;
			case TypeColor.Number:				return BoxedTextColor.Number;
			case TypeColor.Error:				return BoxedTextColor.Error;
			case TypeColor.AssemblyModule:		return BoxedTextColor.AssemblyModule;
			case TypeColor.Token:				return BoxedTextColor.Number;
			case TypeColor.Namespace:			return BoxedTextColor.Namespace;
			case TypeColor.InstanceProperty:	return BoxedTextColor.InstanceProperty;
			case TypeColor.StaticProperty:		return BoxedTextColor.StaticProperty;
			case TypeColor.InstanceEvent:		return BoxedTextColor.InstanceEvent;
			case TypeColor.StaticEvent:			return BoxedTextColor.StaticEvent;
			case TypeColor.Type:				return BoxedTextColor.Type;
			case TypeColor.SealedType:			return BoxedTextColor.SealedType;
			case TypeColor.StaticType:			return BoxedTextColor.StaticType;
			case TypeColor.Delegate:			return BoxedTextColor.Delegate;
			case TypeColor.Enum:				return BoxedTextColor.Enum;
			case TypeColor.Interface:			return BoxedTextColor.Interface;
			case TypeColor.ValueType:			return BoxedTextColor.ValueType;
			case TypeColor.Comment:				return BoxedTextColor.Comment;
			case TypeColor.StaticMethod:		return BoxedTextColor.StaticMethod;
			case TypeColor.ExtensionMethod:		return BoxedTextColor.ExtensionMethod;
			case TypeColor.InstanceMethod:		return BoxedTextColor.InstanceMethod;
			case TypeColor.TypeKeyword:			return BoxedTextColor.Keyword;
			case TypeColor.TypeGenericParameter:return BoxedTextColor.TypeGenericParameter;
			case TypeColor.MethodGenericParameter:return BoxedTextColor.MethodGenericParameter;
			case TypeColor.Keyword:				return BoxedTextColor.Keyword;
			case TypeColor.Parameter:			return BoxedTextColor.Parameter;
			case TypeColor.String:				return BoxedTextColor.String;
			case TypeColor.Char:				return BoxedTextColor.Char;
			case TypeColor.InstanceField:		return BoxedTextColor.InstanceField;
			case TypeColor.EnumField:			return BoxedTextColor.EnumField;
			case TypeColor.LiteralField:		return BoxedTextColor.LiteralField;
			case TypeColor.StaticField:			return BoxedTextColor.StaticField;
			case TypeColor.TypeStringBrace:		return BoxedTextColor.Error;
			case TypeColor.ToStringBrace:		return BoxedTextColor.ToStringEval;
			case TypeColor.ToStringResult:		return BoxedTextColor.ToStringEval;
			default:
				Debug.Fail(string.Format("Unknown color: {0}", color));
				return BoxedTextColor.Text;
			}
		}
	}
}
