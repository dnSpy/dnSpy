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
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ICSharpCode.Decompiler
{
	/// <summary>
	/// Cecil helper methods.
	/// </summary>
	public static class CecilExtensions
	{
		#region GetPushDelta / GetPopDelta
		public static int GetPushDelta(this Instruction instruction)
		{
			OpCode code = instruction.OpCode;
			switch (code.StackBehaviourPush) {
				case StackBehaviour.Push0:
					return 0;

				case StackBehaviour.Push1:
				case StackBehaviour.Pushi:
				case StackBehaviour.Pushi8:
				case StackBehaviour.Pushr4:
				case StackBehaviour.Pushr8:
				case StackBehaviour.Pushref:
					return 1;

				case StackBehaviour.Push1_push1:
					return 2;

				case StackBehaviour.Varpush:
					if (code.FlowControl != FlowControl.Call)
						break;

					IMethodSignature method = (IMethodSignature) instruction.Operand;
					return IsVoid (method.ReturnType) ? 0 : 1;
			}

			throw new NotSupportedException ();
		}
		
		public static int? GetPopDelta(this Instruction instruction, MethodDefinition methodDef)
		{
			OpCode code = instruction.OpCode;
			switch (code.StackBehaviourPop) {
				case StackBehaviour.Pop0:
					return 0;
				case StackBehaviour.Popi:
				case StackBehaviour.Popref:
				case StackBehaviour.Pop1:
					return 1;

				case StackBehaviour.Pop1_pop1:
				case StackBehaviour.Popi_pop1:
				case StackBehaviour.Popi_popi:
				case StackBehaviour.Popi_popi8:
				case StackBehaviour.Popi_popr4:
				case StackBehaviour.Popi_popr8:
				case StackBehaviour.Popref_pop1:
				case StackBehaviour.Popref_popi:
					return 2;

				case StackBehaviour.Popi_popi_popi:
				case StackBehaviour.Popref_popi_popi:
				case StackBehaviour.Popref_popi_popi8:
				case StackBehaviour.Popref_popi_popr4:
				case StackBehaviour.Popref_popi_popr8:
				case StackBehaviour.Popref_popi_popref:
					return 3;

				case StackBehaviour.PopAll:
					return null;

				case StackBehaviour.Varpop:
					if (code == OpCodes.Ret)
						return methodDef.ReturnType.IsVoid() ? 0 : 1;

					if (code.FlowControl != FlowControl.Call)
						break;

					IMethodSignature method = (IMethodSignature) instruction.Operand;
					int count = method.HasParameters ? method.Parameters.Count : 0;
					if (method.HasThis && code != OpCodes.Newobj)
						++count;
					if (code == OpCodes.Calli)
						++count; // calli takes a function pointer in additional to the normal args

					return count;
			}

			throw new NotSupportedException ();
		}
		
		public static bool IsVoid(this TypeReference type)
		{
			while (type is OptionalModifierType || type is RequiredModifierType)
				type = ((TypeSpecification)type).ElementType;
			return type.MetadataType == MetadataType.Void;
		}
		
		public static bool IsValueTypeOrVoid(this TypeReference type)
		{
			while (type is OptionalModifierType || type is RequiredModifierType)
				type = ((TypeSpecification)type).ElementType;
			if (type is ArrayType)
				return false;
			return type.IsValueType || type.IsVoid();
		}

		/// <summary>
		/// checks if the given TypeReference is one of the following types:
		/// [sbyte, short, int, long, IntPtr]
		/// </summary>
		public static bool IsSignedIntegralType(this TypeReference type)
		{
			return type.MetadataType == MetadataType.SByte ||
				   type.MetadataType == MetadataType.Int16 ||
				   type.MetadataType == MetadataType.Int32 ||
				   type.MetadataType == MetadataType.Int64 ||
				   type.MetadataType == MetadataType.IntPtr;
		}

		/// <summary>
		/// checks if the given value is a numeric zero-value.
		/// NOTE that this only works for types: [sbyte, short, int, long, IntPtr, byte, ushort, uint, ulong, float, double and decimal]
		/// </summary>
		public static bool IsZero(this object value)
		{
			return value.Equals((sbyte)0) ||
				   value.Equals((short)0) ||
				   value.Equals(0) ||
				   value.Equals(0L) ||
				   value.Equals(IntPtr.Zero) ||
				   value.Equals((byte)0) ||
				   value.Equals((ushort)0) ||
				   value.Equals(0u) ||
				   value.Equals(0UL) ||
				   value.Equals(0.0f) ||
				   value.Equals(0.0) ||
				   value.Equals((decimal)0);
					
		}

		#endregion
		
		/// <summary>
		/// Gets the (exclusive) end offset of this instruction.
		/// </summary>
		public static int GetEndOffset(this Instruction inst)
		{
			if (inst == null)
				throw new ArgumentNullException("inst");
			return inst.Offset + inst.GetSize();
		}
		
		public static string OffsetToString(int offset)
		{
			return string.Format("IL_{0:x4}", offset);
		}
		
		public static HashSet<MethodDefinition> GetAccessorMethods(this TypeDefinition type)
		{
			HashSet<MethodDefinition> accessorMethods = new HashSet<MethodDefinition>();
			foreach (var property in type.Properties) {
				accessorMethods.Add(property.GetMethod);
				accessorMethods.Add(property.SetMethod);
				if (property.HasOtherMethods) {
					foreach (var m in property.OtherMethods)
						accessorMethods.Add(m);
				}
			}
			foreach (EventDefinition ev in type.Events) {
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
		
		public static TypeDefinition ResolveWithinSameModule(this TypeReference type)
		{
			if (type != null && type.GetElementType().Module == type.Module)
				return type.Resolve();
			else
				return null;
		}
		
		public static FieldDefinition ResolveWithinSameModule(this FieldReference field)
		{
			if (field != null && field.DeclaringType.GetElementType().Module == field.Module)
				return field.Resolve();
			else
				return null;
		}
		
		public static MethodDefinition ResolveWithinSameModule(this MethodReference method)
		{
			if (method != null && method.DeclaringType.GetElementType().Module == method.Module)
				return method.Resolve();
			else
				return null;
		}

		[Obsolete("throwing exceptions is considered a bug")]
		public static TypeDefinition ResolveOrThrow(this TypeReference typeReference)
		{
			var resolved = typeReference.Resolve();
			if (resolved == null)
				throw new ReferenceResolvingException();
			return resolved;
		}

		public static bool IsCompilerGenerated(this ICustomAttributeProvider provider)
		{
			if (provider != null && provider.HasCustomAttributes) {
				foreach (CustomAttribute a in provider.CustomAttributes) {
					if (a.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute")
						return true;
				}
			}
			return false;
		}
		
		public static bool IsCompilerGeneratedOrIsInCompilerGeneratedClass(this IMemberDefinition member)
		{
			if (member == null)
				return false;
			if (member.IsCompilerGenerated())
				return true;
			return IsCompilerGeneratedOrIsInCompilerGeneratedClass(member.DeclaringType);
		}

		public static TypeReference GetEnumUnderlyingType(this TypeDefinition type)
		{
			if (!type.IsEnum)
				throw new ArgumentException("Type must be an enum", "type");

			var fields = type.Fields;

			for (int i = 0; i < fields.Count; i++)
			{
				var field = fields[i];
				if (!field.IsStatic)
					return field.FieldType;
			}

			throw new NotSupportedException();
		}
		
		public static bool IsAnonymousType(this TypeReference type)
		{
			if (type == null)
				return false;
			if (string.IsNullOrEmpty(type.Namespace) && type.HasGeneratedName() && (type.Name.Contains("AnonType") || type.Name.Contains("AnonymousType"))) {
				TypeDefinition td = type.Resolve();
				return td != null && td.IsCompilerGenerated();
			}
			return false;
		}

		public static bool HasGeneratedName(this MemberReference member)
		{
			return member.Name.StartsWith("<", StringComparison.Ordinal);
		}
		
		public static bool ContainsAnonymousType(this TypeReference type)
		{
			GenericInstanceType git = type as GenericInstanceType;
			if (git != null) {
				if (IsAnonymousType(git))
					return true;
				for (int i = 0; i < git.GenericArguments.Count; i++) {
					if (git.GenericArguments[i].ContainsAnonymousType())
						return true;
				}
				return false;
			}
			TypeSpecification typeSpec = type as TypeSpecification;
			if (typeSpec != null)
				return typeSpec.ElementType.ContainsAnonymousType();
			else
				return false;
		}

		public static string GetDefaultMemberName(this TypeDefinition type)
		{
			CustomAttribute attr;
			return type.GetDefaultMemberName(out attr);
		}

		public static string GetDefaultMemberName(this TypeDefinition type, out CustomAttribute defaultMemberAttribute)
		{
			if (type.HasCustomAttributes)
				foreach (CustomAttribute ca in type.CustomAttributes)
					if (ca.Constructor.DeclaringType.Name == "DefaultMemberAttribute" && ca.Constructor.DeclaringType.Namespace == "System.Reflection"
						&& ca.Constructor.FullName == @"System.Void System.Reflection.DefaultMemberAttribute::.ctor(System.String)") {
						defaultMemberAttribute = ca;
						return ca.ConstructorArguments[0].Value as string;
					}
			defaultMemberAttribute = null;
			return null;
		}

		public static bool IsIndexer(this PropertyDefinition property)
		{
			CustomAttribute attr;
			return property.IsIndexer(out attr);
		}

		public static bool IsIndexer(this PropertyDefinition property, out CustomAttribute defaultMemberAttribute)
		{
			defaultMemberAttribute = null;
			if (property.HasParameters) {
				var accessor = property.GetMethod ?? property.SetMethod;
				PropertyDefinition basePropDef = property;
				if (accessor.HasOverrides) {
					// if the property is explicitly implementing an interface, look up the property in the interface:
					MethodDefinition baseAccessor = accessor.Overrides.First().Resolve();
					if (baseAccessor != null) {
						foreach (PropertyDefinition baseProp in baseAccessor.DeclaringType.Properties) {
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

		public static bool IsDelegate(this TypeDefinition type)
		{
			if (type.BaseType != null && type.BaseType.Namespace == "System") {
				if (type.BaseType.Name == "MulticastDelegate")
					return true;
				if (type.BaseType.Name == "Delegate" && type.Name != "MulticastDelegate")
					return true;
			}
			return false;
		}
	}
}
