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
			ILVariable v, v3;
			ILExpression newarrExpr;
			TypeReference elementType;
			ILExpression lengthExpr;
			int arrayLength;
			if (expr.Match(ILCode.Stloc, out v, out newarrExpr) &&
			    newarrExpr.Match(ILCode.Newarr, out elementType, out lengthExpr) &&
			    lengthExpr.Match(ILCode.Ldc_I4, out arrayLength) &&
			    arrayLength > 0) {
				ILExpression[] newArr;
				int initArrayPos;
				if (ForwardScanInitializeArrayRuntimeHelper(body, pos + 1, v, elementType, arrayLength, out newArr, out initArrayPos)) {
					var arrayType = new ArrayType(elementType, 1);
					arrayType.Dimensions[0] = new ArrayDimension(0, arrayLength);
					body[pos] = new ILExpression(ILCode.Stloc, v, new ILExpression(ILCode.InitArray, arrayType, newArr));
					body.RemoveAt(initArrayPos);
				}
				// Put in a limit so that we don't consume too much memory if the code allocates a huge array
				// and populates it extremely sparsly. However, 255 "null" elements in a row actually occur in the Mono C# compiler!
				const int maxConsecutiveDefaultValueExpressions = 300;
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
					    arrayPos <= operands.Count + maxConsecutiveDefaultValueExpressions &&
					    !nextExpr.Arguments[2].ContainsReferenceTo(v3))
					{
						while (operands.Count < arrayPos)
							operands.Add(new ILExpression(ILCode.DefaultValue, elementType));
						operands.Add(nextExpr.Arguments[2]);
						numberOfInstructionsToRemove++;
					} else {
						break;
					}
				}
				if (operands.Count == arrayLength) {
					var arrayType = new ArrayType(elementType, 1);
					arrayType.Dimensions[0] = new ArrayDimension(0, arrayLength);
					expr.Arguments[0] = new ILExpression(ILCode.InitArray, arrayType, operands);
					body.RemoveRange(pos + 1, numberOfInstructionsToRemove);

					new ILInlining(method).InlineIfPossible(body, ref pos);
					return true;
				}
			}
			return false;
		}

		bool TransformMultidimensionalArrayInitializers(List<ILNode> body, ILExpression expr, int pos)
		{
			ILVariable v;
			ILExpression newarrExpr;
			MethodReference ctor;
			List<ILExpression> ctorArgs;
			ArrayType arrayType;
			if (expr.Match(ILCode.Stloc, out v, out newarrExpr) &&
			    newarrExpr.Match(ILCode.Newobj, out ctor, out ctorArgs) &&
			    (arrayType = (ctor.DeclaringType as ArrayType)) != null &&
			    arrayType.Rank == ctorArgs.Count) {
				// Clone the type, so we can muck about with the Dimensions
				arrayType = new ArrayType(arrayType.ElementType, arrayType.Rank);
				var arrayLengths = new int[arrayType.Rank];
				for (int i = 0; i < arrayType.Rank; i++) {
					if (!ctorArgs[i].Match(ILCode.Ldc_I4, out arrayLengths[i])) return false;
					if (arrayLengths[i] <= 0) return false;
					arrayType.Dimensions[i] = new ArrayDimension(0, arrayLengths[i]);
				}

				var totalElements = arrayLengths.Aggregate(1, (t, l) => t * l);
				ILExpression[] newArr;
				int initArrayPos;
				if (ForwardScanInitializeArrayRuntimeHelper(body, pos + 1, v, arrayType, totalElements, out newArr, out initArrayPos)) {
					body[pos] = new ILExpression(ILCode.Stloc, v, new ILExpression(ILCode.InitArray, arrayType, newArr));
					body.RemoveAt(initArrayPos);
					return true;
				}
			}
			return false;
		}

		bool ForwardScanInitializeArrayRuntimeHelper(List<ILNode> body, int pos, ILVariable array, TypeReference arrayType, int arrayLength, out ILExpression[] values, out int foundPos)
		{
			ILVariable v2;
			MethodReference methodRef;
			ILExpression methodArg1;
			ILExpression methodArg2;
			FieldReference fieldRef;
			if (body.ElementAtOrDefault(pos).Match(ILCode.Call, out methodRef, out methodArg1, out methodArg2) &&
			    methodRef.DeclaringType.FullName == "System.Runtime.CompilerServices.RuntimeHelpers" &&
			    methodRef.Name == "InitializeArray" &&
			    methodArg1.Match(ILCode.Ldloc, out v2) &&
			    array == v2 &&
			    methodArg2.Match(ILCode.Ldtoken, out fieldRef))
			{
				FieldDefinition fieldDef = fieldRef.ResolveWithinSameModule();
				if (fieldDef != null && fieldDef.InitialValue != null) {
					ILExpression[] newArr = new ILExpression[arrayLength];
					if (DecodeArrayInitializer(arrayType.GetElementType(), fieldDef.InitialValue, newArr))
					{
						values = newArr;
						foundPos = pos;
						return true;
					}
				}
			}
			values = null;
			foundPos = -1;
			return false;
		}

		static bool DecodeArrayInitializer(TypeReference elementTypeRef, byte[] initialValue, ILExpression[] output)
		{
			TypeCode elementType = TypeAnalysis.GetTypeCode(elementTypeRef);
			switch (elementType) {
				case TypeCode.Boolean:
				case TypeCode.Byte:
					return DecodeArrayInitializer(initialValue, output, elementType, (d, i) => (int)d[i]);
				case TypeCode.SByte:
					return DecodeArrayInitializer(initialValue, output, elementType, (d, i) => (int)unchecked((sbyte)d[i]));
				case TypeCode.Int16:
					return DecodeArrayInitializer(initialValue, output, elementType, (d, i) => (int)BitConverter.ToInt16(d, i));
				case TypeCode.Char:
				case TypeCode.UInt16:
					return DecodeArrayInitializer(initialValue, output, elementType, (d, i) => (int)BitConverter.ToUInt16(d, i));
				case TypeCode.Int32:
				case TypeCode.UInt32:
					return DecodeArrayInitializer(initialValue, output, elementType, BitConverter.ToInt32);
				case TypeCode.Int64:
				case TypeCode.UInt64:
					return DecodeArrayInitializer(initialValue, output, elementType, BitConverter.ToInt64);
				case TypeCode.Single:
					return DecodeArrayInitializer(initialValue, output, elementType, BitConverter.ToSingle);
				case TypeCode.Double:
					return DecodeArrayInitializer(initialValue, output, elementType, BitConverter.ToDouble);
				case TypeCode.Object:
					var typeDef = elementTypeRef.ResolveWithinSameModule();
					if (typeDef != null && typeDef.IsEnum)
						return DecodeArrayInitializer(typeDef.GetEnumUnderlyingType(), initialValue, output);

					return false;
				default:
					return false;
			}
		}

		static bool DecodeArrayInitializer<T>(byte[] initialValue, ILExpression[] output, TypeCode elementType, Func<byte[], int, T> decoder)
		{
			int elementSize = ElementSizeOf(elementType);
			if (initialValue.Length < (output.Length * elementSize))
				return false;

			ILCode code = LoadCodeFor(elementType);
			for (int i = 0; i < output.Length; i++)
				output[i] = new ILExpression(code, decoder(initialValue, i * elementSize));

			return true;
		}

		private static ILCode LoadCodeFor(TypeCode elementType)
		{
			switch (elementType) {
				case TypeCode.Boolean:
				case TypeCode.Byte:
				case TypeCode.SByte:
				case TypeCode.Char:
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.Int32:
				case TypeCode.UInt32:
					return ILCode.Ldc_I4;
				case TypeCode.Int64:
				case TypeCode.UInt64:
					return ILCode.Ldc_I8;
				case TypeCode.Single:
					return ILCode.Ldc_R4;
				case TypeCode.Double:
					return ILCode.Ldc_R8;
				default:
					throw new ArgumentOutOfRangeException("elementType");					
			}
		}

		private static int ElementSizeOf(TypeCode elementType)
		{
			switch (elementType) {
				case TypeCode.Boolean:
				case TypeCode.Byte:
				case TypeCode.SByte:
					return 1;
				case TypeCode.Char:
				case TypeCode.Int16:
				case TypeCode.UInt16:
					return 2;
				case TypeCode.Int32:
				case TypeCode.UInt32:
				case TypeCode.Single:
					return 4;
				case TypeCode.Int64:
				case TypeCode.UInt64:
				case TypeCode.Double:
					return 8;
				default:
					throw new ArgumentOutOfRangeException("elementType");
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
			TypeReference newObjType;
			bool isValueType;
			MethodReference ctor;
			List<ILExpression> ctorArgs;
			if (expr.Match(ILCode.Stloc, out v, out newObjExpr)) {
				if (newObjExpr.Match(ILCode.Newobj, out ctor, out ctorArgs)) {
					// v = newObj(ctor, ctorArgs)
					newObjType = ctor.DeclaringType;
					isValueType = false;
				} else if (newObjExpr.Match(ILCode.DefaultValue, out newObjType)) {
					// v = defaultvalue(type)
					isValueType = true;
				} else {
					return false;
				}
			} else if (expr.Match(ILCode.Call, out ctor, out ctorArgs)) {
				// call(SomeStruct::.ctor, ldloca(v), remainingArgs)
				if (ctorArgs.Count > 0 && ctorArgs[0].Match(ILCode.Ldloca, out v)) {
					isValueType = true;
					newObjType = ctor.DeclaringType;
					ctorArgs = new List<ILExpression>(ctorArgs);
					ctorArgs.RemoveAt(0);
					newObjExpr = new ILExpression(ILCode.Newobj, ctor, ctorArgs);
				} else {
					return false;
				}
			} else {
				return false;
			}
			if (newObjType.IsValueType != isValueType)
				return false;
			
			int originalPos = pos;

			// don't use object initializer syntax for closures
			if (Ast.Transforms.DelegateConstruction.IsPotentialClosure(context, newObjType.ResolveWithinSameModule()))
				return false;

			ILExpression initializer = ParseObjectInitializer(body, ref pos, v, newObjExpr, IsCollectionType(newObjType), isValueType);

			if (initializer.Arguments.Count == 1) // only newobj argument, no initializer elements
				return false;
			int totalElementCount = pos - originalPos - 1; // totalElementCount: includes elements from nested collections
			Debug.Assert(totalElementCount >= initializer.Arguments.Count - 1);

			// Verify that we can inline 'v' into the next instruction:

			if (pos >= body.Count)
				return false; // reached end of block, but there should be another instruction which consumes the initialized object

			ILInlining inlining = new ILInlining(method);
			if (isValueType) {
				// one ldloc for the use of the initialized object
				if (inlining.numLdloc.GetOrDefault(v) != 1)
					return false;
				// one ldloca for each initializer argument, and also for the ctor call (if it exists)
				if (inlining.numLdloca.GetOrDefault(v) != totalElementCount + (expr.Code == ILCode.Call ? 1 : 0))
					return false;
				// one stloc for the initial store (if no ctor call was used)
				if (inlining.numStloc.GetOrDefault(v) != (expr.Code == ILCode.Call ? 0 : 1))
					return false;
			} else {
				// one ldloc for each initializer argument, and another ldloc for the use of the initialized object
				if (inlining.numLdloc.GetOrDefault(v) != totalElementCount + 1)
					return false;
				if (!(inlining.numStloc.GetOrDefault(v) == 1 && inlining.numLdloca.GetOrDefault(v) == 0))
					return false;
			}
			ILExpression nextExpr = body[pos] as ILExpression;
			if (!inlining.CanInlineInto(nextExpr, v, initializer))
				return false;

			if (expr.Code == ILCode.Stloc) {
				expr.Arguments[0] = initializer;
			} else {
				Debug.Assert(expr.Code == ILCode.Call);
				expr.Code = ILCode.Stloc;
				expr.Operand = v;
				expr.Arguments.Clear();
				expr.Arguments.Add(initializer);
			}
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
			while (td != null) {
				if (td.Interfaces.Any(intf => intf.Name == "IEnumerable" && intf.Namespace == "System.Collections"))
					return true;
				td = td.BaseType != null ? td.BaseType.Resolve() : null;
			}
			return false;
		}

		/// <summary>
		/// Gets whether 'expr' represents a setter in an object initializer.
		/// ('CallvirtSetter(Property, v, value)')
		/// </summary>
		static bool IsSetterInObjectInitializer(ILExpression expr)
		{
			if (expr == null)
				return false;
			if (expr.Code == ILCode.CallvirtSetter || expr.Code == ILCode.CallSetter || expr.Code == ILCode.Stfld) {
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
			if (expr.Match(ILCode.Callvirt, out addMethod, out args) || expr.Match(ILCode.Call, out addMethod, out args)) {
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
		ILExpression ParseObjectInitializer(List<ILNode> body, ref int pos, ILVariable v, ILExpression newObjExpr, bool isCollection, bool isValueType)
		{
			// Take care not to modify any existing ILExpressions in here.
			// We just construct new ones around the old ones, any modifications must wait until the whole
			// object/collection initializer was analyzed.
			ILExpression objectInitializer = new ILExpression(isCollection ? ILCode.InitCollection : ILCode.InitObject, null, newObjExpr);
			List<ILExpression> initializerStack = new List<ILExpression>();
			initializerStack.Add(objectInitializer);
			while (++pos < body.Count) {
				ILExpression nextExpr = body[pos] as ILExpression;
				if (IsSetterInObjectInitializer(nextExpr)) {
					if (!AdjustInitializerStack(initializerStack, nextExpr.Arguments[0], v, false, isValueType)) {
						CleanupInitializerStackAfterFailedAdjustment(initializerStack);
						break;
					}
					initializerStack[initializerStack.Count - 1].Arguments.Add(nextExpr);
				} else if (IsAddMethodCall(nextExpr)) {
					if (!AdjustInitializerStack(initializerStack, nextExpr.Arguments[0], v, true, isValueType)) {
						CleanupInitializerStackAfterFailedAdjustment(initializerStack);
						break;
					}
					initializerStack[initializerStack.Count - 1].Arguments.Add(nextExpr);
				} else {
					// can't match any more initializers: end of object initializer
					break;
				}
			}
			return objectInitializer;
		}

		static bool AdjustInitializerStack(List<ILExpression> initializerStack, ILExpression argument, ILVariable v, bool isCollection, bool isValueType)
		{
			// Argument is of the form 'getter(getter(...(v)))'
			// Unpack it into a list of getters:
			List<ILExpression> getters = new List<ILExpression>();
			while (argument.Code == ILCode.CallvirtGetter || argument.Code == ILCode.CallGetter || argument.Code == ILCode.Ldfld) {
				getters.Add(argument);
				if (argument.Arguments.Count != 1)
					return false;
				argument = argument.Arguments[0];
			}
			// Ensure that the final argument is 'v'
			if (isValueType) {
				ILVariable loadedVar;
				if (!(argument.Match(ILCode.Ldloca, out loadedVar) && loadedVar == v))
					return false;
			} else {
				if (!argument.MatchLdloc(v))
					return false;
			}
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

		static void CleanupInitializerStackAfterFailedAdjustment(List<ILExpression> initializerStack)
		{
			// There might be empty nested initializers left over; so we'll remove those:
			while (initializerStack.Count > 1 && initializerStack[initializerStack.Count - 1].Arguments.Count == 1) {
				ILExpression parent = initializerStack[initializerStack.Count - 2];
				Debug.Assert(parent.Arguments.Last() == initializerStack[initializerStack.Count - 1]);
				parent.Arguments.RemoveAt(parent.Arguments.Count - 1);
				initializerStack.RemoveAt(initializerStack.Count - 1);
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
