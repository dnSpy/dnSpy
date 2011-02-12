// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.SharpDevelop.Dom
{
	public interface IEntity : ICompletionEntry, IFreezable, IComparable
	{
		string FullyQualifiedName {
			get;
		}
		
		string Namespace {
			get;
		}
		
		/// <summary>
		/// The fully qualified name in the internal .NET notation (with `1 for generic types)
		/// </summary>
		string DotNetName {
			get;
		}
		
		DomRegion BodyRegion {
			get;
		}
		
		/// <summary>
		/// Gets the declaring type.
		/// For members, this is the type that contains the member.
		/// For classes, this is the outer class (for nested classes), or null if there this
		/// is a top-level class.
		/// </summary>
		IClass DeclaringType {
			get;
		}
		
		ModifierEnum Modifiers {
			get;
		}
		
		IList<IAttribute> Attributes {
			get;
		}
		
		string Documentation {
			get;
		}

		/// <summary>
		/// Returns true if this entity has the 'abstract' modifier set. 
		/// (Returns false for interface members).
		/// </summary>
		bool IsAbstract {
			get;
		}

		bool IsSealed {
			get;
		}

		/// <summary>
		/// Gets whether this entity is static.
		/// Returns true if either the 'static' or the 'const' modifier is set.
		/// </summary>
		bool IsStatic {
			get;
		}
		
		/// <summary>
		/// Gets whether this entity is a constant (C#-like const).
		/// </summary>
		bool IsConst {
			get;
		}

		/// <summary>
		/// Gets if the member is virtual. Is true only if the "virtual" modifier was used, but non-virtual
		/// members can be overridden, too; if they are already overriding a method.
		/// </summary>
		bool IsVirtual {
			get;
		}

		bool IsPublic {
			get;
		}

		bool IsProtected {
			get;
		}

		bool IsPrivate {
			get;
		}

		bool IsInternal {
			get;
		}

		bool IsReadonly {
			get;
		}

		[Obsolete("This property does not do what one would expect - it merely checks if protected+internal are set, it is not the equivalent of AssemblyAndFamily in Reflection!")]
		bool IsProtectedAndInternal {
			get;
		}

		[Obsolete("This property does not do what one would expect - it merely checks if one of protected+internal is set, it is not the equivalent of AssemblyOrFamily in Reflection!")]
		bool IsProtectedOrInternal {
			get;
		}

		bool IsOverride {
			get;
		}
		/// <summary>
		/// Gets if the member can be overridden. Returns true when the member is "virtual" or "override" but not "sealed".
		/// </summary>
		bool IsOverridable {
			get;
		}
		
		bool IsNew {
			get;
		}
		bool IsSynthetic {
			get;
		}
		
		/// <summary>
		/// Gets the compilation unit that contains this entity.
		/// </summary>
		ICompilationUnit CompilationUnit {
			get;
		}
		
		/// <summary>
		/// The project content in which this entity is defined.
		/// </summary>
		IProjectContent ProjectContent {
			get;
		}
		
		/// <summary>
		/// This property can be used to attach any user-defined data to this class/method.
		/// This property is mutable, it can be changed when the class/method is frozen.
		/// </summary>
		object UserData {
			get;
			set;
		}
		
		EntityType EntityType {
			get;
		}
		
		bool IsAccessible(IClass callingClass, bool isAccessThoughReferenceOfCurrentClass);
	}
	
	public enum EntityType {
		Class,
		Field,
		Property,
		Method,
		Event
	}
}
