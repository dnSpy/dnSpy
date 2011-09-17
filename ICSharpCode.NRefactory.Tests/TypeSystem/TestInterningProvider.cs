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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.Utils;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/* Not a real unit test
	[TestFixture]
	public class TestInterningProvider
	{
		sealed class ReferenceComparer : IEqualityComparer<object>
		{
			public new bool Equals(object a, object b)
			{
				return ReferenceEquals(a, b);
			}
			
			public int GetHashCode(object obj)
			{
				return RuntimeHelpers.GetHashCode(obj);
			}
		}
		
		sealed class InterningComparer : IEqualityComparer<ISupportsInterning>
		{
			public bool Equals(ISupportsInterning x, ISupportsInterning y)
			{
				return x.EqualsForInterning(y);
			}
			
			public int GetHashCode(ISupportsInterning obj)
			{
				return obj.GetHashCodeForInterning();
			}
		}
		
		sealed class ListComparer : IEqualityComparer<IEnumerable<object>>
		{
			public bool Equals(IEnumerable<object> a, IEnumerable<object> b)
			{
				if (a.GetType() != b.GetType())
					return false;
				return Enumerable.SequenceEqual(a, b, new ReferenceComparer());
			}
			
			public int GetHashCode(IEnumerable<object> obj)
			{
				int hashCode = obj.GetType().GetHashCode();
				unchecked {
					foreach (object o in obj) {
						hashCode *= 27;
						hashCode += RuntimeHelpers.GetHashCode(o);
					}
				}
				return hashCode;
			}
		}
		
		sealed class InterningProvider : IInterningProvider
		{
			internal HashSet<object> uniqueObjectsPreIntern = new HashSet<object>(new ReferenceComparer());
			internal HashSet<object> uniqueObjectsPostIntern = new HashSet<object>(new ReferenceComparer());
			internal Dictionary<object, object> byValueDict = new Dictionary<object, object>();
			internal Dictionary<ISupportsInterning, ISupportsInterning> supportsInternDict = new Dictionary<ISupportsInterning, ISupportsInterning>(new InterningComparer());
			internal Dictionary<IEnumerable<object>, IEnumerable<object>> listDict = new Dictionary<IEnumerable<object>, IEnumerable<object>>(new ListComparer());
			
			public T Intern<T>(T obj) where T : class
			{
				if (obj == null)
					return null;
				uniqueObjectsPreIntern.Add(obj);
				ISupportsInterning s = obj as ISupportsInterning;
				if (s != null) {
					ISupportsInterning output;
					if (supportsInternDict.TryGetValue(s, out output)) {
						obj = (T)output;
					} else {
						s.PrepareForInterning(this);
						if (supportsInternDict.TryGetValue(s, out output))
							obj = (T)output;
						else
							supportsInternDict.Add(s, s);
					}
				} else if (obj is IType || Type.GetTypeCode(obj.GetType()) >= TypeCode.Boolean) {
					object output;
					if (byValueDict.TryGetValue(obj, out output))
						obj = (T)output;
					else
						byValueDict.Add(obj, obj);
				}
				uniqueObjectsPostIntern.Add(obj);
				return obj;
			}
			
			public IList<T> InternList<T>(IList<T> list) where T : class
			{
				if (list == null)
					return null;
				uniqueObjectsPreIntern.Add(list);
				for (int i = 0; i < list.Count; i++) {
					T oldItem = list[i];
					T newItem = Intern(oldItem);
					if (oldItem != newItem) {
						if (list.IsReadOnly)
							list = new T[list.Count];
						list[i] = newItem;
					}
				}
				if (!list.IsReadOnly)
					list = new ReadOnlyCollection<T>(list);
				IEnumerable<object> output;
				if (listDict.TryGetValue(list, out output))
					list = (IList<T>)output;
				else
					listDict.Add(list, list);
				uniqueObjectsPostIntern.Add(list);
				return list;
			}
			
			public void InternProject(IProjectContent pc)
			{
				foreach (var c in TreeTraversal.PreOrder(pc.GetClasses(), c => c.NestedTypes)) {
					Intern(c.Namespace);
					Intern(c.Name);
					foreach (IMember m in c.Members) {
						Intern(m);
					}
				}
			}
		}
		
		IProjectContent[] LoadProjects(CecilLoader loader)
		{
			const string dir = @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\";
			return new IProjectContent[] {
				loader.LoadAssemblyFile(dir + "mscorlib.dll"),
				loader.LoadAssemblyFile(dir + "System.dll"),
				loader.LoadAssemblyFile(dir + "System.Core.dll"),
				loader.LoadAssemblyFile(dir + "System.Xml.dll"),
				loader.LoadAssemblyFile(dir + "System.Xml.Linq.dll"),
				loader.LoadAssemblyFile(dir + "System.Data.dll"),
				loader.LoadAssemblyFile(dir + "System.Drawing.dll"),
				loader.LoadAssemblyFile(dir + "System.Windows.Forms.dll"),
				loader.LoadAssemblyFile(dir + "WindowsBase.dll"),
				loader.LoadAssemblyFile(dir + "PresentationCore.dll"),
				loader.LoadAssemblyFile(dir + "PresentationFramework.dll")
			};
		}
		
		[Test]
		public void PrintStatistics()
		{
			long startMemory = GC.GetTotalMemory(true);
			IProjectContent[] pc = LoadProjects(new CecilLoader());
			long memoryWithFullPC = GC.GetTotalMemory(true) - startMemory;
			InterningProvider p = new InterningProvider();
			CecilLoader loader = new CecilLoader();
			loader.InterningProvider = p;
			pc = LoadProjects(loader);
			PrintStatistics(p);
			loader = null;
			p = null;
			long memoryWithInternedPC = GC.GetTotalMemory(true) - startMemory;
			GC.KeepAlive(pc);
			Console.WriteLine(memoryWithInternedPC / 1024 + " KB / " + memoryWithFullPC / 1024 + " KB");
		}
		
		void PrintStatistics(InterningProvider p)
		{
			var stats =
				from obj in p.uniqueObjectsPreIntern
				group 1 by obj.GetType() into g
				join g2 in (from obj in p.uniqueObjectsPostIntern group 1 by obj.GetType()) on g.Key equals g2.Key
				orderby g.Key.FullName
				select new { Type = g.Key, PreCount = g.Count(), PostCount = g2.Count() };
			foreach (var element in stats) {
				Console.WriteLine(element.Type + ": " + element.PostCount + "/" + element.PreCount);
			}
		}
	}//*/
}
