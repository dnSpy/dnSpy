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
using System.Collections.ObjectModel;
using System.Linq;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	public static class FreezableHelper
	{
		public static void ThrowIfFrozen(IFreezable freezable)
		{
			if (freezable.IsFrozen)
				throw new InvalidOperationException("Cannot mutate frozen " + freezable.GetType().Name);
		}
		
		public static IList<T> FreezeListAndElements<T>(IList<T> list)
		{
			if (list != null) {
				foreach (T item in list)
					Freeze(item);
			}
			return FreezeList(list);
		}
		
		public static IList<T> FreezeList<T>(IList<T> list)
		{
			if (list == null || list.Count == 0)
				return EmptyList<T>.Instance;
			if (list.IsReadOnly) {
				// If the list is already read-only, return it directly.
				// This is important, otherwise we might undo the effects of interning.
				return list;
			} else {
				return new ReadOnlyCollection<T>(list.ToArray());
			}
		}
		
		public static void Freeze(object item)
		{
			IFreezable f = item as IFreezable;
			if (f != null)
				f.Freeze();
		}
	}

	[Serializable]
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
		
		/*
		protected static IList<T> CopyList<T>(IList<T> inputList)
		{
			if (inputList == null || inputList.Count == 0)
				return null;
			else
				return new List<T>(inputList);
		}*/
	}
}
