// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.ConsistencyCheck
{
	public class RandomizedOrderResolverTest
	{
		static Random sharedRnd = new Random();
		CSharpAstResolver resolver;
		CSharpAstResolver resolveAllResolver;
		
		public static void RunTest(CSharpFile file)
		{
			int seed;
			lock (sharedRnd) {
				seed = sharedRnd.Next();
			}
			Random rnd = new Random(seed);
			var test = new RandomizedOrderResolverTest();
			// Resolve all nodes, but in a random order without using a navigator.
			test.resolver = new CSharpAstResolver(file.Project.Compilation, file.CompilationUnit, file.ParsedFile);
			// For comparing whether the results are equivalent, we also use a normal 'resolve all' resolver:
			test.resolveAllResolver = new CSharpAstResolver(file.Project.Compilation, file.CompilationUnit, file.ParsedFile);
			test.resolveAllResolver.ApplyNavigator(new ResolveAllNavigator(), CancellationToken.None);
			// Prepare list of actions that we need to verify:
			var actions = new List<Func<bool>>();
			bool checkResults = rnd.Next(0, 2) == 0;
			bool checkStateBefore = rnd.Next(0, 2) == 0;
			bool checkStateAfter = rnd.Next(0, 2) == 0;
			foreach (var _node in file.CompilationUnit.DescendantsAndSelf) {
				var node = _node;
				if (CSharpAstResolver.IsUnresolvableNode(node))
					continue;
				if (checkResults)
					actions.Add(() => test.CheckResult(node));
				if (checkStateBefore)
					actions.Add(() => test.CheckStateBefore(node));
				if (checkStateAfter)
					actions.Add(() => test.CheckStateAfter(node));
				var expr = node as Expression;
				if (expr != null) {
					//actions.Add(() => test.CheckExpectedType(node));
					//actions.Add(() => test.CheckConversion(node));
				}
			}
			
			// Fisher-Yates shuffle
			for (int i = actions.Count - 1; i > 0; i--) {
				int j = rnd.Next(0, i);
				var tmp = actions[i];
				actions[i] = actions[j];
				actions[j] = tmp;
			}
			
			foreach (var action in actions) {
				if (!action()) {
					Console.WriteLine("Seed for this file was: " + seed);
					break;
				}
			}
		}
		
		bool CheckResult(AstNode node)
		{
			ResolveResult expectedResult = resolveAllResolver.Resolve(node);
			ResolveResult actualResult = resolver.Resolve(node);
			if (IsEqualResolveResult(expectedResult, actualResult))
				return true;
			Console.WriteLine("Different resolve results for '{0}' at {1} in {2}:", node, node.StartLocation, node.GetRegion().FileName);
			Console.WriteLine(" expected: " + expectedResult);
			Console.WriteLine(" actual:   " + actualResult);
			return false;
		}
		
		bool CheckStateBefore(AstNode node)
		{
			var expectedState = resolveAllResolver.GetResolverStateBefore(node);
			var actualState = resolver.GetResolverStateBefore(node);
			if (IsEqualResolverState(expectedState, actualState))
				return true;
			Console.WriteLine("Different resolver states before '{0}' at {1} in {2}.", node, node.StartLocation, node.GetRegion().FileName);
			return false;
		}
		
		bool CheckStateAfter(AstNode node)
		{
			var expectedState = resolveAllResolver.GetResolverStateAfter(node);
			var actualState = resolver.GetResolverStateAfter(node);
			if (IsEqualResolverState(expectedState, actualState))
				return true;
			Console.WriteLine("Different resolver states after '{0}' at {1} in {2}.", node, node.StartLocation, node.GetRegion().FileName);
			return false;
		}
		
		bool IsEqualResolveResult(ResolveResult rr1, ResolveResult rr2)
		{
			if (rr1 == rr2)
				return true;
			if (rr1 == null || rr2 == null)
				return false;
			if (rr1.GetType() != rr2.GetType())
				return false;
			bool eq = true;
			foreach (var property in rr1.GetType().GetProperties()) {
				object val1 = property.GetValue(rr1, null);
				object val2 = property.GetValue(rr2, null);
				eq &= Compare(val1, val2, property.PropertyType);
			}
			foreach (var field in rr1.GetType().GetFields()) {
				object val1 = field.GetValue(rr1);
				object val2 = field.GetValue(rr2);
				eq &= Compare(val1, val2, field.FieldType);
			}
			return eq;
		}
		
		bool Compare(object val1, object val2, Type type)
		{
			if (val1 == val2)
				return true;
			if (val1 == null || val2 == null)
				return false;
			if (type == typeof(ResolveResult)) {
				return IsEqualResolveResult((ResolveResult)val1, (ResolveResult)val2);
			} else if (type == typeof(IVariable) || type == typeof(IParameter)) {
				return IsEqualVariable((IVariable)val1, (IVariable)val2);
			} else if (type == typeof(MethodListWithDeclaringType)) {
				var l1 = (MethodListWithDeclaringType)val1;
				var l2 = (MethodListWithDeclaringType)val2;
				return object.Equals(l1.DeclaringType, l2.DeclaringType)
					&& Compare(l1, l2, type.BaseType);
			} else if (type.IsArray || type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(List<>) || type.GetGenericTypeDefinition() == typeof(ReadOnlyCollection<>) || type.GetGenericTypeDefinition() == typeof(IList<>) || type.GetGenericTypeDefinition() == typeof(ICollection<>) || type.GetGenericTypeDefinition() == typeof(IEnumerable<>))) {
				Type elementType = type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
				object[] arr1 = ((IEnumerable)val1).Cast<object>().ToArray();
				object[] arr2 = ((IEnumerable)val2).Cast<object>().ToArray();
				if (arr1.Length != arr2.Length)
					return false;
				for (int i = 0; i < arr1.Length; i++) {
					if (!Compare(arr1[i], arr2[i], elementType))
						return false;
				}
				return true;
			} else {
				if (object.Equals(val1, val2))
					return true;
				else if (val1 is Conversion && val2 is Conversion && ((Conversion)val1).IsAnonymousFunctionConversion && ((Conversion)val2).IsAnonymousFunctionConversion)
					return true;
				else
					return false;
			}
		}
		
		bool IsEqualResolverState(CSharpResolver r1, CSharpResolver r2)
		{
			if (r1.CheckForOverflow != r2.CheckForOverflow)
				return false;
			if (r1.Compilation != r2.Compilation)
				return false;
			if (!object.Equals(r1.CurrentMember, r2.CurrentMember))
				return false;
			if (!object.Equals(r1.CurrentObjectInitializerType, r2.CurrentObjectInitializerType))
				return false;
			if (!object.Equals(r1.CurrentTypeDefinition, r2.CurrentTypeDefinition))
				return false;
			if (!object.Equals(r1.CurrentUsingScope, r2.CurrentUsingScope))
				return false;
			if (r1.IsWithinLambdaExpression != r2.IsWithinLambdaExpression)
				return false;
			if (r1.LocalVariables.Count() != r2.LocalVariables.Count())
				return false;
			return r1.LocalVariables.Zip(r2.LocalVariables, IsEqualVariable).All(_ => _);
		}
		
		bool IsEqualVariable(IVariable v1, IVariable v2)
		{
			return object.Equals(v1.ConstantValue, v2.ConstantValue)
				&& v1.IsConst == v2.IsConst
				&& v1.Name == v2.Name
				&& v1.Region == v2.Region
				&& object.Equals(v1.Type, v2.Type);
		}
		
		sealed class ResolveAllNavigator : IResolveVisitorNavigator
		{
			public ResolveVisitorNavigationMode Scan(AstNode node)
			{
				return ResolveVisitorNavigationMode.Resolve;
			}
			
			public void Resolved(AstNode node, ResolveResult result)
			{
			}
			
			public void ProcessConversion(Expression expression, ResolveResult result, Conversion conversion, IType targetType)
			{
			}
		}
	}
}
