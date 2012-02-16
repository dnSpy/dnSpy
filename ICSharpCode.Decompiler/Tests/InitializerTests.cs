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

public class InitializerTests
{
	private enum MyEnum
	{
		a,
		b
	}

	private enum MyEnum2
	{
		c,
		d
	}

	private class Data
	{
		public List<InitializerTests.MyEnum2> FieldList = new List<InitializerTests.MyEnum2>();
		public InitializerTests.MyEnum a
		{
			get;
			set;
		}
		public List<InitializerTests.MyEnum2> PropertyList
		{
			get;
			set;
		}

		public InitializerTests.Data MoreData
		{
			get;
			set;
		}
		
		public InitializerTests.StructData NestedStruct
		{
			get;
			set;
		}
	}
	
	private struct StructData
	{
		public int Field;
		public int Property 
		{
			get;
			set; 
		}
		
		public InitializerTests.Data MoreData
		{
			get;
			set;
		}
		
		public StructData(int initialValue)
		{
			this = default(InitializerTests.StructData);
			this.Field = initialValue;
			this.Property = initialValue;
		}
	}
	
	// Helper methods used to ensure initializers used within expressions work correctly
	private static void X(object a, object b)
	{
	}

	private static object Y()
	{
		return null;
	}

	#region Array Initializers
	public static void Array1()
	{
		InitializerTests.X(InitializerTests.Y(), new int[]
				{
					1,
					2,
					3,
					4, 
					5,
					6,
					7,
					8,
					9,
					10
				});
	}

	public static void Array2(int a, int b, int c)
	{
		InitializerTests.X(InitializerTests.Y(), new int[]
				{
					a,
					0,
					b, 
					0, 
					c
				});
	}

	public static void NestedArray(int a, int b, int c)
	{
		InitializerTests.X(InitializerTests.Y(), new int[][]
		{
				new int[]
					{
						1, 
					2,
					3, 
					4,
					5, 
					6, 
					7, 
					8,
					9, 
					10
					},
				new int[]
					{
						a, 
					b,
					c
					},
				new int[]
					{
						1,
					2, 
					3,
					4, 
					5, 
					6
					}
		  });
	}

	public static void ArrayBoolean()
	{
		InitializerTests.X(InitializerTests.Y(), new bool[]
				{
					true,
					false, 
					true, 
					false, 
					false, 
					false, 
					true, 
					true
				});
	}

	public static void ArrayByte()
	{
		InitializerTests.X(InitializerTests.Y(), new byte[]
				{
					1,
					2,
					3, 
					4, 
					5, 
					6,
					7,
					8,
					254,
					255
				});
	}

	public static void ArraySByte()
	{
		InitializerTests.X(InitializerTests.Y(), new sbyte[]
				{
					-128,
					-127,
					0,
					1,
					2,
					3,
					4,
					127
				});
	}

	public static void ArrayShort()
	{
		InitializerTests.X(InitializerTests.Y(), new short[]
				{
					-32768,
					-1,
					0,
					1,
					32767
				});
	}

	public static void ArrayUShort()
	{
		InitializerTests.X(InitializerTests.Y(), new ushort[]
				{
					0,
					1,
					32767,
					32768, 
					65534,
					65535
				});
	}

	public static void ArrayInt()
	{
		InitializerTests.X(InitializerTests.Y(), new int[]
				{
					1,
					-2,
					2000000000,
					4, 
					5,
					-6,
					7,
					8,
					9, 
					10
				});
	}

	public static void ArrayUInt()
	{
		InitializerTests.X(InitializerTests.Y(), new uint[]
				{
					1u,
					2000000000u,
					3000000000u,
					4u,
					5u,
					6u,
					7u,
					8u,
					9u,
					10u
				});
	}

	public static void ArrayLong()
	{
		InitializerTests.X(InitializerTests.Y(), new long[]
				{
					-4999999999999999999L,
					-1L,
					0L,
					1L,
					4999999999999999999L
				});
	}

	public static void ArrayULong()
	{
		InitializerTests.X(InitializerTests.Y(), new ulong[]
				{
					1uL,
					2000000000uL,
					3000000000uL,
					4uL,
					5uL,
					6uL,
					7uL,
					8uL,
					4999999999999999999uL,
					9999999999999999999uL
				});
	}

	public static void ArrayFloat()
	{
		InitializerTests.X(InitializerTests.Y(), new float[]
				{
					-1.5f,
					0f,
					1.5f,
					float.NegativeInfinity,
					float.PositiveInfinity,
					float.NaN
				});
	}

