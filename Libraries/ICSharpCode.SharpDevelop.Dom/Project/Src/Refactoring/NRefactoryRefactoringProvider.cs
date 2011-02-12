// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.PrettyPrinter;
using NR = ICSharpCode.NRefactory;

namespace ICSharpCode.SharpDevelop.Dom.Refactoring
{
	public class NRefactoryRefactoringProvider : RefactoringProvider
	{
		public static readonly NRefactoryRefactoringProvider NRefactoryCSharpProviderInstance = new NRefactoryRefactoringProvider(NR.SupportedLanguage.CSharp);
		public static readonly NRefactoryRefactoringProvider NRefactoryVBNetProviderInstance = new NRefactoryRefactoringProvider(NR.SupportedLanguage.VBNet);
		
		NR.SupportedLanguage language;
		
		private NRefactoryRefactoringProvider(NR.SupportedLanguage language)
		{
			this.language = language;
		}
		
		public override bool IsEnabledForFile(string fileName)
		{
			string extension = Path.GetExtension(fileName);
			if (extension.Equals(".cs", StringComparison.OrdinalIgnoreCase))
				return language == NR.SupportedLanguage.CSharp;
			else if (extension.Equals(".vb", StringComparison.OrdinalIgnoreCase))
				return language == NR.SupportedLanguage.VBNet;
			else
				return false;
		}
		
		static void ShowSourceCodeErrors(IDomProgressMonitor progressMonitor, string errors)
		{
			if (progressMonitor != null)
				progressMonitor.ShowingDialog = true;
			HostCallback.ShowMessage("${res:SharpDevelop.Refactoring.CannotPerformOperationBecauseOfSyntaxErrors}\n" + errors);
			if (progressMonitor != null)
				progressMonitor.ShowingDialog = false;
		}
		
		NR.IParser ParseFile(IDomProgressMonitor progressMonitor, string fileContent)
		{
			NR.IParser parser = NR.ParserFactory.CreateParser(language, new StringReader(fileContent));
			parser.Parse();
			if (parser.Errors.Count > 0) {
				ShowSourceCodeErrors(progressMonitor, parser.Errors.ErrorOutput);
				parser.Dispose();
				return null;
			} else {
				return parser;
			}
		}
		
		IOutputAstVisitor GetOutputVisitor() {
			switch (language) {
				case NR.SupportedLanguage.CSharp:
					return new CSharpOutputVisitor();
				case NR.SupportedLanguage.VBNet:
					return new VBNetOutputVisitor();
				default:
					throw new NotSupportedException();
			}
		}
		
		string CommentToken {
			get {
				switch (language) {
					case NR.SupportedLanguage.CSharp:
						return "//";
					case NR.SupportedLanguage.VBNet:
						return "'";
					default:
						throw new NotSupportedException();
				}
			}
		}

		#region ExtractInterface
		public override bool SupportsExtractInterface {
			get {
				return true;
			}
		}
		
		public override string GenerateInterfaceForClass(string newInterfaceName, string existingCode, IList<IMember> membersToKeep, IClass sourceClass, bool preserveComments)
		{
			Modifiers modifiers = CodeGenerator.ConvertModifier(sourceClass.Modifiers, new ClassFinder(membersToKeep[0]));
			// keep only visibility modifiers and 'unsafe' modifier
			// -> remove abstract,sealed,static
			modifiers &= Modifiers.Visibility | Modifiers.Unsafe;
			
			TypeDeclaration interfaceDef = new TypeDeclaration(modifiers, new List<AttributeSection>());
			interfaceDef.Name = newInterfaceName;
			interfaceDef.Type = NR.Ast.ClassType.Interface;
			interfaceDef.Templates = CodeGenerator.ConvertTemplates(sourceClass.TypeParameters, new ClassFinder(membersToKeep[0]));
			
			foreach (IMember member in membersToKeep) {
				AttributedNode an = CodeGenerator.ConvertMember(member, new ClassFinder(member));
				INode node = null;
				if (an is MethodDeclaration) {
					MethodDeclaration m = an as MethodDeclaration;
					m.Body = BlockStatement.Null;
					m.Modifier = Modifiers.None;
					node = m;
				} else {
					if (an is PropertyDeclaration) {
						PropertyDeclaration p = an as PropertyDeclaration;
						p.GetRegion.Block = BlockStatement.Null;
						p.SetRegion.Block= BlockStatement.Null;
						p.Modifier = Modifiers.None;
						node = p;
					} else {
						if (an is EventDeclaration) {
							EventDeclaration e = an as EventDeclaration;
							e.Modifier = Modifiers.None;
							node = e;
						}
					}
				}
				
				if (node == null)
					throw new NotSupportedException();
				
				interfaceDef.AddChild(node);
			}
			
			IOutputAstVisitor printer = this.GetOutputVisitor();
			
			interfaceDef.AcceptVisitor(printer, null);
			
			string codeForNewInterface = printer.Text;

			// wrap the new code in the same comments/usings/namespace as the the original class file.
			string newFileContent = CreateNewFileLikeExisting(existingCode, codeForNewInterface);

			return newFileContent;
		}
		
