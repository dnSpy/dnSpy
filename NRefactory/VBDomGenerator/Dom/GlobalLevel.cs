// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace VBDomGenerator.Dom
{
	[CustomImplementation, HasChildren]
	class CompilationUnit : AbstractNode {}
	
	[HasChildren]
	class NamespaceDeclaration : AbstractNode
	{
		string name;
		
		public NamespaceDeclaration(string name) {}
	}
	
	enum VarianceModifier { Invariant, Covariant, Contravariant };
	
	class TemplateDefinition : AttributedNode
	{
		[QuestionMarkDefault]
		string name;
		VarianceModifier varianceModifier;
		List<TypeReference> bases;
		
		public TemplateDefinition() {}
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
	[IncludeBoolProperty("IsXml", "return xmlPrefix != null;")]
	class Using : AbstractNode
	{
		[QuestionMarkDefault]
		string name;
		TypeReference alias;
		string xmlPrefix;
		
		public Using(string name) {}
		public Using(string name, TypeReference alias) {}
		public Using(string name, string xmlPrefix) {}
	}
	
	[IncludeMember("public UsingDeclaration(string @namespace) : this(@namespace, TypeReference.Null) {}")]
	[IncludeMember("public UsingDeclaration(string @namespace, TypeReference alias) {" +
	               " usings = new List<Using>(1);" +
	               " usings.Add(new Using(@namespace, alias)); " +
	               "}")]
	[IncludeMember("public UsingDeclaration(string xmlNamespace, string prefix) {" +
	               " usings = new List<Using>(1);" +
	               " usings.Add(new Using(xmlNamespace, prefix)); " +
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
	
	class ExternAliasDirective : AbstractNode
	{
		string name;
	}
}
