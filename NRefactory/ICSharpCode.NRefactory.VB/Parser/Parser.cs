using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using ICSharpCode.NRefactory.VB.Ast;
using ICSharpCode.NRefactory.VB.Parser;
using ASTAttribute = ICSharpCode.NRefactory.VB.Ast.Attribute;




namespace ICSharpCode.NRefactory.VB.Parser {


// ----------------------------------------------------------------------------
// Parser
// ----------------------------------------------------------------------------
//! A Coco/R Parser
partial class VBParser
{
	public const int _EOF = 0;
	public const int _EOL = 1;
	public const int _ident = 2;
	public const int _LiteralString = 3;
	public const int _LiteralCharacter = 4;
	public const int _LiteralInteger = 5;
	public const int _LiteralDouble = 6;
	public const int _LiteralSingle = 7;
	public const int _LiteralDecimal = 8;
	public const int _LiteralDate = 9;
	public const int _XmlOpenTag = 10;
	public const int _XmlCloseTag = 11;
	public const int _XmlStartInlineVB = 12;
	public const int _XmlEndInlineVB = 13;
	public const int _XmlCloseTagEmptyElement = 14;
	public const int _XmlOpenEndTag = 15;
	public const int _XmlContent = 16;
	public const int _XmlComment = 17;
	public const int _XmlCData = 18;
	public const int _XmlProcessingInstruction = 19;
	public const int maxT = 238;  //<! max term (w/o pragmas)
	public const int StatementEndOfStmt = 1;

	const bool T = true;
	const bool x = false;
	const int minErrDist = 2;

	public Errors  errors;




	void Get () {
		lexer.NextToken();

	}

	bool StartOf (int s) {
		return set[s].Get(la.kind);
	}

	void ExpectWeak (int n, int follow) {
		if (la.kind == n) Get();
		else {
			SynErr(n);
			while (!StartOf(follow)) Get();
		}
	}


	bool WeakSeparator(int n, int syFol, int repFol) {
		int kind = la.kind;
		if (kind == n) {Get(); return true;}
		else if (StartOf(repFol)) {return false;}
		else {
			SynErr(n);
			while (!(set[syFol].Get(kind) || set[repFol].Get(kind) || set[0].Get(kind))) {
				Get();
				kind = la.kind;
			}
			return StartOf(syFol);
		}
	}


	void VB() {
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
		while (IsGlobalAttrTarget()) {
			GlobalAttributeSection();
			while (la.kind == 1 || la.kind == 21) {
				EndOfStmt();
			}
		}
		while (StartOf(2)) {
			NamespaceMemberDecl();
			while (la.kind == 1 || la.kind == 21) {
				EndOfStmt();
			}
		}
	}

	void EndOfStmt() {
		while (!(la.kind == 0 || la.kind == 1 || la.kind == 21)) {SynErr(239); Get();}
		if (la.kind == 1) {
			Get();
		} else if (la.kind == 21) {
			Get();
		} else SynErr(240);
	}

	void OptionStmt() {
		INode node = null; bool val = true;
		Expect(173);
		Location startPos = t.Location;
		if (la.kind == 121) {
			Get();
			if (la.kind == 170 || la.kind == 171) {
				OptionValue(ref val);
			}
			node = new OptionDeclaration(OptionType.Explicit, val);
		} else if (la.kind == 207) {
			Get();
			if (la.kind == 170 || la.kind == 171) {
				OptionValue(ref val);
			}
			node = new OptionDeclaration(OptionType.Strict, val);
		} else if (la.kind == 87) {
			Get();
			if (la.kind == 67) {
				Get();
				node = new OptionDeclaration(OptionType.CompareBinary, val);
			} else if (la.kind == 213) {
				Get();
				node = new OptionDeclaration(OptionType.CompareText, val);
			} else SynErr(241);
		} else if (la.kind == 139) {
			Get();
			if (la.kind == 170 || la.kind == 171) {
				OptionValue(ref val);
			}
			node = new OptionDeclaration(OptionType.Infer, val);
		} else SynErr(242);
		EndOfStmt();
		if (node != null) {
				node.StartLocation = startPos;
				node.EndLocation   = t.Location;
				AddChild(node);
			}

	}

	void ImportsStmt() {
		List<Using> usings = new List<Using>();

		Expect(137);
		Location startPos = t.Location;
			Using u;

		ImportClause(out u);
		if (u != null) { usings.Add(u); }
		while (la.kind == 22) {
			Get();
			ImportClause(out u);
			if (u != null) { usings.Add(u); }
		}
		EndOfStmt();
		UsingDeclaration usingDeclaration = new UsingDeclaration(usings);
			usingDeclaration.StartLocation = startPos;
			usingDeclaration.EndLocation   = t.Location;
			AddChild(usingDeclaration);

	}

	void GlobalAttributeSection() {
		Expect(40);
		Location startPos = t.Location;
		if (la.kind == 65) {
			Get();
		} else if (la.kind == 155) {
			Get();
		} else SynErr(243);
		string attributeTarget = t.val != null ? t.val.ToLower(System.Globalization.CultureInfo.InvariantCulture) : null;
			List<ASTAttribute> attributes = new List<ASTAttribute>();
			ASTAttribute attribute;

		Expect(21);
		Attribute(out attribute);
		attributes.Add(attribute);
		while (NotFinalComma()) {
			if (la.kind == 22) {
				Get();
				if (la.kind == 65) {
					Get();
				} else if (la.kind == 155) {
					Get();
				} else SynErr(244);
				Expect(21);
			}
			Attribute(out attribute);
			attributes.Add(attribute);
		}
		if (la.kind == 22) {
			Get();
		}
		Expect(39);
		EndOfStmt();
		AttributeSection section = new AttributeSection {
				AttributeTarget = attributeTarget,
				Attributes = attributes,
				StartLocation = startPos,
				EndLocation = t.EndLocation
			};
			AddChild(section);

	}

	void NamespaceMemberDecl() {
		ModifierList m = new ModifierList();
		AttributeSection section;
		List<AttributeSection> attributes = new List<AttributeSection>();
		string qualident;

		if (la.kind == 160) {
			Get();
			Location startPos = t.Location;

			Qualident(out qualident);
			INode node =  new NamespaceDeclaration(qualident);
				node.StartLocation = startPos;
				AddChild(node);
				BlockStart(node);

			EndOfStmt();
			NamespaceBody();
			node.EndLocation = t.Location;
				BlockEnd();

		} else if (StartOf(3)) {
			while (la.kind == 40) {
				AttributeSection(out section);
				attributes.Add(section);
			}
			while (StartOf(4)) {
				TypeModifier(m);
			}
			NonModuleDeclaration(m, attributes);
		} else SynErr(245);
	}

	void OptionValue(ref bool val) {
		if (la.kind == 171) {
			Get();
			val = true;
		} else if (la.kind == 170) {
			Get();
			val = false;
		} else SynErr(246);
	}

	void ImportClause(out Using u) {
		string qualident  = null;
		TypeReference aliasedType = null;
		u = null;

		if (StartOf(5)) {
			Qualident(out qualident);
			if (la.kind == 20) {
				Get();
				TypeName(out aliasedType);
			}
			if (qualident != null && qualident.Length > 0) {
					if (aliasedType != null) {
						u = new Using(qualident, aliasedType);
					} else {
						u = new Using(qualident);
					}
				}

		} else if (la.kind == 10) {
			string prefix = null;
			Get();
			Identifier();
			prefix = t.val;
			Expect(20);
			Expect(3);
			u = new Using(t.literalValue as string, prefix);
			Expect(11);
		} else SynErr(247);
	}

	void Qualident(out string qualident) {
		string name;
		qualidentBuilder.Length = 0; 

		Identifier();
		qualidentBuilder.Append(t.val);
		while (DotAndIdentOrKw()) {
			Expect(26);
			IdentifierOrKeyword(out name);
			qualidentBuilder.Append('.'); qualidentBuilder.Append(name);
		}
		qualident = qualidentBuilder.ToString();
	}

	void TypeName(out TypeReference typeref) {
		ArrayList rank = null; Location startLocation = la.Location;
		NonArrayTypeName(out typeref, false);
		ArrayTypeModifiers(out rank);
		if (typeref != null) {
				if (rank != null) {
					typeref.RankSpecifier = (int[])rank.ToArray(typeof(int));
				}
				typeref.StartLocation = startLocation;
				typeref.EndLocation = t.EndLocation;
			}

	}

	void Identifier() {
		if (StartOf(6)) {
			IdentifierForFieldDeclaration();
		} else if (la.kind == 98) {
			Get();
		} else SynErr(248);
	}

	void NamespaceBody() {
		while (la.kind == 1 || la.kind == 21) {
			EndOfStmt();
		}
		while (StartOf(2)) {
			NamespaceMemberDecl();
			while (la.kind == 1 || la.kind == 21) {
				EndOfStmt();
			}
		}
		Expect(113);
		Expect(160);
		EndOfStmt();
	}

	void AttributeSection(out AttributeSection section) {
		string attributeTarget = "";
		List<ASTAttribute> attributes = new List<ASTAttribute>();
		ASTAttribute attribute;
		Location startLocation = la.Location;

		Expect(40);
		if (IsLocalAttrTarget()) {
			if (la.kind == 119) {
				Get();
				attributeTarget = "event";
			} else if (la.kind == 195) {
				Get();
				attributeTarget = "return";
			} else if (StartOf(5)) {
				Identifier();
				string val = t.val.ToLower(System.Globalization.CultureInfo.InvariantCulture);
					if (val != "field"	|| val != "method" ||
						val != "module" || val != "param"  ||
						val != "property" || val != "type")
					Error("attribute target specifier (event, return, field," +
							"method, module, param, property, or type) expected");
					attributeTarget = t.val;

			} else SynErr(249);
			Expect(21);
		}
		Attribute(out attribute);
		attributes.Add(attribute);
		while (NotFinalComma()) {
			Expect(22);
			Attribute(out attribute);
			attributes.Add(attribute);
		}
		if (la.kind == 22) {
			Get();
		}
		Expect(39);
		section = new AttributeSection {
				AttributeTarget = attributeTarget,
				Attributes = attributes,
				StartLocation = startLocation,
				EndLocation = t.EndLocation
			};

	}

	void TypeModifier(ModifierList m) {
		switch (la.kind) {
		case 188: {
			Get();
			m.Add(Modifiers.Public, t.Location);
			break;
		}
		case 187: {
			Get();
			m.Add(Modifiers.Protected, t.Location);
			break;
		}
		case 125: {
			Get();
			m.Add(Modifiers.Internal, t.Location);
			break;
		}
		case 185: {
			Get();
			m.Add(Modifiers.Private, t.Location);
			break;
		}
		case 200: {
			Get();
			m.Add(Modifiers.Static, t.Location);
			break;
		}
		case 199: {
			Get();
			m.Add(Modifiers.New, t.Location);
			break;
		}
		case 156: {
			Get();
			m.Add(Modifiers.Abstract, t.Location);
			break;
		}
		case 166: {
			Get();
			m.Add(Modifiers.Sealed, t.Location);
			break;
		}
		case 183: {
			Get();
			m.Add(Modifiers.Partial, t.Location);
			break;
		}
		default: SynErr(250); break;
		}
	}

	void NonModuleDeclaration(ModifierList m, List<AttributeSection> attributes) {
		TypeReference typeRef = null;
		List<TypeReference> baseInterfaces = null;

		switch (la.kind) {
		case 84: {
			m.Check(Modifiers.Classes);
			Get();
			TypeDeclaration newType = new TypeDeclaration(m.Modifier, attributes);
				newType.StartLocation = t.Location;
				AddChild(newType);
				BlockStart(newType);
				
				newType.Type       = ClassType.Class;

			Identifier();
			newType.Name = t.val;
			TypeParameterList(newType.Templates);
			EndOfStmt();
			newType.BodyStartLocation = t.Location;
			if (la.kind == 140) {
				ClassBaseType(out typeRef);
				SafeAdd(newType, newType.BaseTypes, typeRef);
			}
			while (la.kind == 136) {
				TypeImplementsClause(out baseInterfaces);
				newType.BaseTypes.AddRange(baseInterfaces);
			}
			ClassBody(newType);
			Expect(113);
			Expect(84);
			newType.EndLocation = t.EndLocation;
			EndOfStmt();
			BlockEnd();

			break;
		}
		case 155: {
			Get();
			m.Check(Modifiers.VBModules);
				TypeDeclaration newType = new TypeDeclaration(m.Modifier, attributes);
				AddChild(newType);
				BlockStart(newType);
				newType.StartLocation = m.GetDeclarationLocation(t.Location);
				newType.Type = ClassType.Module;

			Identifier();
			newType.Name = t.val;
			EndOfStmt();
			newType.BodyStartLocation = t.Location;
			ModuleBody(newType);
			BlockEnd();

			break;
		}
		case 209: {
			Get();
			m.Check(Modifiers.VBStructures);
				TypeDeclaration newType = new TypeDeclaration(m.Modifier, attributes);
				AddChild(newType);
				BlockStart(newType);
				newType.StartLocation = m.GetDeclarationLocation(t.Location);
				newType.Type = ClassType.Struct;

			Identifier();
			newType.Name = t.val;
			TypeParameterList(newType.Templates);
			EndOfStmt();
			newType.BodyStartLocation = t.Location;
			while (la.kind == 136) {
				TypeImplementsClause(out baseInterfaces);
				newType.BaseTypes.AddRange(baseInterfaces);
			}
			StructureBody(newType);
			BlockEnd();

			break;
		}
		case 115: {
			Get();
			m.Check(Modifiers.VBEnums);
				TypeDeclaration newType = new TypeDeclaration(m.Modifier, attributes);
				newType.StartLocation = m.GetDeclarationLocation(t.Location);
				AddChild(newType);
				BlockStart(newType);
				
				newType.Type = ClassType.Enum;

			Identifier();
			newType.Name = t.val;
			if (la.kind == 63) {
				Get();
				NonArrayTypeName(out typeRef, false);
				SafeAdd(newType, newType.BaseTypes, typeRef);
			}
			EndOfStmt();
			newType.BodyStartLocation = t.Location;
			EnumBody(newType);
			BlockEnd();

			break;
		}
		case 142: {
			Get();
			m.Check(Modifiers.VBInterfacs);
				TypeDeclaration newType = new TypeDeclaration(m.Modifier, attributes);
				newType.StartLocation = m.GetDeclarationLocation(t.Location);
				AddChild(newType);
				BlockStart(newType);
				newType.Type = ClassType.Interface;

			Identifier();
			newType.Name = t.val;
			TypeParameterList(newType.Templates);
			EndOfStmt();
			newType.BodyStartLocation = t.Location;
			while (la.kind == 140) {
				InterfaceBase(out baseInterfaces);
				newType.BaseTypes.AddRange(baseInterfaces);
			}
			InterfaceBody(newType);
			BlockEnd();

			break;
		}
		case 103: {
			Get();
			m.Check(Modifiers.VBDelegates);
				DelegateDeclaration delegateDeclr = new DelegateDeclaration(m.Modifier, attributes);
				delegateDeclr.ReturnType = new TypeReference("System.Void", true);
				delegateDeclr.StartLocation = m.GetDeclarationLocation(t.Location);
				List<ParameterDeclarationExpression> p = new List<ParameterDeclarationExpression>();

			if (la.kind == 210) {
				Get();
				Identifier();
				delegateDeclr.Name = t.val;
				TypeParameterList(delegateDeclr.Templates);
				if (la.kind == 37) {
					Get();
					if (StartOf(7)) {
						FormalParameterList(p);
					}
					Expect(38);
					delegateDeclr.Parameters = p;
				}
			} else if (la.kind == 127) {
				Get();
				Identifier();
				delegateDeclr.Name = t.val;
				TypeParameterList(delegateDeclr.Templates);
				if (la.kind == 37) {
					Get();
					if (StartOf(7)) {
						FormalParameterList(p);
					}
					Expect(38);
					delegateDeclr.Parameters = p;
				}
				if (la.kind == 63) {
					Get();
					TypeReference type;
					TypeName(out type);
					delegateDeclr.ReturnType = type;
				}
			} else SynErr(251);
			delegateDeclr.EndLocation = t.EndLocation;
			EndOfStmt();
			AddChild(delegateDeclr);

			break;
		}
		default: SynErr(252); break;
		}
	}

	void TypeParameterList(List<TemplateDefinition> templates) {
		TemplateDefinition template;

		if (la.kind == Tokens.OpenParenthesis && Peek(1).kind == Tokens.Of) {
			Expect(37);
			Expect(169);
			TypeParameter(out template);
			if (template != null) templates.Add(template);

			while (la.kind == 22) {
				Get();
				TypeParameter(out template);
				if (template != null) templates.Add(template);

			}
			Expect(38);
		}
	}

	void TypeParameter(out TemplateDefinition template) {
		VarianceModifier modifier = VarianceModifier.Invariant; Location startLocation = la.Location;
		if (la.kind == 138 || (la.kind == Tokens.Out && IsIdentifierToken(Peek(1)))) {
			if (la.kind == 138) {
				Get();
				modifier = VarianceModifier.Contravariant;
			} else {
				Expect(178);
				modifier = VarianceModifier.Covariant;
			}
		}
		Identifier();
		template = new TemplateDefinition(t.val, null) { VarianceModifier = modifier };
		if (la.kind == 63) {
			TypeParameterConstraints(template);
		}
		if (template != null) {
				template.StartLocation = startLocation;
				template.EndLocation = t.EndLocation;
			}

	}

	void TypeParameterConstraints(TemplateDefinition template) {
		TypeReference constraint;

		Expect(63);
		if (la.kind == 35) {
			Get();
			TypeParameterConstraint(out constraint);
			if (constraint != null) { template.Bases.Add(constraint); }
			while (la.kind == 22) {
				Get();
				TypeParameterConstraint(out constraint);
				if (constraint != null) { template.Bases.Add(constraint); }
			}
			Expect(36);
		} else if (StartOf(8)) {
			TypeParameterConstraint(out constraint);
			if (constraint != null) { template.Bases.Add(constraint); }
		} else SynErr(253);
	}

	void TypeParameterConstraint(out TypeReference constraint) {
		constraint = null; Location startLocation = la.Location;
		if (la.kind == 84) {
			Get();
			constraint = TypeReference.ClassConstraint;
		} else if (la.kind == 209) {
			Get();
			constraint = TypeReference.StructConstraint;
		} else if (la.kind == 162) {
			Get();
			constraint = TypeReference.NewConstraint;
		} else if (StartOf(9)) {
			TypeName(out constraint);
		} else SynErr(254);
	}

	void ClassBaseType(out TypeReference typeRef) {
		typeRef = null;

		Expect(140);
		TypeName(out typeRef);
		EndOfStmt();
	}

	void TypeImplementsClause(out List<TypeReference> baseInterfaces) {
		baseInterfaces = new List<TypeReference>();
		TypeReference type = null;

		Expect(136);
		TypeName(out type);
		if (type != null) baseInterfaces.Add(type);

		while (la.kind == 22) {
			Get();
			TypeName(out type);
			if (type != null) baseInterfaces.Add(type);
		}
		EndOfStmt();
	}

