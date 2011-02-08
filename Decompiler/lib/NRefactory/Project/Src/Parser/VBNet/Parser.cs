
#line  1 "VBNET.ATG" 
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Parser.VB;
using ASTAttribute = ICSharpCode.NRefactory.Ast.Attribute;
/*
  Parser.frame file for NRefactory.
 */
using System;
using System.Reflection;

namespace ICSharpCode.NRefactory.Parser.VB {



partial class Parser : AbstractParser
{
	const int maxT = 206;

	const  bool   T            = true;
	const  bool   x            = false;
	

#line  12 "VBNET.ATG" 


/*

*/

	void VBNET() {

#line  231 "VBNET.ATG" 
		lexer.NextToken(); // get the first token
		compilationUnit = new CompilationUnit();
		
		while (la.kind == 1) {
			lexer.NextToken();
		}
		while (la.kind == 136) {
			OptionStmt();
		}
		while (la.kind == 108) {
			ImportsStmt();
		}
		while (
#line  237 "VBNET.ATG" 
IsGlobalAttrTarget()) {
			GlobalAttributeSection();
		}
		while (StartOf(1)) {
			NamespaceMemberDecl();
		}
		Expect(0);
	}

	void OptionStmt() {

#line  242 "VBNET.ATG" 
		INode node = null; bool val = true; 
		Expect(136);

#line  243 "VBNET.ATG" 
		Location startPos = t.Location; 
		if (la.kind == 95) {
			lexer.NextToken();
			if (la.kind == 134 || la.kind == 135) {
				OptionValue(
#line  245 "VBNET.ATG" 
ref val);
			}

#line  246 "VBNET.ATG" 
			node = new OptionDeclaration(OptionType.Explicit, val); 
		} else if (la.kind == 164) {
			lexer.NextToken();
			if (la.kind == 134 || la.kind == 135) {
				OptionValue(
#line  248 "VBNET.ATG" 
ref val);
			}

#line  249 "VBNET.ATG" 
			node = new OptionDeclaration(OptionType.Strict, val); 
		} else if (la.kind == 70) {
			lexer.NextToken();
			if (la.kind == 51) {
				lexer.NextToken();

#line  251 "VBNET.ATG" 
				node = new OptionDeclaration(OptionType.CompareBinary, val); 
			} else if (la.kind == 169) {
				lexer.NextToken();

#line  252 "VBNET.ATG" 
				node = new OptionDeclaration(OptionType.CompareText, val); 
			} else SynErr(207);
		} else SynErr(208);
		EndOfStmt();

#line  257 "VBNET.ATG" 
		if (node != null) {
		node.StartLocation = startPos;
		node.EndLocation   = t.Location;
		compilationUnit.AddChild(node);
		}
		
	}

	void ImportsStmt() {

#line  280 "VBNET.ATG" 
		List<Using> usings = new List<Using>();
		
		Expect(108);

#line  284 "VBNET.ATG" 
		Location startPos = t.Location;
		Using u;
		
		ImportClause(
#line  287 "VBNET.ATG" 
out u);

#line  287 "VBNET.ATG" 
		if (u != null) { usings.Add(u); } 
		while (la.kind == 12) {
			lexer.NextToken();
			ImportClause(
#line  289 "VBNET.ATG" 
out u);

#line  289 "VBNET.ATG" 
			if (u != null) { usings.Add(u); } 
		}
		EndOfStmt();

#line  293 "VBNET.ATG" 
		UsingDeclaration usingDeclaration = new UsingDeclaration(usings);
		usingDeclaration.StartLocation = startPos;
		usingDeclaration.EndLocation   = t.Location;
		compilationUnit.AddChild(usingDeclaration);
		
	}

	void GlobalAttributeSection() {
		Expect(27);

#line  2055 "VBNET.ATG" 
		Location startPos = t.Location; 
		if (la.kind == 49) {
			lexer.NextToken();
		} else if (la.kind == 121) {
			lexer.NextToken();
		} else SynErr(209);

#line  2057 "VBNET.ATG" 
		string attributeTarget = t.val.ToLower(System.Globalization.CultureInfo.InvariantCulture);
		List<ASTAttribute> attributes = new List<ASTAttribute>();
		ASTAttribute attribute;
		
		Expect(13);
		Attribute(
#line  2061 "VBNET.ATG" 
out attribute);

#line  2061 "VBNET.ATG" 
		attributes.Add(attribute); 
		while (
#line  2062 "VBNET.ATG" 
NotFinalComma()) {
			if (la.kind == 12) {
				lexer.NextToken();
				if (la.kind == 49) {
					lexer.NextToken();
				} else if (la.kind == 121) {
					lexer.NextToken();
				} else SynErr(210);
				Expect(13);
			}
			Attribute(
#line  2062 "VBNET.ATG" 
out attribute);

#line  2062 "VBNET.ATG" 
			attributes.Add(attribute); 
		}
		if (la.kind == 12) {
			lexer.NextToken();
		}
		Expect(26);
		EndOfStmt();

#line  2067 "VBNET.ATG" 
		AttributeSection section = new AttributeSection {
		AttributeTarget = attributeTarget,
		Attributes = attributes,
		StartLocation = startPos,
		EndLocation = t.EndLocation
		};
		compilationUnit.AddChild(section);
		
	}

	void NamespaceMemberDecl() {

#line  322 "VBNET.ATG" 
		ModifierList m = new ModifierList();
		AttributeSection section;
		List<AttributeSection> attributes = new List<AttributeSection>();
		string qualident;
		
		if (la.kind == 126) {
			lexer.NextToken();

#line  329 "VBNET.ATG" 
			Location startPos = t.Location;
			
			Qualident(
#line  331 "VBNET.ATG" 
out qualident);

#line  333 "VBNET.ATG" 
			INode node =  new NamespaceDeclaration(qualident);
			node.StartLocation = startPos;
			compilationUnit.AddChild(node);
			compilationUnit.BlockStart(node);
			
			Expect(1);
			NamespaceBody();

#line  341 "VBNET.ATG" 
			node.EndLocation = t.Location;
			compilationUnit.BlockEnd();
			
		} else if (StartOf(2)) {
			while (la.kind == 27) {
				AttributeSection(
#line  345 "VBNET.ATG" 
out section);

#line  345 "VBNET.ATG" 
				attributes.Add(section); 
			}
			while (StartOf(3)) {
				TypeModifier(
#line  346 "VBNET.ATG" 
m);
			}
			NonModuleDeclaration(
#line  346 "VBNET.ATG" 
m, attributes);
		} else SynErr(211);
	}

	void OptionValue(
#line  265 "VBNET.ATG" 
ref bool val) {
		if (la.kind == 135) {
			lexer.NextToken();

#line  267 "VBNET.ATG" 
			val = true; 
		} else if (la.kind == 134) {
			lexer.NextToken();

#line  269 "VBNET.ATG" 
			val = false; 
		} else SynErr(212);
	}

	void EndOfStmt() {
		if (la.kind == 1) {
			lexer.NextToken();
		} else if (la.kind == 13) {
			lexer.NextToken();
			if (la.kind == 1) {
				lexer.NextToken();
			}
		} else SynErr(213);
	}

	void ImportClause(
#line  300 "VBNET.ATG" 
out Using u) {

#line  302 "VBNET.ATG" 
		string qualident  = null;
		TypeReference aliasedType = null;
		u = null;
		
		Qualident(
#line  306 "VBNET.ATG" 
out qualident);
		if (la.kind == 11) {
			lexer.NextToken();
			TypeName(
#line  307 "VBNET.ATG" 
out aliasedType);
		}

#line  309 "VBNET.ATG" 
		if (qualident != null && qualident.Length > 0) {
		if (aliasedType != null) {
			u = new Using(qualident, aliasedType);
		} else {
			u = new Using(qualident);
		}
		}
		
	}

	void Qualident(
#line  2794 "VBNET.ATG" 
out string qualident) {

#line  2796 "VBNET.ATG" 
		string name;
		qualidentBuilder.Length = 0; 
		
		Identifier();

#line  2800 "VBNET.ATG" 
		qualidentBuilder.Append(t.val); 
		while (
#line  2801 "VBNET.ATG" 
DotAndIdentOrKw()) {
			Expect(10);
			IdentifierOrKeyword(
#line  2801 "VBNET.ATG" 
out name);

#line  2801 "VBNET.ATG" 
			qualidentBuilder.Append('.'); qualidentBuilder.Append(name); 
		}

#line  2803 "VBNET.ATG" 
		qualident = qualidentBuilder.ToString(); 
	}

	void TypeName(
#line  1948 "VBNET.ATG" 
out TypeReference typeref) {

#line  1949 "VBNET.ATG" 
		ArrayList rank = null; 
		NonArrayTypeName(
#line  1951 "VBNET.ATG" 
out typeref, false);
		ArrayTypeModifiers(
#line  1952 "VBNET.ATG" 
out rank);

#line  1953 "VBNET.ATG" 
		if (rank != null && typeref != null) {
		typeref.RankSpecifier = (int[])rank.ToArray(typeof(int));
		}
		
	}

	void NamespaceBody() {
		while (StartOf(1)) {
			NamespaceMemberDecl();
		}
		Expect(88);
		Expect(126);
		Expect(1);
	}

	void AttributeSection(
#line  2128 "VBNET.ATG" 
out AttributeSection section) {

#line  2130 "VBNET.ATG" 
		string attributeTarget = "";List<ASTAttribute> attributes = new List<ASTAttribute>();
		ASTAttribute attribute;
		
		
		Expect(27);

#line  2134 "VBNET.ATG" 
		Location startPos = t.Location; 
		if (
#line  2135 "VBNET.ATG" 
IsLocalAttrTarget()) {
			if (la.kind == 93) {
				lexer.NextToken();

#line  2136 "VBNET.ATG" 
				attributeTarget = "event";
			} else if (la.kind == 154) {
				lexer.NextToken();

#line  2137 "VBNET.ATG" 
				attributeTarget = "return";
			} else {
				Identifier();

#line  2140 "VBNET.ATG" 
				string val = t.val.ToLower(System.Globalization.CultureInfo.InvariantCulture);
				if (val != "field"	|| val != "method" ||
					val != "module" || val != "param"  ||
					val != "property" || val != "type")
				Error("attribute target specifier (event, return, field," +
						"method, module, param, property, or type) expected");
				attributeTarget = t.val;
				
			}
			Expect(13);
		}
		Attribute(
#line  2150 "VBNET.ATG" 
out attribute);

#line  2150 "VBNET.ATG" 
		attributes.Add(attribute); 
		while (
#line  2151 "VBNET.ATG" 
NotFinalComma()) {
			Expect(12);
			Attribute(
#line  2151 "VBNET.ATG" 
out attribute);

#line  2151 "VBNET.ATG" 
			attributes.Add(attribute); 
		}
		if (la.kind == 12) {
			lexer.NextToken();
		}
		Expect(26);

#line  2155 "VBNET.ATG" 
		section = new AttributeSection {
		AttributeTarget = attributeTarget,
		Attributes = attributes,
		StartLocation = startPos,
		EndLocation = t.EndLocation
		};
		
	}

	void TypeModifier(
#line  2874 "VBNET.ATG" 
ModifierList m) {
		switch (la.kind) {
		case 148: {
			lexer.NextToken();

#line  2875 "VBNET.ATG" 
			m.Add(Modifiers.Public, t.Location); 
			break;
		}
		case 147: {
			lexer.NextToken();

#line  2876 "VBNET.ATG" 
			m.Add(Modifiers.Protected, t.Location); 
			break;
		}
		case 99: {
			lexer.NextToken();

#line  2877 "VBNET.ATG" 
			m.Add(Modifiers.Internal, t.Location); 
			break;
		}
		case 145: {
			lexer.NextToken();

#line  2878 "VBNET.ATG" 
			m.Add(Modifiers.Private, t.Location); 
			break;
		}
		case 158: {
			lexer.NextToken();

#line  2879 "VBNET.ATG" 
			m.Add(Modifiers.Static, t.Location); 
			break;
		}
		case 157: {
			lexer.NextToken();

#line  2880 "VBNET.ATG" 
			m.Add(Modifiers.New, t.Location); 
			break;
		}
		case 122: {
			lexer.NextToken();

#line  2881 "VBNET.ATG" 
			m.Add(Modifiers.Abstract, t.Location); 
			break;
		}
		case 131: {
			lexer.NextToken();

#line  2882 "VBNET.ATG" 
			m.Add(Modifiers.Sealed, t.Location); 
			break;
		}
		case 204: {
			lexer.NextToken();

#line  2883 "VBNET.ATG" 
			m.Add(Modifiers.Partial, t.Location); 
			break;
		}
		default: SynErr(214); break;
		}
	}

	void NonModuleDeclaration(
#line  405 "VBNET.ATG" 
ModifierList m, List<AttributeSection> attributes) {

#line  407 "VBNET.ATG" 
		TypeReference typeRef = null;
		List<TypeReference> baseInterfaces = null;
		
		switch (la.kind) {
		case 67: {

#line  410 "VBNET.ATG" 
			m.Check(Modifiers.Classes); 
			lexer.NextToken();

#line  413 "VBNET.ATG" 
			TypeDeclaration newType = new TypeDeclaration(m.Modifier, attributes);
			newType.StartLocation = t.Location;
			compilationUnit.AddChild(newType);
			compilationUnit.BlockStart(newType);
			
			newType.Type       = ClassType.Class;
			
			Identifier();

#line  420 "VBNET.ATG" 
			newType.Name = t.val; 
			TypeParameterList(
#line  421 "VBNET.ATG" 
newType.Templates);
			EndOfStmt();

#line  423 "VBNET.ATG" 
			newType.BodyStartLocation = t.Location; 
			if (la.kind == 110) {
				ClassBaseType(
#line  424 "VBNET.ATG" 
out typeRef);

#line  424 "VBNET.ATG" 
				newType.BaseTypes.Add(typeRef); 
			}
			while (la.kind == 107) {
				TypeImplementsClause(
#line  425 "VBNET.ATG" 
out baseInterfaces);

#line  425 "VBNET.ATG" 
				newType.BaseTypes.AddRange(baseInterfaces); 
			}
			ClassBody(
#line  426 "VBNET.ATG" 
newType);
			Expect(88);
			Expect(67);

#line  427 "VBNET.ATG" 
			newType.EndLocation = t.EndLocation; 
			Expect(1);

#line  430 "VBNET.ATG" 
			compilationUnit.BlockEnd();
			
			break;
		}
		case 121: {
			lexer.NextToken();

#line  434 "VBNET.ATG" 
			m.Check(Modifiers.VBModules);
			TypeDeclaration newType = new TypeDeclaration(m.Modifier, attributes);
			compilationUnit.AddChild(newType);
			compilationUnit.BlockStart(newType);
			newType.StartLocation = m.GetDeclarationLocation(t.Location);
			newType.Type = ClassType.Module;
			
			Identifier();

#line  441 "VBNET.ATG" 
			newType.Name = t.val; 
			Expect(1);

#line  443 "VBNET.ATG" 
			newType.BodyStartLocation = t.Location; 
			ModuleBody(
#line  444 "VBNET.ATG" 
newType);

#line  446 "VBNET.ATG" 
			compilationUnit.BlockEnd();
			
			break;
		}
		case 166: {
			lexer.NextToken();

#line  450 "VBNET.ATG" 
			m.Check(Modifiers.VBStructures);
			TypeDeclaration newType = new TypeDeclaration(m.Modifier, attributes);
			compilationUnit.AddChild(newType);
			compilationUnit.BlockStart(newType);
			newType.StartLocation = m.GetDeclarationLocation(t.Location);
			newType.Type = ClassType.Struct;
			
			Identifier();

#line  457 "VBNET.ATG" 
			newType.Name = t.val; 
			TypeParameterList(
#line  458 "VBNET.ATG" 
newType.Templates);
			Expect(1);

#line  460 "VBNET.ATG" 
			newType.BodyStartLocation = t.Location; 
			while (la.kind == 107) {
				TypeImplementsClause(
#line  461 "VBNET.ATG" 
out baseInterfaces);

#line  461 "VBNET.ATG" 
				newType.BaseTypes.AddRange(baseInterfaces);
			}
			StructureBody(
#line  462 "VBNET.ATG" 
newType);

#line  464 "VBNET.ATG" 
			compilationUnit.BlockEnd();
			
			break;
		}
		case 90: {
			lexer.NextToken();

#line  469 "VBNET.ATG" 
			m.Check(Modifiers.VBEnums);
			TypeDeclaration newType = new TypeDeclaration(m.Modifier, attributes);
			newType.StartLocation = m.GetDeclarationLocation(t.Location);
			compilationUnit.AddChild(newType);
			compilationUnit.BlockStart(newType);
			
			newType.Type = ClassType.Enum;
			
			Identifier();

#line  477 "VBNET.ATG" 
			newType.Name = t.val; 
			if (la.kind == 48) {
				lexer.NextToken();
				NonArrayTypeName(
#line  478 "VBNET.ATG" 
out typeRef, false);

#line  478 "VBNET.ATG" 
				newType.BaseTypes.Add(typeRef); 
			}
			Expect(1);

#line  480 "VBNET.ATG" 
			newType.BodyStartLocation = t.Location; 
			EnumBody(
#line  481 "VBNET.ATG" 
newType);

#line  483 "VBNET.ATG" 
			compilationUnit.BlockEnd();
			
			break;
		}
		case 112: {
			lexer.NextToken();

#line  488 "VBNET.ATG" 
			m.Check(Modifiers.VBInterfacs);
			TypeDeclaration newType = new TypeDeclaration(m.Modifier, attributes);
			newType.StartLocation = m.GetDeclarationLocation(t.Location);
			compilationUnit.AddChild(newType);
			compilationUnit.BlockStart(newType);
			newType.Type = ClassType.Interface;
			
			Identifier();

#line  495 "VBNET.ATG" 
			newType.Name = t.val; 
			TypeParameterList(
#line  496 "VBNET.ATG" 
newType.Templates);
			EndOfStmt();

#line  498 "VBNET.ATG" 
			newType.BodyStartLocation = t.Location; 
			while (la.kind == 110) {
				InterfaceBase(
#line  499 "VBNET.ATG" 
out baseInterfaces);

#line  499 "VBNET.ATG" 
				newType.BaseTypes.AddRange(baseInterfaces); 
			}
			InterfaceBody(
#line  500 "VBNET.ATG" 
newType);

#line  502 "VBNET.ATG" 
			compilationUnit.BlockEnd();
			
			break;
		}
		case 80: {
			lexer.NextToken();

#line  507 "VBNET.ATG" 
			m.Check(Modifiers.VBDelegates);
			DelegateDeclaration delegateDeclr = new DelegateDeclaration(m.Modifier, attributes);
			delegateDeclr.ReturnType = new TypeReference("", "System.Void");
			delegateDeclr.StartLocation = m.GetDeclarationLocation(t.Location);
			List<ParameterDeclarationExpression> p = new List<ParameterDeclarationExpression>();
			
			if (la.kind == 167) {
				lexer.NextToken();
				Identifier();

#line  514 "VBNET.ATG" 
				delegateDeclr.Name = t.val; 
				TypeParameterList(
#line  515 "VBNET.ATG" 
delegateDeclr.Templates);
				if (la.kind == 24) {
					lexer.NextToken();
					if (StartOf(4)) {
						FormalParameterList(
#line  516 "VBNET.ATG" 
p);
					}
					Expect(25);

#line  516 "VBNET.ATG" 
					delegateDeclr.Parameters = p; 
				}
			} else if (la.kind == 100) {
				lexer.NextToken();
				Identifier();

#line  518 "VBNET.ATG" 
				delegateDeclr.Name = t.val; 
				TypeParameterList(
#line  519 "VBNET.ATG" 
delegateDeclr.Templates);
				if (la.kind == 24) {
					lexer.NextToken();
					if (StartOf(4)) {
						FormalParameterList(
#line  520 "VBNET.ATG" 
p);
					}
					Expect(25);

#line  520 "VBNET.ATG" 
					delegateDeclr.Parameters = p; 
				}
				if (la.kind == 48) {
					lexer.NextToken();

#line  521 "VBNET.ATG" 
					TypeReference type; 
					TypeName(
#line  521 "VBNET.ATG" 
out type);

#line  521 "VBNET.ATG" 
					delegateDeclr.ReturnType = type; 
				}
			} else SynErr(215);

#line  523 "VBNET.ATG" 
			delegateDeclr.EndLocation = t.EndLocation; 
			Expect(1);

#line  526 "VBNET.ATG" 
			compilationUnit.AddChild(delegateDeclr);
			
			break;
		}
		default: SynErr(216); break;
		}
	}

	void TypeParameterList(
#line  350 "VBNET.ATG" 
List<TemplateDefinition> templates) {

#line  352 "VBNET.ATG" 
		TemplateDefinition template;
		
		if (
#line  355 "VBNET.ATG" 
la.kind == Tokens.OpenParenthesis && Peek(1).kind == Tokens.Of) {
			lexer.NextToken();
			Expect(201);
			TypeParameter(
#line  356 "VBNET.ATG" 
out template);

#line  358 "VBNET.ATG" 
			if (template != null) templates.Add(template);
			
			while (la.kind == 12) {
				lexer.NextToken();
				TypeParameter(
#line  361 "VBNET.ATG" 
out template);

#line  363 "VBNET.ATG" 
				if (template != null) templates.Add(template);
				
			}
			Expect(25);
		}
	}

	void TypeParameter(
#line  371 "VBNET.ATG" 
out TemplateDefinition template) {
		Identifier();

#line  373 "VBNET.ATG" 
		template = new TemplateDefinition(t.val, null); 
		if (la.kind == 48) {
			TypeParameterConstraints(
#line  374 "VBNET.ATG" 
template);
		}
	}

	void Identifier() {
		switch (la.kind) {
		case 2: {
			lexer.NextToken();
			break;
		}
		case 169: {
			lexer.NextToken();
			break;
		}
		case 51: {
			lexer.NextToken();
			break;
		}
		case 70: {
			lexer.NextToken();
			break;
		}
		case 205: {
			lexer.NextToken();
			break;
		}
		case 49: {
			lexer.NextToken();
			break;
		}
		case 47: {
			lexer.NextToken();
			break;
		}
		case 50: {
			lexer.NextToken();
			break;
		}
		case 144: {
			lexer.NextToken();
			break;
		}
		case 176: {
			lexer.NextToken();
			break;
		}
		case 177: {
			lexer.NextToken();
			break;
		}
		case 134: {
			lexer.NextToken();
			break;
		}
		case 95: {
			lexer.NextToken();
			break;
		}
		default: SynErr(217); break;
		}
	}

	void TypeParameterConstraints(
#line  378 "VBNET.ATG" 
TemplateDefinition template) {

#line  380 "VBNET.ATG" 
		TypeReference constraint;
		
		Expect(48);
		if (la.kind == 22) {
			lexer.NextToken();
			TypeParameterConstraint(
#line  386 "VBNET.ATG" 
out constraint);

#line  386 "VBNET.ATG" 
			if (constraint != null) { template.Bases.Add(constraint); } 
			while (la.kind == 12) {
				lexer.NextToken();
				TypeParameterConstraint(
#line  389 "VBNET.ATG" 
out constraint);

#line  389 "VBNET.ATG" 
				if (constraint != null) { template.Bases.Add(constraint); } 
			}
			Expect(23);
		} else if (StartOf(5)) {
			TypeParameterConstraint(
#line  392 "VBNET.ATG" 
out constraint);

#line  392 "VBNET.ATG" 
			if (constraint != null) { template.Bases.Add(constraint); } 
		} else SynErr(218);
	}

