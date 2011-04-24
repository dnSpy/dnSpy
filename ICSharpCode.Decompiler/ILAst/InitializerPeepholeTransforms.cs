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
			if (!context.Settings.ObjectOrCollectionInitializers)
				return false;
			
			Debug.Assert(body[pos] == expr); // should be called for top-level expressions only
			ILVariable v;
			ILExpression newObjExpr;
			MethodReference ctor;
			List<ILExpression> ctorArgs;
			// v = newObj(ctor, ctorArgs)
			if (!(expr.Match(ILCode.Stloc, out v, out newObjExpr) && newObjExpr.Match(ILCode.Newobj, out ctor, out ctorArgs)))
				return false;
			int originalPos = pos;

			// don't use object initializer syntax for closures
			if (Ast.Transforms.DelegateConstruction.IsPotentialClosure(context, ctor.DeclaringType.ResolveWithinSameModule()))
				return false;
			
			ILExpression initializer = ParseObjectInitializer(body, ref pos, v, newObjExpr, IsCollectionType(ctor.DeclaringType));
			
			if (initializer.Arguments.Count == 1) // only newobj argument, no initializer elements
				return false;
			int totalElementCount = pos - originalPos - 1; // totalElementCount: includes elements from nested collections
			Debug.Assert(totalElementCount >= initializer.Arguments.Count - 1);
			
			// Verify that we can inline 'v' into the next instruction:
			
			if (pos >= body.Count)
				return false; // reached end of block, but there should be another instruction which consumes the initialized object
			
			ILInlining inlining = new ILInlining(method);
			// one ldloc for each initializer argument, and another ldloc for the use of the initialized object
			if (inlining.numLdloc.GetOrDefault(v) != totalElementCount + 1)
				return false;
			if (!(inlining.numStloc.GetOrDefault(v) == 1 && inlining.numLdloca.GetOrDefault(v) == 0))
				return false;
			ILExpression nextExpr = body[pos] as ILExpression;
			if (!inlining.CanInlineInto(nextExpr, v, initializer))
				return false;
			
			expr.Arguments[0] = initializer;
			// remove all the instructions that were pulled into the initializer
			body.RemoveRange(originalPos + 1, pos - originalPos - 1);
			
			// now that we know that it's an object initializer, change all the first arguments to 'InitializedObject'
			ChangeFirstArgumentToInitializedObject(initializer);
			
			inlining = new ILInlining(method);
			inlining.InlineIfPossible(body, ref originalPos);
			
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
		static bool IsSetterInObjectInitializer(ILExpression expr)
		{
			if (expr == null)
				return false;
			if (expr.Code == ILCode.CallvirtSetter || expr.Code == ILCode.Stfld) {
				return expr.Arguments.Count == 2;
			}
			return false;
		}
		
		/// <summary>
		/// Gets whether 'expr' represents the invocation of an 'Add' method in a collection initializer.
		/// </summary>
		static bool IsAddMethodCall(ILExpression expr)
		{
			MethodReference addMethod;
			List<ILExpression> args;
			if (expr.Match(ILCode.Callvirt, out addMethod, out args)) {
				if (addMethod.Name == "Add" && addMethod.HasThis) {
					return args.Count >= 2;
				}
			}
			return false;
		}
		
		/// <summary>
		/// Parses an object initializer.
		/// </summary>
		/// <param name="body">ILAst block</param>
		/// <param name="pos">
		/// Input: position of the instruction assigning to 'v'.
		/// Output: first position after the object initializer
		/// </param>
		/// <param name="v">The variable that holds the object being initialized</param>
		/// <param name="newObjExpr">The newobj instruction</param>
		/// <returns>InitObject instruction</returns>
		ILExpression ParseObjectInitializer(List<ILNode> body, ref int pos, ILVariable v, ILExpression newObjExpr, bool isCollection)
		{
			Debug.Assert(((ILExpression)body[pos]).Code == ILCode.Stloc);
			// Take care not to modify any existing ILExpressions in here.
			// We just construct new ones around the old ones, any modifications must wait until the whole
			// object/collection initializer was analyzed.
			ILExpression objectInitializer = new ILExpression(isCollection ? ILCode.InitCollection : ILCode.InitObject, null, newObjExpr);
			List<ILExpression> initializerStack = new List<ILExpression>();
			initializerStack.Add(objectInitializer);
			while (++pos < body.Count) {
				ILExpression nextExpr = body[pos] as ILExpression;
				if (IsSetterInObjectInitializer(nextExpr)) {
					if (!AdjustInitializerStack(initializerStack, nextExpr.Arguments[0], v, false))
						break;
					initializerStack[initializerStack.Count - 1].Arguments.Add(nextExpr);
				} else if (IsAddMethodCall(nextExpr)) {
					if (!AdjustInitializerStack(initializerStack, nextExpr.Arguments[0], v, true))
						break;
					initializerStack[initializerStack.Count - 1].Arguments.Add(nextExpr);
				} else {
					// can't match any more initializers: end of object initializer
					break;
				}
			}
			return objectInitializer;
		}
		
		static bool AdjustInitializerStack(List<ILExpression> initializerStack, ILExpression argument, ILVariable v, bool isCollection)
		{
			// Argument is of the form 'getter(getter(...(v)))'
			// Unpack it into a list of getters:
			List<ILExpression> getters = new List<ILExpression>();
			while (argument.Code == ILCode.CallvirtGetter || argument.Code == ILCode.Ldfld) {
				getters.Add(argument);
				if (argument.Arguments.Count != 1)
					return false;
				argument = argument.Arguments[0];
			}
			// Ensure that the final argument is 'v'
			if (!argument.MatchLdloc(v))
				return false;
			// Now compare the getters with those that are currently active on the initializer stack:
			int i;
			for (i = 1; i <= Math.Min(getters.Count, initializerStack.Count - 1); i++) {
				ILExpression g1 = initializerStack[i].Arguments[0]; // getter stored in initializer
				ILExpression g2 = getters[getters.Count - i]; // matching getter from argument
				if (g1.Operand != g2.Operand) {
					// operands differ, so we abort the comparison
					break;
				}
			}
			// Remove all initializers from the stack that were not matched with one from the argument:
			initializerStack.RemoveRange(i, initializerStack.Count - i);
			// Now create new initializers for the remaining arguments:
			for (; i <= getters.Count; i++) {
				ILExpression g = getters[getters.Count - i];
				MemberReference mr = (MemberReference)g.Operand;
				TypeReference returnType;
				if (mr is FieldReference)
					returnType = TypeAnalysis.GetFieldType((FieldReference)mr);
				else
					returnType = TypeAnalysis.SubstituteTypeArgs(((MethodReference)mr).ReturnType, mr);
				
				ILExpression nestedInitializer = new ILExpression(
					IsCollectionType(returnType) ? ILCode.InitCollection : ILCode.InitObject,
					null, g);
				// add new initializer to its parent:
				ILExpression parentInitializer = initializerStack[initializerStack.Count - 1];
				if (parentInitializer.Code == ILCode.InitCollection) {
					// can't add children to collection initializer
					if (parentInitializer.Arguments.Count == 1) {
						// convert empty collection initializer to object initializer
						parentInitializer.Code = ILCode.InitObject;
					} else {
						return false;
					}
				}
				parentInitializer.Arguments.Add(nestedInitializer);
				initializerStack.Add(nestedInitializer);
			}
			ILExpression lastInitializer = initializerStack[initializerStack.Count - 1];
			if (isCollection) {
				return lastInitializer.Code == ILCode.InitCollection;
			} else {
				if (lastInitializer.Code == ILCode.InitCollection) {
					if (lastInitializer.Arguments.Count == 1) {
						// convert empty collection initializer to object initializer
						lastInitializer.Code = ILCode.InitObject;
						return true;
					} else {
						return false;
					}
				} else {
					return true;
				}
			}
		}
		
		static void ChangeFirstArgumentToInitializedObject(ILExpression initializer)
		{
			// Go through all elements in the initializer (so skip the newobj-instr. at the start)
			for (int i = 1; i < initializer.Arguments.Count; i++) {
				ILExpression element = initializer.Arguments[i];
				if (element.Code == ILCode.InitCollection || element.Code == ILCode.InitObject) {
					// nested collection/object initializer
					ILExpression getCollection = element.Arguments[0];
					getCollection.Arguments[0] = new ILExpression(ILCode.InitializedObject, null);
					ChangeFirstArgumentToInitializedObject(element); // handle the collection elements
				} else {
					element.Arguments[0] = new ILExpression(ILCode.InitializedObject, null);
				}
			}
		}
	}
}
