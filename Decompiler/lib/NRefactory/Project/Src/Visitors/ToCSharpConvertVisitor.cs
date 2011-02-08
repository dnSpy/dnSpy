// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision$</version>
// </file>

using System;
using ICSharpCode.NRefactory.Ast;

namespace ICSharpCode.NRefactory.Visitors
{
	/// <summary>
	/// Converts elements not supported by C# to their C# representation.
	/// Not all elements are converted here, most simple elements (e.g. StopStatement)
	/// are converted in the output visitor.
	/// </summary>
	public class ToCSharpConvertVisitor : ConvertVisitorBase
	{
		// The following conversions are implemented:
		//   Public Event EventName(param As String) -> automatic delegate declaration
		//   static variables inside methods become fields
		//   Explicit interface implementation:
		//      => create additional member for implementing the interface
		//      or convert to implicit interface implementation
		//   Modules: make all members static
		
		public override object VisitTypeDeclaration(TypeDeclaration typeDeclaration, object data)
		{
			if (typeDeclaration.Type == ClassType.Module) {
				typeDeclaration.Type = ClassType.Class;
				typeDeclaration.Modifier |= Modifiers.Static;
				foreach (INode node in typeDeclaration.Children) {
					MemberNode aNode = node as MemberNode;
					if (aNode != null) {
						aNode.Modifier |= Modifiers.Static;
					}
					FieldDeclaration fd = node as FieldDeclaration;
					if (fd != null) {
						fd.Modifier |= Modifiers.Static;
					}
				}
			}
			
			return base.VisitTypeDeclaration(typeDeclaration, data);
		}
		
		public override object VisitEventDeclaration(EventDeclaration eventDeclaration, object data)
		{
			if (!eventDeclaration.HasAddRegion && !eventDeclaration.HasRaiseRegion && !eventDeclaration.HasRemoveRegion) {
				if (eventDeclaration.TypeReference.IsNull) {
					DelegateDeclaration dd = new DelegateDeclaration(eventDeclaration.Modifier, null);
					dd.Name = eventDeclaration.Name + "EventHandler";
					dd.Parameters = eventDeclaration.Parameters;
					dd.ReturnType = new TypeReference("System.Void");
					dd.Parent = eventDeclaration.Parent;
					eventDeclaration.Parameters = null;
					InsertAfterSibling(eventDeclaration, dd);
					eventDeclaration.TypeReference = new TypeReference(dd.Name);
				}
			}
			return base.VisitEventDeclaration(eventDeclaration, data);
		}
		
		public override object VisitMethodDeclaration(MethodDeclaration methodDeclaration, object data)
		{
			ConvertInterfaceImplementation(methodDeclaration);
			return base.VisitMethodDeclaration(methodDeclaration, data);
		}
		
		void ConvertInterfaceImplementation(MethodDeclaration member)
		{
			// members without modifiers are already C# explicit interface implementations, do not convert them
			if (member.Modifier == Modifiers.None)
				return;
			while (member.InterfaceImplementations.Count > 0) {
				InterfaceImplementation impl = member.InterfaceImplementations[0];
				member.InterfaceImplementations.RemoveAt(0);
				if (member.Name != impl.MemberName) {
					MethodDeclaration newMember = new MethodDeclaration {
						Name = impl.MemberName,
						TypeReference = member.TypeReference,
						Parameters = member.Parameters,
						Body = new BlockStatement()
					};
					InvocationExpression callExpression = new InvocationExpression(new IdentifierExpression(member.Name));
					foreach (ParameterDeclarationExpression decl in member.Parameters) {
						callExpression.Arguments.Add(new IdentifierExpression(decl.ParameterName));
					}
					if (member.TypeReference.SystemType == "System.Void") {
						newMember.Body.AddChild(new ExpressionStatement(callExpression));
					} else {
						newMember.Body.AddChild(new ReturnStatement(callExpression));
					}
					newMember.InterfaceImplementations.Add(impl);
					InsertAfterSibling(member, newMember);
				}
			}
		}
		
		public override object VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration, object data)
		{
			ConvertInterfaceImplementation(propertyDeclaration);
			return base.VisitPropertyDeclaration(propertyDeclaration, data);
		}
		
