// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

namespace ICSharpCode.NRefactory.PrettyPrinter
{
	public enum BraceStyle {
		EndOfLine,
		NextLine,
		NextLineShifted,
		NextLineShifted2
	}
	
	/// <summary>
	/// Description of PrettyPrintOptions.	
	/// </summary>
	public class PrettyPrintOptions : AbstractPrettyPrintOptions
	{
		#region BraceStyle
		BraceStyle namespaceBraceStyle = BraceStyle.NextLine;
		BraceStyle classBraceStyle     = BraceStyle.NextLine;
		BraceStyle interfaceBraceStyle = BraceStyle.NextLine;
		BraceStyle structBraceStyle    = BraceStyle.NextLine;
		BraceStyle enumBraceStyle      = BraceStyle.NextLine;
		
		BraceStyle constructorBraceStyle  = BraceStyle.NextLine;
		BraceStyle destructorBraceStyle   = BraceStyle.NextLine;
		BraceStyle methodBraceStyle       = BraceStyle.NextLine;
		
		BraceStyle propertyBraceStyle     = BraceStyle.EndOfLine;
		BraceStyle propertyGetBraceStyle  = BraceStyle.EndOfLine;
		BraceStyle propertySetBraceStyle  = BraceStyle.EndOfLine;
		
		BraceStyle eventAddBraceStyle     = BraceStyle.EndOfLine;
		BraceStyle eventRemoveBraceStyle  = BraceStyle.EndOfLine;
		
		BraceStyle statementBraceStyle = BraceStyle.EndOfLine;
		
		public BraceStyle StatementBraceStyle {
			get {
				return statementBraceStyle;
			}
			set {
				statementBraceStyle = value;
			}
		}
		
		public BraceStyle NamespaceBraceStyle {
			get {
				return namespaceBraceStyle;
			}
			set {
				namespaceBraceStyle = value;
			}
		}
		
		public BraceStyle ClassBraceStyle {
			get {
				return classBraceStyle;
			}
			set {
				classBraceStyle = value;
			}
		}
		
		public BraceStyle InterfaceBraceStyle {
			get {
				return interfaceBraceStyle;
			}
			set {
				interfaceBraceStyle = value;
			}
		}
		
		public BraceStyle StructBraceStyle {
			get {
				return structBraceStyle;
			}
			set {
				structBraceStyle = value;
			}
		}
		
		public BraceStyle EnumBraceStyle {
			get {
				return enumBraceStyle;
			}
			set {
				enumBraceStyle = value;
			}
		}
		
		
		public BraceStyle ConstructorBraceStyle {
			get {
				return constructorBraceStyle;
			}
			set {
				constructorBraceStyle = value;
			}
		}
		
		public BraceStyle DestructorBraceStyle {
			get {
				return destructorBraceStyle;
			}
			set {
				destructorBraceStyle = value;
			}
		}
		
		public BraceStyle MethodBraceStyle {
			get {
				return methodBraceStyle;
			}
			set {
				methodBraceStyle = value;
			}
		}
		
		public BraceStyle PropertyBraceStyle {
			get {
				return propertyBraceStyle;
			}
			set {
				propertyBraceStyle = value;
			}
		}
		public BraceStyle PropertyGetBraceStyle {
			get {
				return propertyGetBraceStyle;
			}
			set {
				propertyGetBraceStyle = value;
			}
		}
		public BraceStyle PropertySetBraceStyle {
			get {
				return propertySetBraceStyle;
			}
			set {
				propertySetBraceStyle = value;
			}
		}
		
		public BraceStyle EventAddBraceStyle {
			get {
				return eventAddBraceStyle;
			}
			set {
				eventAddBraceStyle = value;
			}
		}
		public BraceStyle EventRemoveBraceStyle {
			get {
				return eventRemoveBraceStyle;
			}
			set {
				eventRemoveBraceStyle = value;
			}
		}
		#endregion
		
