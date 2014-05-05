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
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer
{
	internal static class Helpers
	{
		public static bool IsReferencedBy(TypeDef type, ITypeDefOrRef typeRef)
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

			var declType = typeRef.GetDeclaringType();
			if (type.DeclaringType != null || declType != null) {
				if (type.DeclaringType == null || declType == null)
					return false;
				if (!IsReferencedBy(type.DeclaringType, declType))
					return false;
			}

			return true;
		}

		public static IMemberRef GetOriginalCodeLocation(IMemberRef member)
		{
			if (member is MethodDef)
				return GetOriginalCodeLocation((MethodDef)member);
			return member;
		}

		public static MethodDef GetOriginalCodeLocation(MethodDef method)
		{
			if (method.IsCompilerGenerated()) {
				return FindMethodUsageInType(method.DeclaringType, method) ?? method;
			}

			var typeUsage = GetOriginalCodeLocation(method.DeclaringType);

			return typeUsage ?? method;
		}
		public static MethodDef GetOriginalCodeLocation(TypeDef type)
		{
			if (type != null && type.DeclaringType != null && type.IsCompilerGenerated()) {
				MethodDef constructor = GetTypeConstructor(type);
				return FindMethodUsageInType(type.DeclaringType, constructor);
			}
			return null;
		}

		private static MethodDef GetTypeConstructor(TypeDef type)
		{
			return type.FindConstructors().FirstOrDefault();
		}

		private static MethodDef FindMethodUsageInType(TypeDef type, MethodDef analyzedMethod)
		{
			string name = analyzedMethod.Name;
			foreach (MethodDef method in type.Methods) {
				bool found = false;
				if (!method.HasBody)
					continue;
				foreach (Instruction instr in method.Body.Instructions) {
					IMethod mr = instr.Operand as IMethod;
					if (mr != null && !(mr is MemberRef && ((MemberRef)mr).IsFieldRef) && mr.Name == name &&
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
	}
}
