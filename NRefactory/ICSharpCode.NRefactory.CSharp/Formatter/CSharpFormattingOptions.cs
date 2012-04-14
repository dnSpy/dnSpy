// 
// CSharpFormattingOptions.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
//  
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Reflection;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp
{
	public enum BraceStyle
	{
		DoNotChange,
		EndOfLine,
		EndOfLineWithoutSpace,
		NextLine,
		NextLineShifted,
		NextLineShifted2,
		BannerStyle
	}

	public enum BraceForcement
	{
		DoNotChange,
		RemoveBraces,
		AddBraces
	}

	public enum PropertyFormatting
	{
		AllowOneLine,
		ForceOneLine,
		ForceNewLine
	}

	public enum Wrapping {
		DoNotWrap,
		WrapAlways,
		WrapIfTooLong
	}

	public class CSharpFormattingOptions
	{
		public string Name {
			get;
			set;
		}

		public bool IsBuiltIn {
			get;
			set;
		}

		public CSharpFormattingOptions Clone ()
		{
			return (CSharpFormattingOptions)MemberwiseClone ();
		}

		#region Indentation
		public bool IndentNamespaceBody { // tested
			get;
			set;
		}

		public bool IndentClassBody { // tested
			get;
			set;
		}

		public bool IndentInterfaceBody { // tested
			get;
			set;
		}

		public bool IndentStructBody { // tested
			get;
			set;
		}

		public bool IndentEnumBody { // tested
			get;
			set;
		}

		public bool IndentMethodBody { // tested
			get;
			set;
		}

		public bool IndentPropertyBody { // tested
			get;
			set;
		}

		public bool IndentEventBody { // tested
			get;
			set;
		}

		public bool IndentBlocks { // tested
			get;
			set;
		}

		public bool IndentSwitchBody { // tested
			get;
			set;
		}

		public bool IndentCaseBody { // tested
			get;
			set;
		}

		public bool IndentBreakStatements { // tested
			get;
			set;
		}

		public bool AlignEmbeddedUsingStatements { // tested
			get;
			set;
		}

		public bool AlignEmbeddedIfStatements { // tested
			get;
			set;
		}

		public PropertyFormatting PropertyFormatting { // tested
			get;
			set;
		}

		#endregion
		
		#region Braces
		public BraceStyle NamespaceBraceStyle { // tested
			get;
			set;
		}

		public BraceStyle ClassBraceStyle { // tested
			get;
			set;
		}

		public BraceStyle InterfaceBraceStyle { // tested
			get;
			set;
		}

		public BraceStyle StructBraceStyle { // tested
			get;
			set;
		}

		public BraceStyle EnumBraceStyle { // tested
			get;
			set;
		}

		public BraceStyle MethodBraceStyle { // tested
			get;
			set;
		}

		public BraceStyle AnonymousMethodBraceStyle {
			get;
			set;
		}

		public BraceStyle ConstructorBraceStyle {  // tested
			get;
			set;
		}

		public BraceStyle DestructorBraceStyle { // tested
			get;
			set;
		}

		public BraceStyle PropertyBraceStyle { // tested
			get;
			set;
		}

		public BraceStyle PropertyGetBraceStyle { // tested
			get;
			set;
		}

		public BraceStyle PropertySetBraceStyle { // tested
			get;
			set;
		}

		public bool AllowPropertyGetBlockInline { // tested
			get;
			set;
		}

		public bool AllowPropertySetBlockInline { // tested
			get;
			set;
		}

		public BraceStyle EventBraceStyle { // tested
			get;
			set;
		}

		public BraceStyle EventAddBraceStyle { // tested
			get;
			set;
		}

		public BraceStyle EventRemoveBraceStyle { // tested
			get;
			set;
		}

		public bool AllowEventAddBlockInline { // tested
			get;
			set;
		}

		public bool AllowEventRemoveBlockInline { // tested
			get;
			set;
		}

		public BraceStyle StatementBraceStyle { // tested
			get;
			set;
		}

		public bool AllowIfBlockInline {
			get;
			set;
		}

		#endregion
		
		#region Force Braces
		public BraceForcement IfElseBraceForcement { // tested
			get;
			set;
		}

		public BraceForcement ForBraceForcement { // tested
			get;
			set;
		}

		public BraceForcement ForEachBraceForcement { // tested
			get;
			set;
		}

		public BraceForcement WhileBraceForcement { // tested
			get;
			set;
		}

		public BraceForcement UsingBraceForcement { // tested
			get;
			set;
		}

		public BraceForcement FixedBraceForcement { // tested
			get;
			set;
		}

		#endregion
		
		#region NewLines
		public bool PlaceElseOnNewLine { // tested
			get;
			set;
		}

		public bool PlaceElseIfOnNewLine { // tested
			get;
			set;
		}

		public bool PlaceCatchOnNewLine { // tested
			get;
			set;
		}

		public bool PlaceFinallyOnNewLine { // tested
			get;
			set;
		}

		public bool PlaceWhileOnNewLine { // tested
			get;
			set;
		}
		#endregion
		
		#region Spaces
		// Methods
		public bool SpaceBeforeMethodDeclarationParentheses { // tested
			get;
			set;
		}

		public bool SpaceBetweenEmptyMethodDeclarationParentheses {
			get;
			set;
		}

		public bool SpaceBeforeMethodDeclarationParameterComma { // tested
			get;
			set;
		}

		public bool SpaceAfterMethodDeclarationParameterComma { // tested
			get;
			set;
		}

		public bool SpaceWithinMethodDeclarationParentheses { // tested
			get;
			set;
		}
		
		// Method calls
		public bool SpaceBeforeMethodCallParentheses { // tested
			get;
			set;
		}

		public bool SpaceBetweenEmptyMethodCallParentheses { // tested
			get;
			set;
		}

		public bool SpaceBeforeMethodCallParameterComma { // tested
			get;
			set;
		}

		public bool SpaceAfterMethodCallParameterComma { // tested
			get;
			set;
		}

		public bool SpaceWithinMethodCallParentheses { // tested
			get;
			set;
		}
		
		// fields
		
		public bool SpaceBeforeFieldDeclarationComma { // tested
			get;
			set;
		}

		public bool SpaceAfterFieldDeclarationComma { // tested
			get;
			set;
		}
		
		// local variables
		
		public bool SpaceBeforeLocalVariableDeclarationComma { // tested
			get;
			set;
		}

		public bool SpaceAfterLocalVariableDeclarationComma { // tested
			get;
			set;
		}
		
		// constructors
		
		public bool SpaceBeforeConstructorDeclarationParentheses { // tested
			get;
			set;
		}

		public bool SpaceBetweenEmptyConstructorDeclarationParentheses { // tested
			get;
			set;
		}

		public bool SpaceBeforeConstructorDeclarationParameterComma { // tested
			get;
			set;
		}

		public bool SpaceAfterConstructorDeclarationParameterComma { // tested
			get;
			set;
		}

		public bool SpaceWithinConstructorDeclarationParentheses { // tested
			get;
			set;
		}
		
		// indexer
		public bool SpaceBeforeIndexerDeclarationBracket { // tested
			get;
			set;
		}

		public bool SpaceWithinIndexerDeclarationBracket { // tested
			get;
			set;
		}

		public bool SpaceBeforeIndexerDeclarationParameterComma {
			get;
			set;
		}

		public bool SpaceAfterIndexerDeclarationParameterComma {
			get;
			set;
		}
		
		// delegates
		
		public bool SpaceBeforeDelegateDeclarationParentheses {
			get;
			set;
		}

		public bool SpaceBetweenEmptyDelegateDeclarationParentheses {
			get;
			set;
		}

		public bool SpaceBeforeDelegateDeclarationParameterComma {
			get;
			set;
		}

		public bool SpaceAfterDelegateDeclarationParameterComma {
			get;
			set;
		}

		public bool SpaceWithinDelegateDeclarationParentheses {
			get;
			set;
		}

		public bool SpaceBeforeNewParentheses { // tested
			get;
			set;
		}

		public bool SpaceBeforeIfParentheses { // tested
			get;
			set;
		}

		public bool SpaceBeforeWhileParentheses { // tested
			get;
			set;
		}

		public bool SpaceBeforeForParentheses { // tested
			get;
			set;
		}

		public bool SpaceBeforeForeachParentheses { // tested
			get;
			set;
		}

		public bool SpaceBeforeCatchParentheses { // tested
			get;
			set;
		}

		public bool SpaceBeforeSwitchParentheses { // tested
			get;
			set;
		}

		public bool SpaceBeforeLockParentheses { // tested
			get;
			set;
		}

		public bool SpaceBeforeUsingParentheses { // tested
			get;
			set;
		}

		public bool SpaceAroundAssignment { // tested
			get;
			set;
		}

		public bool SpaceAroundLogicalOperator { // tested
			get;
			set;
		}

		public bool SpaceAroundEqualityOperator { // tested
			get;
			set;
		}

		public bool SpaceAroundRelationalOperator { // tested
			get;
			set;
		}

		public bool SpaceAroundBitwiseOperator { // tested
			get;
			set;
		}

		public bool SpaceAroundAdditiveOperator { // tested
			get;
			set;
		}

		public bool SpaceAroundMultiplicativeOperator { // tested
			get;
			set;
		}

		public bool SpaceAroundShiftOperator { // tested
			get;
			set;
		}

		public bool SpaceAroundNullCoalescingOperator {
			get;
			set;
		}

		public bool SpacesWithinParentheses { // tested
			get;
			set;
		}

		public bool SpacesWithinIfParentheses { // tested
			get;
			set;
		}

		public bool SpacesWithinWhileParentheses { // tested
			get;
			set;
		}

		public bool SpacesWithinForParentheses { // tested
			get;
			set;
		}

		public bool SpacesWithinForeachParentheses { // tested
			get;
			set;
		}

		public bool SpacesWithinCatchParentheses { // tested
			get;
			set;
		}

		public bool SpacesWithinSwitchParentheses { // tested
			get;
			set;
		}

		public bool SpacesWithinLockParentheses { // tested
			get;
			set;
		}

		public bool SpacesWithinUsingParentheses { // tested
			get;
			set;
		}

		public bool SpacesWithinCastParentheses { // tested
			get;
			set;
		}

		public bool SpacesWithinSizeOfParentheses { // tested
			get;
			set;
		}

		public bool SpaceBeforeSizeOfParentheses { // tested
			get;
			set;
		}

		public bool SpacesWithinTypeOfParentheses { // tested
			get;
			set;
		}

		public bool SpacesWithinNewParentheses { // tested
			get;
			set;
		}

		public bool SpacesBetweenEmptyNewParentheses { // tested
			get;
			set;
		}

		public bool SpaceBeforeNewParameterComma { // tested
			get;
			set;
		}

		public bool SpaceAfterNewParameterComma { // tested
			get;
			set;
		}

		public bool SpaceBeforeTypeOfParentheses { // tested
			get;
			set;
		}

		public bool SpacesWithinCheckedExpressionParantheses { // tested
			get;
			set;
		}

		public bool SpaceBeforeConditionalOperatorCondition { // tested
			get;
			set;
		}

		public bool SpaceAfterConditionalOperatorCondition { // tested
			get;
			set;
		}

		public bool SpaceBeforeConditionalOperatorSeparator { // tested
			get;
			set;
		}

		public bool SpaceAfterConditionalOperatorSeparator { // tested
			get;
			set;
		}
		
		// brackets
		public bool SpacesWithinBrackets { // tested
			get;
			set;
		}

		public bool SpacesBeforeBrackets { // tested
			get;
			set;
		}

		public bool SpaceBeforeBracketComma { // tested
			get;
			set;
		}

		public bool SpaceAfterBracketComma { // tested
			get;
			set;
		}

		public bool SpaceBeforeForSemicolon { // tested
			get;
			set;
		}

		public bool SpaceAfterForSemicolon { // tested
			get;
			set;
		}

		public bool SpaceAfterTypecast { // tested
			get;
			set;
		}

		public bool SpaceBeforeArrayDeclarationBrackets { // tested
			get;
			set;
		}

		public bool SpaceInNamedArgumentAfterDoubleColon {
			get;
			set;
		}
		#endregion
		
		#region Blank Lines
		public int BlankLinesBeforeUsings {
			get;
			set;
		}

		public int BlankLinesAfterUsings {
			get;
			set;
		}

		public int BlankLinesBeforeFirstDeclaration {
			get;
			set;
		}

		public int BlankLinesBetweenTypes {
			get;
			set;
		}

		public int BlankLinesBetweenFields {
			get;
			set;
		}

		public int BlankLinesBetweenEventFields {
			get;
			set;
		}

		public int BlankLinesBetweenMembers {
			get;
			set;
		}

		#endregion


		#region Keep formatting
		public bool KeepCommentsAtFirstColumn {
			get;
			set;
		}
		#endregion

		#region Wrapping

		public Wrapping ArrayInitializerWrapping {
			get;
			set;
		}

		public BraceStyle ArrayInitializerBraceStyle {
			get;
			set;
		}

		#endregion

		internal CSharpFormattingOptions()
		{
		}

		/*public static CSharpFormattingOptions Load (FilePath selectedFile)
		{
			using (var stream = System.IO.File.OpenRead (selectedFile)) {
				return Load (stream);
			}
		}

		public static CSharpFormattingOptions Load (System.IO.Stream input)
		{
			CSharpFormattingOptions result = FormattingOptionsFactory.CreateMonoOptions ();
			result.Name = "noname";
			using (XmlTextReader reader = new XmlTextReader (input)) {
				while (reader.Read ()) {
					if (reader.NodeType == XmlNodeType.Element) {
						if (reader.LocalName == "Property") {
							var info = typeof(CSharpFormattingOptions).GetProperty (reader.GetAttribute ("name"));
							string valString = reader.GetAttribute ("value");
							object value;
							if (info.PropertyType == typeof(bool)) {
								value = Boolean.Parse (valString);
							} else if (info.PropertyType == typeof(int)) {
								value = Int32.Parse (valString);
							} else {
								value = Enum.Parse (info.PropertyType, valString);
							}
							info.SetValue (result, value, null);
						} else if (reader.LocalName == "FormattingProfile") {
							result.Name = reader.GetAttribute ("name");
						}
					} else if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "FormattingProfile") {
						//Console.WriteLine ("result:" + result.Name);
						return result;
					}
				}
			}
			return result;
		}

		public void Save (string fileName)
		{
			using (var writer = new XmlTextWriter (fileName, Encoding.Default)) {
				writer.Formatting = System.Xml.Formatting.Indented;
				writer.Indentation = 1;
				writer.IndentChar = '\t';
				writer.WriteStartElement ("FormattingProfile");
				writer.WriteAttributeString ("name", Name);
				foreach (PropertyInfo info in typeof (CSharpFormattingOptions).GetProperties ()) {
					if (info.GetCustomAttributes (false).Any (o => o.GetType () == typeof(ItemPropertyAttribute))) {
						writer.WriteStartElement ("Property");
						writer.WriteAttributeString ("name", info.Name);
						writer.WriteAttributeString ("value", info.GetValue (this, null).ToString ());
						writer.WriteEndElement ();
					}
				}
				writer.WriteEndElement ();
			}
		}

		public bool Equals (CSharpFormattingOptions other)
		{
			foreach (PropertyInfo info in typeof (CSharpFormattingOptions).GetProperties ()) {
				if (info.GetCustomAttributes (false).Any (o => o.GetType () == typeof(ItemPropertyAttribute))) {
					object val = info.GetValue (this, null);
					object otherVal = info.GetValue (other, null);
					if (val == null) {
						if (otherVal == null)
							continue;
						return false;
					}
					if (!val.Equals (otherVal)) {
						//Console.WriteLine ("!equal");
						return false;
					}
				}
			}
			//Console.WriteLine ("== equal");
			return true;
		}*/
	}
}
