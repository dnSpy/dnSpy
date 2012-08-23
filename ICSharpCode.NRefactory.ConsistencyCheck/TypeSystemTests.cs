// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.Documentation;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.ConsistencyCheck
{
	public class TypeSystemTests
	{
		public static void IDStringConsistencyCheck(Solution solution)
		{
			foreach (var project in solution.Projects) {
				var compilation = project.Compilation;
				HashSet<string> idStrings = new HashSet<string>();
				var context = compilation.TypeResolveContext;
				foreach (var typeDef in compilation.MainAssembly.GetAllTypeDefinitions()) {
					Check(typeDef, context, idStrings);
					foreach (var member in typeDef.Members) {
						Check(member, context, idStrings);
					}
				}
			}
		}
		
		static void Check(IEntity entity, ITypeResolveContext context, HashSet<string> idStrings)
		{
			string id = IdStringProvider.GetIdString(entity);
			if (!idStrings.Add(id))
				throw new InvalidOperationException("Duplicate ID string " + id);
			IEntity resolvedEntity = IdStringProvider.FindEntity(id, context);
			if (resolvedEntity != entity)
				throw new InvalidOperationException(id);
		}
		
		public static void ResolvedUnresolvedMembers(Solution solution)
		{
			foreach (var project in solution.Projects) {
				var compilation = project.Compilation;
				var assemblyContext = new SimpleTypeResolveContext(compilation.MainAssembly);
				foreach (var typeDef in compilation.MainAssembly.GetAllTypeDefinitions()) {
					foreach (var part in typeDef.Parts) {
						if (!typeDef.Equals(part.Resolve(assemblyContext)))
							throw new InvalidOperationException();
					}
					foreach (var member in IncludeAccessors(typeDef.Members)) {
						var resolvedMember = member.UnresolvedMember.Resolve(assemblyContext);
						if (!member.Equals(resolvedMember))
							throw new InvalidOperationException();
					}
					// ToMemberReference() requires an appropriate generic context if the member
					// contains open generics; otherwise the main context of the compilation is sufficient.
					ITypeResolveContext context;
					if (typeDef.TypeParameterCount > 0)
						context = new SimpleTypeResolveContext(typeDef);
					else
						context = compilation.TypeResolveContext;
					// Include (potentially specialized) inherited members when testing ToMemberReference()
					foreach (var member in IncludeAccessors(typeDef.GetMembers())) {
						var resolvedMember = member.ToMemberReference().Resolve(context);
						if (!member.Equals(resolvedMember))
							throw new InvalidOperationException();
					}
				}
			}
		}
		
		static IEnumerable<IMember> IncludeAccessors(IEnumerable<IMember> members)
		{
			foreach (var member in members) {
				yield return member;
				IProperty p = member as IProperty;
				if (p != null && p.CanGet)
					yield return p.Getter;
				if (p != null && p.CanSet)
					yield return p.Setter;
				IEvent e = member as IEvent;
				if (e != null && e.CanAdd)
					yield return e.AddAccessor;
				if (e != null && e.CanRemove)
					yield return e.RemoveAccessor;
				if (e != null && e.CanInvoke)
					yield return e.InvokeAccessor;
			}
		}
	}
}
