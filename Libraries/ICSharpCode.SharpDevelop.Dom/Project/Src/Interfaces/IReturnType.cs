// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.SharpDevelop.Dom
{
	/// <summary>
	/// Interface for reference to types (classes).
	/// Such a reference can be direct (DefaultReturnType), lazy (SearchClassReturnType) or
	/// returns types that stand for special references (e.g. ArrayReturnType)
	/// </summary>
	public interface IReturnType : IEquatable<IReturnType>
	{
		/// <summary>
		/// Gets the fully qualified name of the class the return type is pointing to.
		/// </summary>
		/// <returns>
		/// "System.Int32" for int[]<br/>
		/// "System.Collections.Generic.List" for List&lt;string&gt;
		/// </returns>
		string FullyQualifiedName {
			get;
		}
		
		/// <summary>
		/// Gets the short name of the class the return type is pointing to.
		/// </summary>
		/// <returns>
		/// "Int32" or "int" (depending how the return type was created) for int[]<br/>
		/// "List" for List&lt;string&gt;
		/// </returns>
		string Name {
			get;
		}
		
		/// <summary>
		/// Gets the namespace of the class the return type is pointing to.
		/// </summary>
		/// <returns>
		/// "System" for int[]<br/>
		/// "System.Collections.Generic" for List&lt;string&gt;
		/// </returns>
		string Namespace {
			get;
		}
		
		/// <summary>
		/// Gets the full dotnet name of the return type. The DotnetName is used for the
		/// documentation tags.
		/// </summary>
		/// <returns>
		/// "System.Int[]" for int[]<br/>
		/// "System.Collections.Generic.List{System.String}" for List&lt;string&gt;
		/// </returns>
		string DotNetName {
			get;
		}
		
		/// <summary>
		/// Gets the number of type parameters the target class should have
		/// / the number of type arguments specified by this type reference.
		/// </summary>
		int TypeArgumentCount {
			get;
		}
		
		/// <summary>
		/// Gets the underlying class of this return type. This method will return <c>null</c> for
		/// generic return types and types that cannot be resolved.
		/// </summary>
		IClass GetUnderlyingClass();
		
		/// <summary>
		/// Gets all methods that can be called on this return type.
		/// </summary>
		List<IMethod> GetMethods();
		
		/// <summary>
		/// Gets all properties that can be called on this return type.
		/// </summary>
		List<IProperty> GetProperties();
		
		/// <summary>
		/// Gets all fields that can be called on this return type.
		/// </summary>
		List<IField> GetFields();
		
		/// <summary>
		/// Gets all events that can be called on this return type.
		/// </summary>
		List<IEvent> GetEvents();
		
		
		/// <summary>
		/// Gets if the return type is a default type, i.e. no array, generic etc.
		/// </summary>
		/// <returns>
		/// True for SearchClassReturnType, GetClassReturnType and DefaultReturnType.<br/>
		/// False for ArrayReturnType, SpecificReturnType etc.
		/// </returns>
		bool IsDefaultReturnType { get; }
		
		/// <summary>
		/// Gets if the cast to the specified decorating return type would be valid.
		/// </summary>
		bool IsDecoratingReturnType<T>() where T : DecoratingReturnType;
		
		/// <summary>
		/// Casts this return type to the decorating return type specified as type parameter.
		/// This methods casts correctly even when the return type is wrapped by a ProxyReturnType.
		/// When the cast is invalid, <c>null</c> is returned.
		/// </summary>
		T CastToDecoratingReturnType<T>() where T : DecoratingReturnType;
		
		bool IsArrayReturnType { get; }
		ArrayReturnType CastToArrayReturnType();
		
		bool IsGenericReturnType { get; }
		GenericReturnType CastToGenericReturnType();
		
		bool IsConstructedReturnType { get; }
		ConstructedReturnType CastToConstructedReturnType();
		
		/// <summary>
		/// Gets whether the type is a reference type or value type.
		/// </summary>
		/// <returns>
		/// true, if the type is a reference type.
		/// false, if the type is a value type.
		/// null, if the type is not known (e.g. generic type argument or type not found)
		/// </returns>
		bool? IsReferenceType { get; }
		
		/// <summary>
		/// Gets an identical return type that binds directly to the underlying class, so
		/// that repeatedly calling methods does not cause repeated class lookups.
		/// The direct return type will always point to the old version of the class, so don't
		/// store direct return types!
		/// </summary>
		/// <returns>This method never returns null.</returns>
		IReturnType GetDirectReturnType();
	}
}
