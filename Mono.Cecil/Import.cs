//
// Import.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2010 Jb Evain
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using SR = System.Reflection;

using Mono.Cecil.Metadata;

namespace Mono.Cecil {

	enum ImportGenericKind {
		Definition,
		Open,
	}

	class MetadataImporter {

		readonly ModuleDefinition module;

		public MetadataImporter (ModuleDefinition module)
		{
			this.module = module;
		}

#if !CF
		static readonly Dictionary<Type, ElementType> type_etype_mapping = new Dictionary<Type, ElementType> (18) {
			{ typeof (void), ElementType.Void },
			{ typeof (bool), ElementType.Boolean },
			{ typeof (char), ElementType.Char },
			{ typeof (sbyte), ElementType.I1 },
			{ typeof (byte), ElementType.U1 },
			{ typeof (short), ElementType.I2 },
			{ typeof (ushort), ElementType.U2 },
			{ typeof (int), ElementType.I4 },
			{ typeof (uint), ElementType.U4 },
			{ typeof (long), ElementType.I8 },
			{ typeof (ulong), ElementType.U8 },
			{ typeof (float), ElementType.R4 },
			{ typeof (double), ElementType.R8 },
			{ typeof (string), ElementType.String },
			{ typeof (TypedReference), ElementType.TypedByRef },
			{ typeof (IntPtr), ElementType.I },
			{ typeof (UIntPtr), ElementType.U },
			{ typeof (object), ElementType.Object },
		};

		public TypeReference ImportType (Type type, IGenericContext context)
		{
			return ImportType (type, context, ImportGenericKind.Open);
		}

		public TypeReference ImportType (Type type, IGenericContext context, ImportGenericKind import_kind)
		{
			if (IsTypeSpecification (type) || ImportOpenGenericType (type, import_kind))
				return ImportTypeSpecification (type, context);

			var reference = new TypeReference (
				string.Empty,
				type.Name,
				module,
				ImportScope (type.Assembly),
				type.IsValueType);

			reference.etype = ImportElementType (type);

			if (IsNestedType (type))
				reference.DeclaringType = ImportType (type.DeclaringType, context, import_kind);
			else
				reference.Namespace = type.Namespace;

			if (type.IsGenericType)
				ImportGenericParameters (reference, type.GetGenericArguments ());

			return reference;
		}

		static bool ImportOpenGenericType (Type type, ImportGenericKind import_kind)
		{
			return type.IsGenericType && type.IsGenericTypeDefinition && import_kind == ImportGenericKind.Open;
		}

		static bool ImportOpenGenericMethod (SR.MethodBase method, ImportGenericKind import_kind)
		{
			return method.IsGenericMethod && method.IsGenericMethodDefinition && import_kind == ImportGenericKind.Open;
		}

		static bool IsNestedType (Type type)
		{
#if !SILVERLIGHT
			return type.IsNested;
#else
			return type.DeclaringType != null;
#endif
		}

		TypeReference ImportTypeSpecification (Type type, IGenericContext context)
		{
			if (type.IsByRef)
				return new ByReferenceType (ImportType (type.GetElementType (), context));

			if (type.IsPointer)
				return new PointerType (ImportType (type.GetElementType (), context));

			if (type.IsArray)
				return new ArrayType (ImportType (type.GetElementType (), context), type.GetArrayRank ());

			if (type.IsGenericType)
				return ImportGenericInstance (type, context);

			if (type.IsGenericParameter)
				return ImportGenericParameter (type, context);

			throw new NotSupportedException (type.FullName);
		}

		static TypeReference ImportGenericParameter (Type type, IGenericContext context)
		{
			if (context == null)
				throw new InvalidOperationException ();

			var owner = type.DeclaringMethod != null
				? context.Method
				: context.Type;

			if (owner == null)
				throw new InvalidOperationException ();

			return owner.GenericParameters [type.GenericParameterPosition];
		}

		TypeReference ImportGenericInstance (Type type, IGenericContext context)
		{
			var element_type = ImportType (type.GetGenericTypeDefinition (), context, ImportGenericKind.Definition);
			var instance = new GenericInstanceType (element_type);
			var arguments = type.GetGenericArguments ();
			var instance_arguments = instance.GenericArguments;

			for (int i = 0; i < arguments.Length; i++)
				instance_arguments.Add (ImportType (arguments [i], context ?? element_type));

			return instance;
		}