		void ConvertInterfaceImplementation(PropertyDeclaration member)
		{
			// members without modifiers are already C# explicit interface implementations, do not convert them
			if (member.Modifier == Modifiers.None)
				return;
			while (member.InterfaceImplementations.Count > 0) {
				InterfaceImplementation impl = member.InterfaceImplementations[0];
				member.InterfaceImplementations.RemoveAt(0);
				if (member.Name != impl.MemberName) {
					PropertyDeclaration newMember = new PropertyDeclaration(Modifiers.None, null, impl.MemberName, null);
					newMember.TypeReference = member.TypeReference;
					if (member.HasGetRegion) {
						newMember.GetRegion = new PropertyGetRegion(new BlockStatement(), null);
						newMember.GetRegion.Block.AddChild(new ReturnStatement(new IdentifierExpression(member.Name)));
					}
					if (member.HasSetRegion) {
						newMember.SetRegion = new PropertySetRegion(new BlockStatement(), null);
						newMember.SetRegion.Block.AddChild(new ExpressionStatement(
							new AssignmentExpression(
								new IdentifierExpression(member.Name),
								AssignmentOperatorType.Assign,
								new IdentifierExpression("value")
							)));
					}
					newMember.Parameters = member.Parameters;
					newMember.InterfaceImplementations.Add(impl);
					InsertAfterSibling(member, newMember);
				}
			}
		}
		
		public override object VisitLocalVariableDeclaration(LocalVariableDeclaration localVariableDeclaration, object data)
		{
			base.VisitLocalVariableDeclaration(localVariableDeclaration, data);
			if ((localVariableDeclaration.Modifier & Modifiers.Static) == Modifiers.Static) {
				INode parent = localVariableDeclaration.Parent;
				while (parent != null && !IsTypeLevel(parent)) {
					parent = parent.Parent;
				}
				if (parent != null) {
					INode type = parent.Parent;
					if (type != null) {
						int pos = type.Children.IndexOf(parent);
						if (pos >= 0) {
							FieldDeclaration field = new FieldDeclaration(null);
							field.TypeReference = localVariableDeclaration.TypeReference;
							field.Modifier = Modifiers.Static;
							field.Fields = localVariableDeclaration.Variables;
							new PrefixFieldsVisitor(field.Fields, "static_" + GetTypeLevelEntityName(parent) + "_").Run(parent);
							type.Children.Insert(pos + 1, field);
							RemoveCurrentNode();
						}
					}
				}
			}
			return null;
		}
		
		public override object VisitWithStatement(WithStatement withStatement, object data)
		{
			withStatement.Body.AcceptVisitor(new ReplaceWithAccessTransformer(withStatement.Expression), data);
			base.VisitWithStatement(withStatement, data);
			ReplaceCurrentNode(withStatement.Body);
			return null;
		}
		
		sealed class ReplaceWithAccessTransformer : AbstractAstTransformer
		{
			readonly Expression replaceWith;
			
			public ReplaceWithAccessTransformer(Expression replaceWith)
			{
				this.replaceWith = replaceWith;
			}
			
			public override object VisitMemberReferenceExpression(MemberReferenceExpression fieldReferenceExpression, object data)
			{
				if (fieldReferenceExpression.TargetObject.IsNull) {
					fieldReferenceExpression.TargetObject = replaceWith;
					return null;
				} else {
					return base.VisitMemberReferenceExpression(fieldReferenceExpression, data);
				}
			}
			
			public override object VisitWithStatement(WithStatement withStatement, object data)
			{
				// do not visit the body of the WithStatement
				return withStatement.Expression.AcceptVisitor(this, data);
			}
		}
		
		static bool IsTypeLevel(INode node)
		{
			return node is MethodDeclaration || node is PropertyDeclaration || node is EventDeclaration
				|| node is OperatorDeclaration || node is FieldDeclaration;
		}
		
		static string GetTypeLevelEntityName(INode node)
		{
			if (node is ParametrizedNode)
				return ((ParametrizedNode)node).Name;
			else if (node is FieldDeclaration)
				return ((FieldDeclaration)node).Fields[0].Name;
			else
				throw new ArgumentException();
		}
		
		public override object VisitSwitchSection(SwitchSection switchSection, object data)
		{
			// Check if a 'break' should be auto inserted.
			if (switchSection.Children.Count == 0 ||
			    !(switchSection.Children[switchSection.Children.Count - 1] is BreakStatement ||
			      switchSection.Children[switchSection.Children.Count - 1] is ContinueStatement ||
			      switchSection.Children[switchSection.Children.Count - 1] is ThrowStatement ||
			      switchSection.Children[switchSection.Children.Count - 1] is ReturnStatement))
			{
				switchSection.Children.Add(new BreakStatement());
			}
			return base.VisitSwitchSection(switchSection, data);
		}
	}
}
