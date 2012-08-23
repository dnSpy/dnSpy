// 
// RedundantFieldInitializerIssue.cs
// 
// Author:
//      Mansheng Yang <lightyang0@gmail.com>
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
using ICSharpCode.NRefactory.PatternMatching;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription ("Initializing field with default value is redundant",
						Description = "Initializing field with default value is redundant.",
						Category = IssueCategories.Redundancies,
						Severity = Severity.Hint,
						IssueMarker = IssueMarker.GrayOut)]
	public class RedundantFieldInitializerIssue : ICodeIssueProvider
	{
		public IEnumerable<CodeIssue> GetIssues (BaseRefactoringContext context)
		{
			return new GatherVisitor (context).GetIssues ();
		}

		class GatherVisitor : GatherVisitorBase
		{
			public GatherVisitor(BaseRefactoringContext ctx)
				: base(ctx)
			{
			}

			public override void VisitFieldDeclaration (FieldDeclaration fieldDeclaration)
			{
				base.VisitFieldDeclaration (fieldDeclaration);

				var defaultValueExpr = GetDefaultValueExpression (fieldDeclaration.ReturnType);
				if (defaultValueExpr == null)
					return;

				foreach (var variable1 in fieldDeclaration.Variables) {
					var variable = variable1;
					if (!defaultValueExpr.Match (variable.Initializer).Success)
						continue;

					AddIssue (variable, ctx.TranslateString ("Remove redundant field initializer"),
						script => script.Replace (variable, new VariableInitializer (variable.Name)));
				}
			}

			Expression GetDefaultValueExpression (AstType astType)
			{
				var type = ctx.ResolveType (astType);

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
							return new PrimitiveExpression (0);

						case KnownTypeCode.Int64:
							return new Choice { new PrimitiveExpression (0), new PrimitiveExpression (0L) };
						case KnownTypeCode.UInt32:
							return new Choice { new PrimitiveExpression (0), new PrimitiveExpression (0U) };
						case KnownTypeCode.UInt64:
							return new Choice {
								new PrimitiveExpression (0), new PrimitiveExpression (0U), new PrimitiveExpression (0UL)
							};
						case KnownTypeCode.Single:
							return new Choice { new PrimitiveExpression (0), new PrimitiveExpression (0F) };
						case KnownTypeCode.Double:
							return new Choice {
								new PrimitiveExpression (0), new PrimitiveExpression (0F), new PrimitiveExpression (0D)
							};
						case KnownTypeCode.Decimal:
							return new Choice { new PrimitiveExpression (0), new PrimitiveExpression (0M) };

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
}
