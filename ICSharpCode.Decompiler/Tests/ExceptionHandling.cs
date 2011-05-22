// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

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
}