	void TypeParameterConstraint(
#line  396 "VBNET.ATG" 
out TypeReference constraint) {

#line  397 "VBNET.ATG" 
		constraint = null; 
		if (la.kind == 67) {
			lexer.NextToken();

#line  398 "VBNET.ATG" 
			constraint = TypeReference.ClassConstraint; 
		} else if (la.kind == 166) {
			lexer.NextToken();

#line  399 "VBNET.ATG" 
			constraint = TypeReference.StructConstraint; 
		} else if (la.kind == 127) {
			lexer.NextToken();

#line  400 "VBNET.ATG" 
			constraint = TypeReference.NewConstraint; 
		} else if (StartOf(6)) {
			TypeName(
#line  401 "VBNET.ATG" 
out constraint);
		} else SynErr(219);
	}

	void ClassBaseType(
#line  727 "VBNET.ATG" 
out TypeReference typeRef) {

#line  729 "VBNET.ATG" 
		typeRef = null;
		
		Expect(110);
		TypeName(
#line  732 "VBNET.ATG" 
out typeRef);
		EndOfStmt();
	}

	void TypeImplementsClause(
#line  1520 "VBNET.ATG" 
out List<TypeReference> baseInterfaces) {

#line  1522 "VBNET.ATG" 
		baseInterfaces = new List<TypeReference>();
		TypeReference type = null;
		
		Expect(107);
		TypeName(
#line  1525 "VBNET.ATG" 
out type);

#line  1527 "VBNET.ATG" 
		baseInterfaces.Add(type);
		
		while (la.kind == 12) {
			lexer.NextToken();
			TypeName(
#line  1530 "VBNET.ATG" 
out type);

#line  1531 "VBNET.ATG" 
			baseInterfaces.Add(type); 
		}
		EndOfStmt();
	}

	void ClassBody(
#line  536 "VBNET.ATG" 
TypeDeclaration newType) {

#line  537 "VBNET.ATG" 
		AttributeSection section; 
		while (StartOf(7)) {

#line  539 "VBNET.ATG" 
			List<AttributeSection> attributes = new List<AttributeSection>();
			ModifierList m = new ModifierList();
			
			while (la.kind == 27) {
				AttributeSection(
#line  542 "VBNET.ATG" 
out section);

#line  542 "VBNET.ATG" 
				attributes.Add(section); 
			}
			while (StartOf(8)) {
				MemberModifier(
#line  543 "VBNET.ATG" 
m);
			}
			ClassMemberDecl(
#line  544 "VBNET.ATG" 
m, attributes);
		}
	}

	void ModuleBody(
#line  563 "VBNET.ATG" 
TypeDeclaration newType) {

#line  564 "VBNET.ATG" 
		AttributeSection section; 
		while (StartOf(7)) {

#line  566 "VBNET.ATG" 
			List<AttributeSection> attributes = new List<AttributeSection>();
			ModifierList m = new ModifierList();
			
			while (la.kind == 27) {
				AttributeSection(
#line  569 "VBNET.ATG" 
out section);

#line  569 "VBNET.ATG" 
				attributes.Add(section); 
			}
			while (StartOf(8)) {
				MemberModifier(
#line  570 "VBNET.ATG" 
m);
			}
			ClassMemberDecl(
#line  571 "VBNET.ATG" 
m, attributes);
		}
		Expect(88);
		Expect(121);

#line  573 "VBNET.ATG" 
		newType.EndLocation = t.EndLocation; 
		Expect(1);
	}

	void StructureBody(
#line  548 "VBNET.ATG" 
TypeDeclaration newType) {

#line  549 "VBNET.ATG" 
		AttributeSection section; 
		while (StartOf(7)) {

#line  551 "VBNET.ATG" 
			List<AttributeSection> attributes = new List<AttributeSection>();
			ModifierList m = new ModifierList();
			
			while (la.kind == 27) {
				AttributeSection(
#line  554 "VBNET.ATG" 
out section);

#line  554 "VBNET.ATG" 
				attributes.Add(section); 
			}
			while (StartOf(8)) {
				MemberModifier(
#line  555 "VBNET.ATG" 
m);
			}
			StructureMemberDecl(
#line  556 "VBNET.ATG" 
m, attributes);
		}
		Expect(88);
		Expect(166);

#line  558 "VBNET.ATG" 
		newType.EndLocation = t.EndLocation; 
		Expect(1);
	}

	void NonArrayTypeName(
#line  1971 "VBNET.ATG" 
out TypeReference typeref, bool canBeUnbound) {

#line  1973 "VBNET.ATG" 
		string name;
		typeref = null;
		bool isGlobal = false;
		
		if (StartOf(9)) {
			if (la.kind == 199) {
				lexer.NextToken();
				Expect(10);

#line  1978 "VBNET.ATG" 
				isGlobal = true; 
			}
			QualIdentAndTypeArguments(
#line  1979 "VBNET.ATG" 
out typeref, canBeUnbound);

#line  1980 "VBNET.ATG" 
			typeref.IsGlobal = isGlobal; 
			while (la.kind == 10) {
				lexer.NextToken();

#line  1981 "VBNET.ATG" 
				TypeReference nestedTypeRef; 
				QualIdentAndTypeArguments(
#line  1982 "VBNET.ATG" 
out nestedTypeRef, canBeUnbound);

#line  1983 "VBNET.ATG" 
				typeref = new InnerClassTypeReference(typeref, nestedTypeRef.Type, nestedTypeRef.GenericTypes); 
			}
		} else if (la.kind == 133) {
			lexer.NextToken();

#line  1986 "VBNET.ATG" 
			typeref = new TypeReference("System.Object"); 
		} else if (StartOf(10)) {
			PrimitiveTypeName(
#line  1987 "VBNET.ATG" 
out name);

#line  1987 "VBNET.ATG" 
			typeref = new TypeReference(name); 
		} else SynErr(220);
	}

	void EnumBody(
#line  577 "VBNET.ATG" 
TypeDeclaration newType) {

#line  578 "VBNET.ATG" 
		FieldDeclaration f; 
		while (StartOf(11)) {
			EnumMemberDecl(
#line  580 "VBNET.ATG" 
out f);

#line  580 "VBNET.ATG" 
			compilationUnit.AddChild(f); 
		}
		Expect(88);
		Expect(90);

#line  582 "VBNET.ATG" 
		newType.EndLocation = t.EndLocation; 
		Expect(1);
	}

	void InterfaceBase(
#line  1505 "VBNET.ATG" 
out List<TypeReference> bases) {

#line  1507 "VBNET.ATG" 
		TypeReference type;
		bases = new List<TypeReference>();
		
		Expect(110);
		TypeName(
#line  1511 "VBNET.ATG" 
out type);

#line  1511 "VBNET.ATG" 
		bases.Add(type); 
		while (la.kind == 12) {
			lexer.NextToken();
			TypeName(
#line  1514 "VBNET.ATG" 
out type);

#line  1514 "VBNET.ATG" 
			bases.Add(type); 
		}
		Expect(1);
	}

	void InterfaceBody(
#line  586 "VBNET.ATG" 
TypeDeclaration newType) {
		while (StartOf(12)) {
			InterfaceMemberDecl();
		}
		Expect(88);
		Expect(112);

#line  588 "VBNET.ATG" 
		newType.EndLocation = t.EndLocation; 
		Expect(1);
	}

	void FormalParameterList(
#line  2165 "VBNET.ATG" 
List<ParameterDeclarationExpression> parameter) {

#line  2167 "VBNET.ATG" 
		ParameterDeclarationExpression p;
		AttributeSection section;
		List<AttributeSection> attributes = new List<AttributeSection>();
		
		while (la.kind == 27) {
			AttributeSection(
#line  2171 "VBNET.ATG" 
out section);

#line  2171 "VBNET.ATG" 
			attributes.Add(section); 
		}
		FormalParameter(
#line  2173 "VBNET.ATG" 
out p);

#line  2175 "VBNET.ATG" 
		bool paramsFound = false;
		p.Attributes = attributes;
		parameter.Add(p);
		
		while (la.kind == 12) {
			lexer.NextToken();

#line  2180 "VBNET.ATG" 
			if (paramsFound) Error("params array must be at end of parameter list"); 
			while (la.kind == 27) {
				AttributeSection(
#line  2181 "VBNET.ATG" 
out section);

#line  2181 "VBNET.ATG" 
				attributes.Add(section); 
			}
			FormalParameter(
#line  2183 "VBNET.ATG" 
out p);

#line  2183 "VBNET.ATG" 
			p.Attributes = attributes; parameter.Add(p); 
		}
	}

	void MemberModifier(
#line  2886 "VBNET.ATG" 
ModifierList m) {
		switch (la.kind) {
		case 122: {
			lexer.NextToken();

#line  2887 "VBNET.ATG" 
			m.Add(Modifiers.Abstract, t.Location);
			break;
		}
		case 79: {
			lexer.NextToken();

#line  2888 "VBNET.ATG" 
			m.Add(Modifiers.Default, t.Location);
			break;
		}
		case 99: {
			lexer.NextToken();

#line  2889 "VBNET.ATG" 
			m.Add(Modifiers.Internal, t.Location);
			break;
		}
		case 157: {
			lexer.NextToken();

#line  2890 "VBNET.ATG" 
			m.Add(Modifiers.New, t.Location);
			break;
		}
		case 142: {
			lexer.NextToken();

#line  2891 "VBNET.ATG" 
			m.Add(Modifiers.Override, t.Location);
			break;
		}
		case 123: {
			lexer.NextToken();

#line  2892 "VBNET.ATG" 
			m.Add(Modifiers.Abstract, t.Location);
			break;
		}
		case 145: {
			lexer.NextToken();

#line  2893 "VBNET.ATG" 
			m.Add(Modifiers.Private, t.Location);
			break;
		}
		case 147: {
			lexer.NextToken();

#line  2894 "VBNET.ATG" 
			m.Add(Modifiers.Protected, t.Location);
			break;
		}
		case 148: {
			lexer.NextToken();

#line  2895 "VBNET.ATG" 
			m.Add(Modifiers.Public, t.Location);
			break;
		}
		case 131: {
			lexer.NextToken();

#line  2896 "VBNET.ATG" 
			m.Add(Modifiers.Sealed, t.Location);
			break;
		}
		case 132: {
			lexer.NextToken();

#line  2897 "VBNET.ATG" 
			m.Add(Modifiers.Sealed, t.Location);
			break;
		}
		case 158: {
			lexer.NextToken();

#line  2898 "VBNET.ATG" 
			m.Add(Modifiers.Static, t.Location);
			break;
		}
		case 141: {
			lexer.NextToken();

#line  2899 "VBNET.ATG" 
			m.Add(Modifiers.Virtual, t.Location);
			break;
		}
		case 140: {
			lexer.NextToken();

#line  2900 "VBNET.ATG" 
			m.Add(Modifiers.Overloads, t.Location);
			break;
		}
		case 150: {
			lexer.NextToken();

#line  2901 "VBNET.ATG" 
			m.Add(Modifiers.ReadOnly, t.Location);
			break;
		}
		case 184: {
			lexer.NextToken();

#line  2902 "VBNET.ATG" 
			m.Add(Modifiers.WriteOnly, t.Location);
			break;
		}
		case 183: {
			lexer.NextToken();

#line  2903 "VBNET.ATG" 
			m.Add(Modifiers.WithEvents, t.Location);
			break;
		}
		case 81: {
			lexer.NextToken();

#line  2904 "VBNET.ATG" 
			m.Add(Modifiers.Dim, t.Location);
			break;
		}
		default: SynErr(221); break;
		}
	}

	void ClassMemberDecl(
#line  723 "VBNET.ATG" 
ModifierList m, List<AttributeSection> attributes) {
		StructureMemberDecl(
#line  724 "VBNET.ATG" 
m, attributes);
	}

