// 
// NamingConventionService.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin <http://xamarin.com>
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
using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	public abstract class NamingConventionService
	{
		public abstract IEnumerable<NamingRule> Rules {
			get;
		}

		public string CheckName(RefactoringContext ctx, string name, AffectedEntity entity, Modifiers accessibilty = Modifiers.Private, bool isStatic = false)
		{
			foreach (var rule in Rules) {
				if (!rule.AffectedEntity.HasFlag(entity)) {
					continue;
				}
				if (!rule.VisibilityMask.HasFlag(accessibilty)) {
					continue;
				}
				if (isStatic && !rule.IncludeStaticEntities || !isStatic && !rule.IncludeInstanceMembers) {
					continue;
				}
				if (!rule.IsValid(name)) {
					IList<string> suggestedNames;
					var msg = rule.GetErrorMessage(ctx, name, out suggestedNames);
					if (suggestedNames.Any ())
						return suggestedNames [0];
				}
			}
			return name;
		}

		public bool IsValidName(string name, AffectedEntity entity, Modifiers accessibilty = Modifiers.Private, bool isStatic = false)
		{
			foreach (var rule in Rules) {
				if (!rule.AffectedEntity.HasFlag(entity)) {
					continue;
				}
				if (!rule.VisibilityMask.HasFlag(accessibilty)) {
					continue;
				}
				if (isStatic && !rule.IncludeStaticEntities || !isStatic && !rule.IncludeInstanceMembers) {
					continue;
				}
				if (!rule.IsValid(name)) {
					return false;
				}
			}
			return true;
		}

		public bool HasValidRule(string name, AffectedEntity entity, Modifiers accessibilty = Modifiers.Private, bool isStatic = false)
		{
			foreach (var rule in Rules) {
				if (!rule.AffectedEntity.HasFlag(entity)) {
					continue;
				}
				if (!rule.VisibilityMask.HasFlag(accessibilty)) {
					continue;
				}
				if (isStatic && !rule.IncludeStaticEntities || !isStatic && !rule.IncludeInstanceMembers) {
					continue;
				}
				if (rule.IsValid(name)) {
					return true;
				}
			}
			return false;
		}
	}
}

