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
				NamingStyle = NamingStyle.PascalCase
			};

			yield return new NamingRule(AffectedEntity.Interface) {
				Name = "Interfaces",
				NamingStyle = NamingStyle.PascalCase,
				RequiredPrefixes = new [] { "I" }
			};

			yield return new NamingRule(AffectedEntity.CustomAttributes) {
				Name = "Attributes",
				NamingStyle = NamingStyle.PascalCase,
				RequiredSuffixes = new [] { "Attribute" }
			};
			
			yield return new NamingRule(AffectedEntity.CustomEventArgs) {
				Name = "Event Arguments",
				NamingStyle = NamingStyle.PascalCase,
				RequiredSuffixes = new [] { "EventArgs" }
			};
			
			yield return new NamingRule(AffectedEntity.CustomExceptions) {
				Name = "Exceptions",
				NamingStyle = NamingStyle.PascalCase,
				RequiredSuffixes = new [] { "Exception" }
			};

			yield return new NamingRule(AffectedEntity.Methods) {
				Name = "Methods",
				NamingStyle = NamingStyle.PascalCase
			};

			yield return new NamingRule(AffectedEntity.ReadonlyField) {
				Name = "Static Readonly Fields",
				VisibilityMask = Modifiers.Public | Modifiers.Protected | Modifiers.Internal,
				NamingStyle = NamingStyle.PascalCase,
				IncludeInstanceMembers = false
			};

			yield return new NamingRule(AffectedEntity.Field) {
				Name = "Fields (Non Private)",
				NamingStyle = NamingStyle.PascalCase,
				VisibilityMask = Modifiers.Public | Modifiers.Protected | Modifiers.Internal
			};
			
			yield return new NamingRule(AffectedEntity.ReadonlyField) {
				Name = "ReadOnly Fields (Non Private)",
				NamingStyle = NamingStyle.PascalCase,
				VisibilityMask = Modifiers.Public | Modifiers.Protected | Modifiers.Internal,
				IncludeStaticEntities = false
			};

			yield return new NamingRule(AffectedEntity.Field | AffectedEntity.ReadonlyField) {
				Name = "Fields (Private)",
				NamingStyle = NamingStyle.CamelCase,
				AllowedPrefixes = new [] { "_", "m_" },
				VisibilityMask = Modifiers.Private,
				IncludeStaticEntities = false
			};
			
			yield return new NamingRule(AffectedEntity.Field) {
				Name = "Static Fields (Private)",
				NamingStyle = NamingStyle.CamelCase,
				VisibilityMask = Modifiers.Private,
				IncludeStaticEntities = true,
				IncludeInstanceMembers = false
			};
			
			yield return new NamingRule(AffectedEntity.ReadonlyField) {
				Name = "ReadOnly Fields (Private)",
				NamingStyle = NamingStyle.CamelCase,
				VisibilityMask = Modifiers.Private,
				AllowedPrefixes = new [] { "_", "m_" },
				IncludeStaticEntities = false
			};

			yield return new NamingRule(AffectedEntity.ConstantField) {
				Name = "Constant Fields",
				NamingStyle = NamingStyle.PascalCase,
				VisibilityMask = Modifiers.Public | Modifiers.Protected | Modifiers.Internal | Modifiers.Private
			};

			yield return new NamingRule(AffectedEntity.Property) {
				Name = "Properties",
				NamingStyle = NamingStyle.PascalCase
			};

			yield return new NamingRule(AffectedEntity.Event) {
				Name = "Events",
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

