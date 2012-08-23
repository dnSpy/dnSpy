//
// ThreadStaticOnInstanceFieldIssue.cs
//
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2012 Simon Lindgren
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
using System;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Semantics;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription("The ThreadStatic attribute does nothing on instance fields",
	                  Description = "Finds usages of ThreadStatic on instance fields.",
	                  Category = IssueCategories.Notifications,
	                  Severity = Severity.Warning)]
	public class ThreadStaticOnInstanceFieldIssue : ICodeIssueProvider
	{
		public IEnumerable<CodeIssue> GetIssues(BaseRefactoringContext context)
		{
			return new GatherVisitor(context).GetIssues();
		}
		
		class GatherVisitor : GatherVisitorBase
		{
			ITypeDefinition threadStaticDefinition;

			public GatherVisitor(BaseRefactoringContext context) : base (context)
			{
				var type = typeof(ThreadStaticAttribute).ToTypeReference().Resolve(ctx.Compilation.TypeResolveContext);
				threadStaticDefinition = type.GetDefinition();
			}

			public override void VisitFieldDeclaration(FieldDeclaration fieldDeclaration)
			{
				if (fieldDeclaration.Modifiers.HasFlag(Modifiers.Static))
					return;

				foreach (var attributeSection in fieldDeclaration.Attributes) {
					int attributeCount = attributeSection.Attributes.Count;
					foreach (var attribute in attributeSection.Attributes) {
						var resolvedAttribute = ctx.Resolve(attribute.Type) as TypeResolveResult;
						if (resolvedAttribute == null)
							continue;
						if (threadStaticDefinition.Equals(resolvedAttribute.Type.GetDefinition())) {
							string title = ctx.TranslateString("ThreadStatic does nothing on instance fields");
							if (attributeCount == 1)
								AddIssue(attributeSection, title, GetActions(attribute, attributeSection, fieldDeclaration));
							else
								AddIssue(attribute, title, GetActions(attribute, attributeSection, fieldDeclaration));
						}
					}
				}
			}

			IEnumerable<CodeAction> GetActions(Attribute attribute, AttributeSection attributeSection, FieldDeclaration fieldDeclaration)
			{
				string removeAttributeMessage = ctx.TranslateString("Remove attribute");
				yield return new CodeAction(removeAttributeMessage, script => {
					if (attributeSection.Attributes.Count > 1) {
						var newSection = new AttributeSection();
						newSection.AttributeTarget = attributeSection.AttributeTarget;
						foreach (var attr in attributeSection.Attributes) {
							if (attr != attribute)
								newSection.Attributes.Add((Attribute)attr.Clone());
						}
						script.Replace(attributeSection, newSection);
					} else {
						script.Remove(attributeSection);
					}
				});

				var makeStaticMessage = ctx.TranslateString("Make the field static");
				yield return new CodeAction(makeStaticMessage, script => {
					var newDeclaration = (FieldDeclaration)fieldDeclaration.Clone();
					newDeclaration.Modifiers |= Modifiers.Static;
					script.Replace(fieldDeclaration, newDeclaration);
				});
			}
		}
	}
}

