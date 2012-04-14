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
				IndentSwitchBody = false,
				IndentCaseBody = true,
				IndentBreakStatements = true,
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
				AllowPropertyGetBlockInline = true,
				AllowPropertySetBlockInline = true,
	
				EventBraceStyle = BraceStyle.EndOfLine,
				EventAddBraceStyle = BraceStyle.EndOfLine,
				EventRemoveBraceStyle = BraceStyle.EndOfLine,
				AllowEventAddBlockInline = true,
				AllowEventRemoveBlockInline = true,
				StatementBraceStyle = BraceStyle.EndOfLine,
	
				PlaceElseOnNewLine = false,
				PlaceCatchOnNewLine = false,
				PlaceFinallyOnNewLine = false,
				PlaceWhileOnNewLine = false,
				ArrayInitializerWrapping = Wrapping.WrapIfTooLong,
				ArrayInitializerBraceStyle = BraceStyle.EndOfLine,
	
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
				
				AlignEmbeddedIfStatements = true,
				AlignEmbeddedUsingStatements = true,
				PropertyFormatting = PropertyFormatting.AllowOneLine,
				SpaceBeforeMethodDeclarationParameterComma = false,
				SpaceAfterMethodDeclarationParameterComma = true,
				SpaceBeforeFieldDeclarationComma = false,
				SpaceAfterFieldDeclarationComma = true,
				SpaceBeforeLocalVariableDeclarationComma = false,
				SpaceAfterLocalVariableDeclarationComma = true,
				
				SpaceBeforeIndexerDeclarationBracket = true,
				SpaceWithinIndexerDeclarationBracket = false,
				SpaceBeforeIndexerDeclarationParameterComma = false,
				SpaceInNamedArgumentAfterDoubleColon = true,
			
				SpaceAfterIndexerDeclarationParameterComma = true,
				
				BlankLinesBeforeUsings = 0,
				BlankLinesAfterUsings = 1,
				
				
				BlankLinesBeforeFirstDeclaration = 0,
				BlankLinesBetweenTypes = 1,
				BlankLinesBetweenFields = 0,
				BlankLinesBetweenEventFields = 0,
				BlankLinesBetweenMembers = 1,
	
				KeepCommentsAtFirstColumn = true,
			};
		}

		/// <summary>
		/// Creates sharp develop indent style CSharpFormatting options.
		/// </summary>
		public static CSharpFormattingOptions CreateSharpDevelop()
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
				AllowPropertyGetBlockInline = true,
				AllowPropertySetBlockInline = true,
	
				EventBraceStyle = BraceStyle.EndOfLine,
				EventAddBraceStyle = BraceStyle.EndOfLine,
				EventRemoveBraceStyle = BraceStyle.EndOfLine,
				AllowEventAddBlockInline = true,
				AllowEventRemoveBlockInline = true,
				StatementBraceStyle = BraceStyle.EndOfLine,
	
				PlaceElseOnNewLine = false,
				PlaceCatchOnNewLine = false,
				PlaceFinallyOnNewLine = false,
				PlaceWhileOnNewLine = false,
				ArrayInitializerWrapping = Wrapping.WrapIfTooLong,
				ArrayInitializerBraceStyle = BraceStyle.EndOfLine,
	
				SpaceBeforeMethodCallParentheses = false,
				SpaceBeforeMethodDeclarationParentheses = false,
				SpaceBeforeConstructorDeclarationParentheses = false,
				SpaceBeforeDelegateDeclarationParentheses = false,
				SpaceAfterMethodCallParameterComma = true,
				SpaceAfterConstructorDeclarationParameterComma = true,
				
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
	
				SpacesWithinBrackets = false,
				SpacesBeforeBrackets = true,
				SpaceBeforeBracketComma = false,
				SpaceAfterBracketComma = true,
						
				SpaceBeforeForSemicolon = false,
				SpaceAfterForSemicolon = true,
				SpaceAfterTypecast = false,
				
				AlignEmbeddedIfStatements = true,
				AlignEmbeddedUsingStatements = true,
				PropertyFormatting = PropertyFormatting.AllowOneLine,
				SpaceBeforeMethodDeclarationParameterComma = false,
				SpaceAfterMethodDeclarationParameterComma = true,
				SpaceBeforeFieldDeclarationComma = false,
				SpaceAfterFieldDeclarationComma = true,
				SpaceBeforeLocalVariableDeclarationComma = false,
				SpaceAfterLocalVariableDeclarationComma = true,
				
				SpaceBeforeIndexerDeclarationBracket = true,
				SpaceWithinIndexerDeclarationBracket = false,
				SpaceBeforeIndexerDeclarationParameterComma = false,
				SpaceInNamedArgumentAfterDoubleColon = true,
			
				SpaceAfterIndexerDeclarationParameterComma = true,
				
				BlankLinesBeforeUsings = 0,
				BlankLinesAfterUsings = 1,

				BlankLinesBeforeFirstDeclaration = 0,
				BlankLinesBetweenTypes = 1,
				BlankLinesBetweenFields = 0,
				BlankLinesBetweenEventFields = 0,
				BlankLinesBetweenMembers = 1,
	
				KeepCommentsAtFirstColumn = true,
			};
		}

		/// <summary>
		/// Creates allman indent style CSharpFormatting options used in Visual Studio.
		/// </summary>
		public static CSharpFormattingOptions CreateAllman()
		{
			var baseOptions = CreateSharpDevelop();
			baseOptions.AnonymousMethodBraceStyle = BraceStyle.EndOfLine;
			baseOptions.PropertyBraceStyle = BraceStyle.EndOfLine;
			baseOptions.PropertyGetBraceStyle = BraceStyle.EndOfLine;
			baseOptions.PropertySetBraceStyle = BraceStyle.EndOfLine;

			baseOptions.EventBraceStyle = BraceStyle.EndOfLine;
			baseOptions.EventAddBraceStyle = BraceStyle.EndOfLine;
			baseOptions.EventRemoveBraceStyle = BraceStyle.EndOfLine;
			baseOptions.StatementBraceStyle = BraceStyle.EndOfLine;
			baseOptions.ArrayInitializerBraceStyle = BraceStyle.EndOfLine;
			return baseOptions;
		}
	}
}

