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

using System.Collections.Generic;
using System.Runtime.InteropServices;
using dnlib.DotNet;

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Custom attributes utils
	/// </summary>
	public static class CustomAttributesUtils {
		static bool IsType(TypeDef type, (UTF8String @namespace, UTF8String name)[] typeNames) {
			if (type.DeclaringType is not null)
				return false;
			var name = type.Name;
			var @namespace = type.Namespace;
			foreach (var info in typeNames) {
				if (name == info.name && @namespace == info.@namespace)
					return true;
			}
			return false;
		}

		/// <summary>
		/// Checks whether <paramref name="type"/> is a pseudo custom attribute type
		/// </summary>
		/// <param name="type">Type to check</param>
		/// <returns></returns>
		public static bool IsPseudoCustomAttributeType(TypeDef type) {
			if (!(type.BaseType is ITypeDefOrRef baseType))
				return false;
			bool canCheck = false;
			if (baseType.Name == attributeName && baseType.Namespace == systemName)
				canCheck = true;
			else if (IsSecurityAttribute(type))
				return true;

			return canCheck && IsType(type, pseudoCANames);
		}

		static bool IsSecurityAttribute(TypeDef type) {
			for (int i = 0; i < 1000; i++) {
				if (type.Name == securityAttributeName && type.Namespace == systemSecurityPermissionsName)
					return true;
				if (!(type.BaseType is ITypeDefOrRef baseType))
					break;
				if (!(baseType.ResolveTypeDef() is TypeDef bt))
					break;
				type = bt;
			}
			return false;
		}

		/// <summary>
		/// Checks whether <paramref name="type"/> is a pseudo custom attribute related type
		/// </summary>
		/// <param name="type">Type to check</param>
		/// <returns></returns>
		public static bool IsPseudoCustomAttributeOtherType(TypeDef type) {
			var nonNestedType = type;
			while (nonNestedType.DeclaringType is TypeDef declType)
				nonNestedType = declType;
			if (nonNestedType.Namespace == systemSecurityPermissionsName && IsPublic(type))
				return true;

			return IsType(type, pseudoCAOtherTypeNames);
		}

		static bool IsPublic(TypeDef type) {
			for (;;) {
				if (type.DeclaringType is TypeDef declType) {
					if (!(type.IsNestedFamily || type.IsNestedFamilyOrAssembly || type.IsNestedPublic))
						return false;
					type = declType;
				}
				else
					return type.IsPublic;
			}
		}

		/// <summary>
		/// Gets custom attributes and pseudo custom attributes
		/// </summary>
		/// <param name="hca">Object with custom attributes</param>
		/// <returns></returns>
		public static IEnumerable<CustomAttribute> GetCustomAttributes(this IHasCustomAttribute hca) {
			switch (hca) {
			case AssemblyDef asm:		return asm.GetCustomAttributes();
			case ModuleDef mod:			return mod.GetCustomAttributes();
			case TypeDef type:			return type.GetCustomAttributes();
			case GenericParam gp:		return gp.GetCustomAttributes();
			case FieldDef field:		return field.GetCustomAttributes();
			case PropertyDef property:	return property.GetCustomAttributes();
			case EventDef @event:		return @event.GetCustomAttributes();
			case MethodDef method:		return method.GetCustomAttributes();
			case ParamDef parameter:	return parameter.GetCustomAttributes();
			default:					return hca.CustomAttributes;
			}
		}

		/// <summary>
		/// Gets custom attributes and pseudo custom attributes
		/// </summary>
		/// <param name="gp">Generic parameter</param>
		/// <returns></returns>
		public static IEnumerable<CustomAttribute> GetCustomAttributes(this GenericParam gp) => gp.CustomAttributes;

		/// <summary>
		/// Gets custom attributes and pseudo custom attributes
		/// </summary>
		/// <param name="event">Event</param>
		/// <returns></returns>
		public static IEnumerable<CustomAttribute> GetCustomAttributes(this EventDef @event) => @event.CustomAttributes;

		static IEnumerable<CustomAttribute> GetSecurityDeclarations(ModuleDef module, IHasDeclSecurity hds) {
			TypeSig? securityActionType = null;
			foreach (var ds in hds.DeclSecurities) {
				if (securityActionType is null)
					securityActionType = new ValueTypeSig(new TypeRefUser(module, systemSecurityPermissionsName, securityActionName, module.CorLibTypes.AssemblyRef));
				foreach (var secAttr in ds.SecurityAttributes) {
					var ca = new CustomAttribute(new MemberRefUser(module, ctorName, MethodSig.CreateInstance(module.CorLibTypes.Void, securityActionType), secAttr.AttributeType));
					ca.ConstructorArguments.Add(new CAArgument(securityActionType, (int)ds.Action));
					foreach (var namedArg in secAttr.NamedArguments)
						ca.NamedArguments.Add(namedArg);
					yield return ca;
				}
			}
		}

		/// <summary>
		/// Gets custom attributes and pseudo custom attributes
		/// </summary>
		/// <param name="assembly">Assembly</param>
		/// <returns></returns>
		public static IEnumerable<CustomAttribute> GetCustomAttributes(this AssemblyDef assembly) {
			var module = (ModuleDef?)assembly.ManifestModule;
			if (module is not null) {
				if (assembly.HashAlgorithm != AssemblyHashAlgorithm.SHA1) {
					var declType = new TypeRefUser(module, systemReflectionName, assemblyAlgorithmIdAttributeName, module.CorLibTypes.AssemblyRef);
					var enumDeclType = new ValueTypeSig(new TypeRefUser(module, systemConfigurationAssembliesName, assemblyHashAlgorithmName, module.CorLibTypes.AssemblyRef));
					var ca = new CustomAttribute(new MemberRefUser(module, ctorName, MethodSig.CreateInstance(module.CorLibTypes.Void, enumDeclType), declType));
					ca.ConstructorArguments.Add(new CAArgument(enumDeclType, (int)assembly.HashAlgorithm));
					yield return ca;
				}
				if (!UTF8String.IsNullOrEmpty(assembly.Culture)) {
					var declType = new TypeRefUser(module, systemReflectionName, assemblyCultureAttributeName, module.CorLibTypes.AssemblyRef);
					var ca = new CustomAttribute(new MemberRefUser(module, ctorName, MethodSig.CreateInstance(module.CorLibTypes.Void, module.CorLibTypes.String), declType));
					ca.ConstructorArguments.Add(new CAArgument(module.CorLibTypes.String, assembly.Culture));
					yield return ca;
				}
				var asmAttrs = assembly.Attributes & ~AssemblyAttributes.PublicKey;
				if (asmAttrs != AssemblyAttributes.None) {
					var declType = new TypeRefUser(module, systemReflectionName, assemblyFlagsAttributeName, module.CorLibTypes.AssemblyRef);
					var enumDeclType = new ValueTypeSig(new TypeRefUser(module, systemReflectionName, assemblyNameFlagsName, module.CorLibTypes.AssemblyRef));
					var ca = new CustomAttribute(new MemberRefUser(module, ctorName, MethodSig.CreateInstance(module.CorLibTypes.Void, enumDeclType), declType));
					ca.ConstructorArguments.Add(new CAArgument(enumDeclType, (int)asmAttrs));
					yield return ca;
				}
				{
					var declType = new TypeRefUser(module, systemReflectionName, assemblyVersionAttributeName, module.CorLibTypes.AssemblyRef);
					var ca = new CustomAttribute(new MemberRefUser(module, ctorName, MethodSig.CreateInstance(module.CorLibTypes.Void, module.CorLibTypes.String), declType));
					ca.ConstructorArguments.Add(new CAArgument(module.CorLibTypes.String, new UTF8String(assembly.Version.ToString())));
					yield return ca;
				}
			}
			foreach (var ca in assembly.CustomAttributes)
				yield return ca;
			if (module is not null) {
				foreach (var ca in GetSecurityDeclarations(module, assembly))
					yield return ca;
			}

			foreach (var asmModule in assembly.Modules) {
				TypeSig? typeType = null;
				TypeRefUser? typeForwardedToAttributeType = null;
				MemberRefUser? ctor = null;
				foreach (var exportedType in asmModule.ExportedTypes) {
					if (!exportedType.MovedToAnotherAssembly)
						continue;
					if (typeForwardedToAttributeType is null)
						typeForwardedToAttributeType = new TypeRefUser(asmModule, systemRuntimeCompilerServicesName, typeForwardedToAttributeName, asmModule.CorLibTypes.AssemblyRef);
					if (typeType is null)
						typeType = new ClassSig(new TypeRefUser(asmModule, systemName, typeName, asmModule.CorLibTypes.AssemblyRef));
					if (ctor is null)
						ctor = new MemberRefUser(asmModule, ctorName, MethodSig.CreateInstance(asmModule.CorLibTypes.Void, typeType), typeForwardedToAttributeType);
					var ca = new CustomAttribute(ctor);
					ca.ConstructorArguments.Add(new CAArgument(typeType, exportedType.ToTypeRef().ToTypeSig()));
					yield return ca;
				}
			}
		}

		/// <summary>
		/// Gets custom attributes and pseudo custom attributes
		/// </summary>
		/// <param name="module">Module</param>
		/// <returns></returns>
		public static IEnumerable<CustomAttribute> GetCustomAttributes(this ModuleDef module) => module.CustomAttributes;

		/// <summary>
		/// Gets custom attributes and pseudo custom attributes
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns></returns>
		public static IEnumerable<CustomAttribute> GetCustomAttributes(this TypeDef type) {
			foreach (var ca in type.CustomAttributes)
				yield return ca;
			var module = type.Module;
			foreach (var ca in GetSecurityDeclarations(module, type))
				yield return ca;
			if (type.IsSerializable) {
				var declType = new TypeRefUser(module, systemName, serializableAttributeName, GetSystemRuntimeSerializationFormattersAssemblyRef(module));
				yield return new CustomAttribute(new MemberRefUser(module, ctorName, MethodSig.CreateInstance(module.CorLibTypes.Void), declType));
			}
			if (type.IsImport) {
				var declType = new TypeRefUser(module, systemRuntimeInteropServicesName, comImportAttributeName, GetSystemRuntimeInteropServicesAssemblyRef(module));
				yield return new CustomAttribute(new MemberRefUser(module, ctorName, MethodSig.CreateInstance(module.CorLibTypes.Void), declType));
			}
			{
				var layoutKind = LayoutKind.Auto;
				switch (type.Layout) {
				case TypeAttributes.SequentialLayout:
					layoutKind = LayoutKind.Sequential;
					break;
				case TypeAttributes.ExplicitLayout:
					layoutKind = LayoutKind.Explicit;
					break;
				}
				var charSet = CharSet.None;
				switch (type.StringFormat) {
				case TypeAttributes.AnsiClass:
					charSet = CharSet.Ansi;
					break;
				case TypeAttributes.AutoClass:
					charSet = CharSet.Auto;
					break;
				case TypeAttributes.UnicodeClass:
					charSet = CharSet.Unicode;
					break;
				}
				bool isValueType = type.IsValueType;
				var defaultLayoutKind = isValueType && !type.IsEnum ? LayoutKind.Sequential : LayoutKind.Auto;
				if (layoutKind != defaultLayoutKind || charSet != CharSet.Ansi || ShowClassLayout(type, isValueType)) {
					var declType = new TypeRefUser(module, systemRuntimeInteropServicesName, structLayoutAttributeName, module.CorLibTypes.AssemblyRef);
					var layoutKindType = new ValueTypeSig(new TypeRefUser(module, systemRuntimeInteropServicesName, layoutKindName, module.CorLibTypes.AssemblyRef));
					var ca = new CustomAttribute(new MemberRefUser(module, ctorName, MethodSig.CreateInstance(module.CorLibTypes.Void, layoutKindType), declType));
					ca.ConstructorArguments.Add(new CAArgument(layoutKindType, (int)layoutKind));
					if (charSet != CharSet.Ansi) {
						var charSetType = new ValueTypeSig(new TypeRefUser(module, systemRuntimeInteropServicesName, charSetName, module.CorLibTypes.AssemblyRef));
						ca.NamedArguments.Add(new CANamedArgument(isField: true, charSetType, charSetName, new CAArgument(charSetType, (int)charSet)));
					}
					if (type.PackingSize != ushort.MaxValue && type.PackingSize > 0)
						ca.NamedArguments.Add(new CANamedArgument(isField: true, module.CorLibTypes.Int32, packName, new CAArgument(module.CorLibTypes.Int32, (int)type.PackingSize)));
					if (type.ClassSize != uint.MaxValue && type.ClassSize > 0)
						ca.NamedArguments.Add(new CANamedArgument(isField: true, module.CorLibTypes.Int32, sizeName, new CAArgument(module.CorLibTypes.Int32, (int)type.ClassSize)));
					yield return ca;
				}
			}
		}

		static bool ShowClassLayout(TypeDef type, bool isValueType) {
			if (!isValueType)
				return type.HasClassLayout;
			if (type.HasClassLayout) {
				foreach (var field in type.Fields) {
					if (!field.IsStatic)
						return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Gets custom attributes and pseudo custom attributes
		/// </summary>
		/// <param name="field">Field</param>
		/// <returns></returns>
		public static IEnumerable<CustomAttribute> GetCustomAttributes(this FieldDef field) {
			foreach (var ca in field.CustomAttributes)
				yield return ca;
			var module = field.Module;
			if (field.IsNotSerialized) {
				var declType = new TypeRefUser(module, systemName, nonSerializedAttributeName, GetSystemRuntimeSerializationFormattersAssemblyRef(module));
				yield return new CustomAttribute(new MemberRefUser(module, ctorName, MethodSig.CreateInstance(module.CorLibTypes.Void), declType));
			}
			if (field.FieldOffset != null) {
				var declType = new TypeRefUser(module, systemRuntimeInteropServicesName, fieldOffsetAttributeName, module.CorLibTypes.AssemblyRef);
				var ca = new CustomAttribute(new MemberRefUser(module, ctorName, MethodSig.CreateInstance(module.CorLibTypes.Void, module.CorLibTypes.Int32), declType));
				ca.ConstructorArguments.Add(new CAArgument(module.CorLibTypes.Int32, (int)field.FieldOffset.Value));
				yield return ca;
			}
			if (field.MarshalType is MarshalType mt)
				yield return CreateMarshalTypeCustomAttribute(module, mt);
		}

		/// <summary>
		/// Gets custom attributes and pseudo custom attributes
		/// </summary>
		/// <param name="method">Method</param>
		/// <returns></returns>
		public static IEnumerable<CustomAttribute> GetCustomAttributes(this MethodDef method) {
			foreach (var ca in method.CustomAttributes)
				yield return ca;
			var module = method.Module;
			foreach (var ca in GetSecurityDeclarations(module, method))
				yield return ca;
			var implAttr = method.ImplAttributes & ~MethodImplAttributes.CodeTypeMask;
			if (method.ImplMap is ImplMap implMap) {
				var declType = new TypeRefUser(module, systemRuntimeInteropServicesName, dllImportAttributeName, GetSystemRuntimeInteropServicesAssemblyRef(module));
				var ca = new CustomAttribute(new MemberRefUser(module, ctorName, MethodSig.CreateInstance(module.CorLibTypes.Void, module.CorLibTypes.String), declType));
				ca.ConstructorArguments.Add(new CAArgument(module.CorLibTypes.String, implMap.Module?.Name ?? UTF8String.Empty));

				if (implMap.IsBestFitDisabled)
					ca.NamedArguments.Add(new CANamedArgument(isField: true, module.CorLibTypes.Boolean, bestFitMappingName, new CAArgument(module.CorLibTypes.Boolean, false)));
				else if (implMap.IsBestFitEnabled)
					ca.NamedArguments.Add(new CANamedArgument(isField: true, module.CorLibTypes.Boolean, bestFitMappingName, new CAArgument(module.CorLibTypes.Boolean, true)));

				System.Runtime.InteropServices.CallingConvention callingConvention;
				switch (implMap.CallConv) {
				case PInvokeAttributes.CallConvCdecl:
					callingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl;
					break;
				case PInvokeAttributes.CallConvFastcall:
					callingConvention = System.Runtime.InteropServices.CallingConvention.FastCall;
					break;
				case PInvokeAttributes.CallConvStdCall:
					callingConvention = System.Runtime.InteropServices.CallingConvention.StdCall;
					break;
				case PInvokeAttributes.CallConvThiscall:
					callingConvention = System.Runtime.InteropServices.CallingConvention.ThisCall;
					break;
				case PInvokeAttributes.CallConvWinapi:
					callingConvention = System.Runtime.InteropServices.CallingConvention.Winapi;
					break;
				default:
					callingConvention = 0;
					break;
				}
				if (callingConvention != System.Runtime.InteropServices.CallingConvention.Winapi) {
					var callingConventionType = new ValueTypeSig(new TypeRefUser(module, systemRuntimeInteropServicesName, callingConventionName, module.CorLibTypes.AssemblyRef));
					ca.NamedArguments.Add(new CANamedArgument(isField: true, callingConventionType, callingConventionName, new CAArgument(callingConventionType, (int)callingConvention)));
				}

				var charSet = CharSet.None;
				switch (implMap.Attributes & PInvokeAttributes.CharSetMask) {
				case PInvokeAttributes.CharSetAnsi:
					charSet = CharSet.Ansi;
					break;
				case PInvokeAttributes.CharSetAuto:
					charSet = CharSet.Auto;
					break;
				case PInvokeAttributes.CharSetUnicode:
					charSet = CharSet.Unicode;
					break;
				}
				if (charSet != CharSet.None) {
					var charSetType = new ValueTypeSig(new TypeRefUser(module, systemRuntimeInteropServicesName, charSetName, module.CorLibTypes.AssemblyRef));
					ca.NamedArguments.Add(new CANamedArgument(isField: true, charSetType, charSetName, new CAArgument(charSetType, (int)charSet)));
				}

				if (!UTF8String.IsNullOrEmpty(implMap.Name) && implMap.Name != method.Name)
					ca.NamedArguments.Add(new CANamedArgument(isField: true, module.CorLibTypes.String, entryPointName, new CAArgument(module.CorLibTypes.String, implMap.Name)));

				if (implMap.IsNoMangle)
					ca.NamedArguments.Add(new CANamedArgument(isField: true, module.CorLibTypes.Boolean, exactSpellingName, new CAArgument(module.CorLibTypes.Boolean, true)));

				if ((implAttr & MethodImplAttributes.PreserveSig) == 0)
					ca.NamedArguments.Add(new CANamedArgument(isField: true, module.CorLibTypes.Boolean, preserveSigName, new CAArgument(module.CorLibTypes.Boolean, false)));
				implAttr &= ~MethodImplAttributes.PreserveSig;

				if (implMap.SupportsLastError)
					ca.NamedArguments.Add(new CANamedArgument(isField: true, module.CorLibTypes.Boolean, setLastErrorName, new CAArgument(module.CorLibTypes.Boolean, true)));

				if (implMap.IsThrowOnUnmappableCharDisabled)
					ca.NamedArguments.Add(new CANamedArgument(isField: true, module.CorLibTypes.Boolean, throwOnUnmappableCharName, new CAArgument(module.CorLibTypes.Boolean, false)));
				else if (implMap.IsThrowOnUnmappableCharEnabled)
					ca.NamedArguments.Add(new CANamedArgument(isField: true, module.CorLibTypes.Boolean, throwOnUnmappableCharName, new CAArgument(module.CorLibTypes.Boolean, true)));
				yield return ca;
			}
			if (implAttr == MethodImplAttributes.PreserveSig) {
				implAttr = 0;
				var declType = new TypeRefUser(module, systemRuntimeInteropServicesName, preserveSigAttributeName, GetSystemRuntimeInteropServicesAssemblyRef(module));
				yield return new CustomAttribute(new MemberRefUser(module, ctorName, MethodSig.CreateInstance(module.CorLibTypes.Void), declType));
			}
			if (implAttr != 0) {
				var declType = new TypeRefUser(module, systemRuntimeCompilerServicesName, methodImplAttributeName, module.CorLibTypes.AssemblyRef);
				var enumDeclType = new ValueTypeSig(new TypeRefUser(module, systemRuntimeCompilerServicesName, methodImplOptionsName, module.CorLibTypes.AssemblyRef));
				var ca = new CustomAttribute(new MemberRefUser(module, ctorName, MethodSig.CreateInstance(module.CorLibTypes.Void, enumDeclType), declType));
				ca.ConstructorArguments.Add(new CAArgument(enumDeclType, (int)implAttr));
				yield return ca;
			}
		}

		static Parameter? GetParameter(MethodDef method, int sequence) {
			if (sequence == 0)
				return method.Parameters.ReturnParameter;
			int index = method.IsStatic ? -1 : 0;
			index += sequence;
			if ((uint)index < (uint)method.Parameters.Count)
				return method.Parameters[index];
			return null;
		}

		static bool HasIsReadOnlyAttribute(IHasCustomAttribute hca) =>
			Find(hca, systemRuntimeCompilerServicesName, isReadOnlyAttributeName) is not null;

		/// <summary>
		/// Gets custom attributes and pseudo custom attributes
		/// </summary>
		/// <param name="parameter">Parameter</param>
		/// <returns></returns>
		public static IEnumerable<CustomAttribute> GetCustomAttributes(this ParamDef parameter) {
			foreach (var ca in parameter.CustomAttributes)
				yield return ca;
			var method = parameter.DeclaringMethod;
			var module = method.Module;
			if (parameter.MarshalType is MarshalType mt)
				yield return CreateMarshalTypeCustomAttribute(module, mt);
			var p = GetParameter(method, parameter.Sequence);
			bool isByRefParam = p?.Type.RemovePinnedAndModifiers().GetElementType() == ElementType.ByRef;
			bool ignoreAttr = isByRefParam && ((!parameter.IsIn && parameter.IsOut)/*out*/ || HasIsReadOnlyAttribute(parameter)/*in*/);
			if (!ignoreAttr) {
				if (parameter.IsIn) {
					var declType = new TypeRefUser(module, systemRuntimeInteropServicesName, inAttributeName, GetSystemRuntimeInteropServicesAssemblyRef(module));
					yield return new CustomAttribute(new MemberRefUser(module, ctorName, MethodSig.CreateInstance(module.CorLibTypes.Void), declType));
				}
				if (parameter.IsOut) {
					var declType = new TypeRefUser(module, systemRuntimeInteropServicesName, outAttributeName, module.CorLibTypes.AssemblyRef);
					yield return new CustomAttribute(new MemberRefUser(module, ctorName, MethodSig.CreateInstance(module.CorLibTypes.Void), declType));
				}
			}
			if (parameter.IsOptional && parameter.Constant == null) {
				var declType = new TypeRefUser(module, systemRuntimeInteropServicesName, optionalAttributeName, GetSystemRuntimeInteropServicesAssemblyRef(module));
				yield return new CustomAttribute(new MemberRefUser(module, ctorName, MethodSig.CreateInstance(module.CorLibTypes.Void), declType));
			}
		}

		static CustomAttribute CreateMarshalTypeCustomAttribute(ModuleDef module, MarshalType mt) {
			var interopAsmRef = GetSystemRuntimeInteropServicesAssemblyRef(module);
			var declType = new TypeRefUser(module, systemRuntimeInteropServicesName, marshalAsAttributeName, interopAsmRef);
			var unmanagedTypeType = new ValueTypeSig(new TypeRefUser(module, systemRuntimeInteropServicesName, unmanagedTypeName, interopAsmRef));
			var ca = new CustomAttribute(new MemberRefUser(module, ctorName, MethodSig.CreateInstance(module.CorLibTypes.Void, unmanagedTypeType), declType));
			ca.ConstructorArguments.Add(new CAArgument(unmanagedTypeType, (int)mt.NativeType));

			if (mt is FixedArrayMarshalType fami) {
				if (fami.IsSizeValid)
					ca.NamedArguments.Add(new CANamedArgument(isField: true, module.CorLibTypes.Int32, sizeConstName, new CAArgument(module.CorLibTypes.Int32, fami.Size)));
				if (fami.IsElementTypeValid)
					ca.NamedArguments.Add(new CANamedArgument(isField: true, unmanagedTypeType, arraySubTypeName, new CAArgument(unmanagedTypeType, (int)fami.ElementType)));
			}
			if (mt is SafeArrayMarshalType sami) {
				if (sami.IsVariantTypeValid) {
					var varEnumType = new ValueTypeSig(new TypeRefUser(module, systemRuntimeInteropServicesName, varEnumName, interopAsmRef));
					ca.NamedArguments.Add(new CANamedArgument(isField: true, varEnumType, safeArraySubTypeName, new CAArgument(varEnumType, (int)sami.VariantType)));
				}
				if (sami.IsUserDefinedSubTypeValid) {
					var typeType = new ClassSig(new TypeRefUser(module, systemName, typeName, module.CorLibTypes.AssemblyRef));
					ca.NamedArguments.Add(new CANamedArgument(isField: true, typeType, safeArrayUserDefinedSubTypeName, new CAArgument(typeType, sami.UserDefinedSubType)));
				}
			}
			if (mt is ArrayMarshalType ami) {
				if (ami.IsElementTypeValid && ami.ElementType != NativeType.Max)
					ca.NamedArguments.Add(new CANamedArgument(isField: true, unmanagedTypeType, arraySubTypeName, new CAArgument(unmanagedTypeType, (int)ami.ElementType)));
				if (ami.IsSizeValid)
					ca.NamedArguments.Add(new CANamedArgument(isField: true, module.CorLibTypes.Int32, sizeConstName, new CAArgument(module.CorLibTypes.Int32, ami.Size)));
				if (ami.Flags != 0 && ami.ParamNumber >= 0)
					ca.NamedArguments.Add(new CANamedArgument(isField: true, module.CorLibTypes.Int16, sizeParamIndexName, new CAArgument(module.CorLibTypes.Int16, (short)ami.ParamNumber)));
			}
			if (mt is CustomMarshalType cmi) {
				if (cmi.CustomMarshaler != null) {
					var typeType = new ClassSig(new TypeRefUser(module, systemName, typeName, module.CorLibTypes.AssemblyRef));
					ca.NamedArguments.Add(new CANamedArgument(isField: true, typeType, marshalTypeRefName, new CAArgument(typeType, cmi.CustomMarshaler)));
				}
				if (!UTF8String.IsNullOrEmpty(cmi.Cookie))
					ca.NamedArguments.Add(new CANamedArgument(isField: true, module.CorLibTypes.String, marshalCookieName, new CAArgument(module.CorLibTypes.String, cmi.Cookie)));
			}
			if (mt is FixedSysStringMarshalType fssmi) {
				if (fssmi.IsSizeValid)
					ca.NamedArguments.Add(new CANamedArgument(isField: true, module.CorLibTypes.Int32, sizeConstName, new CAArgument(module.CorLibTypes.Int32, fssmi.Size)));
			}
			if (mt is InterfaceMarshalType imti) {
				if (imti.IsIidParamIndexValid)
					ca.NamedArguments.Add(new CANamedArgument(isField: true, module.CorLibTypes.Int32, iidParameterIndexName, new CAArgument(module.CorLibTypes.Int32, imti.IidParamIndex)));
			}
			return ca;
		}

		static CustomAttribute? Find(IHasCustomAttribute hca, UTF8String @namespace, UTF8String name) {
			var cas = hca.CustomAttributes;
			for (int i = 0; i < cas.Count; i++) {
				var ca = cas[i];
				var type = ca.AttributeType;
				if (type.Name != name || type.Namespace != @namespace)
					continue;
				if (type.DeclaringType is not null)
					continue;
				return ca;
			}
			return null;
		}

		/// <summary>
		/// Gets custom attributes and pseudo custom attributes
		/// </summary>
		/// <param name="property">Property</param>
		/// <returns></returns>
		public static IEnumerable<CustomAttribute> GetCustomAttributes(this PropertyDef property) {
			foreach (var ca in property.CustomAttributes)
				yield return ca;
			var defMemCa = Find(property.DeclaringType, systemReflectionName, defaultMemberAttributeName);
			if (defMemCa is not null && defMemCa.ConstructorArguments.Count > 0 &&
				defMemCa.ConstructorArguments[0].Value is UTF8String defMember &&
				defMember != itemName && defMember == property.Name) {
				var module = property.Module;
				var declType = new TypeRefUser(module, systemRuntimeCompilerServicesName, indexerNameAttributeName, module.CorLibTypes.AssemblyRef);
				var newCa = new CustomAttribute(new MemberRefUser(module, ctorName, MethodSig.CreateInstance(module.CorLibTypes.Void), declType));
				newCa.ConstructorArguments.Add(new CAArgument(module.CorLibTypes.String, defMember));
				yield return newCa;
			}
		}

		static IResolutionScope GetSystemRuntimeInteropServicesAssemblyRef(ModuleDef module) {
			foreach (var asmRef in module.GetAssemblyRefs()) {
				if (asmRef.Name == systemRuntimeInteropServicesName && contractsPublicKeyToken.Equals(asmRef.PublicKeyOrToken.Token))
					return asmRef;
			}
			return module.CorLibTypes.AssemblyRef;
		}

		static AssemblyRef GetSystemRuntimeSerializationFormattersAssemblyRef(ModuleDef module) {
			foreach (var asmRef in module.GetAssemblyRefs()) {
				if (asmRef.Name == systemRuntimeSerializationFormattersName && contractsPublicKeyToken.Equals(asmRef.PublicKeyOrToken.Token))
					return asmRef;
			}
			return module.CorLibTypes.AssemblyRef;
		}

		static readonly UTF8String ctorName = new UTF8String(".ctor");
		static readonly UTF8String systemRuntimeInteropServicesName = new UTF8String("System.Runtime.InteropServices");
		static readonly UTF8String systemRuntimeSerializationFormattersName = new UTF8String("System.Runtime.Serialization.Formatters");
		static readonly UTF8String systemRuntimeCompilerServicesName = new UTF8String("System.Runtime.CompilerServices");
		static readonly UTF8String systemName = new UTF8String("System");
		static readonly UTF8String systemReflectionName = new UTF8String("System.Reflection");
		static readonly UTF8String systemConfigurationAssembliesName = new UTF8String("System.Configuration.Assemblies");
		static readonly UTF8String systemSecurityPermissionsName = new UTF8String("System.Security.Permissions");
		static readonly UTF8String inAttributeName = new UTF8String("InAttribute");
		static readonly UTF8String outAttributeName = new UTF8String("OutAttribute");
		static readonly UTF8String optionalAttributeName = new UTF8String("OptionalAttribute");
		static readonly UTF8String methodImplAttributeName = new UTF8String("MethodImplAttribute");
		static readonly UTF8String methodImplOptionsName = new UTF8String("MethodImplOptions");
		static readonly UTF8String preserveSigAttributeName = new UTF8String("PreserveSigAttribute");
		static readonly UTF8String nonSerializedAttributeName = new UTF8String("NonSerializedAttribute");
		static readonly UTF8String serializableAttributeName = new UTF8String("SerializableAttribute");
		static readonly UTF8String comImportAttributeName = new UTF8String("ComImportAttribute");
		static readonly UTF8String assemblyAlgorithmIdAttributeName = new UTF8String("AssemblyAlgorithmIdAttribute");
		static readonly UTF8String assemblyHashAlgorithmName = new UTF8String("AssemblyHashAlgorithm");
		static readonly UTF8String assemblyCultureAttributeName = new UTF8String("AssemblyCultureAttribute");
		static readonly UTF8String assemblyFlagsAttributeName = new UTF8String("AssemblyFlagsAttribute");
		static readonly UTF8String assemblyNameFlagsName = new UTF8String("AssemblyNameFlags");
		static readonly UTF8String assemblyVersionAttributeName = new UTF8String("AssemblyVersionAttribute");
		static readonly UTF8String fieldOffsetAttributeName = new UTF8String("FieldOffsetAttribute");
		static readonly UTF8String structLayoutAttributeName = new UTF8String("StructLayoutAttribute");
		static readonly UTF8String layoutKindName = new UTF8String("LayoutKind");
		static readonly UTF8String charSetName = new UTF8String("CharSet");
		static readonly UTF8String packName = new UTF8String("Pack");
		static readonly UTF8String sizeName = new UTF8String("Size");
		static readonly UTF8String dllImportAttributeName = new UTF8String("DllImportAttribute");
		static readonly UTF8String bestFitMappingName = new UTF8String("BestFitMapping");
		static readonly UTF8String callingConventionName = new UTF8String("CallingConvention");
		static readonly UTF8String entryPointName = new UTF8String("EntryPoint");
		static readonly UTF8String exactSpellingName = new UTF8String("ExactSpelling");
		static readonly UTF8String preserveSigName = new UTF8String("PreserveSig");
		static readonly UTF8String setLastErrorName = new UTF8String("SetLastError");
		static readonly UTF8String throwOnUnmappableCharName = new UTF8String("ThrowOnUnmappableChar");
		static readonly UTF8String attributeName = new UTF8String("Attribute");
		static readonly UTF8String marshalAsAttributeName = new UTF8String("MarshalAsAttribute");
		static readonly UTF8String unmanagedTypeName = new UTF8String("UnmanagedType");
		static readonly UTF8String sizeConstName = new UTF8String("SizeConst");
		static readonly UTF8String arraySubTypeName = new UTF8String("ArraySubType");
		static readonly UTF8String varEnumName = new UTF8String("VarEnum");
		static readonly UTF8String safeArraySubTypeName = new UTF8String("SafeArraySubType");
		static readonly UTF8String typeName = new UTF8String("Type");
		static readonly UTF8String safeArrayUserDefinedSubTypeName = new UTF8String("SafeArrayUserDefinedSubType");
		static readonly UTF8String sizeParamIndexName = new UTF8String("SizeParamIndex");
		static readonly UTF8String marshalTypeRefName = new UTF8String("MarshalTypeRef");
		static readonly UTF8String marshalCookieName = new UTF8String("MarshalCookie");
		static readonly UTF8String iidParameterIndexName = new UTF8String("IidParameterIndex");
		static readonly UTF8String typeForwardedToAttributeName = new UTF8String("TypeForwardedToAttribute");
		static readonly UTF8String securityActionName = new UTF8String("SecurityAction");
		static readonly UTF8String securityAttributeName = new UTF8String("SecurityAttribute");
		static readonly UTF8String indexerNameAttributeName = new UTF8String("IndexerNameAttribute");
		static readonly UTF8String itemName = new UTF8String("Item");
		static readonly UTF8String defaultMemberAttributeName = new UTF8String("DefaultMemberAttribute");
		static readonly UTF8String isReadOnlyAttributeName = new UTF8String("IsReadOnlyAttribute");
		static readonly PublicKeyToken contractsPublicKeyToken = new PublicKeyToken("b03f5f7f11d50a3a");

		static readonly (UTF8String @namespace, UTF8String name)[] pseudoCANames = new (UTF8String, UTF8String)[] {
			(systemName, nonSerializedAttributeName),
			(systemName, serializableAttributeName),
			(systemReflectionName, assemblyAlgorithmIdAttributeName),
			(systemReflectionName, assemblyCultureAttributeName),
			(systemReflectionName, assemblyFlagsAttributeName),
			(systemReflectionName, assemblyVersionAttributeName),
			(systemRuntimeCompilerServicesName, methodImplAttributeName),
			(systemRuntimeCompilerServicesName, typeForwardedToAttributeName),
			(systemRuntimeInteropServicesName, comImportAttributeName),
			(systemRuntimeInteropServicesName, dllImportAttributeName),
			(systemRuntimeInteropServicesName, fieldOffsetAttributeName),
			(systemRuntimeInteropServicesName, inAttributeName),
			(systemRuntimeInteropServicesName, marshalAsAttributeName),
			(systemRuntimeInteropServicesName, optionalAttributeName),
			(systemRuntimeInteropServicesName, outAttributeName),
			(systemRuntimeInteropServicesName, preserveSigAttributeName),
			(systemRuntimeInteropServicesName, structLayoutAttributeName),
			(systemRuntimeCompilerServicesName, indexerNameAttributeName),
		};
		static readonly (UTF8String @namespace, UTF8String name)[] pseudoCAOtherTypeNames = new (UTF8String, UTF8String)[] {
			(systemConfigurationAssembliesName, assemblyHashAlgorithmName),
			(systemReflectionName, assemblyNameFlagsName),
			(systemRuntimeCompilerServicesName, methodImplOptionsName),
			(systemRuntimeInteropServicesName, callingConventionName),
			(systemRuntimeInteropServicesName, charSetName),
			(systemRuntimeInteropServicesName, layoutKindName),
			(systemRuntimeInteropServicesName, unmanagedTypeName),
			(systemRuntimeInteropServicesName, varEnumName),
			(systemName, typeName),
		};
	}
}
