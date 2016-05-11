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
using dnSpy.Contracts.TextEditor;

namespace dnSpy.Debugger {
	sealed class OutputConverter : ITypeOutput {
		readonly IOutputColorWriter output;

		public OutputConverter(IOutputColorWriter output) {
			this.output = output;
		}

		public void Write(string s, TypeColor type) => output.Write(Convert(type), s);

		public static object Convert(TypeColor color) {
			switch (color) {
			case TypeColor.Unknown:				return BoxedOutputColor.Text;
			case TypeColor.Space:				return BoxedOutputColor.Text;
			case TypeColor.IPType:				return BoxedOutputColor.Text;
			case TypeColor.Operator:			return BoxedOutputColor.Operator;
			case TypeColor.Punctuation:			return BoxedOutputColor.Punctuation;
			case TypeColor.NativeFrame:			return BoxedOutputColor.Text;
			case TypeColor.InternalFrame:		return BoxedOutputColor.Text;
			case TypeColor.UnknownFrame:		return BoxedOutputColor.Text;
			case TypeColor.Number:				return BoxedOutputColor.Number;
			case TypeColor.Error:				return BoxedOutputColor.Error;
			case TypeColor.Module:				return BoxedOutputColor.Module;
			case TypeColor.Token:				return BoxedOutputColor.Number;
			case TypeColor.Namespace:			return BoxedOutputColor.Namespace;
			case TypeColor.InstanceProperty:	return BoxedOutputColor.InstanceProperty;
			case TypeColor.StaticProperty:		return BoxedOutputColor.StaticProperty;
			case TypeColor.InstanceEvent:		return BoxedOutputColor.InstanceEvent;
			case TypeColor.StaticEvent:			return BoxedOutputColor.StaticEvent;
			case TypeColor.Type:				return BoxedOutputColor.Type;
			case TypeColor.SealedType:			return BoxedOutputColor.SealedType;
			case TypeColor.StaticType:			return BoxedOutputColor.StaticType;
			case TypeColor.Delegate:			return BoxedOutputColor.Delegate;
			case TypeColor.Enum:				return BoxedOutputColor.Enum;
			case TypeColor.Interface:			return BoxedOutputColor.Interface;
			case TypeColor.ValueType:			return BoxedOutputColor.ValueType;
			case TypeColor.Comment:				return BoxedOutputColor.Comment;
			case TypeColor.StaticMethod:		return BoxedOutputColor.StaticMethod;
			case TypeColor.ExtensionMethod:		return BoxedOutputColor.ExtensionMethod;
			case TypeColor.InstanceMethod:		return BoxedOutputColor.InstanceMethod;
			case TypeColor.TypeKeyword:			return BoxedOutputColor.Keyword;
			case TypeColor.TypeGenericParameter:return BoxedOutputColor.TypeGenericParameter;
			case TypeColor.MethodGenericParameter:return BoxedOutputColor.MethodGenericParameter;
			case TypeColor.Keyword:				return BoxedOutputColor.Keyword;
			case TypeColor.Parameter:			return BoxedOutputColor.Parameter;
			case TypeColor.String:				return BoxedOutputColor.String;
			case TypeColor.Char:				return BoxedOutputColor.Char;
			case TypeColor.InstanceField:		return BoxedOutputColor.InstanceField;
			case TypeColor.EnumField:			return BoxedOutputColor.EnumField;
			case TypeColor.LiteralField:		return BoxedOutputColor.LiteralField;
			case TypeColor.StaticField:			return BoxedOutputColor.StaticField;
			case TypeColor.TypeStringBrace:		return BoxedOutputColor.Error;
			case TypeColor.ToStringBrace:		return BoxedOutputColor.ToStringEval;
			case TypeColor.ToStringResult:		return BoxedOutputColor.ToStringEval;
			default:
				Debug.Fail(string.Format("Unknown color: {0}", color));
				return BoxedOutputColor.Text;
			}
		}
	}
}
