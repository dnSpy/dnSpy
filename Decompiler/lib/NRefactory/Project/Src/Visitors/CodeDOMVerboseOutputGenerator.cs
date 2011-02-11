// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Security.Permissions;

namespace ICSharpCode.NRefactory.Visitors
{
	[PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
	[PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
	public class CodeDomVerboseOutputGenerator : System.CodeDom.Compiler.CodeGenerator
	{
		#region System.CodeDom.Compiler.CodeGenerator abstract class implementation
		protected override string NullToken {
			get {
				return "[NULL]";
			}
		}

		protected override void OutputType(CodeTypeReference typeRef)
		{
			Output.Write("[CodeTypeReference: {0}", typeRef.BaseType);
			if (typeRef.ArrayRank > 0) {
				Output.Write(" Rank:" + typeRef.ArrayRank);
			}
			Output.Write("]");
		}
		
		protected override void GenerateArrayCreateExpression(CodeArrayCreateExpression e)
		{
			Output.Write("[CodeArrayCreateExpression: {0}]", e.ToString());
		}
		
		protected override void GenerateBaseReferenceExpression(CodeBaseReferenceExpression e)
		{
			Output.Write("[CodeBaseReferenceExpression: {0}]", e.ToString());
		}
		
		protected override void GenerateCastExpression(CodeCastExpression e)
		{
			Output.Write("[CodeCastExpression: {0}]", e.ToString());
		}
		
		protected override void GenerateDelegateCreateExpression(CodeDelegateCreateExpression e)
		{
			Output.Write("[CodeDelegateCreateExpression: {0}]", e.ToString());
		}
		
		protected override void GenerateFieldReferenceExpression(CodeFieldReferenceExpression e)
		{
			Output.Write("[CodeFieldReferenceExpression: Name={0}, Target=", e.FieldName);
			this.GenerateExpression(e.TargetObject);
			Output.Write("]");
		}
		
		protected override void GenerateMethodReferenceExpression(CodeMethodReferenceExpression e)
		{
			Output.Write("[CodeMethodReferenceExpression: Name={0}, Target=", e.MethodName);
			this.GenerateExpression(e.TargetObject);
			Output.Write("]");
		}
		
		protected override void GenerateEventReferenceExpression(CodeEventReferenceExpression e)
		{
			Output.Write("[CodeEventReferenceExpression: Name={0}, Target=", e.EventName);
			this.GenerateExpression(e.TargetObject);
			Output.Write("]");
		}
		
		protected override void GenerateArgumentReferenceExpression(CodeArgumentReferenceExpression e)
		{
			Output.Write("[CodeArgumentReferenceExpression: {0}]", e.ToString());
		}
		
		protected override void GenerateVariableReferenceExpression(CodeVariableReferenceExpression e)
		{
			Output.Write("[CodeVariableReferenceExpression: Name={0}]", e.VariableName);
		}
		
		protected override void GenerateIndexerExpression(CodeIndexerExpression e)
		{
			Output.Write("[CodeIndexerExpression: {0}]", e.ToString());
		}
		
		protected override void GenerateArrayIndexerExpression(CodeArrayIndexerExpression e)
		{
			Output.Write("[CodeArrayIndexerExpression: {0}]", e.ToString());
		}
		
		protected override void GenerateSnippetExpression(CodeSnippetExpression e)
		{
			Output.Write("[CodeSnippetExpression: {0}]", e.ToString());
		}
		
		protected override void GenerateMethodInvokeExpression(CodeMethodInvokeExpression e)
		{
			Output.Write("[CodeMethodInvokeExpression: Method=");
			GenerateMethodReferenceExpression(e.Method);
			Output.Write(", Parameters=");
			bool first = true;
			foreach (CodeExpression expr in e.Parameters) {
				if (first) first = false; else Output.Write(", ");
				this.GenerateExpression(expr);
			}
			Output.Write("]");
		}
		
		protected override void GenerateDelegateInvokeExpression(CodeDelegateInvokeExpression e)
		{
			Output.Write("[CodeDelegateInvokeExpression: {0}]", e.ToString());
		}
		
		protected override void GenerateObjectCreateExpression(CodeObjectCreateExpression e)
		{
			Output.Write("[CodeObjectCreateExpression: Type={0}, Parameters=", e.CreateType.BaseType);
			bool first = true;
			foreach (CodeExpression expr in e.Parameters) {
				if (first) first = false; else Output.Write(", ");
				this.GenerateExpression(expr);
			}
			Output.Write("]");
		}
		
		protected override void GeneratePropertyReferenceExpression(CodePropertyReferenceExpression e)
		{
			Output.Write("[CodePropertyReferenceExpression: Name={0}, Target=", e.PropertyName);
			this.GenerateExpression(e.TargetObject);
			Output.Write("]");
		}
		
		protected override void GeneratePropertySetValueReferenceExpression(CodePropertySetValueReferenceExpression e)
		{
			Output.Write("[CodePropertySetValueReferenceExpression: {0}]", e.ToString());
		}
		
		protected override void GenerateThisReferenceExpression(CodeThisReferenceExpression e)
		{
			Output.Write("[CodeThisReferenceExpression]");
		}
		
		protected override void GenerateExpressionStatement(CodeExpressionStatement e)
		{
			Output.Write("[CodeExpressionStatement:");
			base.GenerateExpression(e.Expression);
			Output.WriteLine("]");
		}
		
		protected override void GenerateIterationStatement(CodeIterationStatement e)
		{
			Output.WriteLine("[CodeIterationStatement: {0}]", e.ToString());
		}
		
		protected override void GenerateThrowExceptionStatement(CodeThrowExceptionStatement e)
		{
			Output.WriteLine("[CodeThrowExceptionStatement: {0}]", e.ToString());
		}
		
		protected override void GenerateComment(CodeComment e)
		{
			Output.WriteLine("[CodeComment: {0}]", e.ToString());
		}
		
		protected override void GenerateMethodReturnStatement(CodeMethodReturnStatement e)
		{
			Output.WriteLine("[CodeMethodReturnStatement: {0}]", e.ToString());
		}
		
		protected override void GenerateConditionStatement(CodeConditionStatement e)
		{
			Output.WriteLine("[GenerateConditionStatement: {0}]", e.ToString());
		}
		
		protected override void GenerateTryCatchFinallyStatement(CodeTryCatchFinallyStatement e)
		{
			Output.WriteLine("[CodeTryCatchFinallyStatement: {0}]", e.ToString());
		}
		
		protected override void GenerateAssignStatement(CodeAssignStatement e)
		{
			Output.Write("[CodeAssignStatement: Left=");
			base.GenerateExpression(e.Left);
			Output.Write(", Right=");
			base.GenerateExpression(e.Right);
			Output.WriteLine("]");
		}
		
		protected override void GenerateAttachEventStatement(CodeAttachEventStatement e)
		{
			Output.WriteLine("[CodeAttachEventStatement: {0}]", e.ToString());
		}
		
		protected override void GenerateRemoveEventStatement(CodeRemoveEventStatement e)
		{
			Output.WriteLine("[CodeRemoveEventStatement: {0}]", e.ToString());
		}
		
		protected override void GenerateGotoStatement(CodeGotoStatement e)
		{
			Output.WriteLine("[CodeGotoStatement: {0}]", e.ToString());
		}
		
		protected override void GenerateLabeledStatement(CodeLabeledStatement e)
		{
			Output.WriteLine("[CodeLabeledStatement: {0}]", e.ToString());
		}
		
		protected override void GenerateVariableDeclarationStatement(CodeVariableDeclarationStatement e)
		{
			Output.WriteLine("[CodeVariableDeclarationStatement: {0}]", e.ToString());
		}
		
		protected override void GenerateLinePragmaStart(CodeLinePragma e)
		{
			Output.WriteLine("[CodeLinePragma: {0}]", e.ToString());
		}
		
		protected override void GenerateLinePragmaEnd(CodeLinePragma e)
		{
			Output.WriteLine("[CodeLinePragma: {0}]", e.ToString());
		}
		
		protected override void GenerateEvent(CodeMemberEvent e, CodeTypeDeclaration c)
		{
			Output.WriteLine("[CodeMemberEvent: {0}]", e.ToString());
		}
		
		protected override void GenerateField(CodeMemberField e)
		{
			Output.Write("[CodeMemberField: Name={0}, Type=", e.Name);
			Output.Write(e.Type.BaseType);
			Output.WriteLine("]");
		}
		
		protected override void GenerateSnippetMember(CodeSnippetTypeMember e)
		{
			Output.WriteLine("[CodeSnippetTypeMember: {0}]", e.ToString());
		}
		
		protected override void GenerateEntryPointMethod(CodeEntryPointMethod e, CodeTypeDeclaration c)
		{
			Output.WriteLine("[CodeEntryPointMethod: {0}]", e.ToString());
		}
		
		public void PublicGenerateCodeFromStatement(CodeStatement e, TextWriter w, CodeGeneratorOptions o)
		{
			((ICodeGenerator)this).GenerateCodeFromStatement(e, w, o);
		}
		
		protected override void GenerateMethod(CodeMemberMethod e, CodeTypeDeclaration c)
		{
			Output.WriteLine("[CodeMemberMethod: Name={0}, Parameterns={1}]", e.Name, e.Parameters.Count);
			++Indent;
			GenerateStatements(e.Statements);
			--Indent;
		}
		
		protected override void GenerateProperty(CodeMemberProperty e, CodeTypeDeclaration c)
		{
			Output.WriteLine("[CodeMemberProperty : {0}]", e.ToString());
		}
		
		protected override void GenerateConstructor(CodeConstructor e, CodeTypeDeclaration c)
		{
			Output.WriteLine("[CodeConstructor : {0}]", e.ToString());
			++Indent;
			GenerateStatements(e.Statements);
			--Indent;
		}
		
		protected override void GenerateTypeConstructor(CodeTypeConstructor e)
		{
			Output.WriteLine("[CodeTypeConstructor : {0}]", e.ToString());
		}
		
		protected override void GenerateTypeStart(CodeTypeDeclaration e)
		{
			Output.WriteLine("[CodeTypeDeclaration : {0}]", e.ToString());
		}
		
		protected override void GenerateTypeEnd(CodeTypeDeclaration e)
		{
			Output.WriteLine("[CodeTypeDeclaration: {0}]", e.ToString());
		}
		
		protected override void GenerateNamespaceStart(CodeNamespace e)
		{
			Output.WriteLine("[CodeNamespaceStart: {0}]", e.ToString());
		}
		
		protected override void GenerateNamespaceEnd(CodeNamespace e)
		{
			Output.WriteLine("[CodeNamespaceEnd: {0}]", e.ToString());
		}
		
		protected override void GenerateNamespaceImport(CodeNamespaceImport e)
		{
			Output.WriteLine("[CodeNamespaceImport: {0}]", e.ToString());
		}
		
		protected override void GenerateAttributeDeclarationsStart(CodeAttributeDeclarationCollection attributes)
		{
			Output.WriteLine("[CodeAttributeDeclarationCollection: {0}]", attributes.ToString());
		}
		
		protected override void GenerateAttributeDeclarationsEnd(CodeAttributeDeclarationCollection attributes)
		{
			Output.WriteLine("[CodeAttributeDeclarationCollection: {0}]", attributes.ToString());
		}
		
		protected override void GeneratePrimitiveExpression(CodePrimitiveExpression e)
		{
			if (e.Value == null) {
				Output.WriteLine("[CodePrimitiveExpression: null]");
			} else {
				Output.Write("[CodePrimitiveExpression: ");
				base.GeneratePrimitiveExpression(e);
				Output.WriteLine(" (" + e.Value.GetType().Name + ")]");
			}
		}
		
		protected override bool Supports(GeneratorSupport support)
		{
			return true;
		}
		
		protected override bool IsValidIdentifier(string value)
		{
			return true;
		}
		
		protected override string CreateEscapedIdentifier(string value)
		{
			return value;
		}
		
		protected override string CreateValidIdentifier(string value)
		{
			return value;
		}
		
		protected override string GetTypeOutput(CodeTypeReference value)
		{
			return value.ToString();
		}
		
		protected override string QuoteSnippetString(string value)
		{
			return "\"" + value + "\"";
		}
		
		#endregion
	}
}