		class AddTypeToBaseTypesVisitor : NR.Visitors.AbstractAstVisitor
		{
			IClass target, newBaseType;
			
			public AddTypeToBaseTypesVisitor(IClass target, IClass newBaseType)
			{
				this.target = target;
				this.newBaseType = newBaseType;
			}
			
			public override object VisitTypeDeclaration(TypeDeclaration typeDeclaration, object data)
			{
				// test the Type string property explicitly (rather than .BaseTypes.Contains())
				// to ensure that a matching type name is enough to prevent adding a second
				// reference.
				
				if (typeDeclaration.Name != target.Name)
					return base.VisitTypeDeclaration(typeDeclaration, data);
				
				bool exists = false;
				foreach(TypeReference type in typeDeclaration.BaseTypes) {
					if (type.Type == this.newBaseType.Name) {
						exists = true;
						break;
					}
				}
				if (!exists) {
					typeDeclaration.BaseTypes.Add(new TypeReference(newBaseType.Name, newBaseType.TypeParameters.Select(p => new TypeReference(p.Name)).ToList()));
				}
				return base.VisitTypeDeclaration(typeDeclaration, data);
			}
		}
		
		public override string AddBaseTypeToClass(string existingCode, IClass targetClass, IClass newBaseType)
		{
			NR.IParser parser = ParseFile(null, existingCode);
			if (parser == null) {
				return null;
			}

			AddTypeToBaseTypesVisitor addTypeToBaseTypesVisitor = new AddTypeToBaseTypesVisitor(targetClass, newBaseType);

			parser.CompilationUnit.AcceptVisitor(addTypeToBaseTypesVisitor, null);

			// now use an output visitor for the appropriate language (based on
			// extension of the existing code file) to format the new interface.
			IOutputAstVisitor output = GetOutputVisitor();
			
			// run the output visitor with the specials inserter to insert comments
			using (SpecialNodesInserter.Install(parser.Lexer.SpecialTracker.RetrieveSpecials(), output)) {
				parser.CompilationUnit.AcceptVisitor(output, null);
			}

			parser.Dispose();
			
			if (output.Errors.Count > 0) {
				ShowSourceCodeErrors(null, output.Errors.ErrorOutput);
				return null;
			}
			
			return output.Text;
		}
		#endregion
		
		#region FindUnusedUsingDeclarations
		protected class PossibleTypeReference
		{
			public string Name;
			public int TypeParameterCount;
			public IMethod ExtensionMethod;
			
			public PossibleTypeReference(string name)
			{
				this.Name = name;
			}
			
			public PossibleTypeReference(IdentifierExpression identifierExpression)
			{
				this.Name = identifierExpression.Identifier;
				this.TypeParameterCount = identifierExpression.TypeArguments.Count;
			}
			
			public PossibleTypeReference(TypeReference tr)
			{
				this.Name = tr.Type;
				this.TypeParameterCount = tr.GenericTypes.Count;
			}
			
			public PossibleTypeReference(IMethod extensionMethod)
			{
				this.ExtensionMethod = extensionMethod;
			}
			
			public override int GetHashCode()
			{
				int hashCode = 0;
				unchecked {
					if (Name != null) hashCode += 1000000007 * Name.GetHashCode();
					hashCode += 1000000009 * TypeParameterCount.GetHashCode();
					if (ExtensionMethod != null) hashCode += 1000000021 * ExtensionMethod.GetHashCode();
				}
				return hashCode;
			}
			
			public override bool Equals(object obj)
			{
				PossibleTypeReference other = obj as PossibleTypeReference;
				if (other == null) return false;
				return this.Name == other.Name && this.TypeParameterCount == other.TypeParameterCount && object.Equals(this.ExtensionMethod, other.ExtensionMethod);
			}
		}
		
		private class FindPossibleTypeReferencesVisitor : NR.Visitors.AbstractAstVisitor
		{
			internal HashSet<PossibleTypeReference> list = new HashSet<PossibleTypeReference>();
			NRefactoryResolver.NRefactoryResolver resolver;
			ParseInformation parseInformation;
			
