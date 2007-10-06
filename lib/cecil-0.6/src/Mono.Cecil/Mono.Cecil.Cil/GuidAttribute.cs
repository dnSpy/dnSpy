//
// GuidAttribute.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2006 Jb Evain
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

namespace Mono.Cecil.Cil {

	using System;
	using System.Reflection;

	[AttributeUsage (AttributeTargets.Field)]
	public class GuidAttribute : Attribute {

		private Guid m_guid;

		public Guid Guid {
			get { return m_guid; }
		}

		GuidAttribute ()
		{
			m_guid = new Guid ();
		}

		public GuidAttribute (
			uint a,
			ushort b,
			ushort c,
			byte d,
			byte e,
			byte f,
			byte g,
			byte h,
			byte i,
			byte j,
			byte k)
		{
			m_guid = new Guid ((int) a, (short) b, (short) c, d, e, f, g, h, i, j, k);
		}

		public static int GetValueFromGuid (Guid id, Type enumeration)
		{
			foreach (FieldInfo fi in enumeration.GetFields (BindingFlags.Static | BindingFlags.Public))
				if (id == GetGuidAttribute (fi).Guid)
					return (int) fi.GetValue (null);

			return -1;
		}

		public static Guid GetGuidFromValue (int value, Type enumeration)
		{
			foreach (FieldInfo fi in enumeration.GetFields (BindingFlags.Static | BindingFlags.Public))
				if (value == (int) fi.GetValue (null))
					return GetGuidAttribute (fi).Guid;

			return new Guid ();
		}

		static GuidAttribute GetGuidAttribute (FieldInfo fi)
		{
			GuidAttribute [] attributes = fi.GetCustomAttributes (typeof (GuidAttribute), false) as GuidAttribute [];
			if (attributes == null || attributes.Length != 1)
				return new GuidAttribute ();

			return attributes [0];
		}
	}
}
