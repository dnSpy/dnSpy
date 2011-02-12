// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.PrettyPrinter;

namespace ICSharpCode.SharpDevelop.Dom.Refactoring
{
	public class CSharpCodeGenerator : NRefactoryCodeGenerator
	{
		internal static readonly CSharpCodeGenerator Instance = new CSharpCodeGenerator();
		
		public override IOutputAstVisitor CreateOutputVisitor()
		{
			CSharpOutputVisitor v = new CSharpOutputVisitor();
			PrettyPrintOptions pOpt = v.Options;
			
			BraceStyle braceStyle;
			if (this.Options.BracesOnSameLine) {
				braceStyle = BraceStyle.EndOfLine;
			} else {
				braceStyle = BraceStyle.NextLine;
			}
			pOpt.StatementBraceStyle = braceStyle;
			pOpt.EventAddBraceStyle = braceStyle;
			pOpt.EventRemoveBraceStyle = braceStyle;
			pOpt.PropertyBraceStyle = braceStyle;
			pOpt.PropertyGetBraceStyle = braceStyle;
			pOpt.PropertySetBraceStyle = braceStyle;
			
			pOpt.IndentationChar = this.Options.IndentString[0];
			pOpt.IndentSize = this.Options.IndentString.Length;
			pOpt.TabSize = this.Options.IndentString.Length;
			
			return v;
		}
		
		/// <summary>
		/// Ensure that code is inserted correctly in {} code blocks - SD2-1180
		/// </summary>
		public override void InsertCodeAtEnd(DomRegion region, IRefactoringDocument document, params AbstractNode[] nodes)
		{
			string beginLineIndentation = GetIndentation(document, region.BeginLine);
			int insertionLine = region.EndLine - 1;
			
			IRefactoringDocumentLine endLine = document.GetLine(region.EndLine);
			string endLineText = endLine.Text;
			int originalPos = region.EndColumn - 2; // -1 for column coordinate => offset, -1 because EndColumn is after the '}'
			int pos = originalPos;
			if (pos < 0 || pos >= endLineText.Length || endLineText[pos] != '}') {
				LoggingService.Warn("CSharpCodeGenerator.InsertCodeAtEnd: position is invalid (not pointing to '}')"
				                    + " endLineText=" + endLineText + ", pos=" + pos);
			} else {
				for (pos--; pos >= 0; pos--) {
					if (!char.IsWhiteSpace(endLineText[pos])) {
						// range before '}' is not empty: we cannot simply insert in the line before the '}', so
						// 
						pos++; // set pos to first whitespace character / the '{' character
						if (pos < originalPos) {
							// remove whitespace between last non-white character and the '}'
							document.Remove(endLine.Offset + pos, originalPos - pos);
						}
						// insert newline and same indentation as used in beginLine before the '}'
						document.Insert(endLine.Offset + pos, Environment.NewLine + beginLineIndentation);
						insertionLine++;
						
						pos = region.BeginColumn - 1;
						if (region.BeginLine == region.EndLine && pos >= 1 && pos < endLineText.Length) {
							// The whole block was in on a single line, e.g. "get { return field; }".
							// Insert an additional newline after the '{'.
							
							originalPos = pos = endLineText.IndexOf('{', pos);
							if (pos >= 0 && pos < region.EndColumn - 1) {
								// find next non-whitespace after originalPos
								originalPos++; // point to insertion position for newline after {
								for (pos++; pos < endLineText.Length; pos++) {
									if (!char.IsWhiteSpace(endLineText[pos])) {
										// remove all between originalPos and pos
										if (originalPos < pos) {
											document.Remove(endLine.Offset + originalPos, pos - originalPos);
										}
										document.Insert(endLine.Offset + originalPos, Environment.NewLine + beginLineIndentation + '\t');
										insertionLine++;
										break;
									}
								}
							}
						}
						break;
					}
				}
			}
			InsertCodeAfter(insertionLine, document, beginLineIndentation + this.Options.IndentString, nodes);
		}
		
		public override PropertyDeclaration CreateProperty(IField field, bool createGetter, bool createSetter)
		{
			string propertyName = GetPropertyName(field.Name);
			if (propertyName == field.Name && GetParameterName(propertyName) != propertyName) {
				string newName = GetParameterName(propertyName);
				if (HostCallback.RenameMember(field, newName)) {
					field = new DefaultField(field.ReturnType, newName,
					                         field.Modifiers, field.Region, field.DeclaringType);
				}
			}
			return base.CreateProperty(field, createGetter, createSetter);
		}
	}
}