			public FindPossibleTypeReferencesVisitor(ParseInformation parseInformation)
			{
				if (parseInformation != null) {
					this.parseInformation = parseInformation;
					resolver = new NRefactoryResolver.NRefactoryResolver(parseInformation.CompilationUnit.ProjectContent.Language);
				}
			}
			
			public override object VisitIdentifierExpression(IdentifierExpression identifierExpression, object data)
			{
				list.Add(new PossibleTypeReference(identifierExpression));
				return base.VisitIdentifierExpression(identifierExpression, data);
			}
			
			public override object VisitTypeReference(TypeReference typeReference, object data)
			{
				if (!typeReference.IsGlobal) {
					list.Add(new PossibleTypeReference(typeReference));
				}
				return base.VisitTypeReference(typeReference, data);
			}
			
			public override object VisitAttribute(ICSharpCode.NRefactory.Ast.Attribute attribute, object data)
			{
				list.Add(new PossibleTypeReference(attribute.Name));
				list.Add(new PossibleTypeReference(attribute.Name + "Attribute"));
				return base.VisitAttribute(attribute, data);
			}
			
			public override object VisitInvocationExpression(InvocationExpression invocationExpression, object data)
			{
				base.VisitInvocationExpression(invocationExpression, data);
				// don't use uninitialized resolver
				if (resolver.ProjectContent == null)
					return null;
				if (invocationExpression.TargetObject is MemberReferenceExpression) {
					MemberResolveResult mrr = resolver.ResolveInternal(invocationExpression, ExpressionContext.Default) as MemberResolveResult;
					if (mrr != null) {
						IMethod method = mrr.ResolvedMember as IMethod;
						if (method != null && method.IsExtensionMethod) {
							list.Add(new PossibleTypeReference(method));
						}
					}
				}
				return null;
			}
			
			public override object VisitMethodDeclaration(MethodDeclaration methodDeclaration, object data)
			{
				// Initialize resolver for method:
				if (!methodDeclaration.Body.IsNull && resolver != null) {
					if (resolver.Initialize(parseInformation, methodDeclaration.Body.StartLocation.Y, methodDeclaration.Body.StartLocation.X)) {
						resolver.RunLookupTableVisitor(methodDeclaration);
					}
				}
				return base.VisitMethodDeclaration(methodDeclaration, data);
			}
			
			public override object VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration, object data)
			{
				if (resolver != null) {
					if (resolver.Initialize(parseInformation, propertyDeclaration.BodyStart.Y, propertyDeclaration.BodyStart.X)) {
						resolver.RunLookupTableVisitor(propertyDeclaration);
					}
				}
				return base.VisitPropertyDeclaration(propertyDeclaration, data);
			}
			
			public override object VisitFieldDeclaration(FieldDeclaration fieldDeclaration, object data)
			{
				if (resolver != null) {
					resolver.Initialize(parseInformation, fieldDeclaration.StartLocation.X, fieldDeclaration.StartLocation.Y);
				}
				return base.VisitFieldDeclaration(fieldDeclaration, data);
			}
		}
		
		protected virtual HashSet<PossibleTypeReference> FindPossibleTypeReferences(IDomProgressMonitor progressMonitor, string fileContent, ParseInformation parseInfo)
		{
			NR.IParser parser = ParseFile(progressMonitor, fileContent);
			if (parser == null) {
				return null;
			} else {
				FindPossibleTypeReferencesVisitor visitor = new FindPossibleTypeReferencesVisitor(parseInfo);
				parser.CompilationUnit.AcceptVisitor(visitor, null);
				parser.Dispose();
				return visitor.list;
			}
		}
		
		public override bool SupportsFindUnusedUsingDeclarations {
			get {
				return true;
			}
		}
		
