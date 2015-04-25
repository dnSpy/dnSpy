// 
// GeneratedCodeSettings.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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

namespace ICSharpCode.NRefactory.CSharp
{
	public enum GeneratedCodeMember
	{
		Unknown,

		StaticFields,
		InstanceFields,
		StaticProperties,
		InstanceProperties,
		Indexer,
		Constructors,
		StaticMethods,
		InstanceMethods,
		StaticEvents,
		InstanceEvents,
		Operators,
		NestedTypes
	}

	public class GeneratedCodeSettings
	{
		List<GeneratedCodeMember> codeMemberOrder;

		public List<GeneratedCodeMember> CodeMemberOrder {
			get {
				return codeMemberOrder;
			}
			set {
				codeMemberOrder = value;
			}
		}

		public bool GenerateCategoryComments {
			get;
			set;
		}

		public bool SubOrderAlphabetical {
			get;
			set;
		}

		public void Apply (AstNode rootNode)
		{
			if (rootNode == null)
				throw new ArgumentNullException ("rootNode");
			rootNode.AcceptVisitor (new GenerateCodeVisitior (this));
		}

		public virtual string GetCategoryLabel(GeneratedCodeMember memberCategory)
		{
			switch (memberCategory) {
				case GeneratedCodeMember.StaticFields:
					return "Static Fields";
				case GeneratedCodeMember.InstanceFields:
					return "Fields";
				case GeneratedCodeMember.StaticProperties:
					return "Static Properties";
				case GeneratedCodeMember.InstanceProperties:
					return "Properties";
				case GeneratedCodeMember.Indexer:
					return "Indexer";
				case GeneratedCodeMember.Constructors:
					return "Constructors";
				case GeneratedCodeMember.StaticMethods:
					return "Static Methods";
				case GeneratedCodeMember.InstanceMethods:
					return "Methods";
				case GeneratedCodeMember.StaticEvents:
					return "Static Events";
				case GeneratedCodeMember.InstanceEvents:
					return "Events";
				case GeneratedCodeMember.Operators:
					return "Operators";
				case GeneratedCodeMember.NestedTypes:
					return "Nested Types";
			}
			return null;
		}

		class GenerateCodeVisitior : DepthFirstAstVisitor
		{
			GeneratedCodeSettings settings;

			public GenerateCodeVisitior(GeneratedCodeSettings settings)
			{
				if (settings == null)
					throw new ArgumentNullException("settings");
				this.settings = settings;
			}

			GeneratedCodeMember GetCodeMemberCategory(EntityDeclaration x)
			{
				bool isStatic = x.HasModifier(Modifiers.Static) || x.HasModifier(Modifiers.Const);
				if (x is FieldDeclaration)
					return isStatic ? GeneratedCodeMember.StaticFields : GeneratedCodeMember.InstanceFields;
				if (x is IndexerDeclaration)
					return GeneratedCodeMember.Indexer;
				if (x is PropertyDeclaration)
					return isStatic ? GeneratedCodeMember.StaticProperties : GeneratedCodeMember.InstanceProperties;
				if (x is ConstructorDeclaration || x is DestructorDeclaration)
					return GeneratedCodeMember.Constructors;
				if (x is MethodDeclaration)
					return isStatic ? GeneratedCodeMember.StaticMethods : GeneratedCodeMember.InstanceMethods;
				if (x is OperatorDeclaration)
					return GeneratedCodeMember.Operators;
				if (x is EventDeclaration || x is CustomEventDeclaration)
					return isStatic ? GeneratedCodeMember.StaticEvents : GeneratedCodeMember.InstanceEvents;

				if (x is TypeDeclaration)
					return GeneratedCodeMember.NestedTypes;

				return GeneratedCodeMember.Unknown;
			}

			public override void VisitTypeDeclaration (TypeDeclaration typeDeclaration)
			{
				if (typeDeclaration.ClassType == ClassType.Enum)
					return;
				var entities = new List<EntityDeclaration> (typeDeclaration.Members);
				entities.Sort ((x, y) => {
					int i1 = settings.CodeMemberOrder.IndexOf (GetCodeMemberCategory (x));
					int i2 = settings.CodeMemberOrder.IndexOf (GetCodeMemberCategory (y));
					if (i1 != i2)
						return i1.CompareTo (i2);
					if (settings.SubOrderAlphabetical)
						return (x.Name ?? "").CompareTo ((y.Name ?? ""));
					return entities.IndexOf (x).CompareTo (entities.IndexOf (y));
				});
				typeDeclaration.Members.Clear ();
				typeDeclaration.Members.AddRange (entities);

				if (settings.GenerateCategoryComments) {
					var curCat = GeneratedCodeMember.Unknown;
					foreach (var mem in entities) {
						if (mem.NextSibling is EntityDeclaration)
							mem.Parent.InsertChildAfter (mem, new NewLineNode (), Roles.NewLine);

						var cat = GetCodeMemberCategory (mem);
						if (cat == curCat)
							continue;
						curCat = cat;
						var label = settings.GetCategoryLabel (curCat);
						if (string.IsNullOrEmpty (label))
							continue;

						var cmt = new Comment ("", CommentType.SingleLine);
						var cmt2 = new Comment (" " + label, CommentType.SingleLine);
						var cmt3 = new Comment ("", CommentType.SingleLine);
						mem.Parent.InsertChildBefore (mem, cmt, Roles.Comment);
						mem.Parent.InsertChildBefore (mem, cmt2, Roles.Comment);
						mem.Parent.InsertChildBefore (mem, cmt3, Roles.Comment);
						if (cmt.PrevSibling is EntityDeclaration)
							mem.Parent.InsertChildBefore (cmt, new NewLineNode (), Roles.NewLine);
					}
				}
			}
		}

		static Lazy<GeneratedCodeSettings> defaultSettings = new Lazy<GeneratedCodeSettings>(
			() => new GeneratedCodeSettings() {
				CodeMemberOrder = new List<GeneratedCodeMember>() {
					GeneratedCodeMember.StaticFields,
					GeneratedCodeMember.InstanceFields,
					GeneratedCodeMember.StaticProperties,
					GeneratedCodeMember.InstanceProperties,
					GeneratedCodeMember.Indexer,
					GeneratedCodeMember.Constructors,
					GeneratedCodeMember.StaticMethods,
					GeneratedCodeMember.InstanceMethods,
					GeneratedCodeMember.StaticEvents,
					GeneratedCodeMember.InstanceEvents,
					GeneratedCodeMember.Operators,
					GeneratedCodeMember.NestedTypes
				},
				GenerateCategoryComments = true,
				SubOrderAlphabetical = true
		});

		public static GeneratedCodeSettings Default {
			get {
				return defaultSettings.Value;
			}
		}
	}
}