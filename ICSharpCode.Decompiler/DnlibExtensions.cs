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
using System.Text;
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
			return type.ScopeType.ResolveTypeDef();
		}

		public static bool IsCompilerGenerated(this IHasCustomAttribute provider)
		{
			return provider != null && provider.CustomAttributes.IsDefined("System.Runtime.CompilerServices.CompilerGeneratedAttribute");
		}
		
		public static bool IsCompilerGeneratedOrIsInCompilerGeneratedClass(this IMemberDef member)
		{
			if (member == null)
				return false;
			if (member.IsCompilerGenerated())
				return true;
			return IsCompilerGeneratedOrIsInCompilerGeneratedClass(member.DeclaringType);
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

		public static string GetDefaultMemberName(this TypeDef type, out CustomAttribute defaultMemberAttribute)
		{
			if (type.HasCustomAttributes)
				foreach (CustomAttribute ca in type.CustomAttributes.FindAll("System.Reflection.DefaultMemberAttribute"))
					if (ca.Constructor.FullName == @"System.Void System.Reflection.DefaultMemberAttribute::.ctor(System.String)" &&
						ca.ConstructorArguments[0].Value is UTF8String) {
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

		static bool IsIndexer(this PropertyDef property, out CustomAttribute defaultMemberAttribute)
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

		public static bool HasSemanticsButNotInvoke(this MethodDef method)
		{
			return method.DeclaringType.Properties.Any(propertyDef => propertyDef.SetMethod == method || propertyDef.GetMethod == method) ||
				   method.DeclaringType.Events.Any(propertyDef => propertyDef.AddMethod == method || propertyDef.RemoveMethod == method);
		}

		public static bool HasNormalParameter(this IEnumerable<Parameter> list)
		{
			foreach (var p in list)
				if (p.IsNormalMethodParameter)
					return true;
			return false;
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
			else if (type.IsTypeDefOrRefSig)
				return ((TypeDefOrRefSig)type).TypeDefOrRef;
			else
				return null;
		}

		public static bool IsCorlibType(this ITypeDefOrRef type, string ns, string name)
		{
			return type != null && type.DefinitionAssembly.IsCorLib() && type.Namespace == ns && type.Name == name;
		}

		public static IList<Parameter> GetParameters(this IMethod method)
		{
			if (method == null || method.MethodSig == null)
				return new List<Parameter>();

			var md = method as MethodDef;
			if (md != null)
				return md.Parameters;

			var list = new List<Parameter>();
			int paramIndex = 0, methodSigIndex = 0;
			if (method.MethodSig.HasThis)
				list.Add(new Parameter(paramIndex++, Parameter.HIDDEN_THIS_METHOD_SIG_INDEX, method.DeclaringType.ToTypeSigInternal()));
			foreach (var type in method.MethodSig.GetParameters())
				list.Add(new Parameter(paramIndex++, methodSigIndex++, type));
			return list;
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
			if (scope is IFullName)
				return ((IFullName)scope).Name;
			else
				return scope.ScopeName;	// Shouldn't be reached
		}

		public static int GetParametersSkip(this IList<Parameter> parameters)
		{
			if (parameters.Count == 0)
				return 0;
			if (parameters[0].IsHiddenThisParameter)
				return 1;
			return 0;
		}

		public static IEnumerable<Parameter> SkipNonNormal(this IList<Parameter> parameters)
		{
			foreach (var p in parameters)
				if (p.IsNormalMethodParameter)
					yield return p;
		}

		public static int GetNumberOfNormalParameters(this IList<Parameter> parameters)
		{
			return parameters.Count - GetParametersSkip(parameters);
		}

		public static IEnumerable<int> GetLengths(this ArraySigBase ary)
		{
			var sizes = ary.GetSizes();
			for (int i = 0; i < (int)ary.Rank; i++)
				yield return i < sizes.Count ? (int)sizes[i] - 1 : 0;
		}

		public static string GetFnPtrFullName(FnPtrSig sig)
		{
			if (sig == null)
				return string.Empty;
			var methodSig = sig.MethodSig;
			if (methodSig == null)
				return GetFnPtrName(sig);

			var sb = new StringBuilder();

			sb.Append("method ");
			sb.Append(FullNameCreator.FullName(methodSig.RetType, false));
			sb.Append(" *(");
			PrintArgs(sb, methodSig.Params, true);
			if (methodSig.ParamsAfterSentinel != null) {
				if (methodSig.Params.Count > 0)
					sb.Append(",");
				sb.Append("...,");
				PrintArgs(sb, methodSig.ParamsAfterSentinel, false);
			}
			sb.Append(")");

			return sb.ToString();
		}

		public static string GetMethodSigFullName(MethodSig methodSig)
		{
			var sb = new StringBuilder();

			sb.Append(FullNameCreator.FullName(methodSig.RetType, false));
			sb.Append("(");
			PrintArgs(sb, methodSig.Params, true);
			if (methodSig.ParamsAfterSentinel != null) {
				if (methodSig.Params.Count > 0)
					sb.Append(",");
				sb.Append("...,");
				PrintArgs(sb, methodSig.ParamsAfterSentinel, false);
			}
			sb.Append(")");

			return sb.ToString();
		}

		static void PrintArgs(StringBuilder sb, IList<TypeSig> args, bool isFirst) {
			foreach (var arg in args) {
				if (!isFirst)
					sb.Append(",");
				isFirst = false;
				sb.Append(FullNameCreator.FullName(arg, false));
			}
		}

		public static string GetFnPtrName(FnPtrSig sig)
		{
			return "method";
		}

		public static bool IsValueType(ITypeDefOrRef tdr)
		{
			if (tdr == null)
				return false;
			var ts = tdr as TypeSpec;
			if (ts != null)
				return IsValueType(ts.TypeSig);
			return tdr.IsValueTypeCached;
		}

		public static bool IsValueType(TypeSig ts)
		{
			if (ts == null)
				return false;
			switch (ts.ElementType) {
			case ElementType.SZArray:
			case ElementType.Array:
			case ElementType.CModReqd:
			case ElementType.CModOpt:
			case ElementType.Pinned:
			case ElementType.Ptr:
			case ElementType.ByRef:
			case ElementType.Sentinel:
				// Emulate cecil behavior
				return false;
			default:
				return ts.IsValueType;
			}
		}

		public static TypeSig ToTypeSigInternal(this ITypeDefOrRef type)
		{
			return type == null ? null : type.ToTypeSig();
		}
	}
}
