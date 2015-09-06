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

using System.Diagnostics;
using dndbg.Engine;
using dnSpy.NRefactory;
using ICSharpCode.Decompiler;

namespace dnSpy.Debugger {
	sealed class OutputConverter : ITypeOutput {
		readonly ITextOutput output;

		public OutputConverter(ITextOutput output) {
			this.output = output;
		}

		public void Write(string s, TypeColor type) {
			output.Write(s, Convert(type));
		}

		static TextTokenType Convert(TypeColor color) {
			switch (color) {
			case TypeColor.Operator:			return TextTokenType.Operator;
			case TypeColor.NativeFrame:			return TextTokenType.Text;
			case TypeColor.InternalFrame:		return TextTokenType.Text;
			case TypeColor.UnknownFrame:		return TextTokenType.Text;
			case TypeColor.Number:				return TextTokenType.Number;
			case TypeColor.Error:				return TextTokenType.Error;
			case TypeColor.Module:				return TextTokenType.Module;
			case TypeColor.Text:				return TextTokenType.Text;
			case TypeColor.Token:				return TextTokenType.Number;
			case TypeColor.NamespacePart:		return TextTokenType.NamespacePart;
			case TypeColor.Type:				return TextTokenType.Type;
			case TypeColor.Comment:				return TextTokenType.Comment;
			case TypeColor.Method:				return TextTokenType.InstanceMethod;
			case TypeColor.TypeKeyword:			return TextTokenType.Keyword;
			case TypeColor.TypeGenericParameter:return TextTokenType.TypeGenericParameter;
			case TypeColor.MethodGenericParameter:return TextTokenType.MethodGenericParameter;
			case TypeColor.Keyword:				return TextTokenType.Keyword;
			case TypeColor.Parameter:			return TextTokenType.Parameter;
			case TypeColor.String:				return TextTokenType.String;
			case TypeColor.Char:				return TextTokenType.Char;
			default:
				Debug.Fail(string.Format("Unknown color: {0}", color));
				return TextTokenType.Text;
			}
		}
	}
}