	public static void ArrayDouble()
	{
		InitializerTests.X(InitializerTests.Y(), new double[]
				{
					-1.5,
					0.0,
					1.5,
					double.NegativeInfinity,
					double.PositiveInfinity,
					double.NaN
				});
	}

	public static void ArrayDecimal()
	{
		InitializerTests.X(InitializerTests.Y(), new decimal[]
				{
					-100m,
					0m,
					100m,
					-79228162514264337593543950335m, 
					79228162514264337593543950335m, 
					0.0000001m
				});
	}

	public static void ArrayString()
	{
		InitializerTests.X(InitializerTests.Y(), new string[]
				{
					"",
					null,
					"Hello",
					"World"
				});
	}

	public static void ArrayEnum()
	{
		InitializerTests.X(InitializerTests.Y(), new InitializerTests.MyEnum[]
				{
					InitializerTests.MyEnum.a,
					InitializerTests.MyEnum.b,
					InitializerTests.MyEnum.a,
					InitializerTests.MyEnum.b
				});
	}
	
	public static void RecursiveArrayInitializer()
	{
		int[] array = new int[3];
		array[0] = 1;
		array[1] = 2;
		array[2] = array[1] + 1;
		array[0] = 0;
	}
	#endregion

	public static void CollectionInitializerList()
	{
		InitializerTests.X(InitializerTests.Y(), new List<int>
				{
					1,
					2,
					3
				});
	}

	public static object RecursiveCollectionInitializer()
	{
		List<object> list = new List<object>();
		list.Add(list);
		return list;
	}

	public static void CollectionInitializerDictionary()
	{
		InitializerTests.X(InitializerTests.Y(), new Dictionary<string, int>
		{
			{
				"First",
				1
			},
			{
				"Second",
				2
			},
			{
				"Third",
				3
			}
		  });
	}

	public static void CollectionInitializerDictionaryWithEnumTypes()
	{
		InitializerTests.X(InitializerTests.Y(), new Dictionary<InitializerTests.MyEnum, InitializerTests.MyEnum2>
		{
			{
				InitializerTests.MyEnum.a,
				InitializerTests.MyEnum2.c
			},
			{
				InitializerTests.MyEnum.b,
				InitializerTests.MyEnum2.d
			}
		  });
	}

	public static void NotACollectionInitializer()
	{
		List<int> list = new List<int>();
		list.Add(1);
		list.Add(2);
		list.Add(3);
		InitializerTests.X(InitializerTests.Y(), list);
	}

	public static void ObjectInitializer()
	{
		InitializerTests.X(InitializerTests.Y(), new InitializerTests.Data
		  {
			  a = InitializerTests.MyEnum.a
		  });
	}

	public static void NotAObjectInitializer()
	{
		InitializerTests.Data data = new InitializerTests.Data();
		data.a = InitializerTests.MyEnum.a;
		InitializerTests.X(InitializerTests.Y(), data);
	}

	public static void ObjectInitializerAssignCollectionToField()
	{
		InitializerTests.X(InitializerTests.Y(), new InitializerTests.Data
		  {
			  a = InitializerTests.MyEnum.a,
			  FieldList = new List<InitializerTests.MyEnum2>
				{
					InitializerTests.MyEnum2.c,
					InitializerTests.MyEnum2.d
				}
		  });
	}

	public static void ObjectInitializerAddToCollectionInField()
	{
		InitializerTests.X(InitializerTests.Y(), new InitializerTests.Data
		  {
			  a = InitializerTests.MyEnum.a,
			  FieldList =
				{
					InitializerTests.MyEnum2.c,
					InitializerTests.MyEnum2.d
				}
		  });
	}

	public static void ObjectInitializerAssignCollectionToProperty()
	{
		InitializerTests.X(InitializerTests.Y(), new InitializerTests.Data
		  {
			  a = InitializerTests.MyEnum.a,
			  PropertyList = new List<InitializerTests.MyEnum2>
				{
					InitializerTests.MyEnum2.c,
					InitializerTests.MyEnum2.d
				}
		  });
	}

	public static void ObjectInitializerAddToCollectionInProperty()
	{
		InitializerTests.X(InitializerTests.Y(), new InitializerTests.Data
		  {
			  a = InitializerTests.MyEnum.a,
			  PropertyList =
				{
					InitializerTests.MyEnum2.c,
					InitializerTests.MyEnum2.d
				}
		  });
	}

