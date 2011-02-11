// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Linq;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.AstBuilder;

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
		//   Use Convert.ToInt32 for VB casts
		//   Add System.Object-TypeReference to properties without TypeReference
		
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
						if ((fd.Modifier & Modifiers.Const) == 0)
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
					dd.ReturnType = new TypeReference("System.Void", true);
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
					if (member.TypeReference.Type == "System.Void") {
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
			
			if (propertyDeclaration.TypeReference.IsNull)
				propertyDeclaration.TypeReference = new TypeReference("object", true);
			
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
					string fieldPrefix = "static_" + GetTypeLevelEntityName(parent) + "_";
					foreach (VariableDeclaration v in localVariableDeclaration.Variables) {
						if (!v.Initializer.IsNull) {
							string initFieldName = fieldPrefix + v.Name + "_Init";
							FieldDeclaration initField = new FieldDeclaration(null);
							initField.TypeReference = new TypeReference("Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag");
							initField.Modifier = (((AttributedNode)parent).Modifier & Modifiers.Static) | Modifiers.ReadOnly;
							Expression initializer = initField.TypeReference.New();
							initField.Fields.Add(new VariableDeclaration(initFieldName, initializer));
							InsertBeforeSibling(parent, initField);
							
							InsertAfterSibling(localVariableDeclaration, InitStaticVariable(initFieldName, v.Name, v.Initializer, parent.Parent as TypeDeclaration));
						}
						
						FieldDeclaration field = new FieldDeclaration(null);
						field.TypeReference = localVariableDeclaration.TypeReference;
						field.Modifier = ((AttributedNode)parent).Modifier & Modifiers.Static;
						field.Fields.Add(new VariableDeclaration(fieldPrefix + v.Name) { TypeReference = v.TypeReference });
						InsertBeforeSibling(parent, field);
					}
					new PrefixFieldsVisitor(localVariableDeclaration.Variables, fieldPrefix).Run(parent);
					RemoveCurrentNode();
				}
			}
			return null;
		}
		
		INode InitStaticVariable(string initFieldName, string variableName, Expression initializer, TypeDeclaration typeDeclaration)
		{
			const string helperMethodName = "InitStaticVariableHelper";
			
			if (typeDeclaration != null) {
				if (!typeDeclaration.Children.OfType<MethodDeclaration>().Any(m => m.Name == helperMethodName)) {
					// add helper method
					var helperMethod = new MethodDeclaration {
						Name = helperMethodName,
						Modifier = Modifiers.Static,
						TypeReference = new TypeReference("System.Boolean", true),
						Parameters = {
							new ParameterDeclarationExpression(new TypeReference("Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag"), "flag")
						},
						Body = new BlockStatement()
					};
					BlockStatement trueBlock = new BlockStatement();
					BlockStatement elseIfBlock = new BlockStatement();
					BlockStatement falseBlock = new BlockStatement();
					helperMethod.Body.AddStatement(
						new IfElseStatement(ExpressionBuilder.Identifier("flag").Member("State").Operator(BinaryOperatorType.Equality, new PrimitiveExpression(0))) {
							TrueStatement = { trueBlock },
							ElseIfSections = {
								new ElseIfSection(ExpressionBuilder.Identifier("flag").Member("State").Operator(BinaryOperatorType.Equality, new PrimitiveExpression(2)), elseIfBlock)
							},
							FalseStatement = { falseBlock }
						});
					trueBlock.Assign(ExpressionBuilder.Identifier("flag").Member("State"), new PrimitiveExpression(2));
					trueBlock.Return(new PrimitiveExpression(true));
					elseIfBlock.Throw(new TypeReference("Microsoft.VisualBasic.CompilerServices.IncompleteInitialization").New());
					falseBlock.Return(new PrimitiveExpression(false));
					typeDeclaration.AddChild(helperMethod);
				}
			}
			
			BlockStatement tryBlock = new BlockStatement();
			BlockStatement ifTrueBlock = new BlockStatement();
			tryBlock.AddStatement(new IfElseStatement(ExpressionBuilder.Identifier(helperMethodName).Call(ExpressionBuilder.Identifier(initFieldName)), ifTrueBlock));
			ifTrueBlock.Assign(ExpressionBuilder.Identifier(variableName), initializer);
			
			BlockStatement finallyBlock = new BlockStatement();
			finallyBlock.Assign(ExpressionBuilder.Identifier(initFieldName).Member("State"), new PrimitiveExpression(1));
			
			BlockStatement lockBlock = new BlockStatement();
			lockBlock.AddStatement(new TryCatchStatement(tryBlock, null, finallyBlock));
			return new LockStatement(ExpressionBuilder.Identifier(initFieldName), lockBlock);
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
			return node.Parent is TypeDeclaration;
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
		
		public override object VisitCastExpression(CastExpression castExpression, object data)
		{
			base.VisitCastExpression(castExpression, data);
			if (castExpression.CastType == CastType.Conversion || castExpression.CastType == CastType.PrimitiveConversion) {
				switch (castExpression.CastTo.Type) {
					case "System.Boolean":
						return ReplacePrimitiveCastWithConvertMethodCall(castExpression, "ToBoolean");
					case "System.Byte":
						return ReplacePrimitiveCastWithConvertMethodCall(castExpression, "ToByte");
					case "System.Char":
						return ReplacePrimitiveCastWithConvertMethodCall(castExpression, "ToChar");
					case "System.DateTime":
						return ReplacePrimitiveCastWithConvertMethodCall(castExpression, "ToDateTime");
					case "System.Decimal":
						return ReplacePrimitiveCastWithConvertMethodCall(castExpression, "ToDecimal");
					case "System.Double":
						return ReplacePrimitiveCastWithConvertMethodCall(castExpression, "ToDouble");
					case "System.Int16":
						return ReplacePrimitiveCastWithConvertMethodCall(castExpression, "ToInt16");
					case "System.Int32":
						return ReplacePrimitiveCastWithConvertMethodCall(castExpression, "ToInt32");
					case "System.Int64":
						return ReplacePrimitiveCastWithConvertMethodCall(castExpression, "ToInt64");
					case "System.SByte":
						return ReplacePrimitiveCastWithConvertMethodCall(castExpression, "ToSByte");
					case "System.Single":
						return ReplacePrimitiveCastWithConvertMethodCall(castExpression, "ToSingle");
					case "System.String":
						return ReplacePrimitiveCastWithConvertMethodCall(castExpression, "ToString");
					case "System.UInt16":
						return ReplacePrimitiveCastWithConvertMethodCall(castExpression, "ToUInt16");
					case "System.UInt32":
						return ReplacePrimitiveCastWithConvertMethodCall(castExpression, "ToUInt32");
					case "System.UInt64":
						return ReplacePrimitiveCastWithConvertMethodCall(castExpression, "ToUInt64");
				}
			}
			return null;
		}
		
		object ReplacePrimitiveCastWithConvertMethodCall(CastExpression castExpression, string methodName)
		{
			ReplaceCurrentNode(ExpressionBuilder.Identifier("Convert").Call(methodName, castExpression.Expression));
			return null;
		}
	}
}
