// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace VBAstGenerator.Ast
{
	class VariableDeclaration : AbstractNode
	{
		string     name;
		Expression initializer;
		TypeReference typeReference;
		Expression fixedArrayInitialization;
		
		public VariableDeclaration(string name) {}
		public VariableDeclaration(string name, Expression initializer) {}
		public VariableDeclaration(string name, Expression initializer, TypeReference typeReference) {}
	}
	
	class ConstructorDeclaration : ParametrizedNode
	{
		ConstructorInitializer constructorInitializer;
		BlockStatement         body;
		
		public ConstructorDeclaration(string name, Modifiers modifier,
		                              List<ParameterDeclarationExpression> parameters,
		                              List<AttributeSection> attributes)
			: base(modifier, attributes, name, parameters)
		{}
		
		public ConstructorDeclaration(string name, Modifiers modifier,
		                              List<ParameterDeclarationExpression> parameters,
		                              ConstructorInitializer constructorInitializer,
		                              List<AttributeSection> attributes)
			: base(modifier, attributes, name, parameters)
		{}
	}
	
	enum ConstructorInitializerType { None }
	
	[ImplementNullable]
	class ConstructorInitializer : AbstractNode, INullable
	{
		ConstructorInitializerType constructorInitializerType;
		List<Expression>           arguments;
	}
	
	[ImplementNullable(NullableImplementation.Abstract)]
	abstract class EventAddRemoveRegion : AttributedNode, INullable
	{
		BlockStatement block;
		List<ParameterDeclarationExpression> parameters;
		
		public EventAddRemoveRegion(List<AttributeSection> attributes) : base(attributes) {}
	}
	
	[ImplementNullable(NullableImplementation.CompleteAbstract)]
	class EventAddRegion : EventAddRemoveRegion
	{
		public EventAddRegion(List<AttributeSection> attributes) : base(attributes) {}
	}
	
	[ImplementNullable(NullableImplementation.CompleteAbstract)]
	class EventRemoveRegion : EventAddRemoveRegion
	{
		public EventRemoveRegion(List<AttributeSection> attributes) : base(attributes) {}
	}
	
	[ImplementNullable(NullableImplementation.CompleteAbstract)]
	class EventRaiseRegion : EventAddRemoveRegion
	{
		public EventRaiseRegion(List<AttributeSection> attributes) : base(attributes) {}
	}
	
	class InterfaceImplementation : AbstractNode
	{
		TypeReference interfaceType;
		[QuestionMarkDefault]
		string memberName;
		
		public InterfaceImplementation(TypeReference interfaceType, string memberName) {}
	}
	
	[IncludeBoolProperty("HasAddRegion",    "return !addRegion.IsNull;")]
	[IncludeBoolProperty("HasRemoveRegion", "return !removeRegion.IsNull;")]
	[IncludeBoolProperty("HasRaiseRegion",  "return !raiseRegion.IsNull;")]
	class EventDeclaration : MemberNode
	{
		EventAddRegion addRegion;
		EventRemoveRegion removeRegion;
		EventRaiseRegion raiseRegion;
		Location bodyStart;
		Location bodyEnd;
		Expression initializer;
	}
	
	[IncludeMember(@"
		public TypeReference GetTypeForField(int fieldIndex)
		{
			if (!typeReference.IsNull) {
				return typeReference;
			}
			return ((VariableDeclaration)Fields[fieldIndex]).TypeReference;
		}")]
	[IncludeMember(@"
		public VariableDeclaration GetVariableDeclaration(string variableName)
		{
			foreach (VariableDeclaration variableDeclaration in Fields) {
				if (variableDeclaration.Name == variableName) {
					return variableDeclaration;
				}
			}
			return null;
		}")]
	class FieldDeclaration : AttributedNode
	{
		TypeReference             typeReference;
		List<VariableDeclaration> fields;
		
		// for enum members
		public FieldDeclaration(List<AttributeSection> attributes) : base(attributes) {}
		
		// for all other cases
		public FieldDeclaration(List<AttributeSection> attributes, TypeReference typeReference, Modifiers modifier)
			: base(modifier, attributes)
		{}
	}
	
	abstract class MemberNode : ParametrizedNode
	{
		List<InterfaceImplementation> interfaceImplementations;
		TypeReference typeReference;
		
		public MemberNode() {}
		
		public MemberNode(Modifiers modifier, List<AttributeSection> attributes,
		                  string name, List<ParameterDeclarationExpression> parameters)
			: base(modifier, attributes, name, parameters)
		{}
	}
	
	class MethodDeclaration : MemberNode
	{
		BlockStatement   body;
		List<string>     handlesClause;
		List<TemplateDefinition> templates;
		bool isExtensionMethod;
	}
	
	enum ConversionType { None }
	enum OverloadableOperatorType { None }
	
	[IncludeBoolProperty("IsConversionOperator", "return conversionType != ConversionType.None;")]
	class OperatorDeclaration : MethodDeclaration
	{
		ConversionType conversionType;
		OverloadableOperatorType overloadableOperator;
	}
	
	[IncludeBoolProperty("HasGetRegion", "return !getRegion.IsNull;")]
	[IncludeBoolProperty("HasSetRegion", "return !setRegion.IsNull;")]
	[IncludeBoolProperty("IsReadOnly", "return HasGetRegion && !HasSetRegion;")]
	[IncludeBoolProperty("IsWriteOnly", "return !HasGetRegion && HasSetRegion;")]
	[IncludeBoolProperty("IsIndexer", "return (Modifier & Modifiers.Default) != 0;")]
	[IncludeMember(@"
		internal PropertyDeclaration(string name, TypeReference typeReference, Modifiers modifier, List<AttributeSection> attributes) : this(modifier, attributes, name, null)
		{
			this.TypeReference = typeReference;
			if ((modifier & Modifiers.ReadOnly) != Modifiers.ReadOnly) {
				this.SetRegion = new PropertySetRegion(null, null);
			}
			if ((modifier & Modifiers.WriteOnly) != Modifiers.WriteOnly) {
				this.GetRegion = new PropertyGetRegion(null, null);
			}
		}")]
	class PropertyDeclaration : MemberNode
	{
		Location          bodyStart;
		Location          bodyEnd;
		PropertyGetRegion getRegion;
		PropertySetRegion setRegion;
		Expression        initializer;
		
		public PropertyDeclaration(Modifiers modifier, List<AttributeSection> attributes,
		                           string name, List<ParameterDeclarationExpression> parameters)
			: base(modifier, attributes, name, parameters)
		{}
	}
	
	[ImplementNullable(NullableImplementation.Abstract)]
	abstract class PropertyGetSetRegion : AttributedNode, INullable
	{
		// can be null if only the definition is there (interface declaration)
		BlockStatement block;
		
		public PropertyGetSetRegion(BlockStatement block, List<AttributeSection> attributes) : base(attributes) {}
	}
	
	[ImplementNullable(NullableImplementation.CompleteAbstract)]
	class PropertyGetRegion : PropertyGetSetRegion
	{
		public PropertyGetRegion(BlockStatement block, List<AttributeSection> attributes) : base(block, attributes) {}
	}
	
	[ImplementNullable(NullableImplementation.CompleteAbstract)]
	class PropertySetRegion : PropertyGetSetRegion
	{
		List<ParameterDeclarationExpression> parameters;
		
		public PropertySetRegion(BlockStatement block, List<AttributeSection> attributes) : base(block, attributes) {}
	}
	
	enum CharsetModifier { None }
	
	class DeclareDeclaration : ParametrizedNode
	{
		string          alias;
		string          library;
		CharsetModifier charset;
		TypeReference   typeReference;
		
		public DeclareDeclaration(string name, Modifiers modifier, TypeReference typeReference, List<ParameterDeclarationExpression> parameters, List<AttributeSection> attributes, string library, string alias, CharsetModifier charset)
			: base(modifier, attributes, name, parameters)
		{}
	}
}