		#region Before Parentheses
		bool beforeMethodCallParentheses        = false;
		bool beforeDelegateDeclarationParentheses = false;
		bool beforeMethodDeclarationParentheses = false;
		bool beforeConstructorDeclarationParentheses = false;
		
		bool ifParentheses      = true;
		bool whileParentheses   = true;
		bool forParentheses     = true;
		bool foreachParentheses = true;
		bool catchParentheses   = true;
		bool switchParentheses  = true;
		bool lockParentheses    = true;
		bool usingParentheses   = true;
		bool fixedParentheses   = true;
		bool sizeOfParentheses  = false;
		bool typeOfParentheses  = false;
		bool checkedParentheses  = false;
		bool uncheckedParentheses  = false;
		bool newParentheses  = false;
		
		public bool CheckedParentheses {
			get {
				return checkedParentheses;
			}
			set {
				checkedParentheses = value;
			}
		}
		public bool NewParentheses {
			get {
				return newParentheses;
			}
			set {
				newParentheses = value;
			}
		}
		public bool SizeOfParentheses {
			get {
				return sizeOfParentheses;
			}
			set {
				sizeOfParentheses = value;
			}
		}
		public bool TypeOfParentheses {
			get {
				return typeOfParentheses;
			}
			set {
				typeOfParentheses = value;
			}
		}
		public bool UncheckedParentheses {
			get {
				return uncheckedParentheses;
			}
			set {
				uncheckedParentheses = value;
			}
		}
		
		public bool BeforeConstructorDeclarationParentheses {
			get {
				return beforeConstructorDeclarationParentheses;
			}
			set {
				beforeConstructorDeclarationParentheses = value;
			}
		}
		
		public bool BeforeDelegateDeclarationParentheses {
			get {
				return beforeDelegateDeclarationParentheses;
			}
			set {
				beforeDelegateDeclarationParentheses = value;
			}
		}
		
		public bool BeforeMethodCallParentheses {
			get {
				return beforeMethodCallParentheses;
			}
			set {
				beforeMethodCallParentheses = value;
			}
		}
		
		public bool BeforeMethodDeclarationParentheses {
			get {
				return beforeMethodDeclarationParentheses;
			}
			set {
				beforeMethodDeclarationParentheses = value;
			}
		}
		
		public bool IfParentheses {
			get {
				return ifParentheses;
			}
			set {
				ifParentheses = value;
			}
		}
		
		public bool WhileParentheses {
			get {
				return whileParentheses;
			}
			set {
				whileParentheses = value;
			}
		}
		public bool ForeachParentheses {
			get {
				return foreachParentheses;
			}
			set {
				foreachParentheses = value;
			}
		}
		public bool LockParentheses {
			get {
				return lockParentheses;
			}
			set {
				lockParentheses = value;
			}
		}
		public bool UsingParentheses {
			get {
				return usingParentheses;
			}
			set {
				usingParentheses = value;
			}
		}
		
		public bool CatchParentheses {
			get {
				return catchParentheses;
			}
			set {
				catchParentheses = value;
			}
		}
		public bool FixedParentheses {
			get {
				return fixedParentheses;
			}
			set {
				fixedParentheses = value;
			}
		}
		public bool SwitchParentheses {
			get {
				return switchParentheses;
			}
			set {
				switchParentheses = value;
			}
		}
		public bool ForParentheses {
			get {
				return forParentheses;
			}
			set {
				forParentheses = value;
			}
		}
		
		#endregion
		
		#region AroundOperators
		bool aroundAssignmentParentheses = true;
		bool aroundLogicalOperatorParentheses = true;
		bool aroundEqualityOperatorParentheses = true;
		bool aroundRelationalOperatorParentheses = true;
		bool aroundBitwiseOperatorParentheses = true;
		bool aroundAdditiveOperatorParentheses = true;
		bool aroundMultiplicativeOperatorParentheses = true;
		bool aroundShiftOperatorParentheses = true;
		
