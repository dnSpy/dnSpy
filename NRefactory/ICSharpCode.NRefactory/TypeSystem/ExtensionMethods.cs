// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.Utils;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.CSharp;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Contains extension methods for the type system.
	/// </summary>
	public static class ExtensionMethods
	{
		#region GetAllBaseTypes
		/// <summary>
		/// Gets all base types.
		/// </summary>
		/// <remarks>This is the reflexive and transitive closure of <see cref="IType.GetBaseTypes"/>.
		/// Note that this method does not return all supertypes - doing so is impossible due to contravariance
		/// (and undesirable for covariance as the list could become very large).
		/// </remarks>
		public static IEnumerable<IType> GetAllBaseTypes(this IType type, ITypeResolveContext context)
		{
			List<IType> output = new List<IType>();
			Stack<ITypeDefinition> activeTypeDefinitions = new Stack<ITypeDefinition>();
			CollectAllBaseTypes(type, context, activeTypeDefinitions, output);
			return output;
		}
		
		static void CollectAllBaseTypes(IType type, ITypeResolveContext context, Stack<ITypeDefinition> activeTypeDefinitions, List<IType> output)
		{
			ITypeDefinition def = type.GetDefinition();
			if (def != null) {
				// Maintain a stack of currently active type definitions, and avoid having one definition
				// multiple times on that stack.
				// This is necessary to ensure the output is finite in the presence of cyclic inheritance:
				// class C<X> : C<C<X>> {} would not be caught by the 'no duplicate output' check, yet would
				// produce infinite output.
				if (activeTypeDefinitions.Contains(def))
					return;
				activeTypeDefinitions.Push(def);
			}
			// Avoid outputting a type more than once - necessary for "diamond" multiple inheritance
			// (e.g. C implements I1 and I2, and both interfaces derive from Object)
			if (!output.Contains(type)) {
				output.Add(type);
				foreach (IType baseType in type.GetBaseTypes(context)) {
					CollectAllBaseTypes(baseType, context, activeTypeDefinitions, output);
				}
			}
			if (def != null)
				activeTypeDefinitions.Pop();
		}
		#endregion
		
		#region GetAllBaseTypeDefinitions
		/// <summary>
		/// Gets all base type definitions.
		/// </summary>
		/// <remarks>
		/// This is equivalent to type.GetAllBaseTypes().Select(t => t.GetDefinition()).Where(d => d != null).Distinct().
		/// </remarks>
		public static IEnumerable<ITypeDefinition> GetAllBaseTypeDefinitions(this IType type, ITypeResolveContext context)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			if (context == null)
				throw new ArgumentNullException("context");
			
			HashSet<ITypeDefinition> typeDefinitions = new HashSet<ITypeDefinition>();
			Func<ITypeDefinition, IEnumerable<ITypeDefinition>> recursion =
				t => t.GetBaseTypes(context).Select(b => b.GetDefinition()).Where(d => d != null && typeDefinitions.Add(d));
			
			ITypeDefinition typeDef = type.GetDefinition();
			if (typeDef != null) {
				typeDefinitions.Add(typeDef);
				return TreeTraversal.PreOrder(typeDef, recursion);
			} else {
				return TreeTraversal.PreOrder(
					type.GetBaseTypes(context).Select(b => b.GetDefinition()).Where(d => d != null && typeDefinitions.Add(d)),
					recursion);
			}
		}
		
		/// <summary>
		/// Gets whether this type definition is derived from the base type defintiion.
		/// </summary>
		public static bool IsDerivedFrom(this ITypeDefinition type, ITypeDefinition baseType, ITypeResolveContext context)
		{
			return GetAllBaseTypeDefinitions(type, context).Contains(baseType);
		}
		#endregion
		
		#region IsOpen / IsUnbound
		sealed class TypeClassificationVisitor : TypeVisitor
		{
			internal bool isOpen;
			
			public override IType VisitTypeParameter(ITypeParameter type)
			{
				isOpen = true;
				return base.VisitTypeParameter(type);
			}
		}
		
		/// <summary>
		/// Gets whether the type is an open type (contains type parameters).
		/// </summary>
		public static bool IsOpen(this IType type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			TypeClassificationVisitor v = new TypeClassificationVisitor();
			type.AcceptVisitor(v);
			return v.isOpen;
		}
		
		/// <summary>
		/// Gets whether the type is unbound.
		/// </summary>
		public static bool IsUnbound(this IType type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			return type is ITypeDefinition && type.TypeParameterCount > 0;
		}
		#endregion
		
		#region IsEnum / IsDelegate
		/// <summary>
		/// Gets whether the type is an enumeration type.
		/// </summary>
		public static bool IsEnum(this IType type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			ITypeDefinition def = type.GetDefinition();
			return def != null && def.ClassType == ClassType.Enum;
		}
		
		/// <summary>
		/// Gets the underlying type for this enum type.
		/// </summary>
		public static IType GetEnumUnderlyingType(this IType enumType, ITypeResolveContext context)
		{
			if (enumType == null)
				throw new ArgumentNullException("enumType");
			if (context == null)
				throw new ArgumentNullException("context");
			ITypeDefinition def = enumType.GetDefinition();
			if (def != null && def.ClassType == ClassType.Enum) {
				if (def.BaseTypes.Count == 1)
					return def.BaseTypes[0].Resolve(context);
				else
					return KnownTypeReference.Int32.Resolve(context);
			} else {
				throw new ArgumentException("enumType must be an enum");
			}
		}
		
		/// <summary>
		/// Gets whether the type is an delegate type.
		/// </summary>
		/// <remarks>This method returns <c>false</c> for System.Delegate itself</remarks>
		public static bool IsDelegate(this IType type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			ITypeDefinition def = type.GetDefinition();
			return def != null && def.ClassType == ClassType.Delegate;
		}
		
		/// <summary>
		/// Gets the invoke method for a delegate type.
		/// </summary>
		/// <remarks>
		/// Returns null if the type is not a delegate type; or if the invoke method could not be found.
		/// </remarks>
		public static IMethod GetDelegateInvokeMethod(this IType type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			ITypeDefinition def = type.GetDefinition();
			if (def != null && def.ClassType == ClassType.Delegate) {
				foreach (IMethod method in def.Methods) {
					if (method.Name == "Invoke") {
						ParameterizedType pt = type as ParameterizedType;
						if (pt != null) {
							SpecializedMethod m = new SpecializedMethod(method);
							m.SetDeclaringType(pt);
							var substitution = pt.GetSubstitution();
							m.SubstituteTypes(t => new SubstitutionTypeReference(t, substitution));
							return m;
						}
						return method;
					}
				}
			}
			return null;
		}
		#endregion
		
		#region InternalsVisibleTo
		/// <summary>
		/// Gets whether the internals of this project are visible to the other project
		/// </summary>
		public static bool InternalsVisibleTo(this IProjectContent projectContent, IProjectContent other, ITypeResolveContext context)
		{
			if (projectContent == other)
				return true;
			// TODO: implement support for [InternalsVisibleToAttribute]
			// Make sure implementation doesn't hurt performance, e.g. don't resolve all assembly attributes whenever
			// this method is called - it'll be called once per internal member during lookup operations
			return false;
		}
		#endregion
		
		#region GetAllTypes
		/// <summary>
		/// Gets all type definitions, including nested types.
		/// </summary>
		public static IEnumerable<ITypeDefinition> GetAllTypes(this ITypeResolveContext context)
		{
			return TreeTraversal.PreOrder(context.GetTypes(), t => t.NestedTypes);
		}
		#endregion
		
		#region GetType/Member
		/// <summary>
		/// Gets the type (potentially a nested type) defined at the specified location.
		/// Returns null if no type is defined at that location.
		/// </summary>
		public static ITypeDefinition GetTypeDefinition (this IParsedFile file, int line, int column)
		{
			return file.GetTypeDefinition (new AstLocation (line, column));
		}
		
		/// <summary>
		/// Gets the member defined at the specified location.
		/// Returns null if no member is defined at that location.
		/// </summary>
		public static IMember GetMember (this IParsedFile file, int line, int column)
		{
			return file.GetMember (new AstLocation (line, column));
		}
		#endregion
		
		#region GetSubTypeDefinitions
		/// <summary>
		/// Gets all sub type definitions defined in a context.
		/// </summary>
		public static IEnumerable<ITypeDefinition> GetSubTypeDefinitions (this ITypeDefinition baseType, ITypeResolveContext context)
		{
			foreach (var contextType in context.GetAllTypes ()) {
				if (contextType.IsDerivedFrom (baseType, context))
					yield return contextType;
			}
		}
		#endregion
	}
}