		static bool IsTypeSpecification (Type type)
		{
			return type.HasElementType
				|| IsGenericInstance (type)
				|| type.IsGenericParameter;
		}

		static bool IsGenericInstance (Type type)
		{
			return type.IsGenericType && !type.IsGenericTypeDefinition;
		}

		static ElementType ImportElementType (Type type)
		{
			ElementType etype;
			if (!type_etype_mapping.TryGetValue (type, out etype))
				return ElementType.None;

			return etype;
		}

		AssemblyNameReference ImportScope (SR.Assembly assembly)
		{
			AssemblyNameReference scope;
#if !SILVERLIGHT
			var name = assembly.GetName ();

			if (TryGetAssemblyNameReference (name, out scope))
				return scope;

			scope = new AssemblyNameReference (name.Name, name.Version) {
				Culture = name.CultureInfo.Name,
				PublicKeyToken = name.GetPublicKeyToken (),
				HashAlgorithm = (AssemblyHashAlgorithm) name.HashAlgorithm,
			};

			module.AssemblyReferences.Add (scope);

			return scope;
#else
			var name = AssemblyNameReference.Parse (assembly.FullName);

			if (TryGetAssemblyNameReference (name, out scope))
				return scope;

			module.AssemblyReferences.Add (name);

			return name;
#endif
		}

#if !SILVERLIGHT
		bool TryGetAssemblyNameReference (SR.AssemblyName name, out AssemblyNameReference assembly_reference)
		{
			var references = module.AssemblyReferences;

			for (int i = 0; i < references.Count; i++) {
				var reference = references [i];
				if (name.FullName != reference.FullName) // TODO compare field by field
					continue;

				assembly_reference = reference;
				return true;
			}

			assembly_reference = null;
			return false;
		}
#endif

		public FieldReference ImportField (SR.FieldInfo field, IGenericContext context)
		{
			var declaring_type = ImportType (field.DeclaringType, context);

			if (IsGenericInstance (field.DeclaringType))
				field = ResolveFieldDefinition (field);

			return new FieldReference {
				Name = field.Name,
				DeclaringType = declaring_type,
				FieldType = ImportType (field.FieldType, context ?? declaring_type),
			};
		}

		static SR.FieldInfo ResolveFieldDefinition (SR.FieldInfo field)
		{
#if !SILVERLIGHT
			return field.Module.ResolveField (field.MetadataToken);
#else
			return field.DeclaringType.GetGenericTypeDefinition ().GetField (field.Name,
				SR.BindingFlags.Public
				| SR.BindingFlags.NonPublic
				| (field.IsStatic ? SR.BindingFlags.Static : SR.BindingFlags.Instance));
#endif
		}

		public MethodReference ImportMethod (SR.MethodBase method, IGenericContext context, ImportGenericKind import_kind)
		{
			if (IsMethodSpecification (method) || ImportOpenGenericMethod (method, import_kind))
				return ImportMethodSpecification (method, context);

			var declaring_type = ImportType (method.DeclaringType, context);

			if (IsGenericInstance (method.DeclaringType))
				method = method.Module.ResolveMethod (method.MetadataToken);

			var reference = new MethodReference {
				Name = method.Name,
				HasThis = HasCallingConvention (method, SR.CallingConventions.HasThis),
				ExplicitThis = HasCallingConvention (method, SR.CallingConventions.ExplicitThis),
				DeclaringType = ImportType (method.DeclaringType, context, ImportGenericKind.Definition),
			};

			if (HasCallingConvention (method, SR.CallingConventions.VarArgs))
				reference.CallingConvention &= MethodCallingConvention.VarArg;

			if (method.IsGenericMethod)
				ImportGenericParameters (reference, method.GetGenericArguments ());

			var method_info = method as SR.MethodInfo;
			reference.ReturnType = method_info != null
				? ImportType (method_info.ReturnType, context ?? reference)
				: ImportType (typeof (void), null);

			var parameters = method.GetParameters ();
			var reference_parameters = reference.Parameters;

			for (int i = 0; i < parameters.Length; i++)
				reference_parameters.Add (
					new ParameterDefinition (ImportType (parameters [i].ParameterType, context ?? reference)));

			reference.DeclaringType = declaring_type;

			return reference;
		}

