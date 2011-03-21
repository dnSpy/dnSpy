// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

public static class MultidimensionalArray
{
	internal class Generic<T, S> where T : new()
	{
		private T[,] a = new T[20, 20];
		private S[,][] b = new S[20, 20][];

		public T this[int i, int j]
		{
			get
			{
				return this.a[i, j];
			}
			set
			{
				this.a[i, j] = value;
			}
		}
		
		public void TestB(S x, ref S y)
		{
			this.b[5, 3] = new S[10];
			this.b[5, 3][0] = default(S);
			this.b[5, 3][1] = x;
			this.b[5, 3][2] = y;
		}
	}
	
	public static int[][,] MakeArray()
	{
		return new int[10][,];
	}
}
