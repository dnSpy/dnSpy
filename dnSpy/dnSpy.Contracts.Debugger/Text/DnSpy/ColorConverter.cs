/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using System.Collections.Generic;
using System.Diagnostics;
using dnSpy.Contracts.Text;

namespace dnSpy.Contracts.Debugger.Text.DnSpy {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
	public static class ColorConverter {
		static readonly Dictionary<object, DbgTextColor> toDebuggerColor;
		static readonly Dictionary<DbgTextColor, object> toDnSpyColor;

		static ColorConverter() {
			toDebuggerColor = new Dictionary<object, DbgTextColor>();
			toDnSpyColor = new Dictionary<DbgTextColor, object>();

			var colors = new short[] {
				(short)DbgTextColor.Text, (short)TextColor.Text,
				(short)DbgTextColor.Operator, (short)TextColor.Operator,
				(short)DbgTextColor.Punctuation, (short)TextColor.Punctuation,
				(short)DbgTextColor.Number, (short)TextColor.Number,
				(short)DbgTextColor.Comment, (short)TextColor.Comment,
				(short)DbgTextColor.Keyword, (short)TextColor.Keyword,
				(short)DbgTextColor.String, (short)TextColor.String,
				(short)DbgTextColor.VerbatimString, (short)TextColor.VerbatimString,
				(short)DbgTextColor.Char, (short)TextColor.Char,
				(short)DbgTextColor.Namespace, (short)TextColor.Namespace,
				(short)DbgTextColor.Type, (short)TextColor.Type,
				(short)DbgTextColor.SealedType, (short)TextColor.SealedType,
				(short)DbgTextColor.StaticType, (short)TextColor.StaticType,
				(short)DbgTextColor.Delegate, (short)TextColor.Delegate,
				(short)DbgTextColor.Enum, (short)TextColor.Enum,
				(short)DbgTextColor.Interface, (short)TextColor.Interface,
				(short)DbgTextColor.ValueType, (short)TextColor.ValueType,
				(short)DbgTextColor.Module, (short)TextColor.Module,
				(short)DbgTextColor.TypeGenericParameter, (short)TextColor.TypeGenericParameter,
				(short)DbgTextColor.MethodGenericParameter, (short)TextColor.MethodGenericParameter,
				(short)DbgTextColor.InstanceMethod, (short)TextColor.InstanceMethod,
				(short)DbgTextColor.StaticMethod, (short)TextColor.StaticMethod,
				(short)DbgTextColor.ExtensionMethod, (short)TextColor.ExtensionMethod,
				(short)DbgTextColor.InstanceField, (short)TextColor.InstanceField,
				(short)DbgTextColor.EnumField, (short)TextColor.EnumField,
				(short)DbgTextColor.LiteralField, (short)TextColor.LiteralField,
				(short)DbgTextColor.StaticField, (short)TextColor.StaticField,
				(short)DbgTextColor.InstanceEvent, (short)TextColor.InstanceEvent,
				(short)DbgTextColor.StaticEvent, (short)TextColor.StaticEvent,
				(short)DbgTextColor.InstanceProperty, (short)TextColor.InstanceProperty,
				(short)DbgTextColor.StaticProperty, (short)TextColor.StaticProperty,
				(short)DbgTextColor.Local, (short)TextColor.Local,
				(short)DbgTextColor.Parameter, (short)TextColor.Parameter,
				(short)DbgTextColor.ModuleName, (short)TextColor.AssemblyModule,
				(short)DbgTextColor.Error, (short)TextColor.Error,
				(short)DbgTextColor.ToStringEval, (short)TextColor.ToStringEval,
				(short)DbgTextColor.ExceptionName, (short)TextColor.DebugExceptionName,
				(short)DbgTextColor.StowedExceptionName, (short)TextColor.DebugStowedExceptionName,
				(short)DbgTextColor.ReturnValueName, (short)TextColor.DebugReturnValueName,
				(short)DbgTextColor.VariableName, (short)TextColor.DebugVariableName,
				(short)DbgTextColor.ObjectIdName, (short)TextColor.DebugObjectIdName,
				(short)DbgTextColor.DebuggerDisplayAttributeEval, (short)TextColor.DebuggerDisplayAttributeEval,
				(short)DbgTextColor.DebuggerNoStringQuotesEval, (short)TextColor.DebuggerNoStringQuotesEval,
				(short)DbgTextColor.DebugViewPropertyName, (short)TextColor.DebugViewPropertyName,
			};
			Debug.Assert(colors.Length / 2 == Enum.GetValues(typeof(DbgTextColor)).Length);
			AddColors(colors);
			colors = new short[] {
				-1, (short)TextColor.DirectoryPart,
				-2, (short)TextColor.FileNameNoExtension,
				-3, (short)TextColor.FileExtension,
			};
			AddColors(colors);
		}

		static void AddColors(short[] colors) {
			for (int i = 0; i < colors.Length; i += 2) {
				var debuggerColor = (DbgTextColor)colors[i];
				var dnSpyColor = ((TextColor)colors[i + 1]).Box();
				toDebuggerColor.Add(dnSpyColor, debuggerColor);
				toDnSpyColor.Add(debuggerColor, dnSpyColor);
			}
		}

		public static DbgTextColor ToDebuggerColor(object color) {
			if (color == null)
				throw new ArgumentNullException(nameof(color));
			if (!toDebuggerColor.TryGetValue(color, out var debuggerColor)) {
				Debug.Fail($"Couldn't convert color '{color}'");
				debuggerColor = DbgTextColor.Error;
			}
			return debuggerColor;
		}

		public static object ToDnSpyColor(DbgTextColor color) {
			if (!toDnSpyColor.TryGetValue(color, out var dnspyColor)) {
				Debug.Fail($"Couldn't convert color '{color}'");
				dnspyColor = TextColor.Error.Box();
			}
			return dnspyColor;
		}
	}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
