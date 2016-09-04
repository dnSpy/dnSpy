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

using dnSpy.Contracts.Text;

namespace dnSpy.Roslyn.Shared.Intellisense.Classification {
	static class CompletionKindTextColorHelper {
		public static TextColor ToTextColor(this CompletionKind kind) {
			switch (kind) {
			case CompletionKind.Unknown:				return TextColor.Text;
			case CompletionKind.ClassProtected:			return TextColor.Type;
			case CompletionKind.ClassInternal:			return TextColor.Type;
			case CompletionKind.ClassPrivate:			return TextColor.Type;
			case CompletionKind.Class:					return TextColor.Type;
			case CompletionKind.ConstantProtected:		return TextColor.Text;
			case CompletionKind.ConstantInternal:		return TextColor.Text;
			case CompletionKind.ConstantPrivate:		return TextColor.Text;
			case CompletionKind.Constant:				return TextColor.Text;
			case CompletionKind.DelegateProtected:		return TextColor.Delegate;
			case CompletionKind.DelegateInternal:		return TextColor.Delegate;
			case CompletionKind.DelegatePrivate:		return TextColor.Delegate;
			case CompletionKind.Delegate:				return TextColor.Delegate;
			case CompletionKind.EnumProtected:			return TextColor.Enum;
			case CompletionKind.EnumInternal:			return TextColor.Enum;
			case CompletionKind.EnumPrivate:			return TextColor.Enum;
			case CompletionKind.Enum:					return TextColor.Enum;
			case CompletionKind.EventProtected:			return TextColor.InstanceEvent;
			case CompletionKind.EventInternal:			return TextColor.InstanceEvent;
			case CompletionKind.EventPrivate:			return TextColor.InstanceEvent;
			case CompletionKind.Event:					return TextColor.InstanceEvent;
			case CompletionKind.ExtensionMethodProtected:return TextColor.ExtensionMethod;
			case CompletionKind.ExtensionMethodInternal:return TextColor.ExtensionMethod;
			case CompletionKind.ExtensionMethodPrivate: return TextColor.ExtensionMethod;
			case CompletionKind.ExtensionMethod:		return TextColor.ExtensionMethod;
			case CompletionKind.FieldProtected:			return TextColor.InstanceField;
			case CompletionKind.FieldInternal:			return TextColor.InstanceField;
			case CompletionKind.FieldPrivate:			return TextColor.InstanceField;
			case CompletionKind.Field:					return TextColor.InstanceField;
			case CompletionKind.InterfaceProtected:		return TextColor.Interface;
			case CompletionKind.InterfaceInternal:		return TextColor.Interface;
			case CompletionKind.InterfacePrivate:		return TextColor.Interface;
			case CompletionKind.Interface:				return TextColor.Interface;
			case CompletionKind.MethodProtected:		return TextColor.InstanceMethod;
			case CompletionKind.MethodInternal:			return TextColor.InstanceMethod;
			case CompletionKind.MethodPrivate:			return TextColor.InstanceMethod;
			case CompletionKind.Method:					return TextColor.InstanceMethod;
			case CompletionKind.ModuleProtected:		return TextColor.Module;
			case CompletionKind.ModuleInternal:			return TextColor.Module;
			case CompletionKind.ModulePrivate:			return TextColor.Module;
			case CompletionKind.Module:					return TextColor.Module;
			case CompletionKind.OperatorProtected:		return TextColor.Operator;
			case CompletionKind.OperatorInternal:		return TextColor.Operator;
			case CompletionKind.OperatorPrivate:		return TextColor.Operator;
			case CompletionKind.Operator:				return TextColor.Operator;
			case CompletionKind.PropertyProtected:		return TextColor.InstanceProperty;
			case CompletionKind.PropertyInternal:		return TextColor.InstanceProperty;
			case CompletionKind.PropertyPrivate:		return TextColor.InstanceProperty;
			case CompletionKind.Property:				return TextColor.InstanceProperty;
			case CompletionKind.StructureProtected:		return TextColor.ValueType;
			case CompletionKind.StructureInternal:		return TextColor.ValueType;
			case CompletionKind.StructurePrivate:		return TextColor.ValueType;
			case CompletionKind.Structure:				return TextColor.ValueType;
			case CompletionKind.FileCSharp:				return TextColor.Text;
			case CompletionKind.FileVisualBasic:		return TextColor.Text;
			case CompletionKind.ProjectCSharp:			return TextColor.Text;
			case CompletionKind.ProjectVisualBasic:		return TextColor.Text;
			case CompletionKind.EnumMember:				return TextColor.EnumField;
			case CompletionKind.Assembly:				return TextColor.Assembly;
			case CompletionKind.RangeVariable:			return TextColor.Local;
			case CompletionKind.Local:					return TextColor.Local;
			case CompletionKind.Parameter:				return TextColor.Parameter;
			case CompletionKind.Intrinsic:				return TextColor.Keyword;
			case CompletionKind.Keyword:				return TextColor.Keyword;
			case CompletionKind.Label:					return TextColor.Label;
			case CompletionKind.Namespace:				return TextColor.Namespace;
			case CompletionKind.Folder:					return TextColor.Text;
			case CompletionKind.Reference:				return TextColor.Assembly;
			case CompletionKind.TypeParameter:			return TextColor.TypeGenericParameter;
			case CompletionKind.Snippet:				return TextColor.Text;
			case CompletionKind.StatusError:			return TextColor.Error;
			case CompletionKind.StatusWarning:			return TextColor.Text;
			case CompletionKind.StatusInformation:		return TextColor.Text;
			default: return TextColor.Text;
			}
		}
	}
}
