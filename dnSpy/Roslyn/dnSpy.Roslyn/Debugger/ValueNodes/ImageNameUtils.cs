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

using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Debugger.ValueNodes {
	static class ImageNameUtils {
		public static string GetImageName(DmdFieldInfo field) {
			if (field.ReflectedType!.IsEnum && !field.IsSpecialName) {
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

		public static string GetImageName(DmdType type, bool canBeModule) {
			if (canBeModule && type.DeclaringType is null && type.IsSealed && type.IsAbstract) {
				switch (type.Attributes & DmdTypeAttributes.VisibilityMask) {
				case DmdTypeAttributes.NotPublic:			return PredefinedDbgValueNodeImageNames.ModuleInternal;
				case DmdTypeAttributes.Public:				return PredefinedDbgValueNodeImageNames.ModulePublic;
				case DmdTypeAttributes.NestedPublic:		return PredefinedDbgValueNodeImageNames.ModulePublic;
				case DmdTypeAttributes.NestedPrivate:		return PredefinedDbgValueNodeImageNames.ModulePrivate;
				case DmdTypeAttributes.NestedFamily:		return PredefinedDbgValueNodeImageNames.ModuleProtected;
				case DmdTypeAttributes.NestedAssembly:		return PredefinedDbgValueNodeImageNames.ModuleInternal;
				case DmdTypeAttributes.NestedFamANDAssem:	return PredefinedDbgValueNodeImageNames.ModuleInternal;
				case DmdTypeAttributes.NestedFamORAssem:	return PredefinedDbgValueNodeImageNames.ModuleProtected;
				default:									return PredefinedDbgValueNodeImageNames.Module;
				}
			}
			if (type.IsInterface) {
				switch (type.Attributes & DmdTypeAttributes.VisibilityMask) {
				case DmdTypeAttributes.NotPublic:			return PredefinedDbgValueNodeImageNames.InterfaceInternal;
				case DmdTypeAttributes.Public:				return PredefinedDbgValueNodeImageNames.InterfacePublic;
				case DmdTypeAttributes.NestedPublic:		return PredefinedDbgValueNodeImageNames.InterfacePublic;
				case DmdTypeAttributes.NestedPrivate:		return PredefinedDbgValueNodeImageNames.InterfacePrivate;
				case DmdTypeAttributes.NestedFamily:		return PredefinedDbgValueNodeImageNames.InterfaceProtected;
				case DmdTypeAttributes.NestedAssembly:		return PredefinedDbgValueNodeImageNames.InterfaceInternal;
				case DmdTypeAttributes.NestedFamANDAssem:	return PredefinedDbgValueNodeImageNames.InterfaceInternal;
				case DmdTypeAttributes.NestedFamORAssem:	return PredefinedDbgValueNodeImageNames.InterfaceProtected;
				default:									return PredefinedDbgValueNodeImageNames.Interface;
				}
			}
			if (type.IsEnum) {
				switch (type.Attributes & DmdTypeAttributes.VisibilityMask) {
				case DmdTypeAttributes.NotPublic:			return PredefinedDbgValueNodeImageNames.EnumerationInternal;
				case DmdTypeAttributes.Public:				return PredefinedDbgValueNodeImageNames.EnumerationPublic;
				case DmdTypeAttributes.NestedPublic:		return PredefinedDbgValueNodeImageNames.EnumerationPublic;
				case DmdTypeAttributes.NestedPrivate:		return PredefinedDbgValueNodeImageNames.EnumerationPrivate;
				case DmdTypeAttributes.NestedFamily:		return PredefinedDbgValueNodeImageNames.EnumerationProtected;
				case DmdTypeAttributes.NestedAssembly:		return PredefinedDbgValueNodeImageNames.EnumerationInternal;
				case DmdTypeAttributes.NestedFamANDAssem:	return PredefinedDbgValueNodeImageNames.EnumerationInternal;
				case DmdTypeAttributes.NestedFamORAssem:	return PredefinedDbgValueNodeImageNames.EnumerationProtected;
				default:									return PredefinedDbgValueNodeImageNames.Enumeration;
				}
			}
			if (type.IsValueType) {
				switch (type.Attributes & DmdTypeAttributes.VisibilityMask) {
				case DmdTypeAttributes.NotPublic:			return PredefinedDbgValueNodeImageNames.StructureInternal;
				case DmdTypeAttributes.Public:				return PredefinedDbgValueNodeImageNames.StructurePublic;
				case DmdTypeAttributes.NestedPublic:		return PredefinedDbgValueNodeImageNames.StructurePublic;
				case DmdTypeAttributes.NestedPrivate:		return PredefinedDbgValueNodeImageNames.StructurePrivate;
				case DmdTypeAttributes.NestedFamily:		return PredefinedDbgValueNodeImageNames.StructureProtected;
				case DmdTypeAttributes.NestedAssembly:		return PredefinedDbgValueNodeImageNames.StructureInternal;
				case DmdTypeAttributes.NestedFamANDAssem:	return PredefinedDbgValueNodeImageNames.StructureInternal;
				case DmdTypeAttributes.NestedFamORAssem:	return PredefinedDbgValueNodeImageNames.StructureProtected;
				default:									return PredefinedDbgValueNodeImageNames.Structure;
				}
			}
			if (type.BaseType == type.AppDomain.System_MulticastDelegate) {
				switch (type.Attributes & DmdTypeAttributes.VisibilityMask) {
				case DmdTypeAttributes.NotPublic:			return PredefinedDbgValueNodeImageNames.DelegateInternal;
				case DmdTypeAttributes.Public:				return PredefinedDbgValueNodeImageNames.DelegatePublic;
				case DmdTypeAttributes.NestedPublic:		return PredefinedDbgValueNodeImageNames.DelegatePublic;
				case DmdTypeAttributes.NestedPrivate:		return PredefinedDbgValueNodeImageNames.DelegatePrivate;
				case DmdTypeAttributes.NestedFamily:		return PredefinedDbgValueNodeImageNames.DelegateProtected;
				case DmdTypeAttributes.NestedAssembly:		return PredefinedDbgValueNodeImageNames.DelegateInternal;
				case DmdTypeAttributes.NestedFamANDAssem:	return PredefinedDbgValueNodeImageNames.DelegateInternal;
				case DmdTypeAttributes.NestedFamORAssem:	return PredefinedDbgValueNodeImageNames.DelegateProtected;
				default:									return PredefinedDbgValueNodeImageNames.Delegate;
				}
			}
			switch (type.Attributes & DmdTypeAttributes.VisibilityMask) {
			case DmdTypeAttributes.NotPublic:			return PredefinedDbgValueNodeImageNames.ClassInternal;
			case DmdTypeAttributes.Public:				return PredefinedDbgValueNodeImageNames.ClassPublic;
			case DmdTypeAttributes.NestedPublic:		return PredefinedDbgValueNodeImageNames.ClassPublic;
			case DmdTypeAttributes.NestedPrivate:		return PredefinedDbgValueNodeImageNames.ClassPrivate;
			case DmdTypeAttributes.NestedFamily:		return PredefinedDbgValueNodeImageNames.ClassProtected;
			case DmdTypeAttributes.NestedAssembly:		return PredefinedDbgValueNodeImageNames.ClassInternal;
			case DmdTypeAttributes.NestedFamANDAssem:	return PredefinedDbgValueNodeImageNames.ClassInternal;
			case DmdTypeAttributes.NestedFamORAssem:	return PredefinedDbgValueNodeImageNames.ClassProtected;
			default:									return PredefinedDbgValueNodeImageNames.Class;
			}
		}

		public static string GetImageName(DmdMethodBase method, bool canBeModule) {
			if (method is DmdConstructorInfo)
				return GetImageName(method.DeclaringType!, canBeModule);
			if (method.IsStatic) {
				if (method.IsDefined("System.Runtime.CompilerServices.ExtensionAttribute", inherit: false))
					return PredefinedDbgValueNodeImageNames.ExtensionMethod;
			}
			switch (method.Attributes & DmdMethodAttributes.MemberAccessMask) {
			case DmdMethodAttributes.PrivateScope:	return PredefinedDbgValueNodeImageNames.MethodCompilerControlled;
			case DmdMethodAttributes.Private:		return PredefinedDbgValueNodeImageNames.MethodPrivate;
			case DmdMethodAttributes.FamANDAssem:	return PredefinedDbgValueNodeImageNames.MethodFamilyAndAssembly;
			case DmdMethodAttributes.Assembly:		return PredefinedDbgValueNodeImageNames.MethodAssembly;
			case DmdMethodAttributes.Family:		return PredefinedDbgValueNodeImageNames.MethodFamily;
			case DmdMethodAttributes.FamORAssem:	return PredefinedDbgValueNodeImageNames.MethodFamilyOrAssembly;
			case DmdMethodAttributes.Public:		return PredefinedDbgValueNodeImageNames.MethodPublic;
			default:								return PredefinedDbgValueNodeImageNames.Method;
			}
		}

		public static string GetImageName(DmdPropertyInfo property) {
			var method = property.GetGetMethod(DmdGetAccessorOptions.All) ?? property.GetSetMethod(DmdGetAccessorOptions.All);
			if (method is null)
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
