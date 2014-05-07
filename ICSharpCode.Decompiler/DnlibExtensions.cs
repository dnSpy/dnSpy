// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace ICSharpCode.Decompiler
{
	/// <summary>
	/// dnlib helper methods.
	/// </summary>
	public static class DnlibExtensions
	{
		#region GetPushDelta / GetPopDelta
		public static int GetPushDelta(this Instruction instruction)
		{
			int pushes, pops;
			instruction.CalculateStackUsage(out pushes, out pops);
			return pushes;
		}
		
		public static int? GetPopDelta(this Instruction instruction, MethodDef methodDef)
		{
			int pushes, pops;
			instruction.CalculateStackUsage(methodDef.ReturnType.ElementType != ElementType.Void, out pushes, out pops);
			return pops == -1 ? (int?)null : pops;
		}

		public static bool IsVoid(this ITypeDefOrRef type)
		{
			if (type is TypeSpec)
				return IsVoid(((TypeSpec)type).TypeSig);
			return type.DefinitionAssembly.IsCorLib() && type.Namespace == "System" && type.Name == "Void";
		}
		
		public static bool IsVoid(this TypeSig type)
		{
			return type.GetElementType() == ElementType.Void;
		}

		public static bool IsValueTypeOrVoid(this ITypeDefOrRef type)
		{
			if (type is TypeSpec)
				return IsValueTypeOrVoid(((TypeSpec)type).TypeSig);
			return type.IsValueType || IsVoid(type);
		}

		public static bool IsValueTypeOrVoid(this TypeSig type)
		{
			var elemType = type.GetElementType();
			return elemType == ElementType.Void || elemType == ElementType.ValueType;
		}
		#endregion
		
		/// <summary>
		/// Gets the (exclusive) end offset of this instruction.
		/// </summary>
		public static int GetEndOffset(this Instruction inst)
		{
			if (inst == null)
				throw new ArgumentNullException("inst");
			return (int)inst.Offset + inst.GetSize();
		}
		
		public static string OffsetToString(uint offset)
		{
			return string.Format("IL_{0:x4}", offset);
		}
		
		public static HashSet<MethodDef> GetAccessorMethods(this TypeDef type)
		{
			HashSet<MethodDef> accessorMethods = new HashSet<MethodDef>();
			foreach (var property in type.Properties) {
				accessorMethods.Add(property.GetMethod);
				accessorMethods.Add(property.SetMethod);
				if (property.HasOtherMethods) {
					foreach (var m in property.OtherMethods)
						accessorMethods.Add(m);
				}
			}
			foreach (EventDef ev in type.Events) {
				accessorMethods.Add(ev.AddMethod);
				accessorMethods.Add(ev.RemoveMethod);
				accessorMethods.Add(ev.InvokeMethod);
				if (ev.HasOtherMethods) {
					foreach (var m in ev.OtherMethods)
						accessorMethods.Add(m);
				}
			}
			return accessorMethods;
		}
		
		public static TypeDef ResolveWithinSameModule(this ITypeDefOrRef type)
		{
			if (type != null && type.Scope == type.Module)
				return type.ResolveTypeDef();
			else
				return null;
		}
		
		public static FieldDef ResolveFieldWithinSameModule(this MemberRef field)
		{
			if (field != null && field.DeclaringType.Scope == field.Module)
				return field.ResolveField();
			else
				return null;
		}
		
		public static FieldDef ResolveFieldWithinSameModule(this IField field)
		{
			if (field != null && field.DeclaringType.Scope == field.Module)
				return field is FieldDef ? (FieldDef)field : ((MemberRef)field).ResolveField();
			else
				return null;
		}

		public static MethodDef ResolveMethodWithinSameModule(this MemberRef method)
		{
			if (method != null && method.DeclaringType.Scope == method.Module)
				return method.ResolveMethod();
			else
				return null;
		}
		
		public static MethodDef ResolveMethodWithinSameModule(this IMethod method)
		{
			if (method is MethodSpec)
				return ((MethodSpec)method).Method.ResolveMethodWithinSameModule();
			if (method != null && method.DeclaringType.Scope == method.Module)
				return method is MethodDef ? (MethodDef)method : ((MemberRef)method).ResolveMethod();
			else
				return null;
		}

		public static MethodDef Resolve(this IMethod method)
		{
			if (method is MemberRef)
				return ((MemberRef)method).ResolveMethod();
			else if (method is MethodSpec)
				return ((MethodSpec)method).Method.Resolve();
			else
				return (MethodDef)method;
		}

		public static FieldDef Resolve(this IField field)
		{
			if (field is MemberRef)
				return ((MemberRef)field).ResolveField();
			else
				return (FieldDef)field;
		}

		public static TypeDef Resolve(this TypeSig type)
		{
			if (type is GenericInstSig)
				return ((GenericInstSig)type).GenericType.TypeDefOrRef.ResolveTypeDef();
			else if (type is TypeDefOrRefSig)
				return ((TypeDefOrRefSig)type).TypeDefOrRef.ResolveTypeDef();
			else
				return null;
		}

		public static bool IsCompilerGenerated(this IHasCustomAttribute provider)
		{
			if (provider != null && provider.HasCustomAttributes) {
				foreach (CustomAttribute a in provider.CustomAttributes) {
					if (a.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute")
						return true;
				}
			}
			return false;
		}
		
		public static bool IsCompilerGeneratedOrIsInCompilerGeneratedClass(this IMemberDef member)
		{
			if (member == null)
				return false;
			if (member.IsCompilerGenerated())
				return true;
			return IsCompilerGeneratedOrIsInCompilerGeneratedClass(member.DeclaringType);
		}

		public static TypeSig GetEnumUnderlyingType(this TypeDef type)
		{
			if (!type.IsEnum)
				throw new ArgumentException("Type must be an enum", "type");

			return type.GetEnumUnderlyingType();
		}
		
		public static bool IsAnonymousType(this ITypeDefOrRef type)
		{
			if (type == null)
				return false;
			if (string.IsNullOrEmpty(type.Namespace) && type.HasGeneratedName() && (type.Name.Contains("AnonType") || type.Name.Contains("AnonymousType"))) {
				TypeDef td = type.ResolveTypeDef();
				return td != null && td.IsCompilerGenerated();
			}
			return false;
		}

		public static bool HasGeneratedName(this IMemberRef member)
		{
			return member.Name.StartsWith("<", StringComparison.Ordinal);
		}
		
		public static bool ContainsAnonymousType(this TypeSig type)
		{
			GenericInstSig git = type as GenericInstSig;
			if (git != null) {
				if (IsAnonymousType(git.GenericType.TypeDefOrRef))
					return true;
				for (int i = 0; i < git.GenericArguments.Count; i++) {
					if (git.GenericArguments[i].ContainsAnonymousType())
						return true;
				}
				return false;
			}
			if (type.Next != null)
				return ContainsAnonymousType(type.Next);
			return false;
		}

		public static string GetDefaultMemberName(this TypeDef type)
		{
			CustomAttribute attr;
			return type.GetDefaultMemberName(out attr);
		}

		public static string GetDefaultMemberName(this TypeDef type, out CustomAttribute defaultMemberAttribute)
		{
			if (type.HasCustomAttributes)
				foreach (CustomAttribute ca in type.CustomAttributes)
					if (ca.AttributeType.Name == "DefaultMemberAttribute" && ca.AttributeType.Namespace == "System.Reflection"
						&& ((IMethod)ca.Constructor).FullName == @"System.Void System.Reflection.DefaultMemberAttribute::.ctor(System.String)") {
						defaultMemberAttribute = ca;
						return (UTF8String)ca.ConstructorArguments[0].Value;
					}
			defaultMemberAttribute = null;
			return null;
		}

		public static bool IsIndexer(this PropertyDef property)
		{
			CustomAttribute attr;
			return property.IsIndexer(out attr);
		}

		public static bool IsIndexer(this PropertyDef property, out CustomAttribute defaultMemberAttribute)
		{
			defaultMemberAttribute = null;
			if (property.PropertySig.GetParamCount() > 0) {
				var accessor = property.GetMethod ?? property.SetMethod;
				PropertyDef basePropDef = property;
				if (accessor.HasOverrides) {
					// if the property is explicitly implementing an interface, look up the property in the interface:
					MethodDef baseAccessor = accessor.Overrides.First().MethodDeclaration.Resolve();
					if (baseAccessor != null) {
						foreach (PropertyDef baseProp in baseAccessor.DeclaringType.Properties) {
							if (baseProp.GetMethod == baseAccessor || baseProp.SetMethod == baseAccessor) {
								basePropDef = baseProp;
								break;
							}
						}
					} else
						return false;
				}
				CustomAttribute attr;
				var defaultMemberName = basePropDef.DeclaringType.GetDefaultMemberName(out attr);
				if (defaultMemberName == basePropDef.Name) {
					defaultMemberAttribute = attr;
					return true;
				}
			}
			return false;
		}

		public static ITypeDefOrRef GetDeclaringType(this ITypeDefOrRef typeDefOrRef)
		{
			if (typeDefOrRef is TypeSpec)
				throw new NotSupportedException();
			if (typeDefOrRef is TypeDef)
				return ((TypeDef)typeDefOrRef).DeclaringType;
			else
				return ((TypeRef)typeDefOrRef).DeclaringType;
		}

		public static int GetCodeSize(this CilBody body)
		{
			if (body.Instructions.Count == 0)
				return 0;
			else
			{
				var instr = body.Instructions.Last();
				return instr.GetEndOffset();
			}
		}

		public static Instruction GetPrevious(this CilBody body, Instruction instr)
		{
			int index = body.Instructions.IndexOf(instr);
			if (index == -1 || index == 0)
				return null;
			return body.Instructions[index - 1];
		}

		public static Instruction GetNext(this CilBody body, Instruction instr)
		{
			int index = body.Instructions.IndexOf(instr);
			if (index == -1 || index + 1 >= body.Instructions.Count)
				return null;
			return body.Instructions[index + 1];
		}

		public static bool IsGetter(this MethodDef method)
		{
			return method.DeclaringType.Properties.Any(propertyDef => propertyDef.GetMethod == method);
		}

		public static bool IsSetter(this MethodDef method)
		{
			return method.DeclaringType.Properties.Any(propertyDef => propertyDef.SetMethod == method);
		}

		public static bool IsAddOn(this MethodDef method)
		{
			return method.DeclaringType.Events.Any(propertyDef => propertyDef.AddMethod == method);
		}

		public static bool IsRemoveOn(this MethodDef method)
		{
			return method.DeclaringType.Events.Any(propertyDef => propertyDef.RemoveMethod == method);
		}

		public static bool HasSemantics(this MethodDef method)
		{
			return method.DeclaringType.Properties.Any(propertyDef => propertyDef.SetMethod == method || propertyDef.GetMethod == method) ||
				   method.DeclaringType.Events.Any(propertyDef => propertyDef.AddMethod == method || propertyDef.RemoveMethod == method || propertyDef.InvokeMethod == method);
		}

		public static IList<TypeSig> GetParameters(this MethodBaseSig methodSig)
		{
			if (methodSig.ParamsAfterSentinel != null)
				return methodSig.Params
					.Concat(new TypeSig[] { new SentinelSig() })
					.Concat(methodSig.ParamsAfterSentinel)
					.ToList();
			else
				return methodSig.Params;
		}

		public static ITypeDefOrRef GetTypeDefOrRef(this TypeSig type)
		{
			type = type.RemovePinnedAndModifiers();
			if (type.IsGenericInstanceType)
				return ((GenericInstSig)type).GenericType.TypeDefOrRef;
			else if (type.IsTypeDefOrRef)
				return ((TypeDefOrRefSig)type).TypeDefOrRef;
			else
				return null;
		}

		public static bool IsCorlibType(this ITypeDefOrRef type, string ns, string name)
		{
			return type.DefinitionAssembly.IsCorLib() && type.Namespace == ns && type.Name == name;
		}

		public static IEnumerable<Parameter> GetParameters(this PropertyDef property)
		{
			if (property.GetMethod != null)
			{
				foreach (var param in property.GetMethod.Parameters)
					yield return param;
				yield break;
			}
			if (property.SetMethod != null)
			{
				int last = property.SetMethod.Parameters.Count - 1;
				foreach (var param in property.SetMethod.Parameters)
				{
					if (param.Index != last)
						yield return param;
				}
				yield break;
			}

			int i = 0;
			foreach (TypeSig param in property.PropertySig.GetParameters())
			{
				yield return new Parameter(i,i,param);
				i++;
			}
		}

		public static string GetScopeName(this IScope scope)
		{
			if (scope is AssemblyRef)
				return ((AssemblyRef)scope).Name;
			else
				return scope.ScopeName;
		}

		public static object ResolveGenericParams(this object operand, MethodDef methodContext)
		{
			if (methodContext == null)
				return operand;
			var typeParams = methodContext.DeclaringType.GenericParameters
				.Select(param => (TypeSig)new GenericTypeParam(param))
				.ToList();
			var methodParams = methodContext.GenericParameters
				.Select(param => (TypeSig)new GenericMethodParam(param))
				.ToList();
			return ResolveGenericParams(typeParams, methodParams, operand);
		}
		public static TypeSig ResolveGenericParams(this TypeSig type, MethodDef methodContext)
		{
			if (methodContext == null)
				return type;
			var typeParams = methodContext.DeclaringType.GenericParameters
				.Select(param => (TypeSig)new GenericTypeParam(param))
				.ToList();
			var methodParams = methodContext.GenericParameters
				.Select(param => (TypeSig)new GenericMethodParam(param))
				.ToList();
			return (TypeSig)ResolveGenericParams(typeParams, methodParams, type);
		}
		public static TypeSig ResolveGenericParams(this TypeSig type, TypeDef typeContext)
		{
			if (typeContext == null)
				return type;
			var typeParams = typeContext.GenericParameters
				.Select(param => (TypeSig)new GenericTypeParam(param))
				.ToList();
			return (TypeSig)ResolveGenericParams(typeParams, null, type);
		}
		internal static object ResolveGenericParams(List<TypeSig> typeParams, List<TypeSig> methodParams, object operand)
		{
			if (operand is MemberRef)
			{
				MemberRef memberRef = (MemberRef)operand;
				if (memberRef.IsFieldRef)
					return new MemberRefUser(
						memberRef.Module, memberRef.Name,
						new FieldSig((TypeSig)ResolveGenericParams(typeParams, methodParams, memberRef.FieldSig.Type)),
						(IMemberRefParent)ResolveGenericParams(typeParams, methodParams, memberRef.DeclaringType)) { Rid = memberRef.Rid };
				else
					return new MemberRefUser(
						memberRef.Module, memberRef.Name,
						GenericArgumentResolver.Resolve(memberRef.MethodSig, typeParams, methodParams),
						(IMemberRefParent)ResolveGenericParams(typeParams, methodParams, memberRef.DeclaringType)) { Rid = memberRef.Rid };
			}
			else if (operand is MethodSpec)
			{
				MethodSpec spec = (MethodSpec)operand;
				return new MethodSpecUser(
					(IMethodDefOrRef)ResolveGenericParams(typeParams, methodParams, spec.Method),
					new GenericInstMethodSig(spec.GenericInstMethodSig.GenericArguments.Select(arg => (TypeSig)ResolveGenericParams(typeParams, methodParams, arg)).ToList())
					) { Rid = spec.Rid };
			}
			else if (operand is TypeSpec)
			{
				TypeSpec spec = (TypeSpec)operand;
				return new TypeSpecUser((TypeSig)ResolveGenericParams(typeParams, methodParams, spec.TypeSig)) { Rid = spec.Rid };
			}
			else if (operand is TypeSig)
			{
				return GenericArgumentResolver.Resolve((TypeSig)operand, typeParams, methodParams);
			}
			return operand;
		}
	}
}