		static void ImportGenericParameters (IGenericParameterProvider provider, Type [] arguments)
		{
			var provider_parameters = provider.GenericParameters;

			for (int i = 0; i < arguments.Length; i++)
				provider_parameters.Add (new GenericParameter (arguments [i].Name, provider));
		}

		static bool IsMethodSpecification (SR.MethodBase method)
		{
			return method.IsGenericMethod && !method.IsGenericMethodDefinition;
		}

		MethodReference ImportMethodSpecification (SR.MethodBase method, IGenericContext context)
		{
			var method_info = method as SR.MethodInfo;
			if (method_info == null)
				throw new InvalidOperationException ();

			var element_method = ImportMethod (method_info.GetGenericMethodDefinition (), context, ImportGenericKind.Definition);
			var instance = new GenericInstanceMethod (element_method);
			var arguments = method.GetGenericArguments ();
			var instance_arguments = instance.GenericArguments;

			for (int i = 0; i < arguments.Length; i++)
				instance_arguments.Add (ImportType (arguments [i], context ?? element_method));

			return instance;
		}

		static bool HasCallingConvention (SR.MethodBase method, SR.CallingConventions conventions)
		{
			return (method.CallingConvention & conventions) != 0;
		}
#endif

		public TypeReference ImportType (TypeReference type, IGenericContext context)
		{
			if (type.IsTypeSpecification ())
				return ImportTypeSpecification (type, context);

			var reference = new TypeReference (
				type.Namespace,
				type.Name,
				module,
				ImportScope (type.Scope),
				type.IsValueType);

			MetadataSystem.TryProcessPrimitiveType (reference);

			if (type.IsNested)
				reference.DeclaringType = ImportType (type.DeclaringType, context);

			if (type.HasGenericParameters)
				ImportGenericParameters (reference, type);

			return reference;
		}

		IMetadataScope ImportScope (IMetadataScope scope)
		{
			switch (scope.MetadataScopeType) {
			case MetadataScopeType.AssemblyNameReference:
				return ImportAssemblyName ((AssemblyNameReference) scope);
			case MetadataScopeType.ModuleDefinition:
				return ImportAssemblyName (((ModuleDefinition) scope).Assembly.Name);
			case MetadataScopeType.ModuleReference:
				throw new NotImplementedException ();
			}

			throw new NotSupportedException ();
		}

		AssemblyNameReference ImportAssemblyName (AssemblyNameReference name)
		{
			AssemblyNameReference reference;
			if (TryGetAssemblyNameReference (name, out reference))
				return reference;

			reference = new AssemblyNameReference (name.Name, name.Version) {
				Culture = name.Culture,
				HashAlgorithm = name.HashAlgorithm,
			};

			var pk_token = !name.PublicKeyToken.IsNullOrEmpty ()
				? new byte [name.PublicKeyToken.Length]
				: Empty<byte>.Array;

			if (pk_token.Length > 0)
				Buffer.BlockCopy (name.PublicKeyToken, 0, pk_token, 0, pk_token.Length);

			reference.PublicKeyToken = pk_token;

			module.AssemblyReferences.Add (reference);

			return reference;
		}

		bool TryGetAssemblyNameReference (AssemblyNameReference name_reference, out AssemblyNameReference assembly_reference)
		{
			var references = module.AssemblyReferences;

			for (int i = 0; i < references.Count; i++) {
				var reference = references [i];
				if (name_reference.FullName != reference.FullName) // TODO compare field by field
					continue;

				assembly_reference = reference;
				return true;
			}

			assembly_reference = null;
			return false;
		}

		static void ImportGenericParameters (IGenericParameterProvider imported, IGenericParameterProvider original)
		{
			var parameters = original.GenericParameters;
			var imported_parameters = imported.GenericParameters;

			for (int i = 0; i < parameters.Count; i++)
				imported_parameters.Add (new GenericParameter (parameters [i].Name, imported));
		}

