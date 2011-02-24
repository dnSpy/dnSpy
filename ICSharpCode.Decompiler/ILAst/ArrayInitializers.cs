// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
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
			// TODO: move this somewhere else
			// Eliminate 'dups':
			foreach (ILExpression expr in method.GetSelfAndChildrenRecursive<ILExpression>()) {
				for (int i = 0; i < expr.Arguments.Count; i++) {
					if (expr.Arguments[i].Code == ILCode.Dup)
						expr.Arguments[i] = expr.Arguments[i].Arguments[0];
				}
			}
			
			var newArrPattern = new StoreToVariable(new ILExpression(ILCode.Newarr, ILExpression.AnyOperand, new ILExpression(ILCode.Ldc_I4, ILExpression.AnyOperand)));
			var arg1 = new StoreToVariable(new LoadFromVariable(newArrPattern)) { MustBeGenerated = true };
			var arg2 = new StoreToVariable(new LoadFromVariable(newArrPattern)) { MustBeGenerated = true };
			var initializeArrayPattern = new ILCall(
				"System.Runtime.CompilerServices.RuntimeHelpers", "InitializeArray",
				new LoadFromVariable(arg1), new ILExpression(ILCode.Ldtoken, ILExpression.AnyOperand));
			foreach (ILBlock block in method.GetSelfAndChildrenRecursive<ILBlock>()) {
				for (int i = block.Body.Count - 1; i >= 0; i--) {
					if (!newArrPattern.Match(block.Body[i]))
						continue;
					ILExpression newArrInst = ((ILExpression)block.Body[i]).Arguments[0];
					int arrayLength = (int)newArrInst.Arguments[0].Operand;
					if (arrayLength == 0)
						continue;
					if (arg1.Match(block.Body.ElementAtOrDefault(i + 1)) && arg2.Match(block.Body.ElementAtOrDefault(i + 2))) {
						if (initializeArrayPattern.Match(block.Body.ElementAtOrDefault(i + 3))) {
							if (HandleStaticallyInitializedArray(arg2, block, i, newArrInst, arrayLength)) {
								i -= ILInlining.InlineInto(block, i + 1, method) - 1;
							}
							continue;
						}
					}
					if (i + 1 + arrayLength > block.Body.Count)
						continue;
					List<ILExpression> operands = new List<ILExpression>();
					for (int j = 0; j < arrayLength; j++) {
						ILExpression expr = block.Body[i + 1 + j] as ILExpression;
						if (expr == null || !IsStoreToArray(expr.Code))
							break;
						if (!(expr.Arguments[0].Code == ILCode.Ldloc && expr.Arguments[0].Operand == newArrPattern.LastVariable))
							break;
						if (!(expr.Arguments[1].Code == ILCode.Ldc_I4 && (int)expr.Arguments[1].Operand == j))
							break;
						operands.Add(expr.Arguments[2]);
					}
					if (operands.Count == arrayLength) {
						((ILExpression)block.Body[i]).Arguments[0] = new ILExpression(
							ILCode.InitArray, newArrInst.Operand, operands.ToArray());
						block.Body.RemoveRange(i + 1, arrayLength);
						i -= ILInlining.InlineInto(block, i + 1, method) - 1;
					}
				}
			}
		}
		
		static bool IsStoreToArray(ILCode code)
		{
			switch (code) {
				case ILCode.Stelem_Any:
				case ILCode.Stelem_I:
				case ILCode.Stelem_I1:
				case ILCode.Stelem_I2:
				case ILCode.Stelem_I4:
				case ILCode.Stelem_I8:
				case ILCode.Stelem_R4:
				case ILCode.Stelem_R8:
				case ILCode.Stelem_Ref:
					return true;
				default:
					return false;
			}
		}
		
		static bool HandleStaticallyInitializedArray(StoreToVariable arg2, ILBlock block, int i, ILExpression newArrInst, int arrayLength)
		{
			FieldDefinition field = ((ILExpression)block.Body[i + 3]).Arguments[1].Operand as FieldDefinition;
			if (field == null || field.InitialValue == null)
				return false;
			switch (TypeAnalysis.GetTypeCode(newArrInst.Operand as TypeReference)) {
				case TypeCode.Int32:
				case TypeCode.UInt32:
					if (field.InitialValue.Length == arrayLength * 4) {
						ILExpression[] newArr = new ILExpression[arrayLength];
						for (int j = 0; j < newArr.Length; j++) {
							newArr[j] = new ILExpression(ILCode.Ldc_I4, BitConverter.ToInt32(field.InitialValue, j * 4));
						}
						block.Body[i] = new ILExpression(ILCode.Stloc, arg2.LastVariable, new ILExpression(ILCode.InitArray, newArrInst.Operand, newArr));
						block.Body.RemoveRange(i + 1, 3);
						return true;
					}
					break;
			}
			return false;
		}
	}
}