	void ClassBody(TypeDeclaration newType) {
		AttributeSection section;
		while (la.kind == 1 || la.kind == 21) {
			EndOfStmt();
		}
		while (StartOf(10)) {
			List<AttributeSection> attributes = new List<AttributeSection>();
				ModifierList m = new ModifierList();

			while (la.kind == 40) {
				AttributeSection(out section);
				attributes.Add(section);
			}
			while (StartOf(11)) {
				MemberModifier(m);
			}
			ClassMemberDecl(m, attributes);
			while (la.kind == 1 || la.kind == 21) {
				EndOfStmt();
			}
		}
	}

	void ModuleBody(TypeDeclaration newType) {
		AttributeSection section;
		while (la.kind == 1 || la.kind == 21) {
			EndOfStmt();
		}
		while (StartOf(10)) {
			List<AttributeSection> attributes = new List<AttributeSection>();
				ModifierList m = new ModifierList();

			while (la.kind == 40) {
				AttributeSection(out section);
				attributes.Add(section);
			}
			while (StartOf(11)) {
				MemberModifier(m);
			}
			ClassMemberDecl(m, attributes);
			while (la.kind == 1 || la.kind == 21) {
				EndOfStmt();
			}
		}
		Expect(113);
		Expect(155);
		newType.EndLocation = t.EndLocation;
		EndOfStmt();
	}

	void StructureBody(TypeDeclaration newType) {
		AttributeSection section;
		while (la.kind == 1 || la.kind == 21) {
			EndOfStmt();
		}
		while (StartOf(10)) {
			List<AttributeSection> attributes = new List<AttributeSection>();
				ModifierList m = new ModifierList();

			while (la.kind == 40) {
				AttributeSection(out section);
				attributes.Add(section);
			}
			while (StartOf(11)) {
				MemberModifier(m);
			}
			StructureMemberDecl(m, attributes);
			while (la.kind == 1 || la.kind == 21) {
				EndOfStmt();
			}
		}
		Expect(113);
		Expect(209);
		newType.EndLocation = t.EndLocation;
		EndOfStmt();
	}

	void NonArrayTypeName(out TypeReference typeref, bool canBeUnbound) {
		string name;
		typeref = null;
		bool isGlobal = false;

		if (StartOf(12)) {
			if (la.kind == 130) {
				Get();
				Expect(26);
				isGlobal = true;
			}
			QualIdentAndTypeArguments(out typeref, canBeUnbound);
			typeref.IsGlobal = isGlobal;
			while (la.kind == 26) {
				Get();
				TypeReference nestedTypeRef;
				QualIdentAndTypeArguments(out nestedTypeRef, canBeUnbound);
				typeref = new InnerClassTypeReference(typeref, nestedTypeRef.Type, nestedTypeRef.GenericTypes);
			}
		} else if (la.kind == 168) {
			Get();
			typeref = new TypeReference("System.Object", true);
			if (la.kind == 33) {
				Get();
				List<TypeReference> typeArguments = new List<TypeReference>(1);
				  	if (typeref != null) typeArguments.Add(typeref);
					typeref = new TypeReference("System.Nullable", typeArguments) { IsKeyword = true };

			}
		} else if (StartOf(13)) {
			PrimitiveTypeName(out name);
			typeref = new TypeReference(name, true);
			if (la.kind == 33) {
				Get();
				List<TypeReference> typeArguments = new List<TypeReference>(1);
				  	if (typeref != null) typeArguments.Add(typeref);
					typeref = new TypeReference("System.Nullable", typeArguments) { IsKeyword = true };

			}
		} else SynErr(255);
	}

	void EnumBody(TypeDeclaration newType) {
		FieldDeclaration f;
		while (la.kind == 1 || la.kind == 21) {
			EndOfStmt();
		}
		while (StartOf(14)) {
			EnumMemberDecl(out f);
			AddChild(f);

			while (la.kind == 1 || la.kind == 21) {
				EndOfStmt();
			}
		}
		Expect(113);
		Expect(115);
		newType.EndLocation = t.EndLocation;
		EndOfStmt();
	}

	void InterfaceBase(out List<TypeReference> bases) {
		TypeReference type;
		bases = new List<TypeReference>();

		Expect(140);
		TypeName(out type);
		if (type != null) bases.Add(type);
		while (la.kind == 22) {
			Get();
			TypeName(out type);
			if (type != null) bases.Add(type);
		}
		EndOfStmt();
	}

	void InterfaceBody(TypeDeclaration newType) {
		while (la.kind == 1 || la.kind == 21) {
			EndOfStmt();
		}
		while (StartOf(15)) {
			InterfaceMemberDecl();
			while (la.kind == 1 || la.kind == 21) {
				EndOfStmt();
			}
		}
		Expect(113);
		Expect(142);
		newType.EndLocation = t.EndLocation;
		EndOfStmt();
	}

	void FormalParameterList(List<ParameterDeclarationExpression> parameter) {
		ParameterDeclarationExpression p;
		FormalParameter(out p);
		if (p != null) parameter.Add(p);
		while (la.kind == 22) {
			Get();
			FormalParameter(out p);
			if (p != null) parameter.Add(p);
		}
	}

	void MemberModifier(ModifierList m) {
		switch (la.kind) {
		case 156: {
			Get();
			m.Add(Modifiers.Abstract, t.Location);
			break;
		}
		case 102: {
			Get();
			m.Add(Modifiers.Default, t.Location);
			break;
		}
		case 125: {
			Get();
			m.Add(Modifiers.Internal, t.Location);
			break;
		}
		case 199: {
			Get();
			m.Add(Modifiers.New, t.Location);
			break;
		}
		case 181: {
			Get();
			m.Add(Modifiers.Override, t.Location);
			break;
		}
		case 157: {
			Get();
			m.Add(Modifiers.Abstract, t.Location);
			break;
		}
		case 185: {
			Get();
			m.Add(Modifiers.Private, t.Location);
			break;
		}
		case 187: {
			Get();
			m.Add(Modifiers.Protected, t.Location);
			break;
		}
		case 188: {
			Get();
			m.Add(Modifiers.Public, t.Location);
			break;
		}
		case 166: {
			Get();
			m.Add(Modifiers.Sealed, t.Location);
			break;
		}
		case 167: {
			Get();
			m.Add(Modifiers.Sealed, t.Location);
			break;
		}
		case 200: {
			Get();
			m.Add(Modifiers.Static, t.Location);
			break;
		}
		case 180: {
			Get();
			m.Add(Modifiers.Virtual, t.Location);
			break;
		}
		case 179: {
			Get();
			m.Add(Modifiers.Overloads, t.Location);
			break;
		}
		case 190: {
			Get();
			m.Add(Modifiers.ReadOnly, t.Location);
			break;
		}
		case 235: {
			Get();
			m.Add(Modifiers.WriteOnly, t.Location);
			break;
		}
		case 234: {
			Get();
			m.Add(Modifiers.WithEvents, t.Location);
			break;
		}
		case 105: {
			Get();
			m.Add(Modifiers.Dim, t.Location);
			break;
		}
		case 183: {
			Get();
			m.Add(Modifiers.Partial, t.Location);
			break;
		}
		default: SynErr(256); break;
		}
	}

	void ClassMemberDecl(ModifierList m, List<AttributeSection> attributes) {
		StructureMemberDecl(m, attributes);
	}

