// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)
using System;
using System.Collections;
using Debugger.MetaData;
using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.ILSpy.Debugger.Services.Debugger
{
	/// <summary>
	/// Helper for obtaining information about DebugType.
	/// </summary>
	static class TypeResolverExtension
	{
		/// <summary>
		/// Gets generic interface this type implements.
		/// The generic argument of the interface does not have to be specified. 
		/// If you know the generic argument, use DebugType.GetInterface.
		/// </summary>
		/// <param name="fullNamePrefix">Eg. "System.Collections.Generic.IList"</param>
		public static DebugType GetGenericInterface(this DebugType type, string fullNamePrefix)
		{
			foreach(DebugType inter in type.GetInterfaces()) {
				if (inter.FullName.StartsWith(fullNamePrefix) && inter.GetGenericArguments().Length > 0) {
					return inter;
				}
			}
			// not found, search BaseType
			return type.BaseType == null ? null : (DebugType)type.BaseType.GetInterface(fullNamePrefix);
		}
		
		/// <summary>
		/// Resolves implementation of System.Collections.Generic.IList on this type.
		/// </summary>
		/// <param name="iListType">Result found implementation of System.Collections.Generic.IList.</param>
		/// <param name="itemType">The only generic argument of <paramref name="implementation"/></param>
		/// <returns>True if found, false otherwise.</returns>
		public static bool ResolveIListImplementation(this DebugType type, out DebugType iListType, out DebugType itemType)
		{
			return type.ResolveGenericInterfaceImplementation(
				"System.Collections.Generic.IList", out iListType, out itemType);
		}
		
		/// <summary>
		/// Resolves implementation of System.Collections.Generic.IEnumerable on this type.
		/// </summary>
		/// <param name="iEnumerableType">Result found implementation of System.Collections.Generic.IEnumerable.</param>
		/// <param name="itemType">The only generic argument of <paramref name="implementation"/></param>
		/// <returns>True if found, false otherwise.</returns>
		public static bool ResolveIEnumerableImplementation(this DebugType type, out DebugType iEnumerableType, out DebugType itemType)
		{
			return type.ResolveGenericInterfaceImplementation(
				"System.Collections.Generic.IEnumerable", out iEnumerableType, out itemType);
		}
		
		/// <summary>
		/// Resolves implementation of a single-generic-argument interface on this type.
		/// </summary>
		/// <param name="fullNamePrefix">Interface name to search for (eg. "System.Collections.Generic.IList")</param>
		/// <param name="implementation">Result found implementation.</param>
		/// <param name="itemType">The only generic argument of <paramref name="implementation"/></param>
		/// <returns>True if found, false otherwise.</returns>
		public static bool ResolveGenericInterfaceImplementation(this DebugType type, string fullNamePrefix, out DebugType implementation, out DebugType itemType)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			
			implementation = null;
			itemType = null;

			implementation = type.GetGenericInterface(fullNamePrefix);
			if (implementation != null) {
				if (implementation.GetGenericArguments().Length == 1) {
					itemType = (DebugType)implementation.GetGenericArguments()[0];
					return true;
				}
			}
			return false;
		}
	}
}
