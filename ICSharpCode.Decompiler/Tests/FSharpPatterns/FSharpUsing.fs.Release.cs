using System;
using System.IO;

public static class FSharpUsingPatterns
{
	public static void sample1()
	{
		using (FileStream fs = File.Create("x.txt"))
		{
			fs.WriteByte(1);
		}
	}

	public static void sample2()
	{
		Console.WriteLine("some text");
		using (FileStream fs = File.Create("x.txt"))
		{
			fs.WriteByte(2);
			Console.WriteLine("some text");
		}
	}

	public static void sample3()
	{
		Console.WriteLine("some text");
		using (FileStream fs = File.Create("x.txt"))
		{
			fs.WriteByte(3);
		}
		Console.WriteLine("some text");
	}

	public static void sample4()
	{
		Console.WriteLine("some text");
		int num;
		using (FileStream fs = File.OpenRead("x.txt"))
		{
			num = fs.ReadByte();
		}
		int firstByte = num;
		Console.WriteLine("read:" + firstByte.ToString());
	}

	public static void sample5()
	{
		Console.WriteLine("some text");
		int secondByte;
		using (FileStream fs = File.OpenRead("x.txt"))
		{
			secondByte = fs.ReadByte();
		}
		int firstByte = secondByte;
		int num2;
		using (FileStream fs = File.OpenRead("x.txt"))
		{
			int num = fs.ReadByte();
			num2 = fs.ReadByte();
		}
		secondByte = num2;
		Console.WriteLine("read: {0}, {1}", firstByte, secondByte);
	}
}
