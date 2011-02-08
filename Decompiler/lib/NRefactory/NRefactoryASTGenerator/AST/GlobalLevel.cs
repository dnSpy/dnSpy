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
	[CustomImplementation, HasChildren]
	class CompilationUnit : AbstractNode {}
	
	[HasChildren]
	class NamespaceDeclaration : AbstractNode
	{
		string name;
		
		public NamespaceDeclaration(string name) {}
	}
	
	class TemplateDefinition : AttributedNode
	{
		[QuestionMarkDefault]
		string name;
		List<TypeReference> bases;
		
		public TemplateDefinition(string name, List<AttributeSection> attributes) : base(attributes) {}
	}
	
	class DelegateDeclaration : AttributedNode
	{
		[QuestionMarkDefault]
		string          name;
		TypeReference   returnType;
		List<ParameterDeclarationExpression> parameters;
		List<TemplateDefinition> templates;
		
		public DelegateDeclaration(Modifiers modifier, List<AttributeSection> attributes) : base(modifier, attributes) {}
	}
	
	enum ClassType { Class }
	
	[HasChildren]
	class TypeDeclaration : AttributedNode
	{
		// Children of Struct:    FieldDeclaration, MethodDeclaration, EventDeclaration, ConstructorDeclaration,
		//                        OperatorDeclaration, TypeDeclaration, IndexerDeclaration, PropertyDeclaration, in VB: DeclareDeclaration
		// Childrean of class:    children of struct, DestructorDeclaration
		// Children of Interface: MethodDeclaration, PropertyDeclaration, IndexerDeclaration, EventDeclaration, in VB: TypeDeclaration(Enum) too
		// Children of Enum:      FieldDeclaration
		string name;
		ClassType type;
		List<TypeReference> baseTypes;
		List<TemplateDefinition> templates;
		Location bodyStartLocation;
		
		public TypeDeclaration(Modifiers modifier, List<AttributeSection> attributes) : base(modifier, attributes) {}
	}
	
	[IncludeBoolProperty("IsAlias", "return !alias.IsNull;")]
	class Using : AbstractNode
	{
		[QuestionMarkDefault]
		string name;
		TypeReference alias;
		
		public Using(string name) {}
		public Using(string name, TypeReference alias) {}
	}
	
	[IncludeMember("public UsingDeclaration(string @namespace) : this(@namespace, null) {}")]
	[IncludeMember("public UsingDeclaration(string @namespace, TypeReference alias) {" +
	               " usings = new List<Using>(1);" +
	               " usings.Add(new Using(@namespace, alias)); " +
	               "}")]
	class UsingDeclaration : AbstractNode
	{
		List<Using> usings;
		
		public UsingDeclaration(List<Using> usings) {}
	}
	
	enum OptionType { None }
	
	class OptionDeclaration : AbstractNode
	{
		OptionType optionType;
		bool       optionValue;
		
		public OptionDeclaration(OptionType optionType, bool optionValue) {}
	}
}
