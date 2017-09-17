/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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

using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Shared.Debugger.ValueNodes {
	static class ImageNameUtils {
		public static string GetImageName(DmdFieldInfo field) {
			if (field.ReflectedType.IsEnum && !field.IsSpecialName) {
				switch (field.Attributes & DmdFieldAttributes.FieldAccessMask) {
				case DmdFieldAttributes.PrivateScope:	return PredefinedDbgValueNodeImageNames.EnumerationItemCompilerControlled;
				case DmdFieldAttributes.Private:		return PredefinedDbgValueNodeImageNames.EnumerationItemPrivate;
				case DmdFieldAttributes.FamANDAssem:	return PredefinedDbgValueNodeImageNames.EnumerationItemFamilyAndAssembly;
				case DmdFieldAttributes.Assembly:		return PredefinedDbgValueNodeImageNames.EnumerationItemAssembly;
				case DmdFieldAttributes.Family:			return PredefinedDbgValueNodeImageNames.EnumerationItemFamily;
				case DmdFieldAttributes.FamORAssem:		return PredefinedDbgValueNodeImageNames.EnumerationItemFamilyOrAssembly;
				case DmdFieldAttributes.Public:			return PredefinedDbgValueNodeImageNames.EnumerationItemPublic;
				default:								return PredefinedDbgValueNodeImageNames.EnumerationItem;
				}
			}
			if (field.IsLiteral) {
				switch (field.Attributes & DmdFieldAttributes.FieldAccessMask) {
				case DmdFieldAttributes.PrivateScope:	return PredefinedDbgValueNodeImageNames.ConstantCompilerControlled;
				case DmdFieldAttributes.Private:		return PredefinedDbgValueNodeImageNames.ConstantPrivate;
				case DmdFieldAttributes.FamANDAssem:	return PredefinedDbgValueNodeImageNames.ConstantFamilyAndAssembly;
				case DmdFieldAttributes.Assembly:		return PredefinedDbgValueNodeImageNames.ConstantAssembly;
				case DmdFieldAttributes.Family:			return PredefinedDbgValueNodeImageNames.ConstantFamily;
				case DmdFieldAttributes.FamORAssem:		return PredefinedDbgValueNodeImageNames.ConstantFamilyOrAssembly;
				case DmdFieldAttributes.Public:			return PredefinedDbgValueNodeImageNames.ConstantPublic;
				default:								return PredefinedDbgValueNodeImageNames.Constant;
				}
			}
			switch (field.Attributes & DmdFieldAttributes.FieldAccessMask) {
			case DmdFieldAttributes.PrivateScope:	return PredefinedDbgValueNodeImageNames.FieldCompilerControlled;
			case DmdFieldAttributes.Private:		return PredefinedDbgValueNodeImageNames.FieldPrivate;
			case DmdFieldAttributes.FamANDAssem:	return PredefinedDbgValueNodeImageNames.FieldFamilyAndAssembly;
			case DmdFieldAttributes.Assembly:		return PredefinedDbgValueNodeImageNames.FieldAssembly;
			case DmdFieldAttributes.Family:			return PredefinedDbgValueNodeImageNames.FieldFamily;
			case DmdFieldAttributes.FamORAssem:		return PredefinedDbgValueNodeImageNames.FieldFamilyOrAssembly;
			case DmdFieldAttributes.Public:			return PredefinedDbgValueNodeImageNames.FieldPublic;
			default:								return PredefinedDbgValueNodeImageNames.Field;
			}
		}

		public static string GetImageName(DmdPropertyInfo property) {
			var method = property.GetGetMethod(DmdGetAccessorOptions.All) ?? property.GetSetMethod(DmdGetAccessorOptions.All);
			if ((object)method == null)
				return PredefinedDbgValueNodeImageNames.Property;
			switch (method.Attributes & DmdMethodAttributes.MemberAccessMask) {
			case DmdMethodAttributes.PrivateScope:	return PredefinedDbgValueNodeImageNames.PropertyCompilerControlled;
			case DmdMethodAttributes.Private:		return PredefinedDbgValueNodeImageNames.PropertyPrivate;
			case DmdMethodAttributes.FamANDAssem:	return PredefinedDbgValueNodeImageNames.PropertyFamilyAndAssembly;
			case DmdMethodAttributes.Assembly:		return PredefinedDbgValueNodeImageNames.PropertyAssembly;
			case DmdMethodAttributes.Family:		return PredefinedDbgValueNodeImageNames.PropertyFamily;
			case DmdMethodAttributes.FamORAssem:	return PredefinedDbgValueNodeImageNames.PropertyFamilyOrAssembly;
			case DmdMethodAttributes.Public:		return PredefinedDbgValueNodeImageNames.PropertyPublic;
			default:								return PredefinedDbgValueNodeImageNames.Property;
			}
		}
	}
}
