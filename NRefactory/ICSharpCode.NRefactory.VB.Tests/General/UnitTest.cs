// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Reflection;
using NUnit.Framework;
using ICSharpCode.NRefactory.VB.Ast;
using ICSharpCode.NRefactory.VB.Parser;

namespace ICSharpCode.NRefactory.VB.Tests
{
	[TestFixture]
	public class StructuralTest
	{
//		[Test]
//		public void TestToStringMethods()
//		{
//			Type[] allTypes = typeof(INode).Assembly.GetTypes();
//			
//			foreach (Type type in allTypes) {
//				if (type.IsClass && !type.IsAbstract && !type.IsNested && type.GetInterface(typeof(INode).FullName) != null) {
//					MethodInfo methodInfo = type.GetMethod("ToString", BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public);
//					Assert.IsNotNull(methodInfo, "ToString() not found in " + type.FullName);
//				}
//			}
//		}
//		
//		[Test]
//		public void TestUnitTests()
//		{
//			Type[] allTypes = typeof(StructuralTest).Assembly.GetTypes();
//			
//			foreach (Type type in allTypes) {
//				if (type.GetCustomAttributes(typeof(TestFixtureAttribute), true).Length > 0) {
//					foreach (MethodInfo m in type.GetMethods()) {
//						if (m.IsPublic && m.ReturnType == typeof(void) && m.GetParameters().Length == 0) {
//							if (m.GetCustomAttributes(typeof(TestAttribute), true).Length == 0) {
//								Assert.Fail(type.Name + "." + m.Name + " should have the [Test] attribute!");
//							}
//						}
//					}
//				}
//			}
//		}
//		
//		[Test]
//		public void TestAcceptVisitorMethods()
//		{
//			Type[] allTypes = typeof(AbstractNode).Assembly.GetTypes();
//
//			foreach (Type type in allTypes) {
//				if (type.IsClass && !type.IsAbstract && type.GetInterface(typeof(INode).FullName) != null) {
//					MethodInfo methodInfo = type.GetMethod("AcceptVisitor", BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public);
//					Assertion.AssertNotNull("AcceptVisitor() not found in " + type.FullName, methodInfo);
//				}
//			}
//		}
//		
//		[Test]
//		public void TestIAstVisitor()
//		{
//			Type[] allTypes = typeof(AbstractNode).Assembly.GetTypes();
//			Type visitor = typeof(IAstVisitor);
//			
//			foreach (Type type in allTypes) {
//				if (type.IsClass && !type.IsAbstract && !type.IsNested && type.GetInterface(typeof(INode).FullName) != null && !type.Name.StartsWith("Null")) {
//					MethodInfo methodInfo = visitor.GetMethod("Visit" + type.Name, BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.ExactBinding, null, new Type[] {type, typeof(object)}, null);
//					Assert.IsNotNull(methodInfo, "Visit with parameter " + type.FullName + " not found");
//					Assert.AreEqual(2, methodInfo.GetParameters().Length);
//					ParameterInfo first = methodInfo.GetParameters()[0];
//					Assert.AreEqual(Char.ToLower(first.ParameterType.Name[0]) + first.ParameterType.Name.Substring(1), first.Name);
//					
//					ParameterInfo second = methodInfo.GetParameters()[1];
//					Assert.AreEqual(typeof(System.Object), second.ParameterType);
//					Assert.AreEqual("data", second.Name);
//				}
//			}
//		}
//		
//		[Test]
//		public void TestAbstractAstVisitorVisitor()
//		{
//			Type[] allTypes = typeof(AbstractNode).Assembly.GetTypes();
//			Type visitor = typeof(AbstractAstVisitor);
//			
//			foreach (Type type in allTypes) {
//				if (type.IsClass && !type.IsAbstract && !type.IsNested && type.GetInterface(typeof(INode).FullName) != null && !type.Name.StartsWith("Null")) {
//					MethodInfo methodInfo = visitor.GetMethod("Visit" + type.Name, BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.ExactBinding, null, new Type[] {type, typeof(object)}, null);
//					Assert.IsNotNull(methodInfo, "Visit with parameter " + type.FullName + " not found");
//					
//					Assert.AreEqual(2, methodInfo.GetParameters().Length);
//					ParameterInfo first = methodInfo.GetParameters()[0];
//					Assert.AreEqual(Char.ToLower(first.ParameterType.Name[0]) + first.ParameterType.Name.Substring(1), first.Name);
//					
//					ParameterInfo second = methodInfo.GetParameters()[1];
//					Assert.AreEqual(typeof(System.Object), second.ParameterType);
//					Assert.AreEqual("data", second.Name);
//				}
//			}
//		}
	}
}
