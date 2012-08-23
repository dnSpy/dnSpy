// 
// ImplementInterfaceAction.cs
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
using ICSharpCode.NRefactory.TypeSystem;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction("Implement interface", Description = "Creates an interface implementation.")]
	public class ImplementInterfaceAction : ICodeActionProvider
	{
		public IEnumerable<CodeAction> GetActions(RefactoringContext context)
		{
			var type = context.GetNode<AstType>();
			if (type == null || type.Role != Roles.BaseType)
				yield break;
			
			var state = context.GetResolverStateBefore(type);
			if (state.CurrentTypeDefinition == null)
				yield break;
			
			var resolveResult = context.Resolve(type);
			if (resolveResult.Type.Kind != TypeKind.Interface)
				yield break;
			
			var toImplement = CollectMembersToImplement(state.CurrentTypeDefinition, resolveResult.Type, false);
			if (toImplement.Count == 0)
				yield break;
			
			yield return new CodeAction(context.TranslateString("Implement interface"), script => {
				script.InsertWithCursor(
					context.TranslateString("Implement Interface"),
					state.CurrentTypeDefinition,
					GenerateImplementation(context, toImplement)
				);
			});
		}
		
		public static IEnumerable<AstNode> GenerateImplementation(RefactoringContext context, IEnumerable<Tuple<IMember, bool>> toImplement)
		{
			var nodes = new Dictionary<IType, List<AstNode>>();
			
			foreach (var member in toImplement) {
				if (!nodes.ContainsKey(member.Item1.DeclaringType)) 
					nodes [member.Item1.DeclaringType] = new List<AstNode>();
				nodes [member.Item1.DeclaringType].Add(GenerateMemberImplementation(context, member.Item1, member.Item2));
			}
			
			foreach (var kv in nodes) {
				if (kv.Key.Kind == TypeKind.Interface) {
					yield return new PreProcessorDirective(
						PreProcessorDirectiveType.Region,
						string.Format("{0} implementation", kv.Key.Name));
				} else {
					yield return new PreProcessorDirective(
						PreProcessorDirectiveType.Region,
						string.Format("implemented abstract members of {0}", kv.Key.Name));
				}
				foreach (var member in kv.Value)
					yield return member;
				yield return new PreProcessorDirective(
					PreProcessorDirectiveType.Endregion
				);
			}
		}
		
		static EntityDeclaration GenerateMemberImplementation(RefactoringContext context, IMember member, bool explicitImplementation)
		{
			var builder = context.CreateTypeSytemAstBuilder();
			builder.GenerateBody = true;
			builder.ShowConstantValues = !explicitImplementation;
			builder.ShowTypeParameterConstraints = !explicitImplementation;
			builder.UseCustomEvents = explicitImplementation;
			var decl = builder.ConvertEntity(member);
			if (explicitImplementation) {
				decl.Modifiers = Modifiers.None;
				decl.AddChild(builder.ConvertType(member.DeclaringType), EntityDeclaration.PrivateImplementationTypeRole);
			} else {
				decl.Modifiers = Modifiers.Public;
			}
			return decl;
		}
		
		public static List<Tuple<IMember, bool>> CollectMembersToImplement(ITypeDefinition implementingType, IType interfaceType, bool explicitly)
		{
			var def = interfaceType.GetDefinition();
			List<Tuple<IMember, bool>> toImplement = new List<Tuple<IMember, bool>>();
			bool alreadyImplemented;
			
			// Stub out non-implemented events defined by @iface
			foreach (var evGroup in interfaceType.GetEvents (e => !e.IsSynthetic && e.DeclaringTypeDefinition.ReflectionName == def.ReflectionName).GroupBy (m => m.DeclaringType).Reverse ())
				foreach (var ev in evGroup) {
					bool needsExplicitly = explicitly;
					alreadyImplemented = implementingType.GetAllBaseTypeDefinitions().Any(
					x => x.Kind != TypeKind.Interface && x.Events.Any(y => y.Name == ev.Name)
					);
				
					if (!alreadyImplemented)
						toImplement.Add(new Tuple<IMember, bool>(ev, needsExplicitly));
				}
			
			// Stub out non-implemented methods defined by @iface
			foreach (var methodGroup in interfaceType.GetMethods (d => !d.IsSynthetic).GroupBy (m => m.DeclaringType).Reverse ())
				foreach (var method in methodGroup) {
				
					bool needsExplicitly = explicitly;
					alreadyImplemented = false;
				
					foreach (var cmet in implementingType.GetMethods ()) {
						if (CompareMethods(method, cmet)) {
							if (!needsExplicitly && !cmet.ReturnType.Equals(method.ReturnType))
								needsExplicitly = true;
							else
								alreadyImplemented |= !needsExplicitly /*|| cmet.InterfaceImplementations.Any (impl => impl.InterfaceType.Equals (interfaceType))*/;
						}
					}
					if (toImplement.Where(t => t.Item1 is IMethod).Any(t => CompareMethods(method, (IMethod)t.Item1)))
						needsExplicitly = true;
					if (!alreadyImplemented) 
						toImplement.Add(new Tuple<IMember, bool>(method, needsExplicitly));
				}
			
			// Stub out non-implemented properties defined by @iface
			foreach (var propGroup in interfaceType.GetProperties (p => !p.IsSynthetic && p.DeclaringTypeDefinition.ReflectionName == def.ReflectionName).GroupBy (m => m.DeclaringType).Reverse ())
				foreach (var prop in propGroup) {
					bool needsExplicitly = explicitly;
					alreadyImplemented = false;
					foreach (var t in implementingType.GetAllBaseTypeDefinitions ()) {
						if (t.Kind == TypeKind.Interface)
							continue;
						foreach (IProperty cprop in t.Properties) {
							if (cprop.Name == prop.Name) {
								if (!needsExplicitly && !cprop.ReturnType.Equals(prop.ReturnType))
									needsExplicitly = true;
								else
									alreadyImplemented |= !needsExplicitly/* || cprop.InterfaceImplementations.Any (impl => impl.InterfaceType.Resolve (ctx).Equals (interfaceType))*/;
							}
						}
					}
					if (!alreadyImplemented)
						toImplement.Add(new Tuple<IMember, bool>(prop, needsExplicitly));
				}
			return toImplement;
		}
		
		internal static bool CompareMethods(IMethod interfaceMethod, IMethod typeMethod)
		{
			if (typeMethod.IsExplicitInterfaceImplementation)
				return typeMethod.ImplementedInterfaceMembers.Any(m => m.Equals(interfaceMethod));
			return SignatureComparer.Ordinal.Equals(interfaceMethod, typeMethod);
		}
	}
}

