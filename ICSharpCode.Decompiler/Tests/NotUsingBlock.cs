using System;
using System.IO;

namespace ICSharpCode.Decompiler.Tests
{
	public class NotUsingBlock
	{
		public void ThisIsNotAUsingBlock()
		{
			object obj = File.OpenRead("...");
			IDisposable disposable;
			try
			{
				(obj as FileStream).WriteByte(2);
				Console.WriteLine("some text");
			}
			finally
			{
				disposable = (obj as IDisposable);
				if (disposable != null)
				{
					disposable.Dispose();
				}
			}
			Console.WriteLine(disposable);
		}
	}
}
