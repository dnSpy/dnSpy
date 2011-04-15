// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

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
