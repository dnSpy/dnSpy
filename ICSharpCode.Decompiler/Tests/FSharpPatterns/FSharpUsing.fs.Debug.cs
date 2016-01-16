using System;
using System.IO;

public static class FSharpUsingPatterns
{
	public static void sample1()
	{
		using (FileStream fs = File.Create("x.txt"))
		{
			fs.WriteByte((byte)1);
		}
	}

	public static void sample2()
	{
		Console.WriteLine("some text");
		using (FileStream fs = File.Create("x.txt"))
		{
			fs.WriteByte((byte)2);
			Console.WriteLine("some text");
		}
	}

	public static void sample3()
	{
		Console.WriteLine("some text");
		using (FileStream fs = File.Create("x.txt"))
		{
			fs.WriteByte((byte)3);
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
		int num;
		using (FileStream fs = File.OpenRead("x.txt"))
		{
			num = fs.ReadByte();
		}
		int firstByte = num;
		int num3;
		using (FileStream fs = File.OpenRead("x.txt"))
		{
			int num2 = fs.ReadByte();
			num3 = fs.ReadByte();
		}
		int secondByte = num3;
		Console.WriteLine("read: {0}, {1}", firstByte, secondByte);
	}
}