	void StructureMemberDecl(ModifierList m, List<AttributeSection> attributes) {
		TypeReference type = null;
		List<ParameterDeclarationExpression> p = new List<ParameterDeclarationExpression>();
		Statement stmt = null;
		List<VariableDeclaration> variableDeclarators = new List<VariableDeclaration>();
		List<TemplateDefinition> templates = new List<TemplateDefinition>();

		switch (la.kind) {
		case 84: case 103: case 115: case 142: case 155: case 209: {
			NonModuleDeclaration(m, attributes);
			break;
		}
		case 210: {
			Get();
			Location startPos = t.Location;

			if (StartOf(5)) {
				string name = String.Empty;
					MethodDeclaration methodDeclaration; List<string> handlesClause = null;
					List<InterfaceImplementation> implementsClause = null;

				Identifier();
				name = t.val;
					m.Check(Modifiers.VBMethods);

				TypeParameterList(templates);
				if (la.kind == 37) {
					Get();
					if (StartOf(7)) {
						FormalParameterList(p);
					}
					Expect(38);
				}
				if (la.kind == 134 || la.kind == 136) {
					if (la.kind == 136) {
						ImplementsClause(out implementsClause);
					} else {
						HandlesClause(out handlesClause);
					}
				}
				Location endLocation = t.EndLocation;
				if (IsMustOverride(m)) {
					EndOfStmt();
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
					Get();
					methodDeclaration = new MethodDeclaration {
							Name = name, Modifier = m.Modifier, Parameters = p, Attributes = attributes,
							StartLocation = m.GetDeclarationLocation(startPos), EndLocation = endLocation,
							TypeReference = new TypeReference("System.Void", true),
							Templates = templates,
							HandlesClause = handlesClause,
							InterfaceImplementations = implementsClause
						};
						AddChild(methodDeclaration);

					if (ParseMethodBodies) {
					Block(out stmt);
					Expect(113);
					Expect(210);
					} else {
						// don't parse method body
						lexer.SkipCurrentBlock(Tokens.Sub); stmt = new BlockStatement();
					   }

					methodDeclaration.Body  = (BlockStatement)stmt;
					methodDeclaration.Body.EndLocation = t.EndLocation;
					EndOfStmt();
				} else SynErr(257);
			} else if (la.kind == 162) {
				Get();
				if (la.kind == 37) {
					Get();
					if (StartOf(7)) {
						FormalParameterList(p);
					}
					Expect(38);
				}
				m.Check(Modifiers.Constructors);
				Location constructorEndLocation = t.EndLocation;
				Expect(1);
				if (ParseMethodBodies) {
				Block(out stmt);
				Expect(113);
				Expect(210);
				} else {
					// don't parse method body
					lexer.SkipCurrentBlock(Tokens.Sub); stmt = new BlockStatement();
				   }

				Location endLocation = t.EndLocation;
				EndOfStmt();
				ConstructorDeclaration cd = new ConstructorDeclaration("New", m.Modifier, p, attributes);
					cd.StartLocation = m.GetDeclarationLocation(startPos);
					cd.EndLocation   = constructorEndLocation;
					cd.Body = (BlockStatement)stmt;
					cd.Body.EndLocation   = endLocation;
					AddChild(cd);

			} else SynErr(258);
			break;
		}
		case 127: {
			Get();
			m.Check(Modifiers.VBMethods);
				string name = String.Empty;
				Location startPos = t.Location;
				MethodDeclaration methodDeclaration;List<string> handlesClause = null;
				List<InterfaceImplementation> implementsClause = null;
				AttributeSection returnTypeAttributeSection = null;

			Identifier();
			name = t.val;
			TypeParameterList(templates);
			if (la.kind == 37) {
				Get();
				if (StartOf(7)) {
					FormalParameterList(p);
				}
				Expect(38);
			}
			if (la.kind == 63) {
				Get();
				while (la.kind == 40) {
					AttributeSection(out returnTypeAttributeSection);
					if (returnTypeAttributeSection != null) {
							returnTypeAttributeSection.AttributeTarget = "return";
							attributes.Add(returnTypeAttributeSection);
						}

				}
				TypeName(out type);
			}
			if(type == null) {
					type = new TypeReference("System.Object", true);
				}

			if (la.kind == 134 || la.kind == 136) {
				if (la.kind == 136) {
					ImplementsClause(out implementsClause);
				} else {
					HandlesClause(out handlesClause);
				}
			}
			Location endLocation = t.EndLocation;
			if (IsMustOverride(m)) {
				EndOfStmt();
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
				Get();
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
				Block(out stmt);
				Expect(113);
				Expect(127);
				} else {
						// don't parse method body
						lexer.SkipCurrentBlock(Tokens.Function); stmt = new BlockStatement();
					}
					methodDeclaration.Body = (BlockStatement)stmt;
					methodDeclaration.Body.StartLocation = methodDeclaration.EndLocation;
					methodDeclaration.Body.EndLocation   = t.EndLocation;

				EndOfStmt();
			} else SynErr(259);
			break;
		}
		case 101: {
			Get();
			m.Check(Modifiers.VBExternalMethods);
				Location startPos = t.Location;
				CharsetModifier charsetModifer = CharsetModifier.None;
				string library = String.Empty;
				string alias = null;
				string name = String.Empty;

			if (StartOf(16)) {
				Charset(out charsetModifer);
			}
			if (la.kind == 210) {
				Get();
				Identifier();
				name = t.val;
				Expect(149);
				Expect(3);
				library = t.literalValue as string;
				if (la.kind == 59) {
					Get();
					Expect(3);
					alias = t.literalValue as string;
				}
				if (la.kind == 37) {
					Get();
					if (StartOf(7)) {
						FormalParameterList(p);
					}
					Expect(38);
				}
				EndOfStmt();
				DeclareDeclaration declareDeclaration = new DeclareDeclaration(name, m.Modifier, null, p, attributes, library, alias, charsetModifer);
					declareDeclaration.StartLocation = m.GetDeclarationLocation(startPos);
					declareDeclaration.EndLocation   = t.EndLocation;
					AddChild(declareDeclaration);

			} else if (la.kind == 127) {
				Get();
				Identifier();
				name = t.val;
				Expect(149);
				Expect(3);
				library = t.literalValue as string;
				if (la.kind == 59) {
					Get();
					Expect(3);
					alias = t.literalValue as string;
				}
				if (la.kind == 37) {
					Get();
					if (StartOf(7)) {
						FormalParameterList(p);
					}
					Expect(38);
				}
				if (la.kind == 63) {
					Get();
					TypeName(out type);
				}
				EndOfStmt();
				DeclareDeclaration declareDeclaration = new DeclareDeclaration(name, m.Modifier, type, p, attributes, library, alias, charsetModifer);
					declareDeclaration.StartLocation = m.GetDeclarationLocation(startPos);
					declareDeclaration.EndLocation   = t.EndLocation;
					AddChild(declareDeclaration);

			} else SynErr(260);
			break;
		}
		case 119: {
			Get();
			m.Check(Modifiers.VBEvents);
				Location startPos = t.Location;
				EventDeclaration eventDeclaration;
				string name = String.Empty;
				List<InterfaceImplementation> implementsClause = null;

			Identifier();
			name= t.val;
			if (la.kind == 63) {
				Get();
				TypeName(out type);
			} else if (StartOf(17)) {
				if (la.kind == 37) {
					Get();
					if (StartOf(7)) {
						FormalParameterList(p);
					}
					Expect(38);
				}
			} else SynErr(261);
			if (la.kind == 136) {
				ImplementsClause(out implementsClause);
			}
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
			m.Check(Modifiers.Fields);
				FieldDeclaration fd = new FieldDeclaration(attributes, null, m.Modifier);

			IdentifierForFieldDeclaration();
			string name = t.val;
			fd.StartLocation = m.GetDeclarationLocation(t.Location);
			VariableDeclaratorPartAfterIdentifier(variableDeclarators, name);
			while (la.kind == 22) {
				Get();
				VariableDeclarator(variableDeclarators);
			}
			EndOfStmt();
			fd.EndLocation = t.EndLocation;
				fd.Fields = variableDeclarators;
				AddChild(fd);

			break;
		}
		case 88: {
			m.Check(Modifiers.Fields);
			Get();
			m.Add(Modifiers.Const, t.Location); 
			FieldDeclaration fd = new FieldDeclaration(attributes, type, m.Modifier);
				fd.StartLocation = m.GetDeclarationLocation(t.Location);
				List<VariableDeclaration> constantDeclarators = new List<VariableDeclaration>();

			ConstantDeclarator(constantDeclarators);
			while (la.kind == 22) {
				Get();
				ConstantDeclarator(constantDeclarators);
			}
			fd.Fields = constantDeclarators;
				fd.EndLocation = t.Location;

			EndOfStmt();
			fd.EndLocation = t.EndLocation;
				AddChild(fd);

			break;
		}
		case 186: {
			Get();
			m.Check(Modifiers.VBProperties);
				Location startPos = t.Location;
				List<InterfaceImplementation> implementsClause = null;
				AttributeSection returnTypeAttributeSection = null;
				Expression initializer = null;

			Identifier();
			string propertyName = t.val;
			if (la.kind == 37) {
				Get();
				if (StartOf(7)) {
					FormalParameterList(p);
				}
				Expect(38);
			}
			if (la.kind == 63) {
				Get();
				while (la.kind == 40) {
					AttributeSection(out returnTypeAttributeSection);
					if (returnTypeAttributeSection != null) {
							returnTypeAttributeSection.AttributeTarget = "return";
							attributes.Add(returnTypeAttributeSection);
						}

				}
				if (IsNewExpression()) {
					ObjectCreateExpression(out initializer);
					if (initializer is ObjectCreateExpression) {
							type = ((ObjectCreateExpression)initializer).CreateType.Clone();
						} else {
							type = ((ArrayCreateExpression)initializer).CreateType.Clone();
						}

				} else if (StartOf(9)) {
					TypeName(out type);
				} else SynErr(262);
			}
			if (la.kind == 20) {
				Get();
				Expr(out initializer);
			}
			if (la.kind == 136) {
				ImplementsClause(out implementsClause);
			}
			EndOfStmt();
			if (IsMustOverride(m) || IsAutomaticProperty()) {
				PropertyDeclaration pDecl = new PropertyDeclaration(propertyName, type, m.Modifier, attributes);
					pDecl.StartLocation = m.GetDeclarationLocation(startPos);
					pDecl.EndLocation   = t.Location;
					pDecl.TypeReference = type;
					pDecl.InterfaceImplementations = implementsClause;
					pDecl.Parameters = p;
					if (initializer != null)
						pDecl.Initializer = initializer;
					AddChild(pDecl);

			} else if (StartOf(18)) {
				PropertyDeclaration pDecl = new PropertyDeclaration(propertyName, type, m.Modifier, attributes);
					pDecl.StartLocation = m.GetDeclarationLocation(startPos);
					pDecl.EndLocation   = t.Location;
					pDecl.BodyStart   = t.Location;
					pDecl.TypeReference = type;
					pDecl.InterfaceImplementations = implementsClause;
					pDecl.Parameters = p;
					PropertyGetRegion getRegion;
					PropertySetRegion setRegion;

				AccessorDecls(out getRegion, out setRegion);
				Expect(113);
				Expect(186);
				EndOfStmt();
				pDecl.GetRegion = getRegion;
					pDecl.SetRegion = setRegion;
					pDecl.BodyEnd = t.Location; // t = EndOfStmt; not "Property"
					AddChild(pDecl);

			} else SynErr(263);
			break;
		}
		case 98: {
			Get();
			Location startPos = t.Location;
			Expect(119);
			m.Check(Modifiers.VBCustomEvents);
				EventAddRemoveRegion eventAccessorDeclaration;
				EventAddRegion addHandlerAccessorDeclaration = null;
				EventRemoveRegion removeHandlerAccessorDeclaration = null;
				EventRaiseRegion raiseEventAccessorDeclaration = null;
				List<InterfaceImplementation> implementsClause = null;

			Identifier();
			string customEventName = t.val;
			Expect(63);
			TypeName(out type);
			if (la.kind == 136) {
				ImplementsClause(out implementsClause);
			}
			EndOfStmt();
			while (StartOf(19)) {
				EventAccessorDeclaration(out eventAccessorDeclaration);
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
			ConversionType opConversionType = ConversionType.None;
			if (la.kind == 161 || la.kind == 232) {
				if (la.kind == 232) {
					Get();
					opConversionType = ConversionType.Implicit;
				} else {
					Get();
					opConversionType = ConversionType.Explicit;
				}
			}
			Expect(172);
			m.Check(Modifiers.VBOperators);
				Location startPos = t.Location;
				TypeReference returnType = NullTypeReference.Instance;
				TypeReference operandType = NullTypeReference.Instance;
				OverloadableOperatorType operatorType;
				AttributeSection section;
				ParameterDeclarationExpression param;
				List<ParameterDeclarationExpression> parameters = new List<ParameterDeclarationExpression>();

			OverloadableOperator(out operatorType);
			Expect(37);
			FormalParameter(out param);
			if (param != null) parameters.Add(param);
			if (la.kind == 22) {
				Get();
				FormalParameter(out param);
				if (param != null) parameters.Add(param);
			}
			Expect(38);
			Location endPos = t.EndLocation;
			if (la.kind == 63) {
				Get();
				while (la.kind == 40) {
					AttributeSection(out section);
					if (section != null) {
						section.AttributeTarget = "return";
						attributes.Add(section);
					}
				}
				TypeName(out returnType);
				endPos = t.EndLocation;
			}
			Expect(1);
			Block(out stmt);
			Expect(113);
			Expect(172);
			EndOfStmt();
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
		default: SynErr(264); break;
		}
	}

	void EnumMemberDecl(out FieldDeclaration f) {
		Expression expr = null;List<AttributeSection> attributes = new List<AttributeSection>();
		AttributeSection section = null;
		VariableDeclaration varDecl = null;

		while (la.kind == 40) {
			AttributeSection(out section);
			attributes.Add(section);
		}
		Identifier();
		f = new FieldDeclaration(attributes);
			varDecl = new VariableDeclaration(t.val);
			f.Fields.Add(varDecl);
			f.StartLocation = varDecl.StartLocation = t.Location;

		if (la.kind == 20) {
			Get();
			Expr(out expr);
			varDecl.Initializer = expr;
		}
		f.EndLocation = varDecl.EndLocation = t.EndLocation;
		EndOfStmt();
	}

	void InterfaceMemberDecl() {
		TypeReference type =null;
		List<ParameterDeclarationExpression> p = new List<ParameterDeclarationExpression>();
		List<TemplateDefinition> templates = new List<TemplateDefinition>();
		AttributeSection section, returnTypeAttributeSection = null;
		ModifierList mod = new ModifierList();
		List<AttributeSection> attributes = new List<AttributeSection>();
		string name;

		if (StartOf(20)) {
			while (la.kind == 40) {
				AttributeSection(out section);
				attributes.Add(section);
			}
			while (StartOf(11)) {
				MemberModifier(mod);
			}
			if (la.kind == 119) {
				Get();
				mod.Check(Modifiers.VBInterfaceEvents);
					Location startLocation = t.Location;

				Identifier();
				name = t.val;
				if (la.kind == 37) {
					Get();
					if (StartOf(7)) {
						FormalParameterList(p);
					}
					Expect(38);
				}
				if (la.kind == 63) {
					Get();
					TypeName(out type);
				}
				EndOfStmt();
				EventDeclaration ed = new EventDeclaration {
						Name = name, TypeReference = type, Modifier = mod.Modifier,
						Parameters = p, Attributes = attributes,
						StartLocation = startLocation, EndLocation = t.EndLocation
					};
					AddChild(ed);

			} else if (la.kind == 210) {
				Get();
				Location startLocation =  t.Location;
					mod.Check(Modifiers.VBInterfaceMethods);

				Identifier();
				name = t.val;
				TypeParameterList(templates);
				if (la.kind == 37) {
					Get();
					if (StartOf(7)) {
						FormalParameterList(p);
					}
					Expect(38);
				}
				EndOfStmt();
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
				Get();
				mod.Check(Modifiers.VBInterfaceMethods);
					Location startLocation = t.Location;

				Identifier();
				name = t.val;
				TypeParameterList(templates);
				if (la.kind == 37) {
					Get();
					if (StartOf(7)) {
						FormalParameterList(p);
					}
					Expect(38);
				}
				if (la.kind == 63) {
					Get();
					while (la.kind == 40) {
						AttributeSection(out returnTypeAttributeSection);
					}
					TypeName(out type);
				}
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
				Get();
				Location startLocation = t.Location;
					mod.Check(Modifiers.VBInterfaceProperties);

				Identifier();
				name = t.val; 
				if (la.kind == 37) {
					Get();
					if (StartOf(7)) {
						FormalParameterList(p);
					}
					Expect(38);
				}
				if (la.kind == 63) {
					Get();
					TypeName(out type);
				}
				if(type == null) {
						type = new TypeReference("System.Object", true);
					}

				EndOfStmt();
				PropertyDeclaration pd = new PropertyDeclaration(name, type, mod.Modifier, attributes);
					pd.Parameters = p;
					pd.EndLocation = t.EndLocation;
					pd.StartLocation = startLocation;
					AddChild(pd);

			} else SynErr(265);
		} else if (StartOf(21)) {
			NonModuleDeclaration(mod, attributes);
		} else SynErr(266);
	}

	void Expr(out Expression expr) {
		expr = null; Location startLocation = la.Location;
		if (IsQueryExpression()) {
			QueryExpr(out expr);
		} else if (la.kind == 127 || la.kind == 210) {
			LambdaExpr(out expr);
		} else if (StartOf(22)) {
			DisjunctionExpr(out expr);
		} else SynErr(267);
		if (expr != null) {
				expr.StartLocation = startLocation;
				expr.EndLocation = t.EndLocation;
			}

	}

	void ImplementsClause(out List<InterfaceImplementation> baseInterfaces) {
		baseInterfaces = new List<InterfaceImplementation>();
		TypeReference type = null;
		string memberName = null;

		Expect(136);
		NonArrayTypeName(out type, false);
		if (type != null) memberName = TypeReference.StripLastIdentifierFromType(ref type);
		baseInterfaces.Add(new InterfaceImplementation(type, memberName));
		while (la.kind == 22) {
			Get();
			NonArrayTypeName(out type, false);
			if (type != null) memberName = TypeReference.StripLastIdentifierFromType(ref type);
			baseInterfaces.Add(new InterfaceImplementation(type, memberName));
		}
	}

	void HandlesClause(out List<string> handlesClause) {
		handlesClause = new List<string>();
		string name;

		Expect(134);
		EventMemberSpecifier(out name);
		if (name != null) handlesClause.Add(name);
		while (la.kind == 22) {
			Get();
			EventMemberSpecifier(out name);
			if (name != null) handlesClause.Add(name);
		}
	}

	void Block(out Statement stmt) {
		BlockStatement blockStmt = new BlockStatement();
			/* in snippet parsing mode, t might be null */
			if (t != null) blockStmt.StartLocation = t.EndLocation;
			BlockStart(blockStmt);

		while (IsEndStmtAhead() || StartOf(StatementEndOfStmt)) {
			if (la.kind == 113) {
				Get();
				Token first = t;
					AddChild(new EndStatement() {
						StartLocation = first.Location,
						EndLocation = first.EndLocation }
					);

				EndOfStmt();
			} else if (StartOf(1)) {
				Statement();
				EndOfStmt();
			} else SynErr(268);
		}
		stmt = blockStmt;
			if (t != null) blockStmt.EndLocation = t.EndLocation;
			BlockEnd();

	}

	void Charset(out CharsetModifier charsetModifier) {
		charsetModifier = CharsetModifier.None;
		if (la.kind == 127 || la.kind == 210) {
		} else if (la.kind == 62) {
			Get();
			charsetModifier = CharsetModifier.Ansi;
		} else if (la.kind == 66) {
			Get();
			charsetModifier = CharsetModifier.Auto;
		} else if (la.kind == 223) {
			Get();
			charsetModifier = CharsetModifier.Unicode;
		} else SynErr(269);
	}

	void IdentifierForFieldDeclaration() {
		switch (la.kind) {
		case 2: {
			Get();
			break;
		}
		case 58: {
			Get();
			break;
		}
		case 62: {
			Get();
			break;
		}
		case 64: {
			Get();
			break;
		}
		case 65: {
			Get();
			break;
		}
		case 66: {
			Get();
			break;
		}
		case 67: {
			Get();
			break;
		}
		case 70: {
			Get();
			break;
		}
		case 87: {
			Get();
			break;
		}
		case 104: {
			Get();
			break;
		}
		case 107: {
			Get();
			break;
		}
		case 116: {
			Get();
			break;
		}
		case 121: {
			Get();
			break;
		}
		case 126: {
			Get();
			break;
		}
		case 133: {
			Get();
			break;
		}
		case 139: {
			Get();
			break;
		}
		case 143: {
			Get();
			break;
		}
		case 146: {
			Get();
			break;
		}
		case 147: {
			Get();
			break;
		}
		case 170: {
			Get();
			break;
		}
		case 176: {
			Get();
			break;
		}
		case 178: {
			Get();
			break;
		}
		case 184: {
			Get();
			break;
		}
		case 203: {
			Get();
			break;
		}
		case 212: {
			Get();
			break;
		}
		case 213: {
			Get();
			break;
		}
		case 223: {
			Get();
			break;
		}
		case 224: {
			Get();
			break;
		}
		case 230: {
			Get();
			break;
		}
		default: SynErr(270); break;
		}
	}

	void VariableDeclaratorPartAfterIdentifier(List<VariableDeclaration> fieldDeclaration, string name) {
		Expression expr = null;
		TypeReference type = null;
		ArrayList rank = null;
		List<Expression> dimension = null;
		Location startLocation = t.Location;

		if (IsSize() && !IsDims()) {
			ArrayInitializationModifier(out dimension);
		}
		if (IsDims()) {
			ArrayNameModifier(out rank);
		}
		if (IsObjectCreation()) {
			Expect(63);
			ObjectCreateExpression(out expr);
			if (expr is ObjectCreateExpression) {
					type = ((ObjectCreateExpression)expr).CreateType.Clone();
				} else {
					type = ((ArrayCreateExpression)expr).CreateType.Clone();
				}

		} else if (StartOf(23)) {
			if (la.kind == 63) {
				Get();
				TypeName(out type);
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
				Get();
				Expr(out expr);
			}
		} else SynErr(271);
		VariableDeclaration varDecl = new VariableDeclaration(name, expr, type);
			varDecl.StartLocation = startLocation;
			varDecl.EndLocation = t.Location;
			fieldDeclaration.Add(varDecl);

	}

	void VariableDeclarator(List<VariableDeclaration> fieldDeclaration) {
		Identifier();
		string name = t.val;
		VariableDeclaratorPartAfterIdentifier(fieldDeclaration, name);
	}

	void ConstantDeclarator(List<VariableDeclaration> constantDeclaration) {
		Expression expr = null;
		TypeReference type = null;
		string name = String.Empty;
		Location location;

		Identifier();
		name = t.val; location = t.Location;
		if (la.kind == 63) {
			Get();
			TypeName(out type);
		}
		Expect(20);
		Expr(out expr);
		VariableDeclaration f = new VariableDeclaration(name, expr);
			f.TypeReference = type;
			f.StartLocation = location;
			constantDeclaration.Add(f);

	}

	void ObjectCreateExpression(out Expression oce) {
		TypeReference type = null;
		CollectionInitializerExpression initializer = null;
		List<Expression> arguments = null;
		ArrayList dimensions = null;
		oce = null;
		Location startLocation = la.Location;
		bool canBeNormal; bool canBeReDim;

		Expect(162);
		if (StartOf(9)) {
			NonArrayTypeName(out type, false);
			if (la.kind == 37) {
				Get();
				NormalOrReDimArgumentList(out arguments, out canBeNormal, out canBeReDim);
				Expect(38);
				if (la.kind == 35 || (la.kind == Tokens.OpenParenthesis)) {
					if (la.kind == Tokens.OpenParenthesis) {
						ArrayTypeModifiers(out dimensions);
						CollectionInitializer(out initializer);
					} else {
						CollectionInitializer(out initializer);
					}
				}
				if (canBeReDim && !canBeNormal && initializer == null) initializer = new CollectionInitializerExpression();
			}
		}
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
				MemberInitializerExpression memberInitializer = null;
					Expression anonymousMember = null;

				Get();
				CollectionInitializerExpression memberInitializers = new CollectionInitializerExpression();
					memberInitializers.StartLocation = la.Location;

				Expect(35);
				if (la.kind == 26 || la.kind == 147) {
					MemberInitializer(out memberInitializer);
					memberInitializers.CreateExpressions.Add(memberInitializer);
				} else if (StartOf(24)) {
					Expr(out anonymousMember);
					memberInitializers.CreateExpressions.Add(anonymousMember);
				} else SynErr(272);
				while (la.kind == 22) {
					Get();
					if (la.kind == 26 || la.kind == 147) {
						MemberInitializer(out memberInitializer);
						memberInitializers.CreateExpressions.Add(memberInitializer);
					} else if (StartOf(24)) {
						Expr(out anonymousMember);
						memberInitializers.CreateExpressions.Add(anonymousMember);
					} else SynErr(273);
				}
				Expect(36);
				memberInitializers.EndLocation = t.Location;
					if(oce is ObjectCreateExpression)
					{
						((ObjectCreateExpression)oce).ObjectInitializer = memberInitializers;
					}

			} else {
				Get();
				CollectionInitializer(out initializer);
				if(oce is ObjectCreateExpression)
						((ObjectCreateExpression)oce).ObjectInitializer = initializer;

			}
		}
		if (oce != null) {
				oce.StartLocation = startLocation;
				oce.EndLocation = t.EndLocation;
			}

	}

	void AccessorDecls(out PropertyGetRegion getBlock, out PropertySetRegion setBlock) {
		List<AttributeSection> attributes = new List<AttributeSection>();
		AttributeSection section;
		getBlock = null;
		setBlock = null; 

		while (la.kind == 40) {
			AttributeSection(out section);
			attributes.Add(section);
		}
		if (StartOf(25)) {
			GetAccessorDecl(out getBlock, attributes);
			if (StartOf(26)) {
				attributes = new List<AttributeSection>();
				while (la.kind == 40) {
					AttributeSection(out section);
					attributes.Add(section);
				}
				SetAccessorDecl(out setBlock, attributes);
			}
		} else if (StartOf(27)) {
			SetAccessorDecl(out setBlock, attributes);
			if (StartOf(28)) {
				attributes = new List<AttributeSection>();
				while (la.kind == 40) {
					AttributeSection(out section);
					attributes.Add(section);
				}
				GetAccessorDecl(out getBlock, attributes);
			}
		} else SynErr(274);
	}

	void EventAccessorDeclaration(out EventAddRemoveRegion eventAccessorDeclaration) {
		Statement stmt = null;
		List<ParameterDeclarationExpression> p = new List<ParameterDeclarationExpression>();
		AttributeSection section;
		List<AttributeSection> attributes = new List<AttributeSection>();
		eventAccessorDeclaration = null;

		while (la.kind == 40) {
			AttributeSection(out section);
			attributes.Add(section);
		}
		if (la.kind == 56) {
			Get();
			if (la.kind == 37) {
				Get();
				if (StartOf(7)) {
					FormalParameterList(p);
				}
				Expect(38);
			}
			Expect(1);
			Block(out stmt);
			Expect(113);
			Expect(56);
			EndOfStmt();
			eventAccessorDeclaration = new EventAddRegion(attributes);
				eventAccessorDeclaration.Block = (BlockStatement)stmt;
				eventAccessorDeclaration.Parameters = p;

		} else if (la.kind == 193) {
			Get();
			if (la.kind == 37) {
				Get();
				if (StartOf(7)) {
					FormalParameterList(p);
				}
				Expect(38);
			}
			Expect(1);
			Block(out stmt);
			Expect(113);
			Expect(193);
			EndOfStmt();
			eventAccessorDeclaration = new EventRemoveRegion(attributes);
				eventAccessorDeclaration.Block = (BlockStatement)stmt;
				eventAccessorDeclaration.Parameters = p;

		} else if (la.kind == 189) {
			Get();
			if (la.kind == 37) {
				Get();
				if (StartOf(7)) {
					FormalParameterList(p);
				}
				Expect(38);
			}
			Expect(1);
			Block(out stmt);
			Expect(113);
			Expect(189);
			EndOfStmt();
			eventAccessorDeclaration = new EventRaiseRegion(attributes);
				eventAccessorDeclaration.Block = (BlockStatement)stmt;
				eventAccessorDeclaration.Parameters = p;

		} else SynErr(275);
	}

	void OverloadableOperator(out OverloadableOperatorType operatorType) {
		operatorType = OverloadableOperatorType.None;
		switch (la.kind) {
		case 31: {
			Get();
			operatorType = OverloadableOperatorType.Add;
			break;
		}
		case 30: {
			Get();
			operatorType = OverloadableOperatorType.Subtract;
			break;
		}
		case 34: {
			Get();
			operatorType = OverloadableOperatorType.Multiply;
			break;
		}
		case 24: {
			Get();
			operatorType = OverloadableOperatorType.Divide;
			break;
		}
		case 25: {
			Get();
			operatorType = OverloadableOperatorType.DivideInteger;
			break;
		}
		case 23: {
			Get();
			operatorType = OverloadableOperatorType.Concat;
			break;
		}
		case 150: {
			Get();
			operatorType = OverloadableOperatorType.Like;
			break;
		}
		case 154: {
			Get();
			operatorType = OverloadableOperatorType.Modulus;
			break;
		}
		case 60: {
			Get();
			operatorType = OverloadableOperatorType.BitwiseAnd;
			break;
		}
		case 175: {
			Get();
			operatorType = OverloadableOperatorType.BitwiseOr;
			break;
		}
		case 236: {
			Get();
			operatorType = OverloadableOperatorType.ExclusiveOr;
			break;
		}
		case 32: {
			Get();
			operatorType = OverloadableOperatorType.Power;
			break;
		}
		case 44: {
			Get();
			operatorType = OverloadableOperatorType.ShiftLeft;
			break;
		}
		case 45: {
			Get();
			operatorType = OverloadableOperatorType.ShiftRight;
			break;
		}
		case 20: {
			Get();
			operatorType = OverloadableOperatorType.Equality;
			break;
		}
		case 41: {
			Get();
			operatorType = OverloadableOperatorType.InEquality;
			break;
		}
		case 40: {
			Get();
			operatorType = OverloadableOperatorType.LessThan;
			break;
		}
		case 43: {
			Get();
			operatorType = OverloadableOperatorType.LessThanOrEqual;
			break;
		}
		case 39: {
			Get();
			operatorType = OverloadableOperatorType.GreaterThan;
			break;
		}
		case 42: {
			Get();
			operatorType = OverloadableOperatorType.GreaterThanOrEqual;
			break;
		}
		case 94: {
			Get();
			operatorType = OverloadableOperatorType.CType;
			break;
		}
		case 2: case 58: case 62: case 64: case 65: case 66: case 67: case 70: case 87: case 98: case 104: case 107: case 116: case 121: case 126: case 133: case 139: case 143: case 146: case 147: case 170: case 176: case 178: case 184: case 203: case 212: case 213: case 223: case 224: case 230: {
			Identifier();
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
		default: SynErr(276); break;
		}
	}

	void FormalParameter(out ParameterDeclarationExpression p) {
		AttributeSection section;
		List<AttributeSection> attributes = new List<AttributeSection>();
		TypeReference type = null;
		ParamModifierList mod = new ParamModifierList(this);
		Expression expr = null;
		p = null;
		ArrayList arrayModifiers = null;
		Location startLocation = la.Location;

		while (la.kind == 40) {
			AttributeSection(out section);
			attributes.Add(section);
		}
		while (StartOf(29)) {
			ParameterModifier(mod);
		}
		Identifier();
		string parameterName = t.val;
		if (IsDims()) {
			ArrayTypeModifiers(out arrayModifiers);
		}
		if (la.kind == 63) {
			Get();
			TypeName(out type);
		}
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
			Get();
			Expr(out expr);
		}
		mod.Check();
			p = new ParameterDeclarationExpression(type, parameterName, mod.Modifier, expr);
			p.Attributes = attributes;
			p.StartLocation = startLocation;
			p.EndLocation = t.EndLocation;

	}

