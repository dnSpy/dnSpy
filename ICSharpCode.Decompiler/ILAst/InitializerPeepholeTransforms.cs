// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mono.Cecil;

namespace ICSharpCode.Decompiler.ILAst
{
	/// <summary>
	/// IL AST transformation that introduces array, object and collection initializers.
	/// </summary>
	partial class ILAstOptimizer
	{
		#region Array Initializers
		bool TransformArrayInitializers(List<ILNode> body, ILExpression expr, int pos)
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
		#endregion
		
		/// <summary>
		/// Handles both object and collection initializers.
		/// </summary>
		bool TransformObjectInitializers(List<ILNode> body, ILExpression expr, int pos)
		{
			Debug.Assert(body[pos] == expr); // should be called for top-level expressions only
			ILVariable v;
			ILExpression newObjExpr;
			MethodReference ctor;
			List<ILExpression> ctorArgs;
			// v = newObj(ctor, ctorArgs)
			if (!(expr.Match(ILCode.Stloc, out v, out newObjExpr) && newObjExpr.Match(ILCode.Newobj, out ctor, out ctorArgs)))
				return false;
			int originalPos = pos;
			ILInlining inlining = null;
			ILExpression initializer;
			if (IsCollectionType(ctor.DeclaringType)) {
				// Collection Initializer
				initializer = ParseCollectionInitializer(body, ref pos, v, newObjExpr);
				if (initializer.Arguments.Count == 1) // only newobj argument, no initializer elements
					return false;
			} else {
				// Object Initializer
				
				// don't use object initializer syntax for closures
				if (Ast.Transforms.DelegateConstruction.IsPotentialClosure(context, ctor.DeclaringType.ResolveWithinSameModule()))
					return false;
				
				initializer = new ILExpression(ILCode.InitObject, null, newObjExpr);
				pos++;
				while (pos < body.Count) {
					ILExpression nextExpr = body[pos] as ILExpression;
					if (IsSetterInObjectInitializer(nextExpr, v)) {
						initializer.Arguments.Add(nextExpr);
						pos++;
					} else {
						ILVariable collectionVar;
						ILExpression getCollection;
						if (IsCollectionGetterInObjectInitializer(nextExpr, v, out collectionVar, out getCollection)) {
							int posBeforeCollectionInitializer = pos;
							ILExpression collectionInitializer = ParseCollectionInitializer(body, ref pos, collectionVar, getCollection);
							// Validate that the 'collectionVar' is not read (except in the initializers)
							if (inlining == null) {
								// instantiate ILInlining only once - we don't change the method while analysing the object initializer
								inlining = new ILInlining(method);
							}
							if (inlining.numLdloc.GetOrDefault(collectionVar) == collectionInitializer.Arguments.Count
							    && inlining.numStloc.GetOrDefault(collectionVar) == 1
							    && inlining.numLdloca.GetOrDefault(collectionVar) == 0)
							{
								initializer.Arguments.Add(collectionInitializer);
								// no need to increment 'pos' here: ParseCollectionInitializer already did that
							} else {
								// Consider the object initializer to have ended in front of the 'collection getter' instruction.
								pos = posBeforeCollectionInitializer;
								break;
							}
						} else {
							// can't match any more initializers: end of object initializer
							break;
						}
					}
				}
				if (initializer.Arguments.Count == 1)
					return false; // no initializers were matched (the single argument is the newobjExpr)
			}
			// Verify that we can inline 'v' into the next instruction:
			if (!CanInlineInitializer(body, pos, v, initializer, inlining ?? new ILInlining(method)))
				return false;
			
			expr.Arguments[0] = initializer;
			// remove all the instructions that were pulled into the initializer
			body.RemoveRange(originalPos + 1, pos - originalPos - 1);
			
			// now that we know that it's an object initializer, change all the first arguments to 'InitializedObject'
			ChangeFirstArgumentToInitializedObject(initializer);
			
			return true;
		}
		
		/// <summary>
		/// Gets whether the type supports collection initializers.
		/// </summary>
		static bool IsCollectionType(TypeReference tr)
		{
			if (tr == null)
				return false;
			TypeDefinition td = tr.Resolve();
			return td != null && td.Interfaces.Any(intf => intf.Name == "IEnumerable" && intf.Namespace == "System.Collections");
		}
		
