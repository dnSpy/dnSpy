// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using ICSharpCode.NRefactory.CSharp.Parser;
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
			context = new CompositeTypeResolveContext(new [] { project, mscorlib });
			resolver = new CSharpResolver(context);
			resolver.UsingScope = MakeUsingScope("");
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
			resolver.UsingScope.Usings.Add(MakeReference(namespaceName));
		}
		
		/// <summary>
		/// Adds a using alias to the current using scope.
		/// </summary>
		protected void AddUsingAlias(string alias, string target)
		{
			resolver.UsingScope.UsingAliases.Add(new KeyValuePair<string, ITypeOrNamespaceReference>(alias, MakeReference(target)));
		}
		
		protected ITypeOrNamespaceReference MakeReference(string namespaceName)
		{
			string[] nameParts = namespaceName.Split('.');
			ITypeOrNamespaceReference r = new SimpleTypeOrNamespaceReference(nameParts[0], new ITypeReference[0], resolver.CurrentTypeDefinition, resolver.UsingScope, true);
			for (int i = 1; i < nameParts.Length; i++) {
				r = new MemberTypeOrNamespaceReference(r, nameParts[i], new ITypeReference[0], resolver.CurrentTypeDefinition, resolver.UsingScope);
			}
			return r;
		}
		
		protected IType ResolveType(Type type)
		{
			IType t = type.ToTypeReference().Resolve(context);
			if (t == SharedTypes.UnknownType)
				throw new InvalidOperationException("Could not resolve type");
			return t;
		}
		
		protected ConstantResolveResult MakeConstant(object value)
		{
			if (value == null)
				return new ConstantResolveResult(SharedTypes.Null, null);
			IType type = ResolveType(value.GetType());
			if (type.IsEnum())
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
		
		IEnumerable<AstLocation> FindDollarSigns(string code)
		{
			int line = 1;
			int col = 1;
			foreach (char c in code) {
				if (c == '$') {
					yield return new AstLocation(line, col);
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
			
			AstLocation[] dollars = FindDollarSigns(code).ToArray();
			Assert.AreEqual(2, dollars.Length, "Expected 2 dollar signs marking start+end of desired node");
			
			UsingScope rootUsingScope = resolver.UsingScope;
			while (rootUsingScope.Parent != null)
				rootUsingScope = rootUsingScope.Parent;
			
			ParsedFile parsedFile = new ParsedFile("test.cs", rootUsingScope);
			TypeSystemConvertVisitor convertVisitor = new TypeSystemConvertVisitor(parsedFile, resolver.UsingScope, null);
			cu.AcceptVisitor(convertVisitor, null);
			project.UpdateProjectContent(null, convertVisitor.ParsedFile);
			
			FindNodeVisitor fnv = new FindNodeVisitor(dollars[0], dollars[1]);
			cu.AcceptVisitor(fnv, null);
			Assert.IsNotNull(fnv.ResultNode, "Did not find DOM node at the specified location");
			
			var navigator = new NodeListResolveVisitorNavigator(new[] { fnv.ResultNode });
			ResolveResult rr;
			using (var context = this.context.Synchronize()) {
				ResolveVisitor rv = new ResolveVisitor(new CSharpResolver(context), convertVisitor.ParsedFile, navigator);
				rv.Scan(cu);
				rr = rv.GetResolveResult(fnv.ResultNode);
			}
			Assert.IsNotNull(rr, "ResolveResult is null - did something go wrong while navigating to the target node?");
			return rr;
		}
		
		protected T Resolve<T>(string code) where T : ResolveResult
		{
			ResolveResult rr = Resolve(code);
			Assert.IsTrue(rr is T, "Resolve should be " + typeof(T).Name + ", but was " + (rr != null ? rr.GetType().Name : "null"));
			return (T)rr;
		}
		
		protected T Resolve<T>(string code, string exprToResolve) where T : ResolveResult
		{
			return Resolve<T>(code.Replace(exprToResolve, "$" + exprToResolve + "$"));
		}
		
		sealed class FindNodeVisitor : DepthFirstAstVisitor<object, object>
		{
			readonly AstLocation start;
			readonly AstLocation end;
			public AstNode ResultNode;
			
			public FindNodeVisitor(AstLocation start, AstLocation end)
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
	}
}