	void StructureMemberDecl(
#line  737 "VBNET.ATG" 
ModifierList m, List<AttributeSection> attributes) {

#line  739 "VBNET.ATG" 
		TypeReference type = null;
		List<ParameterDeclarationExpression> p = new List<ParameterDeclarationExpression>();
		Statement stmt = null;
		List<VariableDeclaration> variableDeclarators = new List<VariableDeclaration>();
		List<TemplateDefinition> templates = new List<TemplateDefinition>();
		
		switch (la.kind) {
		case 67: case 80: case 90: case 112: case 121: case 166: {
			NonModuleDeclaration(
#line  746 "VBNET.ATG" 
m, attributes);
			break;
		}
		case 167: {
			lexer.NextToken();

#line  750 "VBNET.ATG" 
			Location startPos = t.Location;
			
			if (StartOf(13)) {

#line  754 "VBNET.ATG" 
				string name = String.Empty;
				MethodDeclaration methodDeclaration; List<string> handlesClause = null;
				List<InterfaceImplementation> implementsClause = null;
				
				Identifier();

#line  760 "VBNET.ATG" 
				name = t.val;
				m.Check(Modifiers.VBMethods);
				
				TypeParameterList(
#line  763 "VBNET.ATG" 
templates);
				if (la.kind == 24) {
					lexer.NextToken();
					if (StartOf(4)) {
						FormalParameterList(
#line  764 "VBNET.ATG" 
p);
					}
					Expect(25);
				}
				if (la.kind == 105 || la.kind == 107) {
					if (la.kind == 107) {
						ImplementsClause(
#line  767 "VBNET.ATG" 
out implementsClause);
					} else {
						HandlesClause(
#line  769 "VBNET.ATG" 
out handlesClause);
					}
				}

#line  772 "VBNET.ATG" 
				Location endLocation = t.EndLocation; 
				Expect(1);
				if (
#line  776 "VBNET.ATG" 
IsMustOverride(m)) {

#line  778 "VBNET.ATG" 
					methodDeclaration = new MethodDeclaration {
					Name = name, Modifier = m.Modifier, Parameters = p, Attributes = attributes,
					StartLocation = m.GetDeclarationLocation(startPos), EndLocation = endLocation,
					TypeReference = new TypeReference("", "System.Void"),
					Templates = templates,
					HandlesClause = handlesClause,
					InterfaceImplementations = implementsClause
					};
					compilationUnit.AddChild(methodDeclaration);
					
				} else if (StartOf(14)) {

#line  790 "VBNET.ATG" 
					methodDeclaration = new MethodDeclaration {
					Name = name, Modifier = m.Modifier, Parameters = p, Attributes = attributes,
					StartLocation = m.GetDeclarationLocation(startPos), EndLocation = endLocation,
					TypeReference = new TypeReference("", "System.Void"),
					Templates = templates,
					HandlesClause = handlesClause,
					InterfaceImplementations = implementsClause
					};
					compilationUnit.AddChild(methodDeclaration);
					

#line  801 "VBNET.ATG" 
					if (ParseMethodBodies) { 
					Block(
#line  802 "VBNET.ATG" 
out stmt);
					Expect(88);
					Expect(167);

#line  804 "VBNET.ATG" 
					} else {
					// don't parse method body
					lexer.SkipCurrentBlock(Tokens.Sub); stmt = new BlockStatement();
					  }
					

#line  810 "VBNET.ATG" 
					methodDeclaration.Body  = (BlockStatement)stmt; 

#line  811 "VBNET.ATG" 
					methodDeclaration.Body.EndLocation = t.EndLocation; 
					Expect(1);
				} else SynErr(222);
			} else if (la.kind == 127) {
				lexer.NextToken();
				if (la.kind == 24) {
					lexer.NextToken();
					if (StartOf(4)) {
						FormalParameterList(
#line  814 "VBNET.ATG" 
p);
					}
					Expect(25);
				}

#line  815 "VBNET.ATG" 
				m.Check(Modifiers.Constructors); 

#line  816 "VBNET.ATG" 
				Location constructorEndLocation = t.EndLocation; 
				Expect(1);

#line  819 "VBNET.ATG" 
				if (ParseMethodBodies) { 
				Block(
#line  820 "VBNET.ATG" 
out stmt);
				Expect(88);
				Expect(167);

#line  822 "VBNET.ATG" 
				} else {
				// don't parse method body
				lexer.SkipCurrentBlock(Tokens.Sub); stmt = new BlockStatement();
				  }
				

#line  828 "VBNET.ATG" 
				Location endLocation = t.EndLocation; 
				Expect(1);

#line  830 "VBNET.ATG" 
				ConstructorDeclaration cd = new ConstructorDeclaration("New", m.Modifier, p, attributes); 
				cd.StartLocation = m.GetDeclarationLocation(startPos);
				cd.EndLocation   = constructorEndLocation;
				cd.Body = (BlockStatement)stmt;
				cd.Body.EndLocation   = endLocation;
				compilationUnit.AddChild(cd);
				
			} else SynErr(223);
			break;
		}
		case 100: {
			lexer.NextToken();

#line  842 "VBNET.ATG" 
			m.Check(Modifiers.VBMethods);
			string name = String.Empty;
			Location startPos = t.Location;
			MethodDeclaration methodDeclaration;List<string> handlesClause = null;
			List<InterfaceImplementation> implementsClause = null;
			AttributeSection returnTypeAttributeSection = null;
			
			Identifier();

#line  849 "VBNET.ATG" 
			name = t.val; 
			TypeParameterList(
#line  850 "VBNET.ATG" 
templates);
			if (la.kind == 24) {
				lexer.NextToken();
				if (StartOf(4)) {
					FormalParameterList(
#line  851 "VBNET.ATG" 
p);
				}
				Expect(25);
			}
			if (la.kind == 48) {
				lexer.NextToken();
				while (la.kind == 27) {
					AttributeSection(
#line  852 "VBNET.ATG" 
out returnTypeAttributeSection);
				}
				TypeName(
#line  852 "VBNET.ATG" 
out type);
			}

#line  854 "VBNET.ATG" 
			if(type == null) {
			type = new TypeReference("System.Object");
			}
			
			if (la.kind == 105 || la.kind == 107) {
				if (la.kind == 107) {
					ImplementsClause(
#line  860 "VBNET.ATG" 
out implementsClause);
				} else {
					HandlesClause(
#line  862 "VBNET.ATG" 
out handlesClause);
				}
			}
			Expect(1);
			if (
#line  868 "VBNET.ATG" 
IsMustOverride(m)) {

#line  870 "VBNET.ATG" 
				methodDeclaration = new MethodDeclaration {
				Name = name, Modifier = m.Modifier, TypeReference = type,
				Parameters = p, Attributes = attributes,
				StartLocation = m.GetDeclarationLocation(startPos),
				EndLocation   = t.EndLocation,
				HandlesClause = handlesClause,
				Templates     = templates,
				InterfaceImplementations = implementsClause
				};
				if (returnTypeAttributeSection != null) {
					returnTypeAttributeSection.AttributeTarget = "return";
					methodDeclaration.Attributes.Add(returnTypeAttributeSection);
				}
				compilationUnit.AddChild(methodDeclaration);
				
			} else if (StartOf(14)) {

#line  887 "VBNET.ATG" 
				methodDeclaration = new MethodDeclaration {
				Name = name, Modifier = m.Modifier, TypeReference = type,
				Parameters = p, Attributes = attributes,
				StartLocation = m.GetDeclarationLocation(startPos),
				EndLocation   = t.EndLocation,
				Templates     = templates,
				HandlesClause = handlesClause,
				InterfaceImplementations = implementsClause
				};
				if (returnTypeAttributeSection != null) {
					returnTypeAttributeSection.AttributeTarget = "return";
					methodDeclaration.Attributes.Add(returnTypeAttributeSection);
				}
				
				compilationUnit.AddChild(methodDeclaration);
				
				if (ParseMethodBodies) { 
				Block(
#line  904 "VBNET.ATG" 
out stmt);
				Expect(88);
				Expect(100);

#line  906 "VBNET.ATG" 
				} else {
				// don't parse method body
				lexer.SkipCurrentBlock(Tokens.Function); stmt = new BlockStatement();
				}
				methodDeclaration.Body = (BlockStatement)stmt;
				methodDeclaration.Body.StartLocation = methodDeclaration.EndLocation;
				methodDeclaration.Body.EndLocation   = t.EndLocation;
				
				Expect(1);
			} else SynErr(224);
			break;
		}
		case 78: {
			lexer.NextToken();

#line  920 "VBNET.ATG" 
			m.Check(Modifiers.VBExternalMethods);
			Location startPos = t.Location;
			CharsetModifier charsetModifer = CharsetModifier.None;
			string library = String.Empty;
			string alias = null;
			string name = String.Empty;
			
			if (StartOf(15)) {
				Charset(
#line  927 "VBNET.ATG" 
out charsetModifer);
			}
			if (la.kind == 167) {
				lexer.NextToken();
				Identifier();

#line  930 "VBNET.ATG" 
				name = t.val; 
				Expect(115);
				Expect(3);

#line  931 "VBNET.ATG" 
				library = t.literalValue as string; 
				if (la.kind == 44) {
					lexer.NextToken();
					Expect(3);

#line  932 "VBNET.ATG" 
					alias = t.literalValue as string; 
				}
				if (la.kind == 24) {
					lexer.NextToken();
					if (StartOf(4)) {
						FormalParameterList(
#line  933 "VBNET.ATG" 
p);
					}
					Expect(25);
				}
				Expect(1);

#line  936 "VBNET.ATG" 
				DeclareDeclaration declareDeclaration = new DeclareDeclaration(name, m.Modifier, null, p, attributes, library, alias, charsetModifer);
				declareDeclaration.StartLocation = m.GetDeclarationLocation(startPos);
				declareDeclaration.EndLocation   = t.EndLocation;
				compilationUnit.AddChild(declareDeclaration);
				
			} else if (la.kind == 100) {
				lexer.NextToken();
				Identifier();

#line  943 "VBNET.ATG" 
				name = t.val; 
				Expect(115);
				Expect(3);

#line  944 "VBNET.ATG" 
				library = t.literalValue as string; 
				if (la.kind == 44) {
					lexer.NextToken();
					Expect(3);

#line  945 "VBNET.ATG" 
					alias = t.literalValue as string; 
				}
				if (la.kind == 24) {
					lexer.NextToken();
					if (StartOf(4)) {
						FormalParameterList(
#line  946 "VBNET.ATG" 
p);
					}
					Expect(25);
				}
				if (la.kind == 48) {
					lexer.NextToken();
					TypeName(
#line  947 "VBNET.ATG" 
out type);
				}
				Expect(1);

#line  950 "VBNET.ATG" 
				DeclareDeclaration declareDeclaration = new DeclareDeclaration(name, m.Modifier, type, p, attributes, library, alias, charsetModifer);
				declareDeclaration.StartLocation = m.GetDeclarationLocation(startPos);
				declareDeclaration.EndLocation   = t.EndLocation;
				compilationUnit.AddChild(declareDeclaration);
				
			} else SynErr(225);
			break;
		}
		case 93: {
			lexer.NextToken();

#line  960 "VBNET.ATG" 
			m.Check(Modifiers.VBEvents);
			Location startPos = t.Location;
			EventDeclaration eventDeclaration;
			string name = String.Empty;
			List<InterfaceImplementation> implementsClause = null;
			
			Identifier();

#line  966 "VBNET.ATG" 
			name= t.val; 
			if (la.kind == 48) {
				lexer.NextToken();
				TypeName(
#line  968 "VBNET.ATG" 
out type);
			} else if (la.kind == 1 || la.kind == 24 || la.kind == 107) {
				if (la.kind == 24) {
					lexer.NextToken();
					if (StartOf(4)) {
						FormalParameterList(
#line  970 "VBNET.ATG" 
p);
					}
					Expect(25);
				}
			} else SynErr(226);
			if (la.kind == 107) {
				ImplementsClause(
#line  972 "VBNET.ATG" 
out implementsClause);
			}

#line  974 "VBNET.ATG" 
			eventDeclaration = new EventDeclaration {
			Name = name, TypeReference = type, Modifier = m.Modifier, 
			Parameters = p, Attributes = attributes, InterfaceImplementations = implementsClause,
			StartLocation = m.GetDeclarationLocation(startPos),
			EndLocation = t.EndLocation
			};
			compilationUnit.AddChild(eventDeclaration);
			
			Expect(1);
			break;
		}
		case 2: case 47: case 49: case 50: case 51: case 70: case 95: case 134: case 144: case 169: case 176: case 177: {

#line  984 "VBNET.ATG" 
			Location startPos = t.Location; 

#line  986 "VBNET.ATG" 
			m.Check(Modifiers.Fields);
			FieldDeclaration fd = new FieldDeclaration(attributes, null, m.Modifier);
			fd.StartLocation = m.GetDeclarationLocation(startPos); 
			
			IdentifierForFieldDeclaration();

#line  990 "VBNET.ATG" 
			string name = t.val; 
			VariableDeclaratorPartAfterIdentifier(
#line  991 "VBNET.ATG" 
variableDeclarators, name);
			while (la.kind == 12) {
				lexer.NextToken();
				VariableDeclarator(
#line  992 "VBNET.ATG" 
variableDeclarators);
			}
			Expect(1);

#line  995 "VBNET.ATG" 
			fd.EndLocation = t.EndLocation;
			fd.Fields = variableDeclarators;
			compilationUnit.AddChild(fd);
			
			break;
		}
		case 71: {

#line  1000 "VBNET.ATG" 
			m.Check(Modifiers.Fields); 
			lexer.NextToken();

#line  1001 "VBNET.ATG" 
			m.Add(Modifiers.Const, t.Location);  

#line  1003 "VBNET.ATG" 
			FieldDeclaration fd = new FieldDeclaration(attributes, type, m.Modifier);
			fd.StartLocation = m.GetDeclarationLocation(t.Location);
			List<VariableDeclaration> constantDeclarators = new List<VariableDeclaration>();
			
			ConstantDeclarator(
#line  1007 "VBNET.ATG" 
constantDeclarators);
			while (la.kind == 12) {
				lexer.NextToken();
				ConstantDeclarator(
#line  1008 "VBNET.ATG" 
constantDeclarators);
			}

#line  1010 "VBNET.ATG" 
			fd.Fields = constantDeclarators;
			fd.EndLocation = t.Location;
			
			Expect(1);

#line  1015 "VBNET.ATG" 
			fd.EndLocation = t.EndLocation;
			compilationUnit.AddChild(fd);
			
			break;
		}
		case 146: {
			lexer.NextToken();

#line  1021 "VBNET.ATG" 
			m.Check(Modifiers.VBProperties);
			Location startPos = t.Location;
			List<InterfaceImplementation> implementsClause = null;
			
			Identifier();

#line  1025 "VBNET.ATG" 
			string propertyName = t.val; 
			if (la.kind == 24) {
				lexer.NextToken();
				if (StartOf(4)) {
					FormalParameterList(
#line  1026 "VBNET.ATG" 
p);
				}
				Expect(25);
			}
			if (la.kind == 48) {
				lexer.NextToken();
				TypeName(
#line  1027 "VBNET.ATG" 
out type);
			}

#line  1029 "VBNET.ATG" 
			if(type == null) {
			type = new TypeReference("System.Object");
			}
			
			if (la.kind == 107) {
				ImplementsClause(
#line  1033 "VBNET.ATG" 
out implementsClause);
			}
			Expect(1);
			if (
#line  1037 "VBNET.ATG" 
IsMustOverride(m)) {

#line  1039 "VBNET.ATG" 
				PropertyDeclaration pDecl = new PropertyDeclaration(propertyName, type, m.Modifier, attributes);
				pDecl.StartLocation = m.GetDeclarationLocation(startPos);
				pDecl.EndLocation   = t.Location;
				pDecl.TypeReference = type;
				pDecl.InterfaceImplementations = implementsClause;
				pDecl.Parameters = p;
				compilationUnit.AddChild(pDecl);
				
			} else if (StartOf(16)) {

#line  1049 "VBNET.ATG" 
				PropertyDeclaration pDecl = new PropertyDeclaration(propertyName, type, m.Modifier, attributes);
				pDecl.StartLocation = m.GetDeclarationLocation(startPos);
				pDecl.EndLocation   = t.Location;
				pDecl.BodyStart   = t.Location;
				pDecl.TypeReference = type;
				pDecl.InterfaceImplementations = implementsClause;
				pDecl.Parameters = p;
				PropertyGetRegion getRegion;
				PropertySetRegion setRegion;
				
				AccessorDecls(
#line  1059 "VBNET.ATG" 
out getRegion, out setRegion);
				Expect(88);
				Expect(146);
				Expect(1);

#line  1063 "VBNET.ATG" 
				pDecl.GetRegion = getRegion;
				pDecl.SetRegion = setRegion;
				pDecl.BodyEnd = t.EndLocation;
				compilationUnit.AddChild(pDecl);
				
			} else SynErr(227);
			break;
		}
		case 205: {
			lexer.NextToken();

#line  1070 "VBNET.ATG" 
			Location startPos = t.Location; 
			Expect(93);

#line  1072 "VBNET.ATG" 
			m.Check(Modifiers.VBCustomEvents);
			EventAddRemoveRegion eventAccessorDeclaration;
			EventAddRegion addHandlerAccessorDeclaration = null;
			EventRemoveRegion removeHandlerAccessorDeclaration = null;
			EventRaiseRegion raiseEventAccessorDeclaration = null;
			List<InterfaceImplementation> implementsClause = null;
			
			Identifier();

#line  1079 "VBNET.ATG" 
			string customEventName = t.val; 
			Expect(48);
			TypeName(
#line  1080 "VBNET.ATG" 
out type);
			if (la.kind == 107) {
				ImplementsClause(
#line  1081 "VBNET.ATG" 
out implementsClause);
			}
			Expect(1);
			while (StartOf(17)) {
				EventAccessorDeclaration(
#line  1084 "VBNET.ATG" 
out eventAccessorDeclaration);

#line  1086 "VBNET.ATG" 
				if(eventAccessorDeclaration is EventAddRegion)
				{
					addHandlerAccessorDeclaration = (EventAddRegion)eventAccessorDeclaration;
				}
				else if(eventAccessorDeclaration is EventRemoveRegion)
				{
					removeHandlerAccessorDeclaration = (EventRemoveRegion)eventAccessorDeclaration;
				}
				else if(eventAccessorDeclaration is EventRaiseRegion)
				{
					raiseEventAccessorDeclaration = (EventRaiseRegion)eventAccessorDeclaration;
				}
				
			}
			Expect(88);
			Expect(93);
			Expect(1);

#line  1102 "VBNET.ATG" 
			if(addHandlerAccessorDeclaration == null)
			{
				Error("Need to provide AddHandler accessor.");
			}
			
			if(removeHandlerAccessorDeclaration == null)
			{
				Error("Need to provide RemoveHandler accessor.");
			}
			
			if(raiseEventAccessorDeclaration == null)
			{
				Error("Need to provide RaiseEvent accessor.");
			}
			
			EventDeclaration decl = new EventDeclaration {
				TypeReference = type, Name = customEventName, Modifier = m.Modifier,
				Attributes = attributes,
				StartLocation = m.GetDeclarationLocation(startPos),
				EndLocation = t.EndLocation,
				AddRegion = addHandlerAccessorDeclaration,
				RemoveRegion = removeHandlerAccessorDeclaration,
				RaiseRegion = raiseEventAccessorDeclaration
			};
			compilationUnit.AddChild(decl);
			
			break;
		}
		case 188: case 202: case 203: {

#line  1128 "VBNET.ATG" 
			ConversionType opConversionType = ConversionType.None; 
			if (la.kind == 202 || la.kind == 203) {
				if (la.kind == 203) {
					lexer.NextToken();

#line  1129 "VBNET.ATG" 
					opConversionType = ConversionType.Implicit; 
				} else {
					lexer.NextToken();

#line  1130 "VBNET.ATG" 
					opConversionType = ConversionType.Explicit;
				}
			}
			Expect(188);

#line  1133 "VBNET.ATG" 
			m.Check(Modifiers.VBOperators);
			Location startPos = t.Location;
			TypeReference returnType = NullTypeReference.Instance;
			TypeReference operandType = NullTypeReference.Instance;
			string operandName;
			OverloadableOperatorType operatorType;
			AttributeSection section;
			List<ParameterDeclarationExpression> parameters = new List<ParameterDeclarationExpression>();
			List<AttributeSection> returnTypeAttributes = new List<AttributeSection>();
			
			OverloadableOperator(
#line  1143 "VBNET.ATG" 
out operatorType);
			Expect(24);
			if (la.kind == 55) {
				lexer.NextToken();
			}
			Identifier();

#line  1144 "VBNET.ATG" 
			operandName = t.val; 
			if (la.kind == 48) {
				lexer.NextToken();
				TypeName(
#line  1145 "VBNET.ATG" 
out operandType);
			}

#line  1146 "VBNET.ATG" 
			parameters.Add(new ParameterDeclarationExpression(operandType, operandName, ParameterModifiers.In)); 
			while (la.kind == 12) {
				lexer.NextToken();
				if (la.kind == 55) {
					lexer.NextToken();
				}
				Identifier();

#line  1150 "VBNET.ATG" 
				operandName = t.val; 
				if (la.kind == 48) {
					lexer.NextToken();
					TypeName(
#line  1151 "VBNET.ATG" 
out operandType);
				}

#line  1152 "VBNET.ATG" 
				parameters.Add(new ParameterDeclarationExpression(operandType, operandName, ParameterModifiers.In)); 
			}
			Expect(25);

#line  1155 "VBNET.ATG" 
			Location endPos = t.EndLocation; 
			if (la.kind == 48) {
				lexer.NextToken();
				while (la.kind == 27) {
					AttributeSection(
#line  1156 "VBNET.ATG" 
out section);

#line  1156 "VBNET.ATG" 
					returnTypeAttributes.Add(section); 
				}
				TypeName(
#line  1156 "VBNET.ATG" 
out returnType);

#line  1156 "VBNET.ATG" 
				endPos = t.EndLocation; 
				Expect(1);
			}
			Block(
#line  1157 "VBNET.ATG" 
out stmt);
			Expect(88);
			Expect(188);
			Expect(1);

#line  1159 "VBNET.ATG" 
			OperatorDeclaration operatorDeclaration = new OperatorDeclaration {
			Modifier = m.Modifier,
			Attributes = attributes,
			Parameters = parameters,
			TypeReference = returnType,
			OverloadableOperator = operatorType,
			ConversionType = opConversionType,
			ReturnTypeAttributes = returnTypeAttributes,
			Body = (BlockStatement)stmt,
			StartLocation = m.GetDeclarationLocation(startPos),
			EndLocation = endPos
			};
			operatorDeclaration.Body.StartLocation = startPos;
			operatorDeclaration.Body.EndLocation = t.Location;
			compilationUnit.AddChild(operatorDeclaration);
			
			break;
		}
		default: SynErr(228); break;
		}
	}

	void EnumMemberDecl(
#line  705 "VBNET.ATG" 
out FieldDeclaration f) {

#line  707 "VBNET.ATG" 
		Expression expr = null;List<AttributeSection> attributes = new List<AttributeSection>();
		AttributeSection section = null;
		VariableDeclaration varDecl = null;
		
		while (la.kind == 27) {
			AttributeSection(
#line  711 "VBNET.ATG" 
out section);

#line  711 "VBNET.ATG" 
			attributes.Add(section); 
		}
		Identifier();

#line  714 "VBNET.ATG" 
		f = new FieldDeclaration(attributes);
		varDecl = new VariableDeclaration(t.val);
		f.Fields.Add(varDecl);
		f.StartLocation = varDecl.StartLocation = t.Location;
		
		if (la.kind == 11) {
			lexer.NextToken();
			Expr(
#line  719 "VBNET.ATG" 
out expr);

#line  719 "VBNET.ATG" 
			varDecl.Initializer = expr; 
		}
		Expect(1);
	}

	void InterfaceMemberDecl() {

#line  596 "VBNET.ATG" 
		TypeReference type =null;
		List<ParameterDeclarationExpression> p = new List<ParameterDeclarationExpression>();
		List<TemplateDefinition> templates = new List<TemplateDefinition>();
		AttributeSection section, returnTypeAttributeSection = null;
		ModifierList mod = new ModifierList();
		List<AttributeSection> attributes = new List<AttributeSection>();
		string name;
		
		if (StartOf(18)) {
			while (la.kind == 27) {
				AttributeSection(
#line  604 "VBNET.ATG" 
out section);

#line  604 "VBNET.ATG" 
				attributes.Add(section); 
			}
			while (StartOf(8)) {
				MemberModifier(
#line  607 "VBNET.ATG" 
mod);
			}
			if (la.kind == 93) {
				lexer.NextToken();

#line  611 "VBNET.ATG" 
				mod.Check(Modifiers.VBInterfaceEvents);
				Location startLocation = t.Location;
				
				Identifier();

#line  614 "VBNET.ATG" 
				name = t.val; 
				if (la.kind == 24) {
					lexer.NextToken();
					if (StartOf(4)) {
						FormalParameterList(
#line  615 "VBNET.ATG" 
p);
					}
					Expect(25);
				}
				if (la.kind == 48) {
					lexer.NextToken();
					TypeName(
#line  616 "VBNET.ATG" 
out type);
				}
				Expect(1);

#line  619 "VBNET.ATG" 
				EventDeclaration ed = new EventDeclaration {
				Name = name, TypeReference = type, Modifier = mod.Modifier,
				Parameters = p, Attributes = attributes,
				StartLocation = startLocation, EndLocation = t.EndLocation
				};
				compilationUnit.AddChild(ed);
				
			} else if (la.kind == 167) {
				lexer.NextToken();

#line  629 "VBNET.ATG" 
				Location startLocation =  t.Location;
				mod.Check(Modifiers.VBInterfaceMethods);
				
				Identifier();

#line  632 "VBNET.ATG" 
				name = t.val; 
				TypeParameterList(
#line  633 "VBNET.ATG" 
templates);
				if (la.kind == 24) {
					lexer.NextToken();
					if (StartOf(4)) {
						FormalParameterList(
#line  634 "VBNET.ATG" 
p);
					}
					Expect(25);
				}
				Expect(1);

#line  637 "VBNET.ATG" 
				MethodDeclaration md = new MethodDeclaration {
				Name = name, 
				Modifier = mod.Modifier, 
				Parameters = p,
				Attributes = attributes,
				TypeReference = new TypeReference("", "System.Void"),
				StartLocation = startLocation,
				EndLocation = t.EndLocation,
				Templates = templates
				};
				compilationUnit.AddChild(md);
				
			} else if (la.kind == 100) {
				lexer.NextToken();

#line  652 "VBNET.ATG" 
				mod.Check(Modifiers.VBInterfaceMethods);
				Location startLocation = t.Location;
				
				Identifier();

#line  655 "VBNET.ATG" 
				name = t.val; 
				TypeParameterList(
#line  656 "VBNET.ATG" 
templates);
				if (la.kind == 24) {
					lexer.NextToken();
					if (StartOf(4)) {
						FormalParameterList(
#line  657 "VBNET.ATG" 
p);
					}
					Expect(25);
				}
				if (la.kind == 48) {
					lexer.NextToken();
					while (la.kind == 27) {
						AttributeSection(
#line  658 "VBNET.ATG" 
out returnTypeAttributeSection);
					}
					TypeName(
#line  658 "VBNET.ATG" 
out type);
				}

#line  660 "VBNET.ATG" 
				if(type == null) {
				type = new TypeReference("System.Object");
				}
				MethodDeclaration md = new MethodDeclaration {
					Name = name, Modifier = mod.Modifier, 
					TypeReference = type, Parameters = p, Attributes = attributes
				};
				if (returnTypeAttributeSection != null) {
					returnTypeAttributeSection.AttributeTarget = "return";
					md.Attributes.Add(returnTypeAttributeSection);
				}
				md.StartLocation = startLocation;
				md.EndLocation = t.EndLocation;
				md.Templates = templates;
				compilationUnit.AddChild(md);
				
				Expect(1);
			} else if (la.kind == 146) {
				lexer.NextToken();

#line  680 "VBNET.ATG" 
				Location startLocation = t.Location;
				mod.Check(Modifiers.VBInterfaceProperties);
				
				Identifier();

#line  683 "VBNET.ATG" 
				name = t.val;  
				if (la.kind == 24) {
					lexer.NextToken();
					if (StartOf(4)) {
						FormalParameterList(
#line  684 "VBNET.ATG" 
p);
					}
					Expect(25);
				}
				if (la.kind == 48) {
					lexer.NextToken();
					TypeName(
#line  685 "VBNET.ATG" 
out type);
				}

#line  687 "VBNET.ATG" 
				if(type == null) {
				type = new TypeReference("System.Object");
				}
				
				Expect(1);

#line  693 "VBNET.ATG" 
				PropertyDeclaration pd = new PropertyDeclaration(name, type, mod.Modifier, attributes);
				pd.Parameters = p;
				pd.EndLocation = t.EndLocation;
				pd.StartLocation = startLocation;
				compilationUnit.AddChild(pd);
				
			} else SynErr(229);
		} else if (StartOf(19)) {
			NonModuleDeclaration(
#line  701 "VBNET.ATG" 
mod, attributes);
		} else SynErr(230);
	}

	void Expr(
#line  1564 "VBNET.ATG" 
out Expression expr) {
		DisjunctionExpr(
#line  1566 "VBNET.ATG" 
out expr);
	}

	void ImplementsClause(
#line  1537 "VBNET.ATG" 
out List<InterfaceImplementation> baseInterfaces) {

#line  1539 "VBNET.ATG" 
		baseInterfaces = new List<InterfaceImplementation>();
		TypeReference type = null;
		string memberName = null;
		
		Expect(107);
		NonArrayTypeName(
#line  1544 "VBNET.ATG" 
out type, false);

#line  1545 "VBNET.ATG" 
		if (type != null) memberName = TypeReference.StripLastIdentifierFromType(ref type); 

#line  1546 "VBNET.ATG" 
		baseInterfaces.Add(new InterfaceImplementation(type, memberName)); 
		while (la.kind == 12) {
			lexer.NextToken();
			NonArrayTypeName(
#line  1548 "VBNET.ATG" 
out type, false);

#line  1549 "VBNET.ATG" 
			if (type != null) memberName = TypeReference.StripLastIdentifierFromType(ref type); 

#line  1550 "VBNET.ATG" 
			baseInterfaces.Add(new InterfaceImplementation(type, memberName)); 
		}
	}

	void HandlesClause(
#line  1495 "VBNET.ATG" 
out List<string> handlesClause) {

#line  1497 "VBNET.ATG" 
		handlesClause = new List<string>();
		string name;
		
		Expect(105);
		EventMemberSpecifier(
#line  1500 "VBNET.ATG" 
out name);

#line  1500 "VBNET.ATG" 
		handlesClause.Add(name); 
		while (la.kind == 12) {
			lexer.NextToken();
			EventMemberSpecifier(
#line  1501 "VBNET.ATG" 
out name);

#line  1501 "VBNET.ATG" 
			handlesClause.Add(name); 
		}
	}

	void Block(
#line  2221 "VBNET.ATG" 
out Statement stmt) {

#line  2224 "VBNET.ATG" 
		BlockStatement blockStmt = new BlockStatement();
		/* in snippet parsing mode, t might be null */
		if (t != null) blockStmt.StartLocation = t.Location;
		compilationUnit.BlockStart(blockStmt);
		
		while (StartOf(20) || 
#line  2230 "VBNET.ATG" 
IsEndStmtAhead()) {
			if (
#line  2230 "VBNET.ATG" 
IsEndStmtAhead()) {
				Expect(88);
				EndOfStmt();

#line  2230 "VBNET.ATG" 
				compilationUnit.AddChild(new EndStatement()); 
			} else {
				Statement();
				EndOfStmt();
			}
		}

#line  2235 "VBNET.ATG" 
		stmt = blockStmt;
		if (t != null) blockStmt.EndLocation = t.EndLocation;
		compilationUnit.BlockEnd();
		
	}

	void Charset(
#line  1487 "VBNET.ATG" 
out CharsetModifier charsetModifier) {

#line  1488 "VBNET.ATG" 
		charsetModifier = CharsetModifier.None; 
		if (la.kind == 100 || la.kind == 167) {
		} else if (la.kind == 47) {
			lexer.NextToken();

#line  1489 "VBNET.ATG" 
			charsetModifier = CharsetModifier.Ansi; 
		} else if (la.kind == 50) {
			lexer.NextToken();

#line  1490 "VBNET.ATG" 
			charsetModifier = CharsetModifier.Auto; 
		} else if (la.kind == 176) {
			lexer.NextToken();

#line  1491 "VBNET.ATG" 
			charsetModifier = CharsetModifier.Unicode; 
		} else SynErr(231);
	}

	void IdentifierForFieldDeclaration() {
		switch (la.kind) {
		case 2: {
			lexer.NextToken();
			break;
		}
		case 169: {
			lexer.NextToken();
			break;
		}
		case 51: {
			lexer.NextToken();
			break;
		}
		case 70: {
			lexer.NextToken();
			break;
		}
		case 49: {
			lexer.NextToken();
			break;
		}
		case 47: {
			lexer.NextToken();
			break;
		}
		case 50: {
			lexer.NextToken();
			break;
		}
		case 144: {
			lexer.NextToken();
			break;
		}
		case 176: {
			lexer.NextToken();
			break;
		}
		case 177: {
			lexer.NextToken();
			break;
		}
		case 134: {
			lexer.NextToken();
			break;
		}
		case 95: {
			lexer.NextToken();
			break;
		}
		default: SynErr(232); break;
		}
	}

