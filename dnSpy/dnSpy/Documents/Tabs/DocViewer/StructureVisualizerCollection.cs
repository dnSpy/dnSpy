/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using System.Diagnostics;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Documents.Tabs.DocViewer {
	sealed class StructureVisualizerCollection {
		public static readonly StructureVisualizerCollection Empty = new StructureVisualizerCollection(Array.Empty<CodeBracesRange>());

		readonly SpanDataCollection<CodeBracesRange[]> coll;

		struct Builder {
			readonly List<SpanData<CodeBracesRange[]>> infos;
			readonly List<CodeBracesRange> list;
			readonly Stack<CodeBracesRange[]> listStack;

			public Builder(CodeBracesRange[] ranges) {
				this.infos = new List<SpanData<CodeBracesRange[]>>();
				this.list = new List<CodeBracesRange>();
				this.listStack = new Stack<CodeBracesRange[]>();
				Array.Sort(ranges, Sorter.Instance);

				for (int i = 0; i < ranges.Length; i++) {
					var curr = ranges[i];
					if (!curr.Flags.IsBlock())
						continue;

					Close(curr.Left.Start);
					Add(curr);
				}
				Close(int.MaxValue);
				Debug.Assert(list.Count == 0);
				int write = 0;
				for (int read = 0; read < infos.Count; read++) {
					var info = infos[read];
					if (info.Span.Length == 0)
						continue;
					if (read != write)
						infos[write] = info;
					write++;
				}
				if (write != infos.Count)
					infos.RemoveRange(write, infos.Count - write);
#if DEBUG
				for (int i = 1; i < infos.Count; i++) {
					if (infos[i - 1].Span.End > infos[i].Span.Start)
						throw new InvalidOperationException();
				}
#endif
			}

			void Add(CodeBracesRange curr) {
				list.Add(curr);
				listStack.Push(list.ToArray());
				infos.Add(new SpanData<CodeBracesRange[]>(new Span(curr.Left.Start, 0), listStack.Peek()));
			}

			void Close(int pos) {
				while (list.Count > 0) {
					int index = list.Count - 1;
					var last = list[index];
					if (last.Right.End <= pos) {
						var tmp = infos[infos.Count - 1];
						infos[infos.Count - 1] = new SpanData<CodeBracesRange[]>(Span.FromBounds(tmp.Span.Start, last.Right.End), tmp.Data);
						list.RemoveAt(index);
						listStack.Pop();
						if (list.Count > 0)
							infos.Add(new SpanData<CodeBracesRange[]>(new Span(last.Right.End, 0), listStack.Peek()));
					}
					else {
						var tmp = infos[infos.Count - 1];
						Debug.Assert(tmp.Span.Length == 0);
						infos[infos.Count - 1] = new SpanData<CodeBracesRange[]>(Span.FromBounds(tmp.Span.Start, Math.Min(pos, last.Right.End)), tmp.Data);
						break;
					}
				}
			}

			public SpanData<CodeBracesRange[]>[] Build() => infos.ToArray();

			sealed class Sorter : IComparer<CodeBracesRange> {
				public static readonly Sorter Instance = new Sorter();
				public int Compare(CodeBracesRange x, CodeBracesRange y) {
					int c = x.Left.Start - y.Left.Start;
					if (c != 0)
						return c;
					return y.Left.End - x.Left.End;
				}
			}
		}

		public StructureVisualizerCollection(CodeBracesRange[] ranges) {
			if (ranges.Length == 0)
				coll = SpanDataCollection<CodeBracesRange[]>.Empty;
			else {
				var builder = new Builder(ranges);
				coll = new SpanDataCollection<CodeBracesRange[]>(builder.Build());
			}
		}

		public void GetData(SnapshotSpan lineExtent, List<StructureVisualizerData> list) {
			foreach (var spanData in coll.Find(lineExtent.Span)) {
				if (spanData.Span.End == lineExtent.Start.Position)
					continue;
				foreach (var info in spanData.Data) {
					var data = CreateStructureVisualizerData(info, lineExtent.Snapshot);
					if (data != null)
						list.Add(data.Value);
				}
				return;
			}
		}

		StructureVisualizerData? CreateStructureVisualizerData(CodeBracesRange info, ITextSnapshot snapshot) {
			var blockKind = GetBlockKind(info.Flags);
			if (blockKind == StructureVisualizerDataBlockKind.None)
				return null;

			if (info.Right.End > snapshot.Length)
				return null;
			if (info.Left.End > info.Right.Start)
				return null;
			var top = new SnapshotSpan(snapshot, info.Left.Start, info.Left.Length);
			var bottom = new SnapshotSpan(snapshot, info.Right.Start, info.Right.Length);
			return new StructureVisualizerData(top, bottom, blockKind);
		}

		static StructureVisualizerDataBlockKind GetBlockKind(CodeBracesRangeFlags flags) {
			switch (flags.ToBlockKind()) {
			case CodeBracesRangeFlags.BlockKind_Namespace:		return StructureVisualizerDataBlockKind.Namespace;
			case CodeBracesRangeFlags.BlockKind_Type:			return StructureVisualizerDataBlockKind.Type;
			case CodeBracesRangeFlags.BlockKind_ValueType:		return StructureVisualizerDataBlockKind.ValueType;
			case CodeBracesRangeFlags.BlockKind_Interface:		return StructureVisualizerDataBlockKind.Interface;
			case CodeBracesRangeFlags.BlockKind_Method:			return StructureVisualizerDataBlockKind.Method;
			case CodeBracesRangeFlags.BlockKind_Accessor:		return StructureVisualizerDataBlockKind.Accessor;
			case CodeBracesRangeFlags.BlockKind_AnonymousMethod:return StructureVisualizerDataBlockKind.AnonymousMethod;
			case CodeBracesRangeFlags.BlockKind_Constructor:	return StructureVisualizerDataBlockKind.Constructor;
			case CodeBracesRangeFlags.BlockKind_Destructor:		return StructureVisualizerDataBlockKind.Destructor;
			case CodeBracesRangeFlags.BlockKind_Operator:		return StructureVisualizerDataBlockKind.Operator;
			case CodeBracesRangeFlags.BlockKind_Conditional:	return StructureVisualizerDataBlockKind.Conditional;
			case CodeBracesRangeFlags.BlockKind_Loop:			return StructureVisualizerDataBlockKind.Loop;
			case CodeBracesRangeFlags.BlockKind_Property:		return StructureVisualizerDataBlockKind.Property;
			case CodeBracesRangeFlags.BlockKind_Event:			return StructureVisualizerDataBlockKind.Event;
			case CodeBracesRangeFlags.BlockKind_Try:			return StructureVisualizerDataBlockKind.Try;
			case CodeBracesRangeFlags.BlockKind_Catch:			return StructureVisualizerDataBlockKind.Catch;
			case CodeBracesRangeFlags.BlockKind_Filter:			return StructureVisualizerDataBlockKind.Filter;
			case CodeBracesRangeFlags.BlockKind_Finally:		return StructureVisualizerDataBlockKind.Finally;
			case CodeBracesRangeFlags.BlockKind_Fault:			return StructureVisualizerDataBlockKind.Fault;
			case CodeBracesRangeFlags.BlockKind_Lock:			return StructureVisualizerDataBlockKind.Lock;
			case CodeBracesRangeFlags.BlockKind_Using:			return StructureVisualizerDataBlockKind.Using;
			case CodeBracesRangeFlags.BlockKind_Fixed:			return StructureVisualizerDataBlockKind.Fixed;
			case CodeBracesRangeFlags.BlockKind_Case:			return StructureVisualizerDataBlockKind.Case;
			case CodeBracesRangeFlags.BlockKind_Other:			return StructureVisualizerDataBlockKind.Other;
			default:
				Debug.Fail($"Unknown block kind: {flags.ToBlockKind()}");
				return StructureVisualizerDataBlockKind.None;
			}
		}
	}
}
