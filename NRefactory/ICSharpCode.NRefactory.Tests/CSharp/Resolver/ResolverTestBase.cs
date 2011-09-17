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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using ICSharpCode.NRefactory.CSharp.Parser;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	/// <summary>
	/// Base class with helper functions for resolver unit tests.
	/// </summary>
	public abstract class ResolverTestBase
	{
		protected readonly IProjectContent mscorlib = CecilLoaderTests.Mscorlib;
		protected SimpleProjectContent project;
		protected ITypeResolveContext context;
		protected CSharpResolver resolver;
		
		[SetUp]
		public virtual void SetUp()
		{
			project = new SimpleProjectContent();
			context = new CompositeTypeResolveContext(new [] { project, mscorlib, CecilLoaderTests.SystemCore });
			resolver = new CSharpResolver(context);
			resolver.CurrentUsingScope = MakeUsingScope("");
		}
		
		protected UsingScope MakeUsingScope(string namespaceName)
		{
			UsingScope u = new UsingScope(project);
			if (!string.IsNullOrEmpty(namespaceName)) {
				foreach (string element in namespaceName.Split('.')) {
					u = new UsingScope(u, string.IsNullOrEmpty(u.NamespaceName) ? element : u.NamespaceName + "." + element);
				}
			}
			return u;
		}
		
		/// <summary>
		/// Adds a using to the current using scope.
		/// </summary>
		protected void AddUsing(string namespaceName)
		{
			resolver.CurrentUsingScope.Usings.Add(MakeReference(namespaceName));
		}
		
		/// <summary>
		/// Adds a using alias to the current using scope.
		/// </summary>
		protected void AddUsingAlias(string alias, string target)
		{
			resolver.CurrentUsingScope.UsingAliases.Add(new KeyValuePair<string, ITypeOrNamespaceReference>(alias, MakeReference(target)));
		}
		
		protected ITypeOrNamespaceReference MakeReference(string namespaceName)
		{
			string[] nameParts = namespaceName.Split('.');
			ITypeOrNamespaceReference r = new SimpleTypeOrNamespaceReference(nameParts[0], new ITypeReference[0], resolver.CurrentTypeDefinition, resolver.CurrentUsingScope, SimpleNameLookupMode.TypeInUsingDeclaration);
			for (int i = 1; i < nameParts.Length; i++) {
				r = new MemberTypeOrNamespaceReference(r, nameParts[i], new ITypeReference[0], resolver.CurrentTypeDefinition, resolver.CurrentUsingScope);
			}
			return r;
		}
		
		protected IType ResolveType(Type type)
		{
			IType t = type.ToTypeReference().Resolve(context);
			if (SharedTypes.UnknownType.Equals(t))
				throw new InvalidOperationException("Could not resolve type");
			return t;
		}
		
		protected ConstantResolveResult MakeConstant(object value)
		{
			if (value == null)
				return new ConstantResolveResult(SharedTypes.Null, null);
			IType type = ResolveType(value.GetType());
			if (type.Kind == TypeKind.Enum)
				value = Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType()));
			return new ConstantResolveResult(type, value);
		}
		
		protected ResolveResult MakeResult(Type type)
		{
			return new ResolveResult(ResolveType(type));
		}
		
		protected void AssertConstant(object expectedValue, ResolveResult rr)
		{
			Assert.IsFalse(rr.IsError, rr.ToString() + " is an error");
			Assert.IsTrue(rr.IsCompileTimeConstant, rr.ToString() + " is not a compile-time constant");
			Type expectedType = expectedValue.GetType();
			Assert.AreEqual(ResolveType(expectedType), rr.Type, "ResolveResult.Type is wrong");
			if (expectedType.IsEnum) {
				Assert.AreEqual(Enum.GetUnderlyingType(expectedType), rr.ConstantValue.GetType(), "ResolveResult.ConstantValue has wrong Type");
				Assert.AreEqual(Convert.ChangeType(expectedValue, Enum.GetUnderlyingType(expectedType)), rr.ConstantValue);
			} else {
				Assert.AreEqual(expectedType, rr.ConstantValue.GetType(), "ResolveResult.ConstantValue has wrong Type");
				Assert.AreEqual(expectedValue, rr.ConstantValue);
			}
		}
		
		protected void AssertType(Type expectedType, ResolveResult rr)
		{
			Assert.IsFalse(rr.IsError, rr.ToString() + " is an error");
			Assert.IsFalse(rr.IsCompileTimeConstant, rr.ToString() + " is a compile-time constant");
			Assert.AreEqual(expectedType.ToTypeReference().Resolve(context), rr.Type);
		}
		
		protected void AssertError(Type expectedType, ResolveResult rr)
		{
			Assert.IsTrue(rr.IsError, rr.ToString() + " is not an error, but an error was expected");
			Assert.IsFalse(rr.IsCompileTimeConstant, rr.ToString() + " is a compile-time constant");
			Assert.AreEqual(expectedType.ToTypeReference().Resolve(context), rr.Type);
		}
		
		protected void TestOperator(UnaryOperatorType op, ResolveResult input,
		                            Conversion expectedConversion, Type expectedResultType)
		{
			var rr = resolver.ResolveUnaryOperator(op, input);
			AssertType(expectedResultType, rr);
			Assert.AreEqual(typeof(UnaryOperatorResolveResult), rr.GetType());
			var uorr = (UnaryOperatorResolveResult)rr;
			AssertConversion(uorr.Input, input, expectedConversion, "Conversion");
		}
		
		protected void TestOperator(ResolveResult lhs, BinaryOperatorType op, ResolveResult rhs,
		                            Conversion expectedLeftConversion, Conversion expectedRightConversion, Type expectedResultType)
		{
			var rr = resolver.ResolveBinaryOperator(op, lhs, rhs);
			AssertType(expectedResultType, rr);
			Assert.AreEqual(typeof(BinaryOperatorResolveResult), rr.GetType());
			var borr = (BinaryOperatorResolveResult)rr;
			AssertConversion(borr.Left, lhs, expectedLeftConversion, "Left conversion");
			AssertConversion(borr.Right, rhs, expectedRightConversion, "Right conversion");
		}
		
		protected void AssertConversion(ResolveResult conversionResult, ResolveResult expectedRR, Conversion expectedConversion, string text)
		{
			if (expectedConversion == Conversion.IdentityConversion) {
				Assert.AreSame(expectedRR, conversionResult, "Expected no " + text);
			} else {
				ConversionResolveResult crr = conversionResult as ConversionResolveResult;
				Assert.IsNotNull(crr, "Could not find ConversionResolveResult for " + text);
				Assert.AreEqual(expectedConversion, crr.Conversion, text);
				Assert.AreSame(expectedRR, crr.Input, "Input of " + text);
			}
		}
		
		IEnumerable<TextLocation> FindDollarSigns(string code)
		{
			int line = 1;
			int col = 1;
			foreach (char c in code) {
				if (c == '$') {
					yield return new TextLocation(line, col);
				} else if (c == '\n') {
					line++;
					col = 1;
				} else {
					col++;
				}
			}
		}
		
		protected ResolveResult Resolve(string code)
		{
			CompilationUnit cu = new CSharpParser().Parse(new StringReader(code.Replace("$", "")));
			
			TextLocation[] dollars = FindDollarSigns(code).ToArray();
			Assert.AreEqual(2, dollars.Length, "Expected 2 dollar signs marking start+end of desired node");
			
			SetUp();
			
			CSharpParsedFile parsedFile = new CSharpParsedFile("test.cs", resolver.CurrentUsingScope);
			TypeSystemConvertVisitor convertVisitor = new TypeSystemConvertVisitor(parsedFile, resolver.CurrentUsingScope, null);
			cu.AcceptVisitor(convertVisitor, null);
			project.UpdateProjectContent(null, convertVisitor.ParsedFile);
			
			FindNodeVisitor fnv = new FindNodeVisitor(dollars[0], dollars[1]);
			cu.AcceptVisitor(fnv, null);
			Assert.IsNotNull(fnv.ResultNode, "Did not find DOM node at the specified location");
			
			Debug.WriteLine(new string('=', 70));
			Debug.WriteLine("Starting new resolver for " + fnv.ResultNode);
			
			var navigator = new NodeListResolveVisitorNavigator(new[] { fnv.ResultNode });
			ResolveResult rr;
			using (var context = this.context.Synchronize()) {
				ResolveVisitor rv = new ResolveVisitor(new CSharpResolver(context), convertVisitor.ParsedFile, navigator);
				rv.Scan(cu);
				rr = rv.GetResolveResult(fnv.ResultNode);
			}
			Assert.IsNotNull(rr, "ResolveResult is null - did something go wrong while navigating to the target node?");
			Debug.WriteLine("ResolveResult is " + rr);
			return rr;
		}
		
		protected T Resolve<T>(string code) where T : ResolveResult
		{
			ResolveResult rr = Resolve(code);
			Assert.IsNotNull(rr);
			Assert.IsTrue(rr.GetType() == typeof(T), "Resolve should be " + typeof(T).Name + ", but was " + rr.GetType().Name);
			return (T)rr;
		}
		
		sealed class FindNodeVisitor : DepthFirstAstVisitor<object, object>
		{
			readonly TextLocation start;
			readonly TextLocation end;
			public AstNode ResultNode;
			
			public FindNodeVisitor(TextLocation start, TextLocation end)
			{
				this.start = start;
				this.end = end;
			}
			
			protected override object VisitChildren(AstNode node, object data)
			{
				if (node.StartLocation == start && node.EndLocation == end) {
					if (ResultNode != null)
						throw new InvalidOperationException("found multiple nodes with same start+end");
					return ResultNode = node;
				} else {
					return base.VisitChildren(node, data);
				}
			}
		}
		
		protected ResolveResult ResolveAtLocation(string code)
		{
			CompilationUnit cu = new CSharpParser().Parse(new StringReader(code.Replace("$", "")));
			
			TextLocation[] dollars = FindDollarSigns(code).ToArray();
			Assert.AreEqual(1, dollars.Length, "Expected 1 dollar signs marking the location");
			
			SetUp();
			
			CSharpParsedFile parsedFile = new CSharpParsedFile("test.cs", resolver.CurrentUsingScope);
			TypeSystemConvertVisitor convertVisitor = new TypeSystemConvertVisitor(parsedFile, resolver.CurrentUsingScope, null);
			cu.AcceptVisitor(convertVisitor, null);
			project.UpdateProjectContent(null, convertVisitor.ParsedFile);
			
			ResolveResult rr = Resolver.ResolveAtLocation.Resolve(this.context, parsedFile, cu, dollars[0]);
			return rr;
		}
		
		protected T ResolveAtLocation<T>(string code) where T : ResolveResult
		{
			ResolveResult rr = ResolveAtLocation(code);
			Assert.IsNotNull(rr);
			Assert.IsTrue(rr.GetType() == typeof(T), "Resolve should be " + typeof(T).Name + ", but was " + rr.GetType().Name);
			return (T)rr;
		}
	}
}
