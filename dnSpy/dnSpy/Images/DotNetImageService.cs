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

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using dnlib.DotNet;
using dnlib.PE;
using dnSpy.Contracts.Images;

namespace dnSpy.Images {
	[Export(typeof(IDotNetImageService))]
	sealed class DotNetImageService : IDotNetImageService {
		public ImageReference GetImageReference(IPEImage peImage) =>
			GetImageReference(peImage.ImageNTHeaders.FileHeader.Characteristics);
		public ImageReference GetNamespaceImageReference() => DsImages.Namespace;
		public ImageReference GetImageReference(ModuleDef module) => DsImages.ModulePublic;
		public ImageReference GetImageReference(TypeDef type) {
			if (type.IsValueType) {
				if (type.IsEnum) {
					switch (type.Visibility) {
					case TypeAttributes.Public:
					case TypeAttributes.NestedPublic:
						return DsImages.EnumerationPublic;
					case TypeAttributes.NestedPrivate:
						return DsImages.EnumerationPrivate;
					case TypeAttributes.NestedFamily:
						return DsImages.EnumerationProtected;
					case TypeAttributes.NotPublic:
					case TypeAttributes.NestedAssembly:
					case TypeAttributes.NestedFamANDAssem:
						return DsImages.EnumerationInternal;
					case TypeAttributes.NestedFamORAssem:
						return DsImages.EnumerationShortcut;
					}
				}
				else {
					switch (type.Visibility) {
					case TypeAttributes.Public:
					case TypeAttributes.NestedPublic:
						return DsImages.StructurePublic;
					case TypeAttributes.NestedPrivate:
						return DsImages.StructurePrivate;
					case TypeAttributes.NestedFamily:
						return DsImages.StructureProtected;
					case TypeAttributes.NotPublic:
					case TypeAttributes.NestedAssembly:
					case TypeAttributes.NestedFamANDAssem:
						return DsImages.StructureInternal;
					case TypeAttributes.NestedFamORAssem:
						return DsImages.StructureShortcut;
					}
				}
			}
			else {
				if (type.IsInterface) {
					switch (type.Visibility) {
					case TypeAttributes.Public:
					case TypeAttributes.NestedPublic:
						return DsImages.InterfacePublic;
					case TypeAttributes.NestedPrivate:
						return DsImages.InterfacePrivate;
					case TypeAttributes.NestedFamily:
						return DsImages.InterfaceProtected;
					case TypeAttributes.NotPublic:
					case TypeAttributes.NestedAssembly:
					case TypeAttributes.NestedFamANDAssem:
						return DsImages.InterfaceInternal;
					case TypeAttributes.NestedFamORAssem:
						return DsImages.InterfaceShortcut;
					}
				}
				else if (IsDelegate(type)) {
					switch (type.Visibility) {
					case TypeAttributes.Public:
					case TypeAttributes.NestedPublic:
						return DsImages.DelegatePublic;
					case TypeAttributes.NestedPrivate:
						return DsImages.DelegatePrivate;
					case TypeAttributes.NestedFamily:
						return DsImages.DelegateProtected;
					case TypeAttributes.NotPublic:
					case TypeAttributes.NestedAssembly:
					case TypeAttributes.NestedFamANDAssem:
						return DsImages.DelegateInternal;
					case TypeAttributes.NestedFamORAssem:
						return DsImages.DelegateShortcut;
					}
				}
				else if (IsException(type)) {
					switch (type.Visibility) {
					case TypeAttributes.Public:
					case TypeAttributes.NestedPublic:
						return DsImages.ExceptionPublic;
					case TypeAttributes.NestedPrivate:
						return DsImages.ExceptionPrivate;
					case TypeAttributes.NestedFamily:
						return DsImages.ExceptionProtected;
					case TypeAttributes.NotPublic:
					case TypeAttributes.NestedAssembly:
					case TypeAttributes.NestedFamANDAssem:
						return DsImages.ExceptionInternal;
					case TypeAttributes.NestedFamORAssem:
						return DsImages.ExceptionShortcut;
					}
				}
				else if (type.GenericParameters.Count > 0) {
					switch (type.Visibility) {
					case TypeAttributes.Public:
					case TypeAttributes.NestedPublic:
						return DsImages.Template;
					case TypeAttributes.NestedPrivate:
						return DsImages.TemplatePrivate;
					case TypeAttributes.NestedFamily:
						return DsImages.TemplateProtected;
					case TypeAttributes.NotPublic:
					case TypeAttributes.NestedAssembly:
					case TypeAttributes.NestedFamANDAssem:
						return DsImages.TemplateInternal;
					case TypeAttributes.NestedFamORAssem:
						return DsImages.TemplateShortcut;
					}
				}
				else {
					switch (type.Visibility) {
					case TypeAttributes.Public:
					case TypeAttributes.NestedPublic:
						return DsImages.ClassPublic;
					case TypeAttributes.NestedPrivate:
						return DsImages.ClassPrivate;
					case TypeAttributes.NestedFamily:
						return DsImages.ClassProtected;
					case TypeAttributes.NotPublic:
					case TypeAttributes.NestedAssembly:
					case TypeAttributes.NestedFamANDAssem:
						return DsImages.ClassInternal;
					case TypeAttributes.NestedFamORAssem:
						return DsImages.ClassShortcut;
					}
				}
			}
			Debug.Fail("Impossible to get here");
			return default(ImageReference);
		}

