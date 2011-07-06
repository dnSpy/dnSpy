// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
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
		
		protected static IList<T> CopyList<T>(IList<T> inputList)
		{
			if (inputList == null || inputList.Count == 0)
				return null;
			else
				return new List<T>(inputList);
		}
		
		protected static ReadOnlyCollection<T> FreezeList<T>(IList<T> list) where T : IFreezable
		{
			if (list == null || list.Count == 0)
				return EmptyList<T>.Instance;
			var result = new ReadOnlyCollection<T>(list.ToArray());
			foreach (T item in result) {
				item.Freeze();
			}
			return result;
		}
		
		protected static ReadOnlyCollection<string> FreezeList(IList<string> list)
		{
			if (list == null || list.Count == 0)
				return EmptyList<string>.Instance;
			else
				return new ReadOnlyCollection<string>(list.ToArray());
		}
		
		protected static ReadOnlyCollection<ITypeReference> FreezeList(IList<ITypeReference> list)
		{
			if (list == null || list.Count == 0)
				return EmptyList<ITypeReference>.Instance;
			else
				return new ReadOnlyCollection<ITypeReference>(list.ToArray());
		}
	}
}
