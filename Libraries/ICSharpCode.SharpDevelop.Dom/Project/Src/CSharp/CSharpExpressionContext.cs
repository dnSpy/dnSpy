// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using ICSharpCode.SharpDevelop.Dom;

namespace ICSharpCode.SharpDevelop.Dom.CSharp
{
	public static class CSharpExpressionContext
	{
		/// <summary>The context is the body of a property declaration.</summary>
		/// <example>string Name { *expr* }</example>
		public readonly static ExpressionContext PropertyDeclaration = new ExpressionContext.DefaultExpressionContext("PropertyDeclaration");
		
		/// <summary>The context is the body of a property declaration inside an interface.</summary>
		/// <example>string Name { *expr* }</example>
		public readonly static ExpressionContext InterfacePropertyDeclaration = new ExpressionContext.DefaultExpressionContext("InterfacePropertyDeclaration");
		
		/// <summary>The context is the body of a event declaration.</summary>
		/// <example>event EventHandler NameChanged { *expr* }</example>
		public readonly static ExpressionContext EventDeclaration = new ExpressionContext.DefaultExpressionContext("EventDeclaration");
		
		/// <summary>The context is after the type parameters of a type/method declaration.
		/// The only valid keyword is "where"</summary>
		/// <example>class &lt;T&gt; *expr*</example>
		public readonly static ExpressionContext ConstraintsStart = new ExpressionContext.DefaultExpressionContext("ConstraintsStart");
		
		/// <summary>The context is after the 'where' of a constraints list.</summary>
		/// <example>class &lt;T&gt; where *expr*</example>
		public readonly static ExpressionContext Constraints = new ExpressionContext.NonStaticTypeExpressionContext("Constraints", false);
		
		/// <summary>The context is the body of an interface declaration.</summary>
		public readonly static ExpressionContext InterfaceDeclaration = new ExpressionContext.NonStaticTypeExpressionContext("InterfaceDeclaration", true);
		
		/// <summary>Context expects "base" or "this".</summary>
		/// <example>public ClassName() : *expr*</example>
		public readonly static ExpressionContext BaseConstructorCall = new ExpressionContext.DefaultExpressionContext("BaseConstructorCall");
		
		/// <summary>The first parameter</summary>
		/// <example>void MethodName(*expr*)</example>
		public readonly static ExpressionContext FirstParameterType = new ExpressionContext.NonStaticTypeExpressionContext("FirstParameterType", false);
		
		/// <summary>Another parameter</summary>
		/// <example>void MethodName(..., *expr*)</example>
		public readonly static ExpressionContext ParameterType = new ExpressionContext.NonStaticTypeExpressionContext("ParameterType", false);
		
		/// <summary>Context expects a fully qualified type name.</summary>
		/// <example>using Alias = *expr*;</example>
		public readonly static ExpressionContext FullyQualifiedType = new ExpressionContext.DefaultExpressionContext("FullyQualifiedType");
		
		/// <summary>Context expects is a property name in an object initializer.</summary>
		/// <example>new *type* [(args)] { *expr* = ... }</example>
		public readonly static ExpressionContext ObjectInitializer = new ExpressionContext.DefaultExpressionContext("ObjectInitializer");
	}
}