	void VariableDeclaratorPartAfterIdentifier(
#line  1363 "VBNET.ATG" 
List<VariableDeclaration> fieldDeclaration, string name) {

#line  1365 "VBNET.ATG" 
		Expression expr = null;
		TypeReference type = null;
		ArrayList rank = null;
		List<Expression> dimension = null;
		Location startLocation = t.Location;
		
		if (
#line  1371 "VBNET.ATG" 
IsSize() && !IsDims()) {
			ArrayInitializationModifier(
#line  1371 "VBNET.ATG" 
out dimension);
		}
		if (
#line  1372 "VBNET.ATG" 
IsDims()) {
			ArrayNameModifier(
#line  1372 "VBNET.ATG" 
out rank);
		}
		if (
#line  1374 "VBNET.ATG" 
IsObjectCreation()) {
			Expect(48);
			ObjectCreateExpression(
#line  1374 "VBNET.ATG" 
out expr);

#line  1376 "VBNET.ATG" 
			if (expr is ObjectCreateExpression) {
			type = ((ObjectCreateExpression)expr).CreateType;
			} else {
				type = ((ArrayCreateExpression)expr).CreateType;
			}
			
		} else if (StartOf(21)) {
			if (la.kind == 48) {
				lexer.NextToken();
				TypeName(
#line  1383 "VBNET.ATG" 
out type);

#line  1385 "VBNET.ATG" 
				if (type != null) {
				for (int i = fieldDeclaration.Count - 1; i >= 0; i--) {
					VariableDeclaration vd = fieldDeclaration[i];
					if (vd.TypeReference.Type.Length > 0) break;
					TypeReference newType = type.Clone();
					newType.RankSpecifier = vd.TypeReference.RankSpecifier;
					vd.TypeReference = newType;
				}
				}
				 
			}

#line  1397 "VBNET.ATG" 
			if (type == null && (dimension != null || rank != null)) {
			type = new TypeReference("");
			}
			if (dimension != null) {
				if(type.RankSpecifier != null) {
					Error("array rank only allowed one time");
				} else {
					if (rank == null) {
						type.RankSpecifier = new int[] { dimension.Count - 1 };
					} else {
						rank.Insert(0, dimension.Count - 1);
						type.RankSpecifier = (int[])rank.ToArray(typeof(int));
					}
					expr = new ArrayCreateExpression(type, dimension);
				}
			} else if (rank != null) {
				if(type.RankSpecifier != null) {
					Error("array rank only allowed one time");
				} else {
					type.RankSpecifier = (int[])rank.ToArray(typeof(int));
				}
			}
			
			if (la.kind == 11) {
				lexer.NextToken();
				VariableInitializer(
#line  1420 "VBNET.ATG" 
out expr);
			}
		} else SynErr(233);

#line  1423 "VBNET.ATG" 
		VariableDeclaration varDecl = new VariableDeclaration(name, expr, type);
		varDecl.StartLocation = startLocation;
		varDecl.EndLocation = t.Location;
		fieldDeclaration.Add(varDecl);
		
	}

	void VariableDeclarator(
#line  1357 "VBNET.ATG" 
List<VariableDeclaration> fieldDeclaration) {
		Identifier();

#line  1359 "VBNET.ATG" 
		string name = t.val; 
		VariableDeclaratorPartAfterIdentifier(
#line  1360 "VBNET.ATG" 
fieldDeclaration, name);
	}

	void ConstantDeclarator(
#line  1338 "VBNET.ATG" 
List<VariableDeclaration> constantDeclaration) {

#line  1340 "VBNET.ATG" 
		Expression expr = null;
		TypeReference type = null;
		string name = String.Empty;
		Location location;
		
		Identifier();

#line  1345 "VBNET.ATG" 
		name = t.val; location = t.Location; 
		if (la.kind == 48) {
			lexer.NextToken();
			TypeName(
#line  1346 "VBNET.ATG" 
out type);
		}
		Expect(11);
		Expr(
#line  1347 "VBNET.ATG" 
out expr);

#line  1349 "VBNET.ATG" 
		VariableDeclaration f = new VariableDeclaration(name, expr);
		f.TypeReference = type;
		f.StartLocation = location;
		constantDeclaration.Add(f);
		
	}

	void AccessorDecls(
#line  1272 "VBNET.ATG" 
out PropertyGetRegion getBlock, out PropertySetRegion setBlock) {

#line  1274 "VBNET.ATG" 
		List<AttributeSection> attributes = new List<AttributeSection>();
		AttributeSection section;
		getBlock = null;
		setBlock = null; 
		
		while (la.kind == 27) {
			AttributeSection(
#line  1279 "VBNET.ATG" 
out section);

#line  1279 "VBNET.ATG" 
			attributes.Add(section); 
		}
		if (StartOf(22)) {
			GetAccessorDecl(
#line  1281 "VBNET.ATG" 
out getBlock, attributes);
			if (StartOf(23)) {

#line  1283 "VBNET.ATG" 
				attributes = new List<AttributeSection>(); 
				while (la.kind == 27) {
					AttributeSection(
#line  1284 "VBNET.ATG" 
out section);

#line  1284 "VBNET.ATG" 
					attributes.Add(section); 
				}
				SetAccessorDecl(
#line  1285 "VBNET.ATG" 
out setBlock, attributes);
			}
		} else if (StartOf(24)) {
			SetAccessorDecl(
#line  1288 "VBNET.ATG" 
out setBlock, attributes);
			if (StartOf(25)) {

#line  1290 "VBNET.ATG" 
				attributes = new List<AttributeSection>(); 
				while (la.kind == 27) {
					AttributeSection(
#line  1291 "VBNET.ATG" 
out section);

#line  1291 "VBNET.ATG" 
					attributes.Add(section); 
				}
				GetAccessorDecl(
#line  1292 "VBNET.ATG" 
out getBlock, attributes);
			}
		} else SynErr(234);
	}

	void EventAccessorDeclaration(
#line  1235 "VBNET.ATG" 
out EventAddRemoveRegion eventAccessorDeclaration) {

#line  1237 "VBNET.ATG" 
		Statement stmt = null;
		List<ParameterDeclarationExpression> p = new List<ParameterDeclarationExpression>();
		AttributeSection section;
		List<AttributeSection> attributes = new List<AttributeSection>();
		eventAccessorDeclaration = null;
		
		while (la.kind == 27) {
			AttributeSection(
#line  1243 "VBNET.ATG" 
out section);

#line  1243 "VBNET.ATG" 
			attributes.Add(section); 
		}
		if (la.kind == 42) {
			lexer.NextToken();
			if (la.kind == 24) {
				lexer.NextToken();
				if (StartOf(4)) {
					FormalParameterList(
#line  1245 "VBNET.ATG" 
p);
				}
				Expect(25);
			}
			Expect(1);
			Block(
#line  1246 "VBNET.ATG" 
out stmt);
			Expect(88);
			Expect(42);
			Expect(1);

#line  1248 "VBNET.ATG" 
			eventAccessorDeclaration = new EventAddRegion(attributes);
			eventAccessorDeclaration.Block = (BlockStatement)stmt;
			eventAccessorDeclaration.Parameters = p;
			
		} else if (la.kind == 152) {
			lexer.NextToken();
			if (la.kind == 24) {
				lexer.NextToken();
				if (StartOf(4)) {
					FormalParameterList(
#line  1253 "VBNET.ATG" 
p);
				}
				Expect(25);
			}
			Expect(1);
			Block(
#line  1254 "VBNET.ATG" 
out stmt);
			Expect(88);
			Expect(152);
			Expect(1);

#line  1256 "VBNET.ATG" 
			eventAccessorDeclaration = new EventRemoveRegion(attributes);
			eventAccessorDeclaration.Block = (BlockStatement)stmt;
			eventAccessorDeclaration.Parameters = p;
			
		} else if (la.kind == 149) {
			lexer.NextToken();
			if (la.kind == 24) {
				lexer.NextToken();
				if (StartOf(4)) {
					FormalParameterList(
#line  1261 "VBNET.ATG" 
p);
				}
				Expect(25);
			}
			Expect(1);
			Block(
#line  1262 "VBNET.ATG" 
out stmt);
			Expect(88);
			Expect(149);
			Expect(1);

#line  1264 "VBNET.ATG" 
			eventAccessorDeclaration = new EventRaiseRegion(attributes);
			eventAccessorDeclaration.Block = (BlockStatement)stmt;
			eventAccessorDeclaration.Parameters = p;
			
		} else SynErr(235);
	}

	void OverloadableOperator(
#line  1177 "VBNET.ATG" 
out OverloadableOperatorType operatorType) {

#line  1178 "VBNET.ATG" 
		operatorType = OverloadableOperatorType.None; 
		switch (la.kind) {
		case 14: {
			lexer.NextToken();

#line  1180 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.Add; 
			break;
		}
		case 15: {
			lexer.NextToken();

#line  1182 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.Subtract; 
			break;
		}
		case 16: {
			lexer.NextToken();

#line  1184 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.Multiply; 
			break;
		}
		case 17: {
			lexer.NextToken();

#line  1186 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.Divide; 
			break;
		}
		case 18: {
			lexer.NextToken();

#line  1188 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.DivideInteger; 
			break;
		}
		case 19: {
			lexer.NextToken();

#line  1190 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.Concat; 
			break;
		}
		case 116: {
			lexer.NextToken();

#line  1192 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.Like; 
			break;
		}
		case 120: {
			lexer.NextToken();

#line  1194 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.Modulus; 
			break;
		}
		case 45: {
			lexer.NextToken();

#line  1196 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.BitwiseAnd; 
			break;
		}
		case 138: {
			lexer.NextToken();

#line  1198 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.BitwiseOr; 
			break;
		}
		case 185: {
			lexer.NextToken();

#line  1200 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.ExclusiveOr; 
			break;
		}
		case 20: {
			lexer.NextToken();

#line  1202 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.Power; 
			break;
		}
		case 31: {
			lexer.NextToken();

#line  1204 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.ShiftLeft; 
			break;
		}
		case 32: {
			lexer.NextToken();

#line  1206 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.ShiftRight; 
			break;
		}
		case 11: {
			lexer.NextToken();

#line  1208 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.Equality; 
			break;
		}
		case 28: {
			lexer.NextToken();

#line  1210 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.InEquality; 
			break;
		}
		case 27: {
			lexer.NextToken();

#line  1212 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.LessThan; 
			break;
		}
		case 30: {
			lexer.NextToken();

#line  1214 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.LessThanOrEqual; 
			break;
		}
		case 26: {
			lexer.NextToken();

#line  1216 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.GreaterThan; 
			break;
		}
		case 29: {
			lexer.NextToken();

#line  1218 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.GreaterThanOrEqual; 
			break;
		}
		case 75: {
			lexer.NextToken();

#line  1220 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.CType; 
			break;
		}
		case 2: case 47: case 49: case 50: case 51: case 70: case 95: case 134: case 144: case 169: case 176: case 177: case 205: {
			Identifier();

#line  1224 "VBNET.ATG" 
			string opName = t.val; 
			if (string.Equals(opName, "istrue", StringComparison.InvariantCultureIgnoreCase)) {
				operatorType = OverloadableOperatorType.IsTrue;
			} else if (string.Equals(opName, "isfalse", StringComparison.InvariantCultureIgnoreCase)) {
				operatorType = OverloadableOperatorType.IsFalse;
			} else {
				Error("Invalid operator. Possible operators are '+', '-', 'Not', 'IsTrue', 'IsFalse'.");
			}
			
			break;
		}
		default: SynErr(236); break;
		}
	}

	void GetAccessorDecl(
#line  1298 "VBNET.ATG" 
out PropertyGetRegion getBlock, List<AttributeSection> attributes) {

#line  1299 "VBNET.ATG" 
		Statement stmt = null; Modifiers m; 
		PropertyAccessorAccessModifier(
#line  1301 "VBNET.ATG" 
out m);
		Expect(101);

#line  1303 "VBNET.ATG" 
		Location startLocation = t.Location; 
		Expect(1);
		Block(
#line  1305 "VBNET.ATG" 
out stmt);

#line  1306 "VBNET.ATG" 
		getBlock = new PropertyGetRegion((BlockStatement)stmt, attributes); 
		Expect(88);
		Expect(101);

#line  1308 "VBNET.ATG" 
		getBlock.Modifier = m; 

#line  1309 "VBNET.ATG" 
		getBlock.StartLocation = startLocation; getBlock.EndLocation = t.EndLocation; 
		Expect(1);
	}

	void SetAccessorDecl(
#line  1314 "VBNET.ATG" 
out PropertySetRegion setBlock, List<AttributeSection> attributes) {

#line  1316 "VBNET.ATG" 
		Statement stmt = null;
		List<ParameterDeclarationExpression> p = new List<ParameterDeclarationExpression>();
		Modifiers m;
		
		PropertyAccessorAccessModifier(
#line  1321 "VBNET.ATG" 
out m);
		Expect(156);

#line  1323 "VBNET.ATG" 
		Location startLocation = t.Location; 
		if (la.kind == 24) {
			lexer.NextToken();
			if (StartOf(4)) {
				FormalParameterList(
#line  1324 "VBNET.ATG" 
p);
			}
			Expect(25);
		}
		Expect(1);
		Block(
#line  1326 "VBNET.ATG" 
out stmt);

#line  1328 "VBNET.ATG" 
		setBlock = new PropertySetRegion((BlockStatement)stmt, attributes);
		setBlock.Modifier = m;
		setBlock.Parameters = p;
		
		Expect(88);
		Expect(156);

#line  1333 "VBNET.ATG" 
		setBlock.StartLocation = startLocation; setBlock.EndLocation = t.EndLocation; 
		Expect(1);
	}

	void PropertyAccessorAccessModifier(
#line  2907 "VBNET.ATG" 
out Modifiers m) {

#line  2908 "VBNET.ATG" 
		m = Modifiers.None; 
		while (StartOf(26)) {
			if (la.kind == 148) {
				lexer.NextToken();

#line  2910 "VBNET.ATG" 
				m |= Modifiers.Public; 
			} else if (la.kind == 147) {
				lexer.NextToken();

#line  2911 "VBNET.ATG" 
				m |= Modifiers.Protected; 
			} else if (la.kind == 99) {
				lexer.NextToken();

#line  2912 "VBNET.ATG" 
				m |= Modifiers.Internal; 
			} else {
				lexer.NextToken();

#line  2913 "VBNET.ATG" 
				m |= Modifiers.Private; 
			}
		}
	}

	void ArrayInitializationModifier(
#line  1431 "VBNET.ATG" 
out List<Expression> arrayModifiers) {

#line  1433 "VBNET.ATG" 
		arrayModifiers = null;
		
		Expect(24);
		InitializationRankList(
#line  1435 "VBNET.ATG" 
out arrayModifiers);
		Expect(25);
	}

	void ArrayNameModifier(
#line  2007 "VBNET.ATG" 
out ArrayList arrayModifiers) {

#line  2009 "VBNET.ATG" 
		arrayModifiers = null;
		
		ArrayTypeModifiers(
#line  2011 "VBNET.ATG" 
out arrayModifiers);
	}

	void ObjectCreateExpression(
#line  1860 "VBNET.ATG" 
out Expression oce) {

#line  1862 "VBNET.ATG" 
		TypeReference type = null;
		Expression initializer = null;
		List<Expression> arguments = null;
		ArrayList dimensions = null;
		oce = null;
		bool canBeNormal; bool canBeReDim;
		
		Expect(127);
		NonArrayTypeName(
#line  1869 "VBNET.ATG" 
out type, false);
		if (la.kind == 24) {
			lexer.NextToken();
			NormalOrReDimArgumentList(
#line  1870 "VBNET.ATG" 
out arguments, out canBeNormal, out canBeReDim);
			Expect(25);
			if (la.kind == 22 || 
#line  1871 "VBNET.ATG" 
la.kind == Tokens.OpenParenthesis) {
				if (
#line  1871 "VBNET.ATG" 
la.kind == Tokens.OpenParenthesis) {
					ArrayTypeModifiers(
#line  1872 "VBNET.ATG" 
out dimensions);
					CollectionInitializer(
#line  1873 "VBNET.ATG" 
out initializer);
				} else {
					CollectionInitializer(
#line  1874 "VBNET.ATG" 
out initializer);
				}
			}

#line  1876 "VBNET.ATG" 
			if (canBeReDim && !canBeNormal && initializer == null) initializer = new CollectionInitializerExpression(); 
		}

#line  1879 "VBNET.ATG" 
		if (type == null) type = new TypeReference("Object"); // fallback type on parser errors
		if (initializer == null) {
			oce = new ObjectCreateExpression(type, arguments);
		} else {
			if (dimensions == null) dimensions = new ArrayList();
			dimensions.Insert(0, (arguments == null) ? 0 : Math.Max(arguments.Count - 1, 0));
			type.RankSpecifier = (int[])dimensions.ToArray(typeof(int));
			ArrayCreateExpression ace = new ArrayCreateExpression(type, initializer as CollectionInitializerExpression);
			ace.Arguments = arguments;
			oce = ace;
		}
		
	}

	void VariableInitializer(
#line  1459 "VBNET.ATG" 
out Expression initializerExpression) {

#line  1461 "VBNET.ATG" 
		initializerExpression = null;
		
		if (StartOf(27)) {
			Expr(
#line  1463 "VBNET.ATG" 
out initializerExpression);
		} else if (la.kind == 22) {
			CollectionInitializer(
#line  1464 "VBNET.ATG" 
out initializerExpression);
		} else SynErr(237);
	}

	void InitializationRankList(
#line  1439 "VBNET.ATG" 
out List<Expression> rank) {

#line  1441 "VBNET.ATG" 
		rank = new List<Expression>();
		Expression expr = null;
		
		Expr(
#line  1444 "VBNET.ATG" 
out expr);
		if (la.kind == 172) {
			lexer.NextToken();

#line  1445 "VBNET.ATG" 
			EnsureIsZero(expr); 
			Expr(
#line  1446 "VBNET.ATG" 
out expr);
		}

#line  1448 "VBNET.ATG" 
		if (expr != null) { rank.Add(expr); } 
		while (la.kind == 12) {
			lexer.NextToken();
			Expr(
#line  1450 "VBNET.ATG" 
out expr);
			if (la.kind == 172) {
				lexer.NextToken();

#line  1451 "VBNET.ATG" 
				EnsureIsZero(expr); 
				Expr(
#line  1452 "VBNET.ATG" 
out expr);
			}

#line  1454 "VBNET.ATG" 
			if (expr != null) { rank.Add(expr); } 
		}
	}

	void CollectionInitializer(
#line  1468 "VBNET.ATG" 
out Expression outExpr) {

#line  1470 "VBNET.ATG" 
		Expression expr = null;
		CollectionInitializerExpression initializer = new CollectionInitializerExpression();
		
		Expect(22);
		if (StartOf(28)) {
			VariableInitializer(
#line  1475 "VBNET.ATG" 
out expr);

#line  1477 "VBNET.ATG" 
			if (expr != null) { initializer.CreateExpressions.Add(expr); }
			
			while (
#line  1480 "VBNET.ATG" 
NotFinalComma()) {
				Expect(12);
				VariableInitializer(
#line  1480 "VBNET.ATG" 
out expr);

#line  1481 "VBNET.ATG" 
				if (expr != null) { initializer.CreateExpressions.Add(expr); } 
			}
		}
		Expect(23);

#line  1484 "VBNET.ATG" 
		outExpr = initializer; 
	}

	void EventMemberSpecifier(
#line  1554 "VBNET.ATG" 
out string name) {

#line  1555 "VBNET.ATG" 
		string eventName; 
		if (StartOf(13)) {
			Identifier();
		} else if (la.kind == 124) {
			lexer.NextToken();
		} else if (la.kind == 119) {
			lexer.NextToken();
		} else SynErr(238);

#line  1558 "VBNET.ATG" 
		name = t.val; 
		Expect(10);
		IdentifierOrKeyword(
#line  1560 "VBNET.ATG" 
out eventName);

#line  1561 "VBNET.ATG" 
		name = name + "." + eventName; 
	}

	void IdentifierOrKeyword(
#line  2841 "VBNET.ATG" 
out string name) {

#line  2843 "VBNET.ATG" 
		lexer.NextToken(); name = t.val;  
	}

	void DisjunctionExpr(
#line  1704 "VBNET.ATG" 
out Expression outExpr) {

#line  1706 "VBNET.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		ConjunctionExpr(
#line  1709 "VBNET.ATG" 
out outExpr);
		while (la.kind == 138 || la.kind == 139 || la.kind == 185) {
			if (la.kind == 138) {
				lexer.NextToken();

#line  1712 "VBNET.ATG" 
				op = BinaryOperatorType.BitwiseOr; 
			} else if (la.kind == 139) {
				lexer.NextToken();

#line  1713 "VBNET.ATG" 
				op = BinaryOperatorType.LogicalOr; 
			} else {
				lexer.NextToken();

#line  1714 "VBNET.ATG" 
				op = BinaryOperatorType.ExclusiveOr; 
			}
			ConjunctionExpr(
#line  1716 "VBNET.ATG" 
out expr);

#line  1716 "VBNET.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr);  
		}
	}

	void AssignmentOperator(
#line  1569 "VBNET.ATG" 
out AssignmentOperatorType op) {

#line  1570 "VBNET.ATG" 
		op = AssignmentOperatorType.None; 
		switch (la.kind) {
		case 11: {
			lexer.NextToken();

#line  1571 "VBNET.ATG" 
			op = AssignmentOperatorType.Assign; 
			break;
		}
		case 41: {
			lexer.NextToken();

#line  1572 "VBNET.ATG" 
			op = AssignmentOperatorType.ConcatString; 
			break;
		}
		case 33: {
			lexer.NextToken();

#line  1573 "VBNET.ATG" 
			op = AssignmentOperatorType.Add; 
			break;
		}
		case 35: {
			lexer.NextToken();

#line  1574 "VBNET.ATG" 
			op = AssignmentOperatorType.Subtract; 
			break;
		}
		case 36: {
			lexer.NextToken();

#line  1575 "VBNET.ATG" 
			op = AssignmentOperatorType.Multiply; 
			break;
		}
		case 37: {
			lexer.NextToken();

#line  1576 "VBNET.ATG" 
			op = AssignmentOperatorType.Divide; 
			break;
		}
		case 38: {
			lexer.NextToken();

#line  1577 "VBNET.ATG" 
			op = AssignmentOperatorType.DivideInteger; 
			break;
		}
		case 34: {
			lexer.NextToken();

#line  1578 "VBNET.ATG" 
			op = AssignmentOperatorType.Power; 
			break;
		}
		case 39: {
			lexer.NextToken();

#line  1579 "VBNET.ATG" 
			op = AssignmentOperatorType.ShiftLeft; 
			break;
		}
		case 40: {
			lexer.NextToken();

#line  1580 "VBNET.ATG" 
			op = AssignmentOperatorType.ShiftRight; 
			break;
		}
		default: SynErr(239); break;
		}
	}

	void SimpleExpr(
#line  1584 "VBNET.ATG" 
out Expression pexpr) {
		SimpleNonInvocationExpression(
#line  1586 "VBNET.ATG" 
out pexpr);
		while (la.kind == 10 || la.kind == 24) {
			if (la.kind == 10) {

#line  1587 "VBNET.ATG" 
				string name; 
				lexer.NextToken();
				IdentifierOrKeyword(
#line  1588 "VBNET.ATG" 
out name);

#line  1588 "VBNET.ATG" 
				pexpr = new MemberReferenceExpression(pexpr, name); 
			} else {
				InvocationExpression(
#line  1589 "VBNET.ATG" 
ref pexpr);
			}
		}
	}

