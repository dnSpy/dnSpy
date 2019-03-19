/*
	Copyright (c) 2015 Ki

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
*/

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace dnSpy.BamlDecompiler.Xaml {
	class XamlPathDeserializer {
		enum PathOpCodes {
			BeginFigure,
			LineTo,
			QuadraticBezierTo,
			BezierTo,
			PolyLineTo,
			PolyQuadraticBezierTo,
			PolyBezierTo,
			ArcTo,
			Closed,
			FillRule
		}

		readonly struct Point {
			public readonly double X;
			public readonly double Y;

			public Point(double x, double y) {
				X = x;
				Y = y;
			}

			public override string ToString() => string.Format(CultureInfo.InvariantCulture, "{0:R},{1:R}", X, Y);
		}

		static void UnpackBools(byte b, out bool b1, out bool b2, out bool b3, out bool b4) {
			b1 = (b & 0x10) != 0;
			b2 = (b & 0x20) != 0;
			b3 = (b & 0x40) != 0;
			b4 = (b & 0x80) != 0;
		}

		static Point ReadPoint(BinaryReader reader) => new Point(reader.ReadXamlDouble(), reader.ReadXamlDouble());

		static void ReadPointBoolBool(BinaryReader reader, byte b, out Point pt, out bool b1, out bool b2) {
			UnpackBools(b, out b1, out b2, out bool sx, out bool sy);

			pt = new Point(reader.ReadXamlDouble(sx), reader.ReadXamlDouble(sy));
		}

		static IList<Point> ReadPointsBoolBool(BinaryReader reader, byte b, out bool b1, out bool b2) {
			UnpackBools(b, out b1, out b2, out bool b3, out bool b4);

			var count = reader.ReadInt32();
			var pts = new List<Point>();
			for (int i = 0; i < count; i++)
				pts.Add(ReadPoint(reader));

			return pts;
		}

		public static string Deserialize(BinaryReader reader) {
			bool end = false;
			var sb = new StringBuilder();

			Point pt1, pt2, pt3;
			IList<Point> pts;

			while (!end) {
				var b = reader.ReadByte();

				switch ((PathOpCodes)(b & 0xf)) {
					case PathOpCodes.BeginFigure: {
						ReadPointBoolBool(reader, b, out pt1, out bool filled, out bool closed);

						sb.AppendFormat("M{0} ", pt1);
						break;
					}

					case PathOpCodes.LineTo: {
						ReadPointBoolBool(reader, b, out pt1, out bool stroked, out bool smoothJoin);

						sb.AppendFormat("L{0} ", pt1);
						break;
					}

					case PathOpCodes.QuadraticBezierTo: {
						ReadPointBoolBool(reader, b, out pt1, out bool stroked, out bool smoothJoin);
						pt2 = ReadPoint(reader);

						sb.AppendFormat("Q{0} {1} ", pt1, pt2);
						break;
					}

					case PathOpCodes.BezierTo: {
						ReadPointBoolBool(reader, b, out pt1, out bool stroked, out bool smoothJoin);
						pt2 = ReadPoint(reader);
						pt3 = ReadPoint(reader);

						sb.AppendFormat("C{0} {1} {2} ", pt1, pt2, pt3);
						break;
					}

					case PathOpCodes.PolyLineTo: {
						pts = ReadPointsBoolBool(reader, b, out bool stroked, out bool smoothJoin);

						sb.Append('L');
						foreach (var pt in pts)
							sb.AppendFormat("{0} ", pt);
						break;
					}

					case PathOpCodes.PolyQuadraticBezierTo: {
						pts = ReadPointsBoolBool(reader, b, out bool stroked, out bool smoothJoin);

						sb.Append('Q');
						foreach (var pt in pts)
							sb.AppendFormat("{0} ", pt);
						break;
					}

					case PathOpCodes.PolyBezierTo: {
						pts = ReadPointsBoolBool(reader, b, out bool stroked, out bool smoothJoin);

						sb.Append('C');
						foreach (var pt in pts)
							sb.AppendFormat("{0} ", pt);
						break;
					}

					case PathOpCodes.ArcTo: {
						ReadPointBoolBool(reader, b, out pt1, out bool stroked, out bool smoothJoin);
						byte b2 = reader.ReadByte();
						bool largeArc = (b2 & 0x0f) != 0;
						bool sweepDirection = (b2 & 0xf0) != 0;
						var size = ReadPoint(reader);
						double angle = reader.ReadXamlDouble();

						sb.AppendFormat(CultureInfo.InvariantCulture, "A{0} {2:R} {2} {3} {4}",
							size, angle, largeArc ? '1' : '0', sweepDirection ? '1' : '0', pt1);
						break;
					}
					case PathOpCodes.Closed:
						end = true;
						break;

					case PathOpCodes.FillRule: {
						UnpackBools(b, out bool fillRule, out bool b2, out bool b3, out bool b4);
						if (fillRule)
							sb.Insert(0, "F1 ");
						break;
					}
				}
			}

			return sb.ToString().Trim();
		}
	}
}
