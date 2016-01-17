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

#pragma warning disable 1998
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public class Async
{
	public async void SimpleVoidMethod()
	{
		Console.WriteLine("Before");
		await Task.Delay(TimeSpan.FromSeconds(1.0));
		Console.WriteLine("After");
	}
	
	public async void VoidMethodWithoutAwait()
	{
		Console.WriteLine("No Await");
	}
	
	public async void EmptyVoidMethod()
	{
	}
	
	public async void AwaitYield()
	{
		await Task.Yield();
	}
	
	public async void AwaitDefaultYieldAwaitable()
	{
		await default(YieldAwaitable);
	}
	
	public async Task SimpleVoidTaskMethod()
	{
		Console.WriteLine("Before");
		await Task.Delay(TimeSpan.FromSeconds(1.0));
		Console.WriteLine("After");
	}
	
	public async Task TaskMethodWithoutAwait()
	{
		Console.WriteLine("No Await");
	}
	
	public async Task<bool> SimpleBoolTaskMethod()
	{
		Console.WriteLine("Before");
		await Task.Delay(TimeSpan.FromSeconds(1.0));
		Console.WriteLine("After");
		return true;
	}
	
	public async void TwoAwaitsWithDifferentAwaiterTypes()
	{
		Console.WriteLine("Before");
		if (await this.SimpleBoolTaskMethod()) 
		{
			await Task.Delay(TimeSpan.FromSeconds(1.0));
		}
		Console.WriteLine("After");
	}
	
	public async void StreamCopyTo(Stream destination, int bufferSize)
	{
		byte[] array = new byte[bufferSize];
		int count;
		while ((count = await destination.ReadAsync(array, 0, array.Length)) != 0)
		{
			await destination.WriteAsync(array, 0, count);
		}
	}
	
	public async void StreamCopyToWithConfigureAwait(Stream destination, int bufferSize)
	{
		byte[] array = new byte[bufferSize];
		int count;
		while ((count = await destination.ReadAsync(array, 0, array.Length).ConfigureAwait(false)) != 0)
		{
			await destination.WriteAsync(array, 0, count).ConfigureAwait(false);
		}
	}
	
	public async void AwaitInLoopCondition()
	{
		while (await this.SimpleBoolTaskMethod()) 
		{
			Console.WriteLine("Body");
		}
	}
	
	public async Task<int> AwaitInForEach(IEnumerable<Task<int>> elements)
	{
		int num = 0;
		foreach (Task<int> current in elements)
		{
			num += await current;
		}
		return num;
	}
	
	public async Task TaskMethodWithoutAwaitButWithExceptionHandling()
	{
		try 
		{
			using (new StringWriter())
			{
				Console.WriteLine("No Await");
			}
		} 
		catch (Exception) 
		{
			Console.WriteLine("Crash");
		}
	}
	
	public async Task<int> NestedAwait(Task<Task<int>> task)
	{
		return await(await task);
	}
	
	public async Task AwaitWithStack(Task<int> task)
	{
		Console.WriteLine("A", 1, await task);
	}
	
	public async Task AwaitWithStack2(Task<int> task)
	{
		if (await this.SimpleBoolTaskMethod()) 
		{
			Console.WriteLine("A", 1, await task);
		} 
		else 
		{
			int num = 1;
			Console.WriteLine("A", 1, num);
		}
	}
}
