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
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Parser.TypeMembers
{
	[TestFixture]
	public class EventDeclarationTests
	{
		[Test]
		public void SimpleEventDeclarationTest()
		{
			ParseUtilCSharp.AssertTypeMember(
				"event EventHandler MyEvent;",
				new EventDeclaration {
					ReturnType = new SimpleType("EventHandler"),
					Variables = {
						new VariableInitializer {
							Name = "MyEvent"
						}
					}});
		}
		
		[Test]
		public void MultipleEventDeclarationTest()
		{
			ParseUtilCSharp.AssertTypeMember(
				"public event EventHandler A = null, B = delegate {};",
				new EventDeclaration {
					Modifiers = Modifiers.Public,
					ReturnType = new SimpleType("EventHandler"),
					Variables = {
						new VariableInitializer {
							Name = "A",
							Initializer = new NullReferenceExpression()
						},
						new VariableInitializer {
							Name = "B",
							Initializer = new AnonymousMethodExpression() { Body = new BlockStatement ()}
						}
					}});
		}
		
		[Test]
		public void AddRemoveEventDeclarationTest()
		{
			ParseUtilCSharp.AssertTypeMember(
				"public event System.EventHandler MyEvent { add { } remove { } }",
				new CustomEventDeclaration {
					Modifiers = Modifiers.Public,
					ReturnType = new MemberType {
						Target = new SimpleType("System"),
						MemberName = "EventHandler"
					},
					Name = "MyEvent",
				AddAccessor = new Accessor { Body = new BlockStatement() },
				RemoveAccessor = new Accessor { Body = new BlockStatement() }
			});
		}
		
		[Test]
		public void EventImplementingGenericInterfaceDeclarationTest()
		{
			ParseUtilCSharp.AssertTypeMember(
				"event EventHandler MyInterface<string>.MyEvent { add { } [Attr] remove {} }",
				new CustomEventDeclaration {
					ReturnType = new SimpleType("EventHandler"),
					PrivateImplementationType = new SimpleType{
						Identifier = "MyInterface",
						TypeArguments = { new PrimitiveType("string") }
					},
					Name = "MyEvent",
					AddAccessor = new Accessor { Body = new BlockStatement() },
					RemoveAccessor = new Accessor {
						Attributes = {
							new AttributeSection {
								Attributes = {
									new Attribute { Type = new SimpleType("Attr") }
								}
							}
						},
						Body = new BlockStatement()
					}
				});
		}
	}
}