	void GetAccessorDecl(out PropertyGetRegion getBlock, List<AttributeSection> attributes) {
		Statement stmt = null; Modifiers m;
		PropertyAccessorAccessModifier(out m);
		Expect(128);
		Location startLocation = t.Location;
		Expect(1);
		Block(out stmt);
		getBlock = new PropertyGetRegion((BlockStatement)stmt, attributes);
		Expect(113);
		Expect(128);
		getBlock.Modifier = m;
		getBlock.StartLocation = startLocation; getBlock.EndLocation = t.EndLocation;
		EndOfStmt();
	}

	void SetAccessorDecl(out PropertySetRegion setBlock, List<AttributeSection> attributes) {
		Statement stmt = null;
		List<ParameterDeclarationExpression> p = new List<ParameterDeclarationExpression>();
		Modifiers m;

		PropertyAccessorAccessModifier(out m);
		Expect(198);
		Location startLocation = t.Location;
		if (la.kind == 37) {
			Get();
			if (StartOf(7)) {
				FormalParameterList(p);
			}
			Expect(38);
		}
		Expect(1);
		Block(out stmt);
		setBlock = new PropertySetRegion((BlockStatement)stmt, attributes);
			setBlock.Modifier = m;
			setBlock.Parameters = p;

		Expect(113);
		Expect(198);
		setBlock.StartLocation = startLocation; setBlock.EndLocation = t.EndLocation;
		EndOfStmt();
	}

	void PropertyAccessorAccessModifier(out Modifiers m) {
		m = Modifiers.None;
		while (StartOf(30)) {
			if (la.kind == 188) {
				Get();
				m |= Modifiers.Public;
			} else if (la.kind == 187) {
				Get();
				m |= Modifiers.Protected;
			} else if (la.kind == 125) {
				Get();
				m |= Modifiers.Internal;
			} else {
				Get();
				m |= Modifiers.Private;
			}
		}
	}

	void ArrayInitializationModifier(out List<Expression> arrayModifiers) {
		arrayModifiers = null;

		Expect(37);
		InitializationRankList(out arrayModifiers);
		Expect(38);
	}

	void ArrayNameModifier(out ArrayList arrayModifiers) {
		arrayModifiers = null;

		ArrayTypeModifiers(out arrayModifiers);
	}

	void InitializationRankList(out List<Expression> rank) {
		rank = new List<Expression>();
		Expression expr = null;

		Expr(out expr);
		if (la.kind == 216) {
			Get();
			EnsureIsZero(expr);
			Expr(out expr);
		}
		if (expr != null) { rank.Add(expr); }
		while (la.kind == 22) {
			Get();
			Expr(out expr);
			if (la.kind == 216) {
				Get();
				EnsureIsZero(expr);
				Expr(out expr);
			}
			if (expr != null) { rank.Add(expr); }
		}
	}

	void CollectionInitializer(out CollectionInitializerExpression outExpr) {
		Expression expr = null;
		CollectionInitializerExpression initializer = new CollectionInitializerExpression();
		Location startLocation = la.Location;

		Expect(35);
		if (StartOf(24)) {
			Expr(out expr);
			if (expr != null) { initializer.CreateExpressions.Add(expr); }

			while (NotFinalComma()) {
				Expect(22);
				Expr(out expr);
				if (expr != null) { initializer.CreateExpressions.Add(expr); }
			}
		}
		Expect(36);
		outExpr = initializer;
			outExpr.StartLocation = startLocation;
			outExpr.EndLocation = t.EndLocation;

	}

	void EventMemberSpecifier(out string name) {
		string eventName;
		if (StartOf(5)) {
			Identifier();
		} else if (la.kind == 158) {
			Get();
		} else if (la.kind == 153) {
			Get();
		} else SynErr(277);
		name = t.val;
		Expect(26);
		IdentifierOrKeyword(out eventName);
		name = name + "." + eventName;
	}

	void IdentifierOrKeyword(out string name) {
		Get();
		name = t.val; 
	}

	void QueryExpr(out Expression expr) {
		QueryExpression qexpr = new QueryExpression();
		qexpr.StartLocation = la.Location;
		expr = qexpr;

		FromOrAggregateQueryOperator(qexpr.Clauses);
		while (StartOf(31)) {
			QueryOperator(qexpr.Clauses);
		}
		qexpr.EndLocation = t.EndLocation;

	}

	void LambdaExpr(out Expression expr) {
		LambdaExpression lambda = null;

		if (la.kind == 210) {
			SubLambdaExpression(out lambda);
		} else if (la.kind == 127) {
			FunctionLambdaExpression(out lambda);
		} else SynErr(278);
		expr = lambda;
	}

	void DisjunctionExpr(out Expression outExpr) {
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		Location startLocation = la.Location;

		ConjunctionExpr(out outExpr);
		while (la.kind == 175 || la.kind == 177 || la.kind == 236) {
			if (la.kind == 175) {
				Get();
				op = BinaryOperatorType.BitwiseOr;
			} else if (la.kind == 177) {
				Get();
				op = BinaryOperatorType.LogicalOr;
			} else {
				Get();
				op = BinaryOperatorType.ExclusiveOr;
			}
			ConjunctionExpr(out expr);
			outExpr = new BinaryOperatorExpression(outExpr, op, expr) { StartLocation = startLocation, EndLocation = t.EndLocation }; 
		}
	}

	void AssignmentOperator(out AssignmentOperatorType op) {
		op = AssignmentOperatorType.None;
		switch (la.kind) {
		case 20: {
			Get();
			op = AssignmentOperatorType.Assign;
			break;
		}
		case 54: {
			Get();
			op = AssignmentOperatorType.ConcatString;
			break;
		}
		case 46: {
			Get();
			op = AssignmentOperatorType.Add;
			break;
		}
		case 48: {
			Get();
			op = AssignmentOperatorType.Subtract;
			break;
		}
		case 49: {
			Get();
			op = AssignmentOperatorType.Multiply;
			break;
		}
		case 50: {
			Get();
			op = AssignmentOperatorType.Divide;
			break;
		}
		case 51: {
			Get();
			op = AssignmentOperatorType.DivideInteger;
			break;
		}
		case 47: {
			Get();
			op = AssignmentOperatorType.Power;
			break;
		}
		case 52: {
			Get();
			op = AssignmentOperatorType.ShiftLeft;
			break;
		}
		case 53: {
			Get();
			op = AssignmentOperatorType.ShiftRight;
			break;
		}
		default: SynErr(279); break;
		}
	}

	void SimpleExpr(out Expression pexpr) {
		string name; Location startLocation = la.Location;
		SimpleNonInvocationExpression(out pexpr);
		while (StartOf(32)) {
			if (la.kind == 26) {
				Get();
				if (la.kind == 10) {
					Get();
					IdentifierOrKeyword(out name);
					Expect(11);
					pexpr = new XmlMemberAccessExpression(pexpr, XmlAxisType.Element, name, true);
				} else if (StartOf(33)) {
					IdentifierOrKeyword(out name);
					pexpr = new MemberReferenceExpression(pexpr, name) { StartLocation = startLocation, EndLocation = t.EndLocation };
				} else SynErr(280);
				if (la.kind == Tokens.OpenParenthesis && Peek(1).kind == Tokens.Of) {
					Expect(37);
					Expect(169);
					TypeArgumentList(((MemberReferenceExpression)pexpr).TypeArguments);
					Expect(38);
				}
			} else if (la.kind == 29) {
				Get();
				IdentifierOrKeyword(out name);
				pexpr = new BinaryOperatorExpression(pexpr, BinaryOperatorType.DictionaryAccess, new PrimitiveExpression(name, name) { StartLocation = t.Location, EndLocation = t.EndLocation });
			} else if (la.kind == 27 || la.kind == 28) {
				XmlAxisType type = XmlAxisType.Attribute; bool isXmlName = false;
				if (la.kind == 28) {
					Get();
				} else {
					Get();
					type = XmlAxisType.Descendents;
				}
				if (la.kind == 10) {
					Get();
					isXmlName = true;
				}
				IdentifierOrKeyword(out name);
				if (la.kind == 11) {
					Get();
				}
				pexpr = new XmlMemberAccessExpression(pexpr, type, name, isXmlName);
			} else {
				InvocationExpression(ref pexpr);
			}
		}
		if (pexpr != null) {
				pexpr.StartLocation = startLocation;
				pexpr.EndLocation = t.EndLocation;
			}

	}

	void SimpleNonInvocationExpression(out Expression pexpr) {
		Expression expr;
		CollectionInitializerExpression cie;
		TypeReference type = null;
		string name = String.Empty;
		Location startLocation = la.Location;
		pexpr = null;

		if (StartOf(34)) {
			switch (la.kind) {
			case 3: {
				Get();
				pexpr = new PrimitiveExpression(t.literalValue, t.val) { LiteralFormat = t.literalFormat }; 
				break;
			}
			case 4: {
				Get();
				pexpr = new PrimitiveExpression(t.literalValue, t.val) { LiteralFormat = t.literalFormat }; 
				break;
			}
			case 7: {
				Get();
				pexpr = new PrimitiveExpression(t.literalValue, t.val) { LiteralFormat = t.literalFormat }; 
				break;
			}
			case 6: {
				Get();
				pexpr = new PrimitiveExpression(t.literalValue, t.val) { LiteralFormat = t.literalFormat }; 
				break;
			}
			case 5: {
				Get();
				pexpr = new PrimitiveExpression(t.literalValue, t.val) { LiteralFormat = t.literalFormat }; 
				break;
			}
			case 9: {
				Get();
				pexpr = new PrimitiveExpression(t.literalValue, t.val) { LiteralFormat = t.literalFormat }; 
				break;
			}
			case 8: {
				Get();
				pexpr = new PrimitiveExpression(t.literalValue, t.val) { LiteralFormat = t.literalFormat }; 
				break;
			}
			case 217: {
				Get();
				pexpr = new PrimitiveExpression(true, "true"); 
				break;
			}
			case 122: {
				Get();
				pexpr = new PrimitiveExpression(false, "false");
				break;
			}
			case 165: {
				Get();
				pexpr = new PrimitiveExpression(null, "null"); 
				break;
			}
			case 37: {
				Get();
				Expr(out expr);
				Expect(38);
				pexpr = new ParenthesizedExpression(expr);
				break;
			}
			case 2: case 58: case 62: case 64: case 65: case 66: case 67: case 70: case 87: case 98: case 104: case 107: case 116: case 121: case 126: case 133: case 139: case 143: case 146: case 147: case 170: case 176: case 178: case 184: case 203: case 212: case 213: case 223: case 224: case 230: {
				Identifier();
				pexpr = new IdentifierExpression(t.val);
					pexpr.StartLocation = t.Location; pexpr.EndLocation = t.EndLocation;

				if (la.kind == Tokens.OpenParenthesis && Peek(1).kind == Tokens.Of) {
					Expect(37);
					Expect(169);
					TypeArgumentList(((IdentifierExpression)pexpr).TypeArguments);
					Expect(38);
				}
				break;
			}
			case 68: case 71: case 82: case 99: case 100: case 109: case 141: case 151: case 168: case 196: case 201: case 202: case 208: case 221: case 222: case 225: {
				string val = String.Empty;
				if (StartOf(13)) {
					PrimitiveTypeName(out val);
				} else {
					Get();
					val = "System.Object";
				}
				pexpr = new TypeReferenceExpression(new TypeReference(val, true));
				break;
			}
			case 153: {
				Get();
				pexpr = new ThisReferenceExpression();
				break;
			}
			case 158: case 159: {
				Expression retExpr = null;
				if (la.kind == 158) {
					Get();
					retExpr = new BaseReferenceExpression() { StartLocation = t.Location, EndLocation = t.EndLocation };
				} else {
					Get();
					retExpr = new ClassReferenceExpression() { StartLocation = t.Location, EndLocation = t.EndLocation };
				}
				Expect(26);
				IdentifierOrKeyword(out name);
				pexpr = new MemberReferenceExpression(retExpr, name) { StartLocation = startLocation, EndLocation = t.EndLocation };
				break;
			}
			case 130: {
				Get();
				Expect(26);
				Identifier();
				type = new TypeReference(t.val ?? "");
				type.IsGlobal = true;
				pexpr = new TypeReferenceExpression(type);
				break;
			}
			case 162: {
				ObjectCreateExpression(out expr);
				pexpr = expr;
				break;
			}
			case 35: {
				CollectionInitializer(out cie);
				pexpr = cie;
				break;
			}
			case 94: case 106: case 219: {
				CastType castType = CastType.Cast;
				if (la.kind == 106) {
					Get();
				} else if (la.kind == 94) {
					Get();
					castType = CastType.Conversion;
				} else {
					Get();
					castType = CastType.TryCast;
				}
				Expect(37);
				Expr(out expr);
				Expect(22);
				TypeName(out type);
				Expect(38);
				pexpr = new CastExpression(type, expr, castType);
				break;
			}
			case 76: case 77: case 78: case 79: case 80: case 81: case 83: case 85: case 86: case 90: case 91: case 92: case 93: case 95: case 96: case 97: {
				CastTarget(out type);
				Expect(37);
				Expr(out expr);
				Expect(38);
				pexpr = new CastExpression(type, expr, CastType.PrimitiveConversion);
				break;
			}
			case 57: {
				Get();
				SimpleExpr(out expr);
				pexpr = new AddressOfExpression(expr);
				break;
			}
			case 129: {
				Get();
				Expect(37);
				GetTypeTypeName(out type);
				Expect(38);
				pexpr = new TypeOfExpression(type);
				break;
			}
			case 220: {
				Get();
				SimpleExpr(out expr);
				Expect(144);
				TypeName(out type);
				pexpr = new TypeOfIsExpression(expr, type);
				break;
			}
			case 135: {
				ConditionalExpression(out pexpr);
				break;
			}
			case 10: case 16: case 17: case 18: case 19: {
				XmlLiteralExpression(out pexpr);
				break;
			}
			}
		} else if (StartOf(35)) {
			if (la.kind == 26) {
				Get();
				if (la.kind == 10) {
					Get();
					IdentifierOrKeyword(out name);
					Expect(11);
					pexpr = new XmlMemberAccessExpression(null, XmlAxisType.Element, name, true) { StartLocation = startLocation, EndLocation = t.EndLocation };
				} else if (StartOf(33)) {
					IdentifierOrKeyword(out name);
					pexpr = new MemberReferenceExpression(null, name) { StartLocation = startLocation, EndLocation = t.EndLocation };
				} else SynErr(281);
			} else if (la.kind == 29) {
				Get();
				IdentifierOrKeyword(out name);
				pexpr = new BinaryOperatorExpression(null, BinaryOperatorType.DictionaryAccess, new PrimitiveExpression(name, name) { StartLocation = t.Location, EndLocation = t.EndLocation });
			} else {
				XmlAxisType axisType = XmlAxisType.Element; bool isXmlIdentifier = false;
				if (la.kind == 27) {
					Get();
					axisType = XmlAxisType.Descendents;
				} else {
					Get();
					axisType = XmlAxisType.Attribute;
				}
				if (la.kind == 10) {
					Get();
					isXmlIdentifier = true;
				}
				IdentifierOrKeyword(out name);
				if (la.kind == 11) {
					Get();
				}
				pexpr = new XmlMemberAccessExpression(null, axisType, name, isXmlIdentifier);
			}
		} else SynErr(282);
		if (pexpr != null) {
				pexpr.StartLocation = startLocation;
				pexpr.EndLocation = t.EndLocation;
			}

	}

	void TypeArgumentList(List<TypeReference> typeArguments) {
		TypeReference typeref;

		TypeName(out typeref);
		if (typeref != null) typeArguments.Add(typeref);
		while (la.kind == 22) {
			Get();
			TypeName(out typeref);
			if (typeref != null) typeArguments.Add(typeref);
		}
	}

	void InvocationExpression(ref Expression pexpr) {
		List<Expression> parameters = null;
		Expect(37);
		Location start = t.Location;
		ArgumentList(out parameters);
		Expect(38);
		pexpr = new InvocationExpression(pexpr, parameters);

		pexpr.StartLocation = start; pexpr.EndLocation = t.Location;
	}

	void PrimitiveTypeName(out string type) {
		type = String.Empty;
		switch (la.kind) {
		case 68: {
			Get();
			type = "System.Boolean";
			break;
		}
		case 99: {
			Get();
			type = "System.DateTime";
			break;
		}
		case 82: {
			Get();
			type = "System.Char";
			break;
		}
		case 208: {
			Get();
			type = "System.String";
			break;
		}
		case 100: {
			Get();
			type = "System.Decimal";
			break;
		}
		case 71: {
			Get();
			type = "System.Byte";
			break;
		}
		case 201: {
			Get();
			type = "System.Int16";
			break;
		}
		case 141: {
			Get();
			type = "System.Int32";
			break;
		}
		case 151: {
			Get();
			type = "System.Int64";
			break;
		}
		case 202: {
			Get();
			type = "System.Single";
			break;
		}
		case 109: {
			Get();
			type = "System.Double";
			break;
		}
		case 221: {
			Get();
			type = "System.UInt32";
			break;
		}
		case 222: {
			Get();
			type = "System.UInt64";
			break;
		}
		case 225: {
			Get();
			type = "System.UInt16";
			break;
		}
		case 196: {
			Get();
			type = "System.SByte";
			break;
		}
		default: SynErr(283); break;
		}
	}

	void CastTarget(out TypeReference type) {
		type = null;

		switch (la.kind) {
		case 76: {
			Get();
			type = new TypeReference("System.Boolean", true);
			break;
		}
		case 77: {
			Get();
			type = new TypeReference("System.Byte", true);
			break;
		}
		case 90: {
			Get();
			type = new TypeReference("System.SByte", true);
			break;
		}
		case 78: {
			Get();
			type = new TypeReference("System.Char", true);
			break;
		}
		case 79: {
			Get();
			type = new TypeReference("System.DateTime", true);
			break;
		}
		case 81: {
			Get();
			type = new TypeReference("System.Decimal", true);
			break;
		}
		case 80: {
			Get();
			type = new TypeReference("System.Double", true);
			break;
		}
		case 91: {
			Get();
			type = new TypeReference("System.Int16", true);
			break;
		}
		case 83: {
			Get();
			type = new TypeReference("System.Int32", true);
			break;
		}
		case 85: {
			Get();
			type = new TypeReference("System.Int64", true);
			break;
		}
		case 97: {
			Get();
			type = new TypeReference("System.UInt16", true);
			break;
		}
		case 95: {
			Get();
			type = new TypeReference("System.UInt32", true);
			break;
		}
		case 96: {
			Get();
			type = new TypeReference("System.UInt64", true);
			break;
		}
		case 86: {
			Get();
			type = new TypeReference("System.Object", true);
			break;
		}
		case 92: {
			Get();
			type = new TypeReference("System.Single", true);
			break;
		}
		case 93: {
			Get();
			type = new TypeReference("System.String", true);
			break;
		}
		default: SynErr(284); break;
		}
	}

	void GetTypeTypeName(out TypeReference typeref) {
		ArrayList rank = null;
		NonArrayTypeName(out typeref, true);
		ArrayTypeModifiers(out rank);
		if (rank != null && typeref != null) {
				typeref.RankSpecifier = (int[])rank.ToArray(typeof(int));
			}

	}

	void ConditionalExpression(out Expression expr) {
		ConditionalExpression conditionalExpression = new ConditionalExpression();
		BinaryOperatorExpression binaryOperatorExpression = new BinaryOperatorExpression();
		conditionalExpression.StartLocation = binaryOperatorExpression.StartLocation = la.Location;

		Expression condition = null;
		Expression trueExpr = null;
		Expression falseExpr = null;

		Expect(135);
		Expect(37);
		Expr(out condition);
		Expect(22);
		Expr(out trueExpr);
		if (la.kind == 22) {
			Get();
			Expr(out falseExpr);
		}
		Expect(38);
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

	void XmlLiteralExpression(out Expression pexpr) {
		List<XmlExpression> exprs = new List<XmlExpression>();
		XmlExpression currentExpression = null;

		if (StartOf(36)) {
			XmlContentExpression(exprs);
			while (StartOf(36)) {
				XmlContentExpression(exprs);
			}
			if (la.kind == 10) {
				XmlElement(out currentExpression);
				exprs.Add(currentExpression);
				while (StartOf(36)) {
					XmlContentExpression(exprs);
				}
			}
		} else if (la.kind == 10) {
			XmlElement(out currentExpression);
			exprs.Add(currentExpression);
			while (StartOf(36)) {
				XmlContentExpression(exprs);
			}
		} else SynErr(285);
		if (exprs.Count > 1) {
				pexpr = new XmlDocumentExpression() { Expressions = exprs };
			} else {
				pexpr = exprs[0];
			}

	}

	void XmlContentExpression(List<XmlExpression> exprs) {
		XmlContentExpression expr = null;
		if (la.kind == 16) {
			Get();
			expr = new XmlContentExpression(t.val, XmlContentType.Text);
		} else if (la.kind == 18) {
			Get();
			expr = new XmlContentExpression(t.val, XmlContentType.CData);
		} else if (la.kind == 17) {
			Get();
			expr = new XmlContentExpression(t.val, XmlContentType.Comment);
		} else if (la.kind == 19) {
			Get();
			expr = new XmlContentExpression(t.val, XmlContentType.ProcessingInstruction);
		} else SynErr(286);
		expr.StartLocation = t.Location;
			expr.EndLocation = t.EndLocation;
			exprs.Add(expr);

	}

	void XmlElement(out XmlExpression expr) {
		XmlElementExpression el = new XmlElementExpression();
		Expect(10);
		el.StartLocation = t.Location;
		if (la.kind == 12) {
			Get();
			Expression innerExpression;
			Expr(out innerExpression);
			Expect(13);
			el.NameExpression = new XmlEmbeddedExpression() { InlineVBExpression = innerExpression };
		} else if (StartOf(5)) {
			Identifier();
			el.XmlName = t.val;
		} else SynErr(287);
		while (StartOf(37)) {
			XmlAttribute(el.Attributes);
		}
		if (la.kind == 14) {
			Get();
			el.EndLocation = t.EndLocation;
		} else if (la.kind == 11) {
			Get();
			while (StartOf(38)) {
				XmlExpression child;
				XmlNestedContent(out child);
				el.Children.Add(child);
			}
			Expect(15);
			while (StartOf(39)) {
				Get();
			}
			Expect(11);
			el.EndLocation = t.EndLocation;
		} else SynErr(288);
		expr = el;
	}

	void XmlNestedContent(out XmlExpression expr) {
		XmlExpression tmpExpr = null; Location start = la.Location;
		switch (la.kind) {
		case 16: {
			Get();
			tmpExpr = new XmlContentExpression(t.val, XmlContentType.Text);
			break;
		}
		case 18: {
			Get();
			tmpExpr = new XmlContentExpression(t.val, XmlContentType.CData);
			break;
		}
		case 17: {
			Get();
			tmpExpr = new XmlContentExpression(t.val, XmlContentType.Comment);
			break;
		}
		case 19: {
			Get();
			tmpExpr = new XmlContentExpression(t.val, XmlContentType.ProcessingInstruction);
			break;
		}
		case 12: {
			Get();
			Expression innerExpression;
			Expr(out innerExpression);
			Expect(13);
			tmpExpr = new XmlEmbeddedExpression() { InlineVBExpression = innerExpression };
			break;
		}
		case 10: {
			XmlElement(out tmpExpr);
			break;
		}
		default: SynErr(289); break;
		}
		if (tmpExpr.StartLocation.IsEmpty)
				tmpExpr.StartLocation = start;
			if (tmpExpr.EndLocation.IsEmpty)
				tmpExpr.EndLocation = t.EndLocation;
			expr = tmpExpr;

	}

	void XmlAttribute(List<XmlExpression> attrs) {
		Location start = la.Location;
		if (StartOf(5)) {
			Identifier();
			string name = t.val;
			Expect(20);
			string literalValue = null; Expression expressionValue = null; bool useDoubleQuotes = false;
			if (la.kind == 3) {
				Get();
				literalValue = t.literalValue.ToString(); useDoubleQuotes = t.val[0] == '"';
			} else if (la.kind == 12) {
				Get();
				Expr(out expressionValue);
				Expect(13);
			} else SynErr(290);
			attrs.Add(new XmlAttributeExpression() { Name = name, ExpressionValue = expressionValue, LiteralValue = literalValue, UseDoubleQuotes = useDoubleQuotes, StartLocation = start, EndLocation = t.EndLocation });
		} else if (la.kind == 12) {
			Get();
			Expression innerExpression;
			Expr(out innerExpression);
			Expect(13);
			attrs.Add(new XmlEmbeddedExpression() { InlineVBExpression = innerExpression, StartLocation = start, EndLocation = t.EndLocation });
		} else SynErr(291);
	}

	void ArgumentList(out List<Expression> arguments) {
		arguments = new List<Expression>();
		Expression expr = null;

		if (StartOf(24)) {
			Argument(out expr);
		}
		while (la.kind == 22) {
			Get();
			arguments.Add(expr ?? Expression.Null); expr = null;
			if (StartOf(24)) {
				Argument(out expr);
			}
			if (expr == null) expr = Expression.Null;
		}
		if (expr != null) arguments.Add(expr);
	}

	void ConjunctionExpr(out Expression outExpr) {
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		Location startLocation = la.Location;

		NotExpr(out outExpr);
		while (la.kind == 60 || la.kind == 61) {
			if (la.kind == 60) {
				Get();
				op = BinaryOperatorType.BitwiseAnd;
			} else {
				Get();
				op = BinaryOperatorType.LogicalAnd;
			}
			NotExpr(out expr);
			outExpr = new BinaryOperatorExpression(outExpr, op, expr) { StartLocation = startLocation, EndLocation = t.EndLocation }; 
		}
	}

	void NotExpr(out Expression outExpr) {
		UnaryOperatorType uop = UnaryOperatorType.None;
		while (la.kind == 164) {
			Get();
			uop = UnaryOperatorType.Not;
		}
		ComparisonExpr(out outExpr);
		if (uop != UnaryOperatorType.None)
		    outExpr = new UnaryOperatorExpression(outExpr, uop);

	}

	void ComparisonExpr(out Expression outExpr) {
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		Location startLocation = la.Location;

		ShiftExpr(out outExpr);
		while (StartOf(40)) {
			switch (la.kind) {
			case 40: {
				Get();
				op = BinaryOperatorType.LessThan;
				break;
			}
			case 39: {
				Get();
				op = BinaryOperatorType.GreaterThan;
				break;
			}
			case 43: {
				Get();
				op = BinaryOperatorType.LessThanOrEqual;
				break;
			}
			case 42: {
				Get();
				op = BinaryOperatorType.GreaterThanOrEqual;
				break;
			}
			case 41: {
				Get();
				op = BinaryOperatorType.InEquality;
				break;
			}
			case 20: {
				Get();
				op = BinaryOperatorType.Equality;
				break;
			}
			case 150: {
				Get();
				op = BinaryOperatorType.Like;
				break;
			}
			case 144: {
				Get();
				op = BinaryOperatorType.ReferenceEquality;
				break;
			}
			case 145: {
				Get();
				op = BinaryOperatorType.ReferenceInequality;
				break;
			}
			}
			if (StartOf(41)) {
				ShiftExpr(out expr);
				outExpr = new BinaryOperatorExpression(outExpr, op, expr) { StartLocation = startLocation, EndLocation = t.EndLocation }; 
			} else if (la.kind == 164) {
				Location startLocation2 = la.Location;
				Get();
				ShiftExpr(out expr);
				outExpr = new BinaryOperatorExpression(outExpr, op, new UnaryOperatorExpression(expr, UnaryOperatorType.Not) { StartLocation = startLocation2, EndLocation = t.EndLocation }) { StartLocation = startLocation, EndLocation = t.EndLocation }; 
			} else SynErr(292);
		}
	}

	void ShiftExpr(out Expression outExpr) {
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		Location startLocation = la.Location;

		ConcatenationExpr(out outExpr);
		while (la.kind == 44 || la.kind == 45) {
			if (la.kind == 44) {
				Get();
				op = BinaryOperatorType.ShiftLeft;
			} else {
				Get();
				op = BinaryOperatorType.ShiftRight;
			}
			ConcatenationExpr(out expr);
			outExpr = new BinaryOperatorExpression(outExpr, op, expr) { StartLocation = startLocation, EndLocation = t.EndLocation }; 
		}
	}

	void ConcatenationExpr(out Expression outExpr) {
		Expression expr; Location startLocation = la.Location;
		AdditiveExpr(out outExpr);
		while (la.kind == 23) {
			Get();
			AdditiveExpr(out expr);
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.Concat, expr) { StartLocation = startLocation, EndLocation = t.EndLocation }; 
		}
	}

	void AdditiveExpr(out Expression outExpr) {
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		Location startLocation = la.Location;

		ModuloExpr(out outExpr);
		while (la.kind == 30 || la.kind == 31) {
			if (la.kind == 31) {
				Get();
				op = BinaryOperatorType.Add;
			} else {
				Get();
				op = BinaryOperatorType.Subtract;
			}
			ModuloExpr(out expr);
			outExpr = new BinaryOperatorExpression(outExpr, op, expr) { StartLocation = startLocation, EndLocation = t.EndLocation }; 
		}
	}

	void ModuloExpr(out Expression outExpr) {
		Expression expr; Location startLocation = la.Location;
		IntegerDivisionExpr(out outExpr);
		while (la.kind == 154) {
			Get();
			IntegerDivisionExpr(out expr);
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.Modulus, expr) { StartLocation = startLocation, EndLocation = t.EndLocation }; 
		}
	}

	void IntegerDivisionExpr(out Expression outExpr) {
		Expression expr; Location startLocation = la.Location;
		MultiplicativeExpr(out outExpr);
		while (la.kind == 25) {
			Get();
			MultiplicativeExpr(out expr);
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.DivideInteger, expr) { StartLocation = startLocation, EndLocation = t.EndLocation }; 
		}
	}

	void MultiplicativeExpr(out Expression outExpr) {
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		Location startLocation = la.Location;

		UnaryExpr(out outExpr);
		while (la.kind == 24 || la.kind == 34) {
			if (la.kind == 34) {
				Get();
				op = BinaryOperatorType.Multiply;
			} else {
				Get();
				op = BinaryOperatorType.Divide;
			}
			UnaryExpr(out expr);
			outExpr = new BinaryOperatorExpression(outExpr, op, expr) { StartLocation = startLocation, EndLocation = t.EndLocation };
		}
	}

	void UnaryExpr(out Expression uExpr) {
		Expression expr;
		UnaryOperatorType uop = UnaryOperatorType.None;
		Location startLocation = la.Location;
		bool isUOp = false;

		while (la.kind == 30 || la.kind == 31 || la.kind == 34) {
			if (la.kind == 31) {
				Get();
				uop = UnaryOperatorType.Plus; isUOp = true;
			} else if (la.kind == 30) {
				Get();
				uop = UnaryOperatorType.Minus; isUOp = true;
			} else {
				Get();
				uop = UnaryOperatorType.Dereference;  isUOp = true;
			}
		}
		ExponentiationExpr(out expr);
		if (isUOp) {
				uExpr = new UnaryOperatorExpression(expr, uop) { StartLocation = startLocation, EndLocation = t.EndLocation };
			} else {
				uExpr = expr;
			}

	}

	void ExponentiationExpr(out Expression outExpr) {
		Expression expr; Location startLocation = la.Location;
		SimpleExpr(out outExpr);
		while (la.kind == 32) {
			Get();
			SimpleExpr(out expr);
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.Power, expr) { StartLocation = startLocation, EndLocation = t.EndLocation }; 
		}
	}

	void NormalOrReDimArgumentList(out List<Expression> arguments, out bool canBeNormal, out bool canBeRedim) {
		arguments = new List<Expression>();
		canBeNormal = true; canBeRedim = !IsNamedAssign();
		Expression expr = null;

		if (StartOf(24)) {
			Argument(out expr);
			if (la.kind == 216) {
				Get();
				EnsureIsZero(expr); canBeNormal = false;
				Expr(out expr);
			}
		}
		while (la.kind == 22) {
			Get();
			if (expr == null) canBeRedim = false;
			arguments.Add(expr ?? Expression.Null); expr = null;
			canBeRedim &= !IsNamedAssign();
			if (StartOf(24)) {
				Argument(out expr);
				if (la.kind == 216) {
					Get();
					EnsureIsZero(expr); canBeNormal = false;
					Expr(out expr);
				}
			}
			if (expr == null) { canBeRedim = false; expr = Expression.Null; }
		}
		if (expr != null) arguments.Add(expr); else canBeRedim = false;
	}

	void ArrayTypeModifiers(out ArrayList arrayModifiers) {
		arrayModifiers = new ArrayList();
		int i = 0;

		while (IsDims()) {
			Expect(37);
			if (la.kind == 22 || la.kind == 38) {
				RankList(out i);
			}
			arrayModifiers.Add(i);

			Expect(38);
		}
		if(arrayModifiers.Count == 0) {
				 arrayModifiers = null;
			}

	}

	void MemberInitializer(out MemberInitializerExpression memberInitializer) {
		memberInitializer = new MemberInitializerExpression();
		memberInitializer.StartLocation = la.Location;
		Expression initExpr = null;
		bool isKey = false;
		string name = null;

		if (la.kind == 147) {
			Get();
			isKey = true;
		}
		Expect(26);
		IdentifierOrKeyword(out name);
		Expect(20);
		Expr(out initExpr);
		memberInitializer.Name = name;
			memberInitializer.Expression = initExpr;
			memberInitializer.IsKey = isKey;
			memberInitializer.EndLocation = t.EndLocation;

	}

	void SubLambdaExpression(out LambdaExpression lambda) {
		lambda = new LambdaExpression();
		lambda.ReturnType = new TypeReference("System.Void", true);
		Statement statement = null;
		lambda.StartLocation = la.Location;

		Expect(210);
		Expect(37);
		if (StartOf(7)) {
			FormalParameterList(lambda.Parameters);
		}
		Expect(38);
		if (StartOf(1)) {
			EmbeddedStatement(out statement);
			lambda.StatementBody = statement;
				lambda.EndLocation = t.EndLocation;
				lambda.ExtendedEndLocation = la.Location;

		} else if (la.kind == 1) {
			Get();
			Block(out statement);
			Expect(113);
			Expect(210);
			lambda.StatementBody = statement;
				lambda.EndLocation = t.EndLocation;
				lambda.ExtendedEndLocation = la.Location;

		} else SynErr(293);
	}

	void FunctionLambdaExpression(out LambdaExpression lambda) {
		lambda = new LambdaExpression();
		TypeReference typeRef = null;
		Expression inner = null;
		Statement statement = null;
		lambda.StartLocation = la.Location;

		Expect(127);
		Expect(37);
		if (StartOf(7)) {
			FormalParameterList(lambda.Parameters);
		}
		Expect(38);
		if (la.kind == 63) {
			Get();
			TypeName(out typeRef);
			lambda.ReturnType = typeRef;
		}
		if (StartOf(24)) {
			Expr(out inner);
			lambda.ExpressionBody = inner;
				lambda.EndLocation = t.EndLocation;
				lambda.ExtendedEndLocation = la.Location;

		} else if (la.kind == 1) {
			Get();
			Block(out statement);
			Expect(113);
			Expect(127);
			lambda.StatementBody = statement;
				lambda.EndLocation = t.EndLocation;
				lambda.ExtendedEndLocation = la.Location;

		} else SynErr(294);
	}

	void EmbeddedStatement(out Statement statement) {
		statement = null;
		string name = String.Empty;
		Location startLocation = la.Location;

		if (la.kind == 120) {
			ExitStatement(out statement);
		} else if (la.kind == 218) {
			TryStatement(out statement);
		} else if (la.kind == 89) {
			ContinueStatement(out statement);
		} else if (la.kind == 215) {
			ThrowStatement(out statement);
		} else if (la.kind == 195) {
			ReturnStatement(out statement);
		} else if (la.kind == 211) {
			SyncLockStatement(out statement);
		} else if (la.kind == 189) {
			RaiseEventStatement(out statement);
		} else if (la.kind == 233) {
			WithStatement(out statement);
		} else if (la.kind == 56) {
			AddHandlerStatement(out statement);
		} else if (la.kind == 193) {
			RemoveHandlerStatement(out statement);
		} else if (la.kind == 231) {
			WhileStatement(out statement);
		} else if (la.kind == 108) {
			DoLoopStatement(out statement);
		} else if (la.kind == 124) {
			ForStatement(out statement);
		} else if (la.kind == 118) {
			ErrorStatement(out statement);
		} else if (la.kind == 191) {
			ReDimStatement(out statement);
		} else if (la.kind == 117) {
			EraseStatement(out statement);
		} else if (la.kind == 206) {
			StopStatement(out statement);
		} else if (la.kind == 135) {
			IfStatement(out statement);
		} else if (la.kind == 197) {
			SelectStatement(out statement);
		} else if (la.kind == 171) {
			OnErrorStatement onErrorStatement = null;
			OnErrorStatement(out onErrorStatement);
			statement = onErrorStatement;
		} else if (la.kind == 132) {
			GotoStatement goToStatement = null;
			GotoStatement(out goToStatement);
			statement = goToStatement;
		} else if (la.kind == 194) {
			ResumeStatement(out statement);
		} else if (StartOf(42)) {
			ExpressionStatement(out statement);
		} else if (la.kind == 73) {
			InvocationStatement(out statement);
		} else if (la.kind == 226) {
			UsingStatement(out statement);
		} else if (StartOf(43)) {
			LocalDeclarationStatement(out statement);
		} else SynErr(295);
		if (statement != null) {
				statement.StartLocation = startLocation;
				statement.EndLocation = t.EndLocation;
			}

	}

	void FromOrAggregateQueryOperator(List<QueryExpressionClause> middleClauses) {
		QueryExpressionFromClause fromClause = null;
		QueryExpressionAggregateClause aggregateClause = null;

		if (la.kind == 126) {
			FromQueryOperator(out fromClause);
			middleClauses.Add(fromClause);
		} else if (la.kind == 58) {
			AggregateQueryOperator(out aggregateClause);
			middleClauses.Add(aggregateClause);
		} else SynErr(296);
	}

	void QueryOperator(List<QueryExpressionClause> middleClauses) {
		QueryExpressionJoinVBClause joinClause = null;
		QueryExpressionGroupVBClause groupByClause = null;
		QueryExpressionPartitionVBClause partitionClause = null;
		QueryExpressionGroupJoinVBClause groupJoinClause = null;
		QueryExpressionFromClause fromClause = null;
		QueryExpressionAggregateClause aggregateClause = null;

		if (la.kind == 126) {
			FromQueryOperator(out fromClause);
			middleClauses.Add(fromClause);
		} else if (la.kind == 58) {
			AggregateQueryOperator(out aggregateClause);
			middleClauses.Add(aggregateClause);
		} else if (la.kind == 197) {
			SelectQueryOperator(middleClauses);
		} else if (la.kind == 107) {
			DistinctQueryOperator(middleClauses);
		} else if (la.kind == 230) {
			WhereQueryOperator(middleClauses);
		} else if (la.kind == 176) {
			OrderByQueryOperator(middleClauses);
		} else if (la.kind == 203 || la.kind == 212) {
			PartitionQueryOperator(out partitionClause);
			middleClauses.Add(partitionClause);
		} else if (la.kind == 148) {
			LetQueryOperator(middleClauses);
		} else if (la.kind == 146) {
			JoinQueryOperator(out joinClause);
			middleClauses.Add(joinClause);
		} else if (la.kind == Tokens.Group && Peek(1).kind == Tokens.Join) {
			GroupJoinQueryOperator(out groupJoinClause);
			middleClauses.Add(groupJoinClause);
		} else if (la.kind == 133) {
			GroupByQueryOperator(out groupByClause);
			middleClauses.Add(groupByClause);
		} else SynErr(297);
	}

	void FromQueryOperator(out QueryExpressionFromClause fromClause) {
		fromClause = new QueryExpressionFromClause();
		fromClause.StartLocation = la.Location;

		Expect(126);
		CollectionRangeVariableDeclarationList(fromClause.Sources);
		fromClause.EndLocation = t.EndLocation;

	}

	void AggregateQueryOperator(out QueryExpressionAggregateClause aggregateClause) {
		aggregateClause = new QueryExpressionAggregateClause();
		aggregateClause.IntoVariables = new List<ExpressionRangeVariable>();
		aggregateClause.StartLocation = la.Location;
		CollectionRangeVariable source;

		Expect(58);
		CollectionRangeVariableDeclaration(out source);
		aggregateClause.Source = source;

		while (StartOf(31)) {
			QueryOperator(aggregateClause.MiddleClauses);
		}
		Expect(143);
		ExpressionRangeVariableDeclarationList(aggregateClause.IntoVariables);
		aggregateClause.EndLocation = t.EndLocation;

	}

	void SelectQueryOperator(List<QueryExpressionClause> middleClauses) {
		QueryExpressionSelectVBClause selectClause = new QueryExpressionSelectVBClause();
		selectClause.StartLocation = la.Location;

		Expect(197);
		ExpressionRangeVariableDeclarationList(selectClause.Variables);
		selectClause.EndLocation = t.Location;
			middleClauses.Add(selectClause);

	}

	void DistinctQueryOperator(List<QueryExpressionClause> middleClauses) {
		QueryExpressionDistinctClause distinctClause = new QueryExpressionDistinctClause();
		distinctClause.StartLocation = la.Location;

		Expect(107);
		distinctClause.EndLocation = t.EndLocation;
			middleClauses.Add(distinctClause);

	}

	void WhereQueryOperator(List<QueryExpressionClause> middleClauses) {
		QueryExpressionWhereClause whereClause = new QueryExpressionWhereClause();
		whereClause.StartLocation = la.Location;
		Expression operand = null;

		Expect(230);
		Expr(out operand);
		whereClause.Condition = operand;
			whereClause.EndLocation = t.EndLocation;
			
			middleClauses.Add(whereClause);

	}

	void OrderByQueryOperator(List<QueryExpressionClause> middleClauses) {
		QueryExpressionOrderClause orderClause = new QueryExpressionOrderClause();
		orderClause.StartLocation = la.Location;
		List<QueryExpressionOrdering> orderings = null;

		Expect(176);
		Expect(70);
		OrderExpressionList(out orderings);
		orderClause.Orderings = orderings;
			orderClause.EndLocation = t.EndLocation;
			middleClauses.Add(orderClause);

	}

	void PartitionQueryOperator(out QueryExpressionPartitionVBClause partitionClause) {
		partitionClause = new QueryExpressionPartitionVBClause();
		partitionClause.StartLocation = la.Location;
		Expression expr = null;

		if (la.kind == 212) {
			Get();
			partitionClause.PartitionType = QueryExpressionPartitionType.Take;
			if (la.kind == 231) {
				Get();
				partitionClause.PartitionType = QueryExpressionPartitionType.TakeWhile;
			}
		} else if (la.kind == 203) {
			Get();
			partitionClause.PartitionType = QueryExpressionPartitionType.Skip;
			if (la.kind == 231) {
				Get();
				partitionClause.PartitionType = QueryExpressionPartitionType.SkipWhile;
			}
		} else SynErr(298);
		Expr(out expr);
		partitionClause.Expression = expr;
			partitionClause.EndLocation = t.EndLocation;

	}

	void LetQueryOperator(List<QueryExpressionClause> middleClauses) {
		QueryExpressionLetVBClause letClause = new QueryExpressionLetVBClause();
		letClause.StartLocation = la.Location;

		Expect(148);
		ExpressionRangeVariableDeclarationList(letClause.Variables);
		letClause.EndLocation = t.EndLocation;
			middleClauses.Add(letClause);

	}

	void JoinQueryOperator(out QueryExpressionJoinVBClause joinClause) {
		joinClause = new QueryExpressionJoinVBClause();
		joinClause.StartLocation = la.Location;
		CollectionRangeVariable joinVariable = null;
		QueryExpressionJoinVBClause subJoin = null;
		QueryExpressionJoinConditionVB condition = null;


		Expect(146);
		CollectionRangeVariableDeclaration(out joinVariable);
		joinClause.JoinVariable = joinVariable;
		if (la.kind == 146) {
			JoinQueryOperator(out subJoin);
			joinClause.SubJoin = subJoin;
		}
		Expect(171);
		JoinCondition(out condition);
		SafeAdd(joinClause, joinClause.Conditions, condition);
		while (la.kind == 60) {
			Get();
			JoinCondition(out condition);
			SafeAdd(joinClause, joinClause.Conditions, condition);
		}
		joinClause.EndLocation = t.EndLocation;

	}

	void GroupJoinQueryOperator(out QueryExpressionGroupJoinVBClause groupJoinClause) {
		groupJoinClause = new QueryExpressionGroupJoinVBClause();
		groupJoinClause.StartLocation = la.Location;
		QueryExpressionJoinVBClause joinClause = null;

		Expect(133);
		JoinQueryOperator(out joinClause);
		Expect(143);
		ExpressionRangeVariableDeclarationList(groupJoinClause.IntoVariables);
		groupJoinClause.JoinClause = joinClause;
			groupJoinClause.EndLocation = t.EndLocation;

	}

	void GroupByQueryOperator(out QueryExpressionGroupVBClause groupByClause) {
		groupByClause = new QueryExpressionGroupVBClause();
		groupByClause.StartLocation = la.Location;

		Expect(133);
		ExpressionRangeVariableDeclarationList(groupByClause.GroupVariables);
		Expect(70);
		ExpressionRangeVariableDeclarationList(groupByClause.ByVariables);
		Expect(143);
		ExpressionRangeVariableDeclarationList(groupByClause.IntoVariables);
		groupByClause.EndLocation = t.EndLocation;

	}

	void OrderExpressionList(out List<QueryExpressionOrdering> orderings) {
		orderings = new List<QueryExpressionOrdering>();
		QueryExpressionOrdering ordering = null;

		OrderExpression(out ordering);
		orderings.Add(ordering);
		while (la.kind == 22) {
			Get();
			OrderExpression(out ordering);
			orderings.Add(ordering);
		}
	}

	void OrderExpression(out QueryExpressionOrdering ordering) {
		ordering = new QueryExpressionOrdering();
		ordering.StartLocation = la.Location;
		ordering.Direction = QueryExpressionOrderingDirection.None;
		Expression orderExpr = null;

		Expr(out orderExpr);
		ordering.Criteria = orderExpr;

		if (la.kind == 64 || la.kind == 104) {
			if (la.kind == 64) {
				Get();
				ordering.Direction = QueryExpressionOrderingDirection.Ascending;
			} else {
				Get();
				ordering.Direction = QueryExpressionOrderingDirection.Descending;
			}
		}
		ordering.EndLocation = t.EndLocation;
	}

	void ExpressionRangeVariableDeclarationList(List<ExpressionRangeVariable> variables) {
		ExpressionRangeVariable variable = null;

		ExpressionRangeVariableDeclaration(out variable);
		variables.Add(variable);
		while (la.kind == 22) {
			Get();
			ExpressionRangeVariableDeclaration(out variable);
			variables.Add(variable);
		}
	}

	void CollectionRangeVariableDeclarationList(List<CollectionRangeVariable> rangeVariables) {
		CollectionRangeVariable variableDeclaration;
		CollectionRangeVariableDeclaration(out variableDeclaration);
		rangeVariables.Add(variableDeclaration);
		while (la.kind == 22) {
			Get();
			CollectionRangeVariableDeclaration(out variableDeclaration);
			rangeVariables.Add(variableDeclaration);
		}
	}

	void CollectionRangeVariableDeclaration(out CollectionRangeVariable rangeVariable) {
		rangeVariable = new CollectionRangeVariable();
		rangeVariable.StartLocation = la.Location;
		TypeReference typeName = null;
		Expression inExpr = null;

		Identifier();
		rangeVariable.Identifier = t.val;
		if (la.kind == 63) {
			Get();
			TypeName(out typeName);
			rangeVariable.Type = typeName;
		}
		Expect(138);
		Expr(out inExpr);
		rangeVariable.Expression = inExpr;
			rangeVariable.EndLocation = t.EndLocation;

	}

	void ExpressionRangeVariableDeclaration(out ExpressionRangeVariable variable) {
		variable = new ExpressionRangeVariable();
		variable.StartLocation = la.Location;
		Expression rhs = null;
		TypeReference typeName = null;

		if (IsIdentifiedExpressionRange()) {
			Identifier();
			variable.Identifier = t.val;
			if (la.kind == 63) {
				Get();
				TypeName(out typeName);
				variable.Type = typeName;
			}
			Expect(20);
		}
		Expr(out rhs);
		variable.Expression = rhs;
			variable.EndLocation = t.EndLocation;

	}

	void JoinCondition(out QueryExpressionJoinConditionVB condition) {
		condition = new QueryExpressionJoinConditionVB();
		condition.StartLocation = la.Location;

		Expression lhs = null;
		Expression rhs = null;

		Expr(out lhs);
		Expect(116);
		Expr(out rhs);
		condition.LeftSide = lhs;
			condition.RightSide = rhs;
			condition.EndLocation = t.EndLocation;

	}

	void Argument(out Expression argumentexpr) {
		Expression expr;
		argumentexpr = null;
		string name;
		Location startLocation = la.Location;

		if (IsNamedAssign()) {
			Identifier();
			name = t.val; 
			Expect(55);
			Expr(out expr);
			argumentexpr = new NamedArgumentExpression(name, expr) { StartLocation = startLocation, EndLocation = t.EndLocation };

		} else if (StartOf(24)) {
			Expr(out argumentexpr);
		} else SynErr(299);
	}

	void QualIdentAndTypeArguments(out TypeReference typeref, bool canBeUnbound) {
		string name; typeref = null;
		Qualident(out name);
		typeref = new TypeReference(name);
		if (la.kind == Tokens.OpenParenthesis && Peek(1).kind == Tokens.Of) {
			Expect(37);
			Expect(169);
			if (canBeUnbound && (la.kind == Tokens.CloseParenthesis || la.kind == Tokens.Comma)) {
				typeref.GenericTypes.Add(NullTypeReference.Instance);
				while (la.kind == 22) {
					Get();
					typeref.GenericTypes.Add(NullTypeReference.Instance);
				}
			} else if (StartOf(9)) {
				TypeArgumentList(typeref.GenericTypes);
			} else SynErr(300);
			Expect(38);
		}
	}

	void RankList(out int i) {
		i = 0;
		while (la.kind == 22) {
			Get();
			++i;
		}
	}

	void Attribute(out ASTAttribute attribute) {
		string name;
		List<Expression> positional = new List<Expression>();
		List<NamedArgumentExpression> named = new List<NamedArgumentExpression>();
		Location startLocation = la.Location;

		if (la.kind == 130) {
			Get();
			Expect(26);
		}
		Qualident(out name);
		if (la.kind == 37) {
			AttributeArguments(positional, named);
		}
		attribute  = new ASTAttribute(name, positional, named) { StartLocation = startLocation, EndLocation = t.EndLocation };

	}

	void AttributeArguments(List<Expression> positional, List<NamedArgumentExpression> named) {
		bool nameFound = false;
		string name = "";
		Expression expr;

		Expect(37);
		if (IsNotClosingParenthesis()) {
			Location startLocation = la.Location;
			if (IsNamedAssign()) {
				nameFound = true;
				IdentifierOrKeyword(out name);
				if (la.kind == 55) {
					Get();
				} else if (la.kind == 20) {
					Get();
				} else SynErr(301);
			}
			Expr(out expr);
			if (expr != null) {
					if (string.IsNullOrEmpty(name)) { positional.Add(expr); }
					else { named.Add(new NamedArgumentExpression(name, expr) { StartLocation = startLocation, EndLocation = t.EndLocation }); name = ""; }
				}

			while (la.kind == 22) {
				Get();
				if (IsNamedAssign()) {
					nameFound = true;
					IdentifierOrKeyword(out name);
					if (la.kind == 55) {
						Get();
					} else if (la.kind == 20) {
						Get();
					} else SynErr(302);
				} else if (StartOf(24)) {
					if (nameFound) Error("no positional argument after named argument");
				} else SynErr(303);
				Expr(out expr);
				if (expr != null) { if(name == "") positional.Add(expr);
					else { named.Add(new NamedArgumentExpression(name, expr) { StartLocation = startLocation, EndLocation = t.EndLocation }); name = ""; }
					}

			}
		}
		Expect(38);
	}

	void ParameterModifier(ParamModifierList m) {
		if (la.kind == 72) {
			Get();
			m.Add(ParameterModifiers.In);
		} else if (la.kind == 69) {
			Get();
			m.Add(ParameterModifiers.Ref);
		} else if (la.kind == 174) {
			Get();
			m.Add(ParameterModifiers.Optional);
		} else if (la.kind == 182) {
			Get();
			m.Add(ParameterModifiers.Params);
		} else SynErr(304);
	}

	void Statement() {
		Statement stmt = null;
		Location startPos = la.Location;
		string label = String.Empty;

		if (IsLabel()) {
			LabelName(out label);
			AddChild(new LabelStatement(t.val));

			Expect(21);
			Statement();
		} else if (StartOf(1)) {
			EmbeddedStatement(out stmt);
			AddChild(stmt);
		} else SynErr(305);
		if (stmt != null) {
				stmt.StartLocation = startPos;
				stmt.EndLocation = t.Location;
			}

	}

	void LabelName(out string name) {
		name = string.Empty;

		if (StartOf(5)) {
			Identifier();
			name = t.val;
		} else if (la.kind == 5) {
			Get();
			name = t.val;
		} else SynErr(306);
	}

	void LocalDeclarationStatement(out Statement statement) {
		ModifierList m = new ModifierList();
		LocalVariableDeclaration localVariableDeclaration;
		bool dimfound = false;

		while (la.kind == 88 || la.kind == 105 || la.kind == 204) {
			if (la.kind == 88) {
				Get();
				m.Add(Modifiers.Const, t.Location);
			} else if (la.kind == 204) {
				Get();
				m.Add(Modifiers.Static, t.Location);
			} else {
				Get();
				dimfound = true;
			}
		}
		if(dimfound && (m.Modifier & Modifiers.Const) != 0) {
				Error("Dim is not allowed on constants.");
			}
			
			if(m.isNone && dimfound == false) {
				Error("Const, Dim or Static expected");
			}
			
			localVariableDeclaration = new LocalVariableDeclaration(m.Modifier);
			localVariableDeclaration.StartLocation = t.Location;

		VariableDeclarator(localVariableDeclaration.Variables);
		while (la.kind == 22) {
			Get();
			VariableDeclarator(localVariableDeclaration.Variables);
		}
		statement = localVariableDeclaration;

	}

	void ExitStatement(out Statement statement) {
		Expect(120);
		ExitType exitType = ExitType.None;
		switch (la.kind) {
		case 210: {
			Get();
			exitType = ExitType.Sub;
			break;
		}
		case 127: {
			Get();
			exitType = ExitType.Function;
			break;
		}
		case 186: {
			Get();
			exitType = ExitType.Property;
			break;
		}
		case 108: {
			Get();
			exitType = ExitType.Do;
			break;
		}
		case 124: {
			Get();
			exitType = ExitType.For;
			break;
		}
		case 218: {
			Get();
			exitType = ExitType.Try;
			break;
		}
		case 231: {
			Get();
			exitType = ExitType.While;
			break;
		}
		case 197: {
			Get();
			exitType = ExitType.Select;
			break;
		}
		default: SynErr(307); break;
		}
		statement = new ExitStatement(exitType);
	}

	void TryStatement(out Statement tryStatement) {
		Statement blockStmt = null;
		Statement finallyStmt = null;
		CatchClause clause = null;
		List<CatchClause> catchClauses = new List<CatchClause>();

		Expect(218);
		EndOfStmt();
		Block(out blockStmt);
		while (la.kind == 75) {
			CatchClause(out clause);
			if (clause != null) catchClauses.Add(clause);
		}
		if (la.kind == 123) {
			Get();
			EndOfStmt();
			Block(out finallyStmt);
		}
		Expect(113);
		Expect(218);
		tryStatement = new TryCatchStatement(blockStmt, catchClauses, finallyStmt);
	}

	void ContinueStatement(out Statement statement) {
		Expect(89);
		ContinueType continueType = ContinueType.None;
		if (la.kind == 108 || la.kind == 124 || la.kind == 231) {
			if (la.kind == 108) {
				Get();
				continueType = ContinueType.Do;
			} else if (la.kind == 124) {
				Get();
				continueType = ContinueType.For;
			} else {
				Get();
				continueType = ContinueType.While;
			}
		}
		statement = new ContinueStatement(continueType);
	}

	void ThrowStatement(out Statement statement) {
		Expression expr = null;
		Expect(215);
		if (StartOf(24)) {
			Expr(out expr);
		}
		statement = new ThrowStatement(expr);
	}

	void ReturnStatement(out Statement statement) {
		Expression expr = null;
		Expect(195);
		if (StartOf(24)) {
			Expr(out expr);
		}
		statement = new ReturnStatement(expr);
	}

	void SyncLockStatement(out Statement statement) {
		Expression expr; Statement embeddedStatement;
		Expect(211);
		Expr(out expr);
		EndOfStmt();
		Block(out embeddedStatement);
		Expect(113);
		Expect(211);
		statement = new LockStatement(expr, embeddedStatement);
	}

	void RaiseEventStatement(out Statement statement) {
		List<Expression> arguments = null;
		Expect(189);
		Identifier();
		string name = t.val;
		if (la.kind == 37) {
			Get();
			ArgumentList(out arguments);
			Expect(38);
		}
		statement = new RaiseEventStatement(name, arguments);
	}

	void WithStatement(out Statement withStatement) {
		Statement blockStmt = null;
		Expression expr = null;

		Expect(233);
		Location start = t.Location;
		Expr(out expr);
		EndOfStmt();
		withStatement = new WithStatement(expr);
			withStatement.StartLocation = start;

		Block(out blockStmt);
		((WithStatement)withStatement).Body = (BlockStatement)blockStmt;

		Expect(113);
		Expect(233);
		withStatement.EndLocation = t.Location;
	}

	void AddHandlerStatement(out Statement statement) {
		Expression expr = null;
		Expect(56);
		Expression handlerExpr = null;
		Expr(out expr);
		Expect(22);
		Expr(out handlerExpr);
		statement = new AddHandlerStatement(expr, handlerExpr);
	}

	void RemoveHandlerStatement(out Statement statement) {
		Expression expr = null;
		Expect(193);
		Expression handlerExpr = null;
		Expr(out expr);
		Expect(22);
		Expr(out handlerExpr);
		statement = new RemoveHandlerStatement(expr, handlerExpr);
	}

	void WhileStatement(out Statement statement) {
		Expression expr = null; Statement embeddedStatement;
		Expect(231);
		Expr(out expr);
		EndOfStmt();
		Block(out embeddedStatement);
		Expect(113);
		Expect(231);
		statement = new DoLoopStatement(expr, embeddedStatement, ConditionType.While, ConditionPosition.Start);
	}

	void DoLoopStatement(out Statement statement) {
		Expression expr = null; Statement embeddedStatement; statement = null;
		Expect(108);
		ConditionType conditionType = ConditionType.None;
		if (la.kind == 224 || la.kind == 231) {
			WhileOrUntil(out conditionType);
			Expr(out expr);
			EndOfStmt();
			Block(out embeddedStatement);
			Expect(152);
			statement = new DoLoopStatement(expr, 
				                                embeddedStatement, 
				                                conditionType == ConditionType.While ? ConditionType.DoWhile : conditionType, 
				                                ConditionPosition.Start);

		} else if (la.kind == 1 || la.kind == 21) {
			EndOfStmt();
			Block(out embeddedStatement);
			Expect(152);
			if (la.kind == 224 || la.kind == 231) {
				WhileOrUntil(out conditionType);
				Expr(out expr);
			}
			statement = new DoLoopStatement(expr, embeddedStatement, conditionType, ConditionPosition.End);
		} else SynErr(308);
	}

	void ForStatement(out Statement statement) {
		Expression expr = null; Statement embeddedStatement; statement = null; Location startLocation = la.Location;
		Expect(124);
		Expression group = null;
				TypeReference typeReference;
				string        typeName;

		if (la.kind == 110) {
			Get();
			LoopControlVariable(out typeReference, out typeName);
			Expect(138);
			Expr(out group);
			EndOfStmt();
			Block(out embeddedStatement);
			Expect(163);
			if (StartOf(24)) {
				Expr(out expr);
			}
			statement = new ForeachStatement(typeReference, 
				                                 typeName,
				                                 group, 
				                                 embeddedStatement, 
				                                 expr);
				statement.StartLocation = startLocation;
				statement.EndLocation   = t.EndLocation;
				

		} else if (StartOf(42)) {
			Expression start = null;
				Expression end = null;
				Expression step = null;
				Expression variableExpr = null;
				Expression nextExpr = null;
				List<Expression> nextExpressions = null;

			if (IsLoopVariableDeclaration()) {
				LoopControlVariable(out typeReference, out typeName);
			} else {
				typeReference = null; typeName = null;
				SimpleExpr(out variableExpr);
			}
			Expect(20);
			Expr(out start);
			Expect(216);
			Expr(out end);
			if (la.kind == 205) {
				Get();
				Expr(out step);
			}
			EndOfStmt();
			Block(out embeddedStatement);
			Expect(163);
			if (StartOf(24)) {
				Expr(out nextExpr);
				nextExpressions = new List<Expression>();
					nextExpressions.Add(nextExpr);

				while (la.kind == 22) {
					Get();
					Expr(out nextExpr);
					nextExpressions.Add(nextExpr);
				}
			}
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

		} else SynErr(309);
	}

	void ErrorStatement(out Statement statement) {
		Expression expr = null;
		Expect(118);
		Expr(out expr);
		statement = new ErrorStatement(expr);
	}

	void ReDimStatement(out Statement statement) {
		Expression expr = null;
		Expect(191);
		bool isPreserve = false;
		if (la.kind == 184) {
			Expect(184);
			isPreserve = true;
		}
		ReDimClause(out expr);
		ReDimStatement reDimStatement = new ReDimStatement(isPreserve);
			statement = reDimStatement;
			SafeAdd(reDimStatement, reDimStatement.ReDimClauses, expr as InvocationExpression);

		while (la.kind == 22) {
			Get();
			ReDimClause(out expr);
			SafeAdd(reDimStatement, reDimStatement.ReDimClauses, expr as InvocationExpression);
		}
	}

	void EraseStatement(out Statement statement) {
		Expression expr = null;
		Expect(117);
		Expr(out expr);
		EraseStatement eraseStatement = new EraseStatement();
			if (expr != null) { SafeAdd(eraseStatement, eraseStatement.Expressions, expr);}

		while (la.kind == 22) {
			Get();
			Expr(out expr);
			if (expr != null) { SafeAdd(eraseStatement, eraseStatement.Expressions, expr); }
		}
		statement = eraseStatement;
	}

	void StopStatement(out Statement statement) {
		Expect(206);
		statement = new StopStatement();
	}

	void IfStatement(out Statement statement) {
		Expression expr = null; Statement embeddedStatement; statement = null;
		Expect(135);
		Location ifStartLocation = t.Location;
		Expr(out expr);
		if (la.kind == 214) {
			Get();
		}
		if (la.kind == 1 || la.kind == 21) {
			EndOfStmt();
			Block(out embeddedStatement);
			IfElseStatement ifStatement = new IfElseStatement(expr, embeddedStatement);
				ifStatement.StartLocation = ifStartLocation;
				Location elseIfStart;

			while (la.kind == 112 || (IsElseIf())) {
				if (IsElseIf()) {
					Expect(111);
					elseIfStart = t.Location;
					Expect(135);
				} else {
					Get();
					elseIfStart = t.Location;
				}
				Expression condition = null; Statement block = null;
				Expr(out condition);
				if (la.kind == 214) {
					Get();
				}
				EndOfStmt();
				Block(out block);
				ElseIfSection elseIfSection = new ElseIfSection(condition, block);
					elseIfSection.StartLocation = elseIfStart;
					elseIfSection.EndLocation = t.Location;
					elseIfSection.Parent = ifStatement;
					ifStatement.ElseIfSections.Add(elseIfSection);

			}
			if (la.kind == 111) {
				Get();
				if (la.kind == 1 || la.kind == 21) {
					EndOfStmt();
				}
				Block(out embeddedStatement);
				ifStatement.FalseStatement.Add(embeddedStatement);

			}
			Expect(113);
			Expect(135);
			ifStatement.EndLocation = t.Location;
				statement = ifStatement;

		} else if (StartOf(44)) {
			IfElseStatement ifStatement = new IfElseStatement(expr);
				ifStatement.StartLocation = ifStartLocation;

			SingleLineStatementList(ifStatement.TrueStatement);
			if (la.kind == 111) {
				Get();
				if (StartOf(44)) {
					SingleLineStatementList(ifStatement.FalseStatement);
				}
			}
			ifStatement.EndLocation = t.Location; statement = ifStatement;
		} else SynErr(310);
	}

	void SelectStatement(out Statement statement) {
		Expression expr = null;
		Expect(197);
		if (la.kind == 74) {
			Get();
		}
		Expr(out expr);
		EndOfStmt();
		List<SwitchSection> selectSections = new List<SwitchSection>();
			Statement block = null;

		while (la.kind == 74) {
			List<CaseLabel> caseClauses = null; Location caseLocation = la.Location;
			Get();
			CaseClauses(out caseClauses);
			if (IsNotStatementSeparator()) {
				Expect(21);
			}
			EndOfStmt();
			SwitchSection selectSection = new SwitchSection(caseClauses);
				selectSection.StartLocation = caseLocation;

			Block(out block);
			selectSection.Children = block.Children;
				selectSection.EndLocation = t.EndLocation;
				selectSections.Add(selectSection);

		}
		statement = new SwitchStatement(expr, selectSections);

		Expect(113);
		Expect(197);
	}

	void OnErrorStatement(out OnErrorStatement stmt) {
		stmt = null;
		Location startLocation = la.Location;
		GotoStatement goToStatement = null;

		Expect(171);
		Expect(118);
		if (IsNegativeLabelName()) {
			Expect(132);
			Expect(30);
			Expect(5);
			long intLabel = Int64.Parse(t.val);
				if(intLabel != 1) {
					Error("invalid label in on error statement.");
				}
				stmt = new OnErrorStatement(new GotoStatement((intLabel * -1).ToString()));

		} else if (la.kind == 132) {
			GotoStatement(out goToStatement);
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
			Get();
			Expect(163);
			stmt = new OnErrorStatement(new ResumeStatement(true));

		} else SynErr(311);
		if (stmt != null) {
				stmt.StartLocation = startLocation;
				stmt.EndLocation = t.EndLocation;
			}

	}

	void GotoStatement(out GotoStatement goToStatement) {
		string label = string.Empty;
		Location startLocation = la.Location;

		Expect(132);
		LabelName(out label);
		goToStatement = new GotoStatement(label) {
				StartLocation = startLocation,
				EndLocation = t.EndLocation
			};

	}

	void ResumeStatement(out Statement resumeStatement) {
		resumeStatement = null;
		string label = string.Empty;

		Expect(194);
		if (StartOf(45)) {
			if (la.kind == 163) {
				Get();
				resumeStatement = new ResumeStatement(true);
			} else {
				LabelName(out label);
			}
		}
		resumeStatement = new ResumeStatement(label);
	}

	void ExpressionStatement(out Statement statement) {
		Expression expr = null;
		Expression val = null;
			AssignmentOperatorType op;
			Location startLoc = la.Location;
			
			bool mustBeAssignment = la.kind == Tokens.Plus  || la.kind == Tokens.Minus ||
			                        la.kind == Tokens.Not   || la.kind == Tokens.Times;

		SimpleExpr(out expr);
		if (StartOf(46)) {
			AssignmentOperator(out op);
			Expr(out val);
			expr = new AssignmentExpression(expr, op, val);
				expr.StartLocation = startLoc;
				expr.EndLocation = t.EndLocation;

		} else if (la.kind == 1 || la.kind == 21 || la.kind == 111) {
			if (mustBeAssignment) Error("error in assignment.");
		} else SynErr(312);
		if(expr is MemberReferenceExpression || expr is IdentifierExpression) {
				Location endLocation = expr.EndLocation;
				expr = new InvocationExpression(expr);
				expr.StartLocation = startLoc;
				expr.EndLocation = endLocation;
			}
			statement = new ExpressionStatement(expr);

	}

	void InvocationStatement(out Statement statement) {
		Expression expr = null;
		Expect(73);
		SimpleExpr(out expr);
		statement = new ExpressionStatement(expr);
	}

	void UsingStatement(out Statement statement) {
		Expression expr = null; Statement block; statement = null;
		Expect(226);
		if (Peek(1).kind == Tokens.As) {
			LocalVariableDeclaration resourceAquisition = 
			new LocalVariableDeclaration(Modifiers.None);
			VariableDeclarator(resourceAquisition.Variables);
			while (la.kind == 22) {
				Get();
				VariableDeclarator(resourceAquisition.Variables);
			}
			EndOfStmt();
			Block(out block);
			statement = new UsingStatement(resourceAquisition, block);
		} else if (StartOf(24)) {
			Expr(out expr);
			EndOfStmt();
			Block(out block);
			statement = new UsingStatement(new ExpressionStatement(expr), block);
		} else SynErr(313);
		Expect(113);
		Expect(226);
	}

	void WhileOrUntil(out ConditionType conditionType) {
		conditionType = ConditionType.None;
		if (la.kind == 231) {
			Get();
			conditionType = ConditionType.While;
		} else if (la.kind == 224) {
			Get();
			conditionType = ConditionType.Until;
		} else SynErr(314);
	}

	void LoopControlVariable(out TypeReference type, out string name) {
		ArrayList arrayModifiers = null;
		type = null;

		Qualident(out name);
		if (IsDims()) {
			ArrayTypeModifiers(out arrayModifiers);
		}
		if (la.kind == 63) {
			Get();
			TypeName(out type);
			if (name.IndexOf('.') > 0) { Error("No type def for 'for each' member indexer allowed."); }
		}
		if (type != null) {
				if(type.RankSpecifier != null && arrayModifiers != null) {
					Error("array rank only allowed one time");
				} else if (arrayModifiers != null) {
					type.RankSpecifier = (int[])arrayModifiers.ToArray(typeof(int));
				}
			}

	}

	void ReDimClause(out Expression expr) {
		SimpleNonInvocationExpression(out expr);
		ReDimClauseInternal(ref expr);
	}

	void SingleLineStatementList(List<Statement> list) {
		Statement embeddedStatement = null;
		if (la.kind == 113) {
			Get();
			embeddedStatement = new EndStatement() { StartLocation = t.Location, EndLocation = t.EndLocation };
		} else if (StartOf(1)) {
			EmbeddedStatement(out embeddedStatement);
		} else SynErr(315);
		if (embeddedStatement != null) list.Add(embeddedStatement);
		while (la.kind == 21) {
			Get();
			while (la.kind == 21) {
				Get();
			}
			if (la.kind == 113) {
				Get();
				embeddedStatement = new EndStatement() { StartLocation = t.Location, EndLocation = t.EndLocation };
			} else if (StartOf(1)) {
				EmbeddedStatement(out embeddedStatement);
			} else SynErr(316);
			if (embeddedStatement != null) list.Add(embeddedStatement);
		}
	}

	void CaseClauses(out List<CaseLabel> caseClauses) {
		caseClauses = new List<CaseLabel>();
		CaseLabel caseClause = null;

		CaseClause(out caseClause);
		if (caseClause != null) { caseClauses.Add(caseClause); }
		while (la.kind == 22) {
			Get();
			CaseClause(out caseClause);
			if (caseClause != null) { caseClauses.Add(caseClause); }
		}
	}

	void ReDimClauseInternal(ref Expression expr) {
		List<Expression> arguments; bool canBeNormal; bool canBeRedim; string name; Location startLocation = la.Location;
		while (la.kind == 26 || (la.kind == Tokens.OpenParenthesis && Peek(1).kind == Tokens.Of)) {
			if (la.kind == 26) {
				Get();
				IdentifierOrKeyword(out name);
				expr = new MemberReferenceExpression(expr, name) { StartLocation = startLocation, EndLocation = t.EndLocation };
			} else {
				InvocationExpression(ref expr);
				expr.StartLocation = startLocation;
					expr.EndLocation = t.EndLocation;

			}
		}
		Expect(37);
		NormalOrReDimArgumentList(out arguments, out canBeNormal, out canBeRedim);
		Expect(38);
		expr = new InvocationExpression(expr, arguments);
			if (canBeRedim == false || canBeNormal && (la.kind == Tokens.Dot || la.kind == Tokens.OpenParenthesis)) {
				if (this.Errors.Count == 0) {
					// don't recurse on parse errors - could result in endless recursion
					ReDimClauseInternal(ref expr);
				}
			}

	}

	void CaseClause(out CaseLabel caseClause) {
		Expression expr = null;
		Expression sexpr = null;
		BinaryOperatorType op = BinaryOperatorType.None;
		caseClause = null;

		if (la.kind == 111) {
			Get();
			caseClause = new CaseLabel();
		} else if (StartOf(47)) {
			if (la.kind == 144) {
				Get();
			}
			switch (la.kind) {
			case 40: {
				Get();
				op = BinaryOperatorType.LessThan;
				break;
			}
			case 39: {
				Get();
				op = BinaryOperatorType.GreaterThan;
				break;
			}
			case 43: {
				Get();
				op = BinaryOperatorType.LessThanOrEqual;
				break;
			}
			case 42: {
				Get();
				op = BinaryOperatorType.GreaterThanOrEqual;
				break;
			}
			case 20: {
				Get();
				op = BinaryOperatorType.Equality;
				break;
			}
			case 41: {
				Get();
				op = BinaryOperatorType.InEquality;
				break;
			}
			default: SynErr(317); break;
			}
			Expr(out expr);
			caseClause = new CaseLabel(op, expr);

		} else if (StartOf(24)) {
			Expr(out expr);
			if (la.kind == 216) {
				Get();
				Expr(out sexpr);
			}
			caseClause = new CaseLabel(expr, sexpr);

		} else SynErr(318);
	}

	void CatchClause(out CatchClause catchClause) {
		TypeReference type = null;
		Statement blockStmt = null;
		Expression expr = null;
		string name = String.Empty;

		Expect(75);
		if (StartOf(5)) {
			Identifier();
			name = t.val;
			if (la.kind == 63) {
				Get();
				TypeName(out type);
			}
		}
		if (la.kind == 229) {
			Get();
			Expr(out expr);
		}
		EndOfStmt();
		Block(out blockStmt);
		catchClause = new CatchClause(type, name, blockStmt, expr);
	}



	public void ParseRoot() {
		VB();
		Expect(0); // expect end-of-file automatically added

	}

	static readonly BitArray[] set = {
		new BitArray(new int[] {2097155, 0, 0, 0, 0, 0, 0, 0}),
		new BitArray(new int[] {1007618044, 1191182376, -1051937, 1466973983, -1030969162, -1593504476, -21406146, 711}),
		new BitArray(new int[] {0, 256, 1048576, 537395328, 402669568, 444596289, 131456, 0}),
		new BitArray(new int[] {0, 256, 1048576, 537395328, 402669568, 444596288, 131456, 0}),
		new BitArray(new int[] {0, 0, 0, 536870912, 268435456, 444596288, 384, 0}),
		new BitArray(new int[] {4, 1140850688, 8388687, 1108347140, 821280, 17105920, -2144335872, 65}),
		new BitArray(new int[] {4, 1140850688, 8388687, 1108347136, 821280, 17105920, -2144335872, 65}),
		new BitArray(new int[] {4, 1140850944, 8388975, 1108347140, 821280, 21316608, -2144335872, 65}),
		new BitArray(new int[] {4, 1140850688, 9699551, 1108355356, 9218084, 17106180, -533524976, 67}),
		new BitArray(new int[] {4, 1140850688, 8650975, 1108355356, 9218084, 17106176, -533656048, 67}),
		new BitArray(new int[] {4, 1140850944, 26214479, -493351964, 940361760, 1606227138, -2143942272, 3393}),
		new BitArray(new int[] {0, 0, 0, 536871488, 805306368, 1522008256, 384, 3072}),
		new BitArray(new int[] {4, 1140850688, 8388687, 1108347140, 821284, 17105920, -2144335872, 65}),
		new BitArray(new int[] {0, 0, 262288, 8216, 8396800, 0, 1610679824, 2}),
		new BitArray(new int[] {4, 1140850944, 8388687, 1108347140, 821280, 17105920, -2144335872, 65}),
		new BitArray(new int[] {0, 256, 1048576, -1601699136, 939540480, 1589117120, 393600, 3072}),
		new BitArray(new int[] {0, 1073741824, 4, -2147483648, 0, 0, -2147221504, 0}),
		new BitArray(new int[] {2097154, 32, 0, 0, 256, 0, 0, 0}),
		new BitArray(new int[] {0, 256, 0, 536870912, 1, 436207616, 64, 0}),
		new BitArray(new int[] {0, 16777472, 0, 0, 0, 536870912, 2, 0}),
		new BitArray(new int[] {0, 256, 0, -1602223552, 805306368, 1589117120, 262528, 3072}),
		new BitArray(new int[] {0, 0, 1048576, 524416, 134234112, 0, 131072, 0}),
		new BitArray(new int[] {-66123780, 1174405164, -51384097, 1175465247, -1030969178, 17106228, -97448432, 67}),
		new BitArray(new int[] {7340034, -2147483648, 0, 32768, 0, 0, 0, 0}),
		new BitArray(new int[] {-66123780, 1174405164, -51384097, -972018401, -1030969178, 17106228, -97186288, 67}),
		new BitArray(new int[] {0, 0, 0, 536870912, 1, 436207616, 0, 0}),
		new BitArray(new int[] {0, 256, 0, 536870912, 0, 436207616, 64, 0}),
		new BitArray(new int[] {0, 0, 0, 536870912, 0, 436207616, 64, 0}),
		new BitArray(new int[] {0, 256, 0, 536870912, 1, 436207616, 0, 0}),
		new BitArray(new int[] {0, 0, 288, 0, 0, 4210688, 0, 0}),
		new BitArray(new int[] {0, 0, 0, 536870912, 0, 436207616, 0, 0}),
		new BitArray(new int[] {0, 67108864, 0, 1073743872, 1310752, 65536, 1050656, 64}),
		new BitArray(new int[] {1006632960, 32, 0, 0, 0, 0, 0, 0}),
		new BitArray(new int[] {-2, -1, -1, -1, -1, -1, -1, -1}),
		new BitArray(new int[] {985084, 1174405160, -51384097, 1175465247, -1030969178, 17106212, -97448432, 67}),
		new BitArray(new int[] {1006632960, 0, 0, 0, 0, 0, 0, 0}),
		new BitArray(new int[] {983040, 0, 0, 0, 0, 0, 0, 0}),
		new BitArray(new int[] {4100, 1140850688, 8388687, 1108347140, 821280, 17105920, -2144335872, 65}),
		new BitArray(new int[] {988160, 0, 0, 0, 0, 0, 0, 0}),
		new BitArray(new int[] {-2050, -1, -1, -1, -1, -1, -1, -1}),
		new BitArray(new int[] {1048576, 3968, 0, 0, 4390912, 0, 0, 0}),
		new BitArray(new int[] {-66123780, 1174405164, -51384097, 1175465247, -1030969178, 17106212, -97448432, 67}),
		new BitArray(new int[] {1007618044, 1174405160, -51384097, 1175465247, -1030969178, 17106212, -97448432, 67}),
		new BitArray(new int[] {4, 1140850688, 25165903, 1108347652, 821280, 17105920, -2144331776, 65}),
		new BitArray(new int[] {1007618044, 1191182376, -1051937, 1467105055, -1030969162, -1593504476, -21406146, 711}),
		new BitArray(new int[] {36, 1140850688, 8388687, 1108347140, 821280, 17105928, -2144335872, 65}),
		new BitArray(new int[] {1048576, 8372224, 0, 0, 0, 0, 0, 0}),
		new BitArray(new int[] {1048576, 3968, 0, 0, 65536, 0, 0, 0})

	};
	
	void SynErr(int line, int col, int errorNumber)
	{
		this.Errors.Error(line, col, GetMessage(errorNumber));
	}
	
	string GetMessage(int errorNumber)
	{
		switch (errorNumber) {
						case 0: return "EOF expected";
			case 1: return "EOL expected";
			case 2: return "ident expected";
			case 3: return "LiteralString expected";
			case 4: return "LiteralCharacter expected";
			case 5: return "LiteralInteger expected";
			case 6: return "LiteralDouble expected";
			case 7: return "LiteralSingle expected";
			case 8: return "LiteralDecimal expected";
			case 9: return "LiteralDate expected";
			case 10: return "XmlOpenTag expected";
			case 11: return "XmlCloseTag expected";
			case 12: return "XmlStartInlineVB expected";
			case 13: return "XmlEndInlineVB expected";
			case 14: return "XmlCloseTagEmptyElement expected";
			case 15: return "XmlOpenEndTag expected";
			case 16: return "XmlContent expected";
			case 17: return "XmlComment expected";
			case 18: return "XmlCData expected";
			case 19: return "XmlProcessingInstruction expected";
			case 20: return "\"=\" expected";
			case 21: return "\":\" expected";
			case 22: return "\",\" expected";
			case 23: return "\"&\" expected";
			case 24: return "\"/\" expected";
			case 25: return "\"\\\\\" expected";
			case 26: return "\".\" expected";
			case 27: return "\"...\" expected";
			case 28: return "\".@\" expected";
			case 29: return "\"!\" expected";
			case 30: return "\"-\" expected";
			case 31: return "\"+\" expected";
			case 32: return "\"^\" expected";
			case 33: return "\"?\" expected";
			case 34: return "\"*\" expected";
			case 35: return "\"{\" expected";
			case 36: return "\"}\" expected";
			case 37: return "\"(\" expected";
			case 38: return "\")\" expected";
			case 39: return "\">\" expected";
			case 40: return "\"<\" expected";
			case 41: return "\"<>\" expected";
			case 42: return "\">=\" expected";
			case 43: return "\"<=\" expected";
			case 44: return "\"<<\" expected";
			case 45: return "\">>\" expected";
			case 46: return "\"+=\" expected";
			case 47: return "\"^=\" expected";
			case 48: return "\"-=\" expected";
			case 49: return "\"*=\" expected";
			case 50: return "\"/=\" expected";
			case 51: return "\"\\\\=\" expected";
			case 52: return "\"<<=\" expected";
			case 53: return "\">>=\" expected";
			case 54: return "\"&=\" expected";
			case 55: return "\":=\" expected";
			case 56: return "\"AddHandler\" expected";
			case 57: return "\"AddressOf\" expected";
			case 58: return "\"Aggregate\" expected";
			case 59: return "\"Alias\" expected";
			case 60: return "\"And\" expected";
			case 61: return "\"AndAlso\" expected";
			case 62: return "\"Ansi\" expected";
			case 63: return "\"As\" expected";
			case 64: return "\"Ascending\" expected";
			case 65: return "\"Assembly\" expected";
			case 66: return "\"Auto\" expected";
			case 67: return "\"Binary\" expected";
			case 68: return "\"Boolean\" expected";
			case 69: return "\"ByRef\" expected";
			case 70: return "\"By\" expected";
			case 71: return "\"Byte\" expected";
			case 72: return "\"ByVal\" expected";
			case 73: return "\"Call\" expected";
			case 74: return "\"Case\" expected";
			case 75: return "\"Catch\" expected";
			case 76: return "\"CBool\" expected";
			case 77: return "\"CByte\" expected";
			case 78: return "\"CChar\" expected";
			case 79: return "\"CDate\" expected";
			case 80: return "\"CDbl\" expected";
			case 81: return "\"CDec\" expected";
			case 82: return "\"Char\" expected";
			case 83: return "\"CInt\" expected";
			case 84: return "\"Class\" expected";
			case 85: return "\"CLng\" expected";
			case 86: return "\"CObj\" expected";
			case 87: return "\"Compare\" expected";
			case 88: return "\"Const\" expected";
			case 89: return "\"Continue\" expected";
			case 90: return "\"CSByte\" expected";
			case 91: return "\"CShort\" expected";
			case 92: return "\"CSng\" expected";
			case 93: return "\"CStr\" expected";
			case 94: return "\"CType\" expected";
			case 95: return "\"CUInt\" expected";
			case 96: return "\"CULng\" expected";
			case 97: return "\"CUShort\" expected";
			case 98: return "\"Custom\" expected";
			case 99: return "\"Date\" expected";
			case 100: return "\"Decimal\" expected";
			case 101: return "\"Declare\" expected";
			case 102: return "\"Default\" expected";
			case 103: return "\"Delegate\" expected";
			case 104: return "\"Descending\" expected";
			case 105: return "\"Dim\" expected";
			case 106: return "\"DirectCast\" expected";
			case 107: return "\"Distinct\" expected";
			case 108: return "\"Do\" expected";
			case 109: return "\"Double\" expected";
			case 110: return "\"Each\" expected";
			case 111: return "\"Else\" expected";
			case 112: return "\"ElseIf\" expected";
			case 113: return "\"End\" expected";
			case 114: return "\"EndIf\" expected";
			case 115: return "\"Enum\" expected";
			case 116: return "\"Equals\" expected";
			case 117: return "\"Erase\" expected";
			case 118: return "\"Error\" expected";
			case 119: return "\"Event\" expected";
			case 120: return "\"Exit\" expected";
			case 121: return "\"Explicit\" expected";
			case 122: return "\"False\" expected";
			case 123: return "\"Finally\" expected";
			case 124: return "\"For\" expected";
			case 125: return "\"Friend\" expected";
			case 126: return "\"From\" expected";
			case 127: return "\"Function\" expected";
			case 128: return "\"Get\" expected";
			case 129: return "\"GetType\" expected";
			case 130: return "\"Global\" expected";
			case 131: return "\"GoSub\" expected";
			case 132: return "\"GoTo\" expected";
			case 133: return "\"Group\" expected";
			case 134: return "\"Handles\" expected";
			case 135: return "\"If\" expected";
			case 136: return "\"Implements\" expected";
			case 137: return "\"Imports\" expected";
			case 138: return "\"In\" expected";
			case 139: return "\"Infer\" expected";
			case 140: return "\"Inherits\" expected";
			case 141: return "\"Integer\" expected";
			case 142: return "\"Interface\" expected";
			case 143: return "\"Into\" expected";
			case 144: return "\"Is\" expected";
			case 145: return "\"IsNot\" expected";
			case 146: return "\"Join\" expected";
			case 147: return "\"Key\" expected";
			case 148: return "\"Let\" expected";
			case 149: return "\"Lib\" expected";
			case 150: return "\"Like\" expected";
			case 151: return "\"Long\" expected";
			case 152: return "\"Loop\" expected";
			case 153: return "\"Me\" expected";
			case 154: return "\"Mod\" expected";
			case 155: return "\"Module\" expected";
			case 156: return "\"MustInherit\" expected";
			case 157: return "\"MustOverride\" expected";
			case 158: return "\"MyBase\" expected";
			case 159: return "\"MyClass\" expected";
			case 160: return "\"Namespace\" expected";
			case 161: return "\"Narrowing\" expected";
			case 162: return "\"New\" expected";
			case 163: return "\"Next\" expected";
			case 164: return "\"Not\" expected";
			case 165: return "\"Nothing\" expected";
			case 166: return "\"NotInheritable\" expected";
			case 167: return "\"NotOverridable\" expected";
			case 168: return "\"Object\" expected";
			case 169: return "\"Of\" expected";
			case 170: return "\"Off\" expected";
			case 171: return "\"On\" expected";
			case 172: return "\"Operator\" expected";
			case 173: return "\"Option\" expected";
			case 174: return "\"Optional\" expected";
			case 175: return "\"Or\" expected";
			case 176: return "\"Order\" expected";
			case 177: return "\"OrElse\" expected";
			case 178: return "\"Out\" expected";
			case 179: return "\"Overloads\" expected";
			case 180: return "\"Overridable\" expected";
			case 181: return "\"Overrides\" expected";
			case 182: return "\"ParamArray\" expected";
			case 183: return "\"Partial\" expected";
			case 184: return "\"Preserve\" expected";
			case 185: return "\"Private\" expected";
			case 186: return "\"Property\" expected";
			case 187: return "\"Protected\" expected";
			case 188: return "\"Public\" expected";
			case 189: return "\"RaiseEvent\" expected";
			case 190: return "\"ReadOnly\" expected";
			case 191: return "\"ReDim\" expected";
			case 192: return "\"Rem\" expected";
			case 193: return "\"RemoveHandler\" expected";
			case 194: return "\"Resume\" expected";
			case 195: return "\"Return\" expected";
			case 196: return "\"SByte\" expected";
			case 197: return "\"Select\" expected";
			case 198: return "\"Set\" expected";
			case 199: return "\"Shadows\" expected";
			case 200: return "\"Shared\" expected";
			case 201: return "\"Short\" expected";
			case 202: return "\"Single\" expected";
			case 203: return "\"Skip\" expected";
			case 204: return "\"Static\" expected";
			case 205: return "\"Step\" expected";
			case 206: return "\"Stop\" expected";
			case 207: return "\"Strict\" expected";
			case 208: return "\"String\" expected";
			case 209: return "\"Structure\" expected";
			case 210: return "\"Sub\" expected";
			case 211: return "\"SyncLock\" expected";
			case 212: return "\"Take\" expected";
			case 213: return "\"Text\" expected";
			case 214: return "\"Then\" expected";
			case 215: return "\"Throw\" expected";
			case 216: return "\"To\" expected";
			case 217: return "\"True\" expected";
			case 218: return "\"Try\" expected";
			case 219: return "\"TryCast\" expected";
			case 220: return "\"TypeOf\" expected";
			case 221: return "\"UInteger\" expected";
			case 222: return "\"ULong\" expected";
			case 223: return "\"Unicode\" expected";
			case 224: return "\"Until\" expected";
			case 225: return "\"UShort\" expected";
			case 226: return "\"Using\" expected";
			case 227: return "\"Variant\" expected";
			case 228: return "\"Wend\" expected";
			case 229: return "\"When\" expected";
			case 230: return "\"Where\" expected";
			case 231: return "\"While\" expected";
			case 232: return "\"Widening\" expected";
			case 233: return "\"With\" expected";
			case 234: return "\"WithEvents\" expected";
			case 235: return "\"WriteOnly\" expected";
			case 236: return "\"Xor\" expected";
			case 237: return "\"GetXmlNamespace\" expected";
			case 238: return "??? expected";
			case 239: return "this symbol not expected in EndOfStmt";
			case 240: return "invalid EndOfStmt";
			case 241: return "invalid OptionStmt";
			case 242: return "invalid OptionStmt";
			case 243: return "invalid GlobalAttributeSection";
			case 244: return "invalid GlobalAttributeSection";
			case 245: return "invalid NamespaceMemberDecl";
			case 246: return "invalid OptionValue";
			case 247: return "invalid ImportClause";
			case 248: return "invalid Identifier";
			case 249: return "invalid AttributeSection";
			case 250: return "invalid TypeModifier";
			case 251: return "invalid NonModuleDeclaration";
			case 252: return "invalid NonModuleDeclaration";
			case 253: return "invalid TypeParameterConstraints";
			case 254: return "invalid TypeParameterConstraint";
			case 255: return "invalid NonArrayTypeName";
			case 256: return "invalid MemberModifier";
			case 257: return "invalid StructureMemberDecl";
			case 258: return "invalid StructureMemberDecl";
			case 259: return "invalid StructureMemberDecl";
			case 260: return "invalid StructureMemberDecl";
			case 261: return "invalid StructureMemberDecl";
			case 262: return "invalid StructureMemberDecl";
			case 263: return "invalid StructureMemberDecl";
			case 264: return "invalid StructureMemberDecl";
			case 265: return "invalid InterfaceMemberDecl";
			case 266: return "invalid InterfaceMemberDecl";
			case 267: return "invalid Expr";
			case 268: return "invalid Block";
			case 269: return "invalid Charset";
			case 270: return "invalid IdentifierForFieldDeclaration";
			case 271: return "invalid VariableDeclaratorPartAfterIdentifier";
			case 272: return "invalid ObjectCreateExpression";
			case 273: return "invalid ObjectCreateExpression";
			case 274: return "invalid AccessorDecls";
			case 275: return "invalid EventAccessorDeclaration";
			case 276: return "invalid OverloadableOperator";
			case 277: return "invalid EventMemberSpecifier";
			case 278: return "invalid LambdaExpr";
			case 279: return "invalid AssignmentOperator";
			case 280: return "invalid SimpleExpr";
			case 281: return "invalid SimpleNonInvocationExpression";
			case 282: return "invalid SimpleNonInvocationExpression";
			case 283: return "invalid PrimitiveTypeName";
			case 284: return "invalid CastTarget";
			case 285: return "invalid XmlLiteralExpression";
			case 286: return "invalid XmlContentExpression";
			case 287: return "invalid XmlElement";
			case 288: return "invalid XmlElement";
			case 289: return "invalid XmlNestedContent";
			case 290: return "invalid XmlAttribute";
			case 291: return "invalid XmlAttribute";
			case 292: return "invalid ComparisonExpr";
			case 293: return "invalid SubLambdaExpression";
			case 294: return "invalid FunctionLambdaExpression";
			case 295: return "invalid EmbeddedStatement";
			case 296: return "invalid FromOrAggregateQueryOperator";
			case 297: return "invalid QueryOperator";
			case 298: return "invalid PartitionQueryOperator";
			case 299: return "invalid Argument";
			case 300: return "invalid QualIdentAndTypeArguments";
			case 301: return "invalid AttributeArguments";
			case 302: return "invalid AttributeArguments";
			case 303: return "invalid AttributeArguments";
			case 304: return "invalid ParameterModifier";
			case 305: return "invalid Statement";
			case 306: return "invalid LabelName";
			case 307: return "invalid ExitStatement";
			case 308: return "invalid DoLoopStatement";
			case 309: return "invalid ForStatement";
			case 310: return "invalid IfStatement";
			case 311: return "invalid OnErrorStatement";
			case 312: return "invalid ExpressionStatement";
			case 313: return "invalid UsingStatement";
			case 314: return "invalid WhileOrUntil";
			case 315: return "invalid SingleLineStatementList";
			case 316: return "invalid SingleLineStatementList";
			case 317: return "invalid CaseClause";
			case 318: return "invalid CaseClause";

			default: return "error " + errorNumber;
		}
	}
} // end Parser

} // end namespace
