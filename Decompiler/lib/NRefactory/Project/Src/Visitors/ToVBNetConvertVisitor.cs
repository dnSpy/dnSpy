// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Ast;
using Attribute = ICSharpCode.NRefactory.Ast.Attribute;

namespace ICSharpCode.NRefactory.Visitors
{
	/// <summary>
	/// Converts elements not supported by VB to their VB representation.
	/// Not all elements are converted here, most simple elements (e.g. ConditionalExpression)
	/// are converted in the output visitor.
	/// </summary>
	public class ToVBNetConvertVisitor : ConvertVisitorBase
	{
		// The following conversions are implemented:
		//   Conflicting field/property names -> m_field
		//   Conflicting variable names inside methods
		//   Anonymous methods are put into new methods
		//   Simple event handler creation is replaced with AddressOfExpression
		//   Move Imports-statements out of namespaces
		//   Parenthesis around Cast expressions remove - these are syntax errors in VB.NET
		//   Decrease array creation size - VB specifies upper bound instead of array length
		
		List<INode> nodesToMoveToCompilationUnit = new List<INode>();
		
		public override object VisitCompilationUnit(CompilationUnit compilationUnit, object data)
		{
			base.VisitCompilationUnit(compilationUnit, data);
			for (int i = 0; i < nodesToMoveToCompilationUnit.Count; i++) {
				compilationUnit.Children.Insert(i, nodesToMoveToCompilationUnit[i]);
				nodesToMoveToCompilationUnit[i].Parent = compilationUnit;
			}
			return null;
		}
		
		public override object VisitUsingDeclaration(UsingDeclaration usingDeclaration, object data)
		{
			base.VisitUsingDeclaration(usingDeclaration, data);
			if (usingDeclaration.Parent is NamespaceDeclaration) {
				nodesToMoveToCompilationUnit.Add(usingDeclaration);
				RemoveCurrentNode();
			}
			return null;
		}
		
		TypeDeclaration currentType;
		
		public override object VisitTypeDeclaration(TypeDeclaration typeDeclaration, object data)
		{
			// fix default inner type visibility
			if (currentType != null && (typeDeclaration.Modifier & Modifiers.Visibility) == 0)
				typeDeclaration.Modifier |= Modifiers.Private;
			
			TypeDeclaration outerType = currentType;
			currentType = typeDeclaration;
			
			if ((typeDeclaration.Modifier & Modifiers.Static) == Modifiers.Static) {
				typeDeclaration.Modifier &= ~Modifiers.Static;
				typeDeclaration.Modifier |= Modifiers.Sealed;
				typeDeclaration.Children.Insert(0, new ConstructorDeclaration("#ctor", Modifiers.Private, null, null));
			}
			
			//   Conflicting field/property names -> m_field
			List<string> properties = new List<string>();
			foreach (object o in typeDeclaration.Children) {
				PropertyDeclaration pd = o as PropertyDeclaration;
				if (pd != null) {
					properties.Add(pd.Name);
				}
			}
			List<VariableDeclaration> conflicts = new List<VariableDeclaration>();
			foreach (object o in typeDeclaration.Children) {
				FieldDeclaration fd = o as FieldDeclaration;
				if (fd != null) {
					foreach (VariableDeclaration var in fd.Fields) {
						string name = var.Name;
						foreach (string propertyName in properties) {
							if (name.Equals(propertyName, StringComparison.InvariantCultureIgnoreCase)) {
								conflicts.Add(var);
							}
						}
					}
				}
			}
			new PrefixFieldsVisitor(conflicts, "m_").Run(typeDeclaration);
			base.VisitTypeDeclaration(typeDeclaration, data);
			currentType = outerType;
			
			return null;
		}
		
		public override object VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration, object data)
		{
			// fix default inner type visibility
			if (currentType != null && (delegateDeclaration.Modifier & Modifiers.Visibility) == 0)
				delegateDeclaration.Modifier |= Modifiers.Private;
			
			return base.VisitDelegateDeclaration(delegateDeclaration, data);
		}
		
		public override object VisitExpressionStatement(ExpressionStatement expressionStatement, object data)
		{
			base.VisitExpressionStatement(expressionStatement, data);
			AssignmentExpression ass = expressionStatement.Expression as AssignmentExpression;
			if (ass != null && ass.Right is AddressOfExpression) {
				if (ass.Op == AssignmentOperatorType.Add) {
					ReplaceCurrentNode(new AddHandlerStatement(ass.Left, ass.Right));
				} else if (ass.Op == AssignmentOperatorType.Subtract) {
					ReplaceCurrentNode(new RemoveHandlerStatement(ass.Left, ass.Right));
				}
			}
			return null;
		}
		
