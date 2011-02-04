//
// MetadataResolver.cs
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

using Mono.Collections.Generic;

namespace Mono.Cecil {

	public interface IAssemblyResolver {
		AssemblyDefinition Resolve (AssemblyNameReference name);
		AssemblyDefinition Resolve (string fullName);
	}

	static class MetadataResolver {

		public static TypeDefinition Resolve (IAssemblyResolver resolver, TypeReference type)
		{
			type = type.GetElementType ();

			var scope = type.Scope;
			switch (scope.MetadataScopeType) {
			case MetadataScopeType.AssemblyNameReference:
				var assembly = resolver.Resolve ((AssemblyNameReference) scope);
				if (assembly == null)
					return null;

				return GetType (assembly.MainModule, type);
			case MetadataScopeType.ModuleDefinition:
				return GetType ((ModuleDefinition) scope, type);
			case MetadataScopeType.ModuleReference:
				var modules = type.Module.Assembly.Modules;
				var module_ref = (ModuleReference) scope;
				for (int i = 0; i < modules.Count; i++) {
					var netmodule = modules [i];
					if (netmodule.Name == module_ref.Name)
						return GetType (netmodule, type);
				}
				break;
			}

			throw new NotSupportedException ();
		}

		static TypeDefinition GetType (ModuleDefinition module, TypeReference type)
		{
			if (!type.IsNested)
				return module.GetType (type.Namespace, type.Name);

			var declaring_type = type.DeclaringType.Resolve ();
			if (declaring_type == null)
				return null;

			return declaring_type.GetNestedType (type.Name);
		}

		public static FieldDefinition Resolve (IAssemblyResolver resolver, FieldReference field)
		{
			var type = Resolve (resolver, field.DeclaringType);
			if (type == null)
				return null;

			if (!type.HasFields)
				return null;

			return GetField (resolver, type, field);
		}

		static FieldDefinition GetField (IAssemblyResolver resolver, TypeDefinition type, FieldReference reference)
		{
			while (type != null) {
				var field = GetField (type.Fields, reference);
				if (field != null)
					return field;

				if (type.BaseType == null)
					return null;

				type = Resolve (resolver, type.BaseType);
			}

			return null;
		}

		static FieldDefinition GetField (IList<FieldDefinition> fields, FieldReference reference)
		{
			for (int i = 0; i < fields.Count; i++) {
				var field = fields [i];

				if (field.Name != reference.Name)
					continue;

				if (!AreSame (field.FieldType, reference.FieldType))
					continue;

				return field;
			}

			return null;
		}

		public static MethodDefinition Resolve (IAssemblyResolver resolver, MethodReference method)
		{
			var type = Resolve (resolver, method.DeclaringType);
			if (type == null)
				return null;

			method = method.GetElementMethod ();

			if (!type.HasMethods)
				return null;

			return GetMethod (resolver, type, method);
		}

		static MethodDefinition GetMethod (IAssemblyResolver resolver, TypeDefinition type, MethodReference reference)
		{
			while (type != null) {
				var method = GetMethod (type.Methods, reference);
				if (method != null)
					return method;

				if (type.BaseType == null)
					return null;

				type = Resolve (resolver, type.BaseType);
			}

			return null;
		}

		public static MethodDefinition GetMethod (IList<MethodDefinition> methods, MethodReference reference)
		{
			for (int i = 0; i < methods.Count; i++) {
				var method = methods [i];

				if (method.Name != reference.Name)
					continue;

				if (!AreSame (method.ReturnType, reference.ReturnType))
					continue;

				if (method.HasParameters != reference.HasParameters)
					continue;

				if (!method.HasParameters && !reference.HasParameters)
					return method;

				if (!AreSame (method.Parameters, reference.Parameters))
					continue;

				return method;
			}

			return null;
		}

		static bool AreSame (Collection<ParameterDefinition> a, Collection<ParameterDefinition> b)
		{
			var count = a.Count;

			if (count != b.Count)
				return false;

			if (count == 0)
				return true;

			for (int i = 0; i < count; i++)
				if (!AreSame (a [i].ParameterType, b [i].ParameterType))
					return false;

			return true;
		}

		static bool AreSame (TypeSpecification a, TypeSpecification b)
		{
			if (!AreSame (a.ElementType, b.ElementType))
				return false;

			if (a.IsGenericInstance)
				return AreSame ((GenericInstanceType) a, (GenericInstanceType) b);

			if (a.IsRequiredModifier || a.IsOptionalModifier)
				return AreSame ((IModifierType) a, (IModifierType) b);

			if (a.IsArray)
				return AreSame ((ArrayType) a, (ArrayType) b);

			return true;
		}

		static bool AreSame (ArrayType a, ArrayType b)
		{
			if (a.Rank != b.Rank)
				return false;

			// TODO: dimensions

			return true;
		}

		static bool AreSame (IModifierType a, IModifierType b)
		{
			return AreSame (a.ModifierType, b.ModifierType);
		}

		static bool AreSame (GenericInstanceType a, GenericInstanceType b)
		{
			if (!a.HasGenericArguments)
				return !b.HasGenericArguments;

			if (!b.HasGenericArguments)
				return false;

			if (a.GenericArguments.Count != b.GenericArguments.Count)
				return false;

			for (int i = 0; i < a.GenericArguments.Count; i++)
				if (!AreSame (a.GenericArguments [i], b.GenericArguments [i]))
					return false;

			return true;
		}

		static bool AreSame (GenericParameter a, GenericParameter b)
		{
			return a.Position == b.Position;
		}

		static bool AreSame (TypeReference a, TypeReference b)
		{
			if (a.etype != b.etype)
				return false;

			if (a.IsGenericParameter)
				return AreSame ((GenericParameter) a, (GenericParameter) b);

			if (a.IsTypeSpecification ())
				return AreSame ((TypeSpecification) a, (TypeSpecification) b);

			return a.FullName == b.FullName;
		}
	}
}
