// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.SharpDevelop.Dom
{
	/// <summary>
	/// Type parameter of a generic class/method.
	/// </summary>
	public interface ITypeParameter : IFreezable
	{
		/// <summary>
		/// The name of the type parameter (for example "T")
		/// </summary>
		string Name { get; }
		
		/// <summary>
		/// Gets the index of the type parameter in the type parameter list of the owning method/class.
		/// </summary>
		int Index { get; }
		
		IList<IAttribute> Attributes { get; }
		
		/// <summary>
		/// The method this type parameter is defined for.
		/// This property is null when the type parameter is for a class.
		/// </summary>
		IMethod Method { get; }
		
		/// <summary>
		/// The class this type parameter is defined for.
		/// When the type parameter is defined for a method, this is the class containing
		/// that method.
		/// </summary>
		IClass Class { get; }
		
		/// <summary>
		/// Gets the contraints of this type parameter.
		/// </summary>
		IList<IReturnType> Constraints { get; }
		
		/// <summary>
		/// Gets if the type parameter has the 'new()' constraint.
		/// </summary>
		bool HasConstructableConstraint { get; }
		
		/// <summary>
		/// Gets if the type parameter has the 'class' constraint.
		/// </summary>
		bool HasReferenceTypeConstraint { get; }
		
		/// <summary>
		/// Gets if the type parameter has the 'struct' constraint.
		/// </summary>
		bool HasValueTypeConstraint { get; }
		
		/// <summary>
		/// Gets the type that was used to bind this type parameter.
		/// This property returns null for generic methods/classes, it
		/// is non-null only for constructed versions of generic methods.
		/// </summary>
		IReturnType BoundTo { get; }
		
		/// <summary>
		/// If this type parameter was bound, returns the unbound version of it.
		/// </summary>
		ITypeParameter UnboundTypeParameter { get; }
	}
}
