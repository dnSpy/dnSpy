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
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.Utils;

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
		/// 
		/// The output is ordered so that base types occur in before derived types.
		/// </remarks>
		public static IEnumerable<IType> GetAllBaseTypes(this IType type, ITypeResolveContext context)
		{
			BaseTypeCollector collector = new BaseTypeCollector(context);
			collector.CollectBaseTypes(type);
			return collector;
		}
		
		/// <summary>
		/// Gets the non-interface base types.
		/// </summary>
		/// <remarks>
		/// When <paramref name="type"/> is an interface, this method will also return base interfaces.
		/// 
		/// The output is ordered so that base types occur in before derived types.
		/// </remarks>
		public static IEnumerable<IType> GetNonInterfaceBaseTypes(this IType type, ITypeResolveContext context)
		{
			BaseTypeCollector collector = new BaseTypeCollector(context);
			collector.SkipImplementedInterfaces = true;
			collector.CollectBaseTypes(type);
			return collector;
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
		/// <example>
		/// <code>
		/// class X&lt;T&gt; {
		///   List&lt;T&gt; open;
		///   X&lt;X&lt;T[]&gt;&gt; open;
		///   X&lt;string&gt; closed;
		///   int closed;
		/// }
		/// </code>
		/// </example>
		public static bool IsOpen(this IType type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			TypeClassificationVisitor v = new TypeClassificationVisitor();
			type.AcceptVisitor(v);
			return v.isOpen;
		}
		
		/// <summary>
		/// Gets whether the type is unbound (is a generic type, but no type arguments were provided).
		/// </summary>
		/// <remarks>
		/// In "<c>typeof(List&lt;Dictionary&lt;,&gt;&gt;)</c>", only the Dictionary is unbound, the List is considered
		/// bound despite containing an unbound type.
		/// This method returns false for partially parameterized types (<c>Dictionary&lt;string, &gt;</c>).
		/// </remarks>
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
		[Obsolete("Use type.Kind == TypeKind.Enum instead")]
		public static bool IsEnum(this IType type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			return type.Kind == TypeKind.Enum;
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
			if (def != null && def.Kind == TypeKind.Enum) {
				if (def.BaseTypes.Count == 1)
					return def.BaseTypes[0].Resolve(context);
				else
					return KnownTypeReference.Int32.Resolve(context);
			} else {
				return SharedTypes.UnknownType;
			}
		}
		
		/// <summary>
		/// Gets whether the type is an delegate type.
		/// </summary>
		/// <remarks>This method returns <c>false</c> for System.Delegate itself</remarks>
		[Obsolete("Use type.Kind == TypeKind.Delegate instead")]
		public static bool IsDelegate(this IType type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			return type.Kind == TypeKind.Delegate;
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
			if (def != null && def.Kind == TypeKind.Delegate) {
				foreach (IMethod method in def.Methods) {
					if (method.Name == "Invoke") {
						ParameterizedType pt = type as ParameterizedType;
						if (pt != null) {
							return new SpecializedMethod(pt, method);
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
		public static ITypeDefinition GetInnermostTypeDefinition (this IParsedFile file, int line, int column)
		{
			return file.GetInnermostTypeDefinition (new TextLocation (line, column));
		}
		
		/// <summary>
		/// Gets the member defined at the specified location.
		/// Returns null if no member is defined at that location.
		/// </summary>
		public static IMember GetMember (this IParsedFile file, int line, int column)
		{
			return file.GetMember (new TextLocation (line, column));
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
