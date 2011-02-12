// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.SharpDevelop.Dom
{
	static class ExtensionMethods
	{
		public static void AddRange(this ArrayList arrayList, IEnumerable elements)
		{
			foreach (object o in elements)
				arrayList.Add(o);
		}
		
		public static void AddRange<T>(this ICollection<T> list, IEnumerable<T> elements)
		{
			foreach (T o in elements)
				list.Add(o);
		}
		
		/// <summary>
		/// Converts a recursive data structure into a flat list.
		/// </summary>
		/// <param name="input">The root elements of the recursive data structure.</param>
		/// <param name="recursive">The function that gets the children of an element.</param>
		/// <returns>Iterator that enumerates the tree structure in preorder.</returns>
		public static IEnumerable<T> Flatten<T>(this IEnumerable<T> input, Func<T, IEnumerable<T>> recursion)
		{
			Stack<IEnumerator<T>> stack = new Stack<IEnumerator<T>>();
			try {
				stack.Push(input.GetEnumerator());
				while (stack.Count > 0) {
					while (stack.Peek().MoveNext()) {
						T element = stack.Peek().Current;
						yield return element;
						IEnumerable<T> children = recursion(element);
						if (children != null) {
							stack.Push(children.GetEnumerator());
						}
					}
					stack.Pop().Dispose();
				}
			} finally {
				while (stack.Count > 0) {
					stack.Pop().Dispose();
				}
			}
		}
		
		public static IEnumerable<IUsing> GetAllUsings(this ICompilationUnit cu)
		{
			return (new[]{cu.UsingScope}).Flatten(s=>s.ChildScopes).SelectMany(s=>s.Usings);
		}
	}
	
	/// <summary>
	/// Publicly visible helper methods.
	/// </summary>
	public static class ExtensionMethodsPublic
	{
		// the difference between IClass and IReturnType is that IClass only contains the members
		// that are declared in this very class,
		// and IReturnType contains also members from base classes (including System.Object) and default (undeclared) constructors
		
		static SignatureComparer memberSignatureComparer = new SignatureComparer();
		
		public static bool HasMember(this IClass containingClass, IMember member)
		{
			return containingClass.AllMembers.Any(m => memberSignatureComparer.Equals(member, m));
		}
		
		public static bool HasMember(this IReturnType containingClass, IMember member)
		{
			return containingClass.GetMembers().Any(m => memberSignatureComparer.Equals(member, m));
		}
		
		public static bool ImplementsInterface(this IClass targetClass, IClass requiredInterface)
		{
			var targetClassType = targetClass.GetCompoundClass().DefaultReturnType;
			var requiredInterfaceType = requiredInterface.GetCompoundClass().DefaultReturnType;
			// class.DefaultReturnType.GetMethods() returns also methods from base classes, default ctor, ToString() etc. etc.
			return !requiredInterfaceType.GetMembers().Any(missingMember => !targetClassType.HasMember(missingMember));
		}
		
		public static bool ImplementsAbstractClass(this IClass targetClass, IClass abstractClass)
		{
			var requiredAbstractMembers = MemberLookupHelper.GetAccessibleMembers(abstractClass.DefaultReturnType, targetClass, LanguageProperties.CSharp, true).Where(m => m.IsAbstract);
			return !requiredAbstractMembers.Any(missingMember => !targetClass.HasMember(missingMember));
		}
		
		public static IEnumerable<IMember> GetMembers(this IReturnType typeReference)
		{
			var properties = typeReference.GetProperties().Cast<IMember>();
			var methods = typeReference.GetMethods().Cast<IMember>();
			var fields = typeReference.GetFields().Cast<IMember>();
			var events = typeReference.GetEvents().Cast<IMember>();
			return properties.Concat(methods).Concat(fields).Concat(events);
		}
	}
}
