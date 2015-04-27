// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
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

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Provider used for interning.
	/// </summary>
	/// <remarks>
	/// A simple IInterningProvider implementation could use 3 dictionaries:
	///  1. using value equality comparer (for certain types known to implement value equality, e.g. string and IType)
	///  2. using comparer that calls into ISupportsInterning (for types implementing ISupportsInterning)
	///  3. list comparer (for InternList method)
	/// 
	/// On the first Intern()-call, the provider tells the object to prepare for interning (ISupportsInterning.PrepareForInterning)
	/// and stores it into a dictionary. On further Intern() calls, the original object is returned for all equal objects.
	/// This allows reducing the memory usage by using a single object instance where possible.
	/// 
	/// Interning provider implementations could also use the interning logic for different purposes:
	/// for example, it could be used to determine which objects are used jointly between multiple type definitions
	/// and which are used only within a single type definition. Then a persistent file format could be organized so
	/// that shared objects are loaded only once, yet non-shared objects get loaded lazily together with the class.
	/// </remarks>
	public abstract class InterningProvider
	{
		public static readonly InterningProvider Dummy = new DummyInterningProvider();
		
		/// <summary>
		/// Interns the specified object.
		/// 
		/// If the object is freezable, it will be frozen.
		/// </summary>
		public abstract ISupportsInterning Intern(ISupportsInterning obj);
		
		/// <summary>
		/// Interns the specified object.
		/// 
		/// If the object is freezable, it will be frozen.
		/// </summary>
		public T Intern<T>(T obj) where T : class, ISupportsInterning
		{
			ISupportsInterning input = obj;
			return (T)Intern(input);
		}
		
		/// <summary>
		/// Interns the specified string.
		/// </summary>
		public abstract string Intern(string text);
		
		/// <summary>
		/// Inters a boxed value type.
		/// </summary>
		public abstract object InternValue(object obj);
		
		/// <summary>
		/// Interns the given list. Uses reference equality to compare the list elements.
		/// </summary>
		public abstract IList<T> InternList<T>(IList<T> list) where T : class;
		
		sealed class DummyInterningProvider : InterningProvider
		{
			public override ISupportsInterning Intern(ISupportsInterning obj)
			{
				return obj;
			}
			
			public override string Intern(string text)
			{
				return text;
			}
			
			public override object InternValue(object obj)
			{
				return obj;
			}
			
			public override IList<T> InternList<T>(IList<T> list)
			{
				return list;
			}
		}
	}
}