		static bool IsDelegate(TypeDef type) =>
			type.BaseType != null && type.BaseType.FullName == "System.MulticastDelegate" && type.BaseType.DefinitionAssembly.IsCorLib();

		static bool IsException(TypeDef type) {
			if (IsSystemException(type))
				return true;
			while (type != null) {
				if (IsSystemException(type.BaseType))
					return true;
				var bt = type.BaseType;
				type = bt == null ? null : bt.ScopeType.ResolveTypeDef();
			}
			return false;
		}

		static bool IsSystemException(ITypeDefOrRef type) =>
			type != null &&
			type.DeclaringType == null &&
			type.Namespace == "System" &&
			type.Name == "Exception" &&
			type.DefinitionAssembly.IsCorLib();
		public ImageReference GetImageReference(FieldDef field) {
			if (field.DeclaringType.IsEnum && !field.IsSpecialName) {
				switch (field.Access) {
				default:
				case FieldAttributes.Public:
					return DsImages.EnumerationItemPublic;
				case FieldAttributes.Private:
					return DsImages.EnumerationItemPrivate;
				case FieldAttributes.Family:
					return DsImages.EnumerationItemProtected;
				case FieldAttributes.Assembly:
				case FieldAttributes.FamANDAssem:
					return DsImages.EnumerationItemInternal;
				case FieldAttributes.CompilerControlled:
					return DsImages.EnumerationItemSealed;
				case FieldAttributes.FamORAssem:
					return DsImages.EnumerationItemShortcut;
				}
			}

			if (field.IsLiteral || (field.IsInitOnly && IsDecimalConstant(field))) {
				switch (field.Access) {
				default:
				case FieldAttributes.Public:
					return DsImages.ConstantPublic;
				case FieldAttributes.Private:
					return DsImages.ConstantPrivate;
				case FieldAttributes.Family:
					return DsImages.ConstantProtected;
				case FieldAttributes.Assembly:
				case FieldAttributes.FamANDAssem:
					return DsImages.ConstantInternal;
				case FieldAttributes.CompilerControlled:
					return DsImages.ConstantSealed;
				case FieldAttributes.FamORAssem:
					return DsImages.ConstantShortcut;
				}
			}
			else {
				switch (field.Access) {
				default:
				case FieldAttributes.Public:
					return DsImages.FieldPublic;
				case FieldAttributes.Private:
					return DsImages.FieldPrivate;
				case FieldAttributes.Family:
					return DsImages.FieldProtected;
				case FieldAttributes.Assembly:
				case FieldAttributes.FamANDAssem:
					return DsImages.FieldInternal;
				case FieldAttributes.CompilerControlled:
					return DsImages.FieldSealed;
				case FieldAttributes.FamORAssem:
					return DsImages.FieldShortcut;
				}
			}
		}