	void SimpleNonInvocationExpression(
#line  1593 "VBNET.ATG" 
out Expression pexpr) {

#line  1595 "VBNET.ATG" 
		Expression expr;
		TypeReference type = null;
		string name = String.Empty;
		pexpr = null;
		
		if (StartOf(29)) {
			switch (la.kind) {
			case 3: {
				lexer.NextToken();

#line  1603 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(t.literalValue, t.val);  
				break;
			}
			case 4: {
				lexer.NextToken();

#line  1604 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(t.literalValue, t.val);  
				break;
			}
			case 7: {
				lexer.NextToken();

#line  1605 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(t.literalValue, t.val);  
				break;
			}
			case 6: {
				lexer.NextToken();

#line  1606 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(t.literalValue, t.val);  
				break;
			}
			case 5: {
				lexer.NextToken();

#line  1607 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(t.literalValue, t.val);  
				break;
			}
			case 9: {
				lexer.NextToken();

#line  1608 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(t.literalValue, t.val);  
				break;
			}
			case 8: {
				lexer.NextToken();

#line  1609 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(t.literalValue, t.val);  
				break;
			}
			case 173: {
				lexer.NextToken();

#line  1611 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(true, "true");  
				break;
			}
			case 96: {
				lexer.NextToken();

#line  1612 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(false, "false"); 
				break;
			}
			case 130: {
				lexer.NextToken();

#line  1613 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(null, "null");  
				break;
			}
			case 24: {
				lexer.NextToken();
				Expr(
#line  1614 "VBNET.ATG" 
out expr);
				Expect(25);

#line  1614 "VBNET.ATG" 
				pexpr = new ParenthesizedExpression(expr); 
				break;
			}
			case 2: case 47: case 49: case 50: case 51: case 70: case 95: case 134: case 144: case 169: case 176: case 177: case 205: {
				Identifier();

#line  1616 "VBNET.ATG" 
				pexpr = new IdentifierExpression(t.val); 

#line  1617 "VBNET.ATG" 
				pexpr.StartLocation = t.Location; pexpr.EndLocation = t.EndLocation; 
				break;
			}
			case 52: case 54: case 65: case 76: case 77: case 84: case 111: case 117: case 133: case 159: case 160: case 165: case 191: case 192: case 193: case 194: {

#line  1618 "VBNET.ATG" 
				string val = String.Empty; 
				if (StartOf(10)) {
					PrimitiveTypeName(
#line  1619 "VBNET.ATG" 
out val);
				} else if (la.kind == 133) {
					lexer.NextToken();

#line  1619 "VBNET.ATG" 
					val = "Object"; 
				} else SynErr(240);
				Expect(10);

#line  1620 "VBNET.ATG" 
				t.val = ""; 
				Identifier();

#line  1620 "VBNET.ATG" 
				pexpr = new MemberReferenceExpression(new TypeReferenceExpression(val), t.val); 
				break;
			}
			case 119: {
				lexer.NextToken();

#line  1621 "VBNET.ATG" 
				pexpr = new ThisReferenceExpression(); 
				break;
			}
			case 124: case 125: {

#line  1622 "VBNET.ATG" 
				Expression retExpr = null; 
				if (la.kind == 124) {
					lexer.NextToken();

#line  1623 "VBNET.ATG" 
					retExpr = new BaseReferenceExpression(); 
				} else if (la.kind == 125) {
					lexer.NextToken();

#line  1624 "VBNET.ATG" 
					retExpr = new ClassReferenceExpression(); 
				} else SynErr(241);
				Expect(10);
				IdentifierOrKeyword(
#line  1626 "VBNET.ATG" 
out name);

#line  1626 "VBNET.ATG" 
				pexpr = new MemberReferenceExpression(retExpr, name); 
				break;
			}
			case 199: {
				lexer.NextToken();
				Expect(10);
				Identifier();

#line  1628 "VBNET.ATG" 
				type = new TypeReference(t.val ?? ""); 

#line  1630 "VBNET.ATG" 
				type.IsGlobal = true; 

#line  1631 "VBNET.ATG" 
				pexpr = new TypeReferenceExpression(type); 
				break;
			}
			case 127: {
				ObjectCreateExpression(
#line  1632 "VBNET.ATG" 
out expr);

#line  1632 "VBNET.ATG" 
				pexpr = expr; 
				break;
			}
			case 75: case 82: case 200: {

#line  1634 "VBNET.ATG" 
				CastType castType = CastType.Cast; 
				if (la.kind == 82) {
					lexer.NextToken();
				} else if (la.kind == 75) {
					lexer.NextToken();

#line  1636 "VBNET.ATG" 
					castType = CastType.Conversion; 
				} else if (la.kind == 200) {
					lexer.NextToken();

#line  1637 "VBNET.ATG" 
					castType = CastType.TryCast; 
				} else SynErr(242);
				Expect(24);
				Expr(
#line  1639 "VBNET.ATG" 
out expr);
				Expect(12);
				TypeName(
#line  1639 "VBNET.ATG" 
out type);
				Expect(25);

#line  1640 "VBNET.ATG" 
				pexpr = new CastExpression(type, expr, castType); 
				break;
			}
			case 59: case 60: case 61: case 62: case 63: case 64: case 66: case 68: case 69: case 72: case 73: case 74: case 195: case 196: case 197: case 198: {
				CastTarget(
#line  1641 "VBNET.ATG" 
out type);
				Expect(24);
				Expr(
#line  1641 "VBNET.ATG" 
out expr);
				Expect(25);

#line  1641 "VBNET.ATG" 
				pexpr = new CastExpression(type, expr, CastType.PrimitiveConversion); 
				break;
			}
			case 43: {
				lexer.NextToken();
				Expr(
#line  1642 "VBNET.ATG" 
out expr);

#line  1642 "VBNET.ATG" 
				pexpr = new AddressOfExpression(expr); 
				break;
			}
			case 102: {
				lexer.NextToken();
				Expect(24);
				GetTypeTypeName(
#line  1643 "VBNET.ATG" 
out type);
				Expect(25);

#line  1643 "VBNET.ATG" 
				pexpr = new TypeOfExpression(type); 
				break;
			}
			case 175: {
				lexer.NextToken();
				SimpleExpr(
#line  1644 "VBNET.ATG" 
out expr);
				Expect(113);
				TypeName(
#line  1644 "VBNET.ATG" 
out type);

#line  1644 "VBNET.ATG" 
				pexpr = new TypeOfIsExpression(expr, type); 
				break;
			}
			}
		} else if (la.kind == 10) {
			lexer.NextToken();
			IdentifierOrKeyword(
#line  1648 "VBNET.ATG" 
out name);

#line  1648 "VBNET.ATG" 
			pexpr = new MemberReferenceExpression(null, name);
		} else SynErr(243);
	}

	void InvocationExpression(
#line  1652 "VBNET.ATG" 
ref Expression pexpr) {

#line  1653 "VBNET.ATG" 
		List<TypeReference> typeParameters = new List<TypeReference>();
		List<Expression> parameters = null;
		TypeReference type; 
		Expect(24);

#line  1657 "VBNET.ATG" 
		Location start = t.Location; 
		if (la.kind == 201) {
			lexer.NextToken();
			TypeName(
#line  1659 "VBNET.ATG" 
out type);

#line  1659 "VBNET.ATG" 
			if (type != null) typeParameters.Add(type); 
			while (la.kind == 12) {
				lexer.NextToken();
				TypeName(
#line  1662 "VBNET.ATG" 
out type);

#line  1662 "VBNET.ATG" 
				if (type != null) typeParameters.Add(type); 
			}
			Expect(25);
			if (la.kind == 10) {
				lexer.NextToken();
				Identifier();

#line  1667 "VBNET.ATG" 
				pexpr = new MemberReferenceExpression(GetTypeReferenceExpression(pexpr, typeParameters), t.val); 
			} else if (la.kind == 24) {
				lexer.NextToken();
				ArgumentList(
#line  1669 "VBNET.ATG" 
out parameters);
				Expect(25);

#line  1671 "VBNET.ATG" 
				pexpr = CreateInvocationExpression(pexpr, parameters, typeParameters); 
			} else SynErr(244);
		} else if (StartOf(30)) {
			ArgumentList(
#line  1673 "VBNET.ATG" 
out parameters);
			Expect(25);

#line  1675 "VBNET.ATG" 
			pexpr = CreateInvocationExpression(pexpr, parameters, typeParameters); 
		} else SynErr(245);

#line  1677 "VBNET.ATG" 
		pexpr.StartLocation = start; pexpr.EndLocation = t.Location; 
	}

	void PrimitiveTypeName(
#line  2848 "VBNET.ATG" 
out string type) {

#line  2849 "VBNET.ATG" 
		type = String.Empty; 
		switch (la.kind) {
		case 52: {
			lexer.NextToken();

#line  2850 "VBNET.ATG" 
			type = "Boolean"; 
			break;
		}
		case 76: {
			lexer.NextToken();

#line  2851 "VBNET.ATG" 
			type = "Date"; 
			break;
		}
		case 65: {
			lexer.NextToken();

#line  2852 "VBNET.ATG" 
			type = "Char"; 
			break;
		}
		case 165: {
			lexer.NextToken();

#line  2853 "VBNET.ATG" 
			type = "String"; 
			break;
		}
		case 77: {
			lexer.NextToken();

#line  2854 "VBNET.ATG" 
			type = "Decimal"; 
			break;
		}
		case 54: {
			lexer.NextToken();

#line  2855 "VBNET.ATG" 
			type = "Byte"; 
			break;
		}
		case 159: {
			lexer.NextToken();

#line  2856 "VBNET.ATG" 
			type = "Short"; 
			break;
		}
		case 111: {
			lexer.NextToken();

#line  2857 "VBNET.ATG" 
			type = "Integer"; 
			break;
		}
		case 117: {
			lexer.NextToken();

#line  2858 "VBNET.ATG" 
			type = "Long"; 
			break;
		}
		case 160: {
			lexer.NextToken();

#line  2859 "VBNET.ATG" 
			type = "Single"; 
			break;
		}
		case 84: {
			lexer.NextToken();

#line  2860 "VBNET.ATG" 
			type = "Double"; 
			break;
		}
		case 192: {
			lexer.NextToken();

#line  2861 "VBNET.ATG" 
			type = "UInteger"; 
			break;
		}
		case 193: {
			lexer.NextToken();

#line  2862 "VBNET.ATG" 
			type = "ULong"; 
			break;
		}
		case 194: {
			lexer.NextToken();

#line  2863 "VBNET.ATG" 
			type = "UShort"; 
			break;
		}
		case 191: {
			lexer.NextToken();

#line  2864 "VBNET.ATG" 
			type = "SByte"; 
			break;
		}
		default: SynErr(246); break;
		}
	}

	void CastTarget(
#line  1682 "VBNET.ATG" 
out TypeReference type) {

#line  1684 "VBNET.ATG" 
		type = null;
		
		switch (la.kind) {
		case 59: {
			lexer.NextToken();

#line  1686 "VBNET.ATG" 
			type = new TypeReference("System.Boolean"); 
			break;
		}
		case 60: {
			lexer.NextToken();

#line  1687 "VBNET.ATG" 
			type = new TypeReference("System.Byte"); 
			break;
		}
		case 195: {
			lexer.NextToken();

#line  1688 "VBNET.ATG" 
			type = new TypeReference("System.SByte"); 
			break;
		}
		case 61: {
			lexer.NextToken();

#line  1689 "VBNET.ATG" 
			type = new TypeReference("System.Char"); 
			break;
		}
		case 62: {
			lexer.NextToken();

#line  1690 "VBNET.ATG" 
			type = new TypeReference("System.DateTime"); 
			break;
		}
		case 64: {
			lexer.NextToken();

#line  1691 "VBNET.ATG" 
			type = new TypeReference("System.Decimal"); 
			break;
		}
		case 63: {
			lexer.NextToken();

#line  1692 "VBNET.ATG" 
			type = new TypeReference("System.Double"); 
			break;
		}
		case 72: {
			lexer.NextToken();

#line  1693 "VBNET.ATG" 
			type = new TypeReference("System.Int16"); 
			break;
		}
		case 66: {
			lexer.NextToken();

#line  1694 "VBNET.ATG" 
			type = new TypeReference("System.Int32"); 
			break;
		}
		case 68: {
			lexer.NextToken();

#line  1695 "VBNET.ATG" 
			type = new TypeReference("System.Int64"); 
			break;
		}
		case 196: {
			lexer.NextToken();

#line  1696 "VBNET.ATG" 
			type = new TypeReference("System.UInt16"); 
			break;
		}
		case 197: {
			lexer.NextToken();

#line  1697 "VBNET.ATG" 
			type = new TypeReference("System.UInt32"); 
			break;
		}
		case 198: {
			lexer.NextToken();

#line  1698 "VBNET.ATG" 
			type = new TypeReference("System.UInt64"); 
			break;
		}
		case 69: {
			lexer.NextToken();

#line  1699 "VBNET.ATG" 
			type = new TypeReference("System.Object"); 
			break;
		}
		case 73: {
			lexer.NextToken();

#line  1700 "VBNET.ATG" 
			type = new TypeReference("System.Single"); 
			break;
		}
		case 74: {
			lexer.NextToken();

#line  1701 "VBNET.ATG" 
			type = new TypeReference("System.String"); 
			break;
		}
		default: SynErr(247); break;
		}
	}

	void GetTypeTypeName(
#line  1959 "VBNET.ATG" 
out TypeReference typeref) {

#line  1960 "VBNET.ATG" 
		ArrayList rank = null; 
		NonArrayTypeName(
#line  1962 "VBNET.ATG" 
out typeref, true);
		ArrayTypeModifiers(
#line  1963 "VBNET.ATG" 
out rank);

#line  1964 "VBNET.ATG" 
		if (rank != null && typeref != null) {
		typeref.RankSpecifier = (int[])rank.ToArray(typeof(int));
		}
		
	}

	void ArgumentList(
#line  1894 "VBNET.ATG" 
out List<Expression> arguments) {

#line  1896 "VBNET.ATG" 
		arguments = new List<Expression>();
		Expression expr = null;
		
		if (StartOf(27)) {
			Argument(
#line  1899 "VBNET.ATG" 
out expr);
		}
		while (la.kind == 12) {
			lexer.NextToken();

#line  1900 "VBNET.ATG" 
			arguments.Add(expr ?? Expression.Null); expr = null; 
			if (StartOf(27)) {
				Argument(
#line  1901 "VBNET.ATG" 
out expr);
			}

#line  1902 "VBNET.ATG" 
			if (expr == null) expr = Expression.Null; 
		}

#line  1904 "VBNET.ATG" 
		if (expr != null) arguments.Add(expr); 
	}

