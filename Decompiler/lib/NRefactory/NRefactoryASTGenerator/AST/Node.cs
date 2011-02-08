// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;

namespace NRefactoryASTGenerator.Ast
{
	interface INode {}
	interface INullable {}
	struct Location {}
	
	enum Modifiers { None }
	
	[CustomImplementation]
	abstract class AbstractNode : INode {}
	
	abstract class AttributedNode : AbstractNode
	{
		List<AttributeSection> attributes;
		Modifiers modifier;
		
		public AttributedNode() {}
		public AttributedNode(List<AttributeSection> attributes) {}
		public AttributedNode(Modifiers modifier, List<AttributeSection> attributes) {}
	}
	
	abstract class ParametrizedNode : AttributedNode
	{
		string name;
		List<ParameterDeclarationExpression> parameters;
		
		public ParametrizedNode() {}
		
		public ParametrizedNode(Modifiers modifier, List<AttributeSection> attributes,
		                        string name, List<ParameterDeclarationExpression> parameters)
			: base(modifier, attributes)
		{}
	}
	
	[CustomImplementation]
	class TypeReference : AbstractNode
	{
		List<TypeReference> genericTypes;
	}
	
	[CustomImplementation]
	class InnerClassTypeReference : TypeReference
	{
		TypeReference baseType;
	}
	
	class AttributeSection : AbstractNode, INullable
	{
		string attributeTarget;
		List<Attribute> attributes;
	}
	
	class Attribute : AbstractNode
	{
		string name;
		List<Expression> positionalArguments;
		List<NamedArgumentExpression> namedArguments;
		
		public Attribute() {}
		public Attribute(string name, List<Expression> positionalArguments, List<NamedArgumentExpression> namedArguments) {}
	}
}