		public override IList<IUsing> FindUnusedUsingDeclarations(IDomProgressMonitor progressMonitor, string fileName, string fileContent, ICompilationUnit cu)
		{
			IClass @class = cu.Classes.Count == 0 ? null : cu.Classes[0];
			
			HashSet<PossibleTypeReference> references = FindPossibleTypeReferences(progressMonitor, fileContent, new ParseInformation(cu));
			if (references == null) return new IUsing[0];
			
			HashSet<IUsing> usedUsings = new HashSet<IUsing>();
			foreach (PossibleTypeReference tr in references) {
				if (tr.ExtensionMethod != null) {
					// the invocation of an extension method can implicitly use a using
					StringComparer nameComparer = cu.ProjectContent.Language.NameComparer;
					// go through all usings in all nested child scopes
					foreach (IUsing import in cu.GetAllUsings()) {
						foreach (string i in import.Usings) {
							if (nameComparer.Equals(tr.ExtensionMethod.DeclaringType.Namespace, i)) {
								usedUsings.Add(import);
							}
						}
					}
				} else {
					// normal possible type reference
					SearchTypeRequest request = new SearchTypeRequest(tr.Name, tr.TypeParameterCount, @class, cu, 1, 1);
					SearchTypeResult response = cu.ProjectContent.SearchType(request);
					if (response.UsedUsing != null) {
						usedUsings.Add(response.UsedUsing);
					}
				}
			}
			
			List<IUsing> unusedUsings = new List<IUsing>();
			foreach (IUsing import in cu.GetAllUsings()) {
				if (!usedUsings.Contains(import)) {
					if (import.HasAliases) {
						foreach (string key in import.Aliases.Keys) {
							if (references.Contains(new PossibleTypeReference(key)))
								goto checkNextImport;
						}
					}
					unusedUsings.Add(import); // this using is unused
				}
				checkNextImport:;
			}
			return unusedUsings;
		}
		#endregion
		
		#region CreateNewFileLikeExisting
		public override bool SupportsCreateNewFileLikeExisting {
			get {
				return true;
			}
		}
		
		public override string CreateNewFileLikeExisting(string existingFileContent, string codeForNewType)
		{
			NR.IParser parser = ParseFile(null, existingFileContent);
			if (parser == null) {
				return null;
			}
			RemoveTypesVisitor visitor = new RemoveTypesVisitor();
			parser.CompilationUnit.AcceptVisitor(visitor, null);
			List<NR.ISpecial> comments = new List<NR.ISpecial>();
			foreach (NR.ISpecial c in parser.Lexer.SpecialTracker.CurrentSpecials) {
				if (c.StartPosition.Y <= visitor.includeCommentsUpToLine
				    || c.StartPosition.Y > visitor.includeCommentsAfterLine)
				{
					comments.Add(c);
				}
			}
			IOutputAstVisitor outputVisitor = (language==NR.SupportedLanguage.CSharp) ? new CSharpOutputVisitor() : (IOutputAstVisitor)new VBNetOutputVisitor();
			using (SpecialNodesInserter.Install(comments, outputVisitor)) {
				parser.CompilationUnit.AcceptVisitor(outputVisitor, null);
			}
			string expectedText;
			if (language==NR.SupportedLanguage.CSharp)
				expectedText = "using " + RemoveTypesVisitor.DummyIdentifier + ";";
			else
				expectedText = "Imports " + RemoveTypesVisitor.DummyIdentifier;
			using (StringWriter w = new StringWriter()) {
				using (StringReader r1 = new StringReader(outputVisitor.Text)) {
					string line;
					while ((line = r1.ReadLine()) != null) {
						string trimLine = line.TrimStart();
						if (trimLine == expectedText) {
							string indentation = line.Substring(0, line.Length - trimLine.Length);
							using (StringReader r2 = new StringReader(codeForNewType)) {
								while ((line = r2.ReadLine()) != null) {
									w.Write(indentation);
									w.WriteLine(line);
								}
							}
						} else {
							w.WriteLine(line);
						}
					}
				}
				if (visitor.firstType) {
					w.WriteLine(codeForNewType);
				}
				return w.ToString();
			}
		}
		
		private class RemoveTypesVisitor : NR.Visitors.AbstractAstTransformer
		{
			internal const string DummyIdentifier = "DummyNamespace!InsertionPos";
			
			internal int includeCommentsUpToLine;
			internal int includeCommentsAfterLine = int.MaxValue;
			
			internal bool firstType = true;
			
			public override object VisitUsingDeclaration(UsingDeclaration usingDeclaration, object data)
			{
				if (firstType) {
					includeCommentsUpToLine = usingDeclaration.EndLocation.Y;
				}
				return null;
			}
			
			public override object VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration, object data)
			{
				includeCommentsAfterLine = namespaceDeclaration.EndLocation.Y;
				if (firstType) {
					includeCommentsUpToLine = namespaceDeclaration.StartLocation.Y;
					return base.VisitNamespaceDeclaration(namespaceDeclaration, data);
				} else {
					RemoveCurrentNode();
					return null;
				}
			}
			
			void HandleTypeDeclaration(AttributedNode typeDeclaration)
			{
				if (typeDeclaration.EndLocation.Y > includeCommentsAfterLine)
					includeCommentsAfterLine = typeDeclaration.EndLocation.Y;
				if (firstType) {
					firstType = false;
					ReplaceCurrentNode(new UsingDeclaration(DummyIdentifier));
				} else {
					RemoveCurrentNode();
				}
			}
			
