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
using dnlib.DotNet;

namespace dnSpy.Debugger {
	struct CachedOutput : IEquatable<CachedOutput> {
		public readonly List<Tuple<string, TypeColor>> data;

		public static CachedOutput Create() {
			return new CachedOutput(false);
		}

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

		public static CachedOutput CreateConstant(TypeSig type, object c, TypePrinterFlags flags) {
			return TypePrinterUtils.WriteConstant(new TypeOutput(), type, c, flags).cachedOutput;
		}

		public static CachedOutput Create(TypeSig fieldType, TypePrinterFlags flags) {
			fieldType = fieldType.RemovePinnedAndModifiers() ?? fieldType;
			if (fieldType is ByRefSig)
				fieldType = fieldType.Next ?? fieldType;
			var typeOutput = TypePrinterUtils.Write(new TypeOutput(), fieldType, flags);
			return typeOutput.cachedOutput;
		}

		public static CachedOutput Create(CorFrame frame, TypePrinterFlags flags) {
			var output = new TypeOutput();
			frame.Write(output, flags);
			return output.cachedOutput;
		}

		public static CachedOutput CreateValue(CorValue value, TypePrinterFlags flags, Func<DnEval> getEval = null) {
			var output = new TypeOutput();
			if (value == null)
				output.Write("???", TypeColor.Error);
			else
				value.Write(output, flags, getEval);
			return output.cachedOutput;
		}

		public static CachedOutput CreateType(CorValue value, TypePrinterFlags flags) {
			return CreateType(new TypeOutput(), value, flags).cachedOutput;
		}

		static TypeOutput CreateType(TypeOutput output, CorValue value, TypePrinterFlags flags) {
			if (value == null)
				output.Write("???", TypeColor.Error);
			else {
				if (value.IsReference && value.Type == dndbg.COM.CorDebug.CorElementType.ByRef)
					value = value.NeuterCheckDereferencedValue ?? value;

				var type = value.ExactType;
				if (type != null)
					type.Write(output, flags);
				else {
					var cls = value.Class;
					if (cls != null)
						cls.Write(output, flags);
					else
						output.Write("???", TypeColor.Error);
				}
			}
			return output;
		}

		public static CachedOutput CreateType(CorValue value, TypeSig ts, IList<CorType> typeArgs, IList<CorType> methodArgs, TypePrinterFlags flags) {
			if (value == null && ts != null)
				return TypePrinterUtils.Write(new TypeOutput(), ts, flags, typeArgs, methodArgs).cachedOutput;
			var valueOutput = CreateType(new TypeOutput(), value, flags);
			if (ts == null || value == null)
				return valueOutput.cachedOutput;

			ts = ts.RemovePinnedAndModifiers() ?? ts;
			if (ts is ByRefSig)
				ts = ts.Next ?? ts;

			var typeOutput = value.WriteType(new TypeOutput(), ts, typeArgs, methodArgs, flags);
			return CreateTypeInternal(valueOutput, typeOutput);
		}

		static CachedOutput CreateTypeInternal(TypeOutput valueOutput, TypeOutput typeOutput) {
			// This code doesn't compare the types to see if they're identical, it just compares
			// the output. This should be good enough.

			if (typeOutput.cachedOutput.Equals(valueOutput.cachedOutput))
				return valueOutput.cachedOutput;

			typeOutput.Write(" ", TypeColor.Space);
			typeOutput.Write("{", TypeColor.Error);
			typeOutput.cachedOutput.data.AddRange(valueOutput.cachedOutput.data);
			typeOutput.Write("}", TypeColor.Error);
			return typeOutput.cachedOutput;
		}

		public static CachedOutput CreateType(CorValue value, CorType type, TypePrinterFlags flags) {
			var valueOutput = CreateType(new TypeOutput(), value, flags);
			if (type == null || value == null)
				return valueOutput.cachedOutput;

			var typeOutput = value.WriteType(new TypeOutput(), type, flags);
			return CreateTypeInternal(valueOutput, typeOutput);
		}

		public static CachedOutput CreateType(CorValue value, CorClass cls, TypePrinterFlags flags) {
			var valueOutput = CreateType(new TypeOutput(), value, flags);
			if (cls == null || value == null)
				return valueOutput.cachedOutput;

			var typeOutput = value.WriteType(new TypeOutput(), cls, flags);
			return CreateTypeInternal(valueOutput, typeOutput);
		}

		public static CachedOutput Create(CorType type, TypePrinterFlags flags) {
			var output = new TypeOutput();
			if (type == null)
				output.Write("???", TypeColor.Error);
			else
				type.Write(output, flags);
			return output.cachedOutput;
		}
	}
}
