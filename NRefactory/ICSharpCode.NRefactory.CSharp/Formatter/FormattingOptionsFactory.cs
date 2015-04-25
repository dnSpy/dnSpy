// 
// FormattingOptionsFactory.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// The formatting options factory creates pre defined formatting option styles.
	/// </summary>
	public static class FormattingOptionsFactory
	{
		/// <summary>
		/// Creates empty CSharpFormatting options.
		/// </summary>
		public static CSharpFormattingOptions CreateEmpty()
		{
			return new CSharpFormattingOptions();
		}

		/// <summary>
		/// Creates mono indent style CSharpFormatting options.
		/// </summary>
		public static CSharpFormattingOptions CreateMono()
		{
			return new CSharpFormattingOptions {
				IndentNamespaceBody = true,
				IndentClassBody = true,
				IndentInterfaceBody = true,
				IndentStructBody = true,
				IndentEnumBody = true,
				IndentMethodBody = true,
				IndentPropertyBody = true,
				IndentEventBody = true,
				IndentBlocks = true,
				IndentSwitchBody = false,
				IndentCaseBody = true,
				IndentBreakStatements = true,
				IndentPreprocessorDirectives = true,
				IndentBlocksInsideExpressions = false,
				NamespaceBraceStyle = BraceStyle.NextLine,
				ClassBraceStyle = BraceStyle.NextLine,
				InterfaceBraceStyle = BraceStyle.NextLine,
				StructBraceStyle = BraceStyle.NextLine,
				EnumBraceStyle = BraceStyle.NextLine,
				MethodBraceStyle = BraceStyle.NextLine,
				ConstructorBraceStyle = BraceStyle.NextLine,
				DestructorBraceStyle = BraceStyle.NextLine,
				AnonymousMethodBraceStyle = BraceStyle.EndOfLine,
	
				PropertyBraceStyle = BraceStyle.EndOfLine,
				PropertyGetBraceStyle = BraceStyle.EndOfLine,
				PropertySetBraceStyle = BraceStyle.EndOfLine,
				SimpleGetBlockFormatting = PropertyFormatting.AllowOneLine,
				SimpleSetBlockFormatting = PropertyFormatting.AllowOneLine,

				EventBraceStyle = BraceStyle.EndOfLine,
				EventAddBraceStyle = BraceStyle.EndOfLine,
				EventRemoveBraceStyle = BraceStyle.EndOfLine,
				AllowEventAddBlockInline = true,
				AllowEventRemoveBlockInline = true,
				StatementBraceStyle = BraceStyle.EndOfLine,
	
				ElseNewLinePlacement = NewLinePlacement.SameLine,
				ElseIfNewLinePlacement = NewLinePlacement.SameLine,
				CatchNewLinePlacement = NewLinePlacement.SameLine,
				FinallyNewLinePlacement = NewLinePlacement.SameLine,
				WhileNewLinePlacement = NewLinePlacement.SameLine,
				ArrayInitializerWrapping = Wrapping.WrapIfTooLong,
				ArrayInitializerBraceStyle = BraceStyle.EndOfLine,
				AllowOneLinedArrayInitialziers = true,

				SpaceBeforeMethodCallParentheses = true,
				SpaceBeforeMethodDeclarationParentheses = true,
				SpaceBeforeConstructorDeclarationParentheses = true,
				SpaceBeforeDelegateDeclarationParentheses = true,
				SpaceAfterMethodCallParameterComma = true,
				SpaceAfterConstructorDeclarationParameterComma = true,

				SpaceBeforeNewParentheses = true,
				SpacesWithinNewParentheses = false,
				SpacesBetweenEmptyNewParentheses = false,
				SpaceBeforeNewParameterComma = false,
				SpaceAfterNewParameterComma = true,
				
				SpaceBeforeIfParentheses = true,
				SpaceBeforeWhileParentheses = true,
				SpaceBeforeForParentheses = true,
				SpaceBeforeForeachParentheses = true,
				SpaceBeforeCatchParentheses = true,
				SpaceBeforeSwitchParentheses = true,
				SpaceBeforeLockParentheses = true,
				SpaceBeforeUsingParentheses = true,
				SpaceAroundAssignment = true,
				SpaceAroundLogicalOperator = true,
				SpaceAroundEqualityOperator = true,
				SpaceAroundRelationalOperator = true,
				SpaceAroundBitwiseOperator = true,
				SpaceAroundAdditiveOperator = true,
				SpaceAroundMultiplicativeOperator = true,
				SpaceAroundShiftOperator = true,
				SpaceAroundNullCoalescingOperator = true,
				SpacesWithinParentheses = false,
				SpaceWithinMethodCallParentheses = false,
				SpaceWithinMethodDeclarationParentheses = false,
				SpacesWithinIfParentheses = false,
				SpacesWithinWhileParentheses = false,
				SpacesWithinForParentheses = false,
				SpacesWithinForeachParentheses = false,
				SpacesWithinCatchParentheses = false,
				SpacesWithinSwitchParentheses = false,
				SpacesWithinLockParentheses = false,
				SpacesWithinUsingParentheses = false,
				SpacesWithinCastParentheses = false,
				SpacesWithinSizeOfParentheses = false,
				SpacesWithinTypeOfParentheses = false,
				SpacesWithinCheckedExpressionParantheses = false,
				SpaceBeforeConditionalOperatorCondition = true,
				SpaceAfterConditionalOperatorCondition = true,
				SpaceBeforeConditionalOperatorSeparator = true,
				SpaceAfterConditionalOperatorSeparator = true,
	
				SpacesWithinBrackets = false,
				SpacesBeforeBrackets = true,
				SpaceBeforeBracketComma = false,
				SpaceAfterBracketComma = true,
						
				SpaceBeforeForSemicolon = false,
				SpaceAfterForSemicolon = true,
				SpaceAfterTypecast = false,
				
				AlignEmbeddedStatements = true,
				SimplePropertyFormatting = PropertyFormatting.AllowOneLine,
				AutoPropertyFormatting = PropertyFormatting.AllowOneLine,
				EmptyLineFormatting = EmptyLineFormatting.DoNotIndent,
				SpaceBeforeMethodDeclarationParameterComma = false,
				SpaceAfterMethodDeclarationParameterComma = true,
				SpaceAfterDelegateDeclarationParameterComma = true,
				SpaceBeforeFieldDeclarationComma = false,
				SpaceAfterFieldDeclarationComma = true,
				SpaceBeforeLocalVariableDeclarationComma = false,
				SpaceAfterLocalVariableDeclarationComma = true,
				
				SpaceBeforeIndexerDeclarationBracket = true,
				SpaceWithinIndexerDeclarationBracket = false,
				SpaceBeforeIndexerDeclarationParameterComma = false,
				SpaceInNamedArgumentAfterDoubleColon = true,
				RemoveEndOfLineWhiteSpace = true,
			
				SpaceAfterIndexerDeclarationParameterComma = true,
				
				MinimumBlankLinesBeforeUsings = 0,
				MinimumBlankLinesAfterUsings = 1,
				UsingPlacement = UsingPlacement.TopOfFile,
				
				MinimumBlankLinesBeforeFirstDeclaration = 0,
				MinimumBlankLinesBetweenTypes = 1,
				MinimumBlankLinesBetweenFields = 0,
				MinimumBlankLinesBetweenEventFields = 0,
				MinimumBlankLinesBetweenMembers = 1,
				MinimumBlankLinesAroundRegion = 1,
				MinimumBlankLinesInsideRegion = 1,
				AlignToFirstIndexerArgument = false,
				AlignToFirstIndexerDeclarationParameter = true,
				AlignToFirstMethodCallArgument = false,
				AlignToFirstMethodDeclarationParameter = true,
				KeepCommentsAtFirstColumn = true,
				ChainedMethodCallWrapping = Wrapping.DoNotChange,
				MethodCallArgumentWrapping = Wrapping.DoNotChange,
				NewLineAferMethodCallOpenParentheses = NewLinePlacement.DoNotCare,
				MethodCallClosingParenthesesOnNewLine = NewLinePlacement.DoNotCare,

				IndexerArgumentWrapping = Wrapping.DoNotChange,
				NewLineAferIndexerOpenBracket = NewLinePlacement.DoNotCare,
				IndexerClosingBracketOnNewLine = NewLinePlacement.DoNotCare,

				NewLineBeforeNewQueryClause = NewLinePlacement.NewLine
			};
		}

		/// <summary>
		/// Creates sharp develop indent style CSharpFormatting options.
		/// </summary>
		public static CSharpFormattingOptions CreateSharpDevelop()
		{
			var baseOptions = CreateKRStyle();
			return baseOptions;
		}

		/// <summary>
		/// The K&R style, so named because it was used in Kernighan and Ritchie's book The C Programming Language,
		/// is commonly used in C. It is less common for C++, C#, and others.
		/// </summary>
		public static CSharpFormattingOptions CreateKRStyle()
		{
			return new CSharpFormattingOptions() {
				IndentNamespaceBody = true,
				IndentClassBody = true,
				IndentInterfaceBody = true,
				IndentStructBody = true,
				IndentEnumBody = true,
				IndentMethodBody = true,
				IndentPropertyBody = true,
				IndentEventBody = true,
				IndentBlocks = true,
				IndentSwitchBody = true,
				IndentCaseBody = true,
				IndentBreakStatements = true,
				IndentPreprocessorDirectives = true,
				NamespaceBraceStyle = BraceStyle.NextLine,
				ClassBraceStyle = BraceStyle.NextLine,
				InterfaceBraceStyle = BraceStyle.NextLine,
				StructBraceStyle = BraceStyle.NextLine,
				EnumBraceStyle = BraceStyle.NextLine,
				MethodBraceStyle = BraceStyle.NextLine,
				ConstructorBraceStyle = BraceStyle.NextLine,
				DestructorBraceStyle = BraceStyle.NextLine,
				AnonymousMethodBraceStyle = BraceStyle.EndOfLine,
				PropertyBraceStyle = BraceStyle.EndOfLine,
				PropertyGetBraceStyle = BraceStyle.EndOfLine,
				PropertySetBraceStyle = BraceStyle.EndOfLine,
				SimpleGetBlockFormatting = PropertyFormatting.AllowOneLine,
				SimpleSetBlockFormatting = PropertyFormatting.AllowOneLine,
	
				EventBraceStyle = BraceStyle.EndOfLine,
				EventAddBraceStyle = BraceStyle.EndOfLine,
				EventRemoveBraceStyle = BraceStyle.EndOfLine,
				AllowEventAddBlockInline = true,
				AllowEventRemoveBlockInline = true,
				StatementBraceStyle = BraceStyle.EndOfLine,
	
				ElseNewLinePlacement = NewLinePlacement.SameLine,
				ElseIfNewLinePlacement = NewLinePlacement.SameLine,
				CatchNewLinePlacement = NewLinePlacement.SameLine,
				FinallyNewLinePlacement = NewLinePlacement.SameLine,
				WhileNewLinePlacement = NewLinePlacement.SameLine,
				ArrayInitializerWrapping = Wrapping.WrapIfTooLong,
				ArrayInitializerBraceStyle = BraceStyle.EndOfLine,
	
				SpaceBeforeMethodCallParentheses = false,
				SpaceBeforeMethodDeclarationParentheses = false,
				SpaceBeforeConstructorDeclarationParentheses = false,
				SpaceBeforeDelegateDeclarationParentheses = false,
				SpaceBeforeIndexerDeclarationBracket = false,
				SpaceAfterMethodCallParameterComma = true,
				SpaceAfterConstructorDeclarationParameterComma = true,
				NewLineBeforeConstructorInitializerColon = NewLinePlacement.NewLine,
				NewLineAfterConstructorInitializerColon = NewLinePlacement.SameLine,
				
				SpaceBeforeNewParentheses = false,
				SpacesWithinNewParentheses = false,
				SpacesBetweenEmptyNewParentheses = false,
				SpaceBeforeNewParameterComma = false,
				SpaceAfterNewParameterComma = true,
				
				SpaceBeforeIfParentheses = true,
				SpaceBeforeWhileParentheses = true,
				SpaceBeforeForParentheses = true,
				SpaceBeforeForeachParentheses = true,
				SpaceBeforeCatchParentheses = true,
				SpaceBeforeSwitchParentheses = true,
				SpaceBeforeLockParentheses = true,
				SpaceBeforeUsingParentheses = true,

				SpaceAroundAssignment = true,
				SpaceAroundLogicalOperator = true,
				SpaceAroundEqualityOperator = true,
				SpaceAroundRelationalOperator = true,
				SpaceAroundBitwiseOperator = true,
				SpaceAroundAdditiveOperator = true,
				SpaceAroundMultiplicativeOperator = true,
				SpaceAroundShiftOperator = true,
				SpaceAroundNullCoalescingOperator = true,
				SpacesWithinParentheses = false,
				SpaceWithinMethodCallParentheses = false,
				SpaceWithinMethodDeclarationParentheses = false,
				SpacesWithinIfParentheses = false,
				SpacesWithinWhileParentheses = false,
				SpacesWithinForParentheses = false,
				SpacesWithinForeachParentheses = false,
				SpacesWithinCatchParentheses = false,
				SpacesWithinSwitchParentheses = false,
				SpacesWithinLockParentheses = false,
				SpacesWithinUsingParentheses = false,
				SpacesWithinCastParentheses = false,
				SpacesWithinSizeOfParentheses = false,
				SpacesWithinTypeOfParentheses = false,
				SpacesWithinCheckedExpressionParantheses = false,
				SpaceBeforeConditionalOperatorCondition = true,
				SpaceAfterConditionalOperatorCondition = true,
				SpaceBeforeConditionalOperatorSeparator = true,
				SpaceAfterConditionalOperatorSeparator = true,
				SpaceBeforeArrayDeclarationBrackets = false,

				SpacesWithinBrackets = false,
				SpacesBeforeBrackets = false,
				SpaceBeforeBracketComma = false,
				SpaceAfterBracketComma = true,
						
				SpaceBeforeForSemicolon = false,
				SpaceAfterForSemicolon = true,
				SpaceAfterTypecast = false,
				
				AlignEmbeddedStatements = true,
				SimplePropertyFormatting = PropertyFormatting.AllowOneLine,
				AutoPropertyFormatting = PropertyFormatting.AllowOneLine,
				EmptyLineFormatting = EmptyLineFormatting.DoNotIndent,
				SpaceBeforeMethodDeclarationParameterComma = false,
				SpaceAfterMethodDeclarationParameterComma = true,
				SpaceAfterDelegateDeclarationParameterComma = true,
				SpaceBeforeFieldDeclarationComma = false,
				SpaceAfterFieldDeclarationComma = true,
				SpaceBeforeLocalVariableDeclarationComma = false,
				SpaceAfterLocalVariableDeclarationComma = true,
				
				SpaceWithinIndexerDeclarationBracket = false,
				SpaceBeforeIndexerDeclarationParameterComma = false,
				SpaceInNamedArgumentAfterDoubleColon = true,
			
				SpaceAfterIndexerDeclarationParameterComma = true,
				RemoveEndOfLineWhiteSpace = true,
				
				MinimumBlankLinesBeforeUsings = 0,
				MinimumBlankLinesAfterUsings = 1,

				MinimumBlankLinesBeforeFirstDeclaration = 0,
				MinimumBlankLinesBetweenTypes = 1,
				MinimumBlankLinesBetweenFields = 0,
				MinimumBlankLinesBetweenEventFields = 0,
				MinimumBlankLinesBetweenMembers = 1,
				MinimumBlankLinesAroundRegion = 1,
				MinimumBlankLinesInsideRegion = 1,
	
				KeepCommentsAtFirstColumn = true,
				ChainedMethodCallWrapping = Wrapping.DoNotChange,
				MethodCallArgumentWrapping = Wrapping.DoNotChange,
				NewLineAferMethodCallOpenParentheses = NewLinePlacement.DoNotCare,
				MethodCallClosingParenthesesOnNewLine = NewLinePlacement.DoNotCare,

				IndexerArgumentWrapping = Wrapping.DoNotChange,
				NewLineAferIndexerOpenBracket = NewLinePlacement.DoNotCare,
				IndexerClosingBracketOnNewLine = NewLinePlacement.DoNotCare,

				NewLineBeforeNewQueryClause = NewLinePlacement.NewLine
			};
		}

		/// <summary>
		/// Creates allman indent style CSharpFormatting options used in Visual Studio.
		/// </summary>
		public static CSharpFormattingOptions CreateAllman()
		{
			var baseOptions = CreateKRStyle();
			baseOptions.AnonymousMethodBraceStyle = BraceStyle.NextLine;
			baseOptions.PropertyBraceStyle = BraceStyle.NextLine;
			baseOptions.PropertyGetBraceStyle = BraceStyle.NextLine;
			baseOptions.PropertySetBraceStyle = BraceStyle.NextLine;

			baseOptions.EventBraceStyle = BraceStyle.NextLine;
			baseOptions.EventAddBraceStyle = BraceStyle.NextLine;
			baseOptions.EventRemoveBraceStyle = BraceStyle.NextLine;
			baseOptions.StatementBraceStyle = BraceStyle.NextLine;
			baseOptions.ArrayInitializerBraceStyle = BraceStyle.NextLine;

			baseOptions.CatchNewLinePlacement = NewLinePlacement.NewLine;
			baseOptions.ElseNewLinePlacement = NewLinePlacement.NewLine;
			baseOptions.ElseIfNewLinePlacement = NewLinePlacement.SameLine;

			baseOptions.FinallyNewLinePlacement = NewLinePlacement.NewLine;
			baseOptions.WhileNewLinePlacement = NewLinePlacement.DoNotCare;
			baseOptions.ArrayInitializerWrapping = Wrapping.DoNotChange;
			baseOptions.IndentBlocksInsideExpressions = true;

			return baseOptions;
		}

		/// <summary>
		/// The Whitesmiths style, also called Wishart style to a lesser extent, is less common today than the previous three. It was originally used in the documentation for the first commercial C compiler, the Whitesmiths Compiler.
		/// </summary>
		public static CSharpFormattingOptions CreateWhitesmiths()
		{
			var baseOptions = CreateKRStyle();
				
			baseOptions.NamespaceBraceStyle = BraceStyle.NextLineShifted;
			baseOptions.ClassBraceStyle = BraceStyle.NextLineShifted;
			baseOptions.InterfaceBraceStyle = BraceStyle.NextLineShifted;
			baseOptions.StructBraceStyle = BraceStyle.NextLineShifted;
			baseOptions.EnumBraceStyle = BraceStyle.NextLineShifted;
			baseOptions.MethodBraceStyle = BraceStyle.NextLineShifted;
			baseOptions.ConstructorBraceStyle = BraceStyle.NextLineShifted;
			baseOptions.DestructorBraceStyle = BraceStyle.NextLineShifted;
			baseOptions.AnonymousMethodBraceStyle = BraceStyle.NextLineShifted;
			baseOptions.PropertyBraceStyle = BraceStyle.NextLineShifted;
			baseOptions.PropertyGetBraceStyle = BraceStyle.NextLineShifted;
			baseOptions.PropertySetBraceStyle = BraceStyle.NextLineShifted;
	
			baseOptions.EventBraceStyle = BraceStyle.NextLineShifted;
			baseOptions.EventAddBraceStyle = BraceStyle.NextLineShifted;
			baseOptions.EventRemoveBraceStyle = BraceStyle.NextLineShifted;
			baseOptions.StatementBraceStyle = BraceStyle.NextLineShifted;
			baseOptions.IndentBlocksInsideExpressions = true;
			return baseOptions;
		}

		/// <summary>
		/// Like the Allman and Whitesmiths styles, GNU style puts braces on a line by themselves, indented by 2 spaces,
		/// except when opening a function definition, where they are not indented.
		/// In either case, the contained code is indented by 2 spaces from the braces.
		/// Popularised by Richard Stallman, the layout may be influenced by his background of writing Lisp code.
		/// In Lisp the equivalent to a block (a progn) 
		/// is a first class data entity and giving it its own indent level helps to emphasize that,
		/// whereas in C a block is just syntax.
		/// Although not directly related to indentation, GNU coding style also includes a space before the bracketed 
		/// list of arguments to a function.
		/// </summary>
		public static CSharpFormattingOptions CreateGNU()
		{
			var baseOptions = CreateAllman();
			baseOptions.StatementBraceStyle = BraceStyle.NextLineShifted2;
			return baseOptions;
		}
	}
}

