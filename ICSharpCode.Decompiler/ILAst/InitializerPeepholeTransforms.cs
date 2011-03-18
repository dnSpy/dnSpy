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
	public class Initializers
	{
		public static bool TransformArrayInitializers(List<ILNode> body, ILExpression expr, int pos)
		{
			ILVariable v, v2, v3;
			ILExpression newarrExpr;
			TypeReference arrayType;
			ILExpression lengthExpr;
			int arrayLength;
			if (expr.Match(ILCode.Stloc, out v, out newarrExpr) &&
			    newarrExpr.Match(ILCode.Newarr, out arrayType, out lengthExpr) &&
			    lengthExpr.Match(ILCode.Ldc_I4, out arrayLength) &&
			    arrayLength > 0)
			{
				MethodReference methodRef;
				ILExpression methodArg1;
				ILExpression methodArg2;
				FieldDefinition field;
				if (body.ElementAtOrDefault(pos + 1).Match(ILCode.Call, out methodRef, out methodArg1, out methodArg2) &&
				    methodRef.DeclaringType.FullName == "System.Runtime.CompilerServices.RuntimeHelpers" &&
				    methodRef.Name == "InitializeArray" &&
				    methodArg1.Match(ILCode.Ldloc, out v2) &&
				    v == v2 &&
				    methodArg2.Match(ILCode.Ldtoken, out field) &&
				    field != null && field.InitialValue != null)
				{
					ILExpression[] newArr = new ILExpression[arrayLength];
					if (DecodeArrayInitializer(TypeAnalysis.GetTypeCode(arrayType), field.InitialValue, newArr)) {
						body[pos] = new ILExpression(ILCode.Stloc, v, new ILExpression(ILCode.InitArray, arrayType, newArr));
						body.RemoveAt(pos + 1);
						return true;
					}
				}
				
				const int maxConsecutiveDefaultValueExpressions = 10;
				List<ILExpression> operands = new List<ILExpression>();
				int numberOfInstructionsToRemove = 0;
				for (int j = pos + 1; j < body.Count; j++) {
					ILExpression nextExpr = body[j] as ILExpression;
					int arrayPos;
					if (nextExpr != null &&
					    nextExpr.Code.IsStoreToArray() &&
					    nextExpr.Arguments[0].Match(ILCode.Ldloc, out v3) &&
					    v == v3 &&
					    nextExpr.Arguments[1].Match(ILCode.Ldc_I4, out arrayPos) &&
					    arrayPos >= operands.Count &&
					    arrayPos <= operands.Count + maxConsecutiveDefaultValueExpressions)
					{
						while (operands.Count < arrayPos)
							operands.Add(new ILExpression(ILCode.DefaultValue, arrayType));
						operands.Add(nextExpr.Arguments[2]);
						numberOfInstructionsToRemove++;
					} else {
						break;
					}
				}
				if (operands.Count == arrayLength) {
					expr.Arguments[0] = new ILExpression(ILCode.InitArray, arrayType, operands);
					body.RemoveRange(pos + 1, numberOfInstructionsToRemove);
					return true;
				}
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
		
		public static bool TransformCollectionInitializers(List<ILNode> body, ILExpression expr, int pos)
		{
			ILVariable v, v2;
			ILExpression newObjExpr;
			MethodReference ctor;
			List<ILExpression> ctorArgs;
			if (expr.Match(ILCode.Stloc, out v, out newObjExpr) &&
			    newObjExpr.Match(ILCode.Newobj, out ctor, out ctorArgs))
			{
				TypeDefinition td = ctor.DeclaringType.Resolve();
				if (td == null || !td.Interfaces.Any(intf => intf.Name == "IEnumerable" && intf.Namespace == "System.Collections"))
					return false;
				
				// This is a collection: we can convert Add() calls into a collection initializer
				ILExpression collectionInitializer = new ILExpression(ILCode.InitCollection, null, newObjExpr);
				bool anyAdded = false;
				while(pos + 1 < body.Count) {
					ILExpression nextExpr = body[pos + 1] as ILExpression;
					MethodReference addMethod;
					List<ILExpression> args;
					if (nextExpr.Match(ILCode.Callvirt, out addMethod, out args) &&
					    addMethod.Name == "Add" &&
					    addMethod.HasThis &&
					    args.Count == 2 &&
					    args[0].Match(ILCode.Ldloc, out v2) &&
					    v == v2)
					{
						nextExpr.Code = ILCode.InitCollectionAddMethod;
						nextExpr.Arguments.RemoveAt(0);
						collectionInitializer.Arguments.Add(nextExpr);
						body.RemoveAt(pos + 1);
						anyAdded = true;
					} else {
						break;
					}
				}
				// ensure we added at least one additional arg to the collection initializer:
				if (anyAdded) {
					expr.Arguments[0] = collectionInitializer;
					return true;
				}
			}
			return false;
		}
	}
}
