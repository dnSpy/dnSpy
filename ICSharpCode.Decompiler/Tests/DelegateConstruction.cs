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

public static class DelegateConstruction
{
	class InstanceTests
	{
		public Action CaptureOfThis()
		{
			return delegate {
				CaptureOfThis();
			};
		}
		
		public Action CaptureOfThisAndParameter(int a)
		{
			return delegate {
				CaptureOfThisAndParameter(a);
			};
		}
		
		public Action CaptureOfThisAndParameterInForEach(int a)
		{
			foreach (int item in Enumerable.Empty<int>()) {
				if (item > 0) {
					return delegate {
						CaptureOfThisAndParameter(item + a);
					};
				}
			}
			return null;
		}
		
		public Action CaptureOfThisAndParameterInForEachWithItemCopy(int a)
		{
			foreach (int item in Enumerable.Empty<int>()) {
				int copyOfItem = item;
				if (item > 0) {
					return delegate {
						CaptureOfThisAndParameter(item + a + copyOfItem);
					};
				}
			}
			return null;
		}
		
		public void LambdaInForLoop()
		{
			for (int i = 0; i < 100000; i++) {
			    Bar(() => Foo());
			}
		}
		
		public int Foo()
		{
			return 0;
		}
		
		public void Bar(Func<int> f)
		{
		}
	}

	public static void Test(this string a)
	{
	}

	public static Action<string> ExtensionMethodUnbound()
	{
		return new Action<string>(DelegateConstruction.Test);
	}

	public static Action ExtensionMethodBound()
	{
		return new Action("abc".Test);
	}

	public static Action ExtensionMethodBoundOnNull()
	{
		return new Action(((string)null).Test);
	}

	public static object StaticMethod()
	{
		return new Func<Action>(DelegateConstruction.ExtensionMethodBound);
	}

	public static object InstanceMethod()
	{
		return new Func<string>("hello".ToUpper);
	}

	public static object InstanceMethodOnNull()
	{
		return new Func<string>(((string)null).ToUpper);
	}

	public static List<Action<int>> AnonymousMethodStoreWithinLoop()
	{
		List<Action<int>> list = new List<Action<int>>();
		for (int i = 0; i < 10; i++)
		{
			int counter;
			list.Add(delegate(int x)
			         {
			         	counter = x;
			         }
			        );
		}
		return list;
	}

	public static List<Action<int>> AnonymousMethodStoreOutsideLoop()
	{
		List<Action<int>> list = new List<Action<int>>();
		int counter;
		for (int i = 0; i < 10; i++)
		{
			list.Add(delegate(int x)
			         {
			         	counter = x;
			         }
			        );
		}
		return list;
	}

	public static Action StaticAnonymousMethodNoClosure()
	{
		return delegate
		{
			Console.WriteLine();
		};
	}

	public static void NameConflict()
	{
		// i is captured variable,
		// j is parameter in anonymous method
		// k is local in anonymous method,
		// l is local in main method
		// Ensure that the decompiler doesn't introduce name conflicts
		List<Action<int>> list = new List<Action<int>>();
		for (int l = 0; l < 10; l++) {
			int i;
			for (i = 0; i < 10; i++) {
				list.Add(
					delegate (int j) {
						for (int k = 0; k < i; k += j) {
							Console.WriteLine();
						}
					});
			}
		}
	}

	public static void NameConflict2(int j)
	{
		List<Action<int>> list = new List<Action<int>>();
		for (int k = 0; k < 10; k++) {
			list.Add(
				delegate(int i) {
					Console.WriteLine(i);
				});
		}
	}

	public static Action<int> NameConflict3(int i)
	{
		return delegate(int j) {
			for (int k = 0; k < j; k++) {
				Console.WriteLine(k);
			}
		};
	}
	
	public static Func<int, Func<int, int>> CurriedAddition(int a)
	{
		return b => c => a + b + c;
	}
	
	public static Func<int, Func<int, Func<int, int>>> CurriedAddition2(int a)
	{
		return b => c => d => a + b + c + d;
	}
}
