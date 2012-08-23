// 
// CSharpParser.cs
//
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ICSharpCode.NRefactory.Editor;
using Mono.CSharp;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp
{
	public class CSharpParser
	{
		CompilerSettings compilerSettings;
		
		class ConversionVisitor : StructuralVisitor
		{
			SyntaxTree unit = new SyntaxTree ();
			internal bool convertTypeSystemMode;
			
			public SyntaxTree Unit {
				get {
					return unit;
				}
				set {
					unit = value;
				}
			}
			
			public LocationsBag LocationsBag {
				get;
				private set;
			}
			
			public ConversionVisitor (bool convertTypeSystemMode, LocationsBag locationsBag)
			{
				this.convertTypeSystemMode = convertTypeSystemMode;
				this.LocationsBag = locationsBag;
			}
			
			public static TextLocation Convert (Mono.CSharp.Location loc)
			{
				return new TextLocation (loc.Row, loc.Column);
			}
			
			public override void Visit(ModuleContainer mc)
			{
				bool first = true;
				foreach (var container in mc.Containers) {
					var nspace = container as NamespaceContainer;
					if (nspace == null) {
						container.Accept(this);
						continue;
					}
					NamespaceDeclaration nDecl = null;
					var loc = LocationsBag.GetLocations(nspace);
					
					if (nspace.NS != null && !string.IsNullOrEmpty(nspace.NS.Name)) {
						nDecl = new NamespaceDeclaration ();
						if (loc != null) {
							nDecl.AddChild(new CSharpTokenNode (Convert(loc [0])), Roles.NamespaceKeyword);
						}
						ConvertNamespaceName(nspace.RealMemberName, nDecl);
						if (loc != null && loc.Count > 1) {
							nDecl.AddChild(new CSharpTokenNode (Convert(loc [1])), Roles.LBrace);
						}
						AddToNamespace(nDecl);
						namespaceStack.Push(nDecl);
					}
					
					if (nspace.Usings != null) {
						foreach (var us in nspace.Usings) {
							us.Accept(this);
						}
					}
					
					if (first) {
						first = false;
						AddAttributeSection(Unit, mc);
					}
					
					if (nspace.Containers != null) {
						foreach (var subContainer in nspace.Containers) {
							subContainer.Accept(this);
						}
					}
					if (nDecl != null) {
						AddAttributeSection (nDecl, nspace.UnattachedAttributes, EntityDeclaration.UnattachedAttributeRole);
						if (loc != null && loc.Count > 2)
							nDecl.AddChild (new CSharpTokenNode (Convert (loc [2])), Roles.RBrace);
						if (loc != null && loc.Count > 3)
							nDecl.AddChild (new CSharpTokenNode (Convert (loc [3])), Roles.Semicolon);
						
						namespaceStack.Pop ();
					} else {
						AddAttributeSection (unit, nspace.UnattachedAttributes, EntityDeclaration.UnattachedAttributeRole);
					}
				}
				AddAttributeSection (unit, mc.UnattachedAttributes, EntityDeclaration.UnattachedAttributeRole);
			}
			
			#region Global
			Stack<NamespaceDeclaration> namespaceStack = new Stack<NamespaceDeclaration> ();
			
			void AddTypeArguments (ATypeNameExpression texpr, AstType result)
			{
				if (texpr.TypeArguments == null || texpr.TypeArguments.Args == null)
					return;
				var loc = LocationsBag.GetLocations (texpr.TypeArguments);
				if (loc != null && loc.Count >= 2)
					result.AddChild (new CSharpTokenNode (Convert (loc [loc.Count - 2])), Roles.LChevron);
				int i = 0;
				foreach (var arg in texpr.TypeArguments.Args) {
					result.AddChild (ConvertToType (arg), Roles.TypeArgument);
					if (loc != null && i < loc.Count - 2)
						result.AddChild (new CSharpTokenNode (Convert (loc [i++])), Roles.Comma);
				}
				if (loc != null && loc.Count >= 2)
					result.AddChild (new CSharpTokenNode (Convert (loc [loc.Count - 1])), Roles.RChevron);
			}
			
			AstType ConvertToType (TypeParameter spec)
			{
				AstType result;
				result = new SimpleType () { IdentifierToken = Identifier.Create (spec.Name, Convert (spec.Location)) };
				return result;
			}
			
			AstType ConvertToType (MemberName memberName)
			{
				AstType result;
				if (memberName.Left != null) {
					result = new MemberType ();
					result.AddChild (ConvertToType (memberName.Left), MemberType.TargetRole);
					var loc = LocationsBag.GetLocations (memberName.Left);
					if (loc != null)
						result.AddChild (new CSharpTokenNode (Convert (loc [0])), Roles.Dot);
					result.AddChild (Identifier.Create (memberName.Name, Convert (memberName.Location)), Roles.Identifier);
				} else {
					result = new SimpleType () { IdentifierToken = Identifier.Create (memberName.Name, Convert (memberName.Location)) };
				}
				if (memberName.TypeParameters != null) {
					var chevronLocs = LocationsBag.GetLocations (memberName.TypeParameters);
					if (chevronLocs != null)
						result.AddChild (new CSharpTokenNode (Convert (chevronLocs [chevronLocs.Count - 2])), Roles.LChevron);
					for (int i = 0; i < memberName.TypeParameters.Count; i++) {
						var param = memberName.TypeParameters [i];
						result.AddChild (new SimpleType (Identifier.Create (param.Name, Convert (param.Location))), Roles.TypeArgument);
						if (chevronLocs != null && i < chevronLocs.Count - 2)
							result.AddChild (new CSharpTokenNode (Convert (chevronLocs [i])), Roles.Comma);
					}
					if (chevronLocs != null)
						result.AddChild (new CSharpTokenNode (Convert (chevronLocs [chevronLocs.Count - 1])), Roles.RChevron);
				}
				return result;
			}
			
			AstType ConvertToType (Mono.CSharp.Expression typeName)
			{
				if (typeName == null) // may happen in typeof(Generic<,,,,>)
					return new SimpleType ();
				
				if (typeName is TypeExpression) {
					var typeExpr = (Mono.CSharp.TypeExpression)typeName;
					return new PrimitiveType (typeExpr.GetSignatureForError (), Convert (typeExpr.Location));
				}
				
				if (typeName is Mono.CSharp.QualifiedAliasMember) {
					var qam = (Mono.CSharp.QualifiedAliasMember)typeName;
					var memberType = new MemberType ();
					memberType.Target = new SimpleType (qam.alias, Convert (qam.Location));
					memberType.IsDoubleColon = true;
					memberType.MemberName = qam.Name;
					return memberType;
				}
				
				if (typeName is MemberAccess) {
					MemberAccess ma = (MemberAccess)typeName;
					
					var memberType = new MemberType ();
					memberType.AddChild (ConvertToType (ma.LeftExpression), MemberType.TargetRole);
					memberType.AddChild (new CSharpTokenNode (Convert (ma.DotLocation)), Roles.Dot);
					
					memberType.MemberNameToken = Identifier.Create (ma.Name, Convert (ma.Location));
					
					AddTypeArguments (ma, memberType);
					return memberType;
				}
				
				if (typeName is SimpleName) {
					var sn = (SimpleName)typeName;
					var result = new SimpleType (sn.Name, Convert (sn.Location));
					AddTypeArguments (sn, result);
					return result;
				}
				
				if (typeName is ComposedCast) {
					var cc = (ComposedCast)typeName;
					var baseType = ConvertToType (cc.Left);
					var result = new ComposedType () { BaseType = baseType };
					var ccSpec = cc.Spec;
					while (ccSpec != null) {
						if (ccSpec.IsNullable) {
							result.AddChild (new CSharpTokenNode (Convert (ccSpec.Location)), ComposedType.NullableRole);
						} else if (ccSpec.IsPointer) {
							result.AddChild (new CSharpTokenNode (Convert (ccSpec.Location)), ComposedType.PointerRole);
						} else {
							var location = LocationsBag.GetLocations (ccSpec);
							var spec = new ArraySpecifier () { Dimensions = ccSpec.Dimension };
							spec.AddChild (new CSharpTokenNode (Convert (ccSpec.Location)), Roles.LBracket);
							if (location != null)
								spec.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.RBracket);
							
							result.ArraySpecifiers.Add (spec);
						}
						ccSpec = ccSpec.Next;
					}
					return result;
				}
				
				if (typeName is SpecialContraintExpr) {
					var sce = (SpecialContraintExpr)typeName;
					switch (sce.Constraint) {
						case SpecialConstraint.Class:
							return new PrimitiveType ("class", Convert (sce.Location));
						case SpecialConstraint.Struct:
							return new PrimitiveType ("struct", Convert (sce.Location));
						case SpecialConstraint.Constructor:
							return new PrimitiveType ("new", Convert (sce.Location));
					}
				}
				return new SimpleType ("unknown");
			}
			
			IEnumerable<Attribute> GetAttributes (IEnumerable<Mono.CSharp.Attribute> optAttributes)
			{
				if (optAttributes == null)
					yield break;
				foreach (var attr in optAttributes) {
					Attribute result = new Attribute ();
					result.Type = ConvertToType (attr.TypeNameExpression);
					var loc = LocationsBag.GetLocations (attr);
					result.HasArgumentList = loc != null;
					int pos = 0;
					if (loc != null)
						result.AddChild (new CSharpTokenNode (Convert (loc [pos++])), Roles.LPar);
					
					if (attr.PositionalArguments != null) {
						foreach (var arg in attr.PositionalArguments) {
							var na = arg as NamedArgument;
							if (na != null) {
								var newArg = new NamedArgumentExpression ();
								newArg.AddChild (Identifier.Create (na.Name, Convert (na.Location)), Roles.Identifier);
								
								var argLoc = LocationsBag.GetLocations (na);
								if (argLoc != null)
									newArg.AddChild (new CSharpTokenNode (Convert (argLoc [0])), Roles.Colon);
								if (na.Expr != null)
									newArg.AddChild ((Expression)na.Expr.Accept (this), Roles.Expression);
								result.AddChild (newArg, Roles.Argument);
							} else {
								if (arg.Expr != null)
									result.AddChild ((Expression)arg.Expr.Accept (this), Roles.Argument);
							}
							if (loc != null && pos + 1 < loc.Count)
								result.AddChild (new CSharpTokenNode (Convert (loc [pos++])), Roles.Comma);
						}
					}
					if (attr.NamedArguments != null) {
						foreach (NamedArgument na in attr.NamedArguments) {
							var newArg = new NamedExpression ();
							newArg.AddChild (Identifier.Create (na.Name, Convert (na.Location)), Roles.Identifier);
							
							var argLoc = LocationsBag.GetLocations (na);
							if (argLoc != null)
								newArg.AddChild (new CSharpTokenNode (Convert (argLoc [0])), Roles.Assign);
							if (na.Expr != null)
								newArg.AddChild ((Expression)na.Expr.Accept (this), Roles.Expression);
							result.AddChild (newArg, Roles.Argument);
							if (loc != null && pos + 1 < loc.Count)
								result.AddChild (new CSharpTokenNode (Convert (loc [pos++])), Roles.Comma);
						}
					}
					if (loc != null && pos < loc.Count)
						result.AddChild (new CSharpTokenNode (Convert (loc [pos++])), Roles.RPar);
					
					yield return result;
				}
			}
			
			AttributeSection ConvertAttributeSection (IEnumerable<Mono.CSharp.Attribute> optAttributes)
			{
				if (optAttributes == null)
					return null;
				AttributeSection result = new AttributeSection ();
				var loc = LocationsBag.GetLocations (optAttributes);
				int pos = 0;
				if (loc != null)
					result.AddChild (new CSharpTokenNode (Convert (loc [pos++])), Roles.LBracket);
				
				var first = optAttributes.FirstOrDefault ();
				string target = first != null ? first.ExplicitTarget : null;
				
				if (!string.IsNullOrEmpty (target)) {
					if (loc != null && pos < loc.Count - 1) {
						result.AddChild (Identifier.Create (target, Convert (loc [pos++])), Roles.Identifier);
					} else {
						result.AddChild (Identifier.Create (target), Roles.Identifier);
					}
					if (loc != null && pos < loc.Count)
						result.AddChild (new CSharpTokenNode (Convert (loc [pos++])), Roles.Colon);
				}

				int attributeCount = 0;
				foreach (var attr in GetAttributes (optAttributes)) {
					result.AddChild (attr, Roles.Attribute);
					attributeCount++;
				}
				// Left and right bracket + commas between the attributes
				int locCount = 2 + attributeCount - 1;
				// optional comma
				if (loc != null && pos < loc.Count - 1 && loc.Count == locCount + 1)
					result.AddChild (new CSharpTokenNode (Convert (loc [pos++])), Roles.Comma);
				if (loc != null && pos < loc.Count)
					result.AddChild (new CSharpTokenNode (Convert (loc [pos++])), Roles.RBracket);
				return result;
			}
			
			public override void Visit(NamespaceContainer nspace)
			{
				NamespaceDeclaration nDecl = null;
				var loc = LocationsBag.GetLocations(nspace);
				
				if (nspace.NS != null && !string.IsNullOrEmpty(nspace.NS.Name)) {
					nDecl = new NamespaceDeclaration ();
					if (loc != null) {
						nDecl.AddChild(new CSharpTokenNode (Convert(loc [0])), Roles.NamespaceKeyword);
					}
					ConvertNamespaceName(nspace.RealMemberName, nDecl);
					if (loc != null && loc.Count > 1) {
						nDecl.AddChild(new CSharpTokenNode (Convert(loc [1])), Roles.LBrace);
					}
					AddToNamespace(nDecl);
					namespaceStack.Push(nDecl);
				}
				
				if (nspace.Usings != null) {
					foreach (var us in nspace.Usings) {
						us.Accept(this);
					}
				}
				
				if (nspace.Containers != null) {
					foreach (var container in nspace.Containers) {
						container.Accept(this);
					}
				}
				
				if (nDecl != null) {
					AddAttributeSection(nDecl, nspace.UnattachedAttributes, EntityDeclaration.UnattachedAttributeRole);
					if (loc != null && loc.Count > 2)
						nDecl.AddChild (new CSharpTokenNode (Convert (loc [2])), Roles.RBrace);
					if (loc != null && loc.Count > 3)
						nDecl.AddChild (new CSharpTokenNode (Convert (loc [3])), Roles.Semicolon);
					
					namespaceStack.Pop ();
				}
			}
//			public override void Visit (UsingsBag.Namespace nspace)
//			{
//
//
//				VisitNamespaceUsings (nspace);
//				VisitNamespaceBody (nspace);
//
//			}
//
			void ConvertNamespaceName (MemberName memberName, NamespaceDeclaration namespaceDecl)
			{
				AstNode insertPos = null;
				while (memberName != null) {
					Identifier newIdent = Identifier.Create (memberName.Name, Convert (memberName.Location));
					namespaceDecl.InsertChildBefore (insertPos, newIdent, Roles.Identifier);
					insertPos = newIdent;
					
					if (!memberName.DotLocation.IsNull) {
						var dotToken = new CSharpTokenNode (Convert (memberName.DotLocation));
						namespaceDecl.InsertChildBefore (insertPos, dotToken, Roles.Dot);
						insertPos = dotToken;
					}
					
					memberName = memberName.Left;
				}
			}
			
			public override void Visit (UsingNamespace un)
			{
				var ud = new UsingDeclaration ();
				var loc = LocationsBag.GetLocations (un);
				ud.AddChild (new CSharpTokenNode (Convert (un.Location)), UsingDeclaration.UsingKeywordRole);
				if (un.NamespaceExpression != null)
					ud.AddChild (ConvertToType (un.NamespaceExpression), UsingDeclaration.ImportRole);
				if (loc != null)
					ud.AddChild (new CSharpTokenNode (Convert (loc [0])), Roles.Semicolon);
				AddToNamespace (ud);
			}

			public override void Visit (UsingAliasNamespace uan)
			{
				var ud = new UsingAliasDeclaration ();
				var loc = LocationsBag.GetLocations (uan);
				
				ud.AddChild (new CSharpTokenNode (Convert (uan.Location)), UsingAliasDeclaration.UsingKeywordRole);
				ud.AddChild (Identifier.Create (uan.Alias.Value, Convert (uan.Alias.Location)), UsingAliasDeclaration.AliasRole);
				if (loc != null)
					ud.AddChild (new CSharpTokenNode (Convert (loc [0])), Roles.Assign);
				if (uan.NamespaceExpression != null)
					ud.AddChild (ConvertToType (uan.NamespaceExpression), UsingAliasDeclaration.ImportRole);
				if (loc != null && loc.Count > 1)
					ud.AddChild (new CSharpTokenNode (Convert (loc [1])), Roles.Semicolon);
				AddToNamespace (ud);
			}
			
			public override void Visit (UsingExternAlias uea)
			{
				var ud = new ExternAliasDeclaration ();
				var loc = LocationsBag.GetLocations (uea);
				ud.AddChild (new CSharpTokenNode (Convert (uea.Location)), Roles.ExternKeyword);
				if (loc != null)
					ud.AddChild (new CSharpTokenNode (Convert (loc [0])), Roles.AliasKeyword);
				ud.AddChild (Identifier.Create (uea.Alias.Value, Convert (uea.Alias.Location)), Roles.Identifier);
				if (loc != null && loc.Count > 1)
					ud.AddChild (new CSharpTokenNode (Convert (loc [1])), Roles.Semicolon);
				AddToNamespace (ud);
			}

			AstType ConvertImport (MemberName memberName)
			{
				if (memberName.Left != null) {
					// left.name
					var t = new MemberType ();
//					t.IsDoubleColon = memberName.IsDoubleColon;
					t.AddChild (ConvertImport (memberName.Left), MemberType.TargetRole);
					
					if (!memberName.DotLocation.IsNull)
						t.AddChild (new CSharpTokenNode (Convert (memberName.DotLocation)), Roles.Dot);
					
					t.AddChild (Identifier.Create (memberName.Name, Convert (memberName.Location)), Roles.Identifier);
					AddTypeArguments (t, memberName);
					return t;
				} else {
					SimpleType t = new SimpleType ();
					t.AddChild (Identifier.Create (memberName.Name, Convert (memberName.Location)), Roles.Identifier);
					AddTypeArguments (t, memberName);
					return t;
				}
			}
			
			public override void Visit (MemberCore member)
			{
				Console.WriteLine ("Unknown member:");
				Console.WriteLine (member.GetType () + "-> Member {0}", member.GetSignatureForError ());
			}
			
			Stack<TypeDeclaration> typeStack = new Stack<TypeDeclaration> ();
			
			public override void Visit(Class c)
			{
				var newType = new TypeDeclaration ();
				newType.ClassType = ClassType.Class;
				AddAttributeSection(newType, c);
				
				var location = LocationsBag.GetMemberLocation(c);
				AddModifiers(newType, location);
				int curLoc = 0;
				if (location != null && location.Count > 0)
					newType.AddChild (new CSharpTokenNode (Convert (location [curLoc++])), Roles.ClassKeyword);
				
				newType.AddChild (Identifier.Create (c.MemberName.Name, Convert (c.MemberName.Location)), Roles.Identifier);
				AddTypeParameters (newType, c.MemberName);
				
				if (c.TypeBaseExpressions != null) {
					if (location != null && curLoc < location.Count)
						newType.AddChild (new CSharpTokenNode (Convert (location [curLoc++])), Roles.Colon);
					
					var commaLocations = LocationsBag.GetLocations (c.TypeBaseExpressions);
					int i = 0;
					foreach (var baseTypes in c.TypeBaseExpressions) {
						newType.AddChild (ConvertToType (baseTypes), Roles.BaseType);
						if (commaLocations != null && i < commaLocations.Count) {
							newType.AddChild (new CSharpTokenNode (Convert (commaLocations [i])), Roles.Comma);
							i++;
						}
					}
				}
				
				AddConstraints (newType, c.CurrentTypeParameters);
				if (location != null && curLoc < location.Count)
					newType.AddChild (new CSharpTokenNode (Convert (location [curLoc++])), Roles.LBrace);
				typeStack.Push (newType);
				base.Visit (c);
				AddAttributeSection (newType, c.UnattachedAttributes, EntityDeclaration.UnattachedAttributeRole);
				
				if (location != null && curLoc < location.Count) {
					newType.AddChild (new CSharpTokenNode (Convert (location [curLoc++])), Roles.RBrace);
					
					if (location != null && curLoc < location.Count)
						newType.AddChild (new CSharpTokenNode (Convert (location [curLoc++])), Roles.Semicolon);
					
				} else {
					// parser error, set end node to max value.
					newType.AddChild (new ErrorNode (), Roles.Error);
				}
				typeStack.Pop ();
				AddType (newType);
			}
			
			public override void Visit(Struct s)
			{
				var newType = new TypeDeclaration();
				newType.ClassType = ClassType.Struct;
				AddAttributeSection(newType, s);
				var location = LocationsBag.GetMemberLocation(s);
				AddModifiers(newType, location);
				int curLoc = 0;
				if (location != null && location.Count > 0)
					newType.AddChild (new CSharpTokenNode (Convert (location [curLoc++])), Roles.StructKeyword);
				newType.AddChild (Identifier.Create (s.MemberName.Name, Convert (s.MemberName.Location)), Roles.Identifier);
				AddTypeParameters (newType, s.MemberName);
				
				if (s.TypeBaseExpressions != null) {
					if (location != null && curLoc < location.Count)
						newType.AddChild (new CSharpTokenNode (Convert (location [curLoc++])), Roles.Colon);
					var commaLocations = LocationsBag.GetLocations (s.TypeBaseExpressions);
					int i = 0;
					foreach (var baseTypes in s.TypeBaseExpressions) {
						newType.AddChild (ConvertToType (baseTypes), Roles.BaseType);
						if (commaLocations != null && i < commaLocations.Count) {
							newType.AddChild (new CSharpTokenNode (Convert (commaLocations [i])), Roles.Comma);
							i++;
						}
					}
				}
				
				AddConstraints (newType, s.CurrentTypeParameters);
				if (location != null && curLoc < location.Count)
					newType.AddChild (new CSharpTokenNode (Convert (location [curLoc++])), Roles.LBrace);
				typeStack.Push (newType);
				base.Visit (s);
				if (location != null && location.Count > 2) {
					if (location != null && curLoc < location.Count)
						newType.AddChild (new CSharpTokenNode (Convert (location [curLoc++])), Roles.RBrace);
					if (location != null && curLoc < location.Count)
						newType.AddChild (new CSharpTokenNode (Convert (location [curLoc++])), Roles.Semicolon);
				} else {
					// parser error, set end node to max value.
					newType.AddChild (new ErrorNode (), Roles.Error);
				}
				typeStack.Pop ();
				AddType (newType);
			}
			
			public override void Visit(Interface i)
			{
				var newType = new TypeDeclaration();
				newType.ClassType = ClassType.Interface;
				AddAttributeSection(newType, i);
				var location = LocationsBag.GetMemberLocation(i);
				AddModifiers(newType, location);
				int curLoc = 0;
				if (location != null && location.Count > 0)
					newType.AddChild (new CSharpTokenNode (Convert (location [curLoc++])), Roles.InterfaceKeyword);
				newType.AddChild (Identifier.Create (i.MemberName.Name, Convert (i.MemberName.Location)), Roles.Identifier);
				AddTypeParameters (newType, i.MemberName);
				
				if (i.TypeBaseExpressions != null) {
					if (location != null && curLoc < location.Count)
						newType.AddChild (new CSharpTokenNode (Convert (location [curLoc++])), Roles.Colon);
					var commaLocations = LocationsBag.GetLocations (i.TypeBaseExpressions);
					int j = 0;
					foreach (var baseTypes in i.TypeBaseExpressions) {
						newType.AddChild (ConvertToType (baseTypes), Roles.BaseType);
						if (commaLocations != null && j < commaLocations.Count) {
							newType.AddChild (new CSharpTokenNode (Convert (commaLocations [j])), Roles.Comma);
							j++;
						}
					}
				}
				
				AddConstraints (newType, i.CurrentTypeParameters);
				if (location != null && curLoc < location.Count)
					newType.AddChild (new CSharpTokenNode (Convert (location [curLoc++])), Roles.LBrace);
				typeStack.Push (newType);
				base.Visit (i);
				if (location != null && location.Count > 2) {
					if (location != null && curLoc < location.Count)
						newType.AddChild (new CSharpTokenNode (Convert (location [curLoc++])), Roles.RBrace);
					if (location != null && curLoc < location.Count)
						newType.AddChild (new CSharpTokenNode (Convert (location [curLoc++])), Roles.Semicolon);
				} else {
					// parser error, set end node to max value.
					newType.AddChild (new ErrorNode (), Roles.Error);
				}
				typeStack.Pop ();
				AddType (newType);
			}
			
			public override void Visit(Mono.CSharp.Delegate d)
			{
				DelegateDeclaration newDelegate = new DelegateDeclaration ();
				var location = LocationsBag.GetMemberLocation(d);
				AddAttributeSection(newDelegate, d);
				AddModifiers(newDelegate, location);
				if (location != null && location.Count > 0) {
					newDelegate.AddChild(new CSharpTokenNode (Convert(location [0])), Roles.DelegateKeyword);
				}
				if (d.ReturnType != null)
					newDelegate.AddChild (ConvertToType (d.ReturnType), Roles.Type);
				newDelegate.AddChild (Identifier.Create (d.MemberName.Name, Convert (d.MemberName.Location)), Roles.Identifier);
				AddTypeParameters (newDelegate, d.MemberName);
				
				if (location != null && location.Count > 1)
					newDelegate.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.LPar);
				AddParameter (newDelegate, d.Parameters);
				
				if (location != null && location.Count > 2) {
					newDelegate.AddChild (new CSharpTokenNode (Convert (location [2])), Roles.RPar);
				}
				AddConstraints (newDelegate, d.CurrentTypeParameters);
				if (location != null && location.Count > 3) {
					newDelegate.AddChild (new CSharpTokenNode (Convert (location [3])), Roles.Semicolon);
				}
				AddType (newDelegate);
			}
			
			void AddType (EntityDeclaration child)
			{
				if (typeStack.Count > 0) {
					typeStack.Peek ().AddChild (child, Roles.TypeMemberRole);
				} else {
					AddToNamespace (child);
				}
			}
			
			void AddToNamespace (AstNode child)
			{
				if (namespaceStack.Count > 0) {
					namespaceStack.Peek ().AddChild (child, NamespaceDeclaration.MemberRole);
				} else {
					unit.AddChild (child, SyntaxTree.MemberRole);
				}
			}
			
			public override void Visit(Mono.CSharp.Enum e)
			{
				var newType = new TypeDeclaration();
				newType.ClassType = ClassType.Enum;
				AddAttributeSection(newType, e);
				var location = LocationsBag.GetMemberLocation(e);
				
				AddModifiers(newType, location);
				int curLoc = 0;
				if (location != null && location.Count > 0)
					newType.AddChild(new CSharpTokenNode(Convert(location [curLoc++])), Roles.EnumKeyword);
				newType.AddChild(Identifier.Create(e.MemberName.Name, Convert(e.MemberName.Location)), Roles.Identifier);
				
				if (e.BaseTypeExpression != null) {
					if (location != null && curLoc < location.Count)
						newType.AddChild(new CSharpTokenNode(Convert(location [curLoc++])), Roles.Colon);
					newType.AddChild(ConvertToType(e.BaseTypeExpression), Roles.BaseType);
				}

				if (location != null && curLoc < location.Count)
					newType.AddChild(new CSharpTokenNode(Convert(location [curLoc++])), Roles.LBrace);
				typeStack.Push(newType);
				
				foreach (var m in e.Members) {
					EnumMember member = m as EnumMember;
					if (member == null) {
						Console.WriteLine("WARNING - ENUM MEMBER: " + m);
						continue;
					}
					Visit(member);
					if (location != null && curLoc < location.Count - 1) //last one is closing brace
						newType.AddChild(new CSharpTokenNode(Convert(location [curLoc++])), Roles.Comma);
				}
				
				if (location != null && location.Count > 2) {
					if (location != null && curLoc < location.Count)
						newType.AddChild (new CSharpTokenNode (Convert (location [curLoc++])), Roles.RBrace);
					if (location != null && curLoc < location.Count)
						newType.AddChild (new CSharpTokenNode (Convert (location [curLoc++])), Roles.Semicolon);
				} else {
					// parser error, set end node to max value.
					newType.AddChild (new ErrorNode (), Roles.Error);
				}
				typeStack.Pop ();
				AddType (newType);
			}
			
			public override void Visit (EnumMember em)
			{
				EnumMemberDeclaration newField = new EnumMemberDeclaration ();
				AddAttributeSection (newField, em);
				newField.AddChild (Identifier.Create (em.Name, Convert (em.Location)), Roles.Identifier);
				if (em.Initializer != null) {
					newField.AddChild (new CSharpTokenNode (Convert (em.Initializer.Location)), Roles.Assign);
					newField.AddChild ((Expression)em.Initializer.Accept (this), EnumMemberDeclaration.InitializerRole);
				}
				//Console.WriteLine (newField.StartLocation +"-" + newField.EndLocation);
				
				typeStack.Peek ().AddChild (newField, Roles.TypeMemberRole);
			}
			#endregion
			
			#region Type members
			
			
			public override void Visit (FixedField f)
			{
				var location = LocationsBag.GetMemberLocation (f);
				int locationIdx = 0;
				
				var newField = new FixedFieldDeclaration ();
				AddAttributeSection (newField, f);
				AddModifiers (newField, location);
				if (location != null && location.Count > 0)
					newField.AddChild (new CSharpTokenNode (Convert (location [locationIdx++])), FixedFieldDeclaration.FixedKeywordRole);

				if (f.TypeExpression != null)
					newField.AddChild (ConvertToType (f.TypeExpression), Roles.Type);
				
				var variable = new FixedVariableInitializer ();
				variable.AddChild (Identifier.Create (f.MemberName.Name, Convert (f.MemberName.Location)), Roles.Identifier);
				if (f.Initializer != null && !f.Initializer.IsNull) {
					var bracketLocations = LocationsBag.GetLocations (f.Initializer);
					if (bracketLocations != null && bracketLocations.Count > 1)
						variable.AddChild (new CSharpTokenNode (Convert (bracketLocations [0])), Roles.LBracket);
					
					variable.AddChild ((Expression)f.Initializer.Accept (this), Roles.Expression);
					if (bracketLocations != null && bracketLocations.Count > 1)
						variable.AddChild (new CSharpTokenNode (Convert (bracketLocations [0])), Roles.RBracket);
				}
				newField.AddChild (variable, FixedFieldDeclaration.VariableRole);
				
				if (f.Declarators != null) {
					foreach (var decl in f.Declarators) {
						var declLoc = LocationsBag.GetLocations (decl);
						if (declLoc != null)
							newField.AddChild (new CSharpTokenNode (Convert (declLoc [0])), Roles.Comma);
						
						variable = new FixedVariableInitializer ();
						variable.AddChild (Identifier.Create (decl.Name.Value, Convert (decl.Name.Location)), Roles.Identifier);
						if (!decl.Initializer.IsNull) {
							var bracketLocations = LocationsBag.GetLocations (f.Initializer);
							if (bracketLocations != null && bracketLocations.Count > 1)
								variable.AddChild (new CSharpTokenNode (Convert (bracketLocations [0])), Roles.LBracket);
							variable.AddChild ((Expression)decl.Initializer.Accept (this), Roles.Expression);
							if (bracketLocations != null && bracketLocations.Count > 1)
								variable.AddChild (new CSharpTokenNode (Convert (bracketLocations [0])), Roles.RBracket);
						}
						newField.AddChild (variable, FixedFieldDeclaration.VariableRole);
					}
				}
				if (location != null && location.Count > locationIdx)
					newField.AddChild (new CSharpTokenNode (Convert (location [locationIdx])), Roles.Semicolon);
				typeStack.Peek ().AddChild (newField, Roles.TypeMemberRole);
				
			}

			public override void Visit(Field f)
			{
				var location = LocationsBag.GetMemberLocation(f);
				
				FieldDeclaration newField = new FieldDeclaration ();
				AddAttributeSection (newField, f);
				AddModifiers (newField, location);
				newField.AddChild (ConvertToType (f.TypeExpression), Roles.Type);
				
				VariableInitializer variable = new VariableInitializer ();
				variable.AddChild (Identifier.Create (f.MemberName.Name, Convert (f.MemberName.Location)), Roles.Identifier);
				int locationIdx = 0;
				if (f.Initializer != null) {
					if (location != null)
						variable.AddChild (new CSharpTokenNode (Convert (location [locationIdx++])), Roles.Assign);
					variable.AddChild ((Expression)f.Initializer.Accept (this), Roles.Expression);
				}
				newField.AddChild (variable, Roles.Variable);
				if (f.Declarators != null) {
					foreach (var decl in f.Declarators) {
						var declLoc = LocationsBag.GetLocations (decl);
						if (declLoc != null)
							newField.AddChild (new CSharpTokenNode (Convert (declLoc [0])), Roles.Comma);
						
						variable = new VariableInitializer ();
						variable.AddChild (Identifier.Create (decl.Name.Value, Convert (decl.Name.Location)), Roles.Identifier);
						if (decl.Initializer != null) {
							if (declLoc != null)
								variable.AddChild (new CSharpTokenNode (Convert (declLoc [1])), Roles.Assign);
							variable.AddChild ((Expression)decl.Initializer.Accept (this), Roles.Expression);
						}
						newField.AddChild (variable, Roles.Variable);
					}
				}
				if (location != null &&location.Count > locationIdx)
					newField.AddChild (new CSharpTokenNode (Convert (location [locationIdx++])), Roles.Semicolon);

				typeStack.Peek ().AddChild (newField, Roles.TypeMemberRole);
			}
			
			public override void Visit (Const f)
			{
				var location = LocationsBag.GetMemberLocation (f);
				
				FieldDeclaration newField = new FieldDeclaration ();
				AddAttributeSection (newField, f);
				AddModifiers (newField, location);
				if (location != null)
					newField.AddChild (new CSharpModifierToken (Convert (location [0]), Modifiers.Const), EntityDeclaration.ModifierRole);
				newField.AddChild (ConvertToType (f.TypeExpression), Roles.Type);
				
				VariableInitializer variable = new VariableInitializer ();
				variable.AddChild (Identifier.Create (f.MemberName.Name, Convert (f.MemberName.Location)), Roles.Identifier);
				
				if (f.Initializer != null) {
					variable.AddChild (new CSharpTokenNode (Convert (f.Initializer.Location)), Roles.Assign);
					variable.AddChild ((Expression)f.Initializer.Accept (this), Roles.Expression);
				}
				newField.AddChild (variable, Roles.Variable);
				if (f.Declarators != null) {
					foreach (var decl in f.Declarators) {
						var declLoc = LocationsBag.GetLocations (decl);
						if (declLoc != null)
							newField.AddChild (new CSharpTokenNode (Convert (declLoc [0])), Roles.Comma);
						
						variable = new VariableInitializer ();
						variable.AddChild (Identifier.Create (decl.Name.Value, Convert (decl.Name.Location)), Roles.Identifier);
						if (decl.Initializer != null) {
							variable.AddChild (new CSharpTokenNode (Convert (decl.Initializer.Location)), Roles.Assign);
							variable.AddChild ((Expression)decl.Initializer.Accept (this), Roles.Expression);
						}
						newField.AddChild (variable, Roles.Variable);
					}
				}
				if (location != null)
					newField.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.Semicolon);
				
				typeStack.Peek ().AddChild (newField, Roles.TypeMemberRole);

				
			}
			
			public override void Visit (Operator o)
			{
				OperatorDeclaration newOperator = new OperatorDeclaration ();
				newOperator.OperatorType = (OperatorType)o.OperatorType;
				
				var location = LocationsBag.GetMemberLocation (o);
				AddAttributeSection (newOperator, o);
				AddModifiers (newOperator, location);
				
				
				if (o.OperatorType == Operator.OpType.Implicit) {
					if (location != null && location.Count > 0) {
						newOperator.AddChild (new CSharpTokenNode (Convert (location [0])), OperatorDeclaration.ImplicitRole);
						if (location.Count > 1)
							newOperator.AddChild (new CSharpTokenNode (Convert (location [1])), OperatorDeclaration.OperatorKeywordRole);
					}
					newOperator.AddChild (ConvertToType (o.TypeExpression), Roles.Type);
				} else if (o.OperatorType == Operator.OpType.Explicit) {
					if (location != null && location.Count > 0) {
						newOperator.AddChild (new CSharpTokenNode (Convert (location [0])), OperatorDeclaration.ExplicitRole);
						if (location.Count > 1)
							newOperator.AddChild (new CSharpTokenNode (Convert (location [1])), OperatorDeclaration.OperatorKeywordRole);
					}
					newOperator.AddChild (ConvertToType (o.TypeExpression), Roles.Type);
				} else {
					newOperator.AddChild (ConvertToType (o.TypeExpression), Roles.Type);

					if (location != null && location.Count > 0)
						newOperator.AddChild (new CSharpTokenNode (Convert (location [0])), OperatorDeclaration.OperatorKeywordRole);
					
					if (location != null && location.Count > 1)
						newOperator.AddChild (new CSharpTokenNode (Convert (location [1])), OperatorDeclaration.GetRole (newOperator.OperatorType));
				}
				if (location != null && location.Count > 2)
					newOperator.AddChild (new CSharpTokenNode (Convert (location [2])), Roles.LPar);
				AddParameter (newOperator, o.ParameterInfo);
				if (location != null && location.Count > 3)
					newOperator.AddChild (new CSharpTokenNode (Convert (location [3])), Roles.RPar);
				
				if (o.Block != null) {
					newOperator.AddChild ((BlockStatement)o.Block.Accept (this), Roles.Body);
				} else {
					if (location != null && location.Count >= 5)
						newOperator.AddChild (new CSharpTokenNode (Convert (location [4])), Roles.Semicolon);
				}
				typeStack.Peek ().AddChild (newOperator, Roles.TypeMemberRole);
			}
			
			public void AddAttributeSection (AstNode parent, Attributable a)
			{
				if (a == null || a.OptAttributes == null)
					return;
				AddAttributeSection (parent, a.OptAttributes);
			}
			
			public void AddAttributeSection (AstNode parent, Attributes attrs, Role role)
			{
				if (attrs == null)
					return;
				foreach (var attr in attrs.Sections) {
					parent.AddChild (ConvertAttributeSection (attr), EntityDeclaration.AttributeRole);
				}
			}

			public void AddAttributeSection (AstNode parent, Attributes attrs)
			{
				AddAttributeSection (parent, attrs, EntityDeclaration.AttributeRole);
			}
			
			public override void Visit (Indexer indexer)
			{
				IndexerDeclaration newIndexer = new IndexerDeclaration ();
				AddAttributeSection (newIndexer, indexer);
				var location = LocationsBag.GetMemberLocation (indexer);
				AddModifiers (newIndexer, location);
				newIndexer.AddChild (ConvertToType (indexer.TypeExpression), Roles.Type);
				AddExplicitInterface (newIndexer, indexer.MemberName);
				var name = indexer.MemberName;
				newIndexer.AddChild (Identifier.Create ("this", Convert (name.Location)), Roles.Identifier);
				
				if (location != null && location.Count > 0)
					newIndexer.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.LBracket);
				AddParameter (newIndexer, indexer.ParameterInfo);
				if (location != null && location.Count > 1)
					newIndexer.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.RBracket);
				
				if (location != null && location.Count > 2)
					newIndexer.AddChild (new CSharpTokenNode (Convert (location [2])), Roles.LBrace);
				if (indexer.Get != null) {
					Accessor getAccessor = new Accessor ();
					var getLocation = LocationsBag.GetMemberLocation (indexer.Get);
					AddAttributeSection (getAccessor, indexer.Get);
					AddModifiers (getAccessor, getLocation);
					if (getLocation != null)
						getAccessor.AddChild (new CSharpTokenNode (Convert (indexer.Get.Location)), PropertyDeclaration.GetKeywordRole);
					if (indexer.Get.Block != null) {
						getAccessor.AddChild ((BlockStatement)indexer.Get.Block.Accept (this), Roles.Body);
					} else {
						if (getLocation != null && getLocation.Count > 0)
							newIndexer.AddChild (new CSharpTokenNode (Convert (getLocation [0])), Roles.Semicolon);
					}
					newIndexer.AddChild (getAccessor, PropertyDeclaration.GetterRole);
				}
				
				if (indexer.Set != null) {
					Accessor setAccessor = new Accessor ();
					var setLocation = LocationsBag.GetMemberLocation (indexer.Set);
					AddAttributeSection (setAccessor, indexer.Set);
					AddModifiers (setAccessor, setLocation);
					if (setLocation != null)
						setAccessor.AddChild (new CSharpTokenNode (Convert (indexer.Set.Location)), PropertyDeclaration.SetKeywordRole);
					
					if (indexer.Set.Block != null) {
						setAccessor.AddChild ((BlockStatement)indexer.Set.Block.Accept (this), Roles.Body);
					} else {
						if (setLocation != null && setLocation.Count > 0)
							newIndexer.AddChild (new CSharpTokenNode (Convert (setLocation [0])), Roles.Semicolon);
					}
					newIndexer.AddChild (setAccessor, PropertyDeclaration.SetterRole);
				}
				
				if (location != null) {
					if (location.Count > 3)
						newIndexer.AddChild (new CSharpTokenNode (Convert (location [3])), Roles.RBrace);
				} else {
					// parser error, set end node to max value.
					newIndexer.AddChild (new ErrorNode (), Roles.Error);
				}
				typeStack.Peek ().AddChild (newIndexer, Roles.TypeMemberRole);
			}
			
			public override void Visit (Method m)
			{
				MethodDeclaration newMethod = new MethodDeclaration ();
				AddAttributeSection (newMethod, m);
				var location = LocationsBag.GetMemberLocation (m);
				AddModifiers (newMethod, location);
				newMethod.AddChild (ConvertToType (m.TypeExpression), Roles.Type);
				AddExplicitInterface (newMethod, m.MethodName);
				newMethod.AddChild (Identifier.Create (m.MethodName.Name, Convert (m.Location)), Roles.Identifier);
				
				AddTypeParameters (newMethod, m.MemberName);
				
				if (location != null && location.Count > 0)
					newMethod.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.LPar);
				AddParameter (newMethod, m.ParameterInfo);
				
				if (location != null && location.Count > 1)
					newMethod.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.RPar);
				
				AddConstraints (newMethod, m.CurrentTypeParameters);
				
				if (m.Block != null) {
					var bodyBlock = (BlockStatement)m.Block.Accept (this);
//					if (m.Block is ToplevelBlock) {
//						newMethod.AddChild (bodyBlock.FirstChild.NextSibling, Roles.Body);
//					} else {
					newMethod.AddChild (bodyBlock, Roles.Body);
//					}
				} else if (location != null) {
					if (location.Count < 3) {
						// parser error, set end node to max value.
						newMethod.AddChild (new ErrorNode (), Roles.Error);
					} else {
						newMethod.AddChild (new CSharpTokenNode (Convert (location [2])), Roles.Semicolon);
					}
				}
				typeStack.Peek ().AddChild (newMethod, Roles.TypeMemberRole);
			}
			
			static Dictionary<Mono.CSharp.Modifiers, ICSharpCode.NRefactory.CSharp.Modifiers> modifierTable = new Dictionary<Mono.CSharp.Modifiers, ICSharpCode.NRefactory.CSharp.Modifiers> ();
			static string[] keywordTable;
			
			static ConversionVisitor ()
			{
				modifierTable [Mono.CSharp.Modifiers.NEW] = ICSharpCode.NRefactory.CSharp.Modifiers.New;
				modifierTable [Mono.CSharp.Modifiers.PUBLIC] = ICSharpCode.NRefactory.CSharp.Modifiers.Public;
				modifierTable [Mono.CSharp.Modifiers.PROTECTED] = ICSharpCode.NRefactory.CSharp.Modifiers.Protected;
				modifierTable [Mono.CSharp.Modifiers.PRIVATE] = ICSharpCode.NRefactory.CSharp.Modifiers.Private;
				modifierTable [Mono.CSharp.Modifiers.INTERNAL] = ICSharpCode.NRefactory.CSharp.Modifiers.Internal;
				modifierTable [Mono.CSharp.Modifiers.ABSTRACT] = ICSharpCode.NRefactory.CSharp.Modifiers.Abstract;
				modifierTable [Mono.CSharp.Modifiers.VIRTUAL] = ICSharpCode.NRefactory.CSharp.Modifiers.Virtual;
				modifierTable [Mono.CSharp.Modifiers.SEALED] = ICSharpCode.NRefactory.CSharp.Modifiers.Sealed;
				modifierTable [Mono.CSharp.Modifiers.STATIC] = ICSharpCode.NRefactory.CSharp.Modifiers.Static;
				modifierTable [Mono.CSharp.Modifiers.OVERRIDE] = ICSharpCode.NRefactory.CSharp.Modifiers.Override;
				modifierTable [Mono.CSharp.Modifiers.READONLY] = ICSharpCode.NRefactory.CSharp.Modifiers.Readonly;
				modifierTable [Mono.CSharp.Modifiers.PARTIAL] = ICSharpCode.NRefactory.CSharp.Modifiers.Partial;
				modifierTable [Mono.CSharp.Modifiers.EXTERN] = ICSharpCode.NRefactory.CSharp.Modifiers.Extern;
				modifierTable [Mono.CSharp.Modifiers.VOLATILE] = ICSharpCode.NRefactory.CSharp.Modifiers.Volatile;
				modifierTable [Mono.CSharp.Modifiers.UNSAFE] = ICSharpCode.NRefactory.CSharp.Modifiers.Unsafe;
				modifierTable [Mono.CSharp.Modifiers.ASYNC] = ICSharpCode.NRefactory.CSharp.Modifiers.Async;
				
				keywordTable = new string[255];
				for (int i = 0; i< keywordTable.Length; i++)
					keywordTable [i] = "unknown";
				
				keywordTable [(int)BuiltinTypeSpec.Type.Other] = "void";
				keywordTable [(int)BuiltinTypeSpec.Type.String] = "string";
				keywordTable [(int)BuiltinTypeSpec.Type.Int] = "int";
				keywordTable [(int)BuiltinTypeSpec.Type.Object] = "object";
				keywordTable [(int)BuiltinTypeSpec.Type.Float] = "float";
				keywordTable [(int)BuiltinTypeSpec.Type.Double] = "double";
				keywordTable [(int)BuiltinTypeSpec.Type.Long] = "long";
				keywordTable [(int)BuiltinTypeSpec.Type.Byte] = "byte";
				keywordTable [(int)BuiltinTypeSpec.Type.UInt] = "uint";
				keywordTable [(int)BuiltinTypeSpec.Type.ULong] = "ulong";
				keywordTable [(int)BuiltinTypeSpec.Type.Short] = "short";
				keywordTable [(int)BuiltinTypeSpec.Type.UShort] = "ushort";
				keywordTable [(int)BuiltinTypeSpec.Type.SByte] = "sbyte";
				keywordTable [(int)BuiltinTypeSpec.Type.Decimal] = "decimal";
				keywordTable [(int)BuiltinTypeSpec.Type.Char] = "char";
				keywordTable [(int)BuiltinTypeSpec.Type.Bool] = "bool";
			}
			
			void AddModifiers (EntityDeclaration parent, LocationsBag.MemberLocations location)
			{
				if (location == null || location.Modifiers == null)
					return;
				foreach (var modifier in location.Modifiers) {
					ICSharpCode.NRefactory.CSharp.Modifiers mod;
					if (!modifierTable.TryGetValue (modifier.Item1, out mod)) {
						Console.WriteLine ("modifier " + modifier.Item1 + " can't be converted,");
					}
					
					parent.AddChild (new CSharpModifierToken (Convert (modifier.Item2), mod), EntityDeclaration.ModifierRole);
				}
			}
			
			public override void Visit (Property p)
			{
				PropertyDeclaration newProperty = new PropertyDeclaration ();
				AddAttributeSection (newProperty, p);
				var location = LocationsBag.GetMemberLocation (p);
				AddModifiers (newProperty, location);
				newProperty.AddChild (ConvertToType (p.TypeExpression), Roles.Type);
				AddExplicitInterface (newProperty, p.MemberName);
				newProperty.AddChild (Identifier.Create (p.MemberName.Name, Convert (p.Location)), Roles.Identifier);
				
				if (location != null && location.Count > 0)
					newProperty.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.LBrace);
				
				Accessor getAccessor = null;
				if (p.Get != null) {
					getAccessor = new Accessor ();
					AddAttributeSection (getAccessor, p.Get);
					var getLocation = LocationsBag.GetMemberLocation (p.Get);
					AddModifiers (getAccessor, getLocation);
					getAccessor.AddChild (new CSharpTokenNode (Convert (p.Get.Location)), PropertyDeclaration.GetKeywordRole);
					
					if (p.Get.Block != null) {
						getAccessor.AddChild ((BlockStatement)p.Get.Block.Accept (this), Roles.Body);
					} else {
						if (getLocation != null && getLocation.Count > 0)
							getAccessor.AddChild (new CSharpTokenNode (Convert (getLocation [0])), Roles.Semicolon);
					}
				}
				
				Accessor setAccessor = null;
				if (p.Set != null) {
					setAccessor = new Accessor ();
					AddAttributeSection (setAccessor, p.Set);
					var setLocation = LocationsBag.GetMemberLocation (p.Set);
					AddModifiers (setAccessor, setLocation);
					setAccessor.AddChild (new CSharpTokenNode (Convert (p.Set.Location)), PropertyDeclaration.SetKeywordRole);
					
					if (p.Set.Block != null) {
						setAccessor.AddChild ((BlockStatement)p.Set.Block.Accept (this), Roles.Body);
					} else {
						if (setLocation != null && setLocation.Count > 0)
							setAccessor.AddChild (new CSharpTokenNode (Convert (setLocation [0])), Roles.Semicolon);
					}
				}
				if (getAccessor != null && setAccessor != null) {
					if (getAccessor.StartLocation < setAccessor.StartLocation) {
						newProperty.AddChild (getAccessor, PropertyDeclaration.GetterRole);
						newProperty.AddChild (setAccessor, PropertyDeclaration.SetterRole);
					} else {
						newProperty.AddChild (setAccessor, PropertyDeclaration.SetterRole);
						newProperty.AddChild (getAccessor, PropertyDeclaration.GetterRole);
					}
				} else {
					if (getAccessor != null)
						newProperty.AddChild (getAccessor, PropertyDeclaration.GetterRole);
					if (setAccessor != null)
						newProperty.AddChild (setAccessor, PropertyDeclaration.SetterRole);
				}
				
				if (location != null && location.Count > 1) {
					newProperty.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.RBrace);
				} else {
					// parser error, set end node to max value.
					newProperty.AddChild (new ErrorNode (), Roles.Error);
				}
				
				typeStack.Peek ().AddChild (newProperty, Roles.TypeMemberRole);
			}
			
			public override void Visit (Constructor c)
			{
				ConstructorDeclaration newConstructor = new ConstructorDeclaration ();
				AddAttributeSection (newConstructor, c);
				var location = LocationsBag.GetMemberLocation (c);
				AddModifiers (newConstructor, location);
				newConstructor.AddChild (Identifier.Create (c.MemberName.Name, Convert (c.MemberName.Location)), Roles.Identifier);
				if (location != null && location.Count > 0)
					newConstructor.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.LPar);
				
				AddParameter (newConstructor, c.ParameterInfo);
				if (location != null && location.Count > 1)
					newConstructor.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.RPar);
				
				if (c.Initializer != null) {
					var initializer = new ConstructorInitializer ();
					initializer.ConstructorInitializerType = c.Initializer is ConstructorBaseInitializer ? ConstructorInitializerType.Base : ConstructorInitializerType.This;
					var initializerLocation = LocationsBag.GetLocations (c.Initializer);
					
					if (initializerLocation != null)
						newConstructor.AddChild (new CSharpTokenNode (Convert (initializerLocation [0])), Roles.Colon);
					
					if (initializerLocation != null && initializerLocation.Count > 1) {
						// this and base has the same length
						initializer.AddChild (new CSharpTokenNode (Convert (c.Initializer.Location)), initializer.ConstructorInitializerType == ConstructorInitializerType.This ? ConstructorInitializer.ThisKeywordRole : ConstructorInitializer.BaseKeywordRole);
						initializer.AddChild (new CSharpTokenNode (Convert (initializerLocation [1])), Roles.LPar);
						AddArguments (initializer, LocationsBag.GetLocations (c.Initializer.Arguments), c.Initializer.Arguments);
						initializer.AddChild (new CSharpTokenNode (Convert (initializerLocation [2])), Roles.RPar);
						newConstructor.AddChild (initializer, ConstructorDeclaration.InitializerRole);
					}
				}
				
				if (c.Block != null)
					newConstructor.AddChild ((BlockStatement)c.Block.Accept (this), Roles.Body);
				typeStack.Peek ().AddChild (newConstructor, Roles.TypeMemberRole);
			}
			
			public override void Visit (Destructor d)
			{
				DestructorDeclaration newDestructor = new DestructorDeclaration ();
				AddAttributeSection (newDestructor, d);
				var location = LocationsBag.GetMemberLocation (d);
				AddModifiers (newDestructor, location);
				if (location != null && location.Count > 0)
					newDestructor.AddChild (new CSharpTokenNode (Convert (location [0])), DestructorDeclaration.TildeRole);
				newDestructor.AddChild (Identifier.Create (d.Identifier, Convert (d.MemberName.Location)), Roles.Identifier);
				
				if (location != null && location.Count > 1) {
					newDestructor.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.LPar);
					
					if (location.Count > 2)
						newDestructor.AddChild (new CSharpTokenNode (Convert (location [2])), Roles.RPar);
				}
				
				if (d.Block != null)
					newDestructor.AddChild ((BlockStatement)d.Block.Accept (this), Roles.Body);
				
				typeStack.Peek ().AddChild (newDestructor, Roles.TypeMemberRole);
			}
			
			public override void Visit (EventField e)
			{
				EventDeclaration newEvent = new EventDeclaration ();
				AddAttributeSection (newEvent, e);
				var location = LocationsBag.GetMemberLocation (e);
				AddModifiers (newEvent, location);
				
				if (location != null && location.Count > 0)
					newEvent.AddChild (new CSharpTokenNode (Convert (location [0])), EventDeclaration.EventKeywordRole);
				newEvent.AddChild (ConvertToType (e.TypeExpression), Roles.Type);
				
				VariableInitializer variable = new VariableInitializer ();
				variable.AddChild (Identifier.Create (e.MemberName.Name, Convert (e.MemberName.Location)), Roles.Identifier);
				
				if (e.Initializer != null) {
					if (location != null && location.Count > 0)
						variable.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.Assign);
					variable.AddChild ((Expression)e.Initializer.Accept (this), Roles.Expression);
				}
				newEvent.AddChild (variable, Roles.Variable);
				if (e.Declarators != null) {
					foreach (var decl in e.Declarators) {
						var declLoc = LocationsBag.GetLocations (decl);
						if (declLoc != null)
							newEvent.AddChild (new CSharpTokenNode (Convert (declLoc [0])), Roles.Comma);
						
						variable = new VariableInitializer ();
						variable.AddChild (Identifier.Create (decl.Name.Value, Convert (decl.Name.Location)), Roles.Identifier);

						if (decl.Initializer != null) {
							if (declLoc != null)
								variable.AddChild (new CSharpTokenNode (Convert (declLoc [1])), Roles.Assign);
							variable.AddChild ((Expression)decl.Initializer.Accept (this), Roles.Expression);
						}
						newEvent.AddChild (variable, Roles.Variable);
					}
				}
				
				if (location != null && location.Count > 1)
					newEvent.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.Semicolon);
				
				typeStack.Peek ().AddChild (newEvent, Roles.TypeMemberRole);
			}

			void AddExplicitInterface (AstNode parent, MemberName memberName)
			{
				if (memberName == null || memberName.ExplicitInterface == null)
					return;
				
				parent.AddChild (ConvertToType (memberName.ExplicitInterface), EntityDeclaration.PrivateImplementationTypeRole);
				var privateImplTypeLoc = LocationsBag.GetLocations (memberName.ExplicitInterface);
				if (privateImplTypeLoc != null)
					parent.AddChild (new CSharpTokenNode (Convert (privateImplTypeLoc [0])), Roles.Dot);
			}
			
			public override void Visit (EventProperty ep)
			{
				CustomEventDeclaration newEvent = new CustomEventDeclaration ();
				AddAttributeSection (newEvent, ep);
				var location = LocationsBag.GetMemberLocation (ep);
				AddModifiers (newEvent, location);
				
				if (location != null && location.Count > 0)
					newEvent.AddChild (new CSharpTokenNode (Convert (location [0])), CustomEventDeclaration.EventKeywordRole);
				newEvent.AddChild (ConvertToType (ep.TypeExpression), Roles.Type);
				
				AddExplicitInterface (newEvent, ep.MemberName);
				
				newEvent.AddChild (Identifier.Create (ep.MemberName.Name, Convert (ep.Location)), Roles.Identifier);

				if (location != null && location.Count >= 2)
					newEvent.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.LBrace);
				
				if (ep.Add != null) {
					Accessor addAccessor = new Accessor ();
					AddAttributeSection (addAccessor, ep.Add);
					var addLocation = LocationsBag.GetMemberLocation (ep.Add);
					AddModifiers (addAccessor, addLocation);
					addAccessor.AddChild (new CSharpTokenNode (Convert (ep.Add.Location)), CustomEventDeclaration.AddKeywordRole);
					if (ep.Add.Block != null)
						addAccessor.AddChild ((BlockStatement)ep.Add.Block.Accept (this), Roles.Body);
					newEvent.AddChild (addAccessor, CustomEventDeclaration.AddAccessorRole);
				}
				
				if (ep.Remove != null) {
					Accessor removeAccessor = new Accessor ();
					AddAttributeSection (removeAccessor, ep.Remove);
					var removeLocation = LocationsBag.GetMemberLocation (ep.Remove);
					AddModifiers (removeAccessor, removeLocation);
					removeAccessor.AddChild (new CSharpTokenNode (Convert (ep.Remove.Location)), CustomEventDeclaration.RemoveKeywordRole);
					
					if (ep.Remove.Block != null)
						removeAccessor.AddChild ((BlockStatement)ep.Remove.Block.Accept (this), Roles.Body);
					newEvent.AddChild (removeAccessor, CustomEventDeclaration.RemoveAccessorRole);
				}
				if (location != null && location.Count >= 3) {
					newEvent.AddChild (new CSharpTokenNode (Convert (location [2])), Roles.RBrace);
				} else {
					// parser error, set end node to max value.
					newEvent.AddChild (new ErrorNode (), Roles.Error);
				}
				
				typeStack.Peek ().AddChild (newEvent, Roles.TypeMemberRole);
			}
			
			#endregion
			
			#region Statements
			public override object Visit (Mono.CSharp.Statement stmt)
			{
				Console.WriteLine ("unknown statement:" + stmt);
				return null;
			}
			
			public override object Visit (BlockVariableDeclaration blockVariableDeclaration)
			{
				var result = new VariableDeclarationStatement ();
				result.AddChild (ConvertToType (blockVariableDeclaration.TypeExpression), Roles.Type);
				
				var varInit = new VariableInitializer ();
				var location = LocationsBag.GetLocations (blockVariableDeclaration);
				varInit.AddChild (Identifier.Create (blockVariableDeclaration.Variable.Name, Convert (blockVariableDeclaration.Variable.Location)), Roles.Identifier);
				if (blockVariableDeclaration.Initializer != null) {
					if (location != null && location.Count > 0)
						varInit.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.Assign);
					varInit.AddChild ((Expression)blockVariableDeclaration.Initializer.Accept (this), Roles.Expression);
				}
				
				result.AddChild (varInit, Roles.Variable);
				
				if (blockVariableDeclaration.Declarators != null) {
					foreach (var decl in blockVariableDeclaration.Declarators) {
						var loc = LocationsBag.GetLocations (decl);
						var init = new VariableInitializer ();
						if (loc != null && loc.Count > 0)
							result.AddChild (new CSharpTokenNode (Convert (loc [0])), Roles.Comma);
						init.AddChild (Identifier.Create (decl.Variable.Name, Convert (decl.Variable.Location)), Roles.Identifier);
						if (decl.Initializer != null) {
							if (loc != null && loc.Count > 1)
								init.AddChild (new CSharpTokenNode (Convert (loc [1])), Roles.Assign);
							init.AddChild ((Expression)decl.Initializer.Accept (this), Roles.Expression);
						} else {
						}
						result.AddChild (init, Roles.Variable);
					}
				}
				if (location != null && (blockVariableDeclaration.Initializer == null || location.Count > 1))
					result.AddChild (new CSharpTokenNode (Convert (location [location.Count - 1])), Roles.Semicolon);
				return result;
			}
			
			public override object Visit (BlockConstantDeclaration blockVariableDeclaration)
			{
				var result = new VariableDeclarationStatement ();
				
				var location = LocationsBag.GetLocations (blockVariableDeclaration);
				if (location != null && location.Count > 0)
					result.AddChild (new CSharpModifierToken (Convert (location [0]), ICSharpCode.NRefactory.CSharp.Modifiers.Const), VariableDeclarationStatement.ModifierRole);
				
				result.AddChild (ConvertToType (blockVariableDeclaration.TypeExpression), Roles.Type);
				
				var varInit = new VariableInitializer ();
				varInit.AddChild (Identifier.Create (blockVariableDeclaration.Variable.Name, Convert (blockVariableDeclaration.Variable.Location)), Roles.Identifier);
				if (blockVariableDeclaration.Initializer != null) {
					if (location != null && location.Count > 1)
						varInit.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.Assign);
					varInit.AddChild ((Expression)blockVariableDeclaration.Initializer.Accept (this), Roles.Expression);
				}
				
				result.AddChild (varInit, Roles.Variable);
				
				if (blockVariableDeclaration.Declarators != null) {
					foreach (var decl in blockVariableDeclaration.Declarators) {
						var loc = LocationsBag.GetLocations (decl);
						var init = new VariableInitializer ();
						init.AddChild (Identifier.Create (decl.Variable.Name, Convert (decl.Variable.Location)), Roles.Identifier);
						if (decl.Initializer != null) {
							if (loc != null)
								init.AddChild (new CSharpTokenNode (Convert (loc [0])), Roles.Assign);
							init.AddChild ((Expression)decl.Initializer.Accept (this), Roles.Expression);
							if (loc != null && loc.Count > 1)
								result.AddChild (new CSharpTokenNode (Convert (loc [1])), Roles.Comma);
						} else {
							if (loc != null && loc.Count > 0)
								result.AddChild (new CSharpTokenNode (Convert (loc [0])), Roles.Comma);
						}
						result.AddChild (init, Roles.Variable);
					}
				}
				if (location != null) {
					result.AddChild (new CSharpTokenNode (Convert (location [location.Count - 1])), Roles.Semicolon);
				} else {
					// parser error, set end node to max value.
					result.AddChild (new ErrorNode (), Roles.Error);
				}
				return result;
			}
			
			public override object Visit (Mono.CSharp.EmptyStatement emptyStatement)
			{
				var result = new EmptyStatement ();
				result.Location = Convert (emptyStatement.loc);
				return result;
			}
			
			public override object Visit (Mono.CSharp.EmptyExpression emptyExpression)
			{
				return new ICSharpCode.NRefactory.CSharp.EmptyExpression (Convert (emptyExpression.Location));
			}
			
			public override object Visit (Mono.CSharp.ErrorExpression emptyExpression)
			{
				return new ICSharpCode.NRefactory.CSharp.ErrorExpression (Convert (emptyExpression.Location));
			}
			
			public override object Visit (EmptyExpressionStatement emptyExpressionStatement)
			{
				return new EmptyExpression (Convert (emptyExpressionStatement.Location));
			}
			
			public override object Visit (If ifStatement)
			{
				var result = new IfElseStatement ();
				
				var location = LocationsBag.GetLocations (ifStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (ifStatement.loc)), IfElseStatement.IfKeywordRole);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.LPar);
				if (ifStatement.Expr != null)
					result.AddChild ((Expression)ifStatement.Expr.Accept (this), Roles.Condition);
				if (location != null && location.Count > 1)
					result.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.RPar);
				
				if (ifStatement.TrueStatement != null)
					result.AddChild ((Statement)ifStatement.TrueStatement.Accept (this), IfElseStatement.TrueRole);
				
				if (ifStatement.FalseStatement != null) {
					if (location != null && location.Count > 2)
						result.AddChild (new CSharpTokenNode (Convert (location [2])), IfElseStatement.ElseKeywordRole);
					result.AddChild ((Statement)ifStatement.FalseStatement.Accept (this), IfElseStatement.FalseRole);
				}
				
				return result;
			}
			
			public override object Visit (Do doStatement)
			{
				var result = new DoWhileStatement ();
				var location = LocationsBag.GetLocations (doStatement);
				result.AddChild (new CSharpTokenNode (Convert (doStatement.loc)), DoWhileStatement.DoKeywordRole);
				if (doStatement.EmbeddedStatement != null)
					result.AddChild ((Statement)doStatement.EmbeddedStatement.Accept (this), Roles.EmbeddedStatement);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), DoWhileStatement.WhileKeywordRole);
				if (location != null && location.Count > 1)
					result.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.LPar);
				if (doStatement.expr != null)
					result.AddChild ((Expression)doStatement.expr.Accept (this), Roles.Condition);
				if (location != null && location.Count > 2) {
					result.AddChild (new CSharpTokenNode (Convert (location [2])), Roles.RPar);
					if (location.Count > 3)
						result.AddChild (new CSharpTokenNode (Convert (location [3])), Roles.Semicolon);
				}
				
				return result;
			}
			
			public override object Visit (While whileStatement)
			{
				var result = new WhileStatement ();
				var location = LocationsBag.GetLocations (whileStatement);
				result.AddChild (new CSharpTokenNode (Convert (whileStatement.loc)), WhileStatement.WhileKeywordRole);
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.LPar);
				if (whileStatement.expr != null)
					result.AddChild ((Expression)whileStatement.expr.Accept (this), Roles.Condition);
				if (location != null && location.Count > 1)
					result.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.RPar);
				if (whileStatement.Statement != null)
					result.AddChild ((Statement)whileStatement.Statement.Accept (this), Roles.EmbeddedStatement);
				return result;
			}
			
			void AddStatementOrList (ForStatement forStatement, Mono.CSharp.Statement init, Role<Statement> role)
			{
				if (init == null)
					return;
				if (init is StatementList) {
					foreach (var stmt in ((StatementList)init).Statements) {
						forStatement.AddChild ((Statement)stmt.Accept (this), role);
					}
				} else if (init is Mono.CSharp.EmptyStatement) {
					
				} else {
					forStatement.AddChild ((Statement)init.Accept (this), role);
				}
			}

			public override object Visit (For forStatement)
			{
				var result = new ForStatement ();
				
				var location = LocationsBag.GetLocations (forStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (forStatement.loc)), ForStatement.ForKeywordRole);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.LPar);
				
				AddStatementOrList (result, forStatement.Initializer, ForStatement.InitializerRole);
				
				if (location != null && location.Count > 1)
					result.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.Semicolon);
				if (forStatement.Condition != null)
					result.AddChild ((Expression)forStatement.Condition.Accept (this), Roles.Condition);
				if (location != null && location.Count >= 3)
					result.AddChild (new CSharpTokenNode (Convert (location [2])), Roles.Semicolon);
				
				AddStatementOrList (result, forStatement.Iterator, ForStatement.IteratorRole);
				
				if (location != null && location.Count >= 4)
					result.AddChild (new CSharpTokenNode (Convert (location [3])), Roles.RPar);
				
				if (forStatement.Statement != null)
					result.AddChild ((Statement)forStatement.Statement.Accept (this), Roles.EmbeddedStatement);
				
				return result;
			}
			
			public override object Visit (StatementExpression statementExpression)
			{
				var result = new ExpressionStatement ();
				var expr = statementExpression.Expr.Accept (this) as Expression;
				if (expr != null)
					result.AddChild ((Expression)expr, Roles.Expression);
				var location = LocationsBag.GetLocations (statementExpression);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.Semicolon);
				return result;
			}
			
			public override object Visit (StatementErrorExpression statementErrorExpression)
			{
				var result = new ExpressionStatement ();
				var expr = statementErrorExpression.Expr.Accept (this) as Expression;
				if (expr != null)
					result.AddChild ((Expression)expr, Roles.Expression);
				var location = LocationsBag.GetLocations (statementErrorExpression);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.Semicolon);
				return result;
			}
			
			public override object Visit (InvalidStatementExpression statementExpression)
			{
				var result = new ExpressionStatement ();
				if (statementExpression.Expression == null)
					return result;
				var expr = statementExpression.Expression.Accept (this) as Expression;
				if (expr != null)
					result.AddChild (expr, Roles.Expression);
				var location = LocationsBag.GetLocations (statementExpression);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.Semicolon);
				return result;
			}
			
			public override object Visit (Return returnStatement)
			{
				var result = new ReturnStatement ();
				
				result.AddChild (new CSharpTokenNode (Convert (returnStatement.loc)), ReturnStatement.ReturnKeywordRole);
				if (returnStatement.Expr != null)
					result.AddChild ((Expression)returnStatement.Expr.Accept (this), Roles.Expression);
				
				var location = LocationsBag.GetLocations (returnStatement);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.Semicolon);
				
				return result;
			}
			
			public override object Visit (Goto gotoStatement)
			{
				var result = new GotoStatement ();
				var location = LocationsBag.GetLocations (gotoStatement);
				result.AddChild (new CSharpTokenNode (Convert (gotoStatement.loc)), GotoStatement.GotoKeywordRole);
				var loc = location != null ? Convert (location [0]) : TextLocation.Empty;
				result.AddChild (Identifier.Create (gotoStatement.Target, loc), Roles.Identifier);
				if (location != null && location.Count > 1)
					result.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.Semicolon);
				
				return result;
			}
			
			public override object Visit (LabeledStatement labeledStatement)
			{
				var result = new LabelStatement ();
				result.AddChild (Identifier.Create (labeledStatement.Name, Convert (labeledStatement.loc)), Roles.Identifier);
				var location = LocationsBag.GetLocations (labeledStatement);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.Colon);
				return result;
			}
			
			public override object Visit (GotoDefault gotoDefault)
			{
				var result = new GotoDefaultStatement ();
				result.AddChild (new CSharpTokenNode (Convert (gotoDefault.loc)), GotoDefaultStatement.GotoKeywordRole);
				var location = LocationsBag.GetLocations (gotoDefault);
				if (location != null) {
					result.AddChild (new CSharpTokenNode (Convert (location [0])), GotoDefaultStatement.DefaultKeywordRole);
					if (location.Count > 1)
						result.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.Semicolon);
				}
				
				return result;
			}
			
			public override object Visit (GotoCase gotoCase)
			{
				var result = new GotoCaseStatement ();
				result.AddChild (new CSharpTokenNode (Convert (gotoCase.loc)), GotoCaseStatement.GotoKeywordRole);
				
				var location = LocationsBag.GetLocations (gotoCase);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), GotoCaseStatement.CaseKeywordRole);
				if (gotoCase.Expr != null)
					result.AddChild ((Expression)gotoCase.Expr.Accept (this), Roles.Expression);
				if (location != null && location.Count > 1)
					result.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.Semicolon);
				return result;
			}
			
			public override object Visit (Throw throwStatement)
			{
				var result = new ThrowStatement ();
				var location = LocationsBag.GetLocations (throwStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (throwStatement.loc)), ThrowStatement.ThrowKeywordRole);
				if (throwStatement.Expr != null)
					result.AddChild ((Expression)throwStatement.Expr.Accept (this), Roles.Expression);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.Semicolon);
				return result;
			}
			
			public override object Visit (Break breakStatement)
			{
				var result = new BreakStatement ();
				var location = LocationsBag.GetLocations (breakStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (breakStatement.loc)), BreakStatement.BreakKeywordRole);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.Semicolon);
				return result;
			}
			
			public override object Visit (Continue continueStatement)
			{
				var result = new ContinueStatement ();
				var location = LocationsBag.GetLocations (continueStatement);
				result.AddChild (new CSharpTokenNode (Convert (continueStatement.loc)), ContinueStatement.ContinueKeywordRole);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.Semicolon);
				return result;
			}
			
			public static bool IsLower (Location left, Location right)
			{
				return left.Row < right.Row || left.Row == right.Row && left.Column < right.Column;
			}
			
			public UsingStatement CreateUsingStatement (Block blockStatement)
			{
				var usingResult = new UsingStatement ();
				Mono.CSharp.Statement cur = blockStatement.Statements [0];
				if (cur is Using) {
					Using u = (Using)cur;
					usingResult.AddChild (new CSharpTokenNode (Convert (u.loc)), UsingStatement.UsingKeywordRole);
					usingResult.AddChild (new CSharpTokenNode (Convert (blockStatement.StartLocation)), Roles.LPar);
					if (u.Variables != null) {
						var initializer = new VariableInitializer () {
							NameToken = Identifier.Create (u.Variables.Variable.Name, Convert (u.Variables.Variable.Location)),
						};
						
						var loc = LocationsBag.GetLocations (u.Variables);
						if (loc != null)
							initializer.AddChild (new CSharpTokenNode (Convert (loc [0])), Roles.Assign);
						if (u.Variables.Initializer != null)
							initializer.Initializer = u.Variables.Initializer.Accept (this) as Expression;
						
						
						var varDec = new VariableDeclarationStatement () {
							Type = ConvertToType (u.Variables.TypeExpression),
							Variables = { initializer }
						};
						
						if (u.Variables.Declarators != null) {
							foreach (var decl in u.Variables.Declarators) {
								var declLoc = LocationsBag.GetLocations (decl);
								var init = new VariableInitializer ();
								if (declLoc != null && declLoc.Count > 0)
									varDec.AddChild (new CSharpTokenNode (Convert (declLoc [0])), Roles.Comma);
								init.AddChild (Identifier.Create (decl.Variable.Name, Convert (decl.Variable.Location)), Roles.Identifier);
								if (decl.Initializer != null) {
									if (declLoc != null && declLoc.Count > 1)
										init.AddChild (new CSharpTokenNode (Convert (declLoc [1])), Roles.Assign);
									init.AddChild ((Expression)decl.Initializer.Accept (this), Roles.Expression);
								}
								varDec.AddChild (init, Roles.Variable);
							}
						}
						usingResult.AddChild (varDec, UsingStatement.ResourceAcquisitionRole);
					}
					cur = u.Statement;
					usingResult.AddChild (new CSharpTokenNode (Convert (blockStatement.EndLocation)), Roles.RPar);
					if (cur != null)
						usingResult.AddChild ((Statement)cur.Accept (this), Roles.EmbeddedStatement);
				}
				return usingResult;
			}
			
			void AddBlockChildren (BlockStatement result, Block blockStatement, ref int curLocal)
			{
				if (convertTypeSystemMode) {
					return;
				}
				foreach (Mono.CSharp.Statement stmt in blockStatement.Statements) {
					if (stmt == null)
						continue;
					/*					if (curLocal < localVariables.Count && IsLower (localVariables[curLocal].Location, stmt.loc)) {
						result.AddChild (CreateVariableDeclaration (localVariables[curLocal]), Roles.Statement);
						curLocal++;
					}*/
					if (stmt is Block && !(stmt is ToplevelBlock || stmt is ExplicitBlock)) {
						AddBlockChildren (result, (Block)stmt, ref curLocal);
					} else {
						result.AddChild ((Statement)stmt.Accept (this), BlockStatement.StatementRole);
					}
				}
			}
			
			public override object Visit (Block blockStatement)
			{
				if (blockStatement.IsCompilerGenerated && blockStatement.Statements.Any ()) {
					if (blockStatement.Statements.First () is Using)
						return CreateUsingStatement (blockStatement);
					return blockStatement.Statements.Last ().Accept (this);
				}
				var result = new BlockStatement ();
				result.AddChild (new CSharpTokenNode (Convert (blockStatement.StartLocation)), Roles.LBrace);
				int curLocal = 0;
				AddBlockChildren (result, blockStatement, ref curLocal);
				
				result.AddChild (new CSharpTokenNode (Convert (blockStatement.EndLocation)), Roles.RBrace);
				return result;
			}

			public override object Visit (Switch switchStatement)
			{
				var result = new SwitchStatement ();
				
				var location = LocationsBag.GetLocations (switchStatement);
				result.AddChild (new CSharpTokenNode (Convert (switchStatement.loc)), SwitchStatement.SwitchKeywordRole);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.LPar);
				if (switchStatement.Expr != null)
					result.AddChild ((Expression)switchStatement.Expr.Accept (this), Roles.Expression);
				if (location != null && location.Count > 1)
					result.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.RPar);
				if (location != null && location.Count > 2)
					result.AddChild (new CSharpTokenNode (Convert (location [2])), Roles.LBrace);
				if (switchStatement.Sections != null) {
					foreach (var section in switchStatement.Sections) {
						var newSection = new SwitchSection ();
						if (section.Labels != null) {
							foreach (var caseLabel in section.Labels) {
								var newLabel = new CaseLabel ();
								if (caseLabel.Label != null) {
									newLabel.AddChild (new CSharpTokenNode (Convert (caseLabel.Location)), CaseLabel.CaseKeywordRole);
									if (caseLabel.Label != null)
										newLabel.AddChild ((Expression)caseLabel.Label.Accept (this), Roles.Expression);
									var colonLocation = LocationsBag.GetLocations (caseLabel);
									if (colonLocation != null)
										newLabel.AddChild (new CSharpTokenNode (Convert (colonLocation [0])), Roles.Colon);
								} else {
									newLabel.AddChild (new CSharpTokenNode (Convert (caseLabel.Location)), CaseLabel.DefaultKeywordRole);
									newLabel.AddChild (new CSharpTokenNode (new TextLocation (caseLabel.Location.Row, caseLabel.Location.Column + "default".Length)), Roles.Colon);
								}
								newSection.AddChild (newLabel, SwitchSection.CaseLabelRole);
							}
						}
						
						var blockStatement = section.Block;
						var bodyBlock = new BlockStatement ();
						int curLocal = 0;
						AddBlockChildren (bodyBlock, blockStatement, ref curLocal);
						foreach (var statement in bodyBlock.Statements) {
							statement.Remove ();
							newSection.AddChild (statement, Roles.EmbeddedStatement);
							
						}
						result.AddChild (newSection, SwitchStatement.SwitchSectionRole);
					}
				}
				
				if (location != null && location.Count > 3) {
					result.AddChild (new CSharpTokenNode (Convert (location [3])), Roles.RBrace);
				} else {
					// parser error, set end node to max value.
					result.AddChild (new ErrorNode (), Roles.Error);
				}
				
				return result;
			}
			
			public override object Visit (Lock lockStatement)
			{
				var result = new LockStatement ();
				var location = LocationsBag.GetLocations (lockStatement);
				result.AddChild (new CSharpTokenNode (Convert (lockStatement.loc)), LockStatement.LockKeywordRole);
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.LPar);
				if (lockStatement.Expr != null)
					result.AddChild ((Expression)lockStatement.Expr.Accept (this), Roles.Expression);
				
				if (location != null && location.Count > 1)
					result.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.RPar);
				if (lockStatement.Statement != null)
					result.AddChild ((Statement)lockStatement.Statement.Accept (this), Roles.EmbeddedStatement);
				
				return result;
			}
			
			public override object Visit (Unchecked uncheckedStatement)
			{
				var result = new UncheckedStatement ();
				result.AddChild (new CSharpTokenNode (Convert (uncheckedStatement.loc)), UncheckedStatement.UncheckedKeywordRole);
				if (uncheckedStatement.Block != null)
					result.AddChild ((BlockStatement)uncheckedStatement.Block.Accept (this), Roles.Body);
				return result;
			}
			
			public override object Visit (Checked checkedStatement)
			{
				var result = new CheckedStatement ();
				result.AddChild (new CSharpTokenNode (Convert (checkedStatement.loc)), CheckedStatement.CheckedKeywordRole);
				if (checkedStatement.Block != null)
					result.AddChild ((BlockStatement)checkedStatement.Block.Accept (this), Roles.Body);
				return result;
			}
			
			public override object Visit (Unsafe unsafeStatement)
			{
				var result = new UnsafeStatement ();
				result.AddChild (new CSharpTokenNode (Convert (unsafeStatement.loc)), UnsafeStatement.UnsafeKeywordRole);
				if (unsafeStatement.Block != null)
					result.AddChild ((BlockStatement)unsafeStatement.Block.Accept (this), Roles.Body);
				return result;
			}
			
			public override object Visit (Fixed fixedStatement)
			{
				var result = new FixedStatement ();
				var location = LocationsBag.GetLocations (fixedStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (fixedStatement.loc)), FixedStatement.FixedKeywordRole);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.LPar);
				
				if (fixedStatement.Variables != null) {
					var blockVariableDeclaration = fixedStatement.Variables;
					result.AddChild (ConvertToType (blockVariableDeclaration.TypeExpression), Roles.Type);
					var varInit = new VariableInitializer ();
					var initLocation = LocationsBag.GetLocations (blockVariableDeclaration);
					varInit.AddChild (Identifier.Create (blockVariableDeclaration.Variable.Name, Convert (blockVariableDeclaration.Variable.Location)), Roles.Identifier);
					if (blockVariableDeclaration.Initializer != null) {
						if (initLocation != null)
							varInit.AddChild (new CSharpTokenNode (Convert (initLocation [0])), Roles.Assign);
						varInit.AddChild ((Expression)blockVariableDeclaration.Initializer.Accept (this), Roles.Expression);
					}
					
					result.AddChild (varInit, Roles.Variable);
					
					if (blockVariableDeclaration.Declarators != null) {
						foreach (var decl in blockVariableDeclaration.Declarators) {
							var loc = LocationsBag.GetLocations (decl);
							var init = new VariableInitializer ();
							if (loc != null && loc.Count > 0)
								result.AddChild (new CSharpTokenNode (Convert (loc [0])), Roles.Comma);
							init.AddChild (Identifier.Create (decl.Variable.Name, Convert (decl.Variable.Location)), Roles.Identifier);
							if (decl.Initializer != null) {
								if (loc != null && loc.Count > 1)
									init.AddChild (new CSharpTokenNode (Convert (loc [1])), Roles.Assign);
								init.AddChild ((Expression)decl.Initializer.Accept (this), Roles.Expression);
							} else {
							}
							result.AddChild (init, Roles.Variable);
						}
					}
				}
				
				if (location != null && location.Count > 1)
					result.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.RPar);
				if (fixedStatement.Statement != null)
					result.AddChild ((Statement)fixedStatement.Statement.Accept (this), Roles.EmbeddedStatement);
				return result;
			}
			
			public override object Visit (TryFinally tryFinallyStatement)
			{
				TryCatchStatement result;
				var location = LocationsBag.GetLocations (tryFinallyStatement);
				
				if (tryFinallyStatement.Stmt is TryCatch) {
					result = (TryCatchStatement)tryFinallyStatement.Stmt.Accept (this);
				} else {
					result = new TryCatchStatement ();
					result.AddChild (new CSharpTokenNode (Convert (tryFinallyStatement.loc)), TryCatchStatement.TryKeywordRole);
					if (tryFinallyStatement.Stmt != null)
						result.AddChild ((BlockStatement)tryFinallyStatement.Stmt.Accept (this), TryCatchStatement.TryBlockRole);
				}
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), TryCatchStatement.FinallyKeywordRole);
				if (tryFinallyStatement.Fini != null)
					result.AddChild ((BlockStatement)tryFinallyStatement.Fini.Accept (this), TryCatchStatement.FinallyBlockRole);
				
				return result;
			}
			
			CatchClause ConvertCatch (Catch ctch)
			{
				CatchClause result = new CatchClause ();
				var location = LocationsBag.GetLocations (ctch);
				result.AddChild (new CSharpTokenNode (Convert (ctch.loc)), CatchClause.CatchKeywordRole);
				if (ctch.TypeExpression != null) {
					if (location != null)
						result.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.LPar);
					
					if (ctch.TypeExpression != null)
						result.AddChild (ConvertToType (ctch.TypeExpression), Roles.Type);
					if (ctch.Variable != null && !string.IsNullOrEmpty (ctch.Variable.Name))
						result.AddChild (Identifier.Create (ctch.Variable.Name, Convert (ctch.Variable.Location)), Roles.Identifier);
					
					if (location != null && location.Count > 1)
						result.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.RPar);
				}

				if (ctch.Block != null)
					result.AddChild ((BlockStatement)ctch.Block.Accept (this), Roles.Body);
				
				return result;
			}
			
			public override object Visit (TryCatch tryCatchStatement)
			{
				var result = new TryCatchStatement ();
				result.AddChild (new CSharpTokenNode (Convert (tryCatchStatement.loc)), TryCatchStatement.TryKeywordRole);
				if (tryCatchStatement.Block != null)
					result.AddChild ((BlockStatement)tryCatchStatement.Block.Accept (this), TryCatchStatement.TryBlockRole);
				if (tryCatchStatement.Clauses != null) {
					foreach (var ctch in tryCatchStatement.Clauses) {
						result.AddChild (ConvertCatch (ctch), TryCatchStatement.CatchClauseRole);
					}
				}
//				if (tryCatchStatement.General != null)
//					result.AddChild (ConvertCatch (tryCatchStatement.General), TryCatchStatement.CatchClauseRole);
				
				return result;
			}
			
			public override object Visit (Using usingStatement)
			{
				var result = new UsingStatement ();
				var location = LocationsBag.GetLocations (usingStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (usingStatement.loc)), UsingStatement.UsingKeywordRole);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.LPar);
				if (usingStatement.Expr != null)
					result.AddChild ((AstNode)usingStatement.Expr.Accept (this), UsingStatement.ResourceAcquisitionRole);
				
				if (location != null && location.Count > 1)
					result.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.RPar);
				
				if (usingStatement.Statement != null)
					result.AddChild ((Statement)usingStatement.Statement.Accept (this), Roles.EmbeddedStatement);
				return result;
			}
			
			public override object Visit (Foreach foreachStatement)
			{
				var result = new ForeachStatement ();
				
				var location = LocationsBag.GetLocations (foreachStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (foreachStatement.loc)), ForeachStatement.ForeachKeywordRole);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.LPar);
				
				if (foreachStatement.TypeExpression != null)
					result.AddChild (ConvertToType (foreachStatement.TypeExpression), Roles.Type);
				
				if (foreachStatement.Variable != null)
					result.AddChild (Identifier.Create (foreachStatement.Variable.Name, Convert (foreachStatement.Variable.Location)), Roles.Identifier);
				
				if (location != null && location.Count > 1)
					result.AddChild (new CSharpTokenNode (Convert (location [1])), ForeachStatement.InKeywordRole);
				
				if (foreachStatement.Expr != null)
					result.AddChild ((Expression)foreachStatement.Expr.Accept (this), Roles.Expression);
				
				if (location != null && location.Count > 2)
					result.AddChild (new CSharpTokenNode (Convert (location [2])), Roles.RPar);
				
				if (foreachStatement.Statement != null)
					result.AddChild ((Statement)foreachStatement.Statement.Accept (this), Roles.EmbeddedStatement);
				
				return result;
			}
			
			public override object Visit (Yield yieldStatement)
			{
				var result = new YieldReturnStatement ();
				var location = LocationsBag.GetLocations (yieldStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (yieldStatement.loc)), YieldReturnStatement.YieldKeywordRole);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), YieldReturnStatement.ReturnKeywordRole);
				if (yieldStatement.Expr != null)
					result.AddChild ((Expression)yieldStatement.Expr.Accept (this), Roles.Expression);
				if (location != null && location.Count > 1)
					result.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.Semicolon);
				
				return result;
			}
			
			public override object Visit (YieldBreak yieldBreakStatement)
			{
				var result = new YieldBreakStatement ();
				var location = LocationsBag.GetLocations (yieldBreakStatement);
				result.AddChild (new CSharpTokenNode (Convert (yieldBreakStatement.loc)), YieldBreakStatement.YieldKeywordRole);
				if (location != null) {
					result.AddChild (new CSharpTokenNode (Convert (location [0])), YieldBreakStatement.BreakKeywordRole);
					if (location.Count > 1)
						result.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.Semicolon);
				}
				return result;
			}
			#endregion
			
			#region Expression
			public override object Visit (Mono.CSharp.Expression expression)
			{
				Console.WriteLine ("Visit unknown expression:" + expression);
				System.Console.WriteLine (Environment.StackTrace);
				return null;
			}
			
			public override object Visit (Mono.CSharp.DefaultParameterValueExpression defaultParameterValueExpression)
			{
				return defaultParameterValueExpression.Child.Accept (this);
			}

			public override object Visit (TypeExpression typeExpression)
			{
				return new TypeReferenceExpression (new PrimitiveType (keywordTable [(int)typeExpression.Type.BuiltinType], Convert (typeExpression.Location)));
			}

			public override object Visit (LocalVariableReference localVariableReference)
			{
				return Identifier.Create (localVariableReference.Name, Convert (localVariableReference.Location));
			}

			public override object Visit (MemberAccess memberAccess)
			{
				Expression result;
				if (memberAccess.LeftExpression is Indirection) {
					var ind = memberAccess.LeftExpression as Indirection;
					result = new PointerReferenceExpression ();
					result.AddChild ((Expression)ind.Expr.Accept (this), Roles.TargetExpression);
					result.AddChild (new CSharpTokenNode (Convert (ind.Location)), PointerReferenceExpression.ArrowRole);
				} else {
					result = new MemberReferenceExpression ();
					if (memberAccess.LeftExpression != null) {
						var leftExpr = memberAccess.LeftExpression.Accept (this);
						result.AddChild ((Expression)leftExpr, Roles.TargetExpression);
					}
					if (!memberAccess.DotLocation.IsNull) {
						result.AddChild (new CSharpTokenNode (Convert (memberAccess.DotLocation)), Roles.Dot);
					}
				}
				
				result.AddChild (Identifier.Create (memberAccess.Name, Convert (memberAccess.Location)), Roles.Identifier);
				
				AddTypeArguments (result, memberAccess);
				return result;
			}
			
			public override object Visit (QualifiedAliasMember qualifiedAliasMember)
			{
				var result = new MemberType ();
				result.Target = new SimpleType (qualifiedAliasMember.alias, Convert (qualifiedAliasMember.Location));
				result.IsDoubleColon = true;
				var location = LocationsBag.GetLocations (qualifiedAliasMember);
				result.AddChild (Identifier.Create (qualifiedAliasMember.Name, location != null ? Convert (location [0]) : TextLocation.Empty), Roles.Identifier);
				return  new TypeReferenceExpression () { Type = result };
			}
			
			public override object Visit (Constant constant)
			{
				if (constant.GetValue () == null)
					return new NullReferenceExpression (Convert (constant.Location));
				string literalValue;
				if (constant is ILiteralConstant) {
					literalValue = new string (((ILiteralConstant)constant).ParsedValue);
				} else {
					literalValue = constant.GetValueAsLiteral ();
				}
				object val = constant.GetValue ();
				if (val is bool)
					literalValue = (bool)val ? "true" : "false";
				var result = new PrimitiveExpression (val, Convert (constant.Location), literalValue);
				return result;
			}

			public override object Visit (SimpleName simpleName)
			{
				var result = new IdentifierExpression ();
				result.AddChild (Identifier.Create (simpleName.Name, Convert (simpleName.Location)), Roles.Identifier);
				AddTypeArguments (result, simpleName);
				return result;
			}
			
			public override object Visit (BooleanExpression booleanExpression)
			{
				return booleanExpression.Expr.Accept (this);
			}
			
			public override object Visit (Mono.CSharp.ParenthesizedExpression parenthesizedExpression)
			{
				var result = new ParenthesizedExpression ();
				var location = LocationsBag.GetLocations (parenthesizedExpression);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.LPar);
				if (parenthesizedExpression.Expr != null)
					result.AddChild ((Expression)parenthesizedExpression.Expr.Accept (this), Roles.Expression);
				if (location != null && location.Count > 1)
					result.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.RPar);
				return result;
			}
			
			public override object Visit (Unary unaryExpression)
			{
				var result = new UnaryOperatorExpression ();
				switch (unaryExpression.Oper) {
					case Unary.Operator.UnaryPlus:
						result.Operator = UnaryOperatorType.Plus;
						break;
					case Unary.Operator.UnaryNegation:
						result.Operator = UnaryOperatorType.Minus;
						break;
					case Unary.Operator.LogicalNot:
						result.Operator = UnaryOperatorType.Not;
						break;
					case Unary.Operator.OnesComplement:
						result.Operator = UnaryOperatorType.BitNot;
						break;
					case Unary.Operator.AddressOf:
						result.Operator = UnaryOperatorType.AddressOf;
						break;
				}
				result.AddChild (new CSharpTokenNode (Convert (unaryExpression.Location)), UnaryOperatorExpression.GetOperatorRole (result.Operator));
				if (unaryExpression.Expr != null)
					result.AddChild ((Expression)unaryExpression.Expr.Accept (this), Roles.Expression);
				return result;
			}
			
			public override object Visit (UnaryMutator unaryMutatorExpression)
			{
				var result = new UnaryOperatorExpression ();
				if (unaryMutatorExpression.Expr == null)
					return result;
				var expression = (Expression)unaryMutatorExpression.Expr.Accept (this);
				switch (unaryMutatorExpression.UnaryMutatorMode) {
					case UnaryMutator.Mode.PostDecrement:
						result.Operator = UnaryOperatorType.PostDecrement;
						result.AddChild (expression, Roles.Expression);
						result.AddChild (new CSharpTokenNode (Convert (unaryMutatorExpression.Location)), UnaryOperatorExpression.DecrementRole);
						break;
					case UnaryMutator.Mode.PostIncrement:
						result.Operator = UnaryOperatorType.PostIncrement;
						result.AddChild (expression, Roles.Expression);
						result.AddChild (new CSharpTokenNode (Convert (unaryMutatorExpression.Location)), UnaryOperatorExpression.IncrementRole);
						break;
						
					case UnaryMutator.Mode.PreIncrement:
						result.Operator = UnaryOperatorType.Increment;
						result.AddChild (new CSharpTokenNode (Convert (unaryMutatorExpression.Location)), UnaryOperatorExpression.IncrementRole);
						result.AddChild (expression, Roles.Expression);
						break;
					case UnaryMutator.Mode.PreDecrement:
						result.Operator = UnaryOperatorType.Decrement;
						result.AddChild (new CSharpTokenNode (Convert (unaryMutatorExpression.Location)), UnaryOperatorExpression.DecrementRole);
						result.AddChild (expression, Roles.Expression);
						break;
				}
				
				return result;
			}
			
			public override object Visit (Indirection indirectionExpression)
			{
				var result = new UnaryOperatorExpression ();
				result.Operator = UnaryOperatorType.Dereference;
				result.AddChild (new CSharpTokenNode (Convert (indirectionExpression.Location)), UnaryOperatorExpression.DereferenceRole);
				if (indirectionExpression.Expr != null)
					result.AddChild ((Expression)indirectionExpression.Expr.Accept (this), Roles.Expression);
				return result;
			}
			
			public override object Visit (Is isExpression)
			{
				var result = new IsExpression ();
				if (isExpression.Expr != null)
					result.AddChild ((Expression)isExpression.Expr.Accept (this), Roles.Expression);
				result.AddChild (new CSharpTokenNode (Convert (isExpression.Location)), IsExpression.IsKeywordRole);
				
				if (isExpression.ProbeType != null)
					result.AddChild (ConvertToType (isExpression.ProbeType), Roles.Type);
				return result;
			}
			
			public override object Visit (As asExpression)
			{
				var result = new AsExpression ();
				if (asExpression.Expr != null)
					result.AddChild ((Expression)asExpression.Expr.Accept (this), Roles.Expression);
				result.AddChild (new CSharpTokenNode (Convert (asExpression.Location)), AsExpression.AsKeywordRole);
				if (asExpression.ProbeType != null)
					result.AddChild (ConvertToType (asExpression.ProbeType), Roles.Type);
				return result;
			}
			
			public override object Visit (Cast castExpression)
			{
				var result = new CastExpression ();
				var location = LocationsBag.GetLocations (castExpression);
				
				result.AddChild (new CSharpTokenNode (Convert (castExpression.Location)), Roles.LPar);
				if (castExpression.TargetType != null)
					result.AddChild (ConvertToType (castExpression.TargetType), Roles.Type);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.RPar);
				if (castExpression.Expr != null)
					result.AddChild ((Expression)castExpression.Expr.Accept (this), Roles.Expression);
				return result;
			}
			
			public override object Visit (ComposedCast composedCast)
			{
				var result = new ComposedType ();
				result.AddChild (ConvertToType (composedCast.Left), Roles.Type);
				
				var spec = composedCast.Spec;
				while (spec != null) {
					if (spec.IsNullable) {
						result.AddChild (new CSharpTokenNode (Convert (spec.Location)), ComposedType.NullableRole);
					} else if (spec.IsPointer) {
						result.AddChild (new CSharpTokenNode (Convert (spec.Location)), ComposedType.PointerRole);
					} else {
						var aSpec = new ArraySpecifier ();
						aSpec.AddChild (new CSharpTokenNode (Convert (spec.Location)), Roles.LBracket);
						var location = LocationsBag.GetLocations (spec);
						if (location != null)
							aSpec.AddChild (new CSharpTokenNode (Convert (spec.Location)), Roles.RBracket);
						result.AddChild (aSpec, ComposedType.ArraySpecifierRole);
					}
					spec = spec.Next;
				}
				
				return result;
			}
			
			public override object Visit (Mono.CSharp.DefaultValueExpression defaultValueExpression)
			{
				var result = new DefaultValueExpression ();
				result.AddChild (new CSharpTokenNode (Convert (defaultValueExpression.Location)), DefaultValueExpression.DefaultKeywordRole);
				var location = LocationsBag.GetLocations (defaultValueExpression);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.LPar);
				result.AddChild (ConvertToType (defaultValueExpression.Expr), Roles.Type);
				if (location != null && location.Count > 1)
					result.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.RPar);
				return result;
			}
			
			public override object Visit (Binary binaryExpression)
			{
				var result = new BinaryOperatorExpression ();
				switch (binaryExpression.Oper) {
					case Binary.Operator.Multiply:
						result.Operator = BinaryOperatorType.Multiply;
						break;
					case Binary.Operator.Division:
						result.Operator = BinaryOperatorType.Divide;
						break;
					case Binary.Operator.Modulus:
						result.Operator = BinaryOperatorType.Modulus;
						break;
					case Binary.Operator.Addition:
						result.Operator = BinaryOperatorType.Add;
						break;
					case Binary.Operator.Subtraction:
						result.Operator = BinaryOperatorType.Subtract;
						break;
					case Binary.Operator.LeftShift:
						result.Operator = BinaryOperatorType.ShiftLeft;
						break;
					case Binary.Operator.RightShift:
						result.Operator = BinaryOperatorType.ShiftRight;
						break;
					case Binary.Operator.LessThan:
						result.Operator = BinaryOperatorType.LessThan;
						break;
					case Binary.Operator.GreaterThan:
						result.Operator = BinaryOperatorType.GreaterThan;
						break;
					case Binary.Operator.LessThanOrEqual:
						result.Operator = BinaryOperatorType.LessThanOrEqual;
						break;
					case Binary.Operator.GreaterThanOrEqual:
						result.Operator = BinaryOperatorType.GreaterThanOrEqual;
						break;
					case Binary.Operator.Equality:
						result.Operator = BinaryOperatorType.Equality;
						break;
					case Binary.Operator.Inequality:
						result.Operator = BinaryOperatorType.InEquality;
						break;
					case Binary.Operator.BitwiseAnd:
						result.Operator = BinaryOperatorType.BitwiseAnd;
						break;
					case Binary.Operator.ExclusiveOr:
						result.Operator = BinaryOperatorType.ExclusiveOr;
						break;
					case Binary.Operator.BitwiseOr:
						result.Operator = BinaryOperatorType.BitwiseOr;
						break;
					case Binary.Operator.LogicalAnd:
						result.Operator = BinaryOperatorType.ConditionalAnd;
						break;
					case Binary.Operator.LogicalOr:
						result.Operator = BinaryOperatorType.ConditionalOr;
						break;
				}
				
				if (binaryExpression.Left != null)
					result.AddChild ((Expression)binaryExpression.Left.Accept (this), BinaryOperatorExpression.LeftRole);
				var location = LocationsBag.GetLocations (binaryExpression);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0])), BinaryOperatorExpression.GetOperatorRole (result.Operator));
				if (binaryExpression.Right != null)
					result.AddChild ((Expression)binaryExpression.Right.Accept (this), BinaryOperatorExpression.RightRole);
				return result;
			}
			
			public override object Visit (Mono.CSharp.Nullable.NullCoalescingOperator nullCoalescingOperator)
			{
				var result = new BinaryOperatorExpression ();
				result.Operator = BinaryOperatorType.NullCoalescing;
				if (nullCoalescingOperator.LeftExpression != null)
					result.AddChild ((Expression)nullCoalescingOperator.LeftExpression.Accept (this), BinaryOperatorExpression.LeftRole);
				var location = LocationsBag.GetLocations (nullCoalescingOperator);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0])), BinaryOperatorExpression.NullCoalescingRole);
				if (nullCoalescingOperator.RightExpression != null)
					result.AddChild ((Expression)nullCoalescingOperator.RightExpression.Accept (this), BinaryOperatorExpression.RightRole);
				return result;
			}
			
			public override object Visit (Conditional conditionalExpression)
			{
				var result = new ConditionalExpression ();
				
				if (conditionalExpression.Expr != null)
					result.AddChild ((Expression)conditionalExpression.Expr.Accept (this), Roles.Condition);
				var location = LocationsBag.GetLocations (conditionalExpression);
				
				result.AddChild (new CSharpTokenNode (Convert (conditionalExpression.Location)), ConditionalExpression.QuestionMarkRole);
				if (conditionalExpression.TrueExpr != null)
					result.AddChild ((Expression)conditionalExpression.TrueExpr.Accept (this), ConditionalExpression.TrueRole);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), ConditionalExpression.ColonRole);
				if (conditionalExpression.FalseExpr != null)
					result.AddChild ((Expression)conditionalExpression.FalseExpr.Accept (this), ConditionalExpression.FalseRole);
				return result;
			}
			
			void AddParameter (AstNode parent, Mono.CSharp.AParametersCollection parameters)
			{
				if (parameters == null)
					return;
				var paramLocation = LocationsBag.GetLocations (parameters);
				
				for (int i = 0; i < parameters.Count; i++) {
					var p = (Parameter)parameters.FixedParameters [i];
					if (p == null)
						continue;
					var location = LocationsBag.GetLocations (p);
					ParameterDeclaration parameterDeclarationExpression = new ParameterDeclaration ();
					AddAttributeSection (parameterDeclarationExpression, p);
					switch (p.ModFlags) {
						case Parameter.Modifier.OUT:
							parameterDeclarationExpression.ParameterModifier = ParameterModifier.Out;
							if (location != null)
								parameterDeclarationExpression.AddChild (new CSharpTokenNode (Convert (location [0])), ParameterDeclaration.OutModifierRole);
							break;
						case Parameter.Modifier.REF:
							parameterDeclarationExpression.ParameterModifier = ParameterModifier.Ref;
							if (location != null)
								parameterDeclarationExpression.AddChild (new CSharpTokenNode (Convert (location [0])), ParameterDeclaration.RefModifierRole);
							break;
						case Parameter.Modifier.PARAMS:
							parameterDeclarationExpression.ParameterModifier = ParameterModifier.Params;
							if (location != null)
								parameterDeclarationExpression.AddChild (new CSharpTokenNode (Convert (location [0])), ParameterDeclaration.ParamsModifierRole);
							break;
						default:
							if (p.HasExtensionMethodModifier) {
								parameterDeclarationExpression.ParameterModifier = ParameterModifier.This;
								if (location != null) {
									parameterDeclarationExpression.AddChild (new CSharpTokenNode (Convert (location [0])), ParameterDeclaration.ThisModifierRole);
								}
							}
							break;
					}
					if (p.TypeExpression != null) // lambdas may have no types (a, b) => ...
						parameterDeclarationExpression.AddChild (ConvertToType (p.TypeExpression), Roles.Type);
					if (p.Name != null)
						parameterDeclarationExpression.AddChild (Identifier.Create (p.Name, Convert (p.Location)), Roles.Identifier);
					if (p.HasDefaultValue) {
						if (location != null && location.Count > 1)
							parameterDeclarationExpression.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.Assign);
						parameterDeclarationExpression.AddChild ((Expression)p.DefaultValue.Accept (this), Roles.Expression);
					}
					parent.AddChild (parameterDeclarationExpression, Roles.Parameter);
					if (paramLocation != null && i < paramLocation.Count) {
						parent.AddChild (new CSharpTokenNode (Convert (paramLocation [i])), Roles.Comma);
					}
				}
			}
			
			void AddTypeParameters (AstNode parent, MemberName memberName)
			{
				if (memberName == null || memberName.TypeParameters == null)
					return;
				var chevronLocs = LocationsBag.GetLocations (memberName.TypeParameters);
				if (chevronLocs != null)
					parent.AddChild (new CSharpTokenNode (Convert (chevronLocs [chevronLocs.Count - 2])), Roles.LChevron);
				for (int i = 0; i < memberName.TypeParameters.Count; i++) {
					if (chevronLocs != null && i > 0 && i - 1 < chevronLocs.Count)
						parent.AddChild (new CSharpTokenNode (Convert (chevronLocs [i - 1])), Roles.Comma);
					var arg = memberName.TypeParameters [i];
					if (arg == null)
						continue;
					TypeParameterDeclaration tp = new TypeParameterDeclaration ();
					
					List<Location> varianceLocation;
					switch (arg.Variance) {
						case Variance.Contravariant:
							tp.Variance = VarianceModifier.Contravariant;
							varianceLocation = LocationsBag.GetLocations (arg);
							if (varianceLocation != null)
								tp.AddChild (new CSharpTokenNode (Convert (varianceLocation [0])), TypeParameterDeclaration.InVarianceKeywordRole);
							break;
						case Variance.Covariant:
							tp.Variance = VarianceModifier.Covariant;
							varianceLocation = LocationsBag.GetLocations (arg);
							if (varianceLocation != null)
								tp.AddChild (new CSharpTokenNode (Convert (varianceLocation [0])), TypeParameterDeclaration.OutVarianceKeywordRole);
							break;
						default:
							tp.Variance = VarianceModifier.Invariant;
							break;
							
					}
					
					AddAttributeSection (tp, arg.OptAttributes);

					switch (arg.Variance) {
						case Variance.Covariant:
							tp.Variance = VarianceModifier.Covariant;
							break;
						case Variance.Contravariant:
							tp.Variance = VarianceModifier.Contravariant;
							break;
					}
					tp.AddChild (Identifier.Create (arg.Name, Convert (arg.Location)), Roles.Identifier);
					parent.AddChild (tp, Roles.TypeParameter);
				}
				if (chevronLocs != null)
					parent.AddChild (new CSharpTokenNode (Convert (chevronLocs [chevronLocs.Count - 1])), Roles.RChevron);
			}
			
			void AddTypeArguments (AstNode parent, MemberName memberName)
			{
				if (memberName == null || memberName.TypeParameters == null)
					return;
				var chevronLocs = LocationsBag.GetLocations (memberName.TypeParameters);
				if (chevronLocs != null)
					parent.AddChild (new CSharpTokenNode (Convert (chevronLocs [chevronLocs.Count - 2])), Roles.LChevron);
				
				for (int i = 0; i < memberName.TypeParameters.Count; i++) {
					var arg = memberName.TypeParameters [i];
					if (arg == null)
						continue;
					parent.AddChild (ConvertToType (arg), Roles.TypeArgument);
					if (chevronLocs != null && i < chevronLocs.Count - 2)
						parent.AddChild (new CSharpTokenNode (Convert (chevronLocs [i])), Roles.Comma);
				}
				
				if (chevronLocs != null)
					parent.AddChild (new CSharpTokenNode (Convert (chevronLocs [chevronLocs.Count - 1])), Roles.RChevron);
			}
			
			void AddTypeArguments (AstNode parent, ATypeNameExpression memberName)
			{
				if (memberName == null || !memberName.HasTypeArguments)
					return;
				var chevronLocs = LocationsBag.GetLocations (memberName.TypeArguments);
				if (chevronLocs != null)
					parent.AddChild (new CSharpTokenNode (Convert (chevronLocs [chevronLocs.Count - 2])), Roles.LChevron);
				
				for (int i = 0; i < memberName.TypeArguments.Count; i++) {
					var arg = memberName.TypeArguments.Args [i];
					if (arg == null)
						continue;
					parent.AddChild (ConvertToType (arg), Roles.TypeArgument);
					if (chevronLocs != null && i < chevronLocs.Count - 2)
						parent.AddChild (new CSharpTokenNode (Convert (chevronLocs [i])), Roles.Comma);
				}
				
				if (chevronLocs != null)
					parent.AddChild (new CSharpTokenNode (Convert (chevronLocs [chevronLocs.Count - 1])), Roles.RChevron);
			}
			
			void AddConstraints(AstNode parent, TypeParameters d)
			{
				if (d == null)
					return;
				for (int i = d.Count - 1; i >= 0; i--) {
					var typeParameter = d [i];
					if (typeParameter == null)
						continue;
					var c = typeParameter.Constraints;
					if (c == null)
						continue;
					var location = LocationsBag.GetLocations (c);
					var constraint = new Constraint ();
					constraint.AddChild (new CSharpTokenNode (Convert (c.Location)), Roles.WhereKeyword);
					constraint.AddChild (new SimpleType (Identifier.Create (c.TypeParameter.Value, Convert (c.TypeParameter.Location))), Roles.ConstraintTypeParameter);
					if (location != null)
						constraint.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.Colon);
					var commaLocs = LocationsBag.GetLocations (c.ConstraintExpressions);
					int curComma = 0;
					if (c.ConstraintExpressions != null) {
						foreach (var expr in c.ConstraintExpressions) {
							constraint.AddChild (ConvertToType (expr), Roles.BaseType);
							if (commaLocs != null && curComma < commaLocs.Count)
								constraint.AddChild (new CSharpTokenNode (Convert (commaLocs [curComma++])), Roles.Comma);
						}
					}
					
					parent.AddChild (constraint, Roles.Constraint);
				}
			}
			
			Expression ConvertArgument (Argument arg)
			{
				if (arg is NamedArgument) {
					var na = (NamedArgument)arg;
					NamedArgumentExpression newArg = new NamedArgumentExpression ();
					newArg.AddChild (Identifier.Create (na.Name, Convert (na.Location)), Roles.Identifier);
					
					var loc = LocationsBag.GetLocations (na);
					if (loc != null)
						newArg.AddChild (new CSharpTokenNode (Convert (loc [0])), Roles.Colon);
					
					if (arg.ArgType == Argument.AType.Out || arg.ArgType == Argument.AType.Ref) {
						DirectionExpression direction = new DirectionExpression ();
						direction.FieldDirection = arg.ArgType == Argument.AType.Out ? FieldDirection.Out : FieldDirection.Ref;
						var argLocation = LocationsBag.GetLocations (arg);
						if (argLocation != null)
							direction.AddChild (new CSharpTokenNode (Convert (argLocation [0])), arg.ArgType == Argument.AType.Out ? DirectionExpression.OutKeywordRole : DirectionExpression.RefKeywordRole);
						direction.AddChild ((Expression)arg.Expr.Accept (this), Roles.Expression);
						newArg.AddChild (direction, Roles.Expression);
					} else {
						newArg.AddChild ((Expression)na.Expr.Accept (this), Roles.Expression);
					}
					return newArg;
				}
				
				if (arg.ArgType == Argument.AType.Out || arg.ArgType == Argument.AType.Ref) {
					DirectionExpression direction = new DirectionExpression ();
					direction.FieldDirection = arg.ArgType == Argument.AType.Out ? FieldDirection.Out : FieldDirection.Ref;
					var argLocation = LocationsBag.GetLocations (arg);
					if (argLocation != null)
						direction.AddChild (new CSharpTokenNode (Convert (argLocation [0])), arg.ArgType == Argument.AType.Out ? DirectionExpression.OutKeywordRole : DirectionExpression.RefKeywordRole);
					direction.AddChild ((Expression)arg.Expr.Accept (this), Roles.Expression);
					return direction;
				}
				
				return (Expression)arg.Expr.Accept (this);
			}
			
			void AddArguments (AstNode parent, object location, Mono.CSharp.Arguments args)
			{
				if (args == null)
					return;
				
				var commaLocations = LocationsBag.GetLocations (args);
				for (int i = 0; i < args.Count; i++) {
					parent.AddChild (ConvertArgument (args [i]), Roles.Argument);
					if (commaLocations != null && i < commaLocations.Count) {
						parent.AddChild (new CSharpTokenNode (Convert (commaLocations [i])), Roles.Comma);
					}
				}
				if (commaLocations != null && commaLocations.Count > args.Count)
					parent.AddChild (new CSharpTokenNode (Convert (commaLocations [args.Count])), Roles.Comma);
			}
			
			public override object Visit (Invocation invocationExpression)
			{
				var result = new InvocationExpression ();
				var location = LocationsBag.GetLocations (invocationExpression);
				if (invocationExpression.Exp != null)
					result.AddChild ((Expression)invocationExpression.Exp.Accept (this), Roles.TargetExpression);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.LPar);
				AddArguments (result, location, invocationExpression.Arguments);
				
				if (location != null && location.Count > 1)
					result.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.RPar);
				return result;
			}
			
			public override object Visit (New newExpression)
			{
				var result = new ObjectCreateExpression ();
				var location = LocationsBag.GetLocations (newExpression);
				result.AddChild (new CSharpTokenNode (Convert (newExpression.Location)), ObjectCreateExpression.NewKeywordRole);
				
				if (newExpression.TypeRequested != null)
					result.AddChild (ConvertToType (newExpression.TypeRequested), Roles.Type);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.LPar);
				AddArguments (result, location, newExpression.Arguments);
				
				if (location != null && location.Count > 1)
					result.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.RPar);
				
				return result;
			}
			
			public override object Visit (NewAnonymousType newAnonymousType)
			{
				var result = new AnonymousTypeCreateExpression ();
				var location = LocationsBag.GetLocations (newAnonymousType);
				result.AddChild (new CSharpTokenNode (Convert (newAnonymousType.Location)), ObjectCreateExpression.NewKeywordRole);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.LBrace);
				if (newAnonymousType.Parameters != null) {
					foreach (var par in newAnonymousType.Parameters) {
						if (par == null)
							continue;
						var parLocation = LocationsBag.GetLocations (par);
						
						if (parLocation == null) {
							if (par.Expr != null)
								result.AddChild ((Expression)par.Expr.Accept (this), Roles.Expression);
						} else {
							var namedExpression = new NamedExpression ();
							namedExpression.AddChild (Identifier.Create (par.Name, Convert (par.Location)), Roles.Identifier);
							namedExpression.AddChild (new CSharpTokenNode (Convert (parLocation [0])), Roles.Assign);
							if (par.Expr != null)
								namedExpression.AddChild ((Expression)par.Expr.Accept (this), Roles.Expression);
							result.AddChild (namedExpression, Roles.Expression);
						}
					}
				}
				if (location != null && location.Count > 1)
					result.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.RBrace);
				return result;
			}

			ArrayInitializerExpression ConvertCollectionOrObjectInitializers(CollectionOrObjectInitializers minit)
			{
				if (minit == null)
					return null;
				var init = new ArrayInitializerExpression();
				AddConvertCollectionOrObjectInitializers(init, minit);
				return init;
			}

			void AddConvertCollectionOrObjectInitializers(Expression init, CollectionOrObjectInitializers minit)
			{
				var initLoc = LocationsBag.GetLocations(minit);
				var commaLoc = LocationsBag.GetLocations(minit.Initializers);
				int curComma = 0;
				if (initLoc != null)
					init.AddChild(new CSharpTokenNode(Convert(initLoc [0])), Roles.LBrace);
				foreach (var expr in minit.Initializers) {
					var collectionInit = expr as CollectionElementInitializer;
					if (collectionInit != null) {
						AstNode parent;
						// For ease of use purposes in the resolver the ast representation
						// of { a, b, c }  is { {a}, {b}, {c} } - but the generated ArrayInitializerExpression
						// can be identified by expr.IsSingleElement.
						if (!collectionInit.IsSingle) {
							parent = new ArrayInitializerExpression();
							parent.AddChild(new CSharpTokenNode(Convert(collectionInit.Location)), Roles.LBrace);
						} else {
							parent = ArrayInitializerExpression.CreateSingleElementInitializer ();
						}
						for (int i = 0; i < collectionInit.Arguments.Count; i++) {
							var arg = collectionInit.Arguments [i] as CollectionElementInitializer.ElementInitializerArgument;
							if (arg == null)
								continue;
							parent.AddChild(
								(ICSharpCode.NRefactory.CSharp.Expression)arg.Expr.Accept(this),
								Roles.Expression
							);
						}

						if (!collectionInit.IsSingle) {
							var braceLocs = LocationsBag.GetLocations(expr);
							if (braceLocs != null)
								parent.AddChild(new CSharpTokenNode(Convert(braceLocs [0])), Roles.RBrace);
						}
						init.AddChild((ArrayInitializerExpression)parent, Roles.Expression);
					} else {
						var eleInit = expr as ElementInitializer;
						if (eleInit != null) {
							var nexpr = new NamedExpression();
							nexpr.AddChild(
								Identifier.Create(eleInit.Name, Convert(eleInit.Location)),
								Roles.Identifier
							);
							var assignLoc = LocationsBag.GetLocations(eleInit);
							if (assignLoc != null)
								nexpr.AddChild(new CSharpTokenNode(Convert(assignLoc [0])), Roles.Assign);
							if (eleInit.Source != null) {
								if (eleInit.Source is CollectionOrObjectInitializers) {
									var arrInit = new ArrayInitializerExpression();
									AddConvertCollectionOrObjectInitializers(
										arrInit,
										eleInit.Source as CollectionOrObjectInitializers
									);
									nexpr.AddChild(arrInit, Roles.Expression);
								} else {
									nexpr.AddChild((Expression)eleInit.Source.Accept(this), Roles.Expression);
								}
							}

							init.AddChild(nexpr, Roles.Expression);
						}
					}
					if (commaLoc != null && curComma < commaLoc.Count)
						init.AddChild(new CSharpTokenNode(Convert(commaLoc [curComma++])), Roles.Comma);
				}

				if (initLoc != null) {
					if (initLoc.Count == 3) // optional comma
						init.AddChild(new CSharpTokenNode(Convert(initLoc [1])), Roles.Comma);
					init.AddChild(new CSharpTokenNode(Convert(initLoc [initLoc.Count - 1])), Roles.RBrace);
				}
			}

			
			public override object Visit (NewInitialize newInitializeExpression)
			{
				var result = new ObjectCreateExpression ();
				result.AddChild (new CSharpTokenNode (Convert (newInitializeExpression.Location)), ObjectCreateExpression.NewKeywordRole);
				
				if (newInitializeExpression.TypeRequested != null)
					result.AddChild (ConvertToType (newInitializeExpression.TypeRequested), Roles.Type);
				
				var location = LocationsBag.GetLocations (newInitializeExpression);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.LPar);
				AddArguments (result, location, newInitializeExpression.Arguments);
				if (location != null && location.Count > 1)
					result.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.RPar);
				
				var init = ConvertCollectionOrObjectInitializers (newInitializeExpression.Initializers);
				if (init != null)
					result.AddChild (init, ObjectCreateExpression.InitializerRole);
				
				return result;
			}
			
			public override object Visit (ArrayCreation arrayCreationExpression)
			{
				var result = new ArrayCreateExpression ();
				
				var location = LocationsBag.GetLocations (arrayCreationExpression);
				result.AddChild (new CSharpTokenNode (Convert (arrayCreationExpression.Location)), ArrayCreateExpression.NewKeywordRole);
				if (arrayCreationExpression.TypeExpression != null)
					result.AddChild (ConvertToType (arrayCreationExpression.TypeExpression), Roles.Type);
				
				var next = arrayCreationExpression.Rank;
				if (arrayCreationExpression.Arguments != null) {
					// skip first array rank.
					next = next.Next;
					
					if (location != null)
						result.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.LBracket);
					
					var commaLocations = LocationsBag.GetLocations (arrayCreationExpression.Arguments);
					for (int i = 0; i < arrayCreationExpression.Arguments.Count; i++) {
						result.AddChild ((Expression)arrayCreationExpression.Arguments [i].Accept (this), Roles.Argument);
						if (commaLocations != null && i < commaLocations.Count)
							result.AddChild (new CSharpTokenNode (Convert (commaLocations [i])), Roles.Comma);
					}
					if (location != null && location.Count > 1)
						result.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.RBracket);
					
				}
				
				while (next != null) {
					ArraySpecifier spec = new ArraySpecifier (next.Dimension);
					var loc = LocationsBag.GetLocations (next);
					spec.AddChild (new CSharpTokenNode (Convert (next.Location)), Roles.LBracket);
					result.AddChild (spec, ArrayCreateExpression.AdditionalArraySpecifierRole);
					if (loc != null)
						result.AddChild (new CSharpTokenNode (Convert (loc [0])), Roles.RBracket);
					next = next.Next;
				}
				
				if (arrayCreationExpression.Initializers != null) {
					var initLocation = LocationsBag.GetLocations (arrayCreationExpression.Initializers);
					ArrayInitializerExpression initializer = new ArrayInitializerExpression ();
					
					initializer.AddChild (new CSharpTokenNode (Convert (arrayCreationExpression.Initializers.Location)), Roles.LBrace);
					var commaLocations = LocationsBag.GetLocations (arrayCreationExpression.Initializers.Elements);
					for (int i = 0; i < arrayCreationExpression.Initializers.Count; i++) {
						var init = arrayCreationExpression.Initializers [i];
						if (init == null)
							continue;
						initializer.AddChild ((Expression)init.Accept (this), Roles.Expression);
						if (commaLocations != null && i < commaLocations.Count) {
							initializer.AddChild (new CSharpTokenNode (Convert (commaLocations [i])), Roles.Comma);
						}
					}
					if (initLocation != null) {
						if (initLocation.Count == 2) // optional comma
							initializer.AddChild (new CSharpTokenNode(Convert(initLocation [0])), Roles.Comma);
						initializer.AddChild (new CSharpTokenNode (Convert (initLocation [initLocation.Count - 1])), Roles.RBrace);
					}
					result.AddChild (initializer, ArrayCreateExpression.InitializerRole);
				}
				
				return result;
			}
			
			public override object Visit (This thisExpression)
			{
				var result = new ThisReferenceExpression ();
				result.Location = Convert (thisExpression.Location);
				return result;
			}
			
			public override object Visit (ArglistAccess argListAccessExpression)
			{
				var result = new UndocumentedExpression () {
					UndocumentedExpressionType = UndocumentedExpressionType.ArgListAccess
				};
				result.AddChild (new CSharpTokenNode (Convert (argListAccessExpression.Location)), UndocumentedExpression.ArglistKeywordRole);
				return result;
			}
			
			#region Undocumented expressions
			public override object Visit (Arglist argListExpression)
			{
				var result = new UndocumentedExpression () { UndocumentedExpressionType = UndocumentedExpressionType.ArgList };
				result.AddChild (new CSharpTokenNode (Convert (argListExpression.Location)), UndocumentedExpression.ArglistKeywordRole);
				var location = LocationsBag.GetLocations (argListExpression);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.LPar);
				
				AddArguments (result, location, argListExpression.Arguments);
				
				if (location != null && location.Count > 1)
					result.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.RPar);
				return result;
			}
			
			public override object Visit (MakeRefExpr makeRefExpr)
			{
				var result = new UndocumentedExpression () { UndocumentedExpressionType = UndocumentedExpressionType.MakeRef };
				result.AddChild (new CSharpTokenNode (Convert (makeRefExpr.Location)), UndocumentedExpression.MakerefKeywordRole);
				var location = LocationsBag.GetLocations (makeRefExpr);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.LPar);
				if (makeRefExpr.Expr != null)
					result.AddChild ((Expression)makeRefExpr.Expr.Accept (this), Roles.Argument);
				if (location != null && location.Count > 1)
					result.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.RPar);
				return result;
			}
			
			public override object Visit (RefTypeExpr refTypeExpr)
			{
				var result = new UndocumentedExpression () { UndocumentedExpressionType = UndocumentedExpressionType.RefType };
				result.AddChild (new CSharpTokenNode (Convert (refTypeExpr.Location)), UndocumentedExpression.ReftypeKeywordRole);
				var location = LocationsBag.GetLocations (refTypeExpr);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.LPar);
				
				if (refTypeExpr.Expr != null)
					result.AddChild ((Expression)refTypeExpr.Expr.Accept (this), Roles.Argument);
				
				if (location != null && location.Count > 1)
					result.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.RPar);
				return result;
			}
			
			public override object Visit (RefValueExpr refValueExpr)
			{
				var result = new UndocumentedExpression () { UndocumentedExpressionType = UndocumentedExpressionType.RefValue };
				result.AddChild (new CSharpTokenNode (Convert (refValueExpr.Location)), UndocumentedExpression.RefvalueKeywordRole);
				var location = LocationsBag.GetLocations (refValueExpr);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.LPar);
				
				
				if (refValueExpr.Expr != null)
					result.AddChild ((Expression)refValueExpr.Expr.Accept (this), Roles.Argument);

				if (refValueExpr.FullNamedExpression != null)
					result.AddChild ((Expression)refValueExpr.FullNamedExpression.Accept (this), Roles.Argument);
				
				if (location != null && location.Count > 1)
					result.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.RPar);
				return result;
			}
			#endregion
			
			public override object Visit (TypeOf typeOfExpression)
			{
				var result = new TypeOfExpression ();
				var location = LocationsBag.GetLocations (typeOfExpression);
				result.AddChild (new CSharpTokenNode (Convert (typeOfExpression.Location)), TypeOfExpression.TypeofKeywordRole);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.LPar);
				if (typeOfExpression.TypeExpression != null)
					result.AddChild (ConvertToType (typeOfExpression.TypeExpression), Roles.Type);
				if (location != null && location.Count > 1)
					result.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.RPar);
				return result;
			}
			
			public override object Visit (SizeOf sizeOfExpression)
			{
				var result = new SizeOfExpression ();
				var location = LocationsBag.GetLocations (sizeOfExpression);
				result.AddChild (new CSharpTokenNode (Convert (sizeOfExpression.Location)), SizeOfExpression.SizeofKeywordRole);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.LPar);
				if (sizeOfExpression.TypeExpression != null)
					result.AddChild (ConvertToType (sizeOfExpression.TypeExpression), Roles.Type);
				if (location != null && location.Count > 1)
					result.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.RPar);
				return result;
			}
			
			public override object Visit (CheckedExpr checkedExpression)
			{
				var result = new CheckedExpression ();
				var location = LocationsBag.GetLocations (checkedExpression);
				result.AddChild (new CSharpTokenNode (Convert (checkedExpression.Location)), CheckedExpression.CheckedKeywordRole);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.LPar);
				if (checkedExpression.Expr != null)
					result.AddChild ((Expression)checkedExpression.Expr.Accept (this), Roles.Expression);
				if (location != null && location.Count > 1)
					result.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.RPar);
				return result;
			}
			
			public override object Visit (UnCheckedExpr uncheckedExpression)
			{
				var result = new UncheckedExpression ();
				var location = LocationsBag.GetLocations (uncheckedExpression);
				result.AddChild (new CSharpTokenNode (Convert (uncheckedExpression.Location)), UncheckedExpression.UncheckedKeywordRole);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.LPar);
				if (uncheckedExpression.Expr != null)
					result.AddChild ((Expression)uncheckedExpression.Expr.Accept (this), Roles.Expression);
				if (location != null && location.Count > 1)
					result.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.RPar);
				return result;
			}
			
			public override object Visit (ElementAccess elementAccessExpression)
			{
				IndexerExpression result = new IndexerExpression ();
				var location = LocationsBag.GetLocations (elementAccessExpression);
				
				if (elementAccessExpression.Expr != null)
					result.AddChild ((Expression)elementAccessExpression.Expr.Accept (this), Roles.TargetExpression);
				result.AddChild (new CSharpTokenNode (Convert (elementAccessExpression.Location)), Roles.LBracket);
				AddArguments (result, location, elementAccessExpression.Arguments);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.RBracket);
				return result;
			}
			
			public override object Visit (BaseThis baseAccessExpression)
			{
				var result = new BaseReferenceExpression ();
				result.Location = Convert (baseAccessExpression.Location);
				return result;
			}
			
			public override object Visit (StackAlloc stackAllocExpression)
			{
				var result = new StackAllocExpression ();
				
				var location = LocationsBag.GetLocations (stackAllocExpression);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), StackAllocExpression.StackallocKeywordRole);
				if (stackAllocExpression.TypeExpression != null)
					result.AddChild (ConvertToType (stackAllocExpression.TypeExpression), Roles.Type);
				if (location != null && location.Count > 1)
					result.AddChild (new CSharpTokenNode (Convert (location [1])), Roles.LBracket);
				if (stackAllocExpression.CountExpression != null)
					result.AddChild ((Expression)stackAllocExpression.CountExpression.Accept (this), Roles.Expression);
				if (location != null && location.Count > 2)
					result.AddChild (new CSharpTokenNode (Convert (location [2])), Roles.RBracket);
				return result;
			}
			
			public override object Visit (SimpleAssign simpleAssign)
			{
				var result = new AssignmentExpression ();
				
				result.Operator = AssignmentOperatorType.Assign;
				if (simpleAssign.Target != null)
					result.AddChild ((Expression)simpleAssign.Target.Accept (this), AssignmentExpression.LeftRole);
				var location = LocationsBag.GetLocations (simpleAssign);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0])), AssignmentExpression.AssignRole);
				if (simpleAssign.Source != null) {
					result.AddChild ((Expression)simpleAssign.Source.Accept (this), AssignmentExpression.RightRole);
				}
				return result;
			}
			
			public override object Visit (CompoundAssign compoundAssign)
			{
				var result = new AssignmentExpression ();
				switch (compoundAssign.Op) {
					case Binary.Operator.Multiply:
						result.Operator = AssignmentOperatorType.Multiply;
						break;
					case Binary.Operator.Division:
						result.Operator = AssignmentOperatorType.Divide;
						break;
					case Binary.Operator.Modulus:
						result.Operator = AssignmentOperatorType.Modulus;
						break;
					case Binary.Operator.Addition:
						result.Operator = AssignmentOperatorType.Add;
						break;
					case Binary.Operator.Subtraction:
						result.Operator = AssignmentOperatorType.Subtract;
						break;
					case Binary.Operator.LeftShift:
						result.Operator = AssignmentOperatorType.ShiftLeft;
						break;
					case Binary.Operator.RightShift:
						result.Operator = AssignmentOperatorType.ShiftRight;
						break;
					case Binary.Operator.BitwiseAnd:
						result.Operator = AssignmentOperatorType.BitwiseAnd;
						break;
					case Binary.Operator.BitwiseOr:
						result.Operator = AssignmentOperatorType.BitwiseOr;
						break;
					case Binary.Operator.ExclusiveOr:
						result.Operator = AssignmentOperatorType.ExclusiveOr;
						break;
				}
				
				if (compoundAssign.Target != null)
					result.AddChild ((Expression)compoundAssign.Target.Accept (this), AssignmentExpression.LeftRole);
				var location = LocationsBag.GetLocations (compoundAssign);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0])), AssignmentExpression.GetOperatorRole (result.Operator));
				if (compoundAssign.Source != null)
					result.AddChild ((Expression)compoundAssign.Source.Accept (this), AssignmentExpression.RightRole);
				return result;
			}
			
			public override object Visit (Mono.CSharp.AnonymousMethodExpression anonymousMethodExpression)
			{
				var result = new AnonymousMethodExpression ();
				var location = LocationsBag.GetLocations (anonymousMethodExpression);
				int l = 0;
				if (anonymousMethodExpression.IsAsync) {
					result.IsAsync = true;
					result.AddChild (new CSharpTokenNode (Convert (location [l++])), AnonymousMethodExpression.AsyncModifierRole);
				}
				if (location != null) {
					result.AddChild (new CSharpTokenNode (Convert (location [l++])), AnonymousMethodExpression.DelegateKeywordRole);
					
					if (location.Count > l) {
						result.HasParameterList = true;
						result.AddChild (new CSharpTokenNode (Convert (location [l++])), Roles.LPar);
						AddParameter (result, anonymousMethodExpression.Parameters);
						result.AddChild (new CSharpTokenNode (Convert (location [l++])), Roles.RPar);
					}
				}
				if (anonymousMethodExpression.Block != null)
					result.AddChild ((BlockStatement)anonymousMethodExpression.Block.Accept (this), Roles.Body);
				return result;
			}

			public override object Visit (Mono.CSharp.LambdaExpression lambdaExpression)
			{
				var result = new LambdaExpression ();
				var location = LocationsBag.GetLocations (lambdaExpression);
				int l = 0;
				if (lambdaExpression.IsAsync) {
					result.IsAsync = true;
					result.AddChild (new CSharpTokenNode (Convert (location [l++])), LambdaExpression.AsyncModifierRole);
				}
				
				if (location == null || location.Count == l + 1) {
					AddParameter (result, lambdaExpression.Parameters);
					if (location != null)
						result.AddChild (new CSharpTokenNode (Convert (location [l++])), LambdaExpression.ArrowRole);
				} else {
					result.AddChild (new CSharpTokenNode (Convert (location [l++])), Roles.LPar);
					AddParameter (result, lambdaExpression.Parameters);
					if (location != null) {
						result.AddChild (new CSharpTokenNode (Convert (location [l++])), Roles.RPar);
						result.AddChild (new CSharpTokenNode (Convert (location [l++])), LambdaExpression.ArrowRole);
					}
				}
				if (lambdaExpression.Block != null) {
					if (lambdaExpression.Block.IsCompilerGenerated) {
						ContextualReturn generatedReturn = (ContextualReturn)lambdaExpression.Block.Statements [0];
						result.AddChild ((AstNode)generatedReturn.Expr.Accept (this), LambdaExpression.BodyRole);
					} else {
						result.AddChild ((AstNode)lambdaExpression.Block.Accept (this), LambdaExpression.BodyRole);
					}
				}
				
				return result;
			}
			
			public override object Visit (ConstInitializer constInitializer)
			{
				return constInitializer.Expr.Accept (this);
			}
			
			public override object Visit (ArrayInitializer arrayInitializer)
			{
				var result = new ArrayInitializerExpression ();
				var location = LocationsBag.GetLocations (arrayInitializer);
				result.AddChild (new CSharpTokenNode (Convert (arrayInitializer.Location)), Roles.LBrace);
				var commaLocations = LocationsBag.GetLocations (arrayInitializer.Elements);
				for (int i = 0; i < arrayInitializer.Count; i++) {
					var init = arrayInitializer [i];
					if (init == null)
						continue;
					result.AddChild ((Expression)init.Accept (this), Roles.Expression);
					if (commaLocations != null && i < commaLocations.Count)
						result.AddChild (new CSharpTokenNode (Convert (commaLocations [i])), Roles.Comma);
				}
				
				if (location != null) {
					if (location.Count == 2) // optional comma
						result.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.Comma);
					result.AddChild (new CSharpTokenNode (Convert (location [location.Count - 1])), Roles.RBrace);
				}
				return result;
			}
			
			#endregion
			
			#region LINQ expressions
			QueryOrderClause currentQueryOrderClause;

			public override object Visit (Mono.CSharp.Linq.QueryExpression queryExpression)
			{
				var oldQueryOrderClause = currentQueryOrderClause;
				try {
					currentQueryOrderClause = null;
					var result = new QueryExpression ();
					
					var currentClause = queryExpression.next;
					
					while (currentClause != null) {
						QueryClause clause = (QueryClause)currentClause.Accept (this);
						if (clause is QueryContinuationClause) {
							// insert preceding query at beginning of QueryContinuationClause
							clause.InsertChildAfter (null, result, QueryContinuationClause.PrecedingQueryRole);
							// create a new QueryExpression for the remaining query
							result = new QueryExpression ();
						}
						if (clause != null) {
							result.AddChild (clause, QueryExpression.ClauseRole);
						}
						currentClause = currentClause.next;
					}
					
					return result;
				}
				finally {
					currentQueryOrderClause = oldQueryOrderClause;
				}
			}
			
			public override object Visit (Mono.CSharp.Linq.QueryStartClause queryStart)
			{
				if (queryStart.Expr == null) {
					var intoClause = new QueryContinuationClause ();
					intoClause.AddChild (new CSharpTokenNode (Convert (queryStart.Location)), QueryContinuationClause.IntoKeywordRole);
					intoClause.AddChild (Identifier.Create (queryStart.IntoVariable.Name, Convert (queryStart.IntoVariable.Location)), Roles.Identifier);
					return intoClause;
				}
				
				var fromClause = new QueryFromClause ();

				fromClause.AddChild (new CSharpTokenNode (Convert (queryStart.Location)), QueryFromClause.FromKeywordRole);
				
				if (queryStart.IdentifierType != null)
					fromClause.AddChild (ConvertToType (queryStart.IdentifierType), Roles.Type);
				
				fromClause.AddChild (Identifier.Create (queryStart.IntoVariable.Name, Convert (queryStart.IntoVariable.Location)), Roles.Identifier);
				
				var location = LocationsBag.GetLocations (queryStart);
				if (location != null)
					fromClause.AddChild (new CSharpTokenNode (Convert (location [0])), QueryFromClause.InKeywordRole);

				if (queryStart.Expr != null)
					fromClause.AddChild ((Expression)queryStart.Expr.Accept (this), Roles.Expression);
				return fromClause;
			}
			
			public override object Visit (Mono.CSharp.Linq.SelectMany queryStart)
			{
				var fromClause = new QueryFromClause ();

				fromClause.AddChild (new CSharpTokenNode (Convert (queryStart.Location)), QueryFromClause.FromKeywordRole);
				
				if (queryStart.IdentifierType != null)
					fromClause.AddChild (ConvertToType (queryStart.IdentifierType), Roles.Type);
				
				fromClause.AddChild (Identifier.Create (queryStart.IntoVariable.Name, Convert (queryStart.IntoVariable.Location)), Roles.Identifier);
				
				var location = LocationsBag.GetLocations (queryStart);
				if (location != null)
					fromClause.AddChild (new CSharpTokenNode (Convert (location [0])), QueryFromClause.InKeywordRole);

				if (queryStart.Expr != null)
					fromClause.AddChild ((Expression)queryStart.Expr.Accept (this), Roles.Expression);
				return fromClause;
			}
			
			public override object Visit (Mono.CSharp.Linq.Select sel)
			{
				var result = new QuerySelectClause ();
				result.AddChild (new CSharpTokenNode (Convert (sel.Location)), QuerySelectClause.SelectKeywordRole);
				if (sel.Expr != null)
					result.AddChild ((Expression)sel.Expr.Accept (this), Roles.Expression);
				return result;
			}
			
			public override object Visit (Mono.CSharp.Linq.GroupBy groupBy)
			{
				var result = new QueryGroupClause ();
				var location = LocationsBag.GetLocations (groupBy);
				result.AddChild (new CSharpTokenNode (Convert (groupBy.Location)), QueryGroupClause.GroupKeywordRole);
				if (groupBy.ElementSelector != null)
					result.AddChild ((Expression)groupBy.ElementSelector.Accept (this), QueryGroupClause.ProjectionRole);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), QueryGroupClause.ByKeywordRole);
				if (groupBy.Expr != null)
					result.AddChild ((Expression)groupBy.Expr.Accept (this), QueryGroupClause.KeyRole);
				return result;
			}
			
			public override object Visit (Mono.CSharp.Linq.Let l)
			{
				var result = new QueryLetClause ();
				var location = LocationsBag.GetLocations (l);
				
				result.AddChild (new CSharpTokenNode (Convert (l.Location)), QueryLetClause.LetKeywordRole);
				result.AddChild (Identifier.Create (l.IntoVariable.Name, Convert (l.IntoVariable.Location)), Roles.Identifier);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), Roles.Assign);
				if (l.Expr != null)
					result.AddChild ((Expression)l.Expr.Accept (this), Roles.Expression);
				return result;
			}
			
			public override object Visit (Mono.CSharp.Linq.Where w)
			{
				var result = new QueryWhereClause ();
				var location = LocationsBag.GetLocations (w);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), QueryWhereClause.WhereKeywordRole);
				if (w.Expr != null)
					result.AddChild ((Expression)w.Expr.Accept (this), Roles.Condition);
				return result;
			}
			
			public override object Visit (Mono.CSharp.Linq.Join join)
			{
				var result = new QueryJoinClause ();
				var location = LocationsBag.GetLocations (join);
				result.AddChild (new CSharpTokenNode (Convert (join.Location)), QueryJoinClause.JoinKeywordRole);
				result.AddChild (Identifier.Create (join.JoinVariable.Name, Convert (join.JoinVariable.Location)), QueryJoinClause.JoinIdentifierRole);
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), QueryJoinClause.InKeywordRole);

				if (join.Expr != null)
					result.AddChild ((Expression)join.Expr.Accept (this), QueryJoinClause.InExpressionRole);
				
				if (location != null && location.Count > 1)
					result.AddChild (new CSharpTokenNode (Convert (location [1])), QueryJoinClause.OnKeywordRole);
				
				var outer = join.OuterSelector.Statements.FirstOrDefault () as ContextualReturn;
				if (outer != null)
					result.AddChild ((Expression)outer.Expr.Accept (this), QueryJoinClause.OnExpressionRole);
				
				if (location != null && location.Count > 2)
					result.AddChild (new CSharpTokenNode (Convert (location [2])), QueryJoinClause.EqualsKeywordRole);
				
				var inner = join.InnerSelector.Statements.FirstOrDefault () as ContextualReturn;
				if (inner != null)
					result.AddChild ((Expression)inner.Expr.Accept (this), QueryJoinClause.EqualsExpressionRole);
				
				return result;
			}
			
			public override object Visit (Mono.CSharp.Linq.GroupJoin join)
			{
				var result = new QueryJoinClause ();
				var location = LocationsBag.GetLocations (join);
				result.AddChild (new CSharpTokenNode (Convert (join.Location)), QueryJoinClause.JoinKeywordRole);
				
				// mcs seems to have swapped IntoVariable with JoinVariable, so we'll swap it back here
				result.AddChild (Identifier.Create (join.IntoVariable.Name, Convert (join.IntoVariable.Location)), QueryJoinClause.JoinIdentifierRole);
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location [0])), QueryJoinClause.InKeywordRole);
				
				if (join.Expr != null)
					result.AddChild ((Expression)join.Expr.Accept (this), QueryJoinClause.InExpressionRole);

				if (location != null && location.Count > 1)
					result.AddChild (new CSharpTokenNode (Convert (location [1])), QueryJoinClause.OnKeywordRole);

				var outer = join.OuterSelector.Statements.FirstOrDefault () as ContextualReturn;
				if (outer != null)
					result.AddChild ((Expression)outer.Expr.Accept (this), QueryJoinClause.OnExpressionRole);
				

				if (location != null && location.Count > 2)
					result.AddChild (new CSharpTokenNode (Convert (location [2])), QueryJoinClause.EqualsKeywordRole);
				var inner = join.InnerSelector.Statements.FirstOrDefault () as ContextualReturn;
				if (inner != null)
					result.AddChild ((Expression)inner.Expr.Accept (this), QueryJoinClause.EqualsExpressionRole);
				
				if (location != null && location.Count > 3)
					result.AddChild (new CSharpTokenNode (Convert (location [3])), QueryJoinClause.IntoKeywordRole);
				
				result.AddChild (Identifier.Create (join.JoinVariable.Name, Convert (join.JoinVariable.Location)), QueryJoinClause.IntoIdentifierRole);
				return result;
			}
			
			public override object Visit (Mono.CSharp.Linq.OrderByAscending orderByAscending)
			{
				currentQueryOrderClause = new QueryOrderClause();
				
				var ordering = new QueryOrdering ();
				if (orderByAscending.Expr != null)
					ordering.AddChild ((Expression)orderByAscending.Expr.Accept (this), Roles.Expression);
				var location = LocationsBag.GetLocations (orderByAscending);
				if (location != null) {
					ordering.Direction = QueryOrderingDirection.Ascending;
					ordering.AddChild (new CSharpTokenNode (Convert (location [0])), QueryOrdering.AscendingKeywordRole);
				}
				currentQueryOrderClause.AddChild (ordering, QueryOrderClause.OrderingRole);
				return currentQueryOrderClause;
			}
			
			public override object Visit (Mono.CSharp.Linq.OrderByDescending orderByDescending)
			{
				currentQueryOrderClause = new QueryOrderClause ();
				
				var ordering = new QueryOrdering ();
				if (orderByDescending.Expr != null)
					ordering.AddChild ((Expression)orderByDescending.Expr.Accept (this), Roles.Expression);
				var location = LocationsBag.GetLocations (orderByDescending);
				if (location != null) {
					ordering.Direction = QueryOrderingDirection.Descending;
					ordering.AddChild (new CSharpTokenNode (Convert (location [0])), QueryOrdering.DescendingKeywordRole);
				}
				currentQueryOrderClause.AddChild (ordering, QueryOrderClause.OrderingRole);
				return currentQueryOrderClause;
			}
			
			public override object Visit (Mono.CSharp.Linq.ThenByAscending thenByAscending)
			{
				var ordering = new QueryOrdering ();
				if (thenByAscending.Expr != null)
					ordering.AddChild ((Expression)thenByAscending.Expr.Accept (this), Roles.Expression);
				var location = LocationsBag.GetLocations (thenByAscending);
				if (location != null) {
					ordering.Direction = QueryOrderingDirection.Ascending;
					ordering.AddChild (new CSharpTokenNode (Convert (location [0])), QueryOrdering.AscendingKeywordRole);
				}
				currentQueryOrderClause.AddChild (ordering, QueryOrderClause.OrderingRole);
				return null;
			}
			
			public override object Visit (Mono.CSharp.Linq.ThenByDescending thenByDescending)
			{
				var ordering = new QueryOrdering ();
				if (thenByDescending.Expr != null)
					ordering.AddChild ((Expression)thenByDescending.Expr.Accept (this), Roles.Expression);
				var location = LocationsBag.GetLocations (thenByDescending);
				if (location != null) {
					ordering.Direction = QueryOrderingDirection.Descending;
					ordering.AddChild (new CSharpTokenNode (Convert (location [0])), QueryOrdering.DescendingKeywordRole);
				}
				currentQueryOrderClause.AddChild (ordering, QueryOrderClause.OrderingRole);
				return null;
			}
			
			public override object Visit (Await awaitExpr)
			{
				var result = new UnaryOperatorExpression ();
				result.Operator = UnaryOperatorType.Await;
				result.AddChild (new CSharpTokenNode (Convert (awaitExpr.Location)), UnaryOperatorExpression.AwaitRole);
				if (awaitExpr.Expression != null)
					result.AddChild ((Expression)awaitExpr.Expression.Accept (this), Roles.Expression);
				return result;
			}
			#endregion
			
			#region XmlDoc
			public DocumentationReference ConvertXmlDoc(DocumentationBuilder doc)
			{
				DocumentationReference result = new DocumentationReference();
				if (doc.ParsedName != null) {
					if (doc.ParsedName.Name == "<this>") {
						result.EntityType = EntityType.Indexer;
					} else {
						result.MemberName = doc.ParsedName.Name;
					}
					if (doc.ParsedName.Left != null) {
						result.DeclaringType = ConvertToType(doc.ParsedName.Left);
					} else if (doc.ParsedBuiltinType != null) {
						result.DeclaringType = ConvertToType(doc.ParsedBuiltinType);
					}
					if (doc.ParsedName.TypeParameters != null) {
						for (int i = 0; i < doc.ParsedName.TypeParameters.Count; i++) {
							result.TypeArguments.Add(ConvertToType(doc.ParsedName.TypeParameters[i]));
						}
					}
				} else if (doc.ParsedBuiltinType != null) {
					result.EntityType = EntityType.TypeDefinition;
					result.DeclaringType = ConvertToType(doc.ParsedBuiltinType);
				}
				if (doc.ParsedParameters != null) {
					result.HasParameterList = true;
					result.Parameters.AddRange(doc.ParsedParameters.Select(ConvertXmlDocParameter));
				}
				if (doc.ParsedOperator != null) {
					result.EntityType = EntityType.Operator;
					result.OperatorType = (OperatorType)doc.ParsedOperator;
					if (result.OperatorType == OperatorType.Implicit || result.OperatorType == OperatorType.Explicit) {
						var returnTypeParam = result.Parameters.LastOrNullObject();
						returnTypeParam.Remove(); // detach from parameter list
						var returnType = returnTypeParam.Type;
						returnType.Remove();
						result.ConversionOperatorReturnType = returnType;
					}
					if (result.Parameters.Count == 0) {
						// reset HasParameterList if necessary
						result.HasParameterList = false;
					}
				}
				return result;
			}
			
			ParameterDeclaration ConvertXmlDocParameter(DocumentationParameter p)
			{
				ParameterDeclaration result = new ParameterDeclaration();
				switch (p.Modifier) {
					case Parameter.Modifier.OUT:
						result.ParameterModifier = ParameterModifier.Out;
						break;
					case Parameter.Modifier.REF:
						result.ParameterModifier = ParameterModifier.Ref;
						break;
					case Parameter.Modifier.PARAMS:
						result.ParameterModifier = ParameterModifier.Params;
						break;
				}
				if (p.Type != null) {
					result.Type = ConvertToType(p.Type);
				}
				return result;
			}
			#endregion
		}
		
		public CSharpParser ()
		{
			compilerSettings = new CompilerSettings();
		}
		
		public CSharpParser (CompilerSettings args)
		{
			compilerSettings = args ?? new CompilerSettings();
		}
		
		static AstNode GetOuterLeft (AstNode node)
		{
			var outerLeft = node;
			while (outerLeft.FirstChild != null)
				outerLeft = outerLeft.FirstChild;
			return outerLeft;
		}
		
		static AstNode NextLeaf (AstNode node)
		{
			if (node == null)
				return null;
			var next = node.NextSibling;
			if (next == null)
				return node.Parent;
			return GetOuterLeft (next);
		}
		
		static void InsertComments (CompilerCompilationUnit top, ConversionVisitor conversionVisitor)
		{
			var leaf = GetOuterLeft (conversionVisitor.Unit);
			for (int i = 0; i < top.SpecialsBag.Specials.Count; i++) {
				var special = top.SpecialsBag.Specials [i];
				AstNode newLeaf = null;
				var comment = special as SpecialsBag.Comment;
				if (comment != null) {
					// HACK: multiline documentation comment detection; better move this logic into the mcs tokenizer
					bool isMultilineDocumentationComment = (
						comment.CommentType == SpecialsBag.CommentType.Multi
						&& comment.Content.StartsWith("*", StringComparison.Ordinal)
						&& !comment.Content.StartsWith("**", StringComparison.Ordinal)
					);
					if (conversionVisitor.convertTypeSystemMode && !(comment.CommentType == SpecialsBag.CommentType.Documentation || isMultilineDocumentationComment))
						continue;
					var type = isMultilineDocumentationComment ? CommentType.MultiLineDocumentation : (CommentType)comment.CommentType;
					var start = new TextLocation (comment.Line, comment.Col);
					var end = new TextLocation (comment.EndLine, comment.EndCol);
					newLeaf = new Comment (type, start, end) {
						StartsLine = comment.StartsLine,
						Content = isMultilineDocumentationComment ? comment.Content.Substring(1) : comment.Content
					};
				} else {
					var directive = special as SpecialsBag.PreProcessorDirective;
					if (directive != null) {
						newLeaf = new PreProcessorDirective ((ICSharpCode.NRefactory.CSharp.PreProcessorDirectiveType)((int)directive.Cmd & 0xF), new TextLocation (directive.Line, directive.Col), new TextLocation (directive.EndLine, directive.EndCol)) {
							Argument = directive.Arg,
							Take = directive.Take
						};
					}
				}
				if (newLeaf == null)
					continue;
				
				while (true) {
					var nextLeaf = NextLeaf (leaf);
					// insert comment at begin
					if (newLeaf.StartLocation < leaf.StartLocation) {
						var node = leaf.Parent ?? conversionVisitor.Unit;
						while (node.Parent != null && node.FirstChild == leaf) {
							leaf = node;
							node = node.Parent;
						}
						if (newLeaf is Comment) {
							node.InsertChildBefore (leaf, (Comment)newLeaf, Roles.Comment);
						} else {
							node.InsertChildBefore (leaf, (PreProcessorDirective)newLeaf, Roles.PreProcessorDirective);
						}
						leaf = newLeaf;
						break;
					}
					
					// insert comment at the end
					if (nextLeaf == null) {
						var node = leaf.Parent ?? conversionVisitor.Unit;
						if (newLeaf is Comment) {
							node.AddChild ((Comment)newLeaf, Roles.Comment);
						} else {
							node.AddChild ((PreProcessorDirective)newLeaf, Roles.PreProcessorDirective);
						}
						leaf = newLeaf;
						break;
					}
					
					// comment is between 2 nodes
					if (leaf.EndLocation <= newLeaf.StartLocation && newLeaf.StartLocation <= nextLeaf.StartLocation) {
						var node = leaf.Parent ?? conversionVisitor.Unit;
						if (newLeaf is Comment) {
							node.InsertChildAfter (leaf, (Comment)newLeaf, Roles.Comment);
						} else {
							node.InsertChildAfter (leaf, (PreProcessorDirective)newLeaf, Roles.PreProcessorDirective);
						}
						leaf = newLeaf;
						break;
					}
					leaf = nextLeaf;
				}
			}
		}
		
		public class ErrorReportPrinter : ReportPrinter
		{
			readonly string fileName;
			public readonly List<Error> Errors = new List<Error> ();
			
			public ErrorReportPrinter (string fileName)
			{
				this.fileName = fileName;
			}
			
			public override void Print (AbstractMessage msg, bool showFullPath)
			{
				base.Print (msg, showFullPath);
				var newError = new Error (msg.IsWarning ? ErrorType.Warning : ErrorType.Error, msg.Text, new DomRegion (fileName, msg.Location.Row, msg.Location.Column));
				Errors.Add (newError);
			}
		}
		ErrorReportPrinter errorReportPrinter = new ErrorReportPrinter (null);
		
		[Obsolete("Use the Errors/Warnings/ErrorsAndWarnings properties instead")]
		public ErrorReportPrinter ErrorPrinter {
			get {
				return errorReportPrinter;
			}
		}
		
		public bool HasErrors {
			get {
				return errorReportPrinter.ErrorsCount > 0;
			}
		}
		
		public bool HasWarnings {
			get {
				return errorReportPrinter.WarningsCount > 0;
			}
		}
		
		public IEnumerable<Error> Errors {
			get {
				return errorReportPrinter.Errors.Where(e => e.ErrorType == ErrorType.Error);
			}
		}
		
		public IEnumerable<Error> Warnings {
			get {
				return errorReportPrinter.Errors.Where(e => e.ErrorType == ErrorType.Warning);
			}
		}
		
		public IEnumerable<Error> ErrorsAndWarnings {
			get { return errorReportPrinter.Errors; }
		}
		
		/// <summary>
		/// Parses a C# code file.
		/// </summary>
		/// <param name="program">The source code to parse.</param>
		/// <param name="fileName">The file name. Used to identify the file (e.g. when building a type system).
		/// This can be an arbitrary identifier, NRefactory never tries to access the file on disk.</param>
		/// <returns>Returns the syntax tree.</returns>
		public SyntaxTree Parse (string program, string fileName = "")
		{
			return Parse (new StringTextSource (program), fileName);
		}
		
		/// <summary>
		/// Parses a C# code file.
		/// </summary>
		/// <param name="reader">The text reader containing the source code to parse.</param>
		/// <param name="fileName">The file name. Used to identify the file (e.g. when building a type system).
		/// This can be an arbitrary identifier, NRefactory never tries to access the file on disk.</param>
		/// <returns>Returns the syntax tree.</returns>
		public SyntaxTree Parse (TextReader reader, string fileName = "")
		{
			return Parse(new StringTextSource (reader.ReadToEnd ()), fileName);
		}

		/// <summary>
		/// Converts a Mono.CSharp syntax tree into an NRefactory syntax tree.
		/// </summary>
		public SyntaxTree Parse(CompilerCompilationUnit top, string fileName)
		{
			if (top == null) {
				return null;
			}
			CSharpParser.ConversionVisitor conversionVisitor = new ConversionVisitor (GenerateTypeSystemMode, top.LocationsBag);
			top.ModuleCompiled.Accept(conversionVisitor);
			InsertComments(top, conversionVisitor);
			if (CompilationUnitCallback != null) {
				CompilationUnitCallback(top);
			}
			if (top.LastYYValue is Mono.CSharp.Expression) {
				conversionVisitor.Unit.TopExpression = ((Mono.CSharp.Expression)top.LastYYValue).Accept(conversionVisitor) as AstNode;
			}

			conversionVisitor.Unit.FileName = fileName;
			conversionVisitor.Unit.ConditionalSymbols = top.Conditionals.Concat (compilerSettings.ConditionalSymbols).ToArray ();
			return conversionVisitor.Unit;
		}
		
		public CompilerSettings CompilerSettings {
			get { return compilerSettings; }
			set {
				if (value == null)
					throw new ArgumentNullException();
				compilerSettings = value;
			}
		}
		
		/// <summary>
		/// Callback that gets called with the Mono.CSharp syntax tree whenever some code is parsed.
		/// </summary>
		public Action<CompilerCompilationUnit> CompilationUnitCallback {
			get;
			set;
		}
		
		/// <summary>
		/// Specifies whether to run the parser in a special mode for generating the type system.
		/// If this property is true, the syntax tree will only contain nodes relevant for the
		/// <see cref="SyntaxTree.ToTypeSystem()"/> call and might be missing other nodes (e.g. method bodies).
		/// The default is false.
		/// </summary>
		public bool GenerateTypeSystemMode {
			get;
			set;
		}
		
		TextLocation initialLocation = new TextLocation(1, 1);
		
		/// <summary>
		/// Specifies the text location where parsing starts.
		/// This property can be used when parsing a part of a file to make the locations of the AstNodes
		/// refer to the position in the whole file.
		/// The default is (1,1).
		/// </summary>
		public TextLocation InitialLocation {
			get { return initialLocation; }
			set { initialLocation = value; }
		}
		
		internal static object parseLock = new object ();
		
		/// <summary>
		/// Parses a C# code file.
		/// </summary>
		/// <param name="stream">The stream containing the source code to parse.</param>
		/// <param name="fileName">The file name. Used to identify the file (e.g. when building a type system).
		/// This can be an arbitrary identifier, NRefactory never tries to access the file on disk.</param>
		/// <returns>Returns the syntax tree.</returns>
		public SyntaxTree Parse (Stream stream, string fileName = "")
		{
			return Parse (new StreamReader (stream), fileName);
		}
		
		/// <summary>
		/// Parses a C# code file.
		/// </summary>
		/// <param name="program">The source code to parse.</param>
		/// <param name="fileName">The file name. Used to identify the file (e.g. when building a type system).
		/// This can be an arbitrary identifier, NRefactory never tries to access the file on disk.</param>
		///  </param>
		/// <returns>Returns the syntax tree.</returns>
		public SyntaxTree Parse(ITextSource program, string fileName = "")
		{
			return Parse(program, fileName, initialLocation.Line, initialLocation.Column);
		}
		
		SyntaxTree Parse(ITextSource program, string fileName, int initialLine, int initialColumn)
		{
			lock (parseLock) {
				errorReportPrinter = new ErrorReportPrinter ("");
				var ctx = new CompilerContext (compilerSettings.ToMono(), errorReportPrinter);
				ctx.Settings.TabSize = 1;
				var reader = new SeekableStreamReader (program);
				var file = new SourceFile (fileName, fileName, 0);
				Location.Initialize (new List<SourceFile> (new [] { file }));
				var module = new ModuleContainer (ctx);
				var session = new ParserSession ();
				session.LocationsBag = new LocationsBag ();
				var report = new Report (ctx, errorReportPrinter);
				var parser = Driver.Parse (reader, file, module, session, report, initialLine - 1, initialColumn - 1);
				var top = new CompilerCompilationUnit () {
					ModuleCompiled = module,
					LocationsBag = session.LocationsBag,
					SpecialsBag = parser.Lexer.sbag,
					Conditionals = parser.Lexer.SourceFile.Conditionals
				};
				var unit = Parse (top, fileName);
				unit.Errors.AddRange (errorReportPrinter.Errors);
				CompilerCallableEntryPoint.Reset ();
				return unit;
			}
		}

		public IEnumerable<EntityDeclaration> ParseTypeMembers (string code)
		{
			return ParseTypeMembers(code, initialLocation.Line, initialLocation.Column);
		}
		
		IEnumerable<EntityDeclaration> ParseTypeMembers (string code, int initialLine, int initialColumn)
		{
			const string prefix = "unsafe partial class MyClass { ";
			var syntaxTree = Parse (new StringTextSource (prefix + code + "}"), "parsed.cs", initialLine, initialColumn - prefix.Length);
			if (syntaxTree == null)
				return Enumerable.Empty<EntityDeclaration> ();
			var td = syntaxTree.FirstChild as TypeDeclaration;
			if (td != null) {
				var members = td.Members.ToArray();
				// detach members from parent
				foreach (var m in members)
					m.Remove();
				return members;
			}
			return Enumerable.Empty<EntityDeclaration> ();
		}
		
		public IEnumerable<Statement> ParseStatements (string code)
		{
			return ParseStatements(code, initialLocation.Line, initialLocation.Column);
		}
		
		IEnumerable<Statement> ParseStatements (string code, int initialLine, int initialColumn)
		{
			// the dummy method is async so that 'await' expressions are parsed as expected
			const string prefix = "async void M() { ";
			var members = ParseTypeMembers (prefix + code + "}", initialLine, initialColumn - prefix.Length);
			var method = members.FirstOrDefault () as MethodDeclaration;
			if (method != null && method.Body != null) {
				var statements = method.Body.Statements.ToArray();
				// detach statements from parent
				foreach (var st in statements)
					st.Remove();
				return statements;
			}
			return Enumerable.Empty<Statement> ();
		}
		
		public AstType ParseTypeReference (string code)
		{
			var members = ParseTypeMembers (code + " a;");
			var field = members.FirstOrDefault () as FieldDeclaration;
			if (field != null) {
				AstType type = field.ReturnType;
				type.Remove();
				return type;
			}
			return AstType.Null;
		}
		
		public Expression ParseExpression (string code)
		{
			const string prefix = "tmp = ";
			var statements = ParseStatements (prefix + code + ";", initialLocation.Line, initialLocation.Column - prefix.Length);
			var es = statements.FirstOrDefault () as ExpressionStatement;
			if (es != null) {
				AssignmentExpression ae = es.Expression as AssignmentExpression;
				if (ae != null) {
					Expression expr = ae.Right;
					expr.Remove();
					return expr;
				}
			}
			return Expression.Null;
		}
		
		/*
		/// <summary>
		/// Parses a file snippet; guessing what the code snippet represents (whole file, type members, block, type reference, expression).
		/// </summary>
		public AstNode ParseSnippet (string code)
		{
			// TODO: add support for parsing a part of a file
			throw new NotImplementedException ();
		}
		*/
		
		public DocumentationReference ParseDocumentationReference (string cref)
		{
			// see Mono.CSharp.DocumentationBuilder.HandleXrefCommon
			if (cref == null)
				throw new ArgumentNullException ("cref");
			
			// Additional symbols for < and > are allowed for easier XML typing
			cref = cref.Replace ('{', '<').Replace ('}', '>');
			
			lock (parseLock) {
				errorReportPrinter = new ErrorReportPrinter("");
				var ctx = new CompilerContext(compilerSettings.ToMono(), errorReportPrinter);
				ctx.Settings.TabSize = 1;
				var reader = new SeekableStreamReader(new StringTextSource (cref));
				var file = new SourceFile("", "", 0);
				Location.Initialize(new List<SourceFile> (new [] { file }));
				var module = new ModuleContainer(ctx);
				module.DocumentationBuilder = new DocumentationBuilder(module);
				var source_file = new CompilationSourceFile (module);
				var report = new Report (ctx, errorReportPrinter);
				ParserSession session = new ParserSession ();
				session.LocationsBag = new LocationsBag ();
				var parser = new Mono.CSharp.CSharpParser (reader, source_file, report, session);
				parser.Lexer.Line += initialLocation.Line - 1;
				parser.Lexer.Column += initialLocation.Column - 1;
				parser.Lexer.putback_char = Tokenizer.DocumentationXref;
				parser.Lexer.parsing_generic_declaration_doc = true;
				parser.parse ();
				if (report.Errors > 0) {
//					Report.Warning (1584, 1, mc.Location, "XML comment on `{0}' has syntactically incorrect cref attribute `{1}'",
//					                mc.GetSignatureForError (), cref);
				}
				
				ConversionVisitor conversionVisitor = new ConversionVisitor (false, session.LocationsBag);
				DocumentationReference docRef = conversionVisitor.ConvertXmlDoc(module.DocumentationBuilder);
				CompilerCallableEntryPoint.Reset();
				return docRef;
			}
		}
	}
}