		static string GetMemberNameOnThisReference(Expression expr)
		{
			IdentifierExpression ident = expr as IdentifierExpression;
			if (ident != null)
				return ident.Identifier;
			MemberReferenceExpression fre = expr as MemberReferenceExpression;
			if (fre != null && fre.TargetObject is ThisReferenceExpression)
				return fre.MemberName;
			return null;
		}
		
		public override object VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression, object data)
		{
			base.VisitAnonymousMethodExpression(anonymousMethodExpression, data);
			if (anonymousMethodExpression.Body.Children.Count == 1) {
				ReturnStatement rs = anonymousMethodExpression.Body.Children[0] as ReturnStatement;
				if (rs != null) {
					LambdaExpression lambda = new LambdaExpression();
					lambda.ExpressionBody = rs.Expression;
					lambda.Parameters = anonymousMethodExpression.Parameters;
					ReplaceCurrentNode(lambda);
				}
			}
			return null;
		}
		
		public override object VisitAssignmentExpression(AssignmentExpression assignmentExpression, object data)
		{
			base.VisitAssignmentExpression(assignmentExpression, data);
			if (assignmentExpression.Op == AssignmentOperatorType.Assign && !(assignmentExpression.Parent is ExpressionStatement)) {
				AddInlineAssignHelper();
				ReplaceCurrentNode(
					new InvocationExpression(
						new IdentifierExpression("InlineAssignHelper"),
						new List<Expression> { assignmentExpression.Left, assignmentExpression.Right }
					));
			}
			return null;
		}
		
		void AddInlineAssignHelper()
		{
			MethodDeclaration method;
			foreach (INode node in currentType.Children) {
				method = node as MethodDeclaration;
				if (method != null && method.Name == "InlineAssignHelper") {
					// inline assign helper already exists
					return;
				}
			}
			
			method = new MethodDeclaration {
				Name = "InlineAssignHelper",
				Modifier = Modifiers.Private | Modifiers.Static,
				TypeReference = new TypeReference("T"),
				Parameters = new List<ParameterDeclarationExpression> {
					new ParameterDeclarationExpression(new TypeReference("T"), "target", ParameterModifiers.Ref),
					new ParameterDeclarationExpression(new TypeReference("T"), "value")
				}};
			method.Templates.Add(new TemplateDefinition("T", null));
			method.Body = new BlockStatement();
			method.Body.AddChild(new ExpressionStatement(new AssignmentExpression(
				new IdentifierExpression("target"),
				AssignmentOperatorType.Assign,
				new IdentifierExpression("value"))));
			method.Body.AddChild(new ReturnStatement(new IdentifierExpression("value")));
			currentType.AddChild(method);
		}
		
		bool IsClassType(ClassType c)
		{
			if (currentType == null) return false;
			return currentType.Type == c;
		}
		
		public override object VisitMethodDeclaration(MethodDeclaration methodDeclaration, object data)
		{
			if (!IsClassType(ClassType.Interface) && (methodDeclaration.Modifier & Modifiers.Visibility) == 0)
				methodDeclaration.Modifier |= Modifiers.Private;
			
			base.VisitMethodDeclaration(methodDeclaration, data);
			
			const Modifiers externStatic = Modifiers.Static | Modifiers.Extern;
			if ((methodDeclaration.Modifier & externStatic) == externStatic
			    && methodDeclaration.Body.IsNull)
			{
				foreach (AttributeSection sec in methodDeclaration.Attributes) {
					foreach (Attribute att in sec.Attributes) {
						if ("DllImport".Equals(att.Name, StringComparison.InvariantCultureIgnoreCase)) {
							if (ConvertPInvoke(methodDeclaration, att)) {
								sec.Attributes.Remove(att);
								break;
							}
						}
					}
					if (sec.Attributes.Count == 0) {
						methodDeclaration.Attributes.Remove(sec);
						break;
					}
				}
			}
			
			ToVBNetRenameConflictingVariablesVisitor.RenameConflicting(methodDeclaration);
			
			return null;
		}
		
		bool ConvertPInvoke(MethodDeclaration method, ICSharpCode.NRefactory.Ast.Attribute att)
		{
			if (att.PositionalArguments.Count != 1)
				return false;
			PrimitiveExpression pe = att.PositionalArguments[0] as PrimitiveExpression;
			if (pe == null || !(pe.Value is string))
				return false;
			string libraryName = (string)pe.Value;
			string alias = null;
			bool setLastError = false;
			bool exactSpelling = false;
			CharsetModifier charSet = CharsetModifier.Auto;
			foreach (NamedArgumentExpression arg in att.NamedArguments) {
				switch (arg.Name) {
					case "SetLastError":
						pe = arg.Expression as PrimitiveExpression;
						if (pe != null && pe.Value is bool)
							setLastError = (bool)pe.Value;
						else
							return false;
						break;
					case "ExactSpelling":
						pe = arg.Expression as PrimitiveExpression;
						if (pe != null && pe.Value is bool)
							exactSpelling = (bool)pe.Value;
						else
							return false;
						break;
					case "CharSet":
						{
							MemberReferenceExpression fre = arg.Expression as MemberReferenceExpression;
							if (fre == null || !(fre.TargetObject is IdentifierExpression))
								return false;
							if ((fre.TargetObject as IdentifierExpression).Identifier != "CharSet")
								return false;
							switch (fre.MemberName) {
								case "Unicode":
									charSet = CharsetModifier.Unicode;
									break;
								case "Auto":
									charSet = CharsetModifier.Auto;
									break;
								case "Ansi":
									charSet = CharsetModifier.Ansi;
									break;
								default:
									return false;
							}
						}
						break;
					case "EntryPoint":
						pe = arg.Expression as PrimitiveExpression;
						if (pe != null)
							alias = pe.Value as string;
						break;
					default:
						return false;
				}
			}
			if (setLastError && exactSpelling) {
				// Only P/Invokes with SetLastError and ExactSpelling can be converted to a DeclareDeclaration
				const Modifiers removeModifiers = Modifiers.Static | Modifiers.Extern;
				DeclareDeclaration decl = new DeclareDeclaration(method.Name, method.Modifier &~ removeModifiers,
				                                                 method.TypeReference,
				                                                 method.Parameters,
				                                                 method.Attributes,
				                                                 libraryName, alias, charSet);
				ReplaceCurrentNode(decl);
				base.VisitDeclareDeclaration(decl, null);
				return true;
			} else {
				return false;
			}
		}
		
		public override object VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration, object data)
		{
			if (!IsClassType(ClassType.Interface) && (propertyDeclaration.Modifier & Modifiers.Visibility) == 0)
				propertyDeclaration.Modifier |= Modifiers.Private;
			base.VisitPropertyDeclaration(propertyDeclaration, data);
			
			ToVBNetRenameConflictingVariablesVisitor.RenameConflicting(propertyDeclaration);
			
			return null;
		}
		
		public override object VisitEventDeclaration(EventDeclaration eventDeclaration, object data)
		{
			if (!IsClassType(ClassType.Interface) && (eventDeclaration.Modifier & Modifiers.Visibility) == 0)
				eventDeclaration.Modifier |= Modifiers.Private;
			return base.VisitEventDeclaration(eventDeclaration, data);
		}
		
		public override object VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration, object data)
		{
			// make constructor private if visiblity is not set (unless constructor is static)
			if ((constructorDeclaration.Modifier & (Modifiers.Visibility | Modifiers.Static)) == 0)
				constructorDeclaration.Modifier |= Modifiers.Private;
			base.VisitConstructorDeclaration(constructorDeclaration, data);
			
			ToVBNetRenameConflictingVariablesVisitor.RenameConflicting(constructorDeclaration);
			
			return null;
		}
		
		public override object VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression, object data)
		{
			base.VisitParenthesizedExpression(parenthesizedExpression, data);
			if (parenthesizedExpression.Expression is CastExpression) {
				ReplaceCurrentNode(parenthesizedExpression.Expression); // remove parenthesis
			}
			return null;
		}
		
		public override object VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression, object data)
		{
			for (int i = 0; i < arrayCreateExpression.Arguments.Count; i++) {
				arrayCreateExpression.Arguments[i] = Expression.AddInteger(arrayCreateExpression.Arguments[i], -1);
			}
			return base.VisitArrayCreateExpression(arrayCreateExpression, data);
		}
	}
}
