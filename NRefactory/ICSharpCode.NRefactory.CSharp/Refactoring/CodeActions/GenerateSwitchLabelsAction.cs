// 
// GenerateSwitchLabels.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Mike Krüger <mkrueger@novell.com>
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
using System;
using ICSharpCode.NRefactory.TypeSystem;
using System.Threading;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction("Generate switch labels", Description = "Creates switch lables for enumerations.")]
	public class GenerateSwitchLabelsAction : ICodeActionProvider
	{
		public IEnumerable<CodeAction> GetActions(RefactoringContext context)
		{
			var switchStatement = GetSwitchStatement(context);
			if (switchStatement == null) {
				yield break;
			}
			var result = context.Resolve(switchStatement.Expression);
			if (result.Type.Kind != TypeKind.Enum) {
				yield break;
			}
			yield return new CodeAction (context.TranslateString("Create switch labels"), script => {
				var type = result.Type;
				var newSwitch = (SwitchStatement)switchStatement.Clone();
				
				var target = new TypeReferenceExpression (context.CreateShortType(result.Type));
				foreach (var field in type.GetFields ()) {
					if (field.IsSynthetic || !field.IsConst) {
						continue;
					}
					newSwitch.SwitchSections.Add(new SwitchSection () {
						CaseLabels = {
							new CaseLabel (new MemberReferenceExpression (target.Clone(), field.Name))
						},
						Statements = {
							new BreakStatement ()
						}
					});
				}
				
				newSwitch.SwitchSections.Add(new SwitchSection () {
					CaseLabels = {
						new CaseLabel ()
					},
					Statements = {
						new ThrowStatement (new ObjectCreateExpression (context.CreateShortType("System", "ArgumentOutOfRangeException")))
					}
				});
				
				script.Replace(switchStatement, newSwitch);
			});
		}
		
		static SwitchStatement GetSwitchStatement (RefactoringContext context)
		{
			var switchStatment = context.GetNode<SwitchStatement> ();
			if (switchStatment != null && switchStatment.SwitchSections.Count == 0)
				return switchStatment;
			return null;
		}
	}
}
