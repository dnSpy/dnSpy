// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Linq;
using Mono.Cecil;

namespace Decompiler
{
	/// <summary>
	/// IL AST transformation that introduces array initializers.
	/// </summary>
	public class ArrayInitializers
	{
		public static void Transform(ILBlock method)
		{
			var newArrPattern = new StoreToGenerated(new ILExpression(
				ILCode.Dup, null,
				new ILExpression(ILCode.Newarr, ILExpression.AnyOperand, new ILExpression(ILCode.Ldc_I4, ILExpression.AnyOperand))
			));
			var arg1 = new StoreToGenerated(new LoadFromVariable(newArrPattern));
			var arg2 = new StoreToGenerated(new LoadFromVariable(newArrPattern));
			var initializeArrayPattern = new ILCall(
				"System.Runtime.CompilerServices.RuntimeHelpers", "InitializeArray",
				new LoadFromVariable(arg1), new ILExpression(ILCode.Ldtoken, ILExpression.AnyOperand));
			foreach (ILBlock block in method.GetSelfAndChildrenRecursive<ILBlock>()) {
				for (int i = block.Body.Count - 4; i >= 0; i--) {
					if (!newArrPattern.Match(block.Body[i]))
						continue;
					ILExpression newArrInst = ((ILExpression)block.Body[i]).Arguments[0].Arguments[0];
					int arrayLength = (int)newArrInst.Arguments[0].Operand;
					if (!arg1.Match(block.Body[i + 1]))
						continue;
					if (!arg2.Match(block.Body[i + 2]))
						continue;
					if (initializeArrayPattern.Match(block.Body[i + 3])) {
						FieldDefinition field = ((ILExpression)block.Body[i+3]).Arguments[1].Operand as FieldDefinition;
						if (field == null || field.InitialValue == null)
							continue;
						switch (TypeAnalysis.GetTypeCode(newArrInst.Operand as TypeReference)) {
							case TypeCode.Int32:
							case TypeCode.UInt32:
								if (field.InitialValue.Length == arrayLength * 4) {
									ILExpression[] newArr = new ILExpression[arrayLength];
									for (int j = 0; j < newArr.Length; j++) {
										newArr[j] = new ILExpression(ILCode.Ldc_I4, BitConverter.ToInt32(field.InitialValue, j * 4));
									}
									block.Body[i] = new ILExpression(ILCode.Stloc, arg1.LastVariable, new ILExpression(ILCode.InitArray, newArrInst.Operand, newArr));
									block.Body.RemoveRange(i + 1, 3);
								}
								continue;
							default:
								continue;
						}
					}
				}
			}
		}
		
	}
}