			public override object VisitTypeDeclaration(TypeDeclaration typeDeclaration, object data)
			{
				HandleTypeDeclaration(typeDeclaration);
				return null;
			}
			
			public override object VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration, object data)
			{
				HandleTypeDeclaration(delegateDeclaration);
				return null;
			}
		}
		#endregion
		
		#region ExtractCodeForType
		public override bool SupportsGetFullCodeRangeForType {
			get {
				return true;
			}
		}
		
		public override DomRegion GetFullCodeRangeForType(string fileContent, IClass type)
		{
			NR.Parser.ILexer lexer = NR.ParserFactory.CreateLexer(language, new StringReader(fileContent));
			// use the lexer to determine last token position before type start
			// and next token position after type end
			Stack<NR.Location> stack = new Stack<NR.Location>();
			NR.Location lastPos = NR.Location.Empty;
			NR.Parser.Token t = lexer.NextToken();
			bool csharp = language == NR.SupportedLanguage.CSharp;
			int eof = csharp ? NR.Parser.CSharp.Tokens.EOF : NR.Parser.VB.Tokens.EOF;
			int attribStart = csharp ? NR.Parser.CSharp.Tokens.OpenSquareBracket : NR.Parser.VB.Tokens.LessThan;
			int attribEnd = csharp ? NR.Parser.CSharp.Tokens.CloseSquareBracket : NR.Parser.VB.Tokens.GreaterThan;
			
			while (t.Kind != eof) {
				if (t.Kind == attribStart)
					stack.Push(lastPos);
				if (t.EndLocation.Y >= type.Region.BeginLine)
					break;
				lastPos = t.EndLocation;
				if (t.Kind == attribEnd && stack.Count > 0)
					lastPos = stack.Pop();
				t = lexer.NextToken();
			}
			
			stack = null;
			
			// Skip until end of type
			while (t.Kind != eof) {
				if (t.EndLocation.Y > type.BodyRegion.EndLine)
					break;
				t = lexer.NextToken();
			}
			
			int lastLineBefore = lastPos.IsEmpty ? 0 : lastPos.Y;
			int firstLineAfter = t.EndLocation.IsEmpty ? int.MaxValue : t.EndLocation.Y;
			
			lexer.Dispose();
			lexer = null;
			
			StringReader myReader = new StringReader(fileContent);
			
			string line;
			string mainLine;
			int resultBeginLine = lastLineBefore + 1;
			int resultEndLine = firstLineAfter - 1;
			int lineNumber = 0;
			int largestEmptyLineCount = 0;
			int emptyLinesInRow = 0;
			while ((line = myReader.ReadLine()) != null) {
				lineNumber++;
				if (lineNumber <= lastLineBefore)
					continue;
				if (lineNumber < type.Region.BeginLine) {
					string trimLine = line.TrimStart();
					if (trimLine.Length == 0) {
						if (++emptyLinesInRow > largestEmptyLineCount) {
							largestEmptyLineCount = emptyLinesInRow;
							resultBeginLine = lineNumber + 1;
						}
					} else {
						emptyLinesInRow = 0;
						if (IsEndDirective(trimLine)) {
							largestEmptyLineCount = 0;
							resultBeginLine = lineNumber + 1;
						}
					}
				} else if (lineNumber == type.Region.BeginLine) {
					mainLine = line;
				}
				// Region.BeginLine could be BodyRegion.EndLine
				if (lineNumber == type.BodyRegion.EndLine) {
					largestEmptyLineCount = 0;
					emptyLinesInRow = 0;
					resultEndLine = lineNumber;
				} else if (lineNumber > type.BodyRegion.EndLine) {
					if (lineNumber >= firstLineAfter)
						break;
					string trimLine = line.TrimStart();
					if (trimLine.Length == 0) {
						if (++emptyLinesInRow > largestEmptyLineCount) {
							largestEmptyLineCount = emptyLinesInRow;
							resultEndLine = lineNumber - emptyLinesInRow;
						}
					} else {
						emptyLinesInRow = 0;
						if (IsStartDirective(trimLine)) {
							break;
						}
					}
				}
			}
			
			myReader.Dispose();
			return new DomRegion(resultBeginLine, 0, resultEndLine, int.MaxValue);
		}
		
		static bool IsEndDirective(string trimLine)
		{
			return trimLine.StartsWith("#endregion", StringComparison.Ordinal)
				|| trimLine.StartsWith("#endif", StringComparison.Ordinal);
		}
		
		static bool IsStartDirective(string trimLine)
		{
			return trimLine.StartsWith("#region", StringComparison.Ordinal)
				|| trimLine.StartsWith("#if", StringComparison.Ordinal);
		}
		#endregion
	}
}
