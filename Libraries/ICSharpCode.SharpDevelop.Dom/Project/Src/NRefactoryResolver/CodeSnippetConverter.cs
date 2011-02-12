// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.PrettyPrinter;

namespace ICSharpCode.SharpDevelop.Dom.NRefactoryResolver
{
	/// <summary>
	/// Allows converting code snippets between C# and VB.
	/// This class isn't used by SharpDevelop itself (because it doesn't support projects).
	/// It works by creating a dummy project for the file to convert with a set of default references.
	/// </summary>
	public class CodeSnippetConverter
	{
		/// <summary>
		/// Project-wide imports to add to all files when converting VB to C#.
		/// </summary>
		public IList<string> DefaultImportsToAdd = new List<string> { "Microsoft.VisualBasic", "System", "System.Collections", "System.Collections.Generic", "System.Data", "System.Diagnostics" };
		
		/// <summary>
		/// Imports to remove (because they will become project-wide imports) when converting C# to VB.
		/// </summary>
		public IList<string> DefaultImportsToRemove = new List<string> { "Microsoft.VisualBasic", "System" };
		
		/// <summary>
		/// References project contents, for resolving type references during the conversion.
		/// </summary>
		public IList<IProjectContent> ReferencedContents = new List<IProjectContent>();
		
		DefaultProjectContent project;
		List<ISpecial> specials;
		CompilationUnit compilationUnit;
		ParseInformation parseInfo;
		bool wasExpression;
		
		#region Parsing
		INode Parse(SupportedLanguage sourceLanguage, string sourceCode, out string error)
		{
			project = new DefaultProjectContent();
			project.ReferencedContents.AddRange(ReferencedContents);
			if (sourceLanguage == SupportedLanguage.VBNet) {
				project.Language = LanguageProperties.VBNet;
				project.DefaultImports = new DefaultUsing(project);
				project.DefaultImports.Usings.AddRange(DefaultImportsToAdd);
			} else {
				project.Language = LanguageProperties.CSharp;
			}
			SnippetParser parser = new SnippetParser(sourceLanguage);
			INode result = parser.Parse(sourceCode);
			error = parser.Errors.ErrorOutput;
			specials = parser.Specials;
			if (parser.Errors.Count != 0)
				return null;
			
			wasExpression = parser.SnippetType == SnippetType.Expression;
			if (wasExpression) {
				// Special case 'Expression': expressions may be replaced with other statements in the AST by the ConvertVisitor,
				// but we need to return a 'stable' node so that the correct transformed AST is returned.
				// Thus, we wrap any expressions into a statement block.
				result = MakeBlockFromExpression((Expression)result);
			}
			
			// now create a dummy compilation unit around the snippet result
			switch (parser.SnippetType) {
				case SnippetType.CompilationUnit:
					compilationUnit = (CompilationUnit)result;
					break;
				case SnippetType.Expression:
				case SnippetType.Statements:
					compilationUnit = MakeCompilationUnitFromTypeMembers(
						MakeMethodFromBlock(
							(BlockStatement)result
						));
					break;
				case SnippetType.TypeMembers:
					compilationUnit = MakeCompilationUnitFromTypeMembers(result.Children);
					break;
				default:
					throw new NotSupportedException("Unknown snippet type: " + parser.SnippetType);
			}
			
			// convert NRefactory CU in DOM CU
			NRefactoryASTConvertVisitor visitor = new NRefactoryASTConvertVisitor(project, sourceLanguage);
			visitor.VisitCompilationUnit(compilationUnit, null);
			visitor.Cu.FileName = sourceLanguage == SupportedLanguage.CSharp ? "a.cs" : "a.vb";
			
			// and register the compilation unit in the DOM
			foreach (IClass c in visitor.Cu.Classes) {
				project.AddClassToNamespaceList(c);
			}
			parseInfo = new ParseInformation(visitor.Cu);
			
			return result;
		}
		
		/// <summary>
		/// Unpacks the expression from a statement block; if it was wrapped earlier.
		/// </summary>
		INode UnpackExpression(INode node)
		{
			if (wasExpression) {
				BlockStatement block = node as BlockStatement;
				if (block != null && block.Children.Count == 1) {
					ExpressionStatement es = block.Children[0] as ExpressionStatement;
					if (es != null)
						return es.Expression;
				}
			}
			return node;
		}
		
		BlockStatement MakeBlockFromExpression(Expression expr)
		{
			return new BlockStatement {
				Children = {
					new ExpressionStatement(expr)
				},
				StartLocation = expr.StartLocation,
				EndLocation = expr.EndLocation
			};
		}
		
		INode[] MakeMethodFromBlock(BlockStatement block)
		{
			return new INode[] {
				new MethodDeclaration {
					Name = "DummyMethodForConversion",
					Body = block,
					StartLocation = block.StartLocation,
					EndLocation = block.EndLocation
				}
			};
		}
		
		CompilationUnit MakeCompilationUnitFromTypeMembers(IList<INode> members)
		{
			TypeDeclaration type = new TypeDeclaration(Modifiers.None, null) {
				Name = "DummyTypeForConversion",
				StartLocation = members[0].StartLocation,
				EndLocation = GetEndLocation(members[members.Count - 1])
			};
			type.Children.AddRange(members);
			return new CompilationUnit {
				Children = {
					type
				}
			};
		}
		
		Location GetEndLocation(INode node)
		{
			// workaround: MethodDeclaration.EndLocation is the end of the method header,
			// but for the end of the dummy class we need the body end
			MethodDeclaration method = node as MethodDeclaration;
			if (method != null && !method.Body.IsNull)
				return method.Body.EndLocation;
			else
				return node.EndLocation;
		}
		#endregion
		
		public string CSharpToVB(string input, out string errors)
		{
			INode node = Parse(SupportedLanguage.CSharp, input, out errors);
			if (node == null)
				return null;
			// apply conversion logic:
			compilationUnit.AcceptVisitor(
				new CSharpToVBNetConvertVisitor(project, parseInfo) {
					DefaultImportsToRemove = DefaultImportsToRemove,
				},
				null);
			PreprocessingDirective.CSharpToVB(specials);
			return CreateCode(UnpackExpression(node), new VBNetOutputVisitor());
		}
		
		public string VBToCSharp(string input, out string errors)
		{
			INode node = Parse(SupportedLanguage.VBNet, input, out errors);
			if (node == null)
				return null;
			// apply conversion logic:
			compilationUnit.AcceptVisitor(
				new VBNetToCSharpConvertVisitor(project, parseInfo),
				null);
			PreprocessingDirective.VBToCSharp(specials);
			return CreateCode(UnpackExpression(node), new CSharpOutputVisitor());
		}
		
		string CreateCode(INode node, IOutputAstVisitor outputVisitor)
		{
			using (SpecialNodesInserter.Install(specials, outputVisitor)) {
				node.AcceptVisitor(outputVisitor, null);
			}
			return outputVisitor.Text;
		}
	}
}
