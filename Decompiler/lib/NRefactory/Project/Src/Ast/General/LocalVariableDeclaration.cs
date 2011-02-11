// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.Ast
{
	public class LocalVariableDeclaration : Statement
	{
		TypeReference             typeReference;
		Modifiers                  modifier = Modifiers.None;
		List<VariableDeclaration> variables = new List<VariableDeclaration>();
		
		public TypeReference TypeReference {
			get {
				return typeReference;
			}
			set {
				typeReference = TypeReference.CheckNull(value);
				if (!typeReference.IsNull) typeReference.Parent = this;
			}
		}
		
		public Modifiers Modifier {
			get {
				return modifier;
			}
			set {
				modifier = value;
			}
		}
		
		public List<VariableDeclaration> Variables {
			get {
				return variables;
			}
		}
		
		public TypeReference GetTypeForVariable(int variableIndex)
		{
			if (!typeReference.IsNull) {
				return typeReference;
			}
			
			for (int i = variableIndex; i < Variables.Count;++i) {
				if (!((VariableDeclaration)Variables[i]).TypeReference.IsNull) {
					return ((VariableDeclaration)Variables[i]).TypeReference;
				}
			}
			return null;
		}
		
		public LocalVariableDeclaration(VariableDeclaration declaration) : this(TypeReference.Null)
		{
			Variables.Add(declaration);
		}
		
		public LocalVariableDeclaration(TypeReference typeReference)
		{
			this.TypeReference = typeReference;
		}
		
		public LocalVariableDeclaration(TypeReference typeReference, Modifiers modifier)
		{
			this.TypeReference = typeReference;
			this.modifier      = modifier;
		}
		
		public LocalVariableDeclaration(Modifiers modifier)
		{
			this.typeReference = TypeReference.Null;
			this.modifier      = modifier;
		}
		
		public VariableDeclaration GetVariableDeclaration(string variableName)
		{
			foreach (VariableDeclaration variableDeclaration in variables) {
				if (variableDeclaration.Name == variableName) {
					return variableDeclaration;
				}
			}
			return null;
		}
				
		public override object AcceptVisitor(IAstVisitor visitor, object data)
		{
			return visitor.VisitLocalVariableDeclaration(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[LocalVariableDeclaration: Type={0}, Modifier ={1} Variables={2}]", 
			                     typeReference, 
			                     modifier, 
			                     GetCollectionString(variables));
		}
	}
}
