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
using System.Linq;
using ICSharpCode.Decompiler;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer
{
	internal static class Helpers
	{
		public static bool IsReferencedBy(TypeDefinition type, TypeReference typeRef)
		{
			// TODO: move it to a better place after adding support for more cases.
			if (type == null)
				throw new ArgumentNullException("type");
			if (typeRef == null)
				throw new ArgumentNullException("typeRef");

			if (type == typeRef)
				return true;
			if (type.Name != typeRef.Name)
				return false;
			if (type.Namespace != typeRef.Namespace)
				return false;

			if (type.DeclaringType != null || typeRef.DeclaringType != null) {
				if (type.DeclaringType == null || typeRef.DeclaringType == null)
					return false;
				if (!IsReferencedBy(type.DeclaringType, typeRef.DeclaringType))
					return false;
			}

			return true;
		}

		public static MemberReference GetOriginalCodeLocation(MemberReference member)
		{
			if (member is MethodDefinition)
				return GetOriginalCodeLocation((MethodDefinition)member);
			return member;
		}

		public static MethodDefinition GetOriginalCodeLocation(MethodDefinition method)
		{
			if (method.IsCompilerGenerated()) {
				return FindMethodUsageInType(method.DeclaringType, method) ?? method;
			}

			var typeUsage = GetOriginalCodeLocation(method.DeclaringType);

			return typeUsage ?? method;
		}
		
		/// <summary>
		/// Given a compiler-generated type, returns the method where that type is used.
		/// Used to detect the 'parent method' for a lambda/iterator/async state machine.
		/// </summary>
		public static MethodDefinition GetOriginalCodeLocation(TypeDefinition type)
		{
			if (type != null && type.DeclaringType != null && type.IsCompilerGenerated()) {
				if (type.IsValueType) {
					// Value types might not have any constructor; but they must be stored in a local var
					// because 'initobj' (or 'call .ctor') expects a managed ref.
					return FindVariableOfTypeUsageInType(type.DeclaringType, type);
				} else {
					MethodDefinition constructor = GetTypeConstructor(type);
					if (constructor == null)
						return null;
					return FindMethodUsageInType(type.DeclaringType, constructor);
				}
			}
			return null;
		}

		private static MethodDefinition GetTypeConstructor(TypeDefinition type)
		{
			return type.Methods.FirstOrDefault(method => method.Name == ".ctor");
		}

		private static MethodDefinition FindMethodUsageInType(TypeDefinition type, MethodDefinition analyzedMethod)
		{
			string name = analyzedMethod.Name;
			foreach (MethodDefinition method in type.Methods) {
				bool found = false;
				if (!method.HasBody)
					continue;
				foreach (Instruction instr in method.Body.Instructions) {
					MethodReference mr = instr.Operand as MethodReference;
					if (mr != null && mr.Name == name &&
						IsReferencedBy(analyzedMethod.DeclaringType, mr.DeclaringType) &&
						mr.Resolve() == analyzedMethod) {
						found = true;
						break;
					}
				}

				method.Body = null;

				if (found)
					return method;
			}
			return null;
		}
		
		private static MethodDefinition FindVariableOfTypeUsageInType(TypeDefinition type, TypeDefinition variableType)
		{
			foreach (MethodDefinition method in type.Methods) {
				bool found = false;
				if (!method.HasBody)
					continue;
				foreach (var v in method.Body.Variables) {
					if (v.VariableType.ResolveWithinSameModule() == variableType) {
						found = true;
						break;
					}
				}

				method.Body = null;

				if (found)
					return method;
			}
			return null;
		}
	}
}
