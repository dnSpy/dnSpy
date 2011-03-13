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
		
		public InitializerPeepholeTransforms(ILBlock method)
		{
			this.method = method;
		}
		
		public void TransformArrayInitializers(ILBlock block, ref int i)
		{
			ILVariable v, v2, v3;
			ILExpression newarrExpr;
			TypeReference arrayType;
			ILExpression lengthExpr;
			int arrayLength;
			if (block.Body[i].Match(ILCode.Stloc, out v, out newarrExpr) &&
			    newarrExpr.Match(ILCode.Newarr, out arrayType, out lengthExpr) &&
			    lengthExpr.Match(ILCode.Ldc_I4, out arrayLength) &&
			    arrayLength > 0)
			{
				MethodReference methodRef;
				ILExpression methodArg1;
				ILExpression methodArg2;
				FieldDefinition field;
				if (block.Body.ElementAtOrDefault(i + 1).Match(ILCode.Call, out methodRef, out methodArg1, out methodArg2) &&
				    methodRef.DeclaringType.FullName == "System.Runtime.CompilerServices.RuntimeHelpers" &&
				    methodRef.Name == "InitializeArray" &&
				    methodArg1.Match(ILCode.Ldloc, out v2) &&
				    v == v2 &&
				    methodArg2.Match(ILCode.Ldtoken, out field) &&
				    field != null && field.InitialValue != null)
				{
					ILExpression[] newArr = new ILExpression[arrayLength];
					if (DecodeArrayInitializer(TypeAnalysis.GetTypeCode(arrayType), field.InitialValue, newArr)) {
						block.Body[i] = new ILExpression(ILCode.Stloc, v, new ILExpression(ILCode.InitArray, arrayType, newArr));
						block.Body.RemoveAt(i + 1);
						i -= new ILInlining(method).InlineInto(block, i + 1, aggressive: true) - 1;
						return;
					}
				}
				
				const int maxConsecutiveDefaultValueExpressions = 10;
				List<ILExpression> operands = new List<ILExpression>();
				int numberOfInstructionsToRemove = 0;
				for (int j = i + 1; j < block.Body.Count; j++) {
					ILExpression expr = block.Body[j] as ILExpression;
					int pos;
					if (expr != null &&
					    IsStoreToArray(expr.Code) &&
					    expr.Arguments[0].Match(ILCode.Ldloc, out v3) &&
					    v == v3 &&
					    expr.Arguments[1].Match(ILCode.Ldc_I4, out pos) &&
					    pos >= operands.Count &&
					    pos <= operands.Count + maxConsecutiveDefaultValueExpressions)
					{
						while (operands.Count < pos)
							operands.Add(new ILExpression(ILCode.DefaultValue, arrayType));
						operands.Add(expr.Arguments[2]);
						numberOfInstructionsToRemove++;
					} else {
						break;
					}
				}
				if (operands.Count == arrayLength) {
					((ILExpression)block.Body[i]).Arguments[0] = new ILExpression(ILCode.InitArray, arrayType, operands);
					block.Body.RemoveRange(i + 1, numberOfInstructionsToRemove);
					i -= new ILInlining(method).InlineInto(block, i + 1, aggressive: true) - 1;
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
