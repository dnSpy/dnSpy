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
using System.Threading;

public class ExceptionHandling
{
	public void MethodEndingWithEndFinally()
	{
		try
		{
			throw null;
		}
		finally
		{
			Console.WriteLine();
		}
	}
	
	public void MethodEndingWithRethrow()
	{
		try
		{
			throw null;
		}
		catch
		{
			throw;
		}
	}
	
	public void TryCatchFinally()
	{
		try
		{
			Console.WriteLine("Try");
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
		}
		finally
		{
			Console.WriteLine("Finally");
		}
	}
	
	public void TryCatchMultipleHandlers()
	{
		try
		{
			Console.WriteLine("Try");
		}
		catch (InvalidOperationException ex)
		{
			Console.WriteLine(ex.Message);
		}
		catch (Exception ex2)
		{
			Console.WriteLine(ex2.Message);
		}
		catch
		{
			Console.WriteLine("other");
		}
	}
	
	public void NoUsingStatementBecauseTheVariableIsAssignedTo()
	{
		CancellationTokenSource cancellationTokenSource = null;
		try
		{
			cancellationTokenSource = new CancellationTokenSource();
		}
		finally
		{
			if (cancellationTokenSource != null)
			{
				cancellationTokenSource.Dispose();
			}
		}
	}
	
	public void UsingStatementThatChangesTheVariable()
	{
		CancellationTokenSource cancellationTokenSource = null;
		using (cancellationTokenSource)
		{
			cancellationTokenSource = new CancellationTokenSource();
		}
	}
	
	public void TwoCatchBlocksWithSameVariable()
	{
		try
		{
			Console.WriteLine("Try1");
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
		}
		try
		{
			Console.WriteLine("Try2");
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
		}
	}
}