		static bool IsSystemDecimal(TypeSig ts) => ts != null && ts.DefinitionAssembly.IsCorLib() && ts.FullName == "System.Decimal";
		static bool IsDecimalConstant(FieldDef field) => IsSystemDecimal(field.FieldType) && field.CustomAttributes.IsDefined("System.Runtime.CompilerServices.DecimalConstantAttribute");

		public ImageReference GetImageReference(MethodDef method) {
			if (method.IsSpecialName && method.Name.StartsWith("op_", StringComparison.Ordinal)) {
				switch (method.Access) {
				default:
				case MethodAttributes.Public:
					return DsImages.OperatorPublic;
				case MethodAttributes.Private:
					return DsImages.OperatorPrivate;
				case MethodAttributes.Family:
					return DsImages.OperatorProtected;
				case MethodAttributes.Assembly:
				case MethodAttributes.FamANDAssem:
					return DsImages.OperatorInternal;
				case MethodAttributes.CompilerControlled:
					return DsImages.OperatorSealed;
				case MethodAttributes.FamORAssem:
					return DsImages.OperatorShortcut;
				}
			}

			if (method.IsStatic && method.CustomAttributes.IsDefined("System.Runtime.CompilerServices.ExtensionAttribute"))
				return DsImages.ExtensionMethod;

			switch (method.Access) {
			default:
			case MethodAttributes.Public:
				return DsImages.MethodPublic;
			case MethodAttributes.Private:
				return DsImages.MethodPrivate;
			case MethodAttributes.Family:
				return DsImages.MethodProtected;
			case MethodAttributes.Assembly:
			case MethodAttributes.FamANDAssem:
				return DsImages.MethodInternal;
			case MethodAttributes.CompilerControlled:
				return DsImages.MethodSealed;
			case MethodAttributes.FamORAssem:
				return DsImages.MethodShortcut;
			}
		}

		public ImageReference GetImageReference(EventDef @event) {
			var method = @event.AddMethod ?? @event.RemoveMethod;
			if (method == null)
				return DsImages.EventPublic;

			switch (method.Access) {
			default:
			case MethodAttributes.Public:
				return DsImages.EventPublic;
			case MethodAttributes.Private:
				return DsImages.EventPrivate;
			case MethodAttributes.Family:
				return DsImages.EventProtected;
			case MethodAttributes.Assembly:
			case MethodAttributes.FamANDAssem:
				return DsImages.EventInternal;
			case MethodAttributes.CompilerControlled:
				return DsImages.EventSealed;
			case MethodAttributes.FamORAssem:
				return DsImages.EventShortcut;
			}
		}

		public ImageReference GetImageReference(PropertyDef property) {
			var method = property.GetMethod ?? property.SetMethod;
			if (method == null)
				return DsImages.Property;

			switch (method.Access) {
			default:
			case MethodAttributes.Public:
				return DsImages.Property;
			case MethodAttributes.Private:
				return DsImages.PropertyPrivate;
			case MethodAttributes.Family:
				return DsImages.PropertyProtected;
			case MethodAttributes.Assembly:
			case MethodAttributes.FamANDAssem:
				return DsImages.PropertyInternal;
			case MethodAttributes.CompilerControlled:
				return DsImages.PropertySealed;
			case MethodAttributes.FamORAssem:
				return DsImages.PropertyShortcut;
			}
		}

		public ImageReference GetImageReferenceModuleRef() => DsImages.Reference;
		public ImageReference GetImageReference(AssemblyDef assembly) =>
			GetImageReference(assembly.ManifestModule?.Characteristics ?? Characteristics.Dll);
		public ImageReference GetImageReferenceAssemblyRef() => DsImages.Reference;
		public ImageReference GetImageReferenceGenericParameter() => DsImages.Type;
		public ImageReference GetImageReferenceLocal() => DsImages.LocalVariable;
		public ImageReference GetImageReferenceParameter() => DsImages.LocalVariable;
		public ImageReference GetImageReferenceType() => DsImages.ClassPublic;
		public ImageReference GetImageReferenceMethod() => DsImages.MethodPublic;
		public ImageReference GetImageReferenceField() => DsImages.FieldPublic;

		ImageReference GetImageReference(Characteristics ch) {
			bool isExe = (ch & Characteristics.Dll) == 0;
			return isExe ? DsImages.AssemblyExe : DsImages.Assembly;
		}
	}
}