		TypeReference ImportTypeSpecification (TypeReference type, IGenericContext context)
		{
			switch (type.etype) {
			case ElementType.SzArray:
				var vector = (ArrayType) type;
				return new ArrayType (ImportType (vector.ElementType, context));
			case ElementType.Ptr:
				var pointer = (PointerType) type;
				return new PointerType (ImportType (pointer.ElementType, context));
			case ElementType.ByRef:
				var byref = (ByReferenceType) type;
				return new ByReferenceType (ImportType (byref.ElementType, context));
			case ElementType.Pinned:
				var pinned = (PinnedType) type;
				return new PinnedType (ImportType (pinned.ElementType, context));
			case ElementType.Sentinel:
				var sentinel = (SentinelType) type;
				return new SentinelType (ImportType (sentinel.ElementType, context));
			case ElementType.CModOpt:
				var modopt = (OptionalModifierType) type;
				return new OptionalModifierType (
					ImportType (modopt.ModifierType, context),
					ImportType (modopt.ElementType, context));
			case ElementType.CModReqD:
				var modreq = (RequiredModifierType) type;
				return new RequiredModifierType (
					ImportType (modreq.ModifierType, context),
					ImportType (modreq.ElementType, context));
			case ElementType.Array:
				var array = (ArrayType) type;
				var imported_array = new ArrayType (ImportType (array.ElementType, context));
				if (array.IsVector)
					return imported_array;

				var dimensions = array.Dimensions;
				var imported_dimensions = imported_array.Dimensions;

				imported_dimensions.Clear ();

				for (int i = 0; i < dimensions.Count; i++) {
					var dimension = dimensions [i];

					imported_dimensions.Add (new ArrayDimension (dimension.LowerBound, dimension.UpperBound));
				}

				return imported_array;
			case ElementType.GenericInst:
				var instance = (GenericInstanceType) type;
				var element_type = ImportType (instance.ElementType, context);
				var imported_instance = new GenericInstanceType (element_type);

				var arguments = instance.GenericArguments;
				var imported_arguments = imported_instance.GenericArguments;

				for (int i = 0; i < arguments.Count; i++)
					imported_arguments.Add (ImportType (arguments [i], context));

				return imported_instance;
			case ElementType.Var:
				if (context == null || context.Type == null)
					throw new InvalidOperationException ();

				return ((TypeReference) context.Type).GetElementType ().GenericParameters [((GenericParameter) type).Position];
			case ElementType.MVar:
				if (context == null || context.Method == null)
					throw new InvalidOperationException ();

				return context.Method.GenericParameters [((GenericParameter) type).Position];
			}

			throw new NotSupportedException (type.etype.ToString ());
		}

		public FieldReference ImportField (FieldReference field, IGenericContext context)
		{
			var declaring_type = ImportType (field.DeclaringType, context);

			return new FieldReference {
				Name = field.Name,
				DeclaringType = declaring_type,
				FieldType = ImportType (field.FieldType, context ?? declaring_type),
			};
		}

		public MethodReference ImportMethod (MethodReference method, IGenericContext context)
		{
			if (method.IsGenericInstance)
				return ImportMethodSpecification (method, context);

			var declaring_type = ImportType (method.DeclaringType, context);

			var reference = new MethodReference {
				Name = method.Name,
				HasThis = method.HasThis,
				ExplicitThis = method.ExplicitThis,
				DeclaringType = declaring_type,
			};

			reference.CallingConvention = method.CallingConvention;

			if (method.HasGenericParameters)
				ImportGenericParameters (reference, method);

			reference.ReturnType = ImportType (method.ReturnType, context ?? reference);

			if (!method.HasParameters)
				return reference;

			var reference_parameters = reference.Parameters;

			var parameters = method.Parameters;
			for (int i = 0; i < parameters.Count; i++)
				reference_parameters.Add (
					new ParameterDefinition (ImportType (parameters [i].ParameterType, context ?? reference)));

			return reference;
		}

		MethodSpecification ImportMethodSpecification (MethodReference method, IGenericContext context)
		{
			if (!method.IsGenericInstance)
				throw new NotSupportedException ();

			var instance = (GenericInstanceMethod) method;
			var element_method = ImportMethod (instance.ElementMethod, context);
			var imported_instance = new GenericInstanceMethod (element_method);

			var arguments = instance.GenericArguments;
			var imported_arguments = imported_instance.GenericArguments;

			for (int i = 0; i < arguments.Count; i++)
				imported_arguments.Add (ImportType (arguments [i], context));

			return imported_instance;
		}
	}
}
