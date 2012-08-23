// 
// CreateOverloadWithoutParameterAction.cs
//  
// Author:
//       Mansheng Yang <lightyang0@gmail.com>
// 
// Copyright (c) 2012 Mansheng Yang <lightyang0@gmail.com>
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

using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction ("Create overload without parameter", Description = "Create overload without the selected parameter.")]
	public class CreateOverloadWithoutParameterAction : SpecializedCodeAction<ParameterDeclaration>
	{
		protected override CodeAction GetAction (RefactoringContext context, ParameterDeclaration node)
		{
			if (!node.DefaultExpression.IsNull)
				return null;
			if (node.ParameterModifier == ParameterModifier.This || node.ParameterModifier == ParameterModifier.Params)
				return null;

			var methodDecl = node.Parent as MethodDeclaration;
			if (methodDecl == null)
				return null;

			// explicit implementation
			if (!methodDecl.PrivateImplementationType.IsNull)
				return null;

			// find existing method
			var method = (IMethod)((MemberResolveResult)context.Resolve (methodDecl)).Member;
			var parameters = new List<IParameter> (method.Parameters.Where (param => param.Name != node.Name));
			if (method.DeclaringType.GetMethods (
				m => m.Name == method.Name && m.TypeParameters.Count == method.TypeParameters.Count)
				.Any (m => ParameterListComparer.Instance.Equals (m.Parameters, parameters)))
				return null;
			
			return new CodeAction (context.TranslateString ("Create overload without parameter"),
				script =>
				{
					var defaultExpr = GetDefaultValueExpression (context, node.Type);

					var body = new BlockStatement ();
					Expression argExpr;
					if (node.ParameterModifier == ParameterModifier.Ref) {
						body.Add (new VariableDeclarationStatement (node.Type.Clone (), node.Name, defaultExpr));
						argExpr = new DirectionExpression (FieldDirection.Ref, new IdentifierExpression (node.Name));
					} else if (node.ParameterModifier == ParameterModifier.Out) {
						body.Add (new VariableDeclarationStatement (node.Type.Clone (), node.Name));
						argExpr = new DirectionExpression (FieldDirection.Out, new IdentifierExpression (node.Name));
					} else {
						argExpr = defaultExpr;
					}
					body.Add (new InvocationExpression (new IdentifierExpression (methodDecl.Name),
						methodDecl.Parameters.Select (param => param == node ? argExpr : new IdentifierExpression (param.Name))));

					var decl = (MethodDeclaration)methodDecl.Clone ();
					decl.Parameters.Remove (decl.Parameters.First (param => param.Name == node.Name));
					decl.Body = body;

					script.InsertWithCursor ("Create overload without parameter", Script.InsertPosition.Before, decl);

					//if (node.ParameterModifier != ParameterModifier.Out)
					//    script.Link (defaultExpr);
				}); 
		}

		static Expression GetDefaultValueExpression (RefactoringContext context, AstType astType)
		{
			var type = context.ResolveType (astType);

			// array
			if (type.Kind == TypeKind.Array)
				return new ObjectCreateExpression (astType.Clone ());

			// enum
			if (type.Kind == TypeKind.Enum) {
				var members = type.GetMembers ().ToArray();
				if (members.Length == 0)
					return new DefaultValueExpression (astType.Clone ());
				return astType.Member(members[0].Name);
			}

			if ((type.IsReferenceType ?? false) || type.Kind == TypeKind.Dynamic)
				return new NullReferenceExpression ();

			var typeDefinition = type.GetDefinition ();
			if (typeDefinition != null) {
				switch (typeDefinition.KnownTypeCode) {
					case KnownTypeCode.Boolean:
						return new PrimitiveExpression (false);

					case KnownTypeCode.Char:
						return new PrimitiveExpression ('\0');

					case KnownTypeCode.SByte:
					case KnownTypeCode.Byte:
					case KnownTypeCode.Int16:
					case KnownTypeCode.UInt16:
					case KnownTypeCode.Int32:
					case KnownTypeCode.UInt32:
					case KnownTypeCode.Int64:
					case KnownTypeCode.UInt64:
					case KnownTypeCode.Single:
					case KnownTypeCode.Double:
					case KnownTypeCode.Decimal:
						return new PrimitiveExpression (0);

					case KnownTypeCode.NullableOfT:
						return new NullReferenceExpression ();
				}
				if (type.Kind == TypeKind.Struct)
					return new ObjectCreateExpression (astType.Clone ());
			}
			return new DefaultValueExpression (astType.Clone ());
		}
	}
}