		/// <summary>
		/// Gets whether 'expr' represents a setter in an object initializer.
		/// ('CallvirtSetter(Property, v, value)')
		/// </summary>
		/// <param name="expr">The expression to test</param>
		/// <param name="v">The variable that contains the object being initialized</param>
		static bool IsSetterInObjectInitializer(ILExpression expr, ILVariable v)
		{
			if (expr == null)
				return false;
			if (expr.Code == ILCode.CallvirtSetter || expr.Code == ILCode.Stfld) {
				if (expr.Arguments.Count == 2) {
					return expr.Arguments[0].MatchLdloc(v);
				}
			}
			return false;
		}
		
		/// <summary>
		/// Gets whether 'expr' represents getting a collection in an object initializer.
		/// ('collectionVar = callvirtGetter(Property, v)')
		/// </summary>
		/// <param name="expr">The expression to test</param>
		/// <param name="v">The variable that contains the object being initialized</param>
		static bool IsCollectionGetterInObjectInitializer(ILExpression expr, ILVariable v, out ILVariable collectionVar, out ILExpression init)
		{
			if (expr.Match(ILCode.Stloc, out collectionVar, out init)) {
				if (init.Code == ILCode.Ldfld || init.Code == ILCode.CallvirtGetter) {
					if (init.Arguments.Count == 1 && init.Arguments[0].MatchLdloc(v)) {
						MemberReference mr = (MemberReference)init.Operand;
						return IsCollectionType(mr.DeclaringType);
					}
				}
			}
			return false;
		}
		
		/// <summary>
		/// Parses a collection initializer.
		/// </summary>
		/// <param name="body">ILAst block</param>
		/// <param name="pos">
		/// Input: position of the instruction assigning to 'v'.
		/// Output: first position after the collection initializer
		/// </param>
		/// <param name="v">The variable that holds the collection</param>
		/// <param name="getCollection">The initial value of the collection (newobj instruction)</param>
		/// <returns>InitCollection instruction; or null if parsing failed</returns>
		ILExpression ParseCollectionInitializer(List<ILNode> body, ref int pos, ILVariable v, ILExpression getCollection)
		{
			Debug.Assert(((ILExpression)body[pos]).Code == ILCode.Stloc);
			ILExpression collectionInitializer = new ILExpression(ILCode.InitCollection, null, getCollection);
			// Take care not to modify any existing ILExpressions in here.
			// We just construct new ones around the old ones, any modifications must wait until the whole
			// object/collection initializer was analyzed.
			while (++pos < body.Count) {
				ILExpression nextExpr = body[pos] as ILExpression;
				MethodReference addMethod;
				List<ILExpression> args;
				if (nextExpr.Match(ILCode.Callvirt, out addMethod, out args)) {
					if (addMethod.Name == "Add" && addMethod.HasThis) {
						if (args.Count >= 2 && args[0].MatchLdloc(v)) {
							collectionInitializer.Arguments.Add(nextExpr);
						} else {
							break;
						}
					} else {
						break;
					}
				} else {
					break;
				}
			}
			return collectionInitializer;
		}
		
		static bool CanInlineInitializer(List<ILNode> body, int pos, ILVariable v, ILExpression initializer, ILInlining inlining)
		{
			if (pos >= body.Count)
				return false; // reached end of block, but there should be another instruction which consumes the initialized object
			// one ldloc for each initializer argument except for the newobj, and another ldloc for the use of the initialized object
			if (inlining.numLdloc.GetOrDefault(v) != initializer.Arguments.Count)
				return false;
			if (!(inlining.numStloc.GetOrDefault(v) == 1 && inlining.numLdloca.GetOrDefault(v) == 0))
				return false;
			ILExpression nextExpr = body[pos] as ILExpression;
			return inlining.CanInlineInto(nextExpr, v, initializer);
		}
		
		static void ChangeFirstArgumentToInitializedObject(ILExpression initializer)
		{
			// Go through all elements in the initializer (so skip the newobj-instr. at the start)
			for (int i = 1; i < initializer.Arguments.Count; i++) {
				ILExpression element = initializer.Arguments[i];
				ILExpression arg;
				if (element.Code == ILCode.InitCollection) {
					// nested collection initializer
					ILExpression getCollection = element.Arguments[0];
					arg = getCollection.Arguments[0];
					ChangeFirstArgumentToInitializedObject(element); // handle the collection elements
				} else {
					arg = element.Arguments[0];
				}
				Debug.Assert(arg.Code == ILCode.Ldloc);
				arg.Code = ILCode.InitializedObject;
				arg.Operand = null;
			}
		}
	}
}