	public static void ObjectInitializerWithInitializationOfNestedObjects()
	{
		InitializerTests.X(InitializerTests.Y(), new InitializerTests.Data
		  {
			  MoreData =
			  {
				  a = InitializerTests.MyEnum.a
			  }
		  });
	}
	
	public static void StructInitializer_DefaultConstructor()
	{
		InitializerTests.X(InitializerTests.Y(), new InitializerTests.StructData 
			{
				Field = 1,
				Property = 2
			});
	}
	
	public static void StructInitializer_ExplicitConstructor()
	{
		InitializerTests.X(InitializerTests.Y(), new InitializerTests.StructData(0)
			{
				Field = 1,
				Property = 2
			});
	}
	
	public static void StructInitializerWithInitializationOfNestedObjects()
	{
		InitializerTests.X(InitializerTests.Y(), new InitializerTests.StructData
		  {
			MoreData =
			{
				a = InitializerTests.MyEnum.a,
				FieldList =
				{
					InitializerTests.MyEnum2.c, 
					InitializerTests.MyEnum2.d 
				}
			}
		  });
	}
	
	public static void StructInitializerWithinObjectInitializer()
	{
		InitializerTests.X(InitializerTests.Y(), new InitializerTests.Data
			{
				NestedStruct = new InitializerTests.StructData(2)
				{
					Field = 1,
					Property = 2
				}
			});
	}
	
	public int[,] MultidimensionalInit()
	{
		return new int[,]
		{

	        {
	            0,
	            0,
	            0,
	            0
	        },

	        {
	            1,
	            1,
	            1,
	            1
	        },

	        {
	            0,
	            0,
	            0,
	            0
	        },

	        {
	            0,
	            0,
	            0,
	            0
	        },

	        {
	            0,
	            0,
	            1,
	            0
	        },

	        {
	            0,
	            0,
	            1,
	            0
	        },

	        {
	            0,
	            0,
	            1,
	            0
	        },

	        {
	            0,
	            0,
	            1,
	            0
	        },

	        {
	            0,
	            0,
	            0,
	            0
	        },

	        {
	            1,
	            1,
	            1,
	            1
	        },

	        {
	            0,
	            0,
	            0,
	            0
	        },

	        {
	            0,
	            0,
	            0,
	            0
	        },

	        {
	            0,
	            0,
	            1,
	            0
	        },

	        {
	            0,
	            0,
	            1,
	            0
	        },

	        {
	            0,
	            0,
	            1,
	            0
	        },

	        {
	            0,
	            0,
	            1,
	            0
			}
	    };
	}

	public int[][,] MultidimensionalInit2()
	{
		return new int[][,]
		{
			new int[,]
	            {

	                {
						0,
						0,
						0,
						0
					},

	                {
						1,
						1,
						1,
						1
					},

	                {
						0,
						0,
						0,
						0
					},

	                {
						0,
						0,
						0,
						0
					}

	            },
	        new int[,]
	            {

	                {
						0,
						0,
						1,
						0
					},

	                {
						0,
						0,
						1,
						0
					},

	                {
						0,
						0,
						1,
						0
					},

	                {
						0,
						0,
						1,
						0
					}

	            },
	        new int[,]
	            {

	                {
						0,
						0,
						0,
						0
					},

	                {
						1,
						1,
						1,
						1
					},

	                {
						0,
						0,
						0,
						0
					},

	                {
						0,
						0,
						0,
						0
					}
	            },
	        new int[,]
	            {

	                {
						0,
						0,
						1,
						0
					},

	                {
						0,
						0,
						1,
						0
					},

	                {
						0,
						0,
						1,
						0
					},

	                {
						0,
						0,
						1,
						0
					}

	            }
	    };
	}

	public int[][,,] ArrayOfArrayOfArrayInit()
	{
		return new int[][,,]
		{
			new int[,,]
			{
				{
					{
						1,
						2,
						3
					},
					{ 
						4,
						5,
						6
					},
					{
						7,
						8,
						9
					}
				},
				{
					{
						11,
						12,
						13
					},
					{
						14,
						15,
						16
					},
					{
						17,
						18,
						19
					}
				}
			},

			new int[,,]
			{
				{
					{
						21,
						22,
						23
					},
					{
						24,
						25,
						26
					},
					{
						27,
						28,
						29
					}
				},
				{
					{
						31,
						32,
						33
					},
					{
						34,
						35,
						36
					},
					{
						37,
						38,
						39
					}
				}
			}
	};
	}
}
