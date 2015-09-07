/*
    Copyright (C) 2014-2015 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Text;
using dndbg.Engine;

namespace dnSpy.Debugger.CallStack {
	struct CachedOutput : IEquatable<CachedOutput> {
		public readonly List<Tuple<string, TypeColor>> data;

		public CachedOutput(bool dummy) {
			this.data = new List<Tuple<string, TypeColor>>();
		}

		public void Add(string s, TypeColor type) {
			data.Add(Tuple.Create(s, type));
		}

		public bool Equals(CachedOutput other) {
			if (data.Count != other.data.Count)
				return false;
			for (int i = 0; i < data.Count; i++) {
				var di = data[i];
				var odi = other.data[i];
				if (di.Item2 != odi.Item2 || di.Item1 != odi.Item1)
					return false;
			}
			return true;
		}

		public override bool Equals(object obj) {
			return obj is CachedOutput && Equals((CachedOutput)obj);
		}

		public override int GetHashCode() {
			int hc = data.Count << 16;
			foreach (var d in data)
				hc ^= d.Item1.GetHashCode() ^ (int)d.Item2;
			return hc;
		}

		public override string ToString() {
			var sb = new StringBuilder();
			foreach (var t in data)
				sb.Append(t.Item1);
			return sb.ToString();
		}

		sealed class TypeOutput : ITypeOutput {
			public CachedOutput cachedOutput = new CachedOutput(true);

			public void Write(string s, TypeColor type) {
				cachedOutput.Add(s, type);
			}
		}

		public static CachedOutput Create(string msg, TypeColor color) {
			var output = new CachedOutput(true);
			output.Add(msg, color);
			return output;
		}

		public static CachedOutput Create(CorFrame frame, TypePrinterFlags flags) {
			var output = new TypeOutput();
			frame.Write(output, flags);
			return output.cachedOutput;
		}
	}
}