	void ConjunctionExpr(
#line  1720 "VBNET.ATG" 
out Expression outExpr) {

#line  1722 "VBNET.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		NotExpr(
#line  1725 "VBNET.ATG" 
out outExpr);
		while (la.kind == 45 || la.kind == 46) {
			if (la.kind == 45) {
				lexer.NextToken();

#line  1728 "VBNET.ATG" 
				op = BinaryOperatorType.BitwiseAnd; 
			} else {
				lexer.NextToken();

#line  1729 "VBNET.ATG" 
				op = BinaryOperatorType.LogicalAnd; 
			}
			NotExpr(
#line  1731 "VBNET.ATG" 
out expr);

#line  1731 "VBNET.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr);  
		}
	}

	void NotExpr(
#line  1735 "VBNET.ATG" 
out Expression outExpr) {

#line  1736 "VBNET.ATG" 
		UnaryOperatorType uop = UnaryOperatorType.None; 
		while (la.kind == 129) {
			lexer.NextToken();

#line  1737 "VBNET.ATG" 
			uop = UnaryOperatorType.Not; 
		}
		ComparisonExpr(
#line  1738 "VBNET.ATG" 
out outExpr);

#line  1739 "VBNET.ATG" 
		if (uop != UnaryOperatorType.None)
		outExpr = new UnaryOperatorExpression(outExpr, uop);
		
	}

	void ComparisonExpr(
#line  1744 "VBNET.ATG" 
out Expression outExpr) {

#line  1746 "VBNET.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		ShiftExpr(
#line  1749 "VBNET.ATG" 
out outExpr);
		while (StartOf(31)) {
			switch (la.kind) {
			case 27: {
				lexer.NextToken();

#line  1752 "VBNET.ATG" 
				op = BinaryOperatorType.LessThan; 
				break;
			}
			case 26: {
				lexer.NextToken();

#line  1753 "VBNET.ATG" 
				op = BinaryOperatorType.GreaterThan; 
				break;
			}
			case 30: {
				lexer.NextToken();

#line  1754 "VBNET.ATG" 
				op = BinaryOperatorType.LessThanOrEqual; 
				break;
			}
			case 29: {
				lexer.NextToken();

#line  1755 "VBNET.ATG" 
				op = BinaryOperatorType.GreaterThanOrEqual; 
				break;
			}
			case 28: {
				lexer.NextToken();

#line  1756 "VBNET.ATG" 
				op = BinaryOperatorType.InEquality; 
				break;
			}
			case 11: {
				lexer.NextToken();

#line  1757 "VBNET.ATG" 
				op = BinaryOperatorType.Equality; 
				break;
			}
			case 116: {
				lexer.NextToken();

#line  1758 "VBNET.ATG" 
				op = BinaryOperatorType.Like; 
				break;
			}
			case 113: {
				lexer.NextToken();

#line  1759 "VBNET.ATG" 
				op = BinaryOperatorType.ReferenceEquality; 
				break;
			}
			case 190: {
				lexer.NextToken();

#line  1760 "VBNET.ATG" 
				op = BinaryOperatorType.ReferenceInequality; 
				break;
			}
			}
			if (StartOf(32)) {
				ShiftExpr(
#line  1763 "VBNET.ATG" 
out expr);

#line  1763 "VBNET.ATG" 
				outExpr = new BinaryOperatorExpression(outExpr, op, expr);  
			} else if (la.kind == 129) {
				lexer.NextToken();
				ShiftExpr(
#line  1766 "VBNET.ATG" 
out expr);

#line  1766 "VBNET.ATG" 
				outExpr = new BinaryOperatorExpression(outExpr, op, new UnaryOperatorExpression(expr, UnaryOperatorType.Not));  
			} else SynErr(248);
		}
	}

	void ShiftExpr(
#line  1771 "VBNET.ATG" 
out Expression outExpr) {

#line  1773 "VBNET.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		ConcatenationExpr(
#line  1776 "VBNET.ATG" 
out outExpr);
		while (la.kind == 31 || la.kind == 32) {
			if (la.kind == 31) {
				lexer.NextToken();

#line  1779 "VBNET.ATG" 
				op = BinaryOperatorType.ShiftLeft; 
			} else {
				lexer.NextToken();

#line  1780 "VBNET.ATG" 
				op = BinaryOperatorType.ShiftRight; 
			}
			ConcatenationExpr(
#line  1782 "VBNET.ATG" 
out expr);

#line  1782 "VBNET.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr);  
		}
	}

	void ConcatenationExpr(
#line  1786 "VBNET.ATG" 
out Expression outExpr) {

#line  1787 "VBNET.ATG" 
		Expression expr; 
		AdditiveExpr(
#line  1789 "VBNET.ATG" 
out outExpr);
		while (la.kind == 19) {
			lexer.NextToken();
			AdditiveExpr(
#line  1789 "VBNET.ATG" 
out expr);

#line  1789 "VBNET.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.Concat, expr);  
		}
	}

	void AdditiveExpr(
#line  1792 "VBNET.ATG" 
out Expression outExpr) {

#line  1794 "VBNET.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		ModuloExpr(
#line  1797 "VBNET.ATG" 
out outExpr);
		while (la.kind == 14 || la.kind == 15) {
			if (la.kind == 14) {
				lexer.NextToken();

#line  1800 "VBNET.ATG" 
				op = BinaryOperatorType.Add; 
			} else {
				lexer.NextToken();

#line  1801 "VBNET.ATG" 
				op = BinaryOperatorType.Subtract; 
			}
			ModuloExpr(
#line  1803 "VBNET.ATG" 
out expr);

#line  1803 "VBNET.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr);  
		}
	}

	void ModuloExpr(
#line  1807 "VBNET.ATG" 
out Expression outExpr) {

#line  1808 "VBNET.ATG" 
		Expression expr; 
		IntegerDivisionExpr(
#line  1810 "VBNET.ATG" 
out outExpr);
		while (la.kind == 120) {
			lexer.NextToken();
			IntegerDivisionExpr(
#line  1810 "VBNET.ATG" 
out expr);

#line  1810 "VBNET.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.Modulus, expr);  
		}
	}

	void IntegerDivisionExpr(
#line  1813 "VBNET.ATG" 
out Expression outExpr) {

#line  1814 "VBNET.ATG" 
		Expression expr; 
		MultiplicativeExpr(
#line  1816 "VBNET.ATG" 
out outExpr);
		while (la.kind == 18) {
			lexer.NextToken();
			MultiplicativeExpr(
#line  1816 "VBNET.ATG" 
out expr);

#line  1816 "VBNET.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.DivideInteger, expr);  
		}
	}

	void MultiplicativeExpr(
#line  1819 "VBNET.ATG" 
out Expression outExpr) {

#line  1821 "VBNET.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		UnaryExpr(
#line  1824 "VBNET.ATG" 
out outExpr);
		while (la.kind == 16 || la.kind == 17) {
			if (la.kind == 16) {
				lexer.NextToken();

#line  1827 "VBNET.ATG" 
				op = BinaryOperatorType.Multiply; 
			} else {
				lexer.NextToken();

#line  1828 "VBNET.ATG" 
				op = BinaryOperatorType.Divide; 
			}
			UnaryExpr(
#line  1830 "VBNET.ATG" 
out expr);

#line  1830 "VBNET.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr); 
		}
	}

	void UnaryExpr(
#line  1834 "VBNET.ATG" 
out Expression uExpr) {

#line  1836 "VBNET.ATG" 
		Expression expr;
		UnaryOperatorType uop = UnaryOperatorType.None;
		bool isUOp = false;
		
		while (la.kind == 14 || la.kind == 15 || la.kind == 16) {
			if (la.kind == 14) {
				lexer.NextToken();

#line  1840 "VBNET.ATG" 
				uop = UnaryOperatorType.Plus; isUOp = true; 
			} else if (la.kind == 15) {
				lexer.NextToken();

#line  1841 "VBNET.ATG" 
				uop = UnaryOperatorType.Minus; isUOp = true; 
			} else {
				lexer.NextToken();

#line  1842 "VBNET.ATG" 
				uop = UnaryOperatorType.Star;  isUOp = true;
			}
		}
		ExponentiationExpr(
#line  1844 "VBNET.ATG" 
out expr);

#line  1846 "VBNET.ATG" 
		if (isUOp) {
		uExpr = new UnaryOperatorExpression(expr, uop);
		} else {
			uExpr = expr;
		}
		
	}

	void ExponentiationExpr(
#line  1854 "VBNET.ATG" 
out Expression outExpr) {

#line  1855 "VBNET.ATG" 
		Expression expr; 
		SimpleExpr(
#line  1857 "VBNET.ATG" 
out outExpr);
		while (la.kind == 20) {
			lexer.NextToken();
			SimpleExpr(
#line  1857 "VBNET.ATG" 
out expr);

#line  1857 "VBNET.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.Power, expr);  
		}
	}

	void NormalOrReDimArgumentList(
#line  1908 "VBNET.ATG" 
out List<Expression> arguments, out bool canBeNormal, out bool canBeRedim) {

#line  1910 "VBNET.ATG" 
		arguments = new List<Expression>();
		canBeNormal = true; canBeRedim = !IsNamedAssign();
		Expression expr = null;
		
		if (StartOf(27)) {
			Argument(
#line  1915 "VBNET.ATG" 
out expr);
			if (la.kind == 172) {
				lexer.NextToken();

#line  1916 "VBNET.ATG" 
				EnsureIsZero(expr); canBeNormal = false; 
				Expr(
#line  1917 "VBNET.ATG" 
out expr);
			}
		}
		while (la.kind == 12) {
			lexer.NextToken();

#line  1920 "VBNET.ATG" 
			if (expr == null) canBeRedim = false; 

#line  1921 "VBNET.ATG" 
			arguments.Add(expr ?? Expression.Null); expr = null; 

#line  1922 "VBNET.ATG" 
			canBeRedim &= !IsNamedAssign(); 
			if (StartOf(27)) {
				Argument(
#line  1923 "VBNET.ATG" 
out expr);
				if (la.kind == 172) {
					lexer.NextToken();

#line  1924 "VBNET.ATG" 
					EnsureIsZero(expr); canBeNormal = false; 
					Expr(
#line  1925 "VBNET.ATG" 
out expr);
				}
			}

#line  1927 "VBNET.ATG" 
			if (expr == null) { canBeRedim = false; expr = Expression.Null; } 
		}

#line  1929 "VBNET.ATG" 
		if (expr != null) arguments.Add(expr); else canBeRedim = false; 
	}

	void ArrayTypeModifiers(
#line  2016 "VBNET.ATG" 
out ArrayList arrayModifiers) {

#line  2018 "VBNET.ATG" 
		arrayModifiers = new ArrayList();
		int i = 0;
		
		while (
#line  2021 "VBNET.ATG" 
IsDims()) {
			Expect(24);
			if (la.kind == 12 || la.kind == 25) {
				RankList(
#line  2023 "VBNET.ATG" 
out i);
			}

#line  2025 "VBNET.ATG" 
			arrayModifiers.Add(i);
			
			Expect(25);
		}

#line  2030 "VBNET.ATG" 
		if(arrayModifiers.Count == 0) {
		 arrayModifiers = null;
		}
		
	}

	void Argument(
#line  1933 "VBNET.ATG" 
out Expression argumentexpr) {

#line  1935 "VBNET.ATG" 
		Expression expr;
		argumentexpr = null;
		string name;
		
		if (
#line  1939 "VBNET.ATG" 
IsNamedAssign()) {
			Identifier();

#line  1939 "VBNET.ATG" 
			name = t.val;  
			Expect(13);
			Expect(11);
			Expr(
#line  1939 "VBNET.ATG" 
out expr);

#line  1941 "VBNET.ATG" 
			argumentexpr = new NamedArgumentExpression(name, expr);
			
		} else if (StartOf(27)) {
			Expr(
#line  1944 "VBNET.ATG" 
out argumentexpr);
		} else SynErr(249);
	}

	void QualIdentAndTypeArguments(
#line  1990 "VBNET.ATG" 
out TypeReference typeref, bool canBeUnbound) {

#line  1991 "VBNET.ATG" 
		string name; typeref = null; 
		Qualident(
#line  1993 "VBNET.ATG" 
out name);

#line  1994 "VBNET.ATG" 
		typeref = new TypeReference(name); 
		if (
#line  1995 "VBNET.ATG" 
la.kind == Tokens.OpenParenthesis && Peek(1).kind == Tokens.Of) {
			lexer.NextToken();
			Expect(201);
			if (
#line  1997 "VBNET.ATG" 
canBeUnbound && (la.kind == Tokens.CloseParenthesis || la.kind == Tokens.Comma)) {

#line  1998 "VBNET.ATG" 
				typeref.GenericTypes.Add(NullTypeReference.Instance); 
				while (la.kind == 12) {
					lexer.NextToken();

#line  1999 "VBNET.ATG" 
					typeref.GenericTypes.Add(NullTypeReference.Instance); 
				}
			} else if (StartOf(6)) {
				TypeArgumentList(
#line  2000 "VBNET.ATG" 
typeref.GenericTypes);
			} else SynErr(250);
			Expect(25);
		}
	}

	void TypeArgumentList(
#line  2043 "VBNET.ATG" 
List<TypeReference> typeArguments) {

#line  2045 "VBNET.ATG" 
		TypeReference typeref;
		
		TypeName(
#line  2047 "VBNET.ATG" 
out typeref);

#line  2047 "VBNET.ATG" 
		if (typeref != null) typeArguments.Add(typeref); 
		while (la.kind == 12) {
			lexer.NextToken();
			TypeName(
#line  2050 "VBNET.ATG" 
out typeref);

#line  2050 "VBNET.ATG" 
			if (typeref != null) typeArguments.Add(typeref); 
		}
	}

	void RankList(
#line  2037 "VBNET.ATG" 
out int i) {

#line  2038 "VBNET.ATG" 
		i = 0; 
		while (la.kind == 12) {
			lexer.NextToken();

#line  2039 "VBNET.ATG" 
			++i; 
		}
	}

	void Attribute(
#line  2078 "VBNET.ATG" 
out ASTAttribute attribute) {

#line  2079 "VBNET.ATG" 
		string name;
		List<Expression> positional = new List<Expression>();
		List<NamedArgumentExpression> named = new List<NamedArgumentExpression>();
		
		if (la.kind == 199) {
			lexer.NextToken();
			Expect(10);
		}
		Qualident(
#line  2084 "VBNET.ATG" 
out name);
		if (la.kind == 24) {
			AttributeArguments(
#line  2085 "VBNET.ATG" 
positional, named);
		}

#line  2086 "VBNET.ATG" 
		attribute  = new ASTAttribute(name, positional, named); 
	}

	void AttributeArguments(
#line  2090 "VBNET.ATG" 
List<Expression> positional, List<NamedArgumentExpression> named) {

#line  2092 "VBNET.ATG" 
		bool nameFound = false;
		string name = "";
		Expression expr;
		
		Expect(24);
		if (
#line  2098 "VBNET.ATG" 
IsNotClosingParenthesis()) {
			if (
#line  2100 "VBNET.ATG" 
IsNamedAssign()) {

#line  2100 "VBNET.ATG" 
				nameFound = true; 
				IdentifierOrKeyword(
#line  2101 "VBNET.ATG" 
out name);
				if (la.kind == 13) {
					lexer.NextToken();
				}
				Expect(11);
			}
			Expr(
#line  2103 "VBNET.ATG" 
out expr);

#line  2105 "VBNET.ATG" 
			if (expr != null) {
			if (string.IsNullOrEmpty(name)) { positional.Add(expr); }
			else { named.Add(new NamedArgumentExpression(name, expr)); name = ""; }
			}
			
			while (la.kind == 12) {
				lexer.NextToken();
				if (
#line  2113 "VBNET.ATG" 
IsNamedAssign()) {

#line  2113 "VBNET.ATG" 
					nameFound = true; 
					IdentifierOrKeyword(
#line  2114 "VBNET.ATG" 
out name);
					if (la.kind == 13) {
						lexer.NextToken();
					}
					Expect(11);
				} else if (StartOf(27)) {

#line  2116 "VBNET.ATG" 
					if (nameFound) Error("no positional argument after named argument"); 
				} else SynErr(251);
				Expr(
#line  2117 "VBNET.ATG" 
out expr);

#line  2117 "VBNET.ATG" 
				if (expr != null) { if(name == "") positional.Add(expr);
				else { named.Add(new NamedArgumentExpression(name, expr)); name = ""; }
				}
				
			}
		}
		Expect(25);
	}

	void FormalParameter(
#line  2189 "VBNET.ATG" 
out ParameterDeclarationExpression p) {

#line  2191 "VBNET.ATG" 
		TypeReference type = null;
		ParamModifierList mod = new ParamModifierList(this);
		Expression expr = null;
		p = null;ArrayList arrayModifiers = null;
		
		while (StartOf(33)) {
			ParameterModifier(
#line  2196 "VBNET.ATG" 
mod);
		}
		Identifier();

#line  2197 "VBNET.ATG" 
		string parameterName = t.val; 
		if (
#line  2198 "VBNET.ATG" 
IsDims()) {
			ArrayTypeModifiers(
#line  2198 "VBNET.ATG" 
out arrayModifiers);
		}
		if (la.kind == 48) {
			lexer.NextToken();
			TypeName(
#line  2199 "VBNET.ATG" 
out type);
		}

#line  2201 "VBNET.ATG" 
		if(type != null) {
		if (arrayModifiers != null) {
			if (type.RankSpecifier != null) {
				Error("array rank only allowed one time");
			} else {
				type.RankSpecifier = (int[])arrayModifiers.ToArray(typeof(int));
			}
		}
		} else {
			type = new TypeReference("System.Object", arrayModifiers == null ? null : (int[])arrayModifiers.ToArray(typeof(int)));
		}
		
		if (la.kind == 11) {
			lexer.NextToken();
			Expr(
#line  2213 "VBNET.ATG" 
out expr);
		}

#line  2215 "VBNET.ATG" 
		mod.Check();
		p = new ParameterDeclarationExpression(type, parameterName, mod.Modifier, expr);
		
	}

	void ParameterModifier(
#line  2867 "VBNET.ATG" 
ParamModifierList m) {
		if (la.kind == 55) {
			lexer.NextToken();

#line  2868 "VBNET.ATG" 
			m.Add(ParameterModifiers.In); 
		} else if (la.kind == 53) {
			lexer.NextToken();

#line  2869 "VBNET.ATG" 
			m.Add(ParameterModifiers.Ref); 
		} else if (la.kind == 137) {
			lexer.NextToken();

#line  2870 "VBNET.ATG" 
			m.Add(ParameterModifiers.Optional); 
		} else if (la.kind == 143) {
			lexer.NextToken();

#line  2871 "VBNET.ATG" 
			m.Add(ParameterModifiers.Params); 
		} else SynErr(252);
	}

	void Statement() {

#line  2243 "VBNET.ATG" 
		Statement stmt = null;
		Location startPos = la.Location;
		string label = String.Empty;
		
		
		if (la.kind == 1 || la.kind == 13) {
		} else if (
#line  2249 "VBNET.ATG" 
IsLabel()) {
			LabelName(
#line  2249 "VBNET.ATG" 
out label);

#line  2251 "VBNET.ATG" 
			compilationUnit.AddChild(new LabelStatement(t.val));
			
			Expect(13);
			Statement();
		} else if (StartOf(34)) {
			EmbeddedStatement(
#line  2254 "VBNET.ATG" 
out stmt);

#line  2254 "VBNET.ATG" 
			compilationUnit.AddChild(stmt); 
		} else if (StartOf(35)) {
			LocalDeclarationStatement(
#line  2255 "VBNET.ATG" 
out stmt);

#line  2255 "VBNET.ATG" 
			compilationUnit.AddChild(stmt); 
		} else SynErr(253);

#line  2258 "VBNET.ATG" 
		if (stmt != null) {
		stmt.StartLocation = startPos;
		stmt.EndLocation = t.Location;
		}
		
	}

	void LabelName(
#line  2646 "VBNET.ATG" 
out string name) {

#line  2648 "VBNET.ATG" 
		name = String.Empty;
		
		if (StartOf(13)) {
			Identifier();

#line  2650 "VBNET.ATG" 
			name = t.val; 
		} else if (la.kind == 5) {
			lexer.NextToken();

#line  2651 "VBNET.ATG" 
			name = t.val; 
		} else SynErr(254);
	}

	void EmbeddedStatement(
#line  2297 "VBNET.ATG" 
out Statement statement) {

#line  2299 "VBNET.ATG" 
		Statement embeddedStatement = null;
		statement = null;
		Expression expr = null;
		string name = String.Empty;
		List<Expression> p = null;
		
		switch (la.kind) {
		case 94: {
			lexer.NextToken();

#line  2305 "VBNET.ATG" 
			ExitType exitType = ExitType.None; 
			switch (la.kind) {
			case 167: {
				lexer.NextToken();

#line  2307 "VBNET.ATG" 
				exitType = ExitType.Sub; 
				break;
			}
			case 100: {
				lexer.NextToken();

#line  2309 "VBNET.ATG" 
				exitType = ExitType.Function; 
				break;
			}
			case 146: {
				lexer.NextToken();

#line  2311 "VBNET.ATG" 
				exitType = ExitType.Property; 
				break;
			}
			case 83: {
				lexer.NextToken();

#line  2313 "VBNET.ATG" 
				exitType = ExitType.Do; 
				break;
			}
			case 98: {
				lexer.NextToken();

#line  2315 "VBNET.ATG" 
				exitType = ExitType.For; 
				break;
			}
			case 174: {
				lexer.NextToken();

#line  2317 "VBNET.ATG" 
				exitType = ExitType.Try; 
				break;
			}
			case 181: {
				lexer.NextToken();

#line  2319 "VBNET.ATG" 
				exitType = ExitType.While; 
				break;
			}
			case 155: {
				lexer.NextToken();

#line  2321 "VBNET.ATG" 
				exitType = ExitType.Select; 
				break;
			}
			default: SynErr(255); break;
			}

#line  2323 "VBNET.ATG" 
			statement = new ExitStatement(exitType); 
			break;
		}
		case 174: {
			TryStatement(
#line  2324 "VBNET.ATG" 
out statement);
			break;
		}
		case 187: {
			lexer.NextToken();

#line  2325 "VBNET.ATG" 
			ContinueType continueType = ContinueType.None; 
			if (la.kind == 83 || la.kind == 98 || la.kind == 181) {
				if (la.kind == 83) {
					lexer.NextToken();

#line  2325 "VBNET.ATG" 
					continueType = ContinueType.Do; 
				} else if (la.kind == 98) {
					lexer.NextToken();

#line  2325 "VBNET.ATG" 
					continueType = ContinueType.For; 
				} else {
					lexer.NextToken();

#line  2325 "VBNET.ATG" 
					continueType = ContinueType.While; 
				}
			}

#line  2325 "VBNET.ATG" 
			statement = new ContinueStatement(continueType); 
			break;
		}
		case 171: {
			lexer.NextToken();
			if (StartOf(27)) {
				Expr(
#line  2327 "VBNET.ATG" 
out expr);
			}

#line  2327 "VBNET.ATG" 
			statement = new ThrowStatement(expr); 
			break;
		}
		case 154: {
			lexer.NextToken();
			if (StartOf(27)) {
				Expr(
#line  2329 "VBNET.ATG" 
out expr);
			}

#line  2329 "VBNET.ATG" 
			statement = new ReturnStatement(expr); 
			break;
		}
		case 168: {
			lexer.NextToken();
			Expr(
#line  2331 "VBNET.ATG" 
out expr);
			EndOfStmt();
			Block(
#line  2331 "VBNET.ATG" 
out embeddedStatement);
			Expect(88);
			Expect(168);

#line  2332 "VBNET.ATG" 
			statement = new LockStatement(expr, embeddedStatement); 
			break;
		}
		case 149: {
			lexer.NextToken();
			Identifier();

#line  2334 "VBNET.ATG" 
			name = t.val; 
			if (la.kind == 24) {
				lexer.NextToken();
				if (StartOf(30)) {
					ArgumentList(
#line  2335 "VBNET.ATG" 
out p);
				}
				Expect(25);
			}

#line  2336 "VBNET.ATG" 
			statement = new RaiseEventStatement(name, p); 
			break;
		}
		case 182: {
			WithStatement(
#line  2338 "VBNET.ATG" 
out statement);
			break;
		}
		case 42: {
			lexer.NextToken();

#line  2340 "VBNET.ATG" 
			Expression handlerExpr = null; 
			Expr(
#line  2341 "VBNET.ATG" 
out expr);
			Expect(12);
			Expr(
#line  2341 "VBNET.ATG" 
out handlerExpr);

#line  2343 "VBNET.ATG" 
			statement = new AddHandlerStatement(expr, handlerExpr);
			
			break;
		}
		case 152: {
			lexer.NextToken();

#line  2346 "VBNET.ATG" 
			Expression handlerExpr = null; 
			Expr(
#line  2347 "VBNET.ATG" 
out expr);
			Expect(12);
			Expr(
#line  2347 "VBNET.ATG" 
out handlerExpr);

#line  2349 "VBNET.ATG" 
			statement = new RemoveHandlerStatement(expr, handlerExpr);
			
			break;
		}
		case 181: {
			lexer.NextToken();
			Expr(
#line  2352 "VBNET.ATG" 
out expr);
			EndOfStmt();
			Block(
#line  2353 "VBNET.ATG" 
out embeddedStatement);
			Expect(88);
			Expect(181);

#line  2355 "VBNET.ATG" 
			statement = new DoLoopStatement(expr, embeddedStatement, ConditionType.While, ConditionPosition.Start);
			
			break;
		}
		case 83: {
			lexer.NextToken();

#line  2360 "VBNET.ATG" 
			ConditionType conditionType = ConditionType.None;
			
			if (la.kind == 177 || la.kind == 181) {
				WhileOrUntil(
#line  2363 "VBNET.ATG" 
out conditionType);
				Expr(
#line  2363 "VBNET.ATG" 
out expr);
				EndOfStmt();
				Block(
#line  2364 "VBNET.ATG" 
out embeddedStatement);
				Expect(118);

#line  2367 "VBNET.ATG" 
				statement = new DoLoopStatement(expr, 
				                               embeddedStatement, 
				                               conditionType == ConditionType.While ? ConditionType.DoWhile : conditionType, 
				                               ConditionPosition.Start);
				
			} else if (la.kind == 1 || la.kind == 13) {
				EndOfStmt();
				Block(
#line  2374 "VBNET.ATG" 
out embeddedStatement);
				Expect(118);
				if (la.kind == 177 || la.kind == 181) {
					WhileOrUntil(
#line  2375 "VBNET.ATG" 
out conditionType);
					Expr(
#line  2375 "VBNET.ATG" 
out expr);
				}

#line  2377 "VBNET.ATG" 
				statement = new DoLoopStatement(expr, embeddedStatement, conditionType, ConditionPosition.End);
				
			} else SynErr(256);
			break;
		}
		case 98: {
			lexer.NextToken();

#line  2382 "VBNET.ATG" 
			Expression group = null;
			TypeReference typeReference;
			string        typeName;
			Location startLocation = t.Location;
			
			if (la.kind == 85) {
				lexer.NextToken();
				LoopControlVariable(
#line  2389 "VBNET.ATG" 
out typeReference, out typeName);
				Expect(109);
				Expr(
#line  2390 "VBNET.ATG" 
out group);
				EndOfStmt();
				Block(
#line  2391 "VBNET.ATG" 
out embeddedStatement);
				Expect(128);
				if (StartOf(27)) {
					Expr(
#line  2392 "VBNET.ATG" 
out expr);
				}

#line  2394 "VBNET.ATG" 
				statement = new ForeachStatement(typeReference, 
				                                typeName,
				                                group, 
				                                embeddedStatement, 
				                                expr);
				statement.StartLocation = startLocation;
				statement.EndLocation   = t.EndLocation;
				
				
			} else if (StartOf(13)) {

#line  2405 "VBNET.ATG" 
				Expression start = null;
				Expression end = null;
				Expression step = null;
				Expression nextExpr = null;List<Expression> nextExpressions = null;
				
				LoopControlVariable(
#line  2410 "VBNET.ATG" 
out typeReference, out typeName);
				Expect(11);
				Expr(
#line  2411 "VBNET.ATG" 
out start);
				Expect(172);
				Expr(
#line  2411 "VBNET.ATG" 
out end);
				if (la.kind == 162) {
					lexer.NextToken();
					Expr(
#line  2411 "VBNET.ATG" 
out step);
				}
				EndOfStmt();
				Block(
#line  2412 "VBNET.ATG" 
out embeddedStatement);
				Expect(128);
				if (StartOf(27)) {
					Expr(
#line  2415 "VBNET.ATG" 
out nextExpr);

#line  2415 "VBNET.ATG" 
					nextExpressions = new List<Expression>(); nextExpressions.Add(nextExpr); 
					while (la.kind == 12) {
						lexer.NextToken();
						Expr(
#line  2416 "VBNET.ATG" 
out nextExpr);

#line  2416 "VBNET.ATG" 
						nextExpressions.Add(nextExpr); 
					}
				}

#line  2419 "VBNET.ATG" 
				statement = new ForNextStatement(typeReference, typeName, start, end, step, embeddedStatement, nextExpressions);
				
			} else SynErr(257);
			break;
		}
		case 92: {
			lexer.NextToken();
			Expr(
#line  2423 "VBNET.ATG" 
out expr);

#line  2423 "VBNET.ATG" 
			statement = new ErrorStatement(expr); 
			break;
		}
		case 151: {
			lexer.NextToken();

#line  2425 "VBNET.ATG" 
			bool isPreserve = false; 
			if (la.kind == 144) {
				lexer.NextToken();

#line  2425 "VBNET.ATG" 
				isPreserve = true; 
			}
			ReDimClause(
#line  2426 "VBNET.ATG" 
out expr);

#line  2428 "VBNET.ATG" 
			ReDimStatement reDimStatement = new ReDimStatement(isPreserve);
			statement = reDimStatement;
			SafeAdd(reDimStatement, reDimStatement.ReDimClauses, expr as InvocationExpression);
			
			while (la.kind == 12) {
				lexer.NextToken();
				ReDimClause(
#line  2432 "VBNET.ATG" 
out expr);

#line  2433 "VBNET.ATG" 
				SafeAdd(reDimStatement, reDimStatement.ReDimClauses, expr as InvocationExpression); 
			}
			break;
		}
		case 91: {
			lexer.NextToken();
			Expr(
#line  2437 "VBNET.ATG" 
out expr);

#line  2438 "VBNET.ATG" 
			List<Expression> arrays = new List<Expression>();
			if (expr != null) { arrays.Add(expr);}
			EraseStatement eraseStatement = new EraseStatement(arrays);
			
			
			while (la.kind == 12) {
				lexer.NextToken();
				Expr(
#line  2443 "VBNET.ATG" 
out expr);

#line  2443 "VBNET.ATG" 
				if (expr != null) { arrays.Add(expr); }
			}

#line  2444 "VBNET.ATG" 
			statement = eraseStatement; 
			break;
		}
		case 163: {
			lexer.NextToken();

#line  2446 "VBNET.ATG" 
			statement = new StopStatement(); 
			break;
		}
		case 106: {
			lexer.NextToken();

#line  2448 "VBNET.ATG" 
			Location ifStartLocation = t.Location; 
			Expr(
#line  2448 "VBNET.ATG" 
out expr);
			if (la.kind == 170) {
				lexer.NextToken();
			}
			if (la.kind == 1 || la.kind == 13) {
				EndOfStmt();
				Block(
#line  2451 "VBNET.ATG" 
out embeddedStatement);

#line  2453 "VBNET.ATG" 
				IfElseStatement ifStatement = new IfElseStatement(expr, embeddedStatement);
				ifStatement.StartLocation = ifStartLocation;
				Location elseIfStart;
				
				while (la.kind == 87 || 
#line  2459 "VBNET.ATG" 
IsElseIf()) {
					if (
#line  2459 "VBNET.ATG" 
IsElseIf()) {
						Expect(86);

#line  2459 "VBNET.ATG" 
						elseIfStart = t.Location; 
						Expect(106);
					} else {
						lexer.NextToken();

#line  2460 "VBNET.ATG" 
						elseIfStart = t.Location; 
					}

#line  2462 "VBNET.ATG" 
					Expression condition = null; Statement block = null; 
					Expr(
#line  2463 "VBNET.ATG" 
out condition);
					if (la.kind == 170) {
						lexer.NextToken();
					}
					EndOfStmt();
					Block(
#line  2464 "VBNET.ATG" 
out block);

#line  2466 "VBNET.ATG" 
					ElseIfSection elseIfSection = new ElseIfSection(condition, block);
					elseIfSection.StartLocation = elseIfStart;
					elseIfSection.EndLocation = t.Location;
					elseIfSection.Parent = ifStatement;
					ifStatement.ElseIfSections.Add(elseIfSection);
					
				}
				if (la.kind == 86) {
					lexer.NextToken();
					EndOfStmt();
					Block(
#line  2475 "VBNET.ATG" 
out embeddedStatement);

#line  2477 "VBNET.ATG" 
					ifStatement.FalseStatement.Add(embeddedStatement);
					
				}
				Expect(88);
				Expect(106);

#line  2481 "VBNET.ATG" 
				ifStatement.EndLocation = t.Location;
				statement = ifStatement;
				
			} else if (StartOf(36)) {

#line  2486 "VBNET.ATG" 
				IfElseStatement ifStatement = new IfElseStatement(expr);
				ifStatement.StartLocation = ifStartLocation;
				
				SingleLineStatementList(
#line  2489 "VBNET.ATG" 
ifStatement.TrueStatement);
				if (la.kind == 86) {
					lexer.NextToken();
					if (StartOf(36)) {
						SingleLineStatementList(
#line  2492 "VBNET.ATG" 
ifStatement.FalseStatement);
					}
				}

#line  2494 "VBNET.ATG" 
				ifStatement.EndLocation = t.Location; statement = ifStatement; 
			} else SynErr(258);
			break;
		}
		case 155: {
			lexer.NextToken();
			if (la.kind == 57) {
				lexer.NextToken();
			}
			Expr(
#line  2497 "VBNET.ATG" 
out expr);
			EndOfStmt();

#line  2498 "VBNET.ATG" 
			List<SwitchSection> selectSections = new List<SwitchSection>();
			Statement block = null;
			
			while (la.kind == 57) {

#line  2502 "VBNET.ATG" 
				List<CaseLabel> caseClauses = null; Location caseLocation = la.Location; 
				lexer.NextToken();
				CaseClauses(
#line  2503 "VBNET.ATG" 
out caseClauses);
				if (
#line  2503 "VBNET.ATG" 
IsNotStatementSeparator()) {
					lexer.NextToken();
				}
				EndOfStmt();

#line  2505 "VBNET.ATG" 
				SwitchSection selectSection = new SwitchSection(caseClauses);
				selectSection.StartLocation = caseLocation;
				
				Block(
#line  2508 "VBNET.ATG" 
out block);

#line  2510 "VBNET.ATG" 
				selectSection.Children = block.Children;
				selectSection.EndLocation = t.EndLocation;
				selectSections.Add(selectSection);
				
			}

#line  2515 "VBNET.ATG" 
			statement = new SwitchStatement(expr, selectSections); 
			Expect(88);
			Expect(155);
			break;
		}
		case 135: {

#line  2517 "VBNET.ATG" 
			OnErrorStatement onErrorStatement = null; 
			OnErrorStatement(
#line  2518 "VBNET.ATG" 
out onErrorStatement);

#line  2518 "VBNET.ATG" 
			statement = onErrorStatement; 
			break;
		}
		case 104: {

#line  2519 "VBNET.ATG" 
			GotoStatement goToStatement = null; 
			GotoStatement(
#line  2520 "VBNET.ATG" 
out goToStatement);

#line  2520 "VBNET.ATG" 
			statement = goToStatement; 
			break;
		}
		case 153: {

#line  2521 "VBNET.ATG" 
			ResumeStatement resumeStatement = null; 
			ResumeStatement(
#line  2522 "VBNET.ATG" 
out resumeStatement);

#line  2522 "VBNET.ATG" 
			statement = resumeStatement; 
			break;
		}
		case 2: case 3: case 4: case 5: case 6: case 7: case 8: case 9: case 10: case 24: case 43: case 47: case 49: case 50: case 51: case 52: case 54: case 59: case 60: case 61: case 62: case 63: case 64: case 65: case 66: case 68: case 69: case 70: case 72: case 73: case 74: case 75: case 76: case 77: case 82: case 84: case 95: case 96: case 102: case 111: case 117: case 119: case 124: case 125: case 127: case 130: case 133: case 134: case 144: case 159: case 160: case 165: case 169: case 173: case 175: case 176: case 177: case 191: case 192: case 193: case 194: case 195: case 196: case 197: case 198: case 199: case 200: case 205: {

#line  2525 "VBNET.ATG" 
			Expression val = null;
			AssignmentOperatorType op;
			
			bool mustBeAssignment = la.kind == Tokens.Plus  || la.kind == Tokens.Minus ||
			                        la.kind == Tokens.Not   || la.kind == Tokens.Times;
			
			SimpleExpr(
#line  2531 "VBNET.ATG" 
out expr);
			if (StartOf(37)) {
				AssignmentOperator(
#line  2533 "VBNET.ATG" 
out op);
				Expr(
#line  2533 "VBNET.ATG" 
out val);

#line  2533 "VBNET.ATG" 
				expr = new AssignmentExpression(expr, op, val); 
			} else if (la.kind == 1 || la.kind == 13 || la.kind == 86) {

#line  2534 "VBNET.ATG" 
				if (mustBeAssignment) Error("error in assignment."); 
			} else SynErr(259);

#line  2537 "VBNET.ATG" 
			// a field reference expression that stands alone is a
			// invocation expression without parantheses and arguments
			if(expr is MemberReferenceExpression || expr is IdentifierExpression) {
				expr = new InvocationExpression(expr);
			}
			statement = new ExpressionStatement(expr);
			
			break;
		}
		case 56: {
			lexer.NextToken();
			SimpleExpr(
#line  2544 "VBNET.ATG" 
out expr);

#line  2544 "VBNET.ATG" 
			statement = new ExpressionStatement(expr); 
			break;
		}
		case 189: {
			lexer.NextToken();

#line  2546 "VBNET.ATG" 
			Statement block;  
			if (
#line  2547 "VBNET.ATG" 
Peek(1).kind == Tokens.As) {

#line  2548 "VBNET.ATG" 
				LocalVariableDeclaration resourceAquisition = new LocalVariableDeclaration(Modifiers.None); 
				VariableDeclarator(
#line  2549 "VBNET.ATG" 
resourceAquisition.Variables);
				while (la.kind == 12) {
					lexer.NextToken();
					VariableDeclarator(
#line  2551 "VBNET.ATG" 
resourceAquisition.Variables);
				}
				Block(
#line  2553 "VBNET.ATG" 
out block);

#line  2554 "VBNET.ATG" 
				statement = new UsingStatement(resourceAquisition, block); 
			} else if (StartOf(27)) {
				Expr(
#line  2555 "VBNET.ATG" 
out expr);
				Block(
#line  2556 "VBNET.ATG" 
out block);

#line  2557 "VBNET.ATG" 
				statement = new UsingStatement(new ExpressionStatement(expr), block); 
			} else SynErr(260);
			Expect(88);
			Expect(189);
			break;
		}
		default: SynErr(261); break;
		}
	}

	void LocalDeclarationStatement(
#line  2266 "VBNET.ATG" 
out Statement statement) {

#line  2268 "VBNET.ATG" 
		ModifierList m = new ModifierList();
		LocalVariableDeclaration localVariableDeclaration;
		bool dimfound = false;
		
		while (la.kind == 71 || la.kind == 81 || la.kind == 161) {
			if (la.kind == 71) {
				lexer.NextToken();

#line  2274 "VBNET.ATG" 
				m.Add(Modifiers.Const, t.Location); 
			} else if (la.kind == 161) {
				lexer.NextToken();

#line  2275 "VBNET.ATG" 
				m.Add(Modifiers.Static, t.Location); 
			} else {
				lexer.NextToken();

#line  2276 "VBNET.ATG" 
				dimfound = true; 
			}
		}

#line  2279 "VBNET.ATG" 
		if(dimfound && (m.Modifier & Modifiers.Const) != 0) {
		Error("Dim is not allowed on constants.");
		}
		
		if(m.isNone && dimfound == false) {
			Error("Const, Dim or Static expected");
		}
		
		localVariableDeclaration = new LocalVariableDeclaration(m.Modifier);
		localVariableDeclaration.StartLocation = t.Location;
		
		VariableDeclarator(
#line  2290 "VBNET.ATG" 
localVariableDeclaration.Variables);
		while (la.kind == 12) {
			lexer.NextToken();
			VariableDeclarator(
#line  2291 "VBNET.ATG" 
localVariableDeclaration.Variables);
		}

#line  2293 "VBNET.ATG" 
		statement = localVariableDeclaration;
		
	}

	void TryStatement(
#line  2760 "VBNET.ATG" 
out Statement tryStatement) {

#line  2762 "VBNET.ATG" 
		Statement blockStmt = null, finallyStmt = null;List<CatchClause> catchClauses = null;
		
		Expect(174);
		EndOfStmt();
		Block(
#line  2765 "VBNET.ATG" 
out blockStmt);
		if (la.kind == 58 || la.kind == 88 || la.kind == 97) {
			CatchClauses(
#line  2766 "VBNET.ATG" 
out catchClauses);
		}
		if (la.kind == 97) {
			lexer.NextToken();
			EndOfStmt();
			Block(
#line  2767 "VBNET.ATG" 
out finallyStmt);
		}
		Expect(88);
		Expect(174);

#line  2770 "VBNET.ATG" 
		tryStatement = new TryCatchStatement(blockStmt, catchClauses, finallyStmt);
		
	}

	void WithStatement(
#line  2740 "VBNET.ATG" 
out Statement withStatement) {

#line  2742 "VBNET.ATG" 
		Statement blockStmt = null;
		Expression expr = null;
		
		Expect(182);

#line  2745 "VBNET.ATG" 
		Location start = t.Location; 
		Expr(
#line  2746 "VBNET.ATG" 
out expr);
		EndOfStmt();

#line  2748 "VBNET.ATG" 
		withStatement = new WithStatement(expr);
		withStatement.StartLocation = start;
		
		Block(
#line  2751 "VBNET.ATG" 
out blockStmt);

#line  2753 "VBNET.ATG" 
		((WithStatement)withStatement).Body = (BlockStatement)blockStmt;
		
		Expect(88);
		Expect(182);

#line  2756 "VBNET.ATG" 
		withStatement.EndLocation = t.Location; 
	}

	void WhileOrUntil(
#line  2733 "VBNET.ATG" 
out ConditionType conditionType) {

#line  2734 "VBNET.ATG" 
		conditionType = ConditionType.None; 
		if (la.kind == 181) {
			lexer.NextToken();

#line  2735 "VBNET.ATG" 
			conditionType = ConditionType.While; 
		} else if (la.kind == 177) {
			lexer.NextToken();

#line  2736 "VBNET.ATG" 
			conditionType = ConditionType.Until; 
		} else SynErr(262);
	}

	void LoopControlVariable(
#line  2576 "VBNET.ATG" 
out TypeReference type, out string name) {

#line  2577 "VBNET.ATG" 
		ArrayList arrayModifiers = null;
		type = null;
		
		Qualident(
#line  2581 "VBNET.ATG" 
out name);
		if (
#line  2582 "VBNET.ATG" 
IsDims()) {
			ArrayTypeModifiers(
#line  2582 "VBNET.ATG" 
out arrayModifiers);
		}
		if (la.kind == 48) {
			lexer.NextToken();
			TypeName(
#line  2583 "VBNET.ATG" 
out type);

#line  2583 "VBNET.ATG" 
			if (name.IndexOf('.') > 0) { Error("No type def for 'for each' member indexer allowed."); } 
		}

#line  2585 "VBNET.ATG" 
		if (type != null) {
		if(type.RankSpecifier != null && arrayModifiers != null) {
			Error("array rank only allowed one time");
		} else if (arrayModifiers != null) {
			type.RankSpecifier = (int[])arrayModifiers.ToArray(typeof(int));
		}
		}
		
	}

	void ReDimClause(
#line  2655 "VBNET.ATG" 
out Expression expr) {
		SimpleNonInvocationExpression(
#line  2657 "VBNET.ATG" 
out expr);
		ReDimClauseInternal(
#line  2658 "VBNET.ATG" 
ref expr);
	}

	void SingleLineStatementList(
#line  2562 "VBNET.ATG" 
List<Statement> list) {

#line  2563 "VBNET.ATG" 
		Statement embeddedStatement = null; 
		if (la.kind == 88) {
			lexer.NextToken();

#line  2565 "VBNET.ATG" 
			embeddedStatement = new EndStatement(); 
		} else if (StartOf(34)) {
			EmbeddedStatement(
#line  2566 "VBNET.ATG" 
out embeddedStatement);
		} else SynErr(263);

#line  2567 "VBNET.ATG" 
		if (embeddedStatement != null) list.Add(embeddedStatement); 
		while (la.kind == 13) {
			lexer.NextToken();
			while (la.kind == 13) {
				lexer.NextToken();
			}
			if (la.kind == 88) {
				lexer.NextToken();

#line  2569 "VBNET.ATG" 
				embeddedStatement = new EndStatement(); 
			} else if (StartOf(34)) {
				EmbeddedStatement(
#line  2570 "VBNET.ATG" 
out embeddedStatement);
			} else SynErr(264);

#line  2571 "VBNET.ATG" 
			if (embeddedStatement != null) list.Add(embeddedStatement); 
		}
	}

	void CaseClauses(
#line  2693 "VBNET.ATG" 
out List<CaseLabel> caseClauses) {

#line  2695 "VBNET.ATG" 
		caseClauses = new List<CaseLabel>();
		CaseLabel caseClause = null;
		
		CaseClause(
#line  2698 "VBNET.ATG" 
out caseClause);

#line  2698 "VBNET.ATG" 
		if (caseClause != null) { caseClauses.Add(caseClause); } 
		while (la.kind == 12) {
			lexer.NextToken();
			CaseClause(
#line  2699 "VBNET.ATG" 
out caseClause);

#line  2699 "VBNET.ATG" 
			if (caseClause != null) { caseClauses.Add(caseClause); } 
		}
	}

	void OnErrorStatement(
#line  2596 "VBNET.ATG" 
out OnErrorStatement stmt) {

#line  2598 "VBNET.ATG" 
		stmt = null;
		GotoStatement goToStatement = null;
		
		Expect(135);
		Expect(92);
		if (
#line  2604 "VBNET.ATG" 
IsNegativeLabelName()) {
			Expect(104);
			Expect(15);
			Expect(5);

#line  2606 "VBNET.ATG" 
			long intLabel = Int64.Parse(t.val);
			if(intLabel != 1) {
				Error("invalid label in on error statement.");
			}
			stmt = new OnErrorStatement(new GotoStatement((intLabel * -1).ToString()));
			
		} else if (la.kind == 104) {
			GotoStatement(
#line  2612 "VBNET.ATG" 
out goToStatement);

#line  2614 "VBNET.ATG" 
			string val = goToStatement.Label;
			
			// if value is numeric, make sure that is 0
			try {
				long intLabel = Int64.Parse(val);
				if(intLabel != 0) {
					Error("invalid label in on error statement.");
				}
			} catch {
			}
			stmt = new OnErrorStatement(goToStatement);
			
		} else if (la.kind == 153) {
			lexer.NextToken();
			Expect(128);

#line  2628 "VBNET.ATG" 
			stmt = new OnErrorStatement(new ResumeStatement(true));
			
		} else SynErr(265);
	}

	void GotoStatement(
#line  2634 "VBNET.ATG" 
out GotoStatement goToStatement) {

#line  2636 "VBNET.ATG" 
		string label = String.Empty;
		
		Expect(104);
		LabelName(
#line  2639 "VBNET.ATG" 
out label);

#line  2641 "VBNET.ATG" 
		goToStatement = new GotoStatement(label);
		
	}

	void ResumeStatement(
#line  2682 "VBNET.ATG" 
out ResumeStatement resumeStatement) {

#line  2684 "VBNET.ATG" 
		resumeStatement = null;
		string label = String.Empty;
		
		if (
#line  2687 "VBNET.ATG" 
IsResumeNext()) {
			Expect(153);
			Expect(128);

#line  2688 "VBNET.ATG" 
			resumeStatement = new ResumeStatement(true); 
		} else if (la.kind == 153) {
			lexer.NextToken();
			if (StartOf(38)) {
				LabelName(
#line  2689 "VBNET.ATG" 
out label);
			}

#line  2689 "VBNET.ATG" 
			resumeStatement = new ResumeStatement(label); 
		} else SynErr(266);
	}

	void ReDimClauseInternal(
#line  2661 "VBNET.ATG" 
ref Expression expr) {

#line  2662 "VBNET.ATG" 
		List<Expression> arguments; bool canBeNormal; bool canBeRedim; string name; 
		while (la.kind == 10 || 
#line  2665 "VBNET.ATG" 
la.kind == Tokens.OpenParenthesis && Peek(1).kind == Tokens.Of) {
			if (la.kind == 10) {
				lexer.NextToken();
				IdentifierOrKeyword(
#line  2664 "VBNET.ATG" 
out name);

#line  2664 "VBNET.ATG" 
				expr = new MemberReferenceExpression(expr, name); 
			} else {
				InvocationExpression(
#line  2666 "VBNET.ATG" 
ref expr);
			}
		}
		Expect(24);
		NormalOrReDimArgumentList(
#line  2669 "VBNET.ATG" 
out arguments, out canBeNormal, out canBeRedim);
		Expect(25);

#line  2671 "VBNET.ATG" 
		expr = new InvocationExpression(expr, arguments);
		if (canBeRedim == false || canBeNormal && (la.kind == Tokens.Dot || la.kind == Tokens.OpenParenthesis)) {
			if (this.Errors.Count == 0) {
				// don't recurse on parse errors - could result in endless recursion
				ReDimClauseInternal(ref expr);
			}
		}
		
	}

	void CaseClause(
#line  2703 "VBNET.ATG" 
out CaseLabel caseClause) {

#line  2705 "VBNET.ATG" 
		Expression expr = null;
		Expression sexpr = null;
		BinaryOperatorType op = BinaryOperatorType.None;
		caseClause = null;
		
		if (la.kind == 86) {
			lexer.NextToken();

#line  2711 "VBNET.ATG" 
			caseClause = new CaseLabel(); 
		} else if (StartOf(39)) {
			if (la.kind == 113) {
				lexer.NextToken();
			}
			switch (la.kind) {
			case 27: {
				lexer.NextToken();

#line  2715 "VBNET.ATG" 
				op = BinaryOperatorType.LessThan; 
				break;
			}
			case 26: {
				lexer.NextToken();

#line  2716 "VBNET.ATG" 
				op = BinaryOperatorType.GreaterThan; 
				break;
			}
			case 30: {
				lexer.NextToken();

#line  2717 "VBNET.ATG" 
				op = BinaryOperatorType.LessThanOrEqual; 
				break;
			}
			case 29: {
				lexer.NextToken();

#line  2718 "VBNET.ATG" 
				op = BinaryOperatorType.GreaterThanOrEqual; 
				break;
			}
			case 11: {
				lexer.NextToken();

#line  2719 "VBNET.ATG" 
				op = BinaryOperatorType.Equality; 
				break;
			}
			case 28: {
				lexer.NextToken();

#line  2720 "VBNET.ATG" 
				op = BinaryOperatorType.InEquality; 
				break;
			}
			default: SynErr(267); break;
			}
			Expr(
#line  2722 "VBNET.ATG" 
out expr);

#line  2724 "VBNET.ATG" 
			caseClause = new CaseLabel(op, expr);
			
		} else if (StartOf(27)) {
			Expr(
#line  2726 "VBNET.ATG" 
out expr);
			if (la.kind == 172) {
				lexer.NextToken();
				Expr(
#line  2726 "VBNET.ATG" 
out sexpr);
			}

#line  2728 "VBNET.ATG" 
			caseClause = new CaseLabel(expr, sexpr);
			
		} else SynErr(268);
	}

	void CatchClauses(
#line  2775 "VBNET.ATG" 
out List<CatchClause> catchClauses) {

#line  2777 "VBNET.ATG" 
		catchClauses = new List<CatchClause>();
		TypeReference type = null;
		Statement blockStmt = null;
		Expression expr = null;
		string name = String.Empty;
		
		while (la.kind == 58) {
			lexer.NextToken();
			if (StartOf(13)) {
				Identifier();

#line  2785 "VBNET.ATG" 
				name = t.val; 
				if (la.kind == 48) {
					lexer.NextToken();
					TypeName(
#line  2785 "VBNET.ATG" 
out type);
				}
			}
			if (la.kind == 180) {
				lexer.NextToken();
				Expr(
#line  2786 "VBNET.ATG" 
out expr);
			}
			EndOfStmt();
			Block(
#line  2788 "VBNET.ATG" 
out blockStmt);

#line  2789 "VBNET.ATG" 
			catchClauses.Add(new CatchClause(type, name, blockStmt, expr)); 
		}
	}


	
	public override void Parse()
	{
		VBNET();

	}
	
	protected override void SynErr(int line, int col, int errorNumber)
	{
		string s;
		switch (errorNumber) {
			case 0: s = "EOF expected"; break;
			case 1: s = "EOL expected"; break;
			case 2: s = "ident expected"; break;
			case 3: s = "LiteralString expected"; break;
			case 4: s = "LiteralCharacter expected"; break;
			case 5: s = "LiteralInteger expected"; break;
			case 6: s = "LiteralDouble expected"; break;
			case 7: s = "LiteralSingle expected"; break;
			case 8: s = "LiteralDecimal expected"; break;
			case 9: s = "LiteralDate expected"; break;
			case 10: s = "\".\" expected"; break;
			case 11: s = "\"=\" expected"; break;
			case 12: s = "\",\" expected"; break;
			case 13: s = "\":\" expected"; break;
			case 14: s = "\"+\" expected"; break;
			case 15: s = "\"-\" expected"; break;
			case 16: s = "\"*\" expected"; break;
			case 17: s = "\"/\" expected"; break;
			case 18: s = "\"\\\\\" expected"; break;
			case 19: s = "\"&\" expected"; break;
			case 20: s = "\"^\" expected"; break;
			case 21: s = "\"?\" expected"; break;
			case 22: s = "\"{\" expected"; break;
			case 23: s = "\"}\" expected"; break;
			case 24: s = "\"(\" expected"; break;
			case 25: s = "\")\" expected"; break;
			case 26: s = "\">\" expected"; break;
			case 27: s = "\"<\" expected"; break;
			case 28: s = "\"<>\" expected"; break;
			case 29: s = "\">=\" expected"; break;
			case 30: s = "\"<=\" expected"; break;
			case 31: s = "\"<<\" expected"; break;
			case 32: s = "\">>\" expected"; break;
			case 33: s = "\"+=\" expected"; break;
			case 34: s = "\"^=\" expected"; break;
			case 35: s = "\"-=\" expected"; break;
			case 36: s = "\"*=\" expected"; break;
			case 37: s = "\"/=\" expected"; break;
			case 38: s = "\"\\\\=\" expected"; break;
			case 39: s = "\"<<=\" expected"; break;
			case 40: s = "\">>=\" expected"; break;
			case 41: s = "\"&=\" expected"; break;
			case 42: s = "\"AddHandler\" expected"; break;
			case 43: s = "\"AddressOf\" expected"; break;
			case 44: s = "\"Alias\" expected"; break;
			case 45: s = "\"And\" expected"; break;
			case 46: s = "\"AndAlso\" expected"; break;
			case 47: s = "\"Ansi\" expected"; break;
			case 48: s = "\"As\" expected"; break;
			case 49: s = "\"Assembly\" expected"; break;
			case 50: s = "\"Auto\" expected"; break;
			case 51: s = "\"Binary\" expected"; break;
			case 52: s = "\"Boolean\" expected"; break;
			case 53: s = "\"ByRef\" expected"; break;
			case 54: s = "\"Byte\" expected"; break;
			case 55: s = "\"ByVal\" expected"; break;
			case 56: s = "\"Call\" expected"; break;
			case 57: s = "\"Case\" expected"; break;
			case 58: s = "\"Catch\" expected"; break;
			case 59: s = "\"CBool\" expected"; break;
			case 60: s = "\"CByte\" expected"; break;
			case 61: s = "\"CChar\" expected"; break;
			case 62: s = "\"CDate\" expected"; break;
			case 63: s = "\"CDbl\" expected"; break;
			case 64: s = "\"CDec\" expected"; break;
			case 65: s = "\"Char\" expected"; break;
			case 66: s = "\"CInt\" expected"; break;
			case 67: s = "\"Class\" expected"; break;
			case 68: s = "\"CLng\" expected"; break;
			case 69: s = "\"CObj\" expected"; break;
			case 70: s = "\"Compare\" expected"; break;
			case 71: s = "\"Const\" expected"; break;
			case 72: s = "\"CShort\" expected"; break;
			case 73: s = "\"CSng\" expected"; break;
			case 74: s = "\"CStr\" expected"; break;
			case 75: s = "\"CType\" expected"; break;
			case 76: s = "\"Date\" expected"; break;
			case 77: s = "\"Decimal\" expected"; break;
			case 78: s = "\"Declare\" expected"; break;
			case 79: s = "\"Default\" expected"; break;
			case 80: s = "\"Delegate\" expected"; break;
			case 81: s = "\"Dim\" expected"; break;
			case 82: s = "\"DirectCast\" expected"; break;
			case 83: s = "\"Do\" expected"; break;
			case 84: s = "\"Double\" expected"; break;
			case 85: s = "\"Each\" expected"; break;
			case 86: s = "\"Else\" expected"; break;
			case 87: s = "\"ElseIf\" expected"; break;
			case 88: s = "\"End\" expected"; break;
			case 89: s = "\"EndIf\" expected"; break;
			case 90: s = "\"Enum\" expected"; break;
			case 91: s = "\"Erase\" expected"; break;
			case 92: s = "\"Error\" expected"; break;
			case 93: s = "\"Event\" expected"; break;
			case 94: s = "\"Exit\" expected"; break;
			case 95: s = "\"Explicit\" expected"; break;
			case 96: s = "\"False\" expected"; break;
			case 97: s = "\"Finally\" expected"; break;
			case 98: s = "\"For\" expected"; break;
			case 99: s = "\"Friend\" expected"; break;
			case 100: s = "\"Function\" expected"; break;
			case 101: s = "\"Get\" expected"; break;
			case 102: s = "\"GetType\" expected"; break;
			case 103: s = "\"GoSub\" expected"; break;
			case 104: s = "\"GoTo\" expected"; break;
			case 105: s = "\"Handles\" expected"; break;
			case 106: s = "\"If\" expected"; break;
			case 107: s = "\"Implements\" expected"; break;
			case 108: s = "\"Imports\" expected"; break;
			case 109: s = "\"In\" expected"; break;
			case 110: s = "\"Inherits\" expected"; break;
			case 111: s = "\"Integer\" expected"; break;
			case 112: s = "\"Interface\" expected"; break;
			case 113: s = "\"Is\" expected"; break;
			case 114: s = "\"Let\" expected"; break;
			case 115: s = "\"Lib\" expected"; break;
			case 116: s = "\"Like\" expected"; break;
			case 117: s = "\"Long\" expected"; break;
			case 118: s = "\"Loop\" expected"; break;
			case 119: s = "\"Me\" expected"; break;
			case 120: s = "\"Mod\" expected"; break;
			case 121: s = "\"Module\" expected"; break;
			case 122: s = "\"MustInherit\" expected"; break;
			case 123: s = "\"MustOverride\" expected"; break;
			case 124: s = "\"MyBase\" expected"; break;
			case 125: s = "\"MyClass\" expected"; break;
			case 126: s = "\"Namespace\" expected"; break;
			case 127: s = "\"New\" expected"; break;
			case 128: s = "\"Next\" expected"; break;
			case 129: s = "\"Not\" expected"; break;
			case 130: s = "\"Nothing\" expected"; break;
			case 131: s = "\"NotInheritable\" expected"; break;
			case 132: s = "\"NotOverridable\" expected"; break;
			case 133: s = "\"Object\" expected"; break;
			case 134: s = "\"Off\" expected"; break;
			case 135: s = "\"On\" expected"; break;
			case 136: s = "\"Option\" expected"; break;
			case 137: s = "\"Optional\" expected"; break;
			case 138: s = "\"Or\" expected"; break;
			case 139: s = "\"OrElse\" expected"; break;
			case 140: s = "\"Overloads\" expected"; break;
			case 141: s = "\"Overridable\" expected"; break;
			case 142: s = "\"Overrides\" expected"; break;
			case 143: s = "\"ParamArray\" expected"; break;
			case 144: s = "\"Preserve\" expected"; break;
			case 145: s = "\"Private\" expected"; break;
			case 146: s = "\"Property\" expected"; break;
			case 147: s = "\"Protected\" expected"; break;
			case 148: s = "\"Public\" expected"; break;
			case 149: s = "\"RaiseEvent\" expected"; break;
			case 150: s = "\"ReadOnly\" expected"; break;
			case 151: s = "\"ReDim\" expected"; break;
			case 152: s = "\"RemoveHandler\" expected"; break;
			case 153: s = "\"Resume\" expected"; break;
			case 154: s = "\"Return\" expected"; break;
			case 155: s = "\"Select\" expected"; break;
			case 156: s = "\"Set\" expected"; break;
			case 157: s = "\"Shadows\" expected"; break;
			case 158: s = "\"Shared\" expected"; break;
			case 159: s = "\"Short\" expected"; break;
			case 160: s = "\"Single\" expected"; break;
			case 161: s = "\"Static\" expected"; break;
			case 162: s = "\"Step\" expected"; break;
			case 163: s = "\"Stop\" expected"; break;
			case 164: s = "\"Strict\" expected"; break;
			case 165: s = "\"String\" expected"; break;
			case 166: s = "\"Structure\" expected"; break;
			case 167: s = "\"Sub\" expected"; break;
			case 168: s = "\"SyncLock\" expected"; break;
			case 169: s = "\"Text\" expected"; break;
			case 170: s = "\"Then\" expected"; break;
			case 171: s = "\"Throw\" expected"; break;
			case 172: s = "\"To\" expected"; break;
			case 173: s = "\"True\" expected"; break;
			case 174: s = "\"Try\" expected"; break;
			case 175: s = "\"TypeOf\" expected"; break;
			case 176: s = "\"Unicode\" expected"; break;
			case 177: s = "\"Until\" expected"; break;
			case 178: s = "\"Variant\" expected"; break;
			case 179: s = "\"Wend\" expected"; break;
			case 180: s = "\"When\" expected"; break;
			case 181: s = "\"While\" expected"; break;
			case 182: s = "\"With\" expected"; break;
			case 183: s = "\"WithEvents\" expected"; break;
			case 184: s = "\"WriteOnly\" expected"; break;
			case 185: s = "\"Xor\" expected"; break;
			case 186: s = "\"Rem\" expected"; break;
			case 187: s = "\"Continue\" expected"; break;
			case 188: s = "\"Operator\" expected"; break;
			case 189: s = "\"Using\" expected"; break;
			case 190: s = "\"IsNot\" expected"; break;
			case 191: s = "\"SByte\" expected"; break;
			case 192: s = "\"UInteger\" expected"; break;
			case 193: s = "\"ULong\" expected"; break;
			case 194: s = "\"UShort\" expected"; break;
			case 195: s = "\"CSByte\" expected"; break;
			case 196: s = "\"CUShort\" expected"; break;
			case 197: s = "\"CUInt\" expected"; break;
			case 198: s = "\"CULng\" expected"; break;
			case 199: s = "\"Global\" expected"; break;
			case 200: s = "\"TryCast\" expected"; break;
			case 201: s = "\"Of\" expected"; break;
			case 202: s = "\"Narrowing\" expected"; break;
			case 203: s = "\"Widening\" expected"; break;
			case 204: s = "\"Partial\" expected"; break;
			case 205: s = "\"Custom\" expected"; break;
			case 206: s = "??? expected"; break;
			case 207: s = "invalid OptionStmt"; break;
			case 208: s = "invalid OptionStmt"; break;
			case 209: s = "invalid GlobalAttributeSection"; break;
			case 210: s = "invalid GlobalAttributeSection"; break;
			case 211: s = "invalid NamespaceMemberDecl"; break;
			case 212: s = "invalid OptionValue"; break;
			case 213: s = "invalid EndOfStmt"; break;
			case 214: s = "invalid TypeModifier"; break;
			case 215: s = "invalid NonModuleDeclaration"; break;
			case 216: s = "invalid NonModuleDeclaration"; break;
			case 217: s = "invalid Identifier"; break;
			case 218: s = "invalid TypeParameterConstraints"; break;
			case 219: s = "invalid TypeParameterConstraint"; break;
			case 220: s = "invalid NonArrayTypeName"; break;
			case 221: s = "invalid MemberModifier"; break;
			case 222: s = "invalid StructureMemberDecl"; break;
			case 223: s = "invalid StructureMemberDecl"; break;
			case 224: s = "invalid StructureMemberDecl"; break;
			case 225: s = "invalid StructureMemberDecl"; break;
			case 226: s = "invalid StructureMemberDecl"; break;
			case 227: s = "invalid StructureMemberDecl"; break;
			case 228: s = "invalid StructureMemberDecl"; break;
			case 229: s = "invalid InterfaceMemberDecl"; break;
			case 230: s = "invalid InterfaceMemberDecl"; break;
			case 231: s = "invalid Charset"; break;
			case 232: s = "invalid IdentifierForFieldDeclaration"; break;
			case 233: s = "invalid VariableDeclaratorPartAfterIdentifier"; break;
			case 234: s = "invalid AccessorDecls"; break;
			case 235: s = "invalid EventAccessorDeclaration"; break;
			case 236: s = "invalid OverloadableOperator"; break;
			case 237: s = "invalid VariableInitializer"; break;
			case 238: s = "invalid EventMemberSpecifier"; break;
			case 239: s = "invalid AssignmentOperator"; break;
			case 240: s = "invalid SimpleNonInvocationExpression"; break;
			case 241: s = "invalid SimpleNonInvocationExpression"; break;
			case 242: s = "invalid SimpleNonInvocationExpression"; break;
			case 243: s = "invalid SimpleNonInvocationExpression"; break;
			case 244: s = "invalid InvocationExpression"; break;
			case 245: s = "invalid InvocationExpression"; break;
			case 246: s = "invalid PrimitiveTypeName"; break;
			case 247: s = "invalid CastTarget"; break;
			case 248: s = "invalid ComparisonExpr"; break;
			case 249: s = "invalid Argument"; break;
			case 250: s = "invalid QualIdentAndTypeArguments"; break;
			case 251: s = "invalid AttributeArguments"; break;
			case 252: s = "invalid ParameterModifier"; break;
			case 253: s = "invalid Statement"; break;
			case 254: s = "invalid LabelName"; break;
			case 255: s = "invalid EmbeddedStatement"; break;
			case 256: s = "invalid EmbeddedStatement"; break;
			case 257: s = "invalid EmbeddedStatement"; break;
			case 258: s = "invalid EmbeddedStatement"; break;
			case 259: s = "invalid EmbeddedStatement"; break;
			case 260: s = "invalid EmbeddedStatement"; break;
			case 261: s = "invalid EmbeddedStatement"; break;
			case 262: s = "invalid WhileOrUntil"; break;
			case 263: s = "invalid SingleLineStatementList"; break;
			case 264: s = "invalid SingleLineStatementList"; break;
			case 265: s = "invalid OnErrorStatement"; break;
			case 266: s = "invalid ResumeStatement"; break;
			case 267: s = "invalid CaseClause"; break;
			case 268: s = "invalid CaseClause"; break;

			default: s = "error " + errorNumber; break;
		}
		this.Errors.Error(line, col, s);
	}
	
	private bool StartOf(int s)
	{
		return set[s, lexer.LookAhead.kind];
	}
	
	static bool[,] set = {
	{T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,T,T,x, x,x,T,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,T, T,x,x,x, x,x,x,x, x,T,T,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,T,T,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,T, T,x,x,x, x,x,x,x, x,T,T,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,T, T,x,x,x, x,x,x,x, x,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x},
	{x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,T,T, x,T,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,T,x,x, x,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x},
	{x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,T,T, T,x,T,x, x,x,x,x, x,x,x,x, x,T,x,T, x,x,T,x, x,x,x,x, T,T,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,T,T,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,T,T,x, x,T,x,x, x,x,x,x, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,T,T,x, x,x,x,T, x,x,x,x, x,T,x,x},
	{x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,T,T, T,x,T,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,T,x, x,x,x,x, T,T,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,T,x,x, x,T,x,x, x,x,x,x, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,T,T,x, x,x,x,T, x,x,x,x, x,T,x,x},
	{x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,T,T, x,x,x,x, x,x,T,T, T,T,x,x, x,x,x,x, x,x,T,x, x,T,x,T, x,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,T,T,T, x,x,x,x, x,x,x,T, T,x,T,x, x,x,x,x, T,T,T,x, T,T,T,T, T,x,T,x, x,x,x,x, x,T,T,x, x,x,x,x, x,x,T,T, x,T,x,x, x,x,x,x, T,T,x,x, x,x,x,T, T,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,T,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, T,T,T,x, x,T,x,T, T,x,T,x, x,x,x,x, x,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,T,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,T,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, T,T,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,T, T,T,x,x, x,x,x,x, x,x,T,x, x,T,x,x, x,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,T,T,T, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, T,T,T,x, x,T,T,T, T,x,T,x, x,x,x,x, x,T,T,x, x,x,x,x, x,x,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x},
	{x,T,T,T, T,T,T,T, T,T,T,x, x,T,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,x,x,T, x,T,T,T, T,x,T,x, T,x,x,T, T,T,T,T, T,T,T,x, T,T,T,T, T,T,T,T, T,T,x,x, x,T,T,T, T,x,x,x, T,x,x,T, T,x,T,T, T,x,T,x, x,x,T,x, T,x,T,x, x,x,x,T, x,x,x,x, x,T,x,T, x,x,x,x, T,T,x,T, x,x,T,x, x,T,T,T, x,x,x,x, x,x,x,x, T,x,x,x, x,T,x,T, T,T,T,T, x,x,x,T, T,T,x,T, x,T,x,x, T,T,x,T, x,T,T,T, T,T,x,x, x,T,T,x, x,x,x,T, x,T,x,T, T,T,T,T, T,T,T,T, T,x,x,x, x,T,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,T, T,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, T,T,T,x, x,T,T,T, T,x,T,x, x,x,x,x, x,T,T,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,T,T,T, T,T,T,T, T,T,T,x, x,T,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,x,x,T, x,T,T,T, T,x,T,x, T,x,x,T, T,T,T,T, T,T,T,x, T,T,T,T, T,T,T,T, T,T,x,x, x,T,T,T, T,x,x,x, x,x,x,T, T,x,T,T, T,x,T,x, x,x,T,x, T,x,T,x, x,x,x,T, x,x,x,x, x,T,x,T, x,x,x,x, T,T,x,T, x,x,T,x, x,T,T,T, x,x,x,x, x,x,x,x, T,x,x,x, x,T,x,T, T,T,T,T, x,x,x,T, T,T,x,T, x,T,x,x, T,T,x,T, x,T,T,T, T,T,x,x, x,T,T,x, x,x,x,T, x,T,x,T, T,T,T,T, T,T,T,T, T,x,x,x, x,T,x,x},
	{x,T,T,T, T,T,T,T, T,T,T,T, T,T,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,x,x,T, T,T,T,T, T,x,T,x, T,x,x,T, T,T,T,T, T,T,T,x, T,T,T,T, T,T,T,T, T,T,x,x, x,T,T,T, T,x,x,x, T,x,x,T, T,x,T,T, T,x,T,x, x,x,T,x, T,x,T,x, x,x,x,T, x,x,x,x, x,T,x,T, x,x,x,x, T,T,x,T, x,x,T,x, x,T,T,T, x,x,x,x, x,x,x,x, T,x,x,x, x,T,x,T, T,T,T,T, x,x,x,T, T,T,x,T, x,T,x,x, T,T,x,T, x,T,T,T, T,T,x,x, x,T,T,x, x,x,x,T, x,T,x,T, T,T,T,T, T,T,T,T, T,x,x,x, x,T,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,T, T,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,T, T,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,T,T, T,T,T,T, T,T,T,x, x,x,T,T, T,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,T, x,T,T,T, T,x,T,x, x,x,x,T, T,T,T,T, T,T,T,x, T,T,T,x, T,T,T,T, T,T,x,x, x,x,T,x, T,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,T,x, x,x,x,x, x,x,x,T, x,x,x,x, x,T,x,T, x,x,x,x, T,T,x,T, x,T,T,x, x,T,T,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,T,x,x, x,T,x,x, x,T,x,T, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,T,T,T, T,T,T,T, T,x,x,x, x,T,x,x},
	{x,x,T,T, T,T,T,T, T,T,T,x, x,x,T,T, T,x,x,x, x,x,T,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,T, x,T,T,T, T,x,T,x, x,x,x,T, T,T,T,T, T,T,T,x, T,T,T,x, T,T,T,T, T,T,x,x, x,x,T,x, T,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,T,x, x,x,x,x, x,x,x,T, x,x,x,x, x,T,x,T, x,x,x,x, T,T,x,T, x,T,T,x, x,T,T,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,T,x,x, x,T,x,x, x,T,x,T, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,T,T,T, T,T,T,T, T,x,x,x, x,T,x,x},
	{x,x,T,T, T,T,T,T, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,T, x,T,T,T, T,x,T,x, x,x,x,T, T,T,T,T, T,T,T,x, T,T,T,x, T,T,T,T, T,T,x,x, x,x,T,x, T,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,T,x, x,x,x,x, x,x,x,T, x,x,x,x, x,T,x,T, x,x,x,x, T,T,x,T, x,x,T,x, x,T,T,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,T,x,x, x,T,x,x, x,T,x,T, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,T,T,T, T,T,T,T, T,x,x,x, x,T,x,x},
	{x,x,T,T, T,T,T,T, T,T,T,x, T,x,T,T, T,x,x,x, x,x,x,x, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,T, x,T,T,T, T,x,T,x, x,x,x,T, T,T,T,T, T,T,T,x, T,T,T,x, T,T,T,T, T,T,x,x, x,x,T,x, T,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,T,x, x,x,x,x, x,x,x,T, x,x,x,x, x,T,x,T, x,x,x,x, T,T,x,T, x,T,T,x, x,T,T,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,T,x,x, x,T,x,x, x,T,x,T, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,T,T,T, T,T,T,T, T,x,x,x, x,T,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,T,T, T,T,T,T, T,T,T,x, x,x,T,T, T,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,T, x,T,T,T, T,x,T,x, x,x,x,T, T,T,T,T, T,T,T,x, T,T,T,x, T,T,T,T, T,T,x,x, x,x,T,x, T,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,T,x, x,x,x,x, x,x,x,T, x,x,x,x, x,T,x,T, x,x,x,x, T,T,x,T, x,x,T,x, x,T,T,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,T,x,x, x,T,x,x, x,T,x,T, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,T,T,T, T,T,T,T, T,x,x,x, x,T,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,T,T, T,T,T,T, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,x,x,T, x,T,T,T, T,x,T,x, T,x,x,T, T,T,T,T, T,T,T,x, T,T,T,x, T,T,T,T, T,T,x,x, x,x,T,T, T,x,x,x, x,x,x,T, T,x,T,T, T,x,T,x, x,x,T,x, T,x,T,x, x,x,x,T, x,x,x,x, x,T,x,T, x,x,x,x, T,T,x,T, x,x,T,x, x,T,T,T, x,x,x,x, x,x,x,x, T,x,x,x, x,T,x,T, T,T,T,T, x,x,x,T, T,x,x,T, x,T,x,x, T,T,x,T, x,T,T,T, T,T,x,x, x,T,T,x, x,x,x,T, x,T,x,T, T,T,T,T, T,T,T,T, T,x,x,x, x,T,x,x},
	{x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,T,x,x, x,x,x,x, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x},
	{x,x,T,T, T,T,T,T, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,x,x,T, x,T,T,T, T,x,T,x, T,x,x,T, T,T,T,T, T,T,T,x, T,T,T,x, T,T,T,T, T,T,x,x, x,x,T,T, T,x,x,x, T,x,x,T, T,x,T,T, T,x,T,x, x,x,T,x, T,x,T,x, x,x,x,T, x,x,x,x, x,T,x,T, x,x,x,x, T,T,x,T, x,x,T,x, x,T,T,T, x,x,x,x, x,x,x,x, T,x,x,x, x,T,x,T, T,T,T,T, x,x,x,T, T,x,x,T, x,T,x,x, T,T,x,T, x,T,T,T, T,T,x,x, x,T,T,x, x,x,x,T, x,T,x,T, T,T,T,T, T,T,T,T, T,x,x,x, x,T,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,T, T,T,T,T, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,T,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x}

	};
} // end Parser

}