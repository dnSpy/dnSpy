// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;

using Mono.Cecil;

namespace ICSharpCode.Decompiler.ILAst
{
	/// <summary>
	/// IL AST transformation that introduces array, object and collection initializers.
	/// </summary>
	public class InitializerPeepholeTransforms
	{
		readonly ILBlock method;
		
		#region Array Initializers
		StoreToVariable newArrPattern;
		ILCall initializeArrayPattern;
		
		public InitializerPeepholeTransforms(ILBlock method)
		{
			this.method = method;
			newArrPattern = new StoreToVariable(new ILExpression(
				ILCode.Newarr, ILExpression.AnyOperand, new ILExpression(ILCode.Ldc_I4, ILExpression.AnyOperand)));
			initializeArrayPattern = new ILCall(
				"System.Runtime.CompilerServices.RuntimeHelpers", "InitializeArray",
				new LoadFromVariable(newArrPattern), new ILExpression(ILCode.Ldtoken, ILExpression.AnyOperand));
		}
		
		public void TransformArrayInitializers(ILBlock block, ref int i)
		{
			if (!newArrPattern.Match(block.Body[i]))
				return;
			ILExpression newArrInst = ((ILExpression)block.Body[i]).Arguments[0];
			int arrayLength = (int)newArrInst.Arguments[0].Operand;
			if (arrayLength == 0)
				return;
			if (initializeArrayPattern.Match(block.Body.ElementAtOrDefault(i + 1))) {
				if (HandleStaticallyInitializedArray(newArrPattern, block, i, newArrInst, arrayLength)) {
					i -= new ILInlining(method).InlineInto(block, i + 1, aggressive: true) - 1;
				}
				return;
			}
			List<ILExpression> operands = new List<ILExpression>();
			int numberOfInstructionsToRemove = 0;
			for (int j = i + 1; j < block.Body.Count; j++) {
				ILExpression expr = block.Body[j] as ILExpression;
				if (expr == null || !IsStoreToArray(expr.Code))
					break;
				if (!(expr.Arguments[0].Code == ILCode.Ldloc && expr.Arguments[0].Operand == newArrPattern.LastVariable))
					break;
				if (expr.Arguments[1].Code != ILCode.Ldc_I4)
					break;
				int pos = (int)expr.Arguments[1].Operand;
				const int maxConsecutiveDefaultValueExpressions = 10;
				if (pos < operands.Count || pos > operands.Count + maxConsecutiveDefaultValueExpressions)
					break;
				while (operands.Count < pos)
					operands.Add(new ILExpression(ILCode.DefaultValue, newArrInst.Operand));
				operands.Add(expr.Arguments[2]);
				numberOfInstructionsToRemove++;
			}
			if (operands.Count == arrayLength) {
				((ILExpression)block.Body[i]).Arguments[0] = new ILExpression(
					ILCode.InitArray, newArrInst.Operand, operands.ToArray());
				block.Body.RemoveRange(i + 1, numberOfInstructionsToRemove);
				i -= new ILInlining(method).InlineInto(block, i + 1, aggressive: true) - 1;
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
		
		static bool HandleStaticallyInitializedArray(StoreToVariable newArrPattern, ILBlock block, int i, ILExpression newArrInst, int arrayLength)
		{
			FieldDefinition field = ((ILExpression)block.Body[i + 1]).Arguments[1].Operand as FieldDefinition;
			if (field == null || field.InitialValue == null)
				return false;
			ILExpression[] newArr = new ILExpression[arrayLength];
			if (DecodeArrayInitializer(TypeAnalysis.GetTypeCode(newArrInst.Operand as TypeReference), field.InitialValue, newArr)) {
				block.Body[i] = new ILExpression(ILCode.Stloc, newArrPattern.LastVariable, new ILExpression(ILCode.InitArray, newArrInst.Operand, newArr));
				block.Body.RemoveAt(i + 1);
				return true;
			}
			return false;
		}
		
		static bool DecodeArrayInitializer(TypeCode elementType, byte[] initialValue, ILExpression[] output)
		{
			switch (elementType) {
				case TypeCode.Boolean:
				case TypeCode.SByte:
				case TypeCode.Byte:
					if (initialValue.Length == output.Length) {
						for (int j = 0; j < output.Length; j++) {
							output[j] = new ILExpression(ILCode.Ldc_I4, (int)initialValue[j]);
						}
						return true;
					}
					return false;
				case TypeCode.Char:
				case TypeCode.Int16:
				case TypeCode.UInt16:
					if (initialValue.Length == output.Length * 2) {
						for (int j = 0; j < output.Length; j++) {
							output[j] = new ILExpression(ILCode.Ldc_I4, (int)BitConverter.ToInt16(initialValue, j * 2));
						}
						return true;
					}
					return false;
				case TypeCode.Int32:
				case TypeCode.UInt32:
					if (initialValue.Length == output.Length * 4) {
						for (int j = 0; j < output.Length; j++) {
							output[j] = new ILExpression(ILCode.Ldc_I4, BitConverter.ToInt32(initialValue, j * 4));
						}
						return true;
					}
					return false;
				case TypeCode.Int64:
				case TypeCode.UInt64:
					if (initialValue.Length == output.Length * 8) {
						for (int j = 0; j < output.Length; j++) {
							output[j] = new ILExpression(ILCode.Ldc_I8, BitConverter.ToInt64(initialValue, j * 8));
						}
						return true;
					}
					return false;
				case TypeCode.Single:
					if (initialValue.Length == output.Length * 4) {
						for (int j = 0; j < output.Length; j++) {
							output[j] = new ILExpression(ILCode.Ldc_R4, BitConverter.ToSingle(initialValue, j * 4));
						}
						return true;
					}
					return false;
				case TypeCode.Double:
					if (initialValue.Length == output.Length * 8) {
						for (int j = 0; j < output.Length; j++) {
							output[j] = new ILExpression(ILCode.Ldc_R8, BitConverter.ToDouble(initialValue, j * 8));
						}
						return true;
					}
					return false;
				default:
					return false;
			}
		}
		#endregion
		
		#region Collection Initilializers
		public void TransformCollectionInitializers(ILBlock block, ref int i)
		{
			ILVariable v;
			ILExpression expr;
			if (!block.Body[i].Match(ILCode.Stloc, out v, out expr) || expr.Code != ILCode.Newobj)
				return;
			MethodReference ctor = (MethodReference)expr.Operand;
			TypeDefinition td = ctor.DeclaringType.Resolve();
			if (td == null || !td.Interfaces.Any(intf => intf.Name == "IEnumerable" && intf.Namespace == "System.Collections"))
				return;
			// This is a collection: we can convert Add() calls into a collection initializer
			ILExpression collectionInitializer = new ILExpression(ILCode.InitCollection, null, expr);
			for (int j = i + 1; j < block.Body.Count; j++) {
				MethodReference addMethod;
				List<ILExpression> args;
				if (!block.Body[j].Match(ILCode.Callvirt, out addMethod, out args))
					break;
				if (addMethod.Name != "Add" || !addMethod.HasThis || args.Count < 2 || args[0].Code != ILCode.Ldloc || args[0].Operand != v)
					break;
				collectionInitializer.Arguments.Add((ILExpression)block.Body[j]);
			}
			// ensure we added at least one additional arg to the collection initializer:
			if (collectionInitializer.Arguments.Count == 1)
				return;
			ILInlining inline = new ILInlining(method);
			ILExpression followingExpr = block.Body.ElementAtOrDefault(i + collectionInitializer.Arguments.Count) as ILExpression;
			if (inline.CanInlineInto(followingExpr, v, collectionInitializer)) {
				block.Body.RemoveRange(i + 1, collectionInitializer.Arguments.Count - 1);
				((ILExpression)block.Body[i]).Arguments[0] = collectionInitializer;
				
				// Change add methods into InitCollectionAddMethod:
				for (int j = 1; j < collectionInitializer.Arguments.Count; j++) {
					collectionInitializer.Arguments[j].Arguments.RemoveAt(0);
					collectionInitializer.Arguments[j].Code = ILCode.InitCollectionAddMethod;
				}
				
				inline = new ILInlining(method); // refresh variable usage info
				if (inline.InlineIfPossible(block, ref i))
					i++; // retry transformations on the new combined instruction
			}
		}
		#endregion
	}
}
