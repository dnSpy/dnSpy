// 
// DefaultRules.cs
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

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	public static class DefaultRules
	{
		public static IEnumerable<NamingRule> GetFdgRules()
		{
			yield return new NamingRule(AffectedEntity.Namespace) {
				Name = "Namespaces",
				NamingStyle = NamingStyle.PascalCase
			};
			
			yield return new NamingRule(AffectedEntity.Class | AffectedEntity.Struct | AffectedEntity.Enum | AffectedEntity.Delegate) {
				Name = "Types",
				VisibilityMask = Modifiers.Public,
				NamingStyle = NamingStyle.PascalCase
			};

			yield return new NamingRule(AffectedEntity.Interface) {
				Name = "Interfaces",
				NamingStyle = NamingStyle.PascalCase,
				VisibilityMask = Modifiers.Public,
				RequiredPrefixes = new [] { "I" }
			};

			yield return new NamingRule(AffectedEntity.CustomAttributes) {
				Name = "Attributes",
				NamingStyle = NamingStyle.PascalCase,
				VisibilityMask = Modifiers.Public,
				RequiredSuffixes = new [] { "Attribute" }
			};
			
			yield return new NamingRule(AffectedEntity.CustomEventArgs) {
				Name = "Event Arguments",
				NamingStyle = NamingStyle.PascalCase,
				VisibilityMask = Modifiers.Public,
				RequiredSuffixes = new [] { "EventArgs" }
			};
			
			yield return new NamingRule(AffectedEntity.CustomExceptions) {
				Name = "Exceptions",
				NamingStyle = NamingStyle.PascalCase,
				RequiredSuffixes = new [] { "Exception" }
			};

			yield return new NamingRule(AffectedEntity.Methods) {
				Name = "Methods",
				VisibilityMask = Modifiers.Public | Modifiers.Protected,
				NamingStyle = NamingStyle.PascalCase
			};

			yield return new NamingRule(AffectedEntity.ReadonlyField) {
				Name = "Static Readonly Fields",
				VisibilityMask = Modifiers.Public | Modifiers.Protected,
				NamingStyle = NamingStyle.PascalCase,
				IncludeInstanceMembers = false
			};

			yield return new NamingRule(AffectedEntity.Field) {
				Name = "Fields",
				NamingStyle = NamingStyle.PascalCase,
				VisibilityMask = Modifiers.Public | Modifiers.Protected
			};
			
			yield return new NamingRule(AffectedEntity.ReadonlyField) {
				Name = "ReadOnly Fields",
				NamingStyle = NamingStyle.PascalCase,
				VisibilityMask = Modifiers.Public | Modifiers.Protected,
				IncludeStaticEntities = false
			};

			yield return new NamingRule(AffectedEntity.ConstantField) {
				Name = "Constant Fields",
				NamingStyle = NamingStyle.PascalCase,
				VisibilityMask = Modifiers.Public | Modifiers.Protected
			};

			yield return new NamingRule(AffectedEntity.Property) {
				Name = "Properties",
				VisibilityMask = Modifiers.Public | Modifiers.Protected,
				NamingStyle = NamingStyle.PascalCase
			};

			yield return new NamingRule(AffectedEntity.Event) {
				Name = "Events",
				VisibilityMask = Modifiers.Public | Modifiers.Protected,
				NamingStyle = NamingStyle.PascalCase
			};

			yield return new NamingRule(AffectedEntity.EnumMember) {
				Name = "Enum Members",
				NamingStyle = NamingStyle.PascalCase
			};

			yield return new NamingRule(AffectedEntity.Parameter) {
				Name = "Parameters",
				NamingStyle = NamingStyle.CamelCase
			};

			yield return new NamingRule(AffectedEntity.TypeParameter) {
				Name = "Type Parameters",
				NamingStyle = NamingStyle.PascalCase,
				RequiredPrefixes = new [] { "T" }
			};
		}
	}
}

