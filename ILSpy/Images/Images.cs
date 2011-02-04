// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Windows.Media.Imaging;

namespace ICSharpCode.ILSpy
{
	static class Images
	{
		static BitmapImage LoadBitmap(string name)
		{
			BitmapImage image = new BitmapImage(new Uri("pack://application:,,,/Images/" + name + ".png"));
			image.Freeze();
			return image;
		}
		
		public static readonly BitmapImage Assembly = LoadBitmap("Assembly");
		public static readonly BitmapImage Namespace = LoadBitmap("NameSpace");
		
		public static readonly BitmapImage ReferenceFolderOpen = LoadBitmap("ReferenceFolder.Open");
		public static readonly BitmapImage ReferenceFolderClosed = LoadBitmap("ReferenceFolder.Closed");
		
		public static readonly BitmapImage Redo = LoadBitmap("Redo");
		public static readonly BitmapImage Undo = LoadBitmap("Undo");
		
		
		public static readonly BitmapImage Class = LoadBitmap("Class");
		public static readonly BitmapImage Delegate = LoadBitmap("Delegate");
		public static readonly BitmapImage Enum = LoadBitmap("Enum");
		public static readonly BitmapImage Interface = LoadBitmap("Interface");
		public static readonly BitmapImage Struct = LoadBitmap("Struct");
		public static readonly BitmapImage Field = LoadBitmap("Field");
		public static readonly BitmapImage Method = LoadBitmap("Method");
		public static readonly BitmapImage Literal = LoadBitmap("Literal");
		
		public static readonly BitmapImage InternalClass = LoadBitmap("InternalClass");
		public static readonly BitmapImage InternalDelegate = LoadBitmap("InternalDelegate");
		public static readonly BitmapImage InternalEnum = LoadBitmap("InternalEnum");
		public static readonly BitmapImage InternalInterface = LoadBitmap("InternalInterface");
		public static readonly BitmapImage InternalStruct = LoadBitmap("InternalStruct");
		public static readonly BitmapImage InternalField = LoadBitmap("InternalField");
		public static readonly BitmapImage InternalMethod = LoadBitmap("InternalMethod");
		
		public static readonly BitmapImage PrivateClass = LoadBitmap("PrivateClass");
		public static readonly BitmapImage PrivateDelegate = LoadBitmap("PrivateDelegate");
		public static readonly BitmapImage PrivateEnum = LoadBitmap("PrivateEnum");
		public static readonly BitmapImage PrivateInterface = LoadBitmap("PrivateInterface");
		public static readonly BitmapImage PrivateStruct = LoadBitmap("PrivateStruct");
		public static readonly BitmapImage PrivateField = LoadBitmap("PrivateField");
		public static readonly BitmapImage PrivateMethod = LoadBitmap("PrivateMethod");
		
		public static readonly BitmapImage ProtectedClass = LoadBitmap("ProtectedClass");
		public static readonly BitmapImage ProtectedDelegate = LoadBitmap("ProtectedDelegate");
		public static readonly BitmapImage ProtectedEnum = LoadBitmap("ProtectedEnum");
		public static readonly BitmapImage ProtectedInterface = LoadBitmap("ProtectedInterface");
		public static readonly BitmapImage ProtectedStruct = LoadBitmap("ProtectedStruct");
		public static readonly BitmapImage ProtectedField = LoadBitmap("ProtectedField");
		public static readonly BitmapImage ProtectedMethod = LoadBitmap("ProtectedMethod");
	}
}
