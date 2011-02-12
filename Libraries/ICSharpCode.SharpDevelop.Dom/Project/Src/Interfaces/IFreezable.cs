// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ICSharpCode.SharpDevelop.Dom
{
	public interface IFreezable
	{
		/// <summary>
		/// Gets if this instance is frozen. Frozen instances are immutable and thus thread-safe.
		/// </summary>
		bool IsFrozen { get; }
		
		/// <summary>
		/// Freezes this instance.
		/// </summary>
		void Freeze();
	}
	
	/// <summary>
	/// Base class for immutable objects. Provides implementation for IFreezable that reports the
	/// object as always-frozen.
	/// </summary>
	public abstract class Immutable : IFreezable
	{
		bool IFreezable.IsFrozen {
			get { return true; }
		}
		
		void IFreezable.Freeze()
		{
		}
	}
	
	public abstract class AbstractFreezable : IFreezable
	{
		bool isFrozen;
		
		/// <summary>
		/// Gets if this instance is frozen. Frozen instances are immutable and thus thread-safe.
		/// </summary>
		public bool IsFrozen {
			get { return isFrozen; }
		}
		
		/// <summary>
		/// Freezes this instance.
		/// </summary>
		public void Freeze()
		{
			if (!isFrozen) {
				FreezeInternal();
				isFrozen = true;
			}
		}
		
		protected virtual void FreezeInternal()
		{
		}
		
		protected void CheckBeforeMutation()
		{
			if (isFrozen)
				throw new InvalidOperationException("Cannot mutate frozen " + GetType().Name);
		}
		
		protected static IList<T> FreezeList<T>(IList<T> list) where T : IFreezable
		{
			if (list == null || list.Count == 0)
				return EmptyList<T>.Instance;
			list = new ReadOnlyCollection<T>(list.ToArray());
			foreach (T item in list) {
				item.Freeze();
			}
			return list;
		}
		
		protected static IList<IReturnType> FreezeList(IList<IReturnType> list)
		{
			if (list == null || list.Count == 0)
				return EmptyList<IReturnType>.Instance;
			else
				return new ReadOnlyCollection<IReturnType>(list.ToArray());
		}
		
		protected static IList<string> FreezeList(IList<string> list)
		{
			if (list == null || list.Count == 0)
				return EmptyList<string>.Instance;
			else
				return new ReadOnlyCollection<string>(list.ToArray());
		}
	}
	
	static class EmptyList<T>
	{
		public static readonly ReadOnlyCollection<T> Instance = new ReadOnlyCollection<T>(new T[0]);
	}
}