		public bool AroundAdditiveOperatorParentheses {
			get {
				return aroundAdditiveOperatorParentheses;
			}
			set {
				aroundAdditiveOperatorParentheses = value;
			}
		}
		public bool AroundAssignmentParentheses {
			get {
				return aroundAssignmentParentheses;
			}
			set {
				aroundAssignmentParentheses = value;
			}
		}
		public bool AroundBitwiseOperatorParentheses {
			get {
				return aroundBitwiseOperatorParentheses;
			}
			set {
				aroundBitwiseOperatorParentheses = value;
			}
		}
		public bool AroundEqualityOperatorParentheses {
			get {
				return aroundEqualityOperatorParentheses;
			}
			set {
				aroundEqualityOperatorParentheses = value;
			}
		}
		public bool AroundLogicalOperatorParentheses {
			get {
				return aroundLogicalOperatorParentheses;
			}
			set {
				aroundLogicalOperatorParentheses = value;
			}
		}
		public bool AroundMultiplicativeOperatorParentheses {
			get {
				return aroundMultiplicativeOperatorParentheses;
			}
			set {
				aroundMultiplicativeOperatorParentheses = value;
			}
		}
		public bool AroundRelationalOperatorParentheses {
			get {
				return aroundRelationalOperatorParentheses;
			}
			set {
				aroundRelationalOperatorParentheses = value;
			}
		}
		public bool AroundShiftOperatorParentheses {
			get {
				return aroundShiftOperatorParentheses;
			}
			set {
				aroundShiftOperatorParentheses = value;
			}
		}
		#endregion
		
		#region SpacesInConditionalOperator
		bool conditionalOperatorBeforeConditionSpace = true;
		bool conditionalOperatorAfterConditionSpace = true;
		
		bool conditionalOperatorBeforeSeparatorSpace = true;
		bool conditionalOperatorAfterSeparatorSpace = true;
		
		public bool ConditionalOperatorAfterConditionSpace {
			get {
				return conditionalOperatorAfterConditionSpace;
			}
			set {
				conditionalOperatorAfterConditionSpace = value;
			}
		}
		public bool ConditionalOperatorAfterSeparatorSpace {
			get {
				return conditionalOperatorAfterSeparatorSpace;
			}
			set {
				conditionalOperatorAfterSeparatorSpace = value;
			}
		}
		public bool ConditionalOperatorBeforeConditionSpace {
			get {
				return conditionalOperatorBeforeConditionSpace;
			}
			set {
				conditionalOperatorBeforeConditionSpace = value;
			}
		}
		public bool ConditionalOperatorBeforeSeparatorSpace {
			get {
				return conditionalOperatorBeforeSeparatorSpace;
			}
			set {
				conditionalOperatorBeforeSeparatorSpace = value;
			}
		}
		#endregion

		#region OtherSpaces
		bool spacesWithinBrackets = false;
		bool spacesAfterComma     = true;
		bool spacesBeforeComma    = false;
		bool spacesAfterSemicolon = true;
		bool spacesAfterTypecast  = false;
		
		public bool SpacesAfterComma {
			get {
				return spacesAfterComma;
			}
			set {
				spacesAfterComma = value;
			}
		}
		public bool SpacesAfterSemicolon {
			get {
				return spacesAfterSemicolon;
			}
			set {
				spacesAfterSemicolon = value;
			}
		}
		public bool SpacesAfterTypecast {
			get {
				return spacesAfterTypecast;
			}
			set {
				spacesAfterTypecast = value;
			}
		}
		public bool SpacesBeforeComma {
			get {
				return spacesBeforeComma;
			}
			set {
				spacesBeforeComma = value;
			}
		}
		public bool SpacesWithinBrackets {
			get {
				return spacesWithinBrackets;
			}
			set {
				spacesWithinBrackets = value;
			}
		}
		#endregion
	}
}
