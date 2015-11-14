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

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using dnlib.DotNet;
using dnlib.PE;
using dnSpy.Contracts.Images;

namespace dnSpy.Images {
	[Export, Export(typeof(IDotNetImageManager)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class DotNetImageManager : IDotNetImageManager {
		public ImageReference GetImageReference(IPEImage peImage) {
			return GetGetImageReference(peImage.ImageNTHeaders.FileHeader.Characteristics);
		}

		public ImageReference GetNamespaceImageReference() {
			return new ImageReference(GetType().Assembly, "Namespace");
		}

		public ImageReference GetImageReference(ModuleDef mod) {
			return new ImageReference(GetType().Assembly, "AssemblyModule");
		}

		public ImageReference GetImageReference(TypeDef type) {
			return new ImageReference(GetType().Assembly, GetImageName(type));
		}

		static string GetImageName(TypeDef type) {
			if (type.IsValueType) {
				if (type.IsEnum) {
					switch (type.Visibility) {
					case TypeAttributes.Public:
					case TypeAttributes.NestedPublic:
						return "Enum";
					case TypeAttributes.NestedPrivate:
						return "EnumPrivate";
					case TypeAttributes.NestedFamily:
						return "EnumProtected";
					case TypeAttributes.NotPublic:
					case TypeAttributes.NestedAssembly:
					case TypeAttributes.NestedFamANDAssem:
						return "EnumInternal";
					case TypeAttributes.NestedFamORAssem:
						return "EnumProtectedInternal";
					}
				}
				else {
					switch (type.Visibility) {
					case TypeAttributes.Public:
					case TypeAttributes.NestedPublic:
						return "Struct";
					case TypeAttributes.NestedPrivate:
						return "StructPrivate";
					case TypeAttributes.NestedFamily:
						return "StructProtected";
					case TypeAttributes.NotPublic:
					case TypeAttributes.NestedAssembly:
					case TypeAttributes.NestedFamANDAssem:
						return "StructInternal";
					case TypeAttributes.NestedFamORAssem:
						return "StructProtectedInternal";
					}
				}
			}
			else {
				if (type.IsInterface) {
					switch (type.Visibility) {
					case TypeAttributes.Public:
					case TypeAttributes.NestedPublic:
						return "Interface";
					case TypeAttributes.NestedPrivate:
						return "InterfacePrivate";
					case TypeAttributes.NestedFamily:
						return "InterfaceProtected";
					case TypeAttributes.NotPublic:
					case TypeAttributes.NestedAssembly:
					case TypeAttributes.NestedFamANDAssem:
						return "InterfaceInternal";
					case TypeAttributes.NestedFamORAssem:
						return "InterfaceProtectedInternal";
					}
				}
				else if (IsDelegate(type)) {
					switch (type.Visibility) {
					case TypeAttributes.Public:
					case TypeAttributes.NestedPublic:
						return "Delegate";
					case TypeAttributes.NestedPrivate:
						return "DelegatePrivate";
					case TypeAttributes.NestedFamily:
						return "DelegateProtected";
					case TypeAttributes.NotPublic:
					case TypeAttributes.NestedAssembly:
					case TypeAttributes.NestedFamANDAssem:
						return "DelegateInternal";
					case TypeAttributes.NestedFamORAssem:
						return "DelegateProtectedInternal";
					}
				}
				else if (IsException(type)) {
					switch (type.Visibility) {
					case TypeAttributes.Public:
					case TypeAttributes.NestedPublic:
						return "Exception";
					case TypeAttributes.NestedPrivate:
						return "ExceptionPrivate";
					case TypeAttributes.NestedFamily:
						return "ExceptionProtected";
					case TypeAttributes.NotPublic:
					case TypeAttributes.NestedAssembly:
					case TypeAttributes.NestedFamANDAssem:
						return "ExceptionInternal";
					case TypeAttributes.NestedFamORAssem:
						return "ExceptionProtectedInternal";
					}
				}
				else if (type.GenericParameters.Count > 0) {
					switch (type.Visibility) {
					case TypeAttributes.Public:
					case TypeAttributes.NestedPublic:
						return "Generic";
					case TypeAttributes.NestedPrivate:
						return "GenericPrivate";
					case TypeAttributes.NestedFamily:
						return "GenericProtected";
					case TypeAttributes.NotPublic:
					case TypeAttributes.NestedAssembly:
					case TypeAttributes.NestedFamANDAssem:
						return "GenericInternal";
					case TypeAttributes.NestedFamORAssem:
						return "GenericProtectedInternal";
					}
				}
				else if (IsStaticClass(type))
					return "StaticClass";
				else {
					switch (type.Visibility) {
					case TypeAttributes.Public:
					case TypeAttributes.NestedPublic:
						return "Class";
					case TypeAttributes.NestedPrivate:
						return "ClassPrivate";
					case TypeAttributes.NestedFamily:
						return "ClassProtected";
					case TypeAttributes.NotPublic:
					case TypeAttributes.NestedAssembly:
					case TypeAttributes.NestedFamANDAssem:
						return "ClassInternal";
					case TypeAttributes.NestedFamORAssem:
						return "ClassProtectedInternal";
					}
				}
			}
			Debug.Fail("Impossible to get here");
			return null;
		}

		static bool IsDelegate(TypeDef type) {
			return type.BaseType != null && type.BaseType.FullName == "System.MulticastDelegate" && type.BaseType.DefinitionAssembly.IsCorLib();
		}

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

		static bool IsSystemException(ITypeDefOrRef type) {
			return type != null &&
				type.DeclaringType == null &&
				type.Namespace == "System" &&
				type.Name == "Exception" &&
				type.DefinitionAssembly.IsCorLib();
		}

		static bool IsStaticClass(TypeDef type) {
			return type.IsSealed && type.IsAbstract;
		}

		public ImageReference GetImageReference(FieldDef field) {
			return new ImageReference(GetType().Assembly, GetImageName(field));
		}

		static string GetImageName(FieldDef field) {
			if (field.DeclaringType.IsEnum && !field.IsSpecialName) {
				switch (field.Access) {
				default:
				case FieldAttributes.Public:
					return "EnumValue";
				case FieldAttributes.Private:
					return "EnumValuePrivate";
				case FieldAttributes.Family:
					return "EnumValueProtected";
				case FieldAttributes.Assembly:
				case FieldAttributes.FamANDAssem:
					return "EnumValueInternal";
				case FieldAttributes.CompilerControlled:
					return "EnumValueCompilerControlled";
				case FieldAttributes.FamORAssem:
					return "EnumValueProtectedInternal";
				}
			}

			if (field.IsLiteral || (field.IsInitOnly && IsDecimalConstant(field))) {
				switch (field.Access) {
				default:
				case FieldAttributes.Public:
					return "Literal";
				case FieldAttributes.Private:
					return "LiteralPrivate";
				case FieldAttributes.Family:
					return "LiteralProtected";
				case FieldAttributes.Assembly:
				case FieldAttributes.FamANDAssem:
					return "LiteralInternal";
				case FieldAttributes.CompilerControlled:
					return "LiteralCompilerControlled";
				case FieldAttributes.FamORAssem:
					return "LiteralProtectedInternal";
				}
			}
			else if (field.IsInitOnly) {
				switch (field.Access) {
				default:
				case FieldAttributes.Public:
					return "FieldReadOnly";
				case FieldAttributes.Private:
					return "FieldReadOnlyPrivate";
				case FieldAttributes.Family:
					return "FieldReadOnlyProtected";
				case FieldAttributes.Assembly:
				case FieldAttributes.FamANDAssem:
					return "FieldReadOnlyInternal";
				case FieldAttributes.CompilerControlled:
					return "FieldReadOnlyCompilerControlled";
				case FieldAttributes.FamORAssem:
					return "FieldReadOnlyProtectedInternal";
				}
			}
			else {
				switch (field.Access) {
				default:
				case FieldAttributes.Public:
					return "Field";
				case FieldAttributes.Private:
					return "FieldPrivate";
				case FieldAttributes.Family:
					return "FieldProtected";
				case FieldAttributes.Assembly:
				case FieldAttributes.FamANDAssem:
					return "FieldInternal";
				case FieldAttributes.CompilerControlled:
					return "FieldCompilerControlled";
				case FieldAttributes.FamORAssem:
					return "FieldProtectedInternal";
				}
			}
		}

		static bool IsSystemDecimal(TypeSig ts) {
			return ts != null && ts.DefinitionAssembly.IsCorLib() && ts.FullName != "System.Decimal";
		}

		static bool IsDecimalConstant(FieldDef field) {
			return IsSystemDecimal(field.FieldType) &&
				field.CustomAttributes.IsDefined("System.Runtime.CompilerServices.DecimalConstantAttribute");
		}

		public ImageReference GetImageReference(MethodDef method) {
			return new ImageReference(GetType().Assembly, GetImageName(method));
		}

		static string GetImageName(MethodDef method) {
			if (method.IsSpecialName && method.Name.StartsWith("op_", StringComparison.Ordinal)) {
				switch (method.Access) {
				default:
				case MethodAttributes.Public:
					return "Operator";
				case MethodAttributes.Private:
					return "OperatorPrivate";
				case MethodAttributes.Family:
					return "OperatorProtected";
				case MethodAttributes.Assembly:
				case MethodAttributes.FamANDAssem:
					return "OperatorInternal";
				case MethodAttributes.CompilerControlled:
					return "OperatorCompilerControlled";
				case MethodAttributes.FamORAssem:
					return "OperatorProtectedInternal";
				}
			}

			if (method.IsStatic && method.CustomAttributes.IsDefined("System.Runtime.CompilerServices.ExtensionAttribute")) {
				switch (method.Access) {
				default:
				case MethodAttributes.Public:
					return "ExtensionMethod";
				case MethodAttributes.Private:
					return "ExtensionMethodPrivate";
				case MethodAttributes.Family:
					return "ExtensionMethodProtected";
				case MethodAttributes.Assembly:
				case MethodAttributes.FamANDAssem:
					return "ExtensionMethodInternal";
				case MethodAttributes.CompilerControlled:
					return "ExtensionMethodCompilerControlled";
				case MethodAttributes.FamORAssem:
					return "ExtensionMethodProtectedInternal";
				}
			}

			if (method.IsConstructor) {
				switch (method.Access) {
				default:
				case MethodAttributes.Public:
					return "Constructor";
				case MethodAttributes.Private:
					return "ConstructorPrivate";
				case MethodAttributes.Family:
					return "ConstructorProtected";
				case MethodAttributes.Assembly:
				case MethodAttributes.FamANDAssem:
					return "ConstructorInternal";
				case MethodAttributes.CompilerControlled:
					return "ConstructorCompilerControlled";
				case MethodAttributes.FamORAssem:
					return "ConstructorProtectedInternal";
				}
			}

			if (method.HasImplMap) {
				switch (method.Access) {
				default:
				case MethodAttributes.Public:
					return "PInvokeMethod";
				case MethodAttributes.Private:
					return "PInvokeMethodPrivate";
				case MethodAttributes.Family:
					return "PInvokeMethodProtected";
				case MethodAttributes.Assembly:
				case MethodAttributes.FamANDAssem:
					return "PInvokeMethodInternal";
				case MethodAttributes.CompilerControlled:
					return "PInvokeMethodCompilerControlled";
				case MethodAttributes.FamORAssem:
					return "PInvokeMethodProtectedInternal";
				}
			}

			if (method.IsStatic) {
				switch (method.Access) {
				default:
				case MethodAttributes.Public:
					return "StaticMethod";
				case MethodAttributes.Private:
					return "StaticMethodPrivate";
				case MethodAttributes.Family:
					return "StaticMethodProtected";
				case MethodAttributes.Assembly:
				case MethodAttributes.FamANDAssem:
					return "StaticMethodInternal";
				case MethodAttributes.CompilerControlled:
					return "StaticMethodCompilerControlled";
				case MethodAttributes.FamORAssem:
					return "StaticMethodProtectedInternal";
				}
			}

			if (method.IsVirtual) {
				switch (method.Access) {
				default:
				case MethodAttributes.Public:
					return "VirtualMethod";
				case MethodAttributes.Private:
					return "VirtualMethodPrivate";
				case MethodAttributes.Family:
					return "VirtualMethodProtected";
				case MethodAttributes.Assembly:
				case MethodAttributes.FamANDAssem:
					return "VirtualMethodInternal";
				case MethodAttributes.CompilerControlled:
					return "VirtualMethodCompilerControlled";
				case MethodAttributes.FamORAssem:
					return "VirtualMethodProtectedInternal";
				}
			}
			switch (method.Access) {
			default:
			case MethodAttributes.Public:
				return "Method";
			case MethodAttributes.Private:
				return "MethodPrivate";
			case MethodAttributes.Family:
				return "MethodProtected";
			case MethodAttributes.Assembly:
			case MethodAttributes.FamANDAssem:
				return "MethodInternal";
			case MethodAttributes.CompilerControlled:
				return "MethodCompilerControlled";
			case MethodAttributes.FamORAssem:
				return "MethodProtectedInternal";
			}
		}

		public ImageReference GetImageReference(EventDef @event) {
			return new ImageReference(GetType().Assembly, GetImageName(@event));
		}

		static string GetImageName(EventDef @event) {
			var method = @event.AddMethod ?? @event.RemoveMethod;
			if (method == null)
				return "Event";

			if (method.IsStatic) {
				switch (method.Access) {
				default:
				case MethodAttributes.Public:
					return "StaticEvent";
				case MethodAttributes.Private:
					return "StaticEventPrivate";
				case MethodAttributes.Family:
					return "StaticEventProtected";
				case MethodAttributes.Assembly:
				case MethodAttributes.FamANDAssem:
					return "StaticEventInternal";
				case MethodAttributes.CompilerControlled:
					return "StaticEventCompilerControlled";
				case MethodAttributes.FamORAssem:
					return "StaticEventProtectedInternal";
				}
			}

			if (method.IsVirtual) {
				switch (method.Access) {
				default:
				case MethodAttributes.Public:
					return "VirtualEvent";
				case MethodAttributes.Private:
					return "VirtualEventPrivate";
				case MethodAttributes.Family:
					return "VirtualEventProtected";
				case MethodAttributes.Assembly:
				case MethodAttributes.FamANDAssem:
					return "VirtualEventInternal";
				case MethodAttributes.CompilerControlled:
					return "VirtualEventCompilerControlled";
				case MethodAttributes.FamORAssem:
					return "VirtualEventProtectedInternal";
				}
			}

			switch (method.Access) {
			default:
			case MethodAttributes.Public:
				return "Event";
			case MethodAttributes.Private:
				return "EventPrivate";
			case MethodAttributes.Family:
				return "EventProtected";
			case MethodAttributes.Assembly:
			case MethodAttributes.FamANDAssem:
				return "EventInternal";
			case MethodAttributes.CompilerControlled:
				return "EventCompilerControlled";
			case MethodAttributes.FamORAssem:
				return "EventProtectedInternal";
			}
		}

		public ImageReference GetImageReference(PropertyDef property) {
			return new ImageReference(GetType().Assembly, GetImageName(property));
		}

		static string GetImageName(PropertyDef property) {
			var method = property.GetMethod ?? property.SetMethod;
			if (method == null)
				return "Property";

			if (method.IsStatic) {
				switch (method.Access) {
				default:
				case MethodAttributes.Public:
					return "StaticProperty";
				case MethodAttributes.Private:
					return "StaticPropertyPrivate";
				case MethodAttributes.Family:
					return "StaticPropertyProtected";
				case MethodAttributes.Assembly:
				case MethodAttributes.FamANDAssem:
					return "StaticPropertyInternal";
				case MethodAttributes.CompilerControlled:
					return "StaticPropertyCompilerControlled";
				case MethodAttributes.FamORAssem:
					return "StaticPropertyProtectedInternal";
				}
			}

			if (method.IsVirtual) {
				switch (method.Access) {
				default:
				case MethodAttributes.Public:
					return "VirtualProperty";
				case MethodAttributes.Private:
					return "VirtualPropertyPrivate";
				case MethodAttributes.Family:
					return "VirtualPropertyProtected";
				case MethodAttributes.Assembly:
				case MethodAttributes.FamANDAssem:
					return "VirtualPropertyInternal";
				case MethodAttributes.CompilerControlled:
					return "VirtualPropertyCompilerControlled";
				case MethodAttributes.FamORAssem:
					return "VirtualPropertyProtectedInternal";
				}
			}

			switch (method.Access) {
			default:
			case MethodAttributes.Public:
				return "Property";
			case MethodAttributes.Private:
				return "PropertyPrivate";
			case MethodAttributes.Family:
				return "PropertyProtected";
			case MethodAttributes.Assembly:
			case MethodAttributes.FamANDAssem:
				return "PropertyInternal";
			case MethodAttributes.CompilerControlled:
				return "PropertyCompilerControlled";
			case MethodAttributes.FamORAssem:
				return "PropertyProtectedInternal";
			}
		}

		public ImageReference GetImageReference(ModuleRef modRef) {
			return new ImageReference(GetType().Assembly, "ModuleReference");
		}

		public ImageReference GetImageReference(AssemblyDef assembly) {
			var mod = assembly.ManifestModule;
			return GetGetImageReference(mod != null ? mod.Characteristics : Characteristics.Dll);
		}

		public ImageReference GetImageReference(AssemblyRef asmRef) {
			return new ImageReference(GetType().Assembly, "AssemblyReference");
		}

		ImageReference GetGetImageReference(Characteristics ch) {
			bool isExe = (ch & Characteristics.Dll) == 0;
			return new ImageReference(GetType().Assembly, isExe ? "AssemblyExe" : "Assembly");
		}
	}
}
