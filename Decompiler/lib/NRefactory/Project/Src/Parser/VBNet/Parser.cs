
#line  1 "VBNET.ATG" 
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
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
	const int maxT = 238;

	const  bool   T            = true;
	const  bool   x            = false;
	

#line  13 "VBNET.ATG" 


/*

*/

	void VBNET() {

#line  263 "VBNET.ATG" 
		lexer.NextToken(); // get the first token
		compilationUnit = new CompilationUnit();
		BlockStart(compilationUnit);
		
		while (la.kind == 1 || la.kind == 21) {
			EndOfStmt();
		}
		while (la.kind == 173) {
			OptionStmt();
			while (la.kind == 1 || la.kind == 21) {
				EndOfStmt();
			}
		}
		while (la.kind == 137) {
			ImportsStmt();
			while (la.kind == 1 || la.kind == 21) {
				EndOfStmt();
			}
		}
		while (
#line  271 "VBNET.ATG" 
IsGlobalAttrTarget()) {
			GlobalAttributeSection();
			while (la.kind == 1 || la.kind == 21) {
				EndOfStmt();
			}
		}
		while (StartOf(1)) {
			NamespaceMemberDecl();
			while (la.kind == 1 || la.kind == 21) {
				EndOfStmt();
			}
		}
		Expect(0);
	}

	void EndOfStmt() {
		while (!(la.kind == 0 || la.kind == 1 || la.kind == 21)) {SynErr(239); lexer.NextToken(); }
		if (la.kind == 1) {
			lexer.NextToken();
		} else if (la.kind == 21) {
			lexer.NextToken();
		} else SynErr(240);
	}

	void OptionStmt() {

#line  276 "VBNET.ATG" 
		INode node = null; bool val = true; 
		Expect(173);

#line  277 "VBNET.ATG" 
		Location startPos = t.Location; 
		if (la.kind == 121) {
			lexer.NextToken();
			if (la.kind == 170 || la.kind == 171) {
				OptionValue(
#line  279 "VBNET.ATG" 
ref val);
			}

#line  280 "VBNET.ATG" 
			node = new OptionDeclaration(OptionType.Explicit, val); 
		} else if (la.kind == 207) {
			lexer.NextToken();
			if (la.kind == 170 || la.kind == 171) {
				OptionValue(
#line  282 "VBNET.ATG" 
ref val);
			}

#line  283 "VBNET.ATG" 
			node = new OptionDeclaration(OptionType.Strict, val); 
		} else if (la.kind == 87) {
			lexer.NextToken();
			if (la.kind == 67) {
				lexer.NextToken();

#line  285 "VBNET.ATG" 
				node = new OptionDeclaration(OptionType.CompareBinary, val); 
			} else if (la.kind == 213) {
				lexer.NextToken();

#line  286 "VBNET.ATG" 
				node = new OptionDeclaration(OptionType.CompareText, val); 
			} else SynErr(241);
		} else if (la.kind == 139) {
			lexer.NextToken();
			if (la.kind == 170 || la.kind == 171) {
				OptionValue(
#line  289 "VBNET.ATG" 
ref val);
			}

#line  290 "VBNET.ATG" 
			node = new OptionDeclaration(OptionType.Infer, val); 
		} else SynErr(242);
		EndOfStmt();

#line  294 "VBNET.ATG" 
		if (node != null) {
		node.StartLocation = startPos;
		node.EndLocation   = t.Location;
		AddChild(node);
		}
		
	}

	void ImportsStmt() {

#line  315 "VBNET.ATG" 
		List<Using> usings = new List<Using>();
		
		Expect(137);

#line  319 "VBNET.ATG" 
		Location startPos = t.Location;
		Using u;
		
		ImportClause(
#line  322 "VBNET.ATG" 
out u);

#line  322 "VBNET.ATG" 
		if (u != null) { usings.Add(u); } 
		while (la.kind == 22) {
			lexer.NextToken();
			ImportClause(
#line  324 "VBNET.ATG" 
out u);

#line  324 "VBNET.ATG" 
			if (u != null) { usings.Add(u); } 
		}
		EndOfStmt();

#line  328 "VBNET.ATG" 
		UsingDeclaration usingDeclaration = new UsingDeclaration(usings);
		usingDeclaration.StartLocation = startPos;
		usingDeclaration.EndLocation   = t.Location;
		AddChild(usingDeclaration);
		
	}

	void GlobalAttributeSection() {
		Expect(40);

#line  2817 "VBNET.ATG" 
		Location startPos = t.Location; 
		if (la.kind == 65) {
			lexer.NextToken();
		} else if (la.kind == 155) {
			lexer.NextToken();
		} else SynErr(243);

#line  2819 "VBNET.ATG" 
		string attributeTarget = t.val != null ? t.val.ToLower(System.Globalization.CultureInfo.InvariantCulture) : null;
		List<ASTAttribute> attributes = new List<ASTAttribute>();
		ASTAttribute attribute;
		
		Expect(21);
		Attribute(
#line  2823 "VBNET.ATG" 
out attribute);

#line  2823 "VBNET.ATG" 
		attributes.Add(attribute); 
		while (
#line  2824 "VBNET.ATG" 
NotFinalComma()) {
			if (la.kind == 22) {
				lexer.NextToken();
				if (la.kind == 65) {
					lexer.NextToken();
				} else if (la.kind == 155) {
					lexer.NextToken();
				} else SynErr(244);
				Expect(21);
			}
			Attribute(
#line  2824 "VBNET.ATG" 
out attribute);

#line  2824 "VBNET.ATG" 
			attributes.Add(attribute); 
		}
		if (la.kind == 22) {
			lexer.NextToken();
		}
		Expect(39);
		EndOfStmt();

#line  2829 "VBNET.ATG" 
		AttributeSection section = new AttributeSection {
		AttributeTarget = attributeTarget,
		Attributes = attributes,
		StartLocation = startPos,
		EndLocation = t.EndLocation
		};
		AddChild(section);
		
	}

	void NamespaceMemberDecl() {

#line  361 "VBNET.ATG" 
		ModifierList m = new ModifierList();
		AttributeSection section;
		List<AttributeSection> attributes = new List<AttributeSection>();
		string qualident;
		
		if (la.kind == 160) {
			lexer.NextToken();

#line  368 "VBNET.ATG" 
			Location startPos = t.Location;
			
			Qualident(
#line  370 "VBNET.ATG" 
out qualident);

#line  372 "VBNET.ATG" 
			INode node =  new NamespaceDeclaration(qualident);
			node.StartLocation = startPos;
			AddChild(node);
			BlockStart(node);
			
			EndOfStmt();
			NamespaceBody();

#line  380 "VBNET.ATG" 
			node.EndLocation = t.Location;
			BlockEnd();
			
		} else if (StartOf(2)) {
			while (la.kind == 40) {
				AttributeSection(
#line  384 "VBNET.ATG" 
out section);

#line  384 "VBNET.ATG" 
				attributes.Add(section); 
			}
			while (StartOf(3)) {
				TypeModifier(
#line  385 "VBNET.ATG" 
m);
			}
			NonModuleDeclaration(
#line  385 "VBNET.ATG" 
m, attributes);
		} else SynErr(245);
	}

	void OptionValue(
#line  302 "VBNET.ATG" 
ref bool val) {
		if (la.kind == 171) {
			lexer.NextToken();

#line  304 "VBNET.ATG" 
			val = true; 
		} else if (la.kind == 170) {
			lexer.NextToken();

#line  306 "VBNET.ATG" 
			val = false; 
		} else SynErr(246);
	}

	void ImportClause(
#line  335 "VBNET.ATG" 
out Using u) {

#line  337 "VBNET.ATG" 
		string qualident  = null;
		TypeReference aliasedType = null;
		u = null;
		
		if (StartOf(4)) {
			Qualident(
#line  342 "VBNET.ATG" 
out qualident);
			if (la.kind == 20) {
				lexer.NextToken();
				TypeName(
#line  343 "VBNET.ATG" 
out aliasedType);
			}

#line  345 "VBNET.ATG" 
			if (qualident != null && qualident.Length > 0) {
			if (aliasedType != null) {
				u = new Using(qualident, aliasedType);
			} else {
				u = new Using(qualident);
			}
			}
			
		} else if (la.kind == 10) {

#line  353 "VBNET.ATG" 
			string prefix = null; 
			lexer.NextToken();
			Identifier();

#line  354 "VBNET.ATG" 
			prefix = t.val; 
			Expect(20);
			Expect(3);

#line  354 "VBNET.ATG" 
			u = new Using(t.literalValue as string, prefix); 
			Expect(11);
		} else SynErr(247);
	}

	void Qualident(
#line  3617 "VBNET.ATG" 
out string qualident) {

#line  3619 "VBNET.ATG" 
		string name;
		qualidentBuilder.Length = 0; 
		
		Identifier();

#line  3623 "VBNET.ATG" 
		qualidentBuilder.Append(t.val); 
		while (
#line  3624 "VBNET.ATG" 
DotAndIdentOrKw()) {
			Expect(26);
			IdentifierOrKeyword(
#line  3624 "VBNET.ATG" 
out name);

#line  3624 "VBNET.ATG" 
			qualidentBuilder.Append('.'); qualidentBuilder.Append(name); 
		}

#line  3626 "VBNET.ATG" 
		qualident = qualidentBuilder.ToString(); 
	}

	void TypeName(
#line  2688 "VBNET.ATG" 
out TypeReference typeref) {

#line  2689 "VBNET.ATG" 
		ArrayList rank = null; Location startLocation = la.Location; 
		NonArrayTypeName(
#line  2691 "VBNET.ATG" 
out typeref, false);
		ArrayTypeModifiers(
#line  2692 "VBNET.ATG" 
out rank);

#line  2694 "VBNET.ATG" 
		if (typeref != null) {
		if (rank != null) {
			typeref.RankSpecifier = (int[])rank.ToArray(typeof(int));
		}
		typeref.StartLocation = startLocation;
		typeref.EndLocation = t.EndLocation;
		}
		
	}

	void Identifier() {
		if (StartOf(5)) {
			IdentifierForFieldDeclaration();
		} else if (la.kind == 98) {
			lexer.NextToken();
		} else SynErr(248);
	}

	void NamespaceBody() {
		while (la.kind == 1 || la.kind == 21) {
			EndOfStmt();
		}
		while (StartOf(1)) {
			NamespaceMemberDecl();
			while (la.kind == 1 || la.kind == 21) {
				EndOfStmt();
			}
		}
		Expect(113);
		Expect(160);
		EndOfStmt();
	}

	void AttributeSection(
#line  2895 "VBNET.ATG" 
out AttributeSection section) {

#line  2897 "VBNET.ATG" 
		string attributeTarget = "";
		List<ASTAttribute> attributes = new List<ASTAttribute>();
		ASTAttribute attribute;
		Location startLocation = la.Location;
		
		Expect(40);
		if (
#line  2903 "VBNET.ATG" 
IsLocalAttrTarget()) {
			if (la.kind == 119) {
				lexer.NextToken();

#line  2904 "VBNET.ATG" 
				attributeTarget = "event";
			} else if (la.kind == 195) {
				lexer.NextToken();

#line  2905 "VBNET.ATG" 
				attributeTarget = "return";
			} else {
				Identifier();

#line  2908 "VBNET.ATG" 
				string val = t.val.ToLower(System.Globalization.CultureInfo.InvariantCulture);
				if (val != "field"	|| val != "method" ||
					val != "module" || val != "param"  ||
					val != "property" || val != "type")
				Error("attribute target specifier (event, return, field," +
						"method, module, param, property, or type) expected");
				attributeTarget = t.val;
				
			}
			Expect(21);
		}
		Attribute(
#line  2918 "VBNET.ATG" 
out attribute);

#line  2918 "VBNET.ATG" 
		attributes.Add(attribute); 
		while (
#line  2919 "VBNET.ATG" 
NotFinalComma()) {
			Expect(22);
			Attribute(
#line  2919 "VBNET.ATG" 
out attribute);

#line  2919 "VBNET.ATG" 
			attributes.Add(attribute); 
		}
		if (la.kind == 22) {
			lexer.NextToken();
		}
		Expect(39);

#line  2923 "VBNET.ATG" 
		section = new AttributeSection {
		AttributeTarget = attributeTarget,
		Attributes = attributes,
		StartLocation = startLocation,
		EndLocation = t.EndLocation
		};
		
	}

	void TypeModifier(
#line  3702 "VBNET.ATG" 
ModifierList m) {
		switch (la.kind) {
		case 188: {
			lexer.NextToken();

#line  3703 "VBNET.ATG" 
			m.Add(Modifiers.Public, t.Location); 
			break;
		}
		case 187: {
			lexer.NextToken();

#line  3704 "VBNET.ATG" 
			m.Add(Modifiers.Protected, t.Location); 
			break;
		}
		case 125: {
			lexer.NextToken();

#line  3705 "VBNET.ATG" 
			m.Add(Modifiers.Internal, t.Location); 
			break;
		}
		case 185: {
			lexer.NextToken();

#line  3706 "VBNET.ATG" 
			m.Add(Modifiers.Private, t.Location); 
			break;
		}
		case 200: {
			lexer.NextToken();

#line  3707 "VBNET.ATG" 
			m.Add(Modifiers.Static, t.Location); 
			break;
		}
		case 199: {
			lexer.NextToken();

#line  3708 "VBNET.ATG" 
			m.Add(Modifiers.New, t.Location); 
			break;
		}
		case 156: {
			lexer.NextToken();

#line  3709 "VBNET.ATG" 
			m.Add(Modifiers.Abstract, t.Location); 
			break;
		}
		case 166: {
			lexer.NextToken();

#line  3710 "VBNET.ATG" 
			m.Add(Modifiers.Sealed, t.Location); 
			break;
		}
		case 183: {
			lexer.NextToken();

#line  3711 "VBNET.ATG" 
			m.Add(Modifiers.Partial, t.Location); 
			break;
		}
		default: SynErr(249); break;
		}
	}

	void NonModuleDeclaration(
#line  455 "VBNET.ATG" 
ModifierList m, List<AttributeSection> attributes) {

#line  457 "VBNET.ATG" 
		TypeReference typeRef = null;
		List<TypeReference> baseInterfaces = null;
		
		switch (la.kind) {
		case 84: {

#line  460 "VBNET.ATG" 
			m.Check(Modifiers.Classes); 
			lexer.NextToken();

#line  463 "VBNET.ATG" 
			TypeDeclaration newType = new TypeDeclaration(m.Modifier, attributes);
			newType.StartLocation = t.Location;
			AddChild(newType);
			BlockStart(newType);
			
			newType.Type       = ClassType.Class;
			
			Identifier();

#line  470 "VBNET.ATG" 
			newType.Name = t.val; 
			TypeParameterList(
#line  471 "VBNET.ATG" 
newType.Templates);
			EndOfStmt();

#line  473 "VBNET.ATG" 
			newType.BodyStartLocation = t.Location; 
			if (la.kind == 140) {
				ClassBaseType(
#line  474 "VBNET.ATG" 
out typeRef);

#line  474 "VBNET.ATG" 
				SafeAdd(newType, newType.BaseTypes, typeRef); 
			}
			while (la.kind == 136) {
				TypeImplementsClause(
#line  475 "VBNET.ATG" 
out baseInterfaces);

#line  475 "VBNET.ATG" 
				newType.BaseTypes.AddRange(baseInterfaces); 
			}
			ClassBody(
#line  476 "VBNET.ATG" 
newType);
			Expect(113);
			Expect(84);

#line  477 "VBNET.ATG" 
			newType.EndLocation = t.EndLocation; 
			EndOfStmt();

#line  480 "VBNET.ATG" 
			BlockEnd();
			
			break;
		}
		case 155: {
			lexer.NextToken();

#line  484 "VBNET.ATG" 
			m.Check(Modifiers.VBModules);
			TypeDeclaration newType = new TypeDeclaration(m.Modifier, attributes);
			AddChild(newType);
			BlockStart(newType);
			newType.StartLocation = m.GetDeclarationLocation(t.Location);
			newType.Type = ClassType.Module;
			
			Identifier();

#line  491 "VBNET.ATG" 
			newType.Name = t.val; 
			EndOfStmt();

#line  493 "VBNET.ATG" 
			newType.BodyStartLocation = t.Location; 
			ModuleBody(
#line  494 "VBNET.ATG" 
newType);

#line  496 "VBNET.ATG" 
			BlockEnd();
			
			break;
		}
		case 209: {
			lexer.NextToken();

#line  500 "VBNET.ATG" 
			m.Check(Modifiers.VBStructures);
			TypeDeclaration newType = new TypeDeclaration(m.Modifier, attributes);
			AddChild(newType);
			BlockStart(newType);
			newType.StartLocation = m.GetDeclarationLocation(t.Location);
			newType.Type = ClassType.Struct;
			
			Identifier();

#line  507 "VBNET.ATG" 
			newType.Name = t.val; 
			TypeParameterList(
#line  508 "VBNET.ATG" 
newType.Templates);
			EndOfStmt();

#line  510 "VBNET.ATG" 
			newType.BodyStartLocation = t.Location; 
			while (la.kind == 136) {
				TypeImplementsClause(
#line  511 "VBNET.ATG" 
out baseInterfaces);

#line  511 "VBNET.ATG" 
				newType.BaseTypes.AddRange(baseInterfaces);
			}
			StructureBody(
#line  512 "VBNET.ATG" 
newType);

#line  514 "VBNET.ATG" 
			BlockEnd();
			
			break;
		}
		case 115: {
			lexer.NextToken();

#line  519 "VBNET.ATG" 
			m.Check(Modifiers.VBEnums);
			TypeDeclaration newType = new TypeDeclaration(m.Modifier, attributes);
			newType.StartLocation = m.GetDeclarationLocation(t.Location);
			AddChild(newType);
			BlockStart(newType);
			
			newType.Type = ClassType.Enum;
			
			Identifier();

#line  527 "VBNET.ATG" 
			newType.Name = t.val; 
			if (la.kind == 63) {
				lexer.NextToken();
				NonArrayTypeName(
#line  528 "VBNET.ATG" 
out typeRef, false);

#line  528 "VBNET.ATG" 
				SafeAdd(newType, newType.BaseTypes, typeRef); 
			}
			EndOfStmt();

#line  530 "VBNET.ATG" 
			newType.BodyStartLocation = t.Location; 
			EnumBody(
#line  531 "VBNET.ATG" 
newType);

#line  533 "VBNET.ATG" 
			BlockEnd();
			
			break;
		}
		case 142: {
			lexer.NextToken();

#line  538 "VBNET.ATG" 
			m.Check(Modifiers.VBInterfacs);
			TypeDeclaration newType = new TypeDeclaration(m.Modifier, attributes);
			newType.StartLocation = m.GetDeclarationLocation(t.Location);
			AddChild(newType);
			BlockStart(newType);
			newType.Type = ClassType.Interface;
			
			Identifier();

#line  545 "VBNET.ATG" 
			newType.Name = t.val; 
			TypeParameterList(
#line  546 "VBNET.ATG" 
newType.Templates);
			EndOfStmt();

#line  548 "VBNET.ATG" 
			newType.BodyStartLocation = t.Location; 
			while (la.kind == 140) {
				InterfaceBase(
#line  549 "VBNET.ATG" 
out baseInterfaces);

#line  549 "VBNET.ATG" 
				newType.BaseTypes.AddRange(baseInterfaces); 
			}
			InterfaceBody(
#line  550 "VBNET.ATG" 
newType);

#line  552 "VBNET.ATG" 
			BlockEnd();
			
			break;
		}
		case 103: {
			lexer.NextToken();

#line  557 "VBNET.ATG" 
			m.Check(Modifiers.VBDelegates);
			DelegateDeclaration delegateDeclr = new DelegateDeclaration(m.Modifier, attributes);
			delegateDeclr.ReturnType = new TypeReference("System.Void", true);
			delegateDeclr.StartLocation = m.GetDeclarationLocation(t.Location);
			List<ParameterDeclarationExpression> p = new List<ParameterDeclarationExpression>();
			
			if (la.kind == 210) {
				lexer.NextToken();
				Identifier();

#line  564 "VBNET.ATG" 
				delegateDeclr.Name = t.val; 
				TypeParameterList(
#line  565 "VBNET.ATG" 
delegateDeclr.Templates);
				if (la.kind == 37) {
					lexer.NextToken();
					if (StartOf(6)) {
						FormalParameterList(
#line  566 "VBNET.ATG" 
p);
					}
					Expect(38);

#line  566 "VBNET.ATG" 
					delegateDeclr.Parameters = p; 
				}
			} else if (la.kind == 127) {
				lexer.NextToken();
				Identifier();

#line  568 "VBNET.ATG" 
				delegateDeclr.Name = t.val; 
				TypeParameterList(
#line  569 "VBNET.ATG" 
delegateDeclr.Templates);
				if (la.kind == 37) {
					lexer.NextToken();
					if (StartOf(6)) {
						FormalParameterList(
#line  570 "VBNET.ATG" 
p);
					}
					Expect(38);

#line  570 "VBNET.ATG" 
					delegateDeclr.Parameters = p; 
				}
				if (la.kind == 63) {
					lexer.NextToken();

#line  571 "VBNET.ATG" 
					TypeReference type; 
					TypeName(
#line  571 "VBNET.ATG" 
out type);

#line  571 "VBNET.ATG" 
					delegateDeclr.ReturnType = type; 
				}
			} else SynErr(250);

#line  573 "VBNET.ATG" 
			delegateDeclr.EndLocation = t.EndLocation; 
			EndOfStmt();

#line  576 "VBNET.ATG" 
			AddChild(delegateDeclr);
			
			break;
		}
		default: SynErr(251); break;
		}
	}

	void TypeParameterList(
#line  389 "VBNET.ATG" 
List<TemplateDefinition> templates) {

#line  391 "VBNET.ATG" 
		TemplateDefinition template;
		
		if (
#line  395 "VBNET.ATG" 
la.kind == Tokens.OpenParenthesis && Peek(1).kind == Tokens.Of) {
			lexer.NextToken();
			Expect(169);
			TypeParameter(
#line  396 "VBNET.ATG" 
out template);

#line  398 "VBNET.ATG" 
			if (template != null) templates.Add(template);
			
			while (la.kind == 22) {
				lexer.NextToken();
				TypeParameter(
#line  401 "VBNET.ATG" 
out template);

#line  403 "VBNET.ATG" 
				if (template != null) templates.Add(template);
				
			}
			Expect(38);
		}
	}

	void TypeParameter(
#line  411 "VBNET.ATG" 
out TemplateDefinition template) {

#line  412 "VBNET.ATG" 
		VarianceModifier modifier = VarianceModifier.Invariant; Location startLocation = la.Location; 
		if (la.kind == 138 || la.kind == 178) {
			if (la.kind == 138) {
				lexer.NextToken();

#line  415 "VBNET.ATG" 
				modifier = VarianceModifier.Contravariant; 
			} else {
				lexer.NextToken();

#line  415 "VBNET.ATG" 
				modifier = VarianceModifier.Covariant; 
			}
		}
		Identifier();

#line  415 "VBNET.ATG" 
		template = new TemplateDefinition(t.val, null) { VarianceModifier = modifier }; 
		if (la.kind == 63) {
			TypeParameterConstraints(
#line  416 "VBNET.ATG" 
template);
		}

#line  419 "VBNET.ATG" 
		if (template != null) {
		template.StartLocation = startLocation;
		template.EndLocation = t.EndLocation;
		}
		
	}

	void TypeParameterConstraints(
#line  427 "VBNET.ATG" 
TemplateDefinition template) {

#line  429 "VBNET.ATG" 
		TypeReference constraint;
		
		Expect(63);
		if (la.kind == 35) {
			lexer.NextToken();
			TypeParameterConstraint(
#line  435 "VBNET.ATG" 
out constraint);

#line  435 "VBNET.ATG" 
			if (constraint != null) { template.Bases.Add(constraint); } 
			while (la.kind == 22) {
				lexer.NextToken();
				TypeParameterConstraint(
#line  438 "VBNET.ATG" 
out constraint);

#line  438 "VBNET.ATG" 
				if (constraint != null) { template.Bases.Add(constraint); } 
			}
			Expect(36);
		} else if (StartOf(7)) {
			TypeParameterConstraint(
#line  441 "VBNET.ATG" 
out constraint);

#line  441 "VBNET.ATG" 
			if (constraint != null) { template.Bases.Add(constraint); } 
		} else SynErr(252);
	}

	void TypeParameterConstraint(
#line  445 "VBNET.ATG" 
out TypeReference constraint) {

#line  446 "VBNET.ATG" 
		constraint = null; Location startLocation = la.Location; 
		if (la.kind == 84) {
			lexer.NextToken();

#line  448 "VBNET.ATG" 
			constraint = TypeReference.ClassConstraint; 
		} else if (la.kind == 209) {
			lexer.NextToken();

#line  449 "VBNET.ATG" 
			constraint = TypeReference.StructConstraint; 
		} else if (la.kind == 162) {
			lexer.NextToken();

#line  450 "VBNET.ATG" 
			constraint = TypeReference.NewConstraint; 
		} else if (StartOf(8)) {
			TypeName(
#line  451 "VBNET.ATG" 
out constraint);
		} else SynErr(253);
	}

	void ClassBaseType(
#line  797 "VBNET.ATG" 
out TypeReference typeRef) {

#line  799 "VBNET.ATG" 
		typeRef = null;
		
		Expect(140);
		TypeName(
#line  802 "VBNET.ATG" 
out typeRef);
		EndOfStmt();
	}

	void TypeImplementsClause(
#line  1617 "VBNET.ATG" 
out List<TypeReference> baseInterfaces) {

#line  1619 "VBNET.ATG" 
		baseInterfaces = new List<TypeReference>();
		TypeReference type = null;
		
		Expect(136);
		TypeName(
#line  1622 "VBNET.ATG" 
out type);

#line  1624 "VBNET.ATG" 
		if (type != null) baseInterfaces.Add(type);
		
		while (la.kind == 22) {
			lexer.NextToken();
			TypeName(
#line  1627 "VBNET.ATG" 
out type);

#line  1628 "VBNET.ATG" 
			if (type != null) baseInterfaces.Add(type); 
		}
		EndOfStmt();
	}

	void ClassBody(
#line  590 "VBNET.ATG" 
TypeDeclaration newType) {

#line  591 "VBNET.ATG" 
		AttributeSection section; 
		while (la.kind == 1 || la.kind == 21) {
			EndOfStmt();
		}
		while (StartOf(9)) {

#line  594 "VBNET.ATG" 
			List<AttributeSection> attributes = new List<AttributeSection>();
			ModifierList m = new ModifierList();
			
			while (la.kind == 40) {
				AttributeSection(
#line  597 "VBNET.ATG" 
out section);

#line  597 "VBNET.ATG" 
				attributes.Add(section); 
			}
			while (StartOf(10)) {
				MemberModifier(
#line  598 "VBNET.ATG" 
m);
			}
			ClassMemberDecl(
#line  599 "VBNET.ATG" 
m, attributes);
			while (la.kind == 1 || la.kind == 21) {
				EndOfStmt();
			}
		}
	}

	void ModuleBody(
#line  621 "VBNET.ATG" 
TypeDeclaration newType) {

#line  622 "VBNET.ATG" 
		AttributeSection section; 
		while (la.kind == 1 || la.kind == 21) {
			EndOfStmt();
		}
		while (StartOf(9)) {

#line  625 "VBNET.ATG" 
			List<AttributeSection> attributes = new List<AttributeSection>();
			ModifierList m = new ModifierList();
			
			while (la.kind == 40) {
				AttributeSection(
#line  628 "VBNET.ATG" 
out section);

#line  628 "VBNET.ATG" 
				attributes.Add(section); 
			}
			while (StartOf(10)) {
				MemberModifier(
#line  629 "VBNET.ATG" 
m);
			}
			ClassMemberDecl(
#line  630 "VBNET.ATG" 
m, attributes);
			while (la.kind == 1 || la.kind == 21) {
				EndOfStmt();
			}
		}
		Expect(113);
		Expect(155);

#line  633 "VBNET.ATG" 
		newType.EndLocation = t.EndLocation; 
		EndOfStmt();
	}

	void StructureBody(
#line  604 "VBNET.ATG" 
TypeDeclaration newType) {

#line  605 "VBNET.ATG" 
		AttributeSection section; 
		while (la.kind == 1 || la.kind == 21) {
			EndOfStmt();
		}
		while (StartOf(9)) {

#line  608 "VBNET.ATG" 
			List<AttributeSection> attributes = new List<AttributeSection>();
			ModifierList m = new ModifierList();
			
			while (la.kind == 40) {
				AttributeSection(
#line  611 "VBNET.ATG" 
out section);

#line  611 "VBNET.ATG" 
				attributes.Add(section); 
			}
			while (StartOf(10)) {
				MemberModifier(
#line  612 "VBNET.ATG" 
m);
			}
			StructureMemberDecl(
#line  613 "VBNET.ATG" 
m, attributes);
			while (la.kind == 1 || la.kind == 21) {
				EndOfStmt();
			}
		}
		Expect(113);
		Expect(209);

#line  616 "VBNET.ATG" 
		newType.EndLocation = t.EndLocation; 
		EndOfStmt();
	}

	void NonArrayTypeName(
#line  2716 "VBNET.ATG" 
out TypeReference typeref, bool canBeUnbound) {

#line  2718 "VBNET.ATG" 
		string name;
		typeref = null;
		bool isGlobal = false;
		
		if (StartOf(11)) {
			if (la.kind == 130) {
				lexer.NextToken();
				Expect(26);

#line  2723 "VBNET.ATG" 
				isGlobal = true; 
			}
			QualIdentAndTypeArguments(
#line  2724 "VBNET.ATG" 
out typeref, canBeUnbound);

#line  2725 "VBNET.ATG" 
			typeref.IsGlobal = isGlobal; 
			while (la.kind == 26) {
				lexer.NextToken();

#line  2726 "VBNET.ATG" 
				TypeReference nestedTypeRef; 
				QualIdentAndTypeArguments(
#line  2727 "VBNET.ATG" 
out nestedTypeRef, canBeUnbound);

#line  2728 "VBNET.ATG" 
				typeref = new InnerClassTypeReference(typeref, nestedTypeRef.Type, nestedTypeRef.GenericTypes); 
			}
		} else if (la.kind == 168) {
			lexer.NextToken();

#line  2731 "VBNET.ATG" 
			typeref = new TypeReference("System.Object", true); 
			if (la.kind == 33) {
				lexer.NextToken();

#line  2735 "VBNET.ATG" 
				List<TypeReference> typeArguments = new List<TypeReference>(1);
				if (typeref != null) typeArguments.Add(typeref);
				typeref = new TypeReference("System.Nullable", typeArguments) { IsKeyword = true };
				
			}
		} else if (StartOf(12)) {
			PrimitiveTypeName(
#line  2741 "VBNET.ATG" 
out name);

#line  2741 "VBNET.ATG" 
			typeref = new TypeReference(name, true); 
			if (la.kind == 33) {
				lexer.NextToken();

#line  2745 "VBNET.ATG" 
				List<TypeReference> typeArguments = new List<TypeReference>(1);
				if (typeref != null) typeArguments.Add(typeref);
				typeref = new TypeReference("System.Nullable", typeArguments) { IsKeyword = true };
				
			}
		} else SynErr(254);
	}

	void EnumBody(
#line  637 "VBNET.ATG" 
TypeDeclaration newType) {

#line  638 "VBNET.ATG" 
		FieldDeclaration f; 
		while (la.kind == 1 || la.kind == 21) {
			EndOfStmt();
		}
		while (StartOf(13)) {
			EnumMemberDecl(
#line  641 "VBNET.ATG" 
out f);

#line  643 "VBNET.ATG" 
			AddChild(f);
			
			while (la.kind == 1 || la.kind == 21) {
				EndOfStmt();
			}
		}
		Expect(113);
		Expect(115);

#line  647 "VBNET.ATG" 
		newType.EndLocation = t.EndLocation; 
		EndOfStmt();
	}

	void InterfaceBase(
#line  1602 "VBNET.ATG" 
out List<TypeReference> bases) {

#line  1604 "VBNET.ATG" 
		TypeReference type;
		bases = new List<TypeReference>();
		
		Expect(140);
		TypeName(
#line  1608 "VBNET.ATG" 
out type);

#line  1608 "VBNET.ATG" 
		if (type != null) bases.Add(type); 
		while (la.kind == 22) {
			lexer.NextToken();
			TypeName(
#line  1611 "VBNET.ATG" 
out type);

#line  1611 "VBNET.ATG" 
			if (type != null) bases.Add(type); 
		}
		EndOfStmt();
	}

	void InterfaceBody(
#line  651 "VBNET.ATG" 
TypeDeclaration newType) {
		while (la.kind == 1 || la.kind == 21) {
			EndOfStmt();
		}
		while (StartOf(14)) {
			InterfaceMemberDecl();
			while (la.kind == 1 || la.kind == 21) {
				EndOfStmt();
			}
		}
		Expect(113);
		Expect(142);

#line  657 "VBNET.ATG" 
		newType.EndLocation = t.EndLocation; 
		EndOfStmt();
	}

	void FormalParameterList(
#line  2933 "VBNET.ATG" 
List<ParameterDeclarationExpression> parameter) {

#line  2934 "VBNET.ATG" 
		ParameterDeclarationExpression p; 
		FormalParameter(
#line  2936 "VBNET.ATG" 
out p);

#line  2936 "VBNET.ATG" 
		if (p != null) parameter.Add(p); 
		while (la.kind == 22) {
			lexer.NextToken();
			FormalParameter(
#line  2938 "VBNET.ATG" 
out p);

#line  2938 "VBNET.ATG" 
			if (p != null) parameter.Add(p); 
		}
	}

	void MemberModifier(
#line  3714 "VBNET.ATG" 
ModifierList m) {
		switch (la.kind) {
		case 156: {
			lexer.NextToken();

#line  3715 "VBNET.ATG" 
			m.Add(Modifiers.Abstract, t.Location);
			break;
		}
		case 102: {
			lexer.NextToken();

#line  3716 "VBNET.ATG" 
			m.Add(Modifiers.Default, t.Location);
			break;
		}
		case 125: {
			lexer.NextToken();

#line  3717 "VBNET.ATG" 
			m.Add(Modifiers.Internal, t.Location);
			break;
		}
		case 199: {
			lexer.NextToken();

#line  3718 "VBNET.ATG" 
			m.Add(Modifiers.New, t.Location);
			break;
		}
		case 181: {
			lexer.NextToken();

#line  3719 "VBNET.ATG" 
			m.Add(Modifiers.Override, t.Location);
			break;
		}
		case 157: {
			lexer.NextToken();

#line  3720 "VBNET.ATG" 
			m.Add(Modifiers.Abstract, t.Location);
			break;
		}
		case 185: {
			lexer.NextToken();

#line  3721 "VBNET.ATG" 
			m.Add(Modifiers.Private, t.Location);
			break;
		}
		case 187: {
			lexer.NextToken();

#line  3722 "VBNET.ATG" 
			m.Add(Modifiers.Protected, t.Location);
			break;
		}
		case 188: {
			lexer.NextToken();

#line  3723 "VBNET.ATG" 
			m.Add(Modifiers.Public, t.Location);
			break;
		}
		case 166: {
			lexer.NextToken();

#line  3724 "VBNET.ATG" 
			m.Add(Modifiers.Sealed, t.Location);
			break;
		}
		case 167: {
			lexer.NextToken();

#line  3725 "VBNET.ATG" 
			m.Add(Modifiers.Sealed, t.Location);
			break;
		}
		case 200: {
			lexer.NextToken();

#line  3726 "VBNET.ATG" 
			m.Add(Modifiers.Static, t.Location);
			break;
		}
		case 180: {
			lexer.NextToken();

#line  3727 "VBNET.ATG" 
			m.Add(Modifiers.Virtual, t.Location);
			break;
		}
		case 179: {
			lexer.NextToken();

#line  3728 "VBNET.ATG" 
			m.Add(Modifiers.Overloads, t.Location);
			break;
		}
		case 190: {
			lexer.NextToken();

#line  3729 "VBNET.ATG" 
			m.Add(Modifiers.ReadOnly, t.Location);
			break;
		}
		case 235: {
			lexer.NextToken();

#line  3730 "VBNET.ATG" 
			m.Add(Modifiers.WriteOnly, t.Location);
			break;
		}
		case 234: {
			lexer.NextToken();

#line  3731 "VBNET.ATG" 
			m.Add(Modifiers.WithEvents, t.Location);
			break;
		}
		case 105: {
			lexer.NextToken();

#line  3732 "VBNET.ATG" 
			m.Add(Modifiers.Dim, t.Location);
			break;
		}
		case 183: {
			lexer.NextToken();

#line  3733 "VBNET.ATG" 
			m.Add(Modifiers.Partial, t.Location);
			break;
		}
		default: SynErr(255); break;
		}
	}

	void ClassMemberDecl(
#line  793 "VBNET.ATG" 
ModifierList m, List<AttributeSection> attributes) {
		StructureMemberDecl(
#line  794 "VBNET.ATG" 
m, attributes);
	}

	void StructureMemberDecl(
#line  807 "VBNET.ATG" 
ModifierList m, List<AttributeSection> attributes) {

#line  809 "VBNET.ATG" 
		TypeReference type = null;
		List<ParameterDeclarationExpression> p = new List<ParameterDeclarationExpression>();
		Statement stmt = null;
		List<VariableDeclaration> variableDeclarators = new List<VariableDeclaration>();
		List<TemplateDefinition> templates = new List<TemplateDefinition>();
		
		switch (la.kind) {
		case 84: case 103: case 115: case 142: case 155: case 209: {
			NonModuleDeclaration(
#line  816 "VBNET.ATG" 
m, attributes);
			break;
		}
		case 210: {
			lexer.NextToken();

#line  820 "VBNET.ATG" 
			Location startPos = t.Location;
			
			if (StartOf(4)) {

#line  824 "VBNET.ATG" 
				string name = String.Empty;
				MethodDeclaration methodDeclaration; List<string> handlesClause = null;
				List<InterfaceImplementation> implementsClause = null;
				
				Identifier();

#line  830 "VBNET.ATG" 
				name = t.val;
				m.Check(Modifiers.VBMethods);
				
				TypeParameterList(
#line  833 "VBNET.ATG" 
templates);
				if (la.kind == 37) {
					lexer.NextToken();
					if (StartOf(6)) {
						FormalParameterList(
#line  834 "VBNET.ATG" 
p);
					}
					Expect(38);
				}
				if (la.kind == 134 || la.kind == 136) {
					if (la.kind == 136) {
						ImplementsClause(
#line  837 "VBNET.ATG" 
out implementsClause);
					} else {
						HandlesClause(
#line  839 "VBNET.ATG" 
out handlesClause);
					}
				}

#line  842 "VBNET.ATG" 
				Location endLocation = t.EndLocation; 
				if (
#line  845 "VBNET.ATG" 
IsMustOverride(m)) {
					EndOfStmt();

#line  848 "VBNET.ATG" 
					methodDeclaration = new MethodDeclaration {
					Name = name, Modifier = m.Modifier, Parameters = p, Attributes = attributes,
					StartLocation = m.GetDeclarationLocation(startPos), EndLocation = endLocation,
					TypeReference = new TypeReference("System.Void", true),
					Templates = templates,
					HandlesClause = handlesClause,
					InterfaceImplementations = implementsClause
					};
					AddChild(methodDeclaration);
					
				} else if (la.kind == 1) {
					lexer.NextToken();

#line  861 "VBNET.ATG" 
					methodDeclaration = new MethodDeclaration {
					Name = name, Modifier = m.Modifier, Parameters = p, Attributes = attributes,
					StartLocation = m.GetDeclarationLocation(startPos), EndLocation = endLocation,
					TypeReference = new TypeReference("System.Void", true),
					Templates = templates,
					HandlesClause = handlesClause,
					InterfaceImplementations = implementsClause
					};
					AddChild(methodDeclaration);
					

#line  872 "VBNET.ATG" 
					if (ParseMethodBodies) { 
					Block(
#line  873 "VBNET.ATG" 
out stmt);
					Expect(113);
					Expect(210);

#line  875 "VBNET.ATG" 
					} else {
					// don't parse method body
					lexer.SkipCurrentBlock(Tokens.Sub); stmt = new BlockStatement();
					  }
					

#line  881 "VBNET.ATG" 
					methodDeclaration.Body  = (BlockStatement)stmt; 

#line  882 "VBNET.ATG" 
					methodDeclaration.Body.EndLocation = t.EndLocation; 
					EndOfStmt();
				} else SynErr(256);
			} else if (la.kind == 162) {
				lexer.NextToken();
				if (la.kind == 37) {
					lexer.NextToken();
					if (StartOf(6)) {
						FormalParameterList(
#line  886 "VBNET.ATG" 
p);
					}
					Expect(38);
				}

#line  887 "VBNET.ATG" 
				m.Check(Modifiers.Constructors); 

#line  888 "VBNET.ATG" 
				Location constructorEndLocation = t.EndLocation; 
				Expect(1);

#line  891 "VBNET.ATG" 
				if (ParseMethodBodies) { 
				Block(
#line  892 "VBNET.ATG" 
out stmt);
				Expect(113);
				Expect(210);

#line  894 "VBNET.ATG" 
				} else {
				// don't parse method body
				lexer.SkipCurrentBlock(Tokens.Sub); stmt = new BlockStatement();
				  }
				

#line  900 "VBNET.ATG" 
				Location endLocation = t.EndLocation; 
				EndOfStmt();

#line  903 "VBNET.ATG" 
				ConstructorDeclaration cd = new ConstructorDeclaration("New", m.Modifier, p, attributes);
				cd.StartLocation = m.GetDeclarationLocation(startPos);
				cd.EndLocation   = constructorEndLocation;
				cd.Body = (BlockStatement)stmt;
				cd.Body.EndLocation   = endLocation;
				AddChild(cd);
				
			} else SynErr(257);
			break;
		}
		case 127: {
			lexer.NextToken();

#line  915 "VBNET.ATG" 
			m.Check(Modifiers.VBMethods);
			string name = String.Empty;
			Location startPos = t.Location;
			MethodDeclaration methodDeclaration;List<string> handlesClause = null;
			List<InterfaceImplementation> implementsClause = null;
			AttributeSection returnTypeAttributeSection = null;
			
			Identifier();

#line  922 "VBNET.ATG" 
			name = t.val; 
			TypeParameterList(
#line  923 "VBNET.ATG" 
templates);
			if (la.kind == 37) {
				lexer.NextToken();
				if (StartOf(6)) {
					FormalParameterList(
#line  924 "VBNET.ATG" 
p);
				}
				Expect(38);
			}
			if (la.kind == 63) {
				lexer.NextToken();
				while (la.kind == 40) {
					AttributeSection(
#line  926 "VBNET.ATG" 
out returnTypeAttributeSection);

#line  928 "VBNET.ATG" 
					if (returnTypeAttributeSection != null) {
					returnTypeAttributeSection.AttributeTarget = "return";
					attributes.Add(returnTypeAttributeSection);
					}
					
				}
				TypeName(
#line  934 "VBNET.ATG" 
out type);
			}

#line  936 "VBNET.ATG" 
			if(type == null) {
			type = new TypeReference("System.Object", true);
			}
			
			if (la.kind == 134 || la.kind == 136) {
				if (la.kind == 136) {
					ImplementsClause(
#line  942 "VBNET.ATG" 
out implementsClause);
				} else {
					HandlesClause(
#line  944 "VBNET.ATG" 
out handlesClause);
				}
			}

#line  947 "VBNET.ATG" 
			Location endLocation = t.EndLocation; 
			if (
#line  950 "VBNET.ATG" 
IsMustOverride(m)) {
				EndOfStmt();

#line  953 "VBNET.ATG" 
				methodDeclaration = new MethodDeclaration {
				Name = name, Modifier = m.Modifier, TypeReference = type,
				Parameters = p, Attributes = attributes,
				StartLocation = m.GetDeclarationLocation(startPos),
				EndLocation   = endLocation,
				HandlesClause = handlesClause,
				Templates     = templates,
				InterfaceImplementations = implementsClause
				};
				
				AddChild(methodDeclaration);
				
			} else if (la.kind == 1) {
				lexer.NextToken();

#line  968 "VBNET.ATG" 
				methodDeclaration = new MethodDeclaration {
				Name = name, Modifier = m.Modifier, TypeReference = type,
				Parameters = p, Attributes = attributes,
				StartLocation = m.GetDeclarationLocation(startPos),
				EndLocation   = endLocation,
				Templates     = templates,
				HandlesClause = handlesClause,
				InterfaceImplementations = implementsClause
				};
				
				AddChild(methodDeclaration);
				
				if (ParseMethodBodies) { 
				Block(
#line  981 "VBNET.ATG" 
out stmt);
				Expect(113);
				Expect(127);

#line  983 "VBNET.ATG" 
				} else {
				// don't parse method body
				lexer.SkipCurrentBlock(Tokens.Function); stmt = new BlockStatement();
				}
				methodDeclaration.Body = (BlockStatement)stmt;
				methodDeclaration.Body.StartLocation = methodDeclaration.EndLocation;
				methodDeclaration.Body.EndLocation   = t.EndLocation;
				
				EndOfStmt();
			} else SynErr(258);
			break;
		}
		case 101: {
			lexer.NextToken();

#line  997 "VBNET.ATG" 
			m.Check(Modifiers.VBExternalMethods);
			Location startPos = t.Location;
			CharsetModifier charsetModifer = CharsetModifier.None;
			string library = String.Empty;
			string alias = null;
			string name = String.Empty;
			
			if (StartOf(15)) {
				Charset(
#line  1004 "VBNET.ATG" 
out charsetModifer);
			}
			if (la.kind == 210) {
				lexer.NextToken();
				Identifier();

#line  1007 "VBNET.ATG" 
				name = t.val; 
				Expect(149);
				Expect(3);

#line  1008 "VBNET.ATG" 
				library = t.literalValue as string; 
				if (la.kind == 59) {
					lexer.NextToken();
					Expect(3);

#line  1009 "VBNET.ATG" 
					alias = t.literalValue as string; 
				}
				if (la.kind == 37) {
					lexer.NextToken();
					if (StartOf(6)) {
						FormalParameterList(
#line  1010 "VBNET.ATG" 
p);
					}
					Expect(38);
				}
				EndOfStmt();

#line  1013 "VBNET.ATG" 
				DeclareDeclaration declareDeclaration = new DeclareDeclaration(name, m.Modifier, null, p, attributes, library, alias, charsetModifer);
				declareDeclaration.StartLocation = m.GetDeclarationLocation(startPos);
				declareDeclaration.EndLocation   = t.EndLocation;
				AddChild(declareDeclaration);
				
			} else if (la.kind == 127) {
				lexer.NextToken();
				Identifier();

#line  1020 "VBNET.ATG" 
				name = t.val; 
				Expect(149);
				Expect(3);

#line  1021 "VBNET.ATG" 
				library = t.literalValue as string; 
				if (la.kind == 59) {
					lexer.NextToken();
					Expect(3);

#line  1022 "VBNET.ATG" 
					alias = t.literalValue as string; 
				}
				if (la.kind == 37) {
					lexer.NextToken();
					if (StartOf(6)) {
						FormalParameterList(
#line  1023 "VBNET.ATG" 
p);
					}
					Expect(38);
				}
				if (la.kind == 63) {
					lexer.NextToken();
					TypeName(
#line  1024 "VBNET.ATG" 
out type);
				}
				EndOfStmt();

#line  1027 "VBNET.ATG" 
				DeclareDeclaration declareDeclaration = new DeclareDeclaration(name, m.Modifier, type, p, attributes, library, alias, charsetModifer);
				declareDeclaration.StartLocation = m.GetDeclarationLocation(startPos);
				declareDeclaration.EndLocation   = t.EndLocation;
				AddChild(declareDeclaration);
				
			} else SynErr(259);
			break;
		}
		case 119: {
			lexer.NextToken();

#line  1037 "VBNET.ATG" 
			m.Check(Modifiers.VBEvents);
			Location startPos = t.Location;
			EventDeclaration eventDeclaration;
			string name = String.Empty;
			List<InterfaceImplementation> implementsClause = null;
			
			Identifier();

#line  1043 "VBNET.ATG" 
			name= t.val; 
			if (la.kind == 63) {
				lexer.NextToken();
				TypeName(
#line  1045 "VBNET.ATG" 
out type);
			} else if (StartOf(16)) {
				if (la.kind == 37) {
					lexer.NextToken();
					if (StartOf(6)) {
						FormalParameterList(
#line  1047 "VBNET.ATG" 
p);
					}
					Expect(38);
				}
			} else SynErr(260);
			if (la.kind == 136) {
				ImplementsClause(
#line  1049 "VBNET.ATG" 
out implementsClause);
			}

#line  1051 "VBNET.ATG" 
			eventDeclaration = new EventDeclaration {
			Name = name, TypeReference = type, Modifier = m.Modifier, 
			Parameters = p, Attributes = attributes, InterfaceImplementations = implementsClause,
			StartLocation = m.GetDeclarationLocation(startPos),
			EndLocation = t.EndLocation
			};
			AddChild(eventDeclaration);
			
			EndOfStmt();
			break;
		}
		case 2: case 58: case 62: case 64: case 65: case 66: case 67: case 70: case 87: case 104: case 107: case 116: case 121: case 126: case 133: case 139: case 143: case 146: case 147: case 170: case 176: case 178: case 184: case 203: case 212: case 213: case 223: case 224: case 230: {

#line  1062 "VBNET.ATG" 
			m.Check(Modifiers.Fields);
			FieldDeclaration fd = new FieldDeclaration(attributes, null, m.Modifier);
			
			IdentifierForFieldDeclaration();

#line  1065 "VBNET.ATG" 
			string name = t.val; 

#line  1066 "VBNET.ATG" 
			fd.StartLocation = m.GetDeclarationLocation(t.Location); 
			VariableDeclaratorPartAfterIdentifier(
#line  1068 "VBNET.ATG" 
variableDeclarators, name);
			while (la.kind == 22) {
				lexer.NextToken();
				VariableDeclarator(
#line  1069 "VBNET.ATG" 
variableDeclarators);
			}
			EndOfStmt();

#line  1072 "VBNET.ATG" 
			fd.EndLocation = t.EndLocation;
			fd.Fields = variableDeclarators;
			AddChild(fd);
			
			break;
		}
		case 88: {

#line  1077 "VBNET.ATG" 
			m.Check(Modifiers.Fields); 
			lexer.NextToken();

#line  1078 "VBNET.ATG" 
			m.Add(Modifiers.Const, t.Location);  

#line  1080 "VBNET.ATG" 
			FieldDeclaration fd = new FieldDeclaration(attributes, type, m.Modifier);
			fd.StartLocation = m.GetDeclarationLocation(t.Location);
			List<VariableDeclaration> constantDeclarators = new List<VariableDeclaration>();
			
			ConstantDeclarator(
#line  1084 "VBNET.ATG" 
constantDeclarators);
			while (la.kind == 22) {
				lexer.NextToken();
				ConstantDeclarator(
#line  1085 "VBNET.ATG" 
constantDeclarators);
			}

#line  1087 "VBNET.ATG" 
			fd.Fields = constantDeclarators;
			fd.EndLocation = t.Location;
			
			EndOfStmt();

#line  1092 "VBNET.ATG" 
			fd.EndLocation = t.EndLocation;
			AddChild(fd);
			
			break;
		}
		case 186: {
			lexer.NextToken();

#line  1098 "VBNET.ATG" 
			m.Check(Modifiers.VBProperties);
			Location startPos = t.Location;
			List<InterfaceImplementation> implementsClause = null;
			AttributeSection returnTypeAttributeSection = null;
			Expression initializer = null;
			
			Identifier();

#line  1104 "VBNET.ATG" 
			string propertyName = t.val; 
			if (la.kind == 37) {
				lexer.NextToken();
				if (StartOf(6)) {
					FormalParameterList(
#line  1105 "VBNET.ATG" 
p);
				}
				Expect(38);
			}
			if (la.kind == 63) {
				lexer.NextToken();
				while (la.kind == 40) {
					AttributeSection(
#line  1108 "VBNET.ATG" 
out returnTypeAttributeSection);

#line  1110 "VBNET.ATG" 
					if (returnTypeAttributeSection != null) {
					returnTypeAttributeSection.AttributeTarget = "return";
					attributes.Add(returnTypeAttributeSection);
					}
					
				}
				if (
#line  1117 "VBNET.ATG" 
IsNewExpression()) {
					ObjectCreateExpression(
#line  1117 "VBNET.ATG" 
out initializer);

#line  1119 "VBNET.ATG" 
					if (initializer is ObjectCreateExpression) {
					type = ((ObjectCreateExpression)initializer).CreateType.Clone();
					} else {
						type = ((ArrayCreateExpression)initializer).CreateType.Clone();
					}
					
				} else if (StartOf(8)) {
					TypeName(
#line  1126 "VBNET.ATG" 
out type);
				} else SynErr(261);
			}
			if (la.kind == 20) {
				lexer.NextToken();
				Expr(
#line  1129 "VBNET.ATG" 
out initializer);
			}
			if (la.kind == 136) {
				ImplementsClause(
#line  1130 "VBNET.ATG" 
out implementsClause);
			}
			EndOfStmt();
			if (
#line  1134 "VBNET.ATG" 
IsMustOverride(m) || IsAutomaticProperty()) {

#line  1136 "VBNET.ATG" 
				PropertyDeclaration pDecl = new PropertyDeclaration(propertyName, type, m.Modifier, attributes);
				pDecl.StartLocation = m.GetDeclarationLocation(startPos);
				pDecl.EndLocation   = t.Location;
				pDecl.TypeReference = type;
				pDecl.InterfaceImplementations = implementsClause;
				pDecl.Parameters = p;
				if (initializer != null)
					pDecl.Initializer = initializer;
				AddChild(pDecl);
				
			} else if (StartOf(17)) {

#line  1148 "VBNET.ATG" 
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
#line  1158 "VBNET.ATG" 
out getRegion, out setRegion);
				Expect(113);
				Expect(186);
				EndOfStmt();

#line  1162 "VBNET.ATG" 
				pDecl.GetRegion = getRegion;
				pDecl.SetRegion = setRegion;
				pDecl.BodyEnd = t.Location; // t = EndOfStmt; not "Property"
				AddChild(pDecl);
				
			} else SynErr(262);
			break;
		}
		case 98: {
			lexer.NextToken();

#line  1169 "VBNET.ATG" 
			Location startPos = t.Location; 
			Expect(119);

#line  1171 "VBNET.ATG" 
			m.Check(Modifiers.VBCustomEvents);
			EventAddRemoveRegion eventAccessorDeclaration;
			EventAddRegion addHandlerAccessorDeclaration = null;
			EventRemoveRegion removeHandlerAccessorDeclaration = null;
			EventRaiseRegion raiseEventAccessorDeclaration = null;
			List<InterfaceImplementation> implementsClause = null;
			
			Identifier();

#line  1178 "VBNET.ATG" 
			string customEventName = t.val; 
			Expect(63);
			TypeName(
#line  1179 "VBNET.ATG" 
out type);
			if (la.kind == 136) {
				ImplementsClause(
#line  1180 "VBNET.ATG" 
out implementsClause);
			}
			EndOfStmt();
			while (StartOf(18)) {
				EventAccessorDeclaration(
#line  1183 "VBNET.ATG" 
out eventAccessorDeclaration);

#line  1185 "VBNET.ATG" 
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
			Expect(113);
			Expect(119);
			EndOfStmt();

#line  1201 "VBNET.ATG" 
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
			AddChild(decl);
			
			break;
		}
		case 161: case 172: case 232: {

#line  1227 "VBNET.ATG" 
			ConversionType opConversionType = ConversionType.None; 
			if (la.kind == 161 || la.kind == 232) {
				if (la.kind == 232) {
					lexer.NextToken();

#line  1228 "VBNET.ATG" 
					opConversionType = ConversionType.Implicit; 
				} else {
					lexer.NextToken();

#line  1229 "VBNET.ATG" 
					opConversionType = ConversionType.Explicit;
				}
			}
			Expect(172);

#line  1232 "VBNET.ATG" 
			m.Check(Modifiers.VBOperators);
			Location startPos = t.Location;
			TypeReference returnType = NullTypeReference.Instance;
			TypeReference operandType = NullTypeReference.Instance;
			OverloadableOperatorType operatorType;
			AttributeSection section;
			ParameterDeclarationExpression param;
			List<ParameterDeclarationExpression> parameters = new List<ParameterDeclarationExpression>();
			
			OverloadableOperator(
#line  1241 "VBNET.ATG" 
out operatorType);
			Expect(37);
			FormalParameter(
#line  1243 "VBNET.ATG" 
out param);

#line  1244 "VBNET.ATG" 
			if (param != null) parameters.Add(param); 
			if (la.kind == 22) {
				lexer.NextToken();
				FormalParameter(
#line  1246 "VBNET.ATG" 
out param);

#line  1247 "VBNET.ATG" 
				if (param != null) parameters.Add(param); 
			}
			Expect(38);

#line  1250 "VBNET.ATG" 
			Location endPos = t.EndLocation; 
			if (la.kind == 63) {
				lexer.NextToken();
				while (la.kind == 40) {
					AttributeSection(
#line  1251 "VBNET.ATG" 
out section);

#line  1252 "VBNET.ATG" 
					if (section != null) {
					section.AttributeTarget = "return";
					attributes.Add(section);
					} 
				}
				TypeName(
#line  1256 "VBNET.ATG" 
out returnType);

#line  1256 "VBNET.ATG" 
				endPos = t.EndLocation; 
			}
			Expect(1);
			Block(
#line  1258 "VBNET.ATG" 
out stmt);
			Expect(113);
			Expect(172);
			EndOfStmt();

#line  1260 "VBNET.ATG" 
			OperatorDeclaration operatorDeclaration = new OperatorDeclaration {
			Modifier = m.Modifier,
			Attributes = attributes,
			Parameters = parameters,
			TypeReference = returnType,
			OverloadableOperator = operatorType,
			ConversionType = opConversionType,
			Body = (BlockStatement)stmt,
			StartLocation = m.GetDeclarationLocation(startPos),
			EndLocation = endPos
			};
			operatorDeclaration.Body.StartLocation = startPos;
			operatorDeclaration.Body.EndLocation = t.Location;
			AddChild(operatorDeclaration);
			
			break;
		}
		default: SynErr(263); break;
		}
	}

	void EnumMemberDecl(
#line  774 "VBNET.ATG" 
out FieldDeclaration f) {

#line  776 "VBNET.ATG" 
		Expression expr = null;List<AttributeSection> attributes = new List<AttributeSection>();
		AttributeSection section = null;
		VariableDeclaration varDecl = null;
		
		while (la.kind == 40) {
			AttributeSection(
#line  780 "VBNET.ATG" 
out section);

#line  780 "VBNET.ATG" 
			attributes.Add(section); 
		}
		Identifier();

#line  783 "VBNET.ATG" 
		f = new FieldDeclaration(attributes);
		varDecl = new VariableDeclaration(t.val);
		f.Fields.Add(varDecl);
		f.StartLocation = varDecl.StartLocation = t.Location;
		
		if (la.kind == 20) {
			lexer.NextToken();
			Expr(
#line  788 "VBNET.ATG" 
out expr);

#line  788 "VBNET.ATG" 
			varDecl.Initializer = expr; 
		}

#line  789 "VBNET.ATG" 
		f.EndLocation = varDecl.EndLocation = t.EndLocation; 
		EndOfStmt();
	}

	void InterfaceMemberDecl() {

#line  665 "VBNET.ATG" 
		TypeReference type =null;
		List<ParameterDeclarationExpression> p = new List<ParameterDeclarationExpression>();
		List<TemplateDefinition> templates = new List<TemplateDefinition>();
		AttributeSection section, returnTypeAttributeSection = null;
		ModifierList mod = new ModifierList();
		List<AttributeSection> attributes = new List<AttributeSection>();
		string name;
		
		if (StartOf(19)) {
			while (la.kind == 40) {
				AttributeSection(
#line  673 "VBNET.ATG" 
out section);

#line  673 "VBNET.ATG" 
				attributes.Add(section); 
			}
			while (StartOf(10)) {
				MemberModifier(
#line  676 "VBNET.ATG" 
mod);
			}
			if (la.kind == 119) {
				lexer.NextToken();

#line  680 "VBNET.ATG" 
				mod.Check(Modifiers.VBInterfaceEvents);
				Location startLocation = t.Location;
				
				Identifier();

#line  683 "VBNET.ATG" 
				name = t.val; 
				if (la.kind == 37) {
					lexer.NextToken();
					if (StartOf(6)) {
						FormalParameterList(
#line  684 "VBNET.ATG" 
p);
					}
					Expect(38);
				}
				if (la.kind == 63) {
					lexer.NextToken();
					TypeName(
#line  685 "VBNET.ATG" 
out type);
				}
				EndOfStmt();

#line  688 "VBNET.ATG" 
				EventDeclaration ed = new EventDeclaration {
				Name = name, TypeReference = type, Modifier = mod.Modifier,
				Parameters = p, Attributes = attributes,
				StartLocation = startLocation, EndLocation = t.EndLocation
				};
				AddChild(ed);
				
			} else if (la.kind == 210) {
				lexer.NextToken();

#line  698 "VBNET.ATG" 
				Location startLocation =  t.Location;
				mod.Check(Modifiers.VBInterfaceMethods);
				
				Identifier();

#line  701 "VBNET.ATG" 
				name = t.val; 
				TypeParameterList(
#line  702 "VBNET.ATG" 
templates);
				if (la.kind == 37) {
					lexer.NextToken();
					if (StartOf(6)) {
						FormalParameterList(
#line  703 "VBNET.ATG" 
p);
					}
					Expect(38);
				}
				EndOfStmt();

#line  706 "VBNET.ATG" 
				MethodDeclaration md = new MethodDeclaration {
				Name = name, 
				Modifier = mod.Modifier, 
				Parameters = p,
				Attributes = attributes,
				TypeReference = new TypeReference("System.Void", true),
				StartLocation = startLocation,
				EndLocation = t.EndLocation,
				Templates = templates
				};
				AddChild(md);
				
			} else if (la.kind == 127) {
				lexer.NextToken();

#line  721 "VBNET.ATG" 
				mod.Check(Modifiers.VBInterfaceMethods);
				Location startLocation = t.Location;
				
				Identifier();

#line  724 "VBNET.ATG" 
				name = t.val; 
				TypeParameterList(
#line  725 "VBNET.ATG" 
templates);
				if (la.kind == 37) {
					lexer.NextToken();
					if (StartOf(6)) {
						FormalParameterList(
#line  726 "VBNET.ATG" 
p);
					}
					Expect(38);
				}
				if (la.kind == 63) {
					lexer.NextToken();
					while (la.kind == 40) {
						AttributeSection(
#line  727 "VBNET.ATG" 
out returnTypeAttributeSection);
					}
					TypeName(
#line  727 "VBNET.ATG" 
out type);
				}

#line  729 "VBNET.ATG" 
				if(type == null) {
				type = new TypeReference("System.Object", true);
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
				AddChild(md);
				
				EndOfStmt();
			} else if (la.kind == 186) {
				lexer.NextToken();

#line  749 "VBNET.ATG" 
				Location startLocation = t.Location;
				mod.Check(Modifiers.VBInterfaceProperties);
				
				Identifier();

#line  752 "VBNET.ATG" 
				name = t.val;  
				if (la.kind == 37) {
					lexer.NextToken();
					if (StartOf(6)) {
						FormalParameterList(
#line  753 "VBNET.ATG" 
p);
					}
					Expect(38);
				}
				if (la.kind == 63) {
					lexer.NextToken();
					TypeName(
#line  754 "VBNET.ATG" 
out type);
				}

#line  756 "VBNET.ATG" 
				if(type == null) {
				type = new TypeReference("System.Object", true);
				}
				
				EndOfStmt();

#line  762 "VBNET.ATG" 
				PropertyDeclaration pd = new PropertyDeclaration(name, type, mod.Modifier, attributes);
				pd.Parameters = p;
				pd.EndLocation = t.EndLocation;
				pd.StartLocation = startLocation;
				AddChild(pd);
				
			} else SynErr(264);
		} else if (StartOf(20)) {
			NonModuleDeclaration(
#line  770 "VBNET.ATG" 
mod, attributes);
		} else SynErr(265);
	}

	void Expr(
#line  1661 "VBNET.ATG" 
out Expression expr) {

#line  1662 "VBNET.ATG" 
		expr = null; Location startLocation = la.Location; 
		if (
#line  1665 "VBNET.ATG" 
IsQueryExpression() ) {
			QueryExpr(
#line  1666 "VBNET.ATG" 
out expr);
		} else if (la.kind == 127 || la.kind == 210) {
			LambdaExpr(
#line  1667 "VBNET.ATG" 
out expr);
		} else if (StartOf(21)) {
			DisjunctionExpr(
#line  1668 "VBNET.ATG" 
out expr);
		} else SynErr(266);

#line  1671 "VBNET.ATG" 
		if (expr != null) {
		expr.StartLocation = startLocation;
		expr.EndLocation = t.EndLocation;
		}
		
	}

	void ImplementsClause(
#line  1634 "VBNET.ATG" 
out List<InterfaceImplementation> baseInterfaces) {

#line  1636 "VBNET.ATG" 
		baseInterfaces = new List<InterfaceImplementation>();
		TypeReference type = null;
		string memberName = null;
		
		Expect(136);
		NonArrayTypeName(
#line  1641 "VBNET.ATG" 
out type, false);

#line  1642 "VBNET.ATG" 
		if (type != null) memberName = TypeReference.StripLastIdentifierFromType(ref type); 

#line  1643 "VBNET.ATG" 
		baseInterfaces.Add(new InterfaceImplementation(type, memberName)); 
		while (la.kind == 22) {
			lexer.NextToken();
			NonArrayTypeName(
#line  1645 "VBNET.ATG" 
out type, false);

#line  1646 "VBNET.ATG" 
			if (type != null) memberName = TypeReference.StripLastIdentifierFromType(ref type); 

#line  1647 "VBNET.ATG" 
			baseInterfaces.Add(new InterfaceImplementation(type, memberName)); 
		}
	}

	void HandlesClause(
#line  1592 "VBNET.ATG" 
out List<string> handlesClause) {

#line  1594 "VBNET.ATG" 
		handlesClause = new List<string>();
		string name;
		
		Expect(134);
		EventMemberSpecifier(
#line  1597 "VBNET.ATG" 
out name);

#line  1597 "VBNET.ATG" 
		if (name != null) handlesClause.Add(name); 
		while (la.kind == 22) {
			lexer.NextToken();
			EventMemberSpecifier(
#line  1598 "VBNET.ATG" 
out name);

#line  1598 "VBNET.ATG" 
			if (name != null) handlesClause.Add(name); 
		}
	}

	void Block(
#line  2981 "VBNET.ATG" 
out Statement stmt) {

#line  2984 "VBNET.ATG" 
		BlockStatement blockStmt = new BlockStatement();
		/* in snippet parsing mode, t might be null */
		if (t != null) blockStmt.StartLocation = t.EndLocation;
		BlockStart(blockStmt);
		
		while (StartOf(22) || 
#line  2990 "VBNET.ATG" 
IsEndStmtAhead()) {
			if (
#line  2990 "VBNET.ATG" 
IsEndStmtAhead()) {

#line  2991 "VBNET.ATG" 
				Token first = la; 
				Expect(113);
				EndOfStmt();

#line  2994 "VBNET.ATG" 
				AddChild(new EndStatement() {
				StartLocation = first.Location,
				EndLocation = first.EndLocation }
				);
				
			} else {
				Statement();
				EndOfStmt();
			}
		}

#line  3003 "VBNET.ATG" 
		stmt = blockStmt;
		if (t != null) blockStmt.EndLocation = t.EndLocation;
		BlockEnd();
		
	}

	void Charset(
#line  1584 "VBNET.ATG" 
out CharsetModifier charsetModifier) {

#line  1585 "VBNET.ATG" 
		charsetModifier = CharsetModifier.None; 
		if (la.kind == 127 || la.kind == 210) {
		} else if (la.kind == 62) {
			lexer.NextToken();

#line  1586 "VBNET.ATG" 
			charsetModifier = CharsetModifier.Ansi; 
		} else if (la.kind == 66) {
			lexer.NextToken();

#line  1587 "VBNET.ATG" 
			charsetModifier = CharsetModifier.Auto; 
		} else if (la.kind == 223) {
			lexer.NextToken();

#line  1588 "VBNET.ATG" 
			charsetModifier = CharsetModifier.Unicode; 
		} else SynErr(267);
	}

	void IdentifierForFieldDeclaration() {
		switch (la.kind) {
		case 2: {
			lexer.NextToken();
			break;
		}
		case 58: {
			lexer.NextToken();
			break;
		}
		case 62: {
			lexer.NextToken();
			break;
		}
		case 64: {
			lexer.NextToken();
			break;
		}
		case 65: {
			lexer.NextToken();
			break;
		}
		case 66: {
			lexer.NextToken();
			break;
		}
		case 67: {
			lexer.NextToken();
			break;
		}
		case 70: {
			lexer.NextToken();
			break;
		}
		case 87: {
			lexer.NextToken();
			break;
		}
		case 104: {
			lexer.NextToken();
			break;
		}
		case 107: {
			lexer.NextToken();
			break;
		}
		case 116: {
			lexer.NextToken();
			break;
		}
		case 121: {
			lexer.NextToken();
			break;
		}
		case 126: {
			lexer.NextToken();
			break;
		}
		case 133: {
			lexer.NextToken();
			break;
		}
		case 139: {
			lexer.NextToken();
			break;
		}
		case 143: {
			lexer.NextToken();
			break;
		}
		case 146: {
			lexer.NextToken();
			break;
		}
		case 147: {
			lexer.NextToken();
			break;
		}
		case 170: {
			lexer.NextToken();
			break;
		}
		case 176: {
			lexer.NextToken();
			break;
		}
		case 178: {
			lexer.NextToken();
			break;
		}
		case 184: {
			lexer.NextToken();
			break;
		}
		case 203: {
			lexer.NextToken();
			break;
		}
		case 212: {
			lexer.NextToken();
			break;
		}
		case 213: {
			lexer.NextToken();
			break;
		}
		case 223: {
			lexer.NextToken();
			break;
		}
		case 224: {
			lexer.NextToken();
			break;
		}
		case 230: {
			lexer.NextToken();
			break;
		}
		default: SynErr(268); break;
		}
	}

	void VariableDeclaratorPartAfterIdentifier(
#line  1463 "VBNET.ATG" 
List<VariableDeclaration> fieldDeclaration, string name) {

#line  1465 "VBNET.ATG" 
		Expression expr = null;
		TypeReference type = null;
		ArrayList rank = null;
		List<Expression> dimension = null;
		Location startLocation = t.Location;
		
		if (
#line  1471 "VBNET.ATG" 
IsSize() && !IsDims()) {
			ArrayInitializationModifier(
#line  1471 "VBNET.ATG" 
out dimension);
		}
		if (
#line  1472 "VBNET.ATG" 
IsDims()) {
			ArrayNameModifier(
#line  1472 "VBNET.ATG" 
out rank);
		}
		if (
#line  1474 "VBNET.ATG" 
IsObjectCreation()) {
			Expect(63);
			ObjectCreateExpression(
#line  1474 "VBNET.ATG" 
out expr);

#line  1476 "VBNET.ATG" 
			if (expr is ObjectCreateExpression) {
			type = ((ObjectCreateExpression)expr).CreateType.Clone();
			} else {
				type = ((ArrayCreateExpression)expr).CreateType.Clone();
			}
			
		} else if (StartOf(23)) {
			if (la.kind == 63) {
				lexer.NextToken();
				TypeName(
#line  1483 "VBNET.ATG" 
out type);

#line  1485 "VBNET.ATG" 
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

#line  1497 "VBNET.ATG" 
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
					expr = new ArrayCreateExpression(type.Clone(), dimension);
				}
			} else if (rank != null) {
				if(type.RankSpecifier != null) {
					Error("array rank only allowed one time");
				} else {
					type.RankSpecifier = (int[])rank.ToArray(typeof(int));
				}
			}
			
			if (la.kind == 20) {
				lexer.NextToken();
				Expr(
#line  1520 "VBNET.ATG" 
out expr);
			}
		} else SynErr(269);

#line  1523 "VBNET.ATG" 
		VariableDeclaration varDecl = new VariableDeclaration(name, expr, type);
		varDecl.StartLocation = startLocation;
		varDecl.EndLocation = t.Location;
		fieldDeclaration.Add(varDecl);
		
	}

	void VariableDeclarator(
#line  1457 "VBNET.ATG" 
List<VariableDeclaration> fieldDeclaration) {
		Identifier();

#line  1459 "VBNET.ATG" 
		string name = t.val; 
		VariableDeclaratorPartAfterIdentifier(
#line  1460 "VBNET.ATG" 
fieldDeclaration, name);
	}

	void ConstantDeclarator(
#line  1438 "VBNET.ATG" 
List<VariableDeclaration> constantDeclaration) {

#line  1440 "VBNET.ATG" 
		Expression expr = null;
		TypeReference type = null;
		string name = String.Empty;
		Location location;
		
		Identifier();

#line  1445 "VBNET.ATG" 
		name = t.val; location = t.Location; 
		if (la.kind == 63) {
			lexer.NextToken();
			TypeName(
#line  1446 "VBNET.ATG" 
out type);
		}
		Expect(20);
		Expr(
#line  1447 "VBNET.ATG" 
out expr);

#line  1449 "VBNET.ATG" 
		VariableDeclaration f = new VariableDeclaration(name, expr);
		f.TypeReference = type;
		f.StartLocation = location;
		constantDeclaration.Add(f);
		
	}

	void ObjectCreateExpression(
#line  2123 "VBNET.ATG" 
out Expression oce) {

#line  2125 "VBNET.ATG" 
		TypeReference type = null;
		CollectionInitializerExpression initializer = null;
		List<Expression> arguments = null;
		ArrayList dimensions = null;
		oce = null;
		Location startLocation = la.Location;
		bool canBeNormal; bool canBeReDim;
		
		Expect(162);
		if (StartOf(8)) {
			NonArrayTypeName(
#line  2134 "VBNET.ATG" 
out type, false);
			if (la.kind == 37) {
				lexer.NextToken();
				NormalOrReDimArgumentList(
#line  2135 "VBNET.ATG" 
out arguments, out canBeNormal, out canBeReDim);
				Expect(38);
				if (la.kind == 35 || 
#line  2136 "VBNET.ATG" 
la.kind == Tokens.OpenParenthesis) {
					if (
#line  2136 "VBNET.ATG" 
la.kind == Tokens.OpenParenthesis) {
						ArrayTypeModifiers(
#line  2137 "VBNET.ATG" 
out dimensions);
						CollectionInitializer(
#line  2138 "VBNET.ATG" 
out initializer);
					} else {
						CollectionInitializer(
#line  2139 "VBNET.ATG" 
out initializer);
					}
				}

#line  2141 "VBNET.ATG" 
				if (canBeReDim && !canBeNormal && initializer == null) initializer = new CollectionInitializerExpression(); 
			}
		}

#line  2145 "VBNET.ATG" 
		if (initializer == null) {
		oce = new ObjectCreateExpression(type, arguments);
		} else {
			if (dimensions == null) dimensions = new ArrayList();
			dimensions.Insert(0, (arguments == null) ? 0 : Math.Max(arguments.Count - 1, 0));
			type.RankSpecifier = (int[])dimensions.ToArray(typeof(int));
			ArrayCreateExpression ace = new ArrayCreateExpression(type, initializer);
			ace.Arguments = arguments;
			oce = ace;
		}
		
		if (la.kind == 126 || la.kind == 233) {
			if (la.kind == 233) {

#line  2160 "VBNET.ATG" 
				MemberInitializerExpression memberInitializer = null;
				Expression anonymousMember = null;
				
				lexer.NextToken();

#line  2165 "VBNET.ATG" 
				CollectionInitializerExpression memberInitializers = new CollectionInitializerExpression();
				memberInitializers.StartLocation = la.Location;
				
				Expect(35);
				if (la.kind == 26 || la.kind == 147) {
					MemberInitializer(
#line  2170 "VBNET.ATG" 
out memberInitializer);

#line  2171 "VBNET.ATG" 
					memberInitializers.CreateExpressions.Add(memberInitializer); 
				} else if (StartOf(24)) {
					Expr(
#line  2172 "VBNET.ATG" 
out anonymousMember);

#line  2173 "VBNET.ATG" 
					memberInitializers.CreateExpressions.Add(anonymousMember); 
				} else SynErr(270);
				while (la.kind == 22) {
					lexer.NextToken();
					if (la.kind == 26 || la.kind == 147) {
						MemberInitializer(
#line  2177 "VBNET.ATG" 
out memberInitializer);

#line  2178 "VBNET.ATG" 
						memberInitializers.CreateExpressions.Add(memberInitializer); 
					} else if (StartOf(24)) {
						Expr(
#line  2179 "VBNET.ATG" 
out anonymousMember);

#line  2180 "VBNET.ATG" 
						memberInitializers.CreateExpressions.Add(anonymousMember); 
					} else SynErr(271);
				}
				Expect(36);

#line  2185 "VBNET.ATG" 
				memberInitializers.EndLocation = t.Location;
				if(oce is ObjectCreateExpression)
				{
					((ObjectCreateExpression)oce).ObjectInitializer = memberInitializers;
				}
				
			} else {
				lexer.NextToken();
				CollectionInitializer(
#line  2195 "VBNET.ATG" 
out initializer);

#line  2197 "VBNET.ATG" 
				if(oce is ObjectCreateExpression)
				((ObjectCreateExpression)oce).ObjectInitializer = initializer;
				
			}
		}

#line  2203 "VBNET.ATG" 
		if (oce != null) {
		oce.StartLocation = startLocation;
		oce.EndLocation = t.EndLocation;
		}
		
	}

	void AccessorDecls(
#line  1372 "VBNET.ATG" 
out PropertyGetRegion getBlock, out PropertySetRegion setBlock) {

#line  1374 "VBNET.ATG" 
		List<AttributeSection> attributes = new List<AttributeSection>();
		AttributeSection section;
		getBlock = null;
		setBlock = null; 
		
		while (la.kind == 40) {
			AttributeSection(
#line  1379 "VBNET.ATG" 
out section);

#line  1379 "VBNET.ATG" 
			attributes.Add(section); 
		}
		if (StartOf(25)) {
			GetAccessorDecl(
#line  1381 "VBNET.ATG" 
out getBlock, attributes);
			if (StartOf(26)) {

#line  1383 "VBNET.ATG" 
				attributes = new List<AttributeSection>(); 
				while (la.kind == 40) {
					AttributeSection(
#line  1384 "VBNET.ATG" 
out section);

#line  1384 "VBNET.ATG" 
					attributes.Add(section); 
				}
				SetAccessorDecl(
#line  1385 "VBNET.ATG" 
out setBlock, attributes);
			}
		} else if (StartOf(27)) {
			SetAccessorDecl(
#line  1388 "VBNET.ATG" 
out setBlock, attributes);
			if (StartOf(28)) {

#line  1390 "VBNET.ATG" 
				attributes = new List<AttributeSection>(); 
				while (la.kind == 40) {
					AttributeSection(
#line  1391 "VBNET.ATG" 
out section);

#line  1391 "VBNET.ATG" 
					attributes.Add(section); 
				}
				GetAccessorDecl(
#line  1392 "VBNET.ATG" 
out getBlock, attributes);
			}
		} else SynErr(272);
	}

	void EventAccessorDeclaration(
#line  1335 "VBNET.ATG" 
out EventAddRemoveRegion eventAccessorDeclaration) {

#line  1337 "VBNET.ATG" 
		Statement stmt = null;
		List<ParameterDeclarationExpression> p = new List<ParameterDeclarationExpression>();
		AttributeSection section;
		List<AttributeSection> attributes = new List<AttributeSection>();
		eventAccessorDeclaration = null;
		
		while (la.kind == 40) {
			AttributeSection(
#line  1343 "VBNET.ATG" 
out section);

#line  1343 "VBNET.ATG" 
			attributes.Add(section); 
		}
		if (la.kind == 56) {
			lexer.NextToken();
			if (la.kind == 37) {
				lexer.NextToken();
				if (StartOf(6)) {
					FormalParameterList(
#line  1345 "VBNET.ATG" 
p);
				}
				Expect(38);
			}
			Expect(1);
			Block(
#line  1346 "VBNET.ATG" 
out stmt);
			Expect(113);
			Expect(56);
			EndOfStmt();

#line  1348 "VBNET.ATG" 
			eventAccessorDeclaration = new EventAddRegion(attributes);
			eventAccessorDeclaration.Block = (BlockStatement)stmt;
			eventAccessorDeclaration.Parameters = p;
			
		} else if (la.kind == 193) {
			lexer.NextToken();
			if (la.kind == 37) {
				lexer.NextToken();
				if (StartOf(6)) {
					FormalParameterList(
#line  1353 "VBNET.ATG" 
p);
				}
				Expect(38);
			}
			Expect(1);
			Block(
#line  1354 "VBNET.ATG" 
out stmt);
			Expect(113);
			Expect(193);
			EndOfStmt();

#line  1356 "VBNET.ATG" 
			eventAccessorDeclaration = new EventRemoveRegion(attributes);
			eventAccessorDeclaration.Block = (BlockStatement)stmt;
			eventAccessorDeclaration.Parameters = p;
			
		} else if (la.kind == 189) {
			lexer.NextToken();
			if (la.kind == 37) {
				lexer.NextToken();
				if (StartOf(6)) {
					FormalParameterList(
#line  1361 "VBNET.ATG" 
p);
				}
				Expect(38);
			}
			Expect(1);
			Block(
#line  1362 "VBNET.ATG" 
out stmt);
			Expect(113);
			Expect(189);
			EndOfStmt();

#line  1364 "VBNET.ATG" 
			eventAccessorDeclaration = new EventRaiseRegion(attributes);
			eventAccessorDeclaration.Block = (BlockStatement)stmt;
			eventAccessorDeclaration.Parameters = p;
			
		} else SynErr(273);
	}

	void OverloadableOperator(
#line  1277 "VBNET.ATG" 
out OverloadableOperatorType operatorType) {

#line  1278 "VBNET.ATG" 
		operatorType = OverloadableOperatorType.None; 
		switch (la.kind) {
		case 31: {
			lexer.NextToken();

#line  1280 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.Add; 
			break;
		}
		case 30: {
			lexer.NextToken();

#line  1282 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.Subtract; 
			break;
		}
		case 34: {
			lexer.NextToken();

#line  1284 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.Multiply; 
			break;
		}
		case 24: {
			lexer.NextToken();

#line  1286 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.Divide; 
			break;
		}
		case 25: {
			lexer.NextToken();

#line  1288 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.DivideInteger; 
			break;
		}
		case 23: {
			lexer.NextToken();

#line  1290 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.Concat; 
			break;
		}
		case 150: {
			lexer.NextToken();

#line  1292 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.Like; 
			break;
		}
		case 154: {
			lexer.NextToken();

#line  1294 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.Modulus; 
			break;
		}
		case 60: {
			lexer.NextToken();

#line  1296 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.BitwiseAnd; 
			break;
		}
		case 175: {
			lexer.NextToken();

#line  1298 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.BitwiseOr; 
			break;
		}
		case 236: {
			lexer.NextToken();

#line  1300 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.ExclusiveOr; 
			break;
		}
		case 32: {
			lexer.NextToken();

#line  1302 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.Power; 
			break;
		}
		case 44: {
			lexer.NextToken();

#line  1304 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.ShiftLeft; 
			break;
		}
		case 45: {
			lexer.NextToken();

#line  1306 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.ShiftRight; 
			break;
		}
		case 20: {
			lexer.NextToken();

#line  1308 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.Equality; 
			break;
		}
		case 41: {
			lexer.NextToken();

#line  1310 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.InEquality; 
			break;
		}
		case 40: {
			lexer.NextToken();

#line  1312 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.LessThan; 
			break;
		}
		case 43: {
			lexer.NextToken();

#line  1314 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.LessThanOrEqual; 
			break;
		}
		case 39: {
			lexer.NextToken();

#line  1316 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.GreaterThan; 
			break;
		}
		case 42: {
			lexer.NextToken();

#line  1318 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.GreaterThanOrEqual; 
			break;
		}
		case 94: {
			lexer.NextToken();

#line  1320 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.CType; 
			break;
		}
		case 2: case 58: case 62: case 64: case 65: case 66: case 67: case 70: case 87: case 98: case 104: case 107: case 116: case 121: case 126: case 133: case 139: case 143: case 146: case 147: case 170: case 176: case 178: case 184: case 203: case 212: case 213: case 223: case 224: case 230: {
			Identifier();

#line  1324 "VBNET.ATG" 
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
		default: SynErr(274); break;
		}
	}

	void FormalParameter(
#line  2942 "VBNET.ATG" 
out ParameterDeclarationExpression p) {

#line  2944 "VBNET.ATG" 
		AttributeSection section;
		List<AttributeSection> attributes = new List<AttributeSection>();
		TypeReference type = null;
		ParamModifierList mod = new ParamModifierList(this);
		Expression expr = null;
		p = null;
		ArrayList arrayModifiers = null;
		Location startLocation = la.Location;
		
		while (la.kind == 40) {
			AttributeSection(
#line  2954 "VBNET.ATG" 
out section);

#line  2954 "VBNET.ATG" 
			attributes.Add(section); 
		}
		while (StartOf(29)) {
			ParameterModifier(
#line  2955 "VBNET.ATG" 
mod);
		}
		Identifier();

#line  2956 "VBNET.ATG" 
		string parameterName = t.val; 
		if (
#line  2957 "VBNET.ATG" 
IsDims()) {
			ArrayTypeModifiers(
#line  2957 "VBNET.ATG" 
out arrayModifiers);
		}
		if (la.kind == 63) {
			lexer.NextToken();
			TypeName(
#line  2958 "VBNET.ATG" 
out type);
		}

#line  2960 "VBNET.ATG" 
		if(type != null) {
		if (arrayModifiers != null) {
			if (type.RankSpecifier != null) {
				Error("array rank only allowed one time");
			} else {
				type.RankSpecifier = (int[])arrayModifiers.ToArray(typeof(int));
			}
		}
		}
		
		if (la.kind == 20) {
			lexer.NextToken();
			Expr(
#line  2970 "VBNET.ATG" 
out expr);
		}

#line  2972 "VBNET.ATG" 
		mod.Check();
		p = new ParameterDeclarationExpression(type, parameterName, mod.Modifier, expr);
		p.Attributes = attributes;
		p.StartLocation = startLocation;
		p.EndLocation = t.EndLocation;
		
	}

	void GetAccessorDecl(
#line  1398 "VBNET.ATG" 
out PropertyGetRegion getBlock, List<AttributeSection> attributes) {

#line  1399 "VBNET.ATG" 
		Statement stmt = null; Modifiers m; 
		PropertyAccessorAccessModifier(
#line  1401 "VBNET.ATG" 
out m);
		Expect(128);

#line  1403 "VBNET.ATG" 
		Location startLocation = t.Location; 
		Expect(1);
		Block(
#line  1405 "VBNET.ATG" 
out stmt);

#line  1406 "VBNET.ATG" 
		getBlock = new PropertyGetRegion((BlockStatement)stmt, attributes); 
		Expect(113);
		Expect(128);

#line  1408 "VBNET.ATG" 
		getBlock.Modifier = m; 

#line  1409 "VBNET.ATG" 
		getBlock.StartLocation = startLocation; getBlock.EndLocation = t.EndLocation; 
		EndOfStmt();
	}

	void SetAccessorDecl(
#line  1414 "VBNET.ATG" 
out PropertySetRegion setBlock, List<AttributeSection> attributes) {

#line  1416 "VBNET.ATG" 
		Statement stmt = null;
		List<ParameterDeclarationExpression> p = new List<ParameterDeclarationExpression>();
		Modifiers m;
		
		PropertyAccessorAccessModifier(
#line  1421 "VBNET.ATG" 
out m);
		Expect(198);

#line  1423 "VBNET.ATG" 
		Location startLocation = t.Location; 
		if (la.kind == 37) {
			lexer.NextToken();
			if (StartOf(6)) {
				FormalParameterList(
#line  1424 "VBNET.ATG" 
p);
			}
			Expect(38);
		}
		Expect(1);
		Block(
#line  1426 "VBNET.ATG" 
out stmt);

#line  1428 "VBNET.ATG" 
		setBlock = new PropertySetRegion((BlockStatement)stmt, attributes);
		setBlock.Modifier = m;
		setBlock.Parameters = p;
		
		Expect(113);
		Expect(198);

#line  1433 "VBNET.ATG" 
		setBlock.StartLocation = startLocation; setBlock.EndLocation = t.EndLocation; 
		EndOfStmt();
	}

	void PropertyAccessorAccessModifier(
#line  3736 "VBNET.ATG" 
out Modifiers m) {

#line  3737 "VBNET.ATG" 
		m = Modifiers.None; 
		while (StartOf(30)) {
			if (la.kind == 188) {
				lexer.NextToken();

#line  3739 "VBNET.ATG" 
				m |= Modifiers.Public; 
			} else if (la.kind == 187) {
				lexer.NextToken();

#line  3740 "VBNET.ATG" 
				m |= Modifiers.Protected; 
			} else if (la.kind == 125) {
				lexer.NextToken();

#line  3741 "VBNET.ATG" 
				m |= Modifiers.Internal; 
			} else {
				lexer.NextToken();

#line  3742 "VBNET.ATG" 
				m |= Modifiers.Private; 
			}
		}
	}

	void ArrayInitializationModifier(
#line  1531 "VBNET.ATG" 
out List<Expression> arrayModifiers) {

#line  1533 "VBNET.ATG" 
		arrayModifiers = null;
		
		Expect(37);
		InitializationRankList(
#line  1535 "VBNET.ATG" 
out arrayModifiers);
		Expect(38);
	}

	void ArrayNameModifier(
#line  2769 "VBNET.ATG" 
out ArrayList arrayModifiers) {

#line  2771 "VBNET.ATG" 
		arrayModifiers = null;
		
		ArrayTypeModifiers(
#line  2773 "VBNET.ATG" 
out arrayModifiers);
	}

	void InitializationRankList(
#line  1539 "VBNET.ATG" 
out List<Expression> rank) {

#line  1541 "VBNET.ATG" 
		rank = new List<Expression>();
		Expression expr = null;
		
		Expr(
#line  1544 "VBNET.ATG" 
out expr);
		if (la.kind == 216) {
			lexer.NextToken();

#line  1545 "VBNET.ATG" 
			EnsureIsZero(expr); 
			Expr(
#line  1546 "VBNET.ATG" 
out expr);
		}

#line  1548 "VBNET.ATG" 
		if (expr != null) { rank.Add(expr); } 
		while (la.kind == 22) {
			lexer.NextToken();
			Expr(
#line  1550 "VBNET.ATG" 
out expr);
			if (la.kind == 216) {
				lexer.NextToken();

#line  1551 "VBNET.ATG" 
				EnsureIsZero(expr); 
				Expr(
#line  1552 "VBNET.ATG" 
out expr);
			}

#line  1554 "VBNET.ATG" 
			if (expr != null) { rank.Add(expr); } 
		}
	}

	void CollectionInitializer(
#line  1559 "VBNET.ATG" 
out CollectionInitializerExpression outExpr) {

#line  1561 "VBNET.ATG" 
		Expression expr = null;
		CollectionInitializerExpression initializer = new CollectionInitializerExpression();
		Location startLocation = la.Location;
		
		Expect(35);
		if (StartOf(24)) {
			Expr(
#line  1567 "VBNET.ATG" 
out expr);

#line  1569 "VBNET.ATG" 
			if (expr != null) { initializer.CreateExpressions.Add(expr); }
			
			while (
#line  1572 "VBNET.ATG" 
NotFinalComma()) {
				Expect(22);
				Expr(
#line  1572 "VBNET.ATG" 
out expr);

#line  1573 "VBNET.ATG" 
				if (expr != null) { initializer.CreateExpressions.Add(expr); } 
			}
		}
		Expect(36);

#line  1578 "VBNET.ATG" 
		outExpr = initializer;
		outExpr.StartLocation = startLocation;
		outExpr.EndLocation = t.EndLocation;
		
	}

	void EventMemberSpecifier(
#line  1651 "VBNET.ATG" 
out string name) {

#line  1652 "VBNET.ATG" 
		string eventName; 
		if (StartOf(4)) {
			Identifier();
		} else if (la.kind == 158) {
			lexer.NextToken();
		} else if (la.kind == 153) {
			lexer.NextToken();
		} else SynErr(275);

#line  1655 "VBNET.ATG" 
		name = t.val; 
		Expect(26);
		IdentifierOrKeyword(
#line  1657 "VBNET.ATG" 
out eventName);

#line  1658 "VBNET.ATG" 
		name = name + "." + eventName; 
	}

	void IdentifierOrKeyword(
#line  3669 "VBNET.ATG" 
out string name) {
		lexer.NextToken();

#line  3671 "VBNET.ATG" 
		name = t.val;  
	}

	void QueryExpr(
#line  2290 "VBNET.ATG" 
out Expression expr) {

#line  2292 "VBNET.ATG" 
		QueryExpressionVB qexpr = new QueryExpressionVB();
		qexpr.StartLocation = la.Location;
		expr = qexpr;
		
		FromOrAggregateQueryOperator(
#line  2296 "VBNET.ATG" 
qexpr.Clauses);
		while (StartOf(31)) {
			QueryOperator(
#line  2297 "VBNET.ATG" 
qexpr.Clauses);
		}

#line  2299 "VBNET.ATG" 
		qexpr.EndLocation = t.EndLocation;
		
	}

	void LambdaExpr(
#line  2210 "VBNET.ATG" 
out Expression expr) {

#line  2212 "VBNET.ATG" 
		LambdaExpression lambda = null;
		
		if (la.kind == 210) {
			SubLambdaExpression(
#line  2214 "VBNET.ATG" 
out lambda);
		} else if (la.kind == 127) {
			FunctionLambdaExpression(
#line  2215 "VBNET.ATG" 
out lambda);
		} else SynErr(276);

#line  2216 "VBNET.ATG" 
		expr = lambda; 
	}

	void DisjunctionExpr(
#line  1960 "VBNET.ATG" 
out Expression outExpr) {

#line  1962 "VBNET.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		Location startLocation = la.Location;
		
		ConjunctionExpr(
#line  1966 "VBNET.ATG" 
out outExpr);
		while (la.kind == 175 || la.kind == 177 || la.kind == 236) {
			if (la.kind == 175) {
				lexer.NextToken();

#line  1969 "VBNET.ATG" 
				op = BinaryOperatorType.BitwiseOr; 
			} else if (la.kind == 177) {
				lexer.NextToken();

#line  1970 "VBNET.ATG" 
				op = BinaryOperatorType.LogicalOr; 
			} else {
				lexer.NextToken();

#line  1971 "VBNET.ATG" 
				op = BinaryOperatorType.ExclusiveOr; 
			}
			ConjunctionExpr(
#line  1973 "VBNET.ATG" 
out expr);

#line  1973 "VBNET.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr) { StartLocation = startLocation, EndLocation = t.EndLocation };  
		}
	}

	void AssignmentOperator(
#line  1678 "VBNET.ATG" 
out AssignmentOperatorType op) {

#line  1679 "VBNET.ATG" 
		op = AssignmentOperatorType.None; 
		switch (la.kind) {
		case 20: {
			lexer.NextToken();

#line  1680 "VBNET.ATG" 
			op = AssignmentOperatorType.Assign; 
			break;
		}
		case 54: {
			lexer.NextToken();

#line  1681 "VBNET.ATG" 
			op = AssignmentOperatorType.ConcatString; 
			break;
		}
		case 46: {
			lexer.NextToken();

#line  1682 "VBNET.ATG" 
			op = AssignmentOperatorType.Add; 
			break;
		}
		case 48: {
			lexer.NextToken();

#line  1683 "VBNET.ATG" 
			op = AssignmentOperatorType.Subtract; 
			break;
		}
		case 49: {
			lexer.NextToken();

#line  1684 "VBNET.ATG" 
			op = AssignmentOperatorType.Multiply; 
			break;
		}
		case 50: {
			lexer.NextToken();

#line  1685 "VBNET.ATG" 
			op = AssignmentOperatorType.Divide; 
			break;
		}
		case 51: {
			lexer.NextToken();

#line  1686 "VBNET.ATG" 
			op = AssignmentOperatorType.DivideInteger; 
			break;
		}
		case 47: {
			lexer.NextToken();

#line  1687 "VBNET.ATG" 
			op = AssignmentOperatorType.Power; 
			break;
		}
		case 52: {
			lexer.NextToken();

#line  1688 "VBNET.ATG" 
			op = AssignmentOperatorType.ShiftLeft; 
			break;
		}
		case 53: {
			lexer.NextToken();

#line  1689 "VBNET.ATG" 
			op = AssignmentOperatorType.ShiftRight; 
			break;
		}
		default: SynErr(277); break;
		}
	}

	void SimpleExpr(
#line  1693 "VBNET.ATG" 
out Expression pexpr) {

#line  1694 "VBNET.ATG" 
		string name; Location startLocation = la.Location; 
		SimpleNonInvocationExpression(
#line  1697 "VBNET.ATG" 
out pexpr);
		while (StartOf(32)) {
			if (la.kind == 26) {
				lexer.NextToken();
				if (la.kind == 10) {
					lexer.NextToken();
					IdentifierOrKeyword(
#line  1700 "VBNET.ATG" 
out name);
					Expect(11);

#line  1701 "VBNET.ATG" 
					pexpr = new XmlMemberAccessExpression(pexpr, XmlAxisType.Element, name, true); 
				} else if (StartOf(33)) {
					IdentifierOrKeyword(
#line  1702 "VBNET.ATG" 
out name);

#line  1703 "VBNET.ATG" 
					pexpr = new MemberReferenceExpression(pexpr, name) { StartLocation = startLocation, EndLocation = t.EndLocation }; 
				} else SynErr(278);
				if (
#line  1705 "VBNET.ATG" 
la.kind == Tokens.OpenParenthesis && Peek(1).kind == Tokens.Of) {
					lexer.NextToken();
					Expect(169);
					TypeArgumentList(
#line  1706 "VBNET.ATG" 
((MemberReferenceExpression)pexpr).TypeArguments);
					Expect(38);
				}
			} else if (la.kind == 29) {
				lexer.NextToken();
				IdentifierOrKeyword(
#line  1708 "VBNET.ATG" 
out name);

#line  1708 "VBNET.ATG" 
				pexpr = new BinaryOperatorExpression(pexpr, BinaryOperatorType.DictionaryAccess, new PrimitiveExpression(name, name) { StartLocation = t.Location, EndLocation = t.EndLocation }); 
			} else if (la.kind == 27 || la.kind == 28) {

#line  1709 "VBNET.ATG" 
				XmlAxisType type = XmlAxisType.Attribute; bool isXmlName = false; 
				if (la.kind == 28) {
					lexer.NextToken();
				} else if (la.kind == 27) {
					lexer.NextToken();

#line  1710 "VBNET.ATG" 
					type = XmlAxisType.Descendents; 
				} else SynErr(279);
				if (la.kind == 10) {
					lexer.NextToken();

#line  1710 "VBNET.ATG" 
					isXmlName = true; 
				}
				IdentifierOrKeyword(
#line  1710 "VBNET.ATG" 
out name);
				if (la.kind == 11) {
					lexer.NextToken();
				}

#line  1711 "VBNET.ATG" 
				pexpr = new XmlMemberAccessExpression(pexpr, type, name, isXmlName); 
			} else {
				InvocationExpression(
#line  1712 "VBNET.ATG" 
ref pexpr);
			}
		}

#line  1716 "VBNET.ATG" 
		if (pexpr != null) {
		pexpr.StartLocation = startLocation;
		pexpr.EndLocation = t.EndLocation;
		}
		
	}

	void SimpleNonInvocationExpression(
#line  1723 "VBNET.ATG" 
out Expression pexpr) {

#line  1725 "VBNET.ATG" 
		Expression expr;
		CollectionInitializerExpression cie;
		TypeReference type = null;
		string name = String.Empty;
		Location startLocation = la.Location;
		pexpr = null;
		
		if (StartOf(34)) {
			switch (la.kind) {
			case 3: {
				lexer.NextToken();

#line  1735 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(t.literalValue, t.val) { LiteralFormat = t.literalFormat };  
				break;
			}
			case 4: {
				lexer.NextToken();

#line  1736 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(t.literalValue, t.val) { LiteralFormat = t.literalFormat };  
				break;
			}
			case 7: {
				lexer.NextToken();

#line  1737 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(t.literalValue, t.val) { LiteralFormat = t.literalFormat };  
				break;
			}
			case 6: {
				lexer.NextToken();

#line  1738 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(t.literalValue, t.val) { LiteralFormat = t.literalFormat };  
				break;
			}
			case 5: {
				lexer.NextToken();

#line  1739 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(t.literalValue, t.val) { LiteralFormat = t.literalFormat };  
				break;
			}
			case 9: {
				lexer.NextToken();

#line  1740 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(t.literalValue, t.val) { LiteralFormat = t.literalFormat };  
				break;
			}
			case 8: {
				lexer.NextToken();

#line  1741 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(t.literalValue, t.val) { LiteralFormat = t.literalFormat };  
				break;
			}
			case 217: {
				lexer.NextToken();

#line  1743 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(true, "true");  
				break;
			}
			case 122: {
				lexer.NextToken();

#line  1744 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(false, "false"); 
				break;
			}
			case 165: {
				lexer.NextToken();

#line  1745 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(null, "null");  
				break;
			}
			case 37: {
				lexer.NextToken();
				Expr(
#line  1746 "VBNET.ATG" 
out expr);
				Expect(38);

#line  1746 "VBNET.ATG" 
				pexpr = new ParenthesizedExpression(expr); 
				break;
			}
			case 2: case 58: case 62: case 64: case 65: case 66: case 67: case 70: case 87: case 98: case 104: case 107: case 116: case 121: case 126: case 133: case 139: case 143: case 146: case 147: case 170: case 176: case 178: case 184: case 203: case 212: case 213: case 223: case 224: case 230: {
				Identifier();

#line  1748 "VBNET.ATG" 
				pexpr = new IdentifierExpression(t.val);
				pexpr.StartLocation = t.Location; pexpr.EndLocation = t.EndLocation;
				
				if (
#line  1751 "VBNET.ATG" 
la.kind == Tokens.OpenParenthesis && Peek(1).kind == Tokens.Of) {
					lexer.NextToken();
					Expect(169);
					TypeArgumentList(
#line  1752 "VBNET.ATG" 
((IdentifierExpression)pexpr).TypeArguments);
					Expect(38);
				}
				break;
			}
			case 68: case 71: case 82: case 99: case 100: case 109: case 141: case 151: case 168: case 196: case 201: case 202: case 208: case 221: case 222: case 225: {

#line  1754 "VBNET.ATG" 
				string val = String.Empty; 
				if (StartOf(12)) {
					PrimitiveTypeName(
#line  1755 "VBNET.ATG" 
out val);
				} else if (la.kind == 168) {
					lexer.NextToken();

#line  1755 "VBNET.ATG" 
					val = "System.Object"; 
				} else SynErr(280);

#line  1756 "VBNET.ATG" 
				pexpr = new TypeReferenceExpression(new TypeReference(val, true)); 
				break;
			}
			case 153: {
				lexer.NextToken();

#line  1757 "VBNET.ATG" 
				pexpr = new ThisReferenceExpression(); 
				break;
			}
			case 158: case 159: {

#line  1758 "VBNET.ATG" 
				Expression retExpr = null; 
				if (la.kind == 158) {
					lexer.NextToken();

#line  1759 "VBNET.ATG" 
					retExpr = new BaseReferenceExpression() { StartLocation = t.Location, EndLocation = t.EndLocation }; 
				} else if (la.kind == 159) {
					lexer.NextToken();

#line  1760 "VBNET.ATG" 
					retExpr = new ClassReferenceExpression() { StartLocation = t.Location, EndLocation = t.EndLocation }; 
				} else SynErr(281);
				Expect(26);
				IdentifierOrKeyword(
#line  1762 "VBNET.ATG" 
out name);

#line  1762 "VBNET.ATG" 
				pexpr = new MemberReferenceExpression(retExpr, name) { StartLocation = startLocation, EndLocation = t.EndLocation }; 
				break;
			}
			case 130: {
				lexer.NextToken();
				Expect(26);
				Identifier();

#line  1764 "VBNET.ATG" 
				type = new TypeReference(t.val ?? ""); 

#line  1766 "VBNET.ATG" 
				type.IsGlobal = true; 

#line  1767 "VBNET.ATG" 
				pexpr = new TypeReferenceExpression(type); 
				break;
			}
			case 162: {
				ObjectCreateExpression(
#line  1768 "VBNET.ATG" 
out expr);

#line  1768 "VBNET.ATG" 
				pexpr = expr; 
				break;
			}
			case 35: {
				CollectionInitializer(
#line  1769 "VBNET.ATG" 
out cie);

#line  1769 "VBNET.ATG" 
				pexpr = cie; 
				break;
			}
			case 94: case 106: case 219: {

#line  1771 "VBNET.ATG" 
				CastType castType = CastType.Cast; 
				if (la.kind == 106) {
					lexer.NextToken();
				} else if (la.kind == 94) {
					lexer.NextToken();

#line  1773 "VBNET.ATG" 
					castType = CastType.Conversion; 
				} else if (la.kind == 219) {
					lexer.NextToken();

#line  1774 "VBNET.ATG" 
					castType = CastType.TryCast; 
				} else SynErr(282);
				Expect(37);
				Expr(
#line  1776 "VBNET.ATG" 
out expr);
				Expect(22);
				TypeName(
#line  1776 "VBNET.ATG" 
out type);
				Expect(38);

#line  1777 "VBNET.ATG" 
				pexpr = new CastExpression(type, expr, castType); 
				break;
			}
			case 76: case 77: case 78: case 79: case 80: case 81: case 83: case 85: case 86: case 90: case 91: case 92: case 93: case 95: case 96: case 97: {
				CastTarget(
#line  1778 "VBNET.ATG" 
out type);
				Expect(37);
				Expr(
#line  1778 "VBNET.ATG" 
out expr);
				Expect(38);

#line  1778 "VBNET.ATG" 
				pexpr = new CastExpression(type, expr, CastType.PrimitiveConversion); 
				break;
			}
			case 57: {
				lexer.NextToken();
				Expr(
#line  1779 "VBNET.ATG" 
out expr);

#line  1779 "VBNET.ATG" 
				pexpr = new AddressOfExpression(expr); 
				break;
			}
			case 129: {
				lexer.NextToken();
				Expect(37);
				GetTypeTypeName(
#line  1780 "VBNET.ATG" 
out type);
				Expect(38);

#line  1780 "VBNET.ATG" 
				pexpr = new TypeOfExpression(type); 
				break;
			}
			case 220: {
				lexer.NextToken();
				SimpleExpr(
#line  1781 "VBNET.ATG" 
out expr);
				Expect(144);
				TypeName(
#line  1781 "VBNET.ATG" 
out type);

#line  1781 "VBNET.ATG" 
				pexpr = new TypeOfIsExpression(expr, type); 
				break;
			}
			case 135: {
				ConditionalExpression(
#line  1782 "VBNET.ATG" 
out pexpr);
				break;
			}
			case 10: case 16: case 17: case 18: case 19: {
				XmlLiteralExpression(
#line  1783 "VBNET.ATG" 
out pexpr);
				break;
			}
			}
		} else if (StartOf(35)) {
			if (la.kind == 26) {
				lexer.NextToken();
				if (la.kind == 10) {
					lexer.NextToken();
					IdentifierOrKeyword(
#line  1789 "VBNET.ATG" 
out name);
					Expect(11);

#line  1790 "VBNET.ATG" 
					pexpr = new XmlMemberAccessExpression(null, XmlAxisType.Element, name, true) { StartLocation = startLocation, EndLocation = t.EndLocation }; 
				} else if (StartOf(33)) {
					IdentifierOrKeyword(
#line  1791 "VBNET.ATG" 
out name);

#line  1792 "VBNET.ATG" 
					pexpr = new MemberReferenceExpression(null, name) { StartLocation = startLocation, EndLocation = t.EndLocation }; 
				} else SynErr(283);
			} else if (la.kind == 29) {
				lexer.NextToken();
				IdentifierOrKeyword(
#line  1794 "VBNET.ATG" 
out name);

#line  1794 "VBNET.ATG" 
				pexpr = new BinaryOperatorExpression(null, BinaryOperatorType.DictionaryAccess, new PrimitiveExpression(name, name) { StartLocation = t.Location, EndLocation = t.EndLocation }); 
			} else {

#line  1795 "VBNET.ATG" 
				XmlAxisType axisType = XmlAxisType.Element; bool isXmlIdentifier = false; 
				if (la.kind == 27) {
					lexer.NextToken();

#line  1796 "VBNET.ATG" 
					axisType = XmlAxisType.Descendents; 
				} else if (la.kind == 28) {
					lexer.NextToken();

#line  1796 "VBNET.ATG" 
					axisType = XmlAxisType.Attribute; 
				} else SynErr(284);
				if (la.kind == 10) {
					lexer.NextToken();

#line  1797 "VBNET.ATG" 
					isXmlIdentifier = true; 
				}
				IdentifierOrKeyword(
#line  1797 "VBNET.ATG" 
out name);
				if (la.kind == 11) {
					lexer.NextToken();
				}

#line  1798 "VBNET.ATG" 
				pexpr = new XmlMemberAccessExpression(null, axisType, name, isXmlIdentifier); 
			}
		} else SynErr(285);

#line  1803 "VBNET.ATG" 
		if (pexpr != null) {
		pexpr.StartLocation = startLocation;
		pexpr.EndLocation = t.EndLocation;
		}
		
	}

	void TypeArgumentList(
#line  2805 "VBNET.ATG" 
List<TypeReference> typeArguments) {

#line  2807 "VBNET.ATG" 
		TypeReference typeref;
		
		TypeName(
#line  2809 "VBNET.ATG" 
out typeref);

#line  2809 "VBNET.ATG" 
		if (typeref != null) typeArguments.Add(typeref); 
		while (la.kind == 22) {
			lexer.NextToken();
			TypeName(
#line  2812 "VBNET.ATG" 
out typeref);

#line  2812 "VBNET.ATG" 
			if (typeref != null) typeArguments.Add(typeref); 
		}
	}

	void InvocationExpression(
#line  1924 "VBNET.ATG" 
ref Expression pexpr) {

#line  1925 "VBNET.ATG" 
		List<Expression> parameters = null; 
		Expect(37);

#line  1927 "VBNET.ATG" 
		Location start = t.Location; 
		ArgumentList(
#line  1928 "VBNET.ATG" 
out parameters);
		Expect(38);

#line  1931 "VBNET.ATG" 
		pexpr = new InvocationExpression(pexpr, parameters);
		

#line  1933 "VBNET.ATG" 
		pexpr.StartLocation = start; pexpr.EndLocation = t.Location; 
	}

	void PrimitiveTypeName(
#line  3676 "VBNET.ATG" 
out string type) {

#line  3677 "VBNET.ATG" 
		type = String.Empty; 
		switch (la.kind) {
		case 68: {
			lexer.NextToken();

#line  3678 "VBNET.ATG" 
			type = "System.Boolean"; 
			break;
		}
		case 99: {
			lexer.NextToken();

#line  3679 "VBNET.ATG" 
			type = "System.DateTime"; 
			break;
		}
		case 82: {
			lexer.NextToken();

#line  3680 "VBNET.ATG" 
			type = "System.Char"; 
			break;
		}
		case 208: {
			lexer.NextToken();

#line  3681 "VBNET.ATG" 
			type = "System.String"; 
			break;
		}
		case 100: {
			lexer.NextToken();

#line  3682 "VBNET.ATG" 
			type = "System.Decimal"; 
			break;
		}
		case 71: {
			lexer.NextToken();

#line  3683 "VBNET.ATG" 
			type = "System.Byte"; 
			break;
		}
		case 201: {
			lexer.NextToken();

#line  3684 "VBNET.ATG" 
			type = "System.Int16"; 
			break;
		}
		case 141: {
			lexer.NextToken();

#line  3685 "VBNET.ATG" 
			type = "System.Int32"; 
			break;
		}
		case 151: {
			lexer.NextToken();

#line  3686 "VBNET.ATG" 
			type = "System.Int64"; 
			break;
		}
		case 202: {
			lexer.NextToken();

#line  3687 "VBNET.ATG" 
			type = "System.Single"; 
			break;
		}
		case 109: {
			lexer.NextToken();

#line  3688 "VBNET.ATG" 
			type = "System.Double"; 
			break;
		}
		case 221: {
			lexer.NextToken();

#line  3689 "VBNET.ATG" 
			type = "System.UInt32"; 
			break;
		}
		case 222: {
			lexer.NextToken();

#line  3690 "VBNET.ATG" 
			type = "System.UInt64"; 
			break;
		}
		case 225: {
			lexer.NextToken();

#line  3691 "VBNET.ATG" 
			type = "System.UInt16"; 
			break;
		}
		case 196: {
			lexer.NextToken();

#line  3692 "VBNET.ATG" 
			type = "System.SByte"; 
			break;
		}
		default: SynErr(286); break;
		}
	}

	void CastTarget(
#line  1938 "VBNET.ATG" 
out TypeReference type) {

#line  1940 "VBNET.ATG" 
		type = null;
		
		switch (la.kind) {
		case 76: {
			lexer.NextToken();

#line  1942 "VBNET.ATG" 
			type = new TypeReference("System.Boolean", true); 
			break;
		}
		case 77: {
			lexer.NextToken();

#line  1943 "VBNET.ATG" 
			type = new TypeReference("System.Byte", true); 
			break;
		}
		case 90: {
			lexer.NextToken();

#line  1944 "VBNET.ATG" 
			type = new TypeReference("System.SByte", true); 
			break;
		}
		case 78: {
			lexer.NextToken();

#line  1945 "VBNET.ATG" 
			type = new TypeReference("System.Char", true); 
			break;
		}
		case 79: {
			lexer.NextToken();

#line  1946 "VBNET.ATG" 
			type = new TypeReference("System.DateTime", true); 
			break;
		}
		case 81: {
			lexer.NextToken();

#line  1947 "VBNET.ATG" 
			type = new TypeReference("System.Decimal", true); 
			break;
		}
		case 80: {
			lexer.NextToken();

#line  1948 "VBNET.ATG" 
			type = new TypeReference("System.Double", true); 
			break;
		}
		case 91: {
			lexer.NextToken();

#line  1949 "VBNET.ATG" 
			type = new TypeReference("System.Int16", true); 
			break;
		}
		case 83: {
			lexer.NextToken();

#line  1950 "VBNET.ATG" 
			type = new TypeReference("System.Int32", true); 
			break;
		}
		case 85: {
			lexer.NextToken();

#line  1951 "VBNET.ATG" 
			type = new TypeReference("System.Int64", true); 
			break;
		}
		case 97: {
			lexer.NextToken();

#line  1952 "VBNET.ATG" 
			type = new TypeReference("System.UInt16", true); 
			break;
		}
		case 95: {
			lexer.NextToken();

#line  1953 "VBNET.ATG" 
			type = new TypeReference("System.UInt32", true); 
			break;
		}
		case 96: {
			lexer.NextToken();

#line  1954 "VBNET.ATG" 
			type = new TypeReference("System.UInt64", true); 
			break;
		}
		case 86: {
			lexer.NextToken();

#line  1955 "VBNET.ATG" 
			type = new TypeReference("System.Object", true); 
			break;
		}
		case 92: {
			lexer.NextToken();

#line  1956 "VBNET.ATG" 
			type = new TypeReference("System.Single", true); 
			break;
		}
		case 93: {
			lexer.NextToken();

#line  1957 "VBNET.ATG" 
			type = new TypeReference("System.String", true); 
			break;
		}
		default: SynErr(287); break;
		}
	}

	void GetTypeTypeName(
#line  2704 "VBNET.ATG" 
out TypeReference typeref) {

#line  2705 "VBNET.ATG" 
		ArrayList rank = null; 
		NonArrayTypeName(
#line  2707 "VBNET.ATG" 
out typeref, true);
		ArrayTypeModifiers(
#line  2708 "VBNET.ATG" 
out rank);

#line  2709 "VBNET.ATG" 
		if (rank != null && typeref != null) {
		typeref.RankSpecifier = (int[])rank.ToArray(typeof(int));
		}
		
	}

	void ConditionalExpression(
#line  1890 "VBNET.ATG" 
out Expression expr) {

#line  1892 "VBNET.ATG" 
		ConditionalExpression conditionalExpression = new ConditionalExpression();
		BinaryOperatorExpression binaryOperatorExpression = new BinaryOperatorExpression();
		conditionalExpression.StartLocation = binaryOperatorExpression.StartLocation = la.Location;
		
		Expression condition = null;
		Expression trueExpr = null;
		Expression falseExpr = null;
		
		Expect(135);
		Expect(37);
		Expr(
#line  1901 "VBNET.ATG" 
out condition);
		Expect(22);
		Expr(
#line  1901 "VBNET.ATG" 
out trueExpr);
		if (la.kind == 22) {
			lexer.NextToken();
			Expr(
#line  1901 "VBNET.ATG" 
out falseExpr);
		}
		Expect(38);

#line  1903 "VBNET.ATG" 
		if(falseExpr != null)
		{
			conditionalExpression.Condition = condition;
			conditionalExpression.TrueExpression = trueExpr;
			conditionalExpression.FalseExpression = falseExpr;
			conditionalExpression.EndLocation = t.EndLocation;
			
			expr = conditionalExpression;
		}
		else
		{
			binaryOperatorExpression.Left = condition;
			binaryOperatorExpression.Right = trueExpr;
			binaryOperatorExpression.Op = BinaryOperatorType.NullCoalescing;
			binaryOperatorExpression.EndLocation = t.EndLocation;
			
			expr = binaryOperatorExpression;
		}
		
	}

	void XmlLiteralExpression(
#line  1810 "VBNET.ATG" 
out Expression pexpr) {

#line  1812 "VBNET.ATG" 
		List<XmlExpression> exprs = new List<XmlExpression>();
		XmlExpression currentExpression = null;
		
		if (StartOf(36)) {
			XmlContentExpression(
#line  1817 "VBNET.ATG" 
exprs);
			while (StartOf(36)) {
				XmlContentExpression(
#line  1817 "VBNET.ATG" 
exprs);
			}
			if (la.kind == 10) {
				XmlElement(
#line  1817 "VBNET.ATG" 
out currentExpression);

#line  1817 "VBNET.ATG" 
				exprs.Add(currentExpression); 
				while (StartOf(36)) {
					XmlContentExpression(
#line  1817 "VBNET.ATG" 
exprs);
				}
			}
		} else if (la.kind == 10) {
			XmlElement(
#line  1819 "VBNET.ATG" 
out currentExpression);

#line  1819 "VBNET.ATG" 
			exprs.Add(currentExpression); 
			while (StartOf(36)) {
				XmlContentExpression(
#line  1819 "VBNET.ATG" 
exprs);
			}
		} else SynErr(288);

#line  1822 "VBNET.ATG" 
		if (exprs.Count > 1) {
		pexpr = new XmlDocumentExpression() { Expressions = exprs };
		} else {
			pexpr = exprs[0];
		}
		
	}

	void XmlContentExpression(
#line  1830 "VBNET.ATG" 
List<XmlExpression> exprs) {

#line  1831 "VBNET.ATG" 
		XmlContentExpression expr = null; 
		if (la.kind == 16) {
			lexer.NextToken();

#line  1833 "VBNET.ATG" 
			expr = new XmlContentExpression(t.val, XmlContentType.Text); 
		} else if (la.kind == 18) {
			lexer.NextToken();

#line  1834 "VBNET.ATG" 
			expr = new XmlContentExpression(t.val, XmlContentType.CData); 
		} else if (la.kind == 17) {
			lexer.NextToken();

#line  1835 "VBNET.ATG" 
			expr = new XmlContentExpression(t.val, XmlContentType.Comment); 
		} else if (la.kind == 19) {
			lexer.NextToken();

#line  1836 "VBNET.ATG" 
			expr = new XmlContentExpression(t.val, XmlContentType.ProcessingInstruction); 
		} else SynErr(289);

#line  1839 "VBNET.ATG" 
		expr.StartLocation = t.Location;
		expr.EndLocation = t.EndLocation;
		exprs.Add(expr);
		
	}

	void XmlElement(
#line  1865 "VBNET.ATG" 
out XmlExpression expr) {

#line  1866 "VBNET.ATG" 
		XmlElementExpression el = new XmlElementExpression(); 
		Expect(10);

#line  1869 "VBNET.ATG" 
		el.StartLocation = t.Location; 
		if (la.kind == 12) {
			lexer.NextToken();

#line  1870 "VBNET.ATG" 
			Expression innerExpression; 
			Expr(
#line  1870 "VBNET.ATG" 
out innerExpression);
			Expect(13);

#line  1871 "VBNET.ATG" 
			el.NameExpression = new XmlEmbeddedExpression() { InlineVBExpression = innerExpression }; 
		} else if (StartOf(4)) {
			Identifier();

#line  1872 "VBNET.ATG" 
			el.XmlName = t.val; 
		} else SynErr(290);
		while (StartOf(37)) {
			XmlAttribute(
#line  1872 "VBNET.ATG" 
el.Attributes);
		}
		if (la.kind == 14) {
			lexer.NextToken();

#line  1873 "VBNET.ATG" 
			el.EndLocation = t.EndLocation; 
		} else if (la.kind == 11) {
			lexer.NextToken();
			while (StartOf(38)) {

#line  1873 "VBNET.ATG" 
				XmlExpression child; 
				XmlNestedContent(
#line  1873 "VBNET.ATG" 
out child);

#line  1873 "VBNET.ATG" 
				el.Children.Add(child); 
			}
			Expect(15);
			while (StartOf(39)) {
				lexer.NextToken();
			}
			Expect(11);

#line  1873 "VBNET.ATG" 
			el.EndLocation = t.EndLocation; 
		} else SynErr(291);

#line  1875 "VBNET.ATG" 
		expr = el; 
	}

	void XmlNestedContent(
#line  1845 "VBNET.ATG" 
out XmlExpression expr) {

#line  1846 "VBNET.ATG" 
		XmlExpression tmpExpr = null; Location start = la.Location; 
		switch (la.kind) {
		case 16: {
			lexer.NextToken();

#line  1849 "VBNET.ATG" 
			tmpExpr = new XmlContentExpression(t.val, XmlContentType.Text); 
			break;
		}
		case 18: {
			lexer.NextToken();

#line  1850 "VBNET.ATG" 
			tmpExpr = new XmlContentExpression(t.val, XmlContentType.CData); 
			break;
		}
		case 17: {
			lexer.NextToken();

#line  1851 "VBNET.ATG" 
			tmpExpr = new XmlContentExpression(t.val, XmlContentType.Comment); 
			break;
		}
		case 19: {
			lexer.NextToken();

#line  1852 "VBNET.ATG" 
			tmpExpr = new XmlContentExpression(t.val, XmlContentType.ProcessingInstruction); 
			break;
		}
		case 12: {
			lexer.NextToken();

#line  1853 "VBNET.ATG" 
			Expression innerExpression; 
			Expr(
#line  1853 "VBNET.ATG" 
out innerExpression);
			Expect(13);

#line  1853 "VBNET.ATG" 
			tmpExpr = new XmlEmbeddedExpression() { InlineVBExpression = innerExpression }; 
			break;
		}
		case 10: {
			XmlElement(
#line  1854 "VBNET.ATG" 
out tmpExpr);
			break;
		}
		default: SynErr(292); break;
		}

#line  1857 "VBNET.ATG" 
		if (tmpExpr.StartLocation.IsEmpty)
		tmpExpr.StartLocation = start;
		if (tmpExpr.EndLocation.IsEmpty)
			tmpExpr.EndLocation = t.EndLocation;
		expr = tmpExpr;
		
	}

	void XmlAttribute(
#line  1878 "VBNET.ATG" 
List<XmlExpression> attrs) {

#line  1879 "VBNET.ATG" 
		Location start = la.Location; 
		if (StartOf(4)) {
			Identifier();

#line  1881 "VBNET.ATG" 
			string name = t.val; 
			Expect(20);

#line  1882 "VBNET.ATG" 
			string literalValue = null; Expression expressionValue = null; bool useDoubleQuotes = false; 
			if (la.kind == 3) {
				lexer.NextToken();

#line  1883 "VBNET.ATG" 
				literalValue = t.literalValue.ToString(); useDoubleQuotes = t.val[0] == '"'; 
			} else if (la.kind == 12) {
				lexer.NextToken();
				Expr(
#line  1883 "VBNET.ATG" 
out expressionValue);
				Expect(13);
			} else SynErr(293);

#line  1884 "VBNET.ATG" 
			attrs.Add(new XmlAttributeExpression() { Name = name, ExpressionValue = expressionValue, LiteralValue = literalValue, UseDoubleQuotes = useDoubleQuotes, StartLocation = start, EndLocation = t.EndLocation }); 
		} else if (la.kind == 12) {
			lexer.NextToken();

#line  1886 "VBNET.ATG" 
			Expression innerExpression; 
			Expr(
#line  1886 "VBNET.ATG" 
out innerExpression);
			Expect(13);

#line  1887 "VBNET.ATG" 
			attrs.Add(new XmlEmbeddedExpression() { InlineVBExpression = innerExpression, StartLocation = start, EndLocation = t.EndLocation }); 
		} else SynErr(294);
	}

	void ArgumentList(
#line  2633 "VBNET.ATG" 
out List<Expression> arguments) {

#line  2635 "VBNET.ATG" 
		arguments = new List<Expression>();
		Expression expr = null;
		
		if (StartOf(24)) {
			Argument(
#line  2638 "VBNET.ATG" 
out expr);
		}
		while (la.kind == 22) {
			lexer.NextToken();

#line  2639 "VBNET.ATG" 
			arguments.Add(expr ?? Expression.Null); expr = null; 
			if (StartOf(24)) {
				Argument(
#line  2640 "VBNET.ATG" 
out expr);
			}

#line  2641 "VBNET.ATG" 
			if (expr == null) expr = Expression.Null; 
		}

#line  2643 "VBNET.ATG" 
		if (expr != null) arguments.Add(expr); 
	}

	void ConjunctionExpr(
#line  1977 "VBNET.ATG" 
out Expression outExpr) {

#line  1979 "VBNET.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		Location startLocation = la.Location;
		
		NotExpr(
#line  1983 "VBNET.ATG" 
out outExpr);
		while (la.kind == 60 || la.kind == 61) {
			if (la.kind == 60) {
				lexer.NextToken();

#line  1986 "VBNET.ATG" 
				op = BinaryOperatorType.BitwiseAnd; 
			} else {
				lexer.NextToken();

#line  1987 "VBNET.ATG" 
				op = BinaryOperatorType.LogicalAnd; 
			}
			NotExpr(
#line  1989 "VBNET.ATG" 
out expr);

#line  1989 "VBNET.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr) { StartLocation = startLocation, EndLocation = t.EndLocation };  
		}
	}

	void NotExpr(
#line  1993 "VBNET.ATG" 
out Expression outExpr) {

#line  1994 "VBNET.ATG" 
		UnaryOperatorType uop = UnaryOperatorType.None; 
		while (la.kind == 164) {
			lexer.NextToken();

#line  1995 "VBNET.ATG" 
			uop = UnaryOperatorType.Not; 
		}
		ComparisonExpr(
#line  1996 "VBNET.ATG" 
out outExpr);

#line  1997 "VBNET.ATG" 
		if (uop != UnaryOperatorType.None)
		outExpr = new UnaryOperatorExpression(outExpr, uop);
		
	}

	void ComparisonExpr(
#line  2002 "VBNET.ATG" 
out Expression outExpr) {

#line  2004 "VBNET.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		Location startLocation = la.Location;
		
		ShiftExpr(
#line  2008 "VBNET.ATG" 
out outExpr);
		while (StartOf(40)) {
			switch (la.kind) {
			case 40: {
				lexer.NextToken();

#line  2011 "VBNET.ATG" 
				op = BinaryOperatorType.LessThan; 
				break;
			}
			case 39: {
				lexer.NextToken();

#line  2012 "VBNET.ATG" 
				op = BinaryOperatorType.GreaterThan; 
				break;
			}
			case 43: {
				lexer.NextToken();

#line  2013 "VBNET.ATG" 
				op = BinaryOperatorType.LessThanOrEqual; 
				break;
			}
			case 42: {
				lexer.NextToken();

#line  2014 "VBNET.ATG" 
				op = BinaryOperatorType.GreaterThanOrEqual; 
				break;
			}
			case 41: {
				lexer.NextToken();

#line  2015 "VBNET.ATG" 
				op = BinaryOperatorType.InEquality; 
				break;
			}
			case 20: {
				lexer.NextToken();

#line  2016 "VBNET.ATG" 
				op = BinaryOperatorType.Equality; 
				break;
			}
			case 150: {
				lexer.NextToken();

#line  2017 "VBNET.ATG" 
				op = BinaryOperatorType.Like; 
				break;
			}
			case 144: {
				lexer.NextToken();

#line  2018 "VBNET.ATG" 
				op = BinaryOperatorType.ReferenceEquality; 
				break;
			}
			case 145: {
				lexer.NextToken();

#line  2019 "VBNET.ATG" 
				op = BinaryOperatorType.ReferenceInequality; 
				break;
			}
			}
			if (StartOf(41)) {
				ShiftExpr(
#line  2022 "VBNET.ATG" 
out expr);

#line  2022 "VBNET.ATG" 
				outExpr = new BinaryOperatorExpression(outExpr, op, expr) { StartLocation = startLocation, EndLocation = t.EndLocation };  
			} else if (la.kind == 164) {

#line  2023 "VBNET.ATG" 
				Location startLocation2 = la.Location; 
				lexer.NextToken();
				ShiftExpr(
#line  2025 "VBNET.ATG" 
out expr);

#line  2025 "VBNET.ATG" 
				outExpr = new BinaryOperatorExpression(outExpr, op, new UnaryOperatorExpression(expr, UnaryOperatorType.Not) { StartLocation = startLocation2, EndLocation = t.EndLocation }) { StartLocation = startLocation, EndLocation = t.EndLocation };  
			} else SynErr(295);
		}
	}

	void ShiftExpr(
#line  2030 "VBNET.ATG" 
out Expression outExpr) {

#line  2032 "VBNET.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		Location startLocation = la.Location;
		
		ConcatenationExpr(
#line  2036 "VBNET.ATG" 
out outExpr);
		while (la.kind == 44 || la.kind == 45) {
			if (la.kind == 44) {
				lexer.NextToken();

#line  2039 "VBNET.ATG" 
				op = BinaryOperatorType.ShiftLeft; 
			} else {
				lexer.NextToken();

#line  2040 "VBNET.ATG" 
				op = BinaryOperatorType.ShiftRight; 
			}
			ConcatenationExpr(
#line  2042 "VBNET.ATG" 
out expr);

#line  2042 "VBNET.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr) { StartLocation = startLocation, EndLocation = t.EndLocation };  
		}
	}

	void ConcatenationExpr(
#line  2046 "VBNET.ATG" 
out Expression outExpr) {

#line  2047 "VBNET.ATG" 
		Expression expr; Location startLocation = la.Location; 
		AdditiveExpr(
#line  2049 "VBNET.ATG" 
out outExpr);
		while (la.kind == 23) {
			lexer.NextToken();
			AdditiveExpr(
#line  2049 "VBNET.ATG" 
out expr);

#line  2049 "VBNET.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.Concat, expr) { StartLocation = startLocation, EndLocation = t.EndLocation };  
		}
	}

	void AdditiveExpr(
#line  2052 "VBNET.ATG" 
out Expression outExpr) {

#line  2054 "VBNET.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		Location startLocation = la.Location;
		
		ModuloExpr(
#line  2058 "VBNET.ATG" 
out outExpr);
		while (la.kind == 30 || la.kind == 31) {
			if (la.kind == 31) {
				lexer.NextToken();

#line  2061 "VBNET.ATG" 
				op = BinaryOperatorType.Add; 
			} else {
				lexer.NextToken();

#line  2062 "VBNET.ATG" 
				op = BinaryOperatorType.Subtract; 
			}
			ModuloExpr(
#line  2064 "VBNET.ATG" 
out expr);

#line  2064 "VBNET.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr) { StartLocation = startLocation, EndLocation = t.EndLocation };  
		}
	}

	void ModuloExpr(
#line  2068 "VBNET.ATG" 
out Expression outExpr) {

#line  2069 "VBNET.ATG" 
		Expression expr; Location startLocation = la.Location; 
		IntegerDivisionExpr(
#line  2071 "VBNET.ATG" 
out outExpr);
		while (la.kind == 154) {
			lexer.NextToken();
			IntegerDivisionExpr(
#line  2071 "VBNET.ATG" 
out expr);

#line  2071 "VBNET.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.Modulus, expr) { StartLocation = startLocation, EndLocation = t.EndLocation };  
		}
	}

	void IntegerDivisionExpr(
#line  2074 "VBNET.ATG" 
out Expression outExpr) {

#line  2075 "VBNET.ATG" 
		Expression expr; Location startLocation = la.Location; 
		MultiplicativeExpr(
#line  2077 "VBNET.ATG" 
out outExpr);
		while (la.kind == 25) {
			lexer.NextToken();
			MultiplicativeExpr(
#line  2077 "VBNET.ATG" 
out expr);

#line  2077 "VBNET.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.DivideInteger, expr) { StartLocation = startLocation, EndLocation = t.EndLocation };  
		}
	}

	void MultiplicativeExpr(
#line  2080 "VBNET.ATG" 
out Expression outExpr) {

#line  2082 "VBNET.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		Location startLocation = la.Location;
		
		UnaryExpr(
#line  2086 "VBNET.ATG" 
out outExpr);
		while (la.kind == 24 || la.kind == 34) {
			if (la.kind == 34) {
				lexer.NextToken();

#line  2089 "VBNET.ATG" 
				op = BinaryOperatorType.Multiply; 
			} else {
				lexer.NextToken();

#line  2090 "VBNET.ATG" 
				op = BinaryOperatorType.Divide; 
			}
			UnaryExpr(
#line  2092 "VBNET.ATG" 
out expr);

#line  2092 "VBNET.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr) { StartLocation = startLocation, EndLocation = t.EndLocation }; 
		}
	}

	void UnaryExpr(
#line  2096 "VBNET.ATG" 
out Expression uExpr) {

#line  2098 "VBNET.ATG" 
		Expression expr;
		UnaryOperatorType uop = UnaryOperatorType.None;
		Location startLocation = la.Location;
		bool isUOp = false;
		
		while (la.kind == 30 || la.kind == 31 || la.kind == 34) {
			if (la.kind == 31) {
				lexer.NextToken();

#line  2103 "VBNET.ATG" 
				uop = UnaryOperatorType.Plus; isUOp = true; 
			} else if (la.kind == 30) {
				lexer.NextToken();

#line  2104 "VBNET.ATG" 
				uop = UnaryOperatorType.Minus; isUOp = true; 
			} else {
				lexer.NextToken();

#line  2105 "VBNET.ATG" 
				uop = UnaryOperatorType.Dereference;  isUOp = true;
			}
		}
		ExponentiationExpr(
#line  2107 "VBNET.ATG" 
out expr);

#line  2109 "VBNET.ATG" 
		if (isUOp) {
		uExpr = new UnaryOperatorExpression(expr, uop) { StartLocation = startLocation, EndLocation = t.EndLocation };
		} else {
			uExpr = expr;
		}
		
	}

	void ExponentiationExpr(
#line  2117 "VBNET.ATG" 
out Expression outExpr) {

#line  2118 "VBNET.ATG" 
		Expression expr; Location startLocation = la.Location; 
		SimpleExpr(
#line  2120 "VBNET.ATG" 
out outExpr);
		while (la.kind == 32) {
			lexer.NextToken();
			SimpleExpr(
#line  2120 "VBNET.ATG" 
out expr);

#line  2120 "VBNET.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.Power, expr) { StartLocation = startLocation, EndLocation = t.EndLocation };  
		}
	}

	void NormalOrReDimArgumentList(
#line  2647 "VBNET.ATG" 
out List<Expression> arguments, out bool canBeNormal, out bool canBeRedim) {

#line  2649 "VBNET.ATG" 
		arguments = new List<Expression>();
		canBeNormal = true; canBeRedim = !IsNamedAssign();
		Expression expr = null;
		
		if (StartOf(24)) {
			Argument(
#line  2654 "VBNET.ATG" 
out expr);
			if (la.kind == 216) {
				lexer.NextToken();

#line  2655 "VBNET.ATG" 
				EnsureIsZero(expr); canBeNormal = false; 
				Expr(
#line  2656 "VBNET.ATG" 
out expr);
			}
		}
		while (la.kind == 22) {
			lexer.NextToken();

#line  2659 "VBNET.ATG" 
			if (expr == null) canBeRedim = false; 

#line  2660 "VBNET.ATG" 
			arguments.Add(expr ?? Expression.Null); expr = null; 

#line  2661 "VBNET.ATG" 
			canBeRedim &= !IsNamedAssign(); 
			if (StartOf(24)) {
				Argument(
#line  2662 "VBNET.ATG" 
out expr);
				if (la.kind == 216) {
					lexer.NextToken();

#line  2663 "VBNET.ATG" 
					EnsureIsZero(expr); canBeNormal = false; 
					Expr(
#line  2664 "VBNET.ATG" 
out expr);
				}
			}

#line  2666 "VBNET.ATG" 
			if (expr == null) { canBeRedim = false; expr = Expression.Null; } 
		}

#line  2668 "VBNET.ATG" 
		if (expr != null) arguments.Add(expr); else canBeRedim = false; 
	}

	void ArrayTypeModifiers(
#line  2778 "VBNET.ATG" 
out ArrayList arrayModifiers) {

#line  2780 "VBNET.ATG" 
		arrayModifiers = new ArrayList();
		int i = 0;
		
		while (
#line  2783 "VBNET.ATG" 
IsDims()) {
			Expect(37);
			if (la.kind == 22 || la.kind == 38) {
				RankList(
#line  2785 "VBNET.ATG" 
out i);
			}

#line  2787 "VBNET.ATG" 
			arrayModifiers.Add(i);
			
			Expect(38);
		}

#line  2792 "VBNET.ATG" 
		if(arrayModifiers.Count == 0) {
		 arrayModifiers = null;
		}
		
	}

	void MemberInitializer(
#line  2614 "VBNET.ATG" 
out MemberInitializerExpression memberInitializer) {

#line  2616 "VBNET.ATG" 
		memberInitializer = new MemberInitializerExpression();
		memberInitializer.StartLocation = la.Location;
		Expression initExpr = null;
		bool isKey = false;
		string name = null;
		
		if (la.kind == 147) {
			lexer.NextToken();

#line  2622 "VBNET.ATG" 
			isKey = true; 
		}
		Expect(26);
		IdentifierOrKeyword(
#line  2623 "VBNET.ATG" 
out name);
		Expect(20);
		Expr(
#line  2623 "VBNET.ATG" 
out initExpr);

#line  2625 "VBNET.ATG" 
		memberInitializer.Name = name;
		memberInitializer.Expression = initExpr;
		memberInitializer.IsKey = isKey;
		memberInitializer.EndLocation = t.EndLocation;
		
	}

	void SubLambdaExpression(
#line  2219 "VBNET.ATG" 
out LambdaExpression lambda) {

#line  2221 "VBNET.ATG" 
		lambda = new LambdaExpression();
		lambda.ReturnType = new TypeReference("System.Void", true);
		Expression inner = null;
		Statement statement = null;
		lambda.StartLocation = la.Location;
		
		Expect(210);
		if (la.kind == 37) {
			lexer.NextToken();
			if (StartOf(6)) {
				FormalParameterList(
#line  2228 "VBNET.ATG" 
lambda.Parameters);
			}
			Expect(38);
		}
		if (StartOf(42)) {
			if (StartOf(24)) {
				Expr(
#line  2231 "VBNET.ATG" 
out inner);

#line  2233 "VBNET.ATG" 
				lambda.ExpressionBody = inner;
				lambda.EndLocation = t.EndLocation; // la.Location?
				
			} else {
				EmbeddedStatement(
#line  2237 "VBNET.ATG" 
out statement);

#line  2239 "VBNET.ATG" 
				lambda.StatementBody = statement;
				lambda.EndLocation = t.EndLocation;
				
			}
		} else if (la.kind == 1) {
			lexer.NextToken();
			Block(
#line  2245 "VBNET.ATG" 
out statement);
			Expect(113);
			Expect(210);

#line  2248 "VBNET.ATG" 
			lambda.StatementBody = statement;
			lambda.EndLocation = t.EndLocation;
			
		} else SynErr(296);
	}

	void FunctionLambdaExpression(
#line  2254 "VBNET.ATG" 
out LambdaExpression lambda) {

#line  2256 "VBNET.ATG" 
		lambda = new LambdaExpression();
		TypeReference typeRef = null;
		Expression inner = null;
		Statement statement = null;
		lambda.StartLocation = la.Location;
		
		Expect(127);
		if (la.kind == 37) {
			lexer.NextToken();
			if (StartOf(6)) {
				FormalParameterList(
#line  2263 "VBNET.ATG" 
lambda.Parameters);
			}
			Expect(38);
		}
		if (la.kind == 63) {
			lexer.NextToken();
			TypeName(
#line  2264 "VBNET.ATG" 
out typeRef);

#line  2264 "VBNET.ATG" 
			lambda.ReturnType = typeRef; 
		}
		if (StartOf(42)) {
			if (StartOf(24)) {
				Expr(
#line  2267 "VBNET.ATG" 
out inner);

#line  2269 "VBNET.ATG" 
				lambda.ExpressionBody = inner;
				lambda.EndLocation = t.EndLocation; // la.Location?
				
			} else {
				EmbeddedStatement(
#line  2273 "VBNET.ATG" 
out statement);

#line  2275 "VBNET.ATG" 
				lambda.StatementBody = statement;
				lambda.EndLocation = t.EndLocation;
				
			}
		} else if (la.kind == 1) {
			lexer.NextToken();
			Block(
#line  2281 "VBNET.ATG" 
out statement);
			Expect(113);
			Expect(127);

#line  2284 "VBNET.ATG" 
			lambda.StatementBody = statement;
			lambda.EndLocation = t.EndLocation;
			
		} else SynErr(297);
	}

	void EmbeddedStatement(
#line  3064 "VBNET.ATG" 
out Statement statement) {

#line  3066 "VBNET.ATG" 
		Statement embeddedStatement = null;
		statement = null;
		Expression expr = null;
		string name = String.Empty;
		List<Expression> p = null;
		Location startLocation = la.Location;
		
		if (la.kind == 120) {
			lexer.NextToken();

#line  3074 "VBNET.ATG" 
			ExitType exitType = ExitType.None; 
			switch (la.kind) {
			case 210: {
				lexer.NextToken();

#line  3076 "VBNET.ATG" 
				exitType = ExitType.Sub; 
				break;
			}
			case 127: {
				lexer.NextToken();

#line  3078 "VBNET.ATG" 
				exitType = ExitType.Function; 
				break;
			}
			case 186: {
				lexer.NextToken();

#line  3080 "VBNET.ATG" 
				exitType = ExitType.Property; 
				break;
			}
			case 108: {
				lexer.NextToken();

#line  3082 "VBNET.ATG" 
				exitType = ExitType.Do; 
				break;
			}
			case 124: {
				lexer.NextToken();

#line  3084 "VBNET.ATG" 
				exitType = ExitType.For; 
				break;
			}
			case 218: {
				lexer.NextToken();

#line  3086 "VBNET.ATG" 
				exitType = ExitType.Try; 
				break;
			}
			case 231: {
				lexer.NextToken();

#line  3088 "VBNET.ATG" 
				exitType = ExitType.While; 
				break;
			}
			case 197: {
				lexer.NextToken();

#line  3090 "VBNET.ATG" 
				exitType = ExitType.Select; 
				break;
			}
			default: SynErr(298); break;
			}

#line  3092 "VBNET.ATG" 
			statement = new ExitStatement(exitType); 
		} else if (la.kind == 218) {
			TryStatement(
#line  3093 "VBNET.ATG" 
out statement);
		} else if (la.kind == 89) {
			lexer.NextToken();

#line  3094 "VBNET.ATG" 
			ContinueType continueType = ContinueType.None; 
			if (la.kind == 108 || la.kind == 124 || la.kind == 231) {
				if (la.kind == 108) {
					lexer.NextToken();

#line  3094 "VBNET.ATG" 
					continueType = ContinueType.Do; 
				} else if (la.kind == 124) {
					lexer.NextToken();

#line  3094 "VBNET.ATG" 
					continueType = ContinueType.For; 
				} else {
					lexer.NextToken();

#line  3094 "VBNET.ATG" 
					continueType = ContinueType.While; 
				}
			}

#line  3094 "VBNET.ATG" 
			statement = new ContinueStatement(continueType); 
		} else if (la.kind == 215) {
			lexer.NextToken();
			if (StartOf(24)) {
				Expr(
#line  3096 "VBNET.ATG" 
out expr);
			}

#line  3096 "VBNET.ATG" 
			statement = new ThrowStatement(expr); 
		} else if (la.kind == 195) {
			lexer.NextToken();
			if (StartOf(24)) {
				Expr(
#line  3098 "VBNET.ATG" 
out expr);
			}

#line  3098 "VBNET.ATG" 
			statement = new ReturnStatement(expr); 
		} else if (la.kind == 211) {
			lexer.NextToken();
			Expr(
#line  3100 "VBNET.ATG" 
out expr);
			EndOfStmt();
			Block(
#line  3100 "VBNET.ATG" 
out embeddedStatement);
			Expect(113);
			Expect(211);

#line  3101 "VBNET.ATG" 
			statement = new LockStatement(expr, embeddedStatement); 
		} else if (la.kind == 189) {
			lexer.NextToken();
			Identifier();

#line  3103 "VBNET.ATG" 
			name = t.val; 
			if (la.kind == 37) {
				lexer.NextToken();
				if (StartOf(43)) {
					ArgumentList(
#line  3104 "VBNET.ATG" 
out p);
				}
				Expect(38);
			}

#line  3106 "VBNET.ATG" 
			statement = new RaiseEventStatement(name, p);
			
		} else if (la.kind == 233) {
			WithStatement(
#line  3109 "VBNET.ATG" 
out statement);
		} else if (la.kind == 56) {
			lexer.NextToken();

#line  3111 "VBNET.ATG" 
			Expression handlerExpr = null; 
			Expr(
#line  3112 "VBNET.ATG" 
out expr);
			Expect(22);
			Expr(
#line  3112 "VBNET.ATG" 
out handlerExpr);

#line  3114 "VBNET.ATG" 
			statement = new AddHandlerStatement(expr, handlerExpr);
			
		} else if (la.kind == 193) {
			lexer.NextToken();

#line  3117 "VBNET.ATG" 
			Expression handlerExpr = null; 
			Expr(
#line  3118 "VBNET.ATG" 
out expr);
			Expect(22);
			Expr(
#line  3118 "VBNET.ATG" 
out handlerExpr);

#line  3120 "VBNET.ATG" 
			statement = new RemoveHandlerStatement(expr, handlerExpr);
			
		} else if (la.kind == 231) {
			lexer.NextToken();
			Expr(
#line  3123 "VBNET.ATG" 
out expr);
			EndOfStmt();
			Block(
#line  3124 "VBNET.ATG" 
out embeddedStatement);
			Expect(113);
			Expect(231);

#line  3126 "VBNET.ATG" 
			statement = new DoLoopStatement(expr, embeddedStatement, ConditionType.While, ConditionPosition.Start);
			
		} else if (la.kind == 108) {
			lexer.NextToken();

#line  3131 "VBNET.ATG" 
			ConditionType conditionType = ConditionType.None;
			
			if (la.kind == 224 || la.kind == 231) {
				WhileOrUntil(
#line  3134 "VBNET.ATG" 
out conditionType);
				Expr(
#line  3134 "VBNET.ATG" 
out expr);
				EndOfStmt();
				Block(
#line  3135 "VBNET.ATG" 
out embeddedStatement);
				Expect(152);

#line  3138 "VBNET.ATG" 
				statement = new DoLoopStatement(expr, 
				                               embeddedStatement, 
				                               conditionType == ConditionType.While ? ConditionType.DoWhile : conditionType, 
				                               ConditionPosition.Start);
				
			} else if (la.kind == 1 || la.kind == 21) {
				EndOfStmt();
				Block(
#line  3145 "VBNET.ATG" 
out embeddedStatement);
				Expect(152);
				if (la.kind == 224 || la.kind == 231) {
					WhileOrUntil(
#line  3146 "VBNET.ATG" 
out conditionType);
					Expr(
#line  3146 "VBNET.ATG" 
out expr);
				}

#line  3148 "VBNET.ATG" 
				statement = new DoLoopStatement(expr, embeddedStatement, conditionType, ConditionPosition.End);
				
			} else SynErr(299);
		} else if (la.kind == 124) {
			lexer.NextToken();

#line  3153 "VBNET.ATG" 
			Expression group = null;
			TypeReference typeReference;
			string        typeName;
			
			if (la.kind == 110) {
				lexer.NextToken();
				LoopControlVariable(
#line  3159 "VBNET.ATG" 
out typeReference, out typeName);
				Expect(138);
				Expr(
#line  3160 "VBNET.ATG" 
out group);
				EndOfStmt();
				Block(
#line  3161 "VBNET.ATG" 
out embeddedStatement);
				Expect(163);
				if (StartOf(24)) {
					Expr(
#line  3162 "VBNET.ATG" 
out expr);
				}

#line  3164 "VBNET.ATG" 
				statement = new ForeachStatement(typeReference, 
				                                typeName,
				                                group, 
				                                embeddedStatement, 
				                                expr);
				statement.StartLocation = startLocation;
				statement.EndLocation   = t.EndLocation;
				
				
			} else if (StartOf(44)) {

#line  3175 "VBNET.ATG" 
				Expression start = null;
				Expression end = null;
				Expression step = null;
				Expression variableExpr = null;
				Expression nextExpr = null;
				List<Expression> nextExpressions = null;
				
				if (
#line  3182 "VBNET.ATG" 
IsLoopVariableDeclaration()) {
					LoopControlVariable(
#line  3183 "VBNET.ATG" 
out typeReference, out typeName);
				} else {

#line  3185 "VBNET.ATG" 
					typeReference = null; typeName = null; 
					SimpleExpr(
#line  3186 "VBNET.ATG" 
out variableExpr);
				}
				Expect(20);
				Expr(
#line  3188 "VBNET.ATG" 
out start);
				Expect(216);
				Expr(
#line  3188 "VBNET.ATG" 
out end);
				if (la.kind == 205) {
					lexer.NextToken();
					Expr(
#line  3188 "VBNET.ATG" 
out step);
				}
				EndOfStmt();
				Block(
#line  3189 "VBNET.ATG" 
out embeddedStatement);
				Expect(163);
				if (StartOf(24)) {
					Expr(
#line  3192 "VBNET.ATG" 
out nextExpr);

#line  3194 "VBNET.ATG" 
					nextExpressions = new List<Expression>();
					nextExpressions.Add(nextExpr);
					
					while (la.kind == 22) {
						lexer.NextToken();
						Expr(
#line  3197 "VBNET.ATG" 
out nextExpr);

#line  3197 "VBNET.ATG" 
						nextExpressions.Add(nextExpr); 
					}
				}

#line  3200 "VBNET.ATG" 
				statement = new ForNextStatement {
				TypeReference = typeReference,
				VariableName = typeName, 
				LoopVariableExpression = variableExpr,
				Start = start, 
				End = end, 
				Step = step, 
				EmbeddedStatement = embeddedStatement, 
				NextExpressions = nextExpressions
				};
				
			} else SynErr(300);
		} else if (la.kind == 118) {
			lexer.NextToken();
			Expr(
#line  3213 "VBNET.ATG" 
out expr);

#line  3213 "VBNET.ATG" 
			statement = new ErrorStatement(expr); 
		} else if (la.kind == 191) {
			lexer.NextToken();

#line  3215 "VBNET.ATG" 
			bool isPreserve = false; 
			if (la.kind == 184) {
				lexer.NextToken();

#line  3215 "VBNET.ATG" 
				isPreserve = true; 
			}
			ReDimClause(
#line  3216 "VBNET.ATG" 
out expr);

#line  3218 "VBNET.ATG" 
			ReDimStatement reDimStatement = new ReDimStatement(isPreserve);
			statement = reDimStatement;
			SafeAdd(reDimStatement, reDimStatement.ReDimClauses, expr as InvocationExpression);
			
			while (la.kind == 22) {
				lexer.NextToken();
				ReDimClause(
#line  3222 "VBNET.ATG" 
out expr);

#line  3223 "VBNET.ATG" 
				SafeAdd(reDimStatement, reDimStatement.ReDimClauses, expr as InvocationExpression); 
			}
		} else if (la.kind == 117) {
			lexer.NextToken();
			Expr(
#line  3227 "VBNET.ATG" 
out expr);

#line  3229 "VBNET.ATG" 
			EraseStatement eraseStatement = new EraseStatement();
			if (expr != null) { SafeAdd(eraseStatement, eraseStatement.Expressions, expr);}
			
			while (la.kind == 22) {
				lexer.NextToken();
				Expr(
#line  3232 "VBNET.ATG" 
out expr);

#line  3232 "VBNET.ATG" 
				if (expr != null) { SafeAdd(eraseStatement, eraseStatement.Expressions, expr); }
			}

#line  3233 "VBNET.ATG" 
			statement = eraseStatement; 
		} else if (la.kind == 206) {
			lexer.NextToken();

#line  3235 "VBNET.ATG" 
			statement = new StopStatement(); 
		} else if (
#line  3237 "VBNET.ATG" 
la.kind == Tokens.If) {
			Expect(135);

#line  3238 "VBNET.ATG" 
			Location ifStartLocation = t.Location; 
			Expr(
#line  3238 "VBNET.ATG" 
out expr);
			if (la.kind == 214) {
				lexer.NextToken();
			}
			if (la.kind == 1 || la.kind == 21) {
				EndOfStmt();
				Block(
#line  3241 "VBNET.ATG" 
out embeddedStatement);

#line  3243 "VBNET.ATG" 
				IfElseStatement ifStatement = new IfElseStatement(expr, embeddedStatement);
				ifStatement.StartLocation = ifStartLocation;
				Location elseIfStart;
				
				while (la.kind == 112 || 
#line  3249 "VBNET.ATG" 
IsElseIf()) {
					if (
#line  3249 "VBNET.ATG" 
IsElseIf()) {
						Expect(111);

#line  3249 "VBNET.ATG" 
						elseIfStart = t.Location; 
						Expect(135);
					} else {
						lexer.NextToken();

#line  3250 "VBNET.ATG" 
						elseIfStart = t.Location; 
					}

#line  3252 "VBNET.ATG" 
					Expression condition = null; Statement block = null; 
					Expr(
#line  3253 "VBNET.ATG" 
out condition);
					if (la.kind == 214) {
						lexer.NextToken();
					}
					EndOfStmt();
					Block(
#line  3254 "VBNET.ATG" 
out block);

#line  3256 "VBNET.ATG" 
					ElseIfSection elseIfSection = new ElseIfSection(condition, block);
					elseIfSection.StartLocation = elseIfStart;
					elseIfSection.EndLocation = t.Location;
					elseIfSection.Parent = ifStatement;
					ifStatement.ElseIfSections.Add(elseIfSection);
					
				}
				if (la.kind == 111) {
					lexer.NextToken();
					if (la.kind == 1 || la.kind == 21) {
						EndOfStmt();
					}
					Block(
#line  3265 "VBNET.ATG" 
out embeddedStatement);

#line  3267 "VBNET.ATG" 
					ifStatement.FalseStatement.Add(embeddedStatement);
					
				}
				Expect(113);
				Expect(135);

#line  3271 "VBNET.ATG" 
				ifStatement.EndLocation = t.Location;
				statement = ifStatement;
				
			} else if (StartOf(45)) {

#line  3276 "VBNET.ATG" 
				IfElseStatement ifStatement = new IfElseStatement(expr);
				ifStatement.StartLocation = ifStartLocation;
				
				SingleLineStatementList(
#line  3279 "VBNET.ATG" 
ifStatement.TrueStatement);
				if (la.kind == 111) {
					lexer.NextToken();
					if (StartOf(45)) {
						SingleLineStatementList(
#line  3282 "VBNET.ATG" 
ifStatement.FalseStatement);
					}
				}

#line  3284 "VBNET.ATG" 
				ifStatement.EndLocation = t.Location; statement = ifStatement; 
			} else SynErr(301);
		} else if (la.kind == 197) {
			lexer.NextToken();
			if (la.kind == 74) {
				lexer.NextToken();
			}
			Expr(
#line  3287 "VBNET.ATG" 
out expr);
			EndOfStmt();

#line  3288 "VBNET.ATG" 
			List<SwitchSection> selectSections = new List<SwitchSection>();
			Statement block = null;
			
			while (la.kind == 74) {

#line  3292 "VBNET.ATG" 
				List<CaseLabel> caseClauses = null; Location caseLocation = la.Location; 
				lexer.NextToken();
				CaseClauses(
#line  3293 "VBNET.ATG" 
out caseClauses);
				if (
#line  3293 "VBNET.ATG" 
IsNotStatementSeparator()) {
					lexer.NextToken();
				}
				EndOfStmt();

#line  3295 "VBNET.ATG" 
				SwitchSection selectSection = new SwitchSection(caseClauses);
				selectSection.StartLocation = caseLocation;
				
				Block(
#line  3298 "VBNET.ATG" 
out block);

#line  3300 "VBNET.ATG" 
				selectSection.Children = block.Children;
				selectSection.EndLocation = t.EndLocation;
				selectSections.Add(selectSection);
				
			}

#line  3306 "VBNET.ATG" 
			statement = new SwitchStatement(expr, selectSections);
			
			Expect(113);
			Expect(197);
		} else if (la.kind == 171) {

#line  3309 "VBNET.ATG" 
			OnErrorStatement onErrorStatement = null; 
			OnErrorStatement(
#line  3310 "VBNET.ATG" 
out onErrorStatement);

#line  3310 "VBNET.ATG" 
			statement = onErrorStatement; 
		} else if (la.kind == 132) {

#line  3311 "VBNET.ATG" 
			GotoStatement goToStatement = null; 
			GotoStatement(
#line  3312 "VBNET.ATG" 
out goToStatement);

#line  3312 "VBNET.ATG" 
			statement = goToStatement; 
		} else if (la.kind == 194) {

#line  3313 "VBNET.ATG" 
			ResumeStatement resumeStatement = null; 
			ResumeStatement(
#line  3314 "VBNET.ATG" 
out resumeStatement);

#line  3314 "VBNET.ATG" 
			statement = resumeStatement; 
		} else if (StartOf(44)) {

#line  3317 "VBNET.ATG" 
			Expression val = null;
			AssignmentOperatorType op;
			Location startLoc = la.Location;
			
			bool mustBeAssignment = la.kind == Tokens.Plus  || la.kind == Tokens.Minus ||
			                        la.kind == Tokens.Not   || la.kind == Tokens.Times;
			
			SimpleExpr(
#line  3324 "VBNET.ATG" 
out expr);
			if (StartOf(46)) {
				AssignmentOperator(
#line  3326 "VBNET.ATG" 
out op);
				Expr(
#line  3326 "VBNET.ATG" 
out val);

#line  3328 "VBNET.ATG" 
				expr = new AssignmentExpression(expr, op, val);
				expr.StartLocation = startLoc;
				expr.EndLocation = t.EndLocation;
				
			} else if (StartOf(47)) {

#line  3332 "VBNET.ATG" 
				if (mustBeAssignment) Error("error in assignment."); 
			} else SynErr(302);

#line  3335 "VBNET.ATG" 
			// a field reference expression that stands alone is a
			// invocation expression without parantheses and arguments
			if(expr is MemberReferenceExpression || expr is IdentifierExpression) {
				Location endLocation = expr.EndLocation;
				expr = new InvocationExpression(expr);
				expr.StartLocation = startLoc;
				expr.EndLocation = endLocation;
			}
			statement = new ExpressionStatement(expr);
			
		} else if (la.kind == 73) {
			lexer.NextToken();
			SimpleExpr(
#line  3345 "VBNET.ATG" 
out expr);

#line  3345 "VBNET.ATG" 
			statement = new ExpressionStatement(expr); 
		} else if (la.kind == 226) {
			lexer.NextToken();

#line  3347 "VBNET.ATG" 
			Statement block;  
			if (
#line  3348 "VBNET.ATG" 
Peek(1).kind == Tokens.As) {

#line  3349 "VBNET.ATG" 
				LocalVariableDeclaration resourceAquisition = new LocalVariableDeclaration(Modifiers.None); 
				VariableDeclarator(
#line  3350 "VBNET.ATG" 
resourceAquisition.Variables);
				while (la.kind == 22) {
					lexer.NextToken();
					VariableDeclarator(
#line  3352 "VBNET.ATG" 
resourceAquisition.Variables);
				}
				Block(
#line  3354 "VBNET.ATG" 
out block);

#line  3356 "VBNET.ATG" 
				statement = new UsingStatement(resourceAquisition, block);
				
			} else if (StartOf(24)) {
				Expr(
#line  3358 "VBNET.ATG" 
out expr);
				Block(
#line  3359 "VBNET.ATG" 
out block);

#line  3360 "VBNET.ATG" 
				statement = new UsingStatement(new ExpressionStatement(expr), block); 
			} else SynErr(303);
			Expect(113);
			Expect(226);
		} else if (StartOf(48)) {
			LocalDeclarationStatement(
#line  3363 "VBNET.ATG" 
out statement);
		} else SynErr(304);

#line  3366 "VBNET.ATG" 
		if (statement != null) {
		statement.StartLocation = startLocation;
		statement.EndLocation = t.EndLocation;
		}
		
	}

	void FromOrAggregateQueryOperator(
#line  2303 "VBNET.ATG" 
List<QueryExpressionClause> middleClauses) {

#line  2305 "VBNET.ATG" 
		QueryExpressionFromClause fromClause = null;
		QueryExpressionAggregateClause aggregateClause = null;
		
		if (la.kind == 126) {
			FromQueryOperator(
#line  2308 "VBNET.ATG" 
out fromClause);

#line  2309 "VBNET.ATG" 
			middleClauses.Add(fromClause); 
		} else if (la.kind == 58) {
			AggregateQueryOperator(
#line  2310 "VBNET.ATG" 
out aggregateClause);

#line  2311 "VBNET.ATG" 
			middleClauses.Add(aggregateClause); 
		} else SynErr(305);
	}

	void QueryOperator(
#line  2314 "VBNET.ATG" 
List<QueryExpressionClause> middleClauses) {

#line  2316 "VBNET.ATG" 
		QueryExpressionJoinVBClause joinClause = null;
		QueryExpressionGroupVBClause groupByClause = null;
		QueryExpressionPartitionVBClause partitionClause = null;
		QueryExpressionGroupJoinVBClause groupJoinClause = null;
		QueryExpressionFromClause fromClause = null;
		QueryExpressionAggregateClause aggregateClause = null;
		
		if (la.kind == 126) {
			FromQueryOperator(
#line  2323 "VBNET.ATG" 
out fromClause);

#line  2324 "VBNET.ATG" 
			middleClauses.Add(fromClause); 
		} else if (la.kind == 58) {
			AggregateQueryOperator(
#line  2325 "VBNET.ATG" 
out aggregateClause);

#line  2326 "VBNET.ATG" 
			middleClauses.Add(aggregateClause); 
		} else if (la.kind == 197) {
			SelectQueryOperator(
#line  2327 "VBNET.ATG" 
middleClauses);
		} else if (la.kind == 107) {
			DistinctQueryOperator(
#line  2328 "VBNET.ATG" 
middleClauses);
		} else if (la.kind == 230) {
			WhereQueryOperator(
#line  2329 "VBNET.ATG" 
middleClauses);
		} else if (la.kind == 176) {
			OrderByQueryOperator(
#line  2330 "VBNET.ATG" 
middleClauses);
		} else if (la.kind == 203 || la.kind == 212) {
			PartitionQueryOperator(
#line  2331 "VBNET.ATG" 
out partitionClause);

#line  2332 "VBNET.ATG" 
			middleClauses.Add(partitionClause); 
		} else if (la.kind == 148) {
			LetQueryOperator(
#line  2333 "VBNET.ATG" 
middleClauses);
		} else if (la.kind == 146) {
			JoinQueryOperator(
#line  2334 "VBNET.ATG" 
out joinClause);

#line  2335 "VBNET.ATG" 
			middleClauses.Add(joinClause); 
		} else if (
#line  2336 "VBNET.ATG" 
la.kind == Tokens.Group && Peek(1).kind == Tokens.Join) {
			GroupJoinQueryOperator(
#line  2336 "VBNET.ATG" 
out groupJoinClause);

#line  2337 "VBNET.ATG" 
			middleClauses.Add(groupJoinClause); 
		} else if (la.kind == 133) {
			GroupByQueryOperator(
#line  2338 "VBNET.ATG" 
out groupByClause);

#line  2339 "VBNET.ATG" 
			middleClauses.Add(groupByClause); 
		} else SynErr(306);
	}

	void FromQueryOperator(
#line  2414 "VBNET.ATG" 
out QueryExpressionFromClause fromClause) {

#line  2416 "VBNET.ATG" 
		fromClause = new QueryExpressionFromClause();
		fromClause.StartLocation = la.Location;
		
		Expect(126);
		CollectionRangeVariableDeclarationList(
#line  2419 "VBNET.ATG" 
fromClause.Sources);

#line  2421 "VBNET.ATG" 
		fromClause.EndLocation = t.EndLocation;
		
	}

	void AggregateQueryOperator(
#line  2483 "VBNET.ATG" 
out QueryExpressionAggregateClause aggregateClause) {

#line  2485 "VBNET.ATG" 
		aggregateClause = new QueryExpressionAggregateClause();
		aggregateClause.IntoVariables = new List<ExpressionRangeVariable>();
		aggregateClause.StartLocation = la.Location;
		CollectionRangeVariable source;
		
		Expect(58);
		CollectionRangeVariableDeclaration(
#line  2490 "VBNET.ATG" 
out source);

#line  2492 "VBNET.ATG" 
		aggregateClause.Source = source;
		
		while (StartOf(31)) {
			QueryOperator(
#line  2495 "VBNET.ATG" 
aggregateClause.MiddleClauses);
		}
		Expect(143);
		ExpressionRangeVariableDeclarationList(
#line  2497 "VBNET.ATG" 
aggregateClause.IntoVariables);

#line  2499 "VBNET.ATG" 
		aggregateClause.EndLocation = t.EndLocation;
		
	}

	void SelectQueryOperator(
#line  2425 "VBNET.ATG" 
List<QueryExpressionClause> middleClauses) {

#line  2427 "VBNET.ATG" 
		QueryExpressionSelectVBClause selectClause = new QueryExpressionSelectVBClause();
		selectClause.StartLocation = la.Location;
		
		Expect(197);
		ExpressionRangeVariableDeclarationList(
#line  2430 "VBNET.ATG" 
selectClause.Variables);

#line  2432 "VBNET.ATG" 
		selectClause.EndLocation = t.Location;
		middleClauses.Add(selectClause);
		
	}

	void DistinctQueryOperator(
#line  2437 "VBNET.ATG" 
List<QueryExpressionClause> middleClauses) {

#line  2439 "VBNET.ATG" 
		QueryExpressionDistinctClause distinctClause = new QueryExpressionDistinctClause();
		distinctClause.StartLocation = la.Location;
		
		Expect(107);

#line  2444 "VBNET.ATG" 
		distinctClause.EndLocation = t.EndLocation;
		middleClauses.Add(distinctClause);
		
	}

	void WhereQueryOperator(
#line  2449 "VBNET.ATG" 
List<QueryExpressionClause> middleClauses) {

#line  2451 "VBNET.ATG" 
		QueryExpressionWhereClause whereClause = new QueryExpressionWhereClause();
		whereClause.StartLocation = la.Location;
		Expression operand = null;
		
		Expect(230);
		Expr(
#line  2455 "VBNET.ATG" 
out operand);

#line  2457 "VBNET.ATG" 
		whereClause.Condition = operand;
		whereClause.EndLocation = t.EndLocation;
		
		middleClauses.Add(whereClause);
		
	}

	void OrderByQueryOperator(
#line  2342 "VBNET.ATG" 
List<QueryExpressionClause> middleClauses) {

#line  2344 "VBNET.ATG" 
		QueryExpressionOrderClause orderClause = new QueryExpressionOrderClause();
		orderClause.StartLocation = la.Location;
		List<QueryExpressionOrdering> orderings = null;
		
		Expect(176);
		Expect(70);
		OrderExpressionList(
#line  2348 "VBNET.ATG" 
out orderings);

#line  2350 "VBNET.ATG" 
		orderClause.Orderings = orderings;
		orderClause.EndLocation = t.EndLocation;
		middleClauses.Add(orderClause);
		
	}

	void PartitionQueryOperator(
#line  2464 "VBNET.ATG" 
out QueryExpressionPartitionVBClause partitionClause) {

#line  2466 "VBNET.ATG" 
		partitionClause = new QueryExpressionPartitionVBClause();
		partitionClause.StartLocation = la.Location;
		Expression expr = null;
		
		if (la.kind == 212) {
			lexer.NextToken();

#line  2471 "VBNET.ATG" 
			partitionClause.PartitionType = QueryExpressionPartitionType.Take; 
			if (la.kind == 231) {
				lexer.NextToken();

#line  2472 "VBNET.ATG" 
				partitionClause.PartitionType = QueryExpressionPartitionType.TakeWhile; 
			}
		} else if (la.kind == 203) {
			lexer.NextToken();

#line  2473 "VBNET.ATG" 
			partitionClause.PartitionType = QueryExpressionPartitionType.Skip; 
			if (la.kind == 231) {
				lexer.NextToken();

#line  2474 "VBNET.ATG" 
				partitionClause.PartitionType = QueryExpressionPartitionType.SkipWhile; 
			}
		} else SynErr(307);
		Expr(
#line  2476 "VBNET.ATG" 
out expr);

#line  2478 "VBNET.ATG" 
		partitionClause.Expression = expr;
		partitionClause.EndLocation = t.EndLocation;
		
	}

	void LetQueryOperator(
#line  2503 "VBNET.ATG" 
List<QueryExpressionClause> middleClauses) {

#line  2505 "VBNET.ATG" 
		QueryExpressionLetVBClause letClause = new QueryExpressionLetVBClause();
		letClause.StartLocation = la.Location;
		
		Expect(148);
		ExpressionRangeVariableDeclarationList(
#line  2508 "VBNET.ATG" 
letClause.Variables);

#line  2510 "VBNET.ATG" 
		letClause.EndLocation = t.EndLocation;
		middleClauses.Add(letClause);
		
	}

	void JoinQueryOperator(
#line  2547 "VBNET.ATG" 
out QueryExpressionJoinVBClause joinClause) {

#line  2549 "VBNET.ATG" 
		joinClause = new QueryExpressionJoinVBClause();
		joinClause.StartLocation = la.Location;
		CollectionRangeVariable joinVariable = null;
		QueryExpressionJoinVBClause subJoin = null;
		QueryExpressionJoinConditionVB condition = null;
		
		
		Expect(146);
		CollectionRangeVariableDeclaration(
#line  2556 "VBNET.ATG" 
out joinVariable);

#line  2557 "VBNET.ATG" 
		joinClause.JoinVariable = joinVariable; 
		if (la.kind == 146) {
			JoinQueryOperator(
#line  2559 "VBNET.ATG" 
out subJoin);

#line  2560 "VBNET.ATG" 
			joinClause.SubJoin = subJoin; 
		}
		Expect(171);
		JoinCondition(
#line  2563 "VBNET.ATG" 
out condition);

#line  2564 "VBNET.ATG" 
		SafeAdd(joinClause, joinClause.Conditions, condition); 
		while (la.kind == 60) {
			lexer.NextToken();
			JoinCondition(
#line  2566 "VBNET.ATG" 
out condition);

#line  2567 "VBNET.ATG" 
			SafeAdd(joinClause, joinClause.Conditions, condition); 
		}

#line  2570 "VBNET.ATG" 
		joinClause.EndLocation = t.EndLocation;
		
	}

	void GroupJoinQueryOperator(
#line  2400 "VBNET.ATG" 
out QueryExpressionGroupJoinVBClause groupJoinClause) {

#line  2402 "VBNET.ATG" 
		groupJoinClause = new QueryExpressionGroupJoinVBClause();
		groupJoinClause.StartLocation = la.Location;
		QueryExpressionJoinVBClause joinClause = null;
		
		Expect(133);
		JoinQueryOperator(
#line  2406 "VBNET.ATG" 
out joinClause);
		Expect(143);
		ExpressionRangeVariableDeclarationList(
#line  2407 "VBNET.ATG" 
groupJoinClause.IntoVariables);

#line  2409 "VBNET.ATG" 
		groupJoinClause.JoinClause = joinClause;
		groupJoinClause.EndLocation = t.EndLocation;
		
	}

	void GroupByQueryOperator(
#line  2387 "VBNET.ATG" 
out QueryExpressionGroupVBClause groupByClause) {

#line  2389 "VBNET.ATG" 
		groupByClause = new QueryExpressionGroupVBClause();
		groupByClause.StartLocation = la.Location;
		
		Expect(133);
		ExpressionRangeVariableDeclarationList(
#line  2392 "VBNET.ATG" 
groupByClause.GroupVariables);
		Expect(70);
		ExpressionRangeVariableDeclarationList(
#line  2393 "VBNET.ATG" 
groupByClause.ByVariables);
		Expect(143);
		ExpressionRangeVariableDeclarationList(
#line  2394 "VBNET.ATG" 
groupByClause.IntoVariables);

#line  2396 "VBNET.ATG" 
		groupByClause.EndLocation = t.EndLocation;
		
	}

	void OrderExpressionList(
#line  2356 "VBNET.ATG" 
out List<QueryExpressionOrdering> orderings) {

#line  2358 "VBNET.ATG" 
		orderings = new List<QueryExpressionOrdering>();
		QueryExpressionOrdering ordering = null;
		
		OrderExpression(
#line  2361 "VBNET.ATG" 
out ordering);

#line  2362 "VBNET.ATG" 
		orderings.Add(ordering); 
		while (la.kind == 22) {
			lexer.NextToken();
			OrderExpression(
#line  2364 "VBNET.ATG" 
out ordering);

#line  2365 "VBNET.ATG" 
			orderings.Add(ordering); 
		}
	}

	void OrderExpression(
#line  2369 "VBNET.ATG" 
out QueryExpressionOrdering ordering) {

#line  2371 "VBNET.ATG" 
		ordering = new QueryExpressionOrdering();
		ordering.StartLocation = la.Location;
		ordering.Direction = QueryExpressionOrderingDirection.None;
		Expression orderExpr = null;
		
		Expr(
#line  2376 "VBNET.ATG" 
out orderExpr);

#line  2378 "VBNET.ATG" 
		ordering.Criteria = orderExpr;
		
		if (la.kind == 64 || la.kind == 104) {
			if (la.kind == 64) {
				lexer.NextToken();

#line  2381 "VBNET.ATG" 
				ordering.Direction = QueryExpressionOrderingDirection.Ascending; 
			} else {
				lexer.NextToken();

#line  2382 "VBNET.ATG" 
				ordering.Direction = QueryExpressionOrderingDirection.Descending; 
			}
		}

#line  2384 "VBNET.ATG" 
		ordering.EndLocation = t.EndLocation; 
	}

	void ExpressionRangeVariableDeclarationList(
#line  2515 "VBNET.ATG" 
List<ExpressionRangeVariable> variables) {

#line  2517 "VBNET.ATG" 
		ExpressionRangeVariable variable = null;
		
		ExpressionRangeVariableDeclaration(
#line  2519 "VBNET.ATG" 
out variable);

#line  2520 "VBNET.ATG" 
		variables.Add(variable); 
		while (la.kind == 22) {
			lexer.NextToken();
			ExpressionRangeVariableDeclaration(
#line  2521 "VBNET.ATG" 
out variable);

#line  2521 "VBNET.ATG" 
			variables.Add(variable); 
		}
	}

	void CollectionRangeVariableDeclarationList(
#line  2574 "VBNET.ATG" 
List<CollectionRangeVariable> rangeVariables) {

#line  2575 "VBNET.ATG" 
		CollectionRangeVariable variableDeclaration; 
		CollectionRangeVariableDeclaration(
#line  2577 "VBNET.ATG" 
out variableDeclaration);

#line  2578 "VBNET.ATG" 
		rangeVariables.Add(variableDeclaration); 
		while (la.kind == 22) {
			lexer.NextToken();
			CollectionRangeVariableDeclaration(
#line  2579 "VBNET.ATG" 
out variableDeclaration);

#line  2579 "VBNET.ATG" 
			rangeVariables.Add(variableDeclaration); 
		}
	}

	void CollectionRangeVariableDeclaration(
#line  2582 "VBNET.ATG" 
out CollectionRangeVariable rangeVariable) {

#line  2584 "VBNET.ATG" 
		rangeVariable = new CollectionRangeVariable();
		rangeVariable.StartLocation = la.Location;
		TypeReference typeName = null;
		Expression inExpr = null;
		
		Identifier();

#line  2589 "VBNET.ATG" 
		rangeVariable.Identifier = t.val; 
		if (la.kind == 63) {
			lexer.NextToken();
			TypeName(
#line  2590 "VBNET.ATG" 
out typeName);

#line  2590 "VBNET.ATG" 
			rangeVariable.Type = typeName; 
		}
		Expect(138);
		Expr(
#line  2591 "VBNET.ATG" 
out inExpr);

#line  2593 "VBNET.ATG" 
		rangeVariable.Expression = inExpr;
		rangeVariable.EndLocation = t.EndLocation;
		
	}

	void ExpressionRangeVariableDeclaration(
#line  2524 "VBNET.ATG" 
out ExpressionRangeVariable variable) {

#line  2526 "VBNET.ATG" 
		variable = new ExpressionRangeVariable();
		variable.StartLocation = la.Location;
		Expression rhs = null;
		TypeReference typeName = null;
		
		if (
#line  2532 "VBNET.ATG" 
IsIdentifiedExpressionRange()) {
			Identifier();

#line  2533 "VBNET.ATG" 
			variable.Identifier = t.val; 
			if (la.kind == 63) {
				lexer.NextToken();
				TypeName(
#line  2535 "VBNET.ATG" 
out typeName);

#line  2536 "VBNET.ATG" 
				variable.Type = typeName; 
			}
			Expect(20);
		}
		Expr(
#line  2540 "VBNET.ATG" 
out rhs);

#line  2542 "VBNET.ATG" 
		variable.Expression = rhs;
		variable.EndLocation = t.EndLocation;
		
	}

	void JoinCondition(
#line  2598 "VBNET.ATG" 
out QueryExpressionJoinConditionVB condition) {

#line  2600 "VBNET.ATG" 
		condition = new QueryExpressionJoinConditionVB();
		condition.StartLocation = la.Location;
		
		Expression lhs = null;
		Expression rhs = null;
		
		Expr(
#line  2606 "VBNET.ATG" 
out lhs);
		Expect(116);
		Expr(
#line  2606 "VBNET.ATG" 
out rhs);

#line  2608 "VBNET.ATG" 
		condition.LeftSide = lhs;
		condition.RightSide = rhs;
		condition.EndLocation = t.EndLocation;
		
	}

	void Argument(
#line  2672 "VBNET.ATG" 
out Expression argumentexpr) {

#line  2674 "VBNET.ATG" 
		Expression expr;
		argumentexpr = null;
		string name;
		Location startLocation = la.Location;
		
		if (
#line  2679 "VBNET.ATG" 
IsNamedAssign()) {
			Identifier();

#line  2679 "VBNET.ATG" 
			name = t.val;  
			Expect(55);
			Expr(
#line  2679 "VBNET.ATG" 
out expr);

#line  2681 "VBNET.ATG" 
			argumentexpr = new NamedArgumentExpression(name, expr) { StartLocation = startLocation, EndLocation = t.EndLocation };
			
		} else if (StartOf(24)) {
			Expr(
#line  2684 "VBNET.ATG" 
out argumentexpr);
		} else SynErr(308);
	}

	void QualIdentAndTypeArguments(
#line  2752 "VBNET.ATG" 
out TypeReference typeref, bool canBeUnbound) {

#line  2753 "VBNET.ATG" 
		string name; typeref = null; 
		Qualident(
#line  2755 "VBNET.ATG" 
out name);

#line  2756 "VBNET.ATG" 
		typeref = new TypeReference(name); 
		if (
#line  2757 "VBNET.ATG" 
la.kind == Tokens.OpenParenthesis && Peek(1).kind == Tokens.Of) {
			lexer.NextToken();
			Expect(169);
			if (
#line  2759 "VBNET.ATG" 
canBeUnbound && (la.kind == Tokens.CloseParenthesis || la.kind == Tokens.Comma)) {

#line  2760 "VBNET.ATG" 
				typeref.GenericTypes.Add(NullTypeReference.Instance); 
				while (la.kind == 22) {
					lexer.NextToken();

#line  2761 "VBNET.ATG" 
					typeref.GenericTypes.Add(NullTypeReference.Instance); 
				}
			} else if (StartOf(8)) {
				TypeArgumentList(
#line  2762 "VBNET.ATG" 
typeref.GenericTypes);
			} else SynErr(309);
			Expect(38);
		}
	}

	void RankList(
#line  2799 "VBNET.ATG" 
out int i) {

#line  2800 "VBNET.ATG" 
		i = 0; 
		while (la.kind == 22) {
			lexer.NextToken();

#line  2801 "VBNET.ATG" 
			++i; 
		}
	}

	void Attribute(
#line  2840 "VBNET.ATG" 
out ASTAttribute attribute) {

#line  2842 "VBNET.ATG" 
		string name;
		List<Expression> positional = new List<Expression>();
		List<NamedArgumentExpression> named = new List<NamedArgumentExpression>();
		Location startLocation = la.Location;
		
		if (la.kind == 130) {
			lexer.NextToken();
			Expect(26);
		}
		Qualident(
#line  2848 "VBNET.ATG" 
out name);
		if (la.kind == 37) {
			AttributeArguments(
#line  2849 "VBNET.ATG" 
positional, named);
		}

#line  2851 "VBNET.ATG" 
		attribute  = new ASTAttribute(name, positional, named) { StartLocation = startLocation, EndLocation = t.EndLocation };
		
	}

	void AttributeArguments(
#line  2856 "VBNET.ATG" 
List<Expression> positional, List<NamedArgumentExpression> named) {

#line  2858 "VBNET.ATG" 
		bool nameFound = false;
		string name = "";
		Expression expr;
		
		Expect(37);
		if (
#line  2864 "VBNET.ATG" 
IsNotClosingParenthesis()) {

#line  2865 "VBNET.ATG" 
			Location startLocation = la.Location; 
			if (
#line  2867 "VBNET.ATG" 
IsNamedAssign()) {

#line  2867 "VBNET.ATG" 
				nameFound = true; 
				IdentifierOrKeyword(
#line  2868 "VBNET.ATG" 
out name);
				if (la.kind == 55) {
					lexer.NextToken();
				} else if (la.kind == 20) {
					lexer.NextToken();
				} else SynErr(310);
			}
			Expr(
#line  2870 "VBNET.ATG" 
out expr);

#line  2872 "VBNET.ATG" 
			if (expr != null) {
			if (string.IsNullOrEmpty(name)) { positional.Add(expr); }
			else { named.Add(new NamedArgumentExpression(name, expr) { StartLocation = startLocation, EndLocation = t.EndLocation }); name = ""; }
			}
			
			while (la.kind == 22) {
				lexer.NextToken();
				if (
#line  2880 "VBNET.ATG" 
IsNamedAssign()) {

#line  2880 "VBNET.ATG" 
					nameFound = true; 
					IdentifierOrKeyword(
#line  2881 "VBNET.ATG" 
out name);
					if (la.kind == 55) {
						lexer.NextToken();
					} else if (la.kind == 20) {
						lexer.NextToken();
					} else SynErr(311);
				} else if (StartOf(24)) {

#line  2883 "VBNET.ATG" 
					if (nameFound) Error("no positional argument after named argument"); 
				} else SynErr(312);
				Expr(
#line  2884 "VBNET.ATG" 
out expr);

#line  2884 "VBNET.ATG" 
				if (expr != null) { if(name == "") positional.Add(expr);
				else { named.Add(new NamedArgumentExpression(name, expr) { StartLocation = startLocation, EndLocation = t.EndLocation }); name = ""; }
				}
				
			}
		}
		Expect(38);
	}

	void ParameterModifier(
#line  3695 "VBNET.ATG" 
ParamModifierList m) {
		if (la.kind == 72) {
			lexer.NextToken();

#line  3696 "VBNET.ATG" 
			m.Add(ParameterModifiers.In); 
		} else if (la.kind == 69) {
			lexer.NextToken();

#line  3697 "VBNET.ATG" 
			m.Add(ParameterModifiers.Ref); 
		} else if (la.kind == 174) {
			lexer.NextToken();

#line  3698 "VBNET.ATG" 
			m.Add(ParameterModifiers.Optional); 
		} else if (la.kind == 182) {
			lexer.NextToken();

#line  3699 "VBNET.ATG" 
			m.Add(ParameterModifiers.Params); 
		} else SynErr(313);
	}

	void Statement() {

#line  3011 "VBNET.ATG" 
		Statement stmt = null;
		Location startPos = la.Location;
		string label = String.Empty;
		
		
		if (la.kind == 1 || la.kind == 21) {
		} else if (
#line  3017 "VBNET.ATG" 
IsLabel()) {
			LabelName(
#line  3017 "VBNET.ATG" 
out label);

#line  3019 "VBNET.ATG" 
			AddChild(new LabelStatement(t.val));
			
			Expect(21);
			Statement();
		} else if (StartOf(49)) {
			EmbeddedStatement(
#line  3022 "VBNET.ATG" 
out stmt);

#line  3022 "VBNET.ATG" 
			AddChild(stmt); 
		} else SynErr(314);

#line  3025 "VBNET.ATG" 
		if (stmt != null) {
		stmt.StartLocation = startPos;
		stmt.EndLocation = t.Location;
		}
		
	}

	void LabelName(
#line  3465 "VBNET.ATG" 
out string name) {

#line  3467 "VBNET.ATG" 
		name = String.Empty;
		
		if (StartOf(4)) {
			Identifier();

#line  3469 "VBNET.ATG" 
			name = t.val; 
		} else if (la.kind == 5) {
			lexer.NextToken();

#line  3470 "VBNET.ATG" 
			name = t.val; 
		} else SynErr(315);
	}

	void LocalDeclarationStatement(
#line  3033 "VBNET.ATG" 
out Statement statement) {

#line  3035 "VBNET.ATG" 
		ModifierList m = new ModifierList();
		LocalVariableDeclaration localVariableDeclaration;
		bool dimfound = false;
		
		while (la.kind == 88 || la.kind == 105 || la.kind == 204) {
			if (la.kind == 88) {
				lexer.NextToken();

#line  3041 "VBNET.ATG" 
				m.Add(Modifiers.Const, t.Location); 
			} else if (la.kind == 204) {
				lexer.NextToken();

#line  3042 "VBNET.ATG" 
				m.Add(Modifiers.Static, t.Location); 
			} else {
				lexer.NextToken();

#line  3043 "VBNET.ATG" 
				dimfound = true; 
			}
		}

#line  3046 "VBNET.ATG" 
		if(dimfound && (m.Modifier & Modifiers.Const) != 0) {
		Error("Dim is not allowed on constants.");
		}
		
		if(m.isNone && dimfound == false) {
			Error("Const, Dim or Static expected");
		}
		
		localVariableDeclaration = new LocalVariableDeclaration(m.Modifier);
		localVariableDeclaration.StartLocation = t.Location;
		
		VariableDeclarator(
#line  3057 "VBNET.ATG" 
localVariableDeclaration.Variables);
		while (la.kind == 22) {
			lexer.NextToken();
			VariableDeclarator(
#line  3058 "VBNET.ATG" 
localVariableDeclaration.Variables);
		}

#line  3060 "VBNET.ATG" 
		statement = localVariableDeclaration;
		
	}

	void TryStatement(
#line  3583 "VBNET.ATG" 
out Statement tryStatement) {

#line  3585 "VBNET.ATG" 
		Statement blockStmt = null, finallyStmt = null;List<CatchClause> catchClauses = null;
		
		Expect(218);
		EndOfStmt();
		Block(
#line  3588 "VBNET.ATG" 
out blockStmt);
		if (la.kind == 75 || la.kind == 113 || la.kind == 123) {
			CatchClauses(
#line  3589 "VBNET.ATG" 
out catchClauses);
		}
		if (la.kind == 123) {
			lexer.NextToken();
			EndOfStmt();
			Block(
#line  3590 "VBNET.ATG" 
out finallyStmt);
		}
		Expect(113);
		Expect(218);

#line  3593 "VBNET.ATG" 
		tryStatement = new TryCatchStatement(blockStmt, catchClauses, finallyStmt);
		
	}

	void WithStatement(
#line  3563 "VBNET.ATG" 
out Statement withStatement) {

#line  3565 "VBNET.ATG" 
		Statement blockStmt = null;
		Expression expr = null;
		
		Expect(233);

#line  3568 "VBNET.ATG" 
		Location start = t.Location; 
		Expr(
#line  3569 "VBNET.ATG" 
out expr);
		EndOfStmt();

#line  3571 "VBNET.ATG" 
		withStatement = new WithStatement(expr);
		withStatement.StartLocation = start;
		
		Block(
#line  3574 "VBNET.ATG" 
out blockStmt);

#line  3576 "VBNET.ATG" 
		((WithStatement)withStatement).Body = (BlockStatement)blockStmt;
		
		Expect(113);
		Expect(233);

#line  3579 "VBNET.ATG" 
		withStatement.EndLocation = t.Location; 
	}

	void WhileOrUntil(
#line  3556 "VBNET.ATG" 
out ConditionType conditionType) {

#line  3557 "VBNET.ATG" 
		conditionType = ConditionType.None; 
		if (la.kind == 231) {
			lexer.NextToken();

#line  3558 "VBNET.ATG" 
			conditionType = ConditionType.While; 
		} else if (la.kind == 224) {
			lexer.NextToken();

#line  3559 "VBNET.ATG" 
			conditionType = ConditionType.Until; 
		} else SynErr(316);
	}

	void LoopControlVariable(
#line  3387 "VBNET.ATG" 
out TypeReference type, out string name) {

#line  3388 "VBNET.ATG" 
		ArrayList arrayModifiers = null;
		type = null;
		
		Qualident(
#line  3392 "VBNET.ATG" 
out name);
		if (
#line  3393 "VBNET.ATG" 
IsDims()) {
			ArrayTypeModifiers(
#line  3393 "VBNET.ATG" 
out arrayModifiers);
		}
		if (la.kind == 63) {
			lexer.NextToken();
			TypeName(
#line  3394 "VBNET.ATG" 
out type);

#line  3394 "VBNET.ATG" 
			if (name.IndexOf('.') > 0) { Error("No type def for 'for each' member indexer allowed."); } 
		}

#line  3396 "VBNET.ATG" 
		if (type != null) {
		if(type.RankSpecifier != null && arrayModifiers != null) {
			Error("array rank only allowed one time");
		} else if (arrayModifiers != null) {
			type.RankSpecifier = (int[])arrayModifiers.ToArray(typeof(int));
		}
		}
		
	}

	void ReDimClause(
#line  3474 "VBNET.ATG" 
out Expression expr) {
		SimpleNonInvocationExpression(
#line  3476 "VBNET.ATG" 
out expr);
		ReDimClauseInternal(
#line  3477 "VBNET.ATG" 
ref expr);
	}

	void SingleLineStatementList(
#line  3373 "VBNET.ATG" 
List<Statement> list) {

#line  3374 "VBNET.ATG" 
		Statement embeddedStatement = null; 
		if (la.kind == 113) {
			lexer.NextToken();

#line  3376 "VBNET.ATG" 
			embeddedStatement = new EndStatement() { StartLocation = t.Location, EndLocation = t.EndLocation }; 
		} else if (StartOf(49)) {
			EmbeddedStatement(
#line  3377 "VBNET.ATG" 
out embeddedStatement);
		} else SynErr(317);

#line  3378 "VBNET.ATG" 
		if (embeddedStatement != null) list.Add(embeddedStatement); 
		while (la.kind == 21) {
			lexer.NextToken();
			while (la.kind == 21) {
				lexer.NextToken();
			}
			if (la.kind == 113) {
				lexer.NextToken();

#line  3380 "VBNET.ATG" 
				embeddedStatement = new EndStatement() { StartLocation = t.Location, EndLocation = t.EndLocation }; 
			} else if (StartOf(49)) {
				EmbeddedStatement(
#line  3381 "VBNET.ATG" 
out embeddedStatement);
			} else SynErr(318);

#line  3382 "VBNET.ATG" 
			if (embeddedStatement != null) list.Add(embeddedStatement); 
		}
	}

	void CaseClauses(
#line  3516 "VBNET.ATG" 
out List<CaseLabel> caseClauses) {

#line  3518 "VBNET.ATG" 
		caseClauses = new List<CaseLabel>();
		CaseLabel caseClause = null;
		
		CaseClause(
#line  3521 "VBNET.ATG" 
out caseClause);

#line  3521 "VBNET.ATG" 
		if (caseClause != null) { caseClauses.Add(caseClause); } 
		while (la.kind == 22) {
			lexer.NextToken();
			CaseClause(
#line  3522 "VBNET.ATG" 
out caseClause);

#line  3522 "VBNET.ATG" 
			if (caseClause != null) { caseClauses.Add(caseClause); } 
		}
	}

	void OnErrorStatement(
#line  3407 "VBNET.ATG" 
out OnErrorStatement stmt) {

#line  3409 "VBNET.ATG" 
		stmt = null;
		Location startLocation = la.Location;
		GotoStatement goToStatement = null;
		
		Expect(171);
		Expect(118);
		if (
#line  3416 "VBNET.ATG" 
IsNegativeLabelName()) {
			Expect(132);
			Expect(30);
			Expect(5);

#line  3418 "VBNET.ATG" 
			long intLabel = Int64.Parse(t.val);
			if(intLabel != 1) {
				Error("invalid label in on error statement.");
			}
			stmt = new OnErrorStatement(new GotoStatement((intLabel * -1).ToString()));
			
		} else if (la.kind == 132) {
			GotoStatement(
#line  3424 "VBNET.ATG" 
out goToStatement);

#line  3426 "VBNET.ATG" 
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
			
		} else if (la.kind == 194) {
			lexer.NextToken();
			Expect(163);

#line  3440 "VBNET.ATG" 
			stmt = new OnErrorStatement(new ResumeStatement(true));
			
		} else SynErr(319);

#line  3444 "VBNET.ATG" 
		if (stmt != null) {
		stmt.StartLocation = startLocation;
		stmt.EndLocation = t.EndLocation;
		}
		
	}

	void GotoStatement(
#line  3452 "VBNET.ATG" 
out GotoStatement goToStatement) {

#line  3453 "VBNET.ATG" 
		string label = String.Empty; Location startLocation = la.Location; 
		Expect(132);
		LabelName(
#line  3455 "VBNET.ATG" 
out label);

#line  3457 "VBNET.ATG" 
		goToStatement = new GotoStatement(label) {
		StartLocation = startLocation,
		EndLocation = t.EndLocation
		};
		
	}

	void ResumeStatement(
#line  3505 "VBNET.ATG" 
out ResumeStatement resumeStatement) {

#line  3507 "VBNET.ATG" 
		resumeStatement = null;
		string label = String.Empty;
		
		if (
#line  3510 "VBNET.ATG" 
IsResumeNext()) {
			Expect(194);
			Expect(163);

#line  3511 "VBNET.ATG" 
			resumeStatement = new ResumeStatement(true); 
		} else if (la.kind == 194) {
			lexer.NextToken();
			if (StartOf(50)) {
				LabelName(
#line  3512 "VBNET.ATG" 
out label);
			}

#line  3512 "VBNET.ATG" 
			resumeStatement = new ResumeStatement(label); 
		} else SynErr(320);
	}

	void ReDimClauseInternal(
#line  3480 "VBNET.ATG" 
ref Expression expr) {

#line  3481 "VBNET.ATG" 
		List<Expression> arguments; bool canBeNormal; bool canBeRedim; string name; Location startLocation = la.Location; 
		while (la.kind == 26 || 
#line  3484 "VBNET.ATG" 
la.kind == Tokens.OpenParenthesis && Peek(1).kind == Tokens.Of) {
			if (la.kind == 26) {
				lexer.NextToken();
				IdentifierOrKeyword(
#line  3483 "VBNET.ATG" 
out name);

#line  3483 "VBNET.ATG" 
				expr = new MemberReferenceExpression(expr, name) { StartLocation = startLocation, EndLocation = t.EndLocation }; 
			} else {
				InvocationExpression(
#line  3485 "VBNET.ATG" 
ref expr);

#line  3487 "VBNET.ATG" 
				expr.StartLocation = startLocation;
				expr.EndLocation = t.EndLocation;
				
			}
		}
		Expect(37);
		NormalOrReDimArgumentList(
#line  3492 "VBNET.ATG" 
out arguments, out canBeNormal, out canBeRedim);
		Expect(38);

#line  3494 "VBNET.ATG" 
		expr = new InvocationExpression(expr, arguments);
		if (canBeRedim == false || canBeNormal && (la.kind == Tokens.Dot || la.kind == Tokens.OpenParenthesis)) {
			if (this.Errors.Count == 0) {
				// don't recurse on parse errors - could result in endless recursion
				ReDimClauseInternal(ref expr);
			}
		}
		
	}

	void CaseClause(
#line  3526 "VBNET.ATG" 
out CaseLabel caseClause) {

#line  3528 "VBNET.ATG" 
		Expression expr = null;
		Expression sexpr = null;
		BinaryOperatorType op = BinaryOperatorType.None;
		caseClause = null;
		
		if (la.kind == 111) {
			lexer.NextToken();

#line  3534 "VBNET.ATG" 
			caseClause = new CaseLabel(); 
		} else if (StartOf(51)) {
			if (la.kind == 144) {
				lexer.NextToken();
			}
			switch (la.kind) {
			case 40: {
				lexer.NextToken();

#line  3538 "VBNET.ATG" 
				op = BinaryOperatorType.LessThan; 
				break;
			}
			case 39: {
				lexer.NextToken();

#line  3539 "VBNET.ATG" 
				op = BinaryOperatorType.GreaterThan; 
				break;
			}
			case 43: {
				lexer.NextToken();

#line  3540 "VBNET.ATG" 
				op = BinaryOperatorType.LessThanOrEqual; 
				break;
			}
			case 42: {
				lexer.NextToken();

#line  3541 "VBNET.ATG" 
				op = BinaryOperatorType.GreaterThanOrEqual; 
				break;
			}
			case 20: {
				lexer.NextToken();

#line  3542 "VBNET.ATG" 
				op = BinaryOperatorType.Equality; 
				break;
			}
			case 41: {
				lexer.NextToken();

#line  3543 "VBNET.ATG" 
				op = BinaryOperatorType.InEquality; 
				break;
			}
			default: SynErr(321); break;
			}
			Expr(
#line  3545 "VBNET.ATG" 
out expr);

#line  3547 "VBNET.ATG" 
			caseClause = new CaseLabel(op, expr);
			
		} else if (StartOf(24)) {
			Expr(
#line  3549 "VBNET.ATG" 
out expr);
			if (la.kind == 216) {
				lexer.NextToken();
				Expr(
#line  3549 "VBNET.ATG" 
out sexpr);
			}

#line  3551 "VBNET.ATG" 
			caseClause = new CaseLabel(expr, sexpr);
			
		} else SynErr(322);
	}

	void CatchClauses(
#line  3598 "VBNET.ATG" 
out List<CatchClause> catchClauses) {

#line  3600 "VBNET.ATG" 
		catchClauses = new List<CatchClause>();
		TypeReference type = null;
		Statement blockStmt = null;
		Expression expr = null;
		string name = String.Empty;
		
		while (la.kind == 75) {
			lexer.NextToken();
			if (StartOf(4)) {
				Identifier();

#line  3608 "VBNET.ATG" 
				name = t.val; 
				if (la.kind == 63) {
					lexer.NextToken();
					TypeName(
#line  3608 "VBNET.ATG" 
out type);
				}
			}
			if (la.kind == 229) {
				lexer.NextToken();
				Expr(
#line  3609 "VBNET.ATG" 
out expr);
			}
			EndOfStmt();
			Block(
#line  3611 "VBNET.ATG" 
out blockStmt);

#line  3612 "VBNET.ATG" 
			catchClauses.Add(new CatchClause(type, name, blockStmt, expr)); 
		}
	}


	
	void ParseRoot()
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
			case 10: s = "XmlOpenTag expected"; break;
			case 11: s = "XmlCloseTag expected"; break;
			case 12: s = "XmlStartInlineVB expected"; break;
			case 13: s = "XmlEndInlineVB expected"; break;
			case 14: s = "XmlCloseTagEmptyElement expected"; break;
			case 15: s = "XmlOpenEndTag expected"; break;
			case 16: s = "XmlContent expected"; break;
			case 17: s = "XmlComment expected"; break;
			case 18: s = "XmlCData expected"; break;
			case 19: s = "XmlProcessingInstruction expected"; break;
			case 20: s = "\"=\" expected"; break;
			case 21: s = "\":\" expected"; break;
			case 22: s = "\",\" expected"; break;
			case 23: s = "\"&\" expected"; break;
			case 24: s = "\"/\" expected"; break;
			case 25: s = "\"\\\\\" expected"; break;
			case 26: s = "\".\" expected"; break;
			case 27: s = "\"...\" expected"; break;
			case 28: s = "\".@\" expected"; break;
			case 29: s = "\"!\" expected"; break;
			case 30: s = "\"-\" expected"; break;
			case 31: s = "\"+\" expected"; break;
			case 32: s = "\"^\" expected"; break;
			case 33: s = "\"?\" expected"; break;
			case 34: s = "\"*\" expected"; break;
			case 35: s = "\"{\" expected"; break;
			case 36: s = "\"}\" expected"; break;
			case 37: s = "\"(\" expected"; break;
			case 38: s = "\")\" expected"; break;
			case 39: s = "\">\" expected"; break;
			case 40: s = "\"<\" expected"; break;
			case 41: s = "\"<>\" expected"; break;
			case 42: s = "\">=\" expected"; break;
			case 43: s = "\"<=\" expected"; break;
			case 44: s = "\"<<\" expected"; break;
			case 45: s = "\">>\" expected"; break;
			case 46: s = "\"+=\" expected"; break;
			case 47: s = "\"^=\" expected"; break;
			case 48: s = "\"-=\" expected"; break;
			case 49: s = "\"*=\" expected"; break;
			case 50: s = "\"/=\" expected"; break;
			case 51: s = "\"\\\\=\" expected"; break;
			case 52: s = "\"<<=\" expected"; break;
			case 53: s = "\">>=\" expected"; break;
			case 54: s = "\"&=\" expected"; break;
			case 55: s = "\":=\" expected"; break;
			case 56: s = "\"AddHandler\" expected"; break;
			case 57: s = "\"AddressOf\" expected"; break;
			case 58: s = "\"Aggregate\" expected"; break;
			case 59: s = "\"Alias\" expected"; break;
			case 60: s = "\"And\" expected"; break;
			case 61: s = "\"AndAlso\" expected"; break;
			case 62: s = "\"Ansi\" expected"; break;
			case 63: s = "\"As\" expected"; break;
			case 64: s = "\"Ascending\" expected"; break;
			case 65: s = "\"Assembly\" expected"; break;
			case 66: s = "\"Auto\" expected"; break;
			case 67: s = "\"Binary\" expected"; break;
			case 68: s = "\"Boolean\" expected"; break;
			case 69: s = "\"ByRef\" expected"; break;
			case 70: s = "\"By\" expected"; break;
			case 71: s = "\"Byte\" expected"; break;
			case 72: s = "\"ByVal\" expected"; break;
			case 73: s = "\"Call\" expected"; break;
			case 74: s = "\"Case\" expected"; break;
			case 75: s = "\"Catch\" expected"; break;
			case 76: s = "\"CBool\" expected"; break;
			case 77: s = "\"CByte\" expected"; break;
			case 78: s = "\"CChar\" expected"; break;
			case 79: s = "\"CDate\" expected"; break;
			case 80: s = "\"CDbl\" expected"; break;
			case 81: s = "\"CDec\" expected"; break;
			case 82: s = "\"Char\" expected"; break;
			case 83: s = "\"CInt\" expected"; break;
			case 84: s = "\"Class\" expected"; break;
			case 85: s = "\"CLng\" expected"; break;
			case 86: s = "\"CObj\" expected"; break;
			case 87: s = "\"Compare\" expected"; break;
			case 88: s = "\"Const\" expected"; break;
			case 89: s = "\"Continue\" expected"; break;
			case 90: s = "\"CSByte\" expected"; break;
			case 91: s = "\"CShort\" expected"; break;
			case 92: s = "\"CSng\" expected"; break;
			case 93: s = "\"CStr\" expected"; break;
			case 94: s = "\"CType\" expected"; break;
			case 95: s = "\"CUInt\" expected"; break;
			case 96: s = "\"CULng\" expected"; break;
			case 97: s = "\"CUShort\" expected"; break;
			case 98: s = "\"Custom\" expected"; break;
			case 99: s = "\"Date\" expected"; break;
			case 100: s = "\"Decimal\" expected"; break;
			case 101: s = "\"Declare\" expected"; break;
			case 102: s = "\"Default\" expected"; break;
			case 103: s = "\"Delegate\" expected"; break;
			case 104: s = "\"Descending\" expected"; break;
			case 105: s = "\"Dim\" expected"; break;
			case 106: s = "\"DirectCast\" expected"; break;
			case 107: s = "\"Distinct\" expected"; break;
			case 108: s = "\"Do\" expected"; break;
			case 109: s = "\"Double\" expected"; break;
			case 110: s = "\"Each\" expected"; break;
			case 111: s = "\"Else\" expected"; break;
			case 112: s = "\"ElseIf\" expected"; break;
			case 113: s = "\"End\" expected"; break;
			case 114: s = "\"EndIf\" expected"; break;
			case 115: s = "\"Enum\" expected"; break;
			case 116: s = "\"Equals\" expected"; break;
			case 117: s = "\"Erase\" expected"; break;
			case 118: s = "\"Error\" expected"; break;
			case 119: s = "\"Event\" expected"; break;
			case 120: s = "\"Exit\" expected"; break;
			case 121: s = "\"Explicit\" expected"; break;
			case 122: s = "\"False\" expected"; break;
			case 123: s = "\"Finally\" expected"; break;
			case 124: s = "\"For\" expected"; break;
			case 125: s = "\"Friend\" expected"; break;
			case 126: s = "\"From\" expected"; break;
			case 127: s = "\"Function\" expected"; break;
			case 128: s = "\"Get\" expected"; break;
			case 129: s = "\"GetType\" expected"; break;
			case 130: s = "\"Global\" expected"; break;
			case 131: s = "\"GoSub\" expected"; break;
			case 132: s = "\"GoTo\" expected"; break;
			case 133: s = "\"Group\" expected"; break;
			case 134: s = "\"Handles\" expected"; break;
			case 135: s = "\"If\" expected"; break;
			case 136: s = "\"Implements\" expected"; break;
			case 137: s = "\"Imports\" expected"; break;
			case 138: s = "\"In\" expected"; break;
			case 139: s = "\"Infer\" expected"; break;
			case 140: s = "\"Inherits\" expected"; break;
			case 141: s = "\"Integer\" expected"; break;
			case 142: s = "\"Interface\" expected"; break;
			case 143: s = "\"Into\" expected"; break;
			case 144: s = "\"Is\" expected"; break;
			case 145: s = "\"IsNot\" expected"; break;
			case 146: s = "\"Join\" expected"; break;
			case 147: s = "\"Key\" expected"; break;
			case 148: s = "\"Let\" expected"; break;
			case 149: s = "\"Lib\" expected"; break;
			case 150: s = "\"Like\" expected"; break;
			case 151: s = "\"Long\" expected"; break;
			case 152: s = "\"Loop\" expected"; break;
			case 153: s = "\"Me\" expected"; break;
			case 154: s = "\"Mod\" expected"; break;
			case 155: s = "\"Module\" expected"; break;
			case 156: s = "\"MustInherit\" expected"; break;
			case 157: s = "\"MustOverride\" expected"; break;
			case 158: s = "\"MyBase\" expected"; break;
			case 159: s = "\"MyClass\" expected"; break;
			case 160: s = "\"Namespace\" expected"; break;
			case 161: s = "\"Narrowing\" expected"; break;
			case 162: s = "\"New\" expected"; break;
			case 163: s = "\"Next\" expected"; break;
			case 164: s = "\"Not\" expected"; break;
			case 165: s = "\"Nothing\" expected"; break;
			case 166: s = "\"NotInheritable\" expected"; break;
			case 167: s = "\"NotOverridable\" expected"; break;
			case 168: s = "\"Object\" expected"; break;
			case 169: s = "\"Of\" expected"; break;
			case 170: s = "\"Off\" expected"; break;
			case 171: s = "\"On\" expected"; break;
			case 172: s = "\"Operator\" expected"; break;
			case 173: s = "\"Option\" expected"; break;
			case 174: s = "\"Optional\" expected"; break;
			case 175: s = "\"Or\" expected"; break;
			case 176: s = "\"Order\" expected"; break;
			case 177: s = "\"OrElse\" expected"; break;
			case 178: s = "\"Out\" expected"; break;
			case 179: s = "\"Overloads\" expected"; break;
			case 180: s = "\"Overridable\" expected"; break;
			case 181: s = "\"Overrides\" expected"; break;
			case 182: s = "\"ParamArray\" expected"; break;
			case 183: s = "\"Partial\" expected"; break;
			case 184: s = "\"Preserve\" expected"; break;
			case 185: s = "\"Private\" expected"; break;
			case 186: s = "\"Property\" expected"; break;
			case 187: s = "\"Protected\" expected"; break;
			case 188: s = "\"Public\" expected"; break;
			case 189: s = "\"RaiseEvent\" expected"; break;
			case 190: s = "\"ReadOnly\" expected"; break;
			case 191: s = "\"ReDim\" expected"; break;
			case 192: s = "\"Rem\" expected"; break;
			case 193: s = "\"RemoveHandler\" expected"; break;
			case 194: s = "\"Resume\" expected"; break;
			case 195: s = "\"Return\" expected"; break;
			case 196: s = "\"SByte\" expected"; break;
			case 197: s = "\"Select\" expected"; break;
			case 198: s = "\"Set\" expected"; break;
			case 199: s = "\"Shadows\" expected"; break;
			case 200: s = "\"Shared\" expected"; break;
			case 201: s = "\"Short\" expected"; break;
			case 202: s = "\"Single\" expected"; break;
			case 203: s = "\"Skip\" expected"; break;
			case 204: s = "\"Static\" expected"; break;
			case 205: s = "\"Step\" expected"; break;
			case 206: s = "\"Stop\" expected"; break;
			case 207: s = "\"Strict\" expected"; break;
			case 208: s = "\"String\" expected"; break;
			case 209: s = "\"Structure\" expected"; break;
			case 210: s = "\"Sub\" expected"; break;
			case 211: s = "\"SyncLock\" expected"; break;
			case 212: s = "\"Take\" expected"; break;
			case 213: s = "\"Text\" expected"; break;
			case 214: s = "\"Then\" expected"; break;
			case 215: s = "\"Throw\" expected"; break;
			case 216: s = "\"To\" expected"; break;
			case 217: s = "\"True\" expected"; break;
			case 218: s = "\"Try\" expected"; break;
			case 219: s = "\"TryCast\" expected"; break;
			case 220: s = "\"TypeOf\" expected"; break;
			case 221: s = "\"UInteger\" expected"; break;
			case 222: s = "\"ULong\" expected"; break;
			case 223: s = "\"Unicode\" expected"; break;
			case 224: s = "\"Until\" expected"; break;
			case 225: s = "\"UShort\" expected"; break;
			case 226: s = "\"Using\" expected"; break;
			case 227: s = "\"Variant\" expected"; break;
			case 228: s = "\"Wend\" expected"; break;
			case 229: s = "\"When\" expected"; break;
			case 230: s = "\"Where\" expected"; break;
			case 231: s = "\"While\" expected"; break;
			case 232: s = "\"Widening\" expected"; break;
			case 233: s = "\"With\" expected"; break;
			case 234: s = "\"WithEvents\" expected"; break;
			case 235: s = "\"WriteOnly\" expected"; break;
			case 236: s = "\"Xor\" expected"; break;
			case 237: s = "\"GetXmlNamespace\" expected"; break;
			case 238: s = "??? expected"; break;
			case 239: s = "this symbol not expected in EndOfStmt"; break;
			case 240: s = "invalid EndOfStmt"; break;
			case 241: s = "invalid OptionStmt"; break;
			case 242: s = "invalid OptionStmt"; break;
			case 243: s = "invalid GlobalAttributeSection"; break;
			case 244: s = "invalid GlobalAttributeSection"; break;
			case 245: s = "invalid NamespaceMemberDecl"; break;
			case 246: s = "invalid OptionValue"; break;
			case 247: s = "invalid ImportClause"; break;
			case 248: s = "invalid Identifier"; break;
			case 249: s = "invalid TypeModifier"; break;
			case 250: s = "invalid NonModuleDeclaration"; break;
			case 251: s = "invalid NonModuleDeclaration"; break;
			case 252: s = "invalid TypeParameterConstraints"; break;
			case 253: s = "invalid TypeParameterConstraint"; break;
			case 254: s = "invalid NonArrayTypeName"; break;
			case 255: s = "invalid MemberModifier"; break;
			case 256: s = "invalid StructureMemberDecl"; break;
			case 257: s = "invalid StructureMemberDecl"; break;
			case 258: s = "invalid StructureMemberDecl"; break;
			case 259: s = "invalid StructureMemberDecl"; break;
			case 260: s = "invalid StructureMemberDecl"; break;
			case 261: s = "invalid StructureMemberDecl"; break;
			case 262: s = "invalid StructureMemberDecl"; break;
			case 263: s = "invalid StructureMemberDecl"; break;
			case 264: s = "invalid InterfaceMemberDecl"; break;
			case 265: s = "invalid InterfaceMemberDecl"; break;
			case 266: s = "invalid Expr"; break;
			case 267: s = "invalid Charset"; break;
			case 268: s = "invalid IdentifierForFieldDeclaration"; break;
			case 269: s = "invalid VariableDeclaratorPartAfterIdentifier"; break;
			case 270: s = "invalid ObjectCreateExpression"; break;
			case 271: s = "invalid ObjectCreateExpression"; break;
			case 272: s = "invalid AccessorDecls"; break;
			case 273: s = "invalid EventAccessorDeclaration"; break;
			case 274: s = "invalid OverloadableOperator"; break;
			case 275: s = "invalid EventMemberSpecifier"; break;
			case 276: s = "invalid LambdaExpr"; break;
			case 277: s = "invalid AssignmentOperator"; break;
			case 278: s = "invalid SimpleExpr"; break;
			case 279: s = "invalid SimpleExpr"; break;
			case 280: s = "invalid SimpleNonInvocationExpression"; break;
			case 281: s = "invalid SimpleNonInvocationExpression"; break;
			case 282: s = "invalid SimpleNonInvocationExpression"; break;
			case 283: s = "invalid SimpleNonInvocationExpression"; break;
			case 284: s = "invalid SimpleNonInvocationExpression"; break;
			case 285: s = "invalid SimpleNonInvocationExpression"; break;
			case 286: s = "invalid PrimitiveTypeName"; break;
			case 287: s = "invalid CastTarget"; break;
			case 288: s = "invalid XmlLiteralExpression"; break;
			case 289: s = "invalid XmlContentExpression"; break;
			case 290: s = "invalid XmlElement"; break;
			case 291: s = "invalid XmlElement"; break;
			case 292: s = "invalid XmlNestedContent"; break;
			case 293: s = "invalid XmlAttribute"; break;
			case 294: s = "invalid XmlAttribute"; break;
			case 295: s = "invalid ComparisonExpr"; break;
			case 296: s = "invalid SubLambdaExpression"; break;
			case 297: s = "invalid FunctionLambdaExpression"; break;
			case 298: s = "invalid EmbeddedStatement"; break;
			case 299: s = "invalid EmbeddedStatement"; break;
			case 300: s = "invalid EmbeddedStatement"; break;
			case 301: s = "invalid EmbeddedStatement"; break;
			case 302: s = "invalid EmbeddedStatement"; break;
			case 303: s = "invalid EmbeddedStatement"; break;
			case 304: s = "invalid EmbeddedStatement"; break;
			case 305: s = "invalid FromOrAggregateQueryOperator"; break;
			case 306: s = "invalid QueryOperator"; break;
			case 307: s = "invalid PartitionQueryOperator"; break;
			case 308: s = "invalid Argument"; break;
			case 309: s = "invalid QualIdentAndTypeArguments"; break;
			case 310: s = "invalid AttributeArguments"; break;
			case 311: s = "invalid AttributeArguments"; break;
			case 312: s = "invalid AttributeArguments"; break;
			case 313: s = "invalid ParameterModifier"; break;
			case 314: s = "invalid Statement"; break;
			case 315: s = "invalid LabelName"; break;
			case 316: s = "invalid WhileOrUntil"; break;
			case 317: s = "invalid SingleLineStatementList"; break;
			case 318: s = "invalid SingleLineStatementList"; break;
			case 319: s = "invalid OnErrorStatement"; break;
			case 320: s = "invalid ResumeStatement"; break;
			case 321: s = "invalid CaseClause"; break;
			case 322: s = "invalid CaseClause"; break;

			default: s = "error " + errorNumber; break;
		}
		this.Errors.Error(line, col, s);
	}
	
	private bool StartOf(int s)
	{
		return set[s, lexer.LookAhead.kind];
	}
	
	static bool[,] set = {
	{T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, T,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,x,T, T,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,x,T, T,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,x,T, T,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,T,x, T,T,T,T, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, T,x,x,T, x,x,x,x, x,x,x,x, T,x,x,x, x,T,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,x,T, x,x,x,T, x,x,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, T,x,T,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, T,T,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x},
	{x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,T,x, T,T,T,T, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,T, x,x,x,x, x,x,x,x, T,x,x,x, x,T,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,x,T, x,x,x,T, x,x,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, T,x,T,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, T,T,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x},
	{x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,T,x, T,T,T,T, x,T,T,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, T,x,x,T, x,x,x,x, x,x,x,x, T,x,x,x, x,T,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,x,T, x,x,x,T, x,x,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,T,x, T,x,T,x, x,x,T,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, T,T,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x},
	{x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,T,x, T,T,T,T, T,x,T,T, x,x,x,x, x,x,x,x, x,x,T,x, T,x,x,T, x,x,x,x, x,x,x,x, x,x,T,T, T,x,x,x, T,x,x,T, x,T,x,x, x,x,x,x, T,x,x,x, x,T,x,x, x,x,T,x, x,x,T,x, x,T,x,x, x,x,x,T, x,T,x,T, x,x,T,T, x,x,x,T, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, T,x,T,x, x,x,x,x, T,x,T,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,T,T,T, x,x,x,x, T,T,x,x, T,T,x,x, x,x,x,x, x,T,T,T, T,T,x,x, x,x,T,x, x,x,x,x, x,x,x,x},
	{x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,T,x, T,T,T,T, T,x,T,T, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,T,T, T,x,x,x, T,x,x,T, x,T,x,x, x,x,x,x, T,x,x,x, x,T,x,x, x,x,T,x, x,x,T,x, x,T,x,x, x,x,x,T, x,T,x,T, x,x,T,T, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,T,x, x,x,x,x, T,x,T,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,T,T,T, x,x,x,x, T,x,x,x, T,T,x,x, x,x,x,x, x,T,T,T, T,T,x,x, x,x,T,x, x,x,x,x, x,x,x,x},
	{x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,T,x, T,T,T,T, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,T, T,x,x,x, x,x,x,x, x,x,T,x, x,T,T,T, T,T,x,T, x,x,x,x, x,x,x,T, T,x,x,T, x,T,x,x, x,T,T,T, x,x,x,x, x,T,x,x, x,x,x,T, x,x,T,T, x,x,T,T, x,x,x,x, x,x,x,T, T,T,x,x, x,T,x,x, x,x,T,T, x,x,T,x, T,x,x,x, T,x,T,T, T,T,x,T, T,T,T,T, T,x,T,x, x,x,x,x, x,x,x,T, T,x,x,T, x,x,x,x, x,T,T,x, T,T,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,T,x, T,x,T,T, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,T,x,x, x,x,x,x, x,x,T,T, x,x,x,x, x,x,x,x, x,x,x,T, T,T,x,T, x,T,x,T, T,x,T,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,x,x,x},
	{x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,T,x, T,T,T,T, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, T,x,x,T, x,x,x,x, x,x,x,x, T,x,x,x, x,T,x,x, x,x,T,x, x,x,T,x, x,T,x,x, x,x,x,T, x,x,x,T, x,x,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, T,x,T,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, T,T,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,T, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,T,T,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,T,T,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,T,x, T,T,T,T, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, T,x,x,T, x,x,x,x, x,x,x,x, T,x,x,x, x,T,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,x,T, x,x,x,T, x,x,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, T,x,T,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, T,T,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,T,x,x, x,x,x,x, x,x,x,T, x,x,x,T, x,x,x,x, x,T,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,T, T,T,x,x, x,x,x,x, x,x,T,T, x,x,x,x, x,x,x,x, x,x,x,T, T,T,x,T, x,T,T,T, T,x,T,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, x,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,T, T,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,T,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,T,x,x, x,x,x,x, x,x,T,T, x,x,x,x, x,x,x,x, x,x,x,T, T,T,x,T, x,T,T,T, T,x,T,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,T,T, T,T,T,T, T,T,T,x, x,x,x,x, T,T,T,T, x,x,x,x, x,x,T,T, T,T,T,T, x,x,T,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,x, x,x,T,x, T,T,T,T, T,x,T,T, x,x,x,x, T,T,T,T, T,T,T,T, x,T,T,T, x,x,T,T, T,T,T,T, T,T,T,T, T,x,x,x, T,x,T,T, x,T,x,x, x,x,x,x, T,x,x,x, x,T,T,x, x,x,T,x, x,T,T,x, x,T,x,T, x,x,x,T, x,T,x,T, x,x,T,T, x,x,x,T, x,T,x,x, x,x,T,T, x,x,T,x, T,T,x,x, T,x,T,x, x,x,x,x, T,x,T,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,T,T,T, x,x,x,x, T,x,x,x, T,T,x,x, x,T,x,T, T,T,T,T, T,T,x,x, x,x,T,x, x,x,x,x, x,x,x,x},
	{x,T,T,T, T,T,T,T, T,T,T,x, x,x,x,x, T,T,T,T, x,T,x,x, x,x,T,T, T,T,x,x, x,x,x,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,T,T,x, x,x,T,x, T,T,T,T, T,x,T,T, x,T,x,x, T,T,T,T, T,T,T,T, x,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,x,x, T,T,T,T, T,T,x,x, x,x,x,x, T,T,T,x, T,T,T,x, T,x,T,x, x,T,T,x, T,T,x,T, x,x,x,T, x,T,x,T, x,x,T,T, x,x,x,T, x,T,x,x, x,x,T,T, x,x,T,x, x,T,x,x, T,x,T,T, x,x,x,x, T,x,T,x, x,x,x,x, T,x,x,x, x,T,x,T, x,T,T,T, T,T,x,x, x,T,T,T, T,x,T,x, T,x,x,T, T,T,x,T, x,T,T,T, T,T,T,T, T,T,T,x, x,x,T,T, x,T,x,x, x,x,x,x},
	{x,T,T,T, T,T,T,T, T,T,T,x, x,T,x,x, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,x, T,T,T,x, T,T,T,T, T,T,T,T, T,x,T,T, x,T,x,x, T,T,T,T, T,T,T,T, x,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,x,x, T,T,T,T, T,T,x,T, x,T,x,x, T,T,T,x, T,T,T,x, T,x,T,x, x,T,T,x, T,T,x,T, T,x,x,T, x,T,x,T, T,T,T,T, T,x,T,T, x,T,T,x, x,x,T,T, x,x,T,x, x,T,x,x, T,x,T,T, x,x,x,T, T,T,T,x, x,x,x,x, T,x,x,x, x,T,x,T, x,T,T,T, T,T,x,x, x,T,T,T, T,T,T,x, T,x,x,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,x, x,x,T,T, x,T,x,x, T,x,x,x},
	{x,x,T,T, T,T,T,T, T,T,T,x, x,x,x,x, T,T,T,T, x,x,x,x, x,x,T,T, T,T,T,T, x,x,T,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,x, x,x,T,x, T,T,T,T, T,x,T,T, x,x,x,x, T,T,T,T, T,T,T,T, x,T,T,T, x,x,T,T, T,T,T,T, T,T,T,T, T,x,x,x, T,x,T,T, x,T,x,x, x,x,x,x, T,x,x,x, x,T,T,x, x,x,T,T, x,T,T,x, x,T,x,T, x,x,x,T, x,T,x,T, x,x,T,T, x,x,x,T, x,T,x,x, x,x,T,T, x,x,T,x, T,T,x,x, T,x,T,x, x,x,x,x, T,x,T,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,T,T,T, x,x,x,x, T,x,T,x, T,T,x,x, x,T,x,T, T,T,T,T, T,T,x,x, x,x,T,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,T, T,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,T, T,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,T,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,T, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, T,T,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,x},
	{x,x,T,T, T,T,T,T, T,T,T,x, x,x,x,x, T,T,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,x, x,x,T,x, T,T,T,T, T,x,T,T, x,x,x,x, T,T,T,T, T,T,T,T, x,T,T,T, x,x,T,T, T,T,T,T, T,T,T,T, T,x,x,x, T,x,T,T, x,T,x,x, x,x,x,x, T,x,x,x, x,T,T,x, x,x,T,x, x,T,T,x, x,T,x,T, x,x,x,T, x,T,x,T, x,x,T,T, x,x,x,T, x,T,x,x, x,x,T,T, x,x,T,x, x,T,x,x, T,x,T,x, x,x,x,x, T,x,T,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,T,T,T, x,x,x,x, T,x,x,x, T,T,x,x, x,T,x,T, T,T,T,T, T,T,x,x, x,x,T,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,T,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,T,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,T,x, T,T,T,T, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, T,x,x,T, x,x,x,x, x,x,x,x, T,x,x,x, x,T,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,x,T, x,x,x,T, x,x,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, T,x,T,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, T,T,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,T,x, T,x,x,x, T,T,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,T,T,T, T,T,T,T, T,T,T,x, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,T,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,T,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,T,T, T,T,T,T, T,T,T,x, x,x,x,x, T,T,T,T, x,x,x,x, x,x,T,T, T,T,T,T, x,x,T,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,x, x,x,T,x, T,T,T,T, T,x,T,T, x,x,x,x, T,T,T,T, T,T,T,T, x,T,T,T, x,x,T,T, T,T,T,T, T,T,T,T, T,x,x,x, T,x,T,T, x,T,x,x, x,x,x,x, T,x,x,x, x,T,T,x, x,x,T,x, x,T,T,x, x,T,x,T, x,x,x,T, x,T,x,T, x,x,T,T, x,x,x,T, x,T,x,x, x,x,T,T, x,x,T,x, x,T,x,x, T,x,T,x, x,x,x,x, T,x,T,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,T,T,T, x,x,x,x, T,x,x,x, T,T,x,x, x,T,x,T, T,T,T,T, T,T,x,x, x,x,T,x, x,x,x,x, x,x,x,x},
	{x,x,T,T, T,T,T,T, T,T,T,x, x,x,x,x, T,T,T,T, x,x,x,x, x,x,T,T, T,T,T,T, x,x,T,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,T,T,x, x,x,T,x, T,T,T,T, T,x,T,T, x,T,x,x, T,T,T,T, T,T,T,T, x,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,x,x, T,T,T,T, T,T,x,x, x,x,x,x, T,T,T,x, T,T,T,x, T,x,T,T, x,T,T,x, T,T,x,T, x,x,x,T, x,T,x,T, x,x,T,T, x,x,x,T, x,T,x,x, x,x,T,T, x,x,T,x, T,T,x,x, T,x,T,T, x,x,x,x, T,x,T,x, x,x,x,x, T,x,x,x, x,T,x,T, x,T,T,T, T,T,x,x, x,T,T,T, T,x,T,x, T,x,T,T, T,T,x,T, x,T,T,T, T,T,T,T, T,T,T,x, x,x,T,T, x,T,x,x, x,x,x,x},
	{x,x,T,T, T,T,T,T, T,T,T,x, x,x,x,x, T,T,T,T, x,x,T,x, x,x,T,T, T,T,T,T, x,x,T,T, x,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,x, x,x,T,x, T,T,T,T, T,x,T,T, x,x,x,x, T,T,T,T, T,T,T,T, x,T,T,T, x,x,T,T, T,T,T,T, T,T,T,T, T,x,x,x, T,x,T,T, x,T,x,x, x,x,x,x, T,x,x,x, x,T,T,x, x,x,T,T, x,T,T,x, x,T,x,T, x,x,x,T, x,T,x,T, x,x,T,T, x,x,x,T, x,T,x,x, x,x,T,T, x,x,T,x, T,T,x,x, T,x,T,x, x,x,x,x, T,x,T,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,T,T,T, x,x,x,x, T,x,T,x, T,T,x,x, x,T,x,T, T,T,T,T, T,T,x,x, x,x,T,x, x,x,x,x, x,x,x,x},
	{x,x,T,T, T,T,T,T, T,T,T,x, x,x,x,x, T,T,T,T, x,x,x,x, x,x,T,T, T,T,x,x, x,x,x,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,x, x,x,T,x, T,T,T,T, T,x,T,T, x,x,x,x, T,T,T,T, T,T,T,T, x,T,T,T, x,x,T,T, T,T,T,T, T,T,T,T, T,x,x,x, T,x,T,T, x,T,x,x, x,x,x,x, T,x,x,x, x,T,T,x, x,x,T,x, x,T,T,x, x,T,x,T, x,x,x,T, x,T,x,T, x,x,T,T, x,x,x,T, x,T,x,x, x,x,T,T, x,x,T,x, x,T,x,x, T,x,T,x, x,x,x,x, T,x,T,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,T,T,T, x,x,x,x, T,x,x,x, T,T,x,x, x,T,x,T, T,T,T,T, T,T,x,x, x,x,T,x, x,x,x,x, x,x,x,x},
	{x,x,T,T, T,T,T,T, T,T,T,x, x,x,x,x, T,T,T,T, x,x,x,x, x,x,T,T, T,T,x,x, x,x,x,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,T,T,x, x,x,T,x, T,T,T,T, T,x,T,T, x,T,x,x, T,T,T,T, T,T,T,T, x,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,x,x, T,T,T,T, T,T,x,x, x,T,x,x, T,T,T,x, T,T,T,x, T,x,T,x, x,T,T,x, T,T,x,T, x,x,x,T, x,T,x,T, x,x,T,T, x,x,x,T, x,T,x,x, x,x,T,T, x,x,T,x, x,T,x,x, T,x,T,T, x,x,x,x, T,x,T,x, x,x,x,x, T,x,x,x, x,T,x,T, x,T,T,T, T,T,x,x, x,T,T,T, T,x,T,x, T,x,x,T, T,T,x,T, x,T,T,T, T,T,T,T, T,T,T,x, x,x,T,T, x,T,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, T,T,T,T, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,T,T,T, T,T,T,T, T,T,T,x, x,T,x,x, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,x, T,T,T,x, T,T,T,x, T,T,T,T, T,x,T,T, x,T,x,x, T,T,T,T, T,T,T,T, x,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,x,x, T,T,T,T, T,T,x,T, x,T,x,x, T,T,T,x, T,T,T,x, T,x,T,x, x,T,T,x, T,T,x,T, T,x,x,T, x,T,x,T, T,T,T,T, T,x,T,T, x,T,T,x, x,x,T,T, x,x,T,x, x,T,x,x, T,x,T,T, x,x,x,T, T,T,T,x, x,x,x,x, T,x,x,x, x,T,x,T, x,T,T,T, T,T,x,x, x,T,T,T, T,T,T,x, T,x,x,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,x, x,x,T,T, x,T,x,x, T,x,x,x},
	{x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,T,x, T,T,T,T, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, T,T,x,T, x,x,x,x, x,x,x,x, T,x,x,x, x,T,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,x,T, x,x,x,T, x,x,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, T,x,T,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, T,T,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x},
	{x,x,T,T, T,T,T,T, T,T,T,x, x,x,x,x, T,T,T,T, x,x,x,x, x,x,T,T, T,T,x,x, x,x,x,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,T,T,x, x,x,T,x, T,T,T,T, T,x,T,T, x,T,x,x, T,T,T,T, T,T,T,T, x,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,x,x, T,T,T,T, T,T,x,x, x,x,x,x, T,T,T,x, T,T,T,x, T,x,T,x, x,T,T,x, T,T,x,T, x,x,x,T, x,T,x,T, x,x,T,T, x,x,x,T, x,T,x,x, x,x,T,T, x,x,T,x, x,T,x,x, T,x,T,T, x,x,x,x, T,x,T,x, x,x,x,x, T,x,x,x, x,T,x,T, x,T,T,T, T,T,x,x, x,T,T,T, T,x,T,x, T,x,x,T, T,T,x,T, x,T,T,T, T,T,T,T, T,T,T,x, x,x,T,T, x,T,x,x, x,x,x,x},
	{x,x,T,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,T,x, T,T,T,T, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, T,x,x,T, x,x,x,x, x,x,x,x, T,x,x,x, x,T,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,x,T, x,x,x,T, x,x,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, T,x,T,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, T,T,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,T,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x}

	};
} // end Parser

}