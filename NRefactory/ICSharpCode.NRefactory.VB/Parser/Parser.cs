using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using ICSharpCode.NRefactory.VB.Ast;
using ICSharpCode.NRefactory.VB.Parser;
using ASTAttribute = ICSharpCode.NRefactory.VB.Ast.Attribute;
using Roles = ICSharpCode.NRefactory.VB.AstNode.Roles;




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
		compilationUnit = new CompilationUnit();
			NodeStart(compilationUnit);
			Get();

		while (la.kind == 1 || la.kind == 21) {
			StatementTerminator();
		}
		while (la.kind == 173) {
			OptionStatement(CompilationUnit.MemberRole);
			while (la.kind == 1 || la.kind == 21) {
				StatementTerminator();
			}
		}
		while (la.kind == 137) {
			ImportsStatement(CompilationUnit.MemberRole);
			while (la.kind == 1 || la.kind == 21) {
				StatementTerminator();
			}
		}
	}

	void StatementTerminator() {
		while (!(la.kind == 0 || la.kind == 1 || la.kind == 21)) {SynErr(239); Get();}
		if (la.kind == 1) {
			Get();
			AddTerminal(Roles.StatementTerminator);
		} else if (la.kind == 21) {
			Get();
			AddTerminal(Roles.StatementTerminator);
		} else SynErr(240);
	}

	void OptionStatement(Role role) {
		var result = new OptionStatement(); NodeStart(result);
		Expect(173);
		AddTerminal(Roles.Keyword);
		if (la.kind == 121) {
			Get();
			AddTerminal(Ast.OptionStatement.OptionTypeRole);
			result.OptionType = OptionType.Explicit;
			if (la.kind == 170 || la.kind == 171) {
				OnOff(result);
			}
		} else if (la.kind == 207) {
			Get();
			AddTerminal(Ast.OptionStatement.OptionTypeRole);
			result.OptionType = OptionType.Strict;
			if (la.kind == 170 || la.kind == 171) {
				OnOff(result);
			}
		} else if (la.kind == 139) {
			Get();
			AddTerminal(Ast.OptionStatement.OptionTypeRole);
			result.OptionType = OptionType.Infer;
			if (la.kind == 170 || la.kind == 171) {
				OnOff(result);
			}
		} else if (la.kind == 87) {
			Get();
			AddTerminal(Ast.OptionStatement.OptionTypeRole);
			result.OptionType = OptionType.Compare;
			BinaryText(result);
		} else SynErr(241);
		StatementTerminator();
		NodeEnd(result, role);
	}

	void ImportsStatement(Role role) {
		var result = new ImportsStatement(); NodeStart(result);
		Expect(137);
		AddTerminal(Roles.Keyword);
		ImportsClause();
		while (la.kind == 22) {
			Get();
			ImportsClause();
		}
		StatementTerminator();
		NodeEnd(result, role);
	}

	void Identifier() {
		if (StartOf(1)) {
			IdentifierForFieldDeclaration();
		} else if (la.kind == 98) {
			Get();
		} else SynErr(242);
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
		default: SynErr(243); break;
		}
	}

	void TypeName(out AstType type) {
		type = null;
		if (StartOf(2)) {
			PrimitiveTypeName(out type);
		} else if (StartOf(3)) {
			QualifiedTypeName(out type);
		} else SynErr(244);
	}

	void PrimitiveTypeName(out AstType type) {
		type = null;
		switch (la.kind) {
		case 168: {
			Get();
			type = new PrimitiveType("Object", t.Location);
			break;
		}
		case 68: {
			Get();
			type = new PrimitiveType("Boolean", t.Location);
			break;
		}
		case 99: {
			Get();
			type = new PrimitiveType("Date", t.Location);
			break;
		}
		case 82: {
			Get();
			type = new PrimitiveType("Char", t.Location);
			break;
		}
		case 208: {
			Get();
			type = new PrimitiveType("String", t.Location);
			break;
		}
		case 100: {
			Get();
			type = new PrimitiveType("Decimal", t.Location);
			break;
		}
		case 71: {
			Get();
			type = new PrimitiveType("Byte", t.Location);
			break;
		}
		case 201: {
			Get();
			type = new PrimitiveType("Short", t.Location);
			break;
		}
		case 141: {
			Get();
			type = new PrimitiveType("Integer", t.Location);
			break;
		}
		case 151: {
			Get();
			type = new PrimitiveType("Long", t.Location);
			break;
		}
		case 202: {
			Get();
			type = new PrimitiveType("Single", t.Location);
			break;
		}
		case 109: {
			Get();
			type = new PrimitiveType("Double", t.Location);
			break;
		}
		case 221: {
			Get();
			type = new PrimitiveType("UInteger", t.Location);
			break;
		}
		case 222: {
			Get();
			type = new PrimitiveType("ULong", t.Location);
			break;
		}
		case 225: {
			Get();
			type = new PrimitiveType("UShort", t.Location);
			break;
		}
		case 196: {
			Get();
			type = new PrimitiveType("SByte", t.Location);
			break;
		}
		default: SynErr(245); break;
		}
	}

	void QualifiedTypeName(out AstType type) {
		if (la.kind == 130) {
			Get();
		} else if (StartOf(4)) {
			Identifier();
		} else SynErr(246);
		type = new SimpleType(t.val, t.Location);
		while (la.kind == 26) {
			Get();
			Identifier();
			type = new QualifiedType(type, new Identifier (t.val, t.Location));
		}
	}

	void OnOff(OptionStatement os) {
		if (la.kind == 171) {
			Get();
			AddTerminal(Ast.OptionStatement.OptionValueRole);
			os.OptionValue = OptionValue.On;
		} else if (la.kind == 170) {
			Get();
			AddTerminal(Ast.OptionStatement.OptionValueRole);
			os.OptionValue  = OptionValue.Off;
		} else SynErr(247);
	}

	void BinaryText(OptionStatement os) {
		if (la.kind == 213) {
			Get();
			AddTerminal(Ast.OptionStatement.OptionValueRole);
			os.OptionValue = OptionValue.Text;
		} else if (la.kind == 67) {
			Get();
			AddTerminal(Ast.OptionStatement.OptionValueRole);
			os.OptionValue  = OptionValue.Binary;
		} else SynErr(248);
	}

	void ImportsClause() {
		if (IsAliasImportsClause()) {
			AliasImportsClause(Ast.ImportsStatement.ImportsClauseRole);
		} else if (StartOf(5)) {
			MemberImportsClause(Ast.ImportsStatement.ImportsClauseRole);
		} else if (la.kind == 10) {
			XmlNamespaceImportsClause(Ast.ImportsStatement.ImportsClauseRole);
		} else SynErr(249);
		while (!(StartOf(6))) {SynErr(250); Get();}
	}

	void AliasImportsClause(Role role) {
		var result = new AliasImportsClause(); NodeStart(result);
		AstType alias;
		Identifier();
		result.Name = new Identifier (t.val, t.Location);
		Expect(20);
		TypeName(out alias);
		result.Alias = alias;
		NodeEnd(result, role);
	}

	void MemberImportsClause(Role role) {
		var result = new MemberImportsClause(); NodeStart(result);
		AstType member;
		TypeName(out member);
		result.Member = member;
		NodeEnd(result, role);
	}

	void XmlNamespaceImportsClause(Role role) {
		var result = new XmlNamespaceImportsClause(); NodeStart(result);
		Expect(10);
		AddTerminal(Roles.XmlOpenTag);
		Identifier();
		Expect(20);
		AddTerminal(Roles.Assign);
		Expect(3);
		Expect(11);
		AddTerminal(Roles.XmlCloseTag);
		NodeEnd(result, role);
	}



	public void ParseRoot() {
		VB();
		Expect(0); // expect end-of-file automatically added

	}

	static readonly BitArray[] set = {
		new BitArray(new int[] {6291459, 0, 0, 0, 0, 0, 0, 0}),
		new BitArray(new int[] {4, 1140850688, 8388687, 1108347136, 821280, 17105920, -2144335872, 65}),
		new BitArray(new int[] {0, 0, 262288, 8216, 8396800, 256, 1610679824, 2}),
		new BitArray(new int[] {4, 1140850688, 8388687, 1108347140, 821284, 17105920, -2144335872, 65}),
		new BitArray(new int[] {4, 1140850688, 8388687, 1108347140, 821280, 17105920, -2144335872, 65}),
		new BitArray(new int[] {4, 1140850688, 8650975, 1108355356, 9218084, 17106176, -533656048, 67}),
		new BitArray(new int[] {6291459, 0, 0, 0, 0, 0, 0, 0})

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
			case 239: return "this symbol not expected in StatementTerminator";
			case 240: return "invalid StatementTerminator";
			case 241: return "invalid OptionStatement";
			case 242: return "invalid Identifier";
			case 243: return "invalid IdentifierForFieldDeclaration";
			case 244: return "invalid TypeName";
			case 245: return "invalid PrimitiveTypeName";
			case 246: return "invalid QualifiedTypeName";
			case 247: return "invalid OnOff";
			case 248: return "invalid BinaryText";
			case 249: return "invalid ImportsClause";
			case 250: return "this symbol not expected in ImportsClause";

			default: return "error " + errorNumber;
		}
	}
} // end Parser

} // end namespace
