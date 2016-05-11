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
using System.Linq;
using dnSpy.Contracts.TextEditor;
using dnSpy.Contracts.Themes;

namespace dnSpy.Shared.TextEditor {
	public sealed class ColorAggregator {
		ITheme theme;
		Span span;
		readonly List<ColorInfo> colorInfos = new List<ColorInfo>();
		WeakReference extraListWeakRef;
		bool hasFinished;

		public ColorAggregator() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="theme">Theme to use</param>
		/// <param name="span">Valid span</param>
		public ColorAggregator(ITheme theme, Span span) {
			if (theme == null)
				throw new ArgumentNullException(nameof(theme));
			this.theme = theme;
			this.span = span;
		}

		/// <summary>
		/// Initialize this instance. Should be called after <see cref="CleanUp"/>
		/// </summary>
		/// <param name="theme">Theme to use</param>
		/// <param name="span">Valid span</param>
		public void Initialize(ITheme theme, Span span) {
			if (theme == null)
				throw new ArgumentNullException(nameof(theme));
			this.theme = theme;
			this.span = span;
			this.hasFinished = false;
			Debug.Assert(colorInfos.Count == 0);
		}

		/// <summary>
		/// Free stuff so <see cref="Initialize(ITheme, Span)"/> can be called again
		/// </summary>
		public void CleanUp() {
			theme = null;
			colorInfos.Clear();
			(extraListWeakRef?.Target as List<ColorInfo>)?.Clear();
		}

		public void Add(ColorSpan colorSpan) {
			Debug.Assert(!hasFinished);
			var newSpan = colorSpan.Span.Intersection(span);
			if (newSpan == null || newSpan.Value.IsEmpty)
				return;
			var color = colorSpan.Color.ToTextColor(theme);
			if (color == null || (color.Foreground == null && color.Background == null))
				return;
			colorInfos.Add(new ColorInfo(newSpan.Value, color, colorSpan.Priority));
		}

		public void Add(IEnumerable<ColorSpan> colorSpans) {
			Debug.Assert(!hasFinished);
			foreach (var colorSpan in colorSpans)
				Add(colorSpan);
		}

		public List<ColorInfo> Finish() {
			Debug.Assert(!hasFinished);
			hasFinished = true;

			colorInfos.Sort((a, b) => a.Span.Start - b.Span.Start);

			List<ColorInfo> list;
			// Check if it's the common case
			if (!HasOverlaps(colorInfos))
				list = colorInfos;
			else {
				Debug.Assert(colorInfos.Count != 0);

				list = extraListWeakRef?.Target as List<ColorInfo> ?? new List<ColorInfo>();
				var stack = new List<ColorInfo>();
				int currOffs = 0;
				for (int i = 0; i < colorInfos.Count;) {
					if (stack.Count == 0)
						currOffs = colorInfos[i].Span.Start;
					for (; i < colorInfos.Count; i++) {
						var curr = colorInfos[i];
						if (curr.Span.Start != currOffs)
							break;
						stack.Add(curr);
					}
					Debug.Assert(stack.Count != 0);
					Debug.Assert(stack.All(a => a.Span.Start == currOffs));

					var newInfo = AddColor(list, stack, currOffs, i);

					for (int j = stack.Count - 1; j >= 0; j--) {
						var info = stack[j];
						if (newInfo.Span.End >= info.Span.End)
							stack.RemoveAt(j);
						else
							stack[j] = new ColorInfo(Span.FromBounds(newInfo.Span.End, info.Span.End), info.Foreground, info.Background, info.Priority);
					}
					currOffs = newInfo.Span.End;
				}
				if (stack.Count != 0) {
					Debug.Assert(stack.All(a => a.Span == stack[0].Span));
					AddColor(list, stack, currOffs, colorInfos.Count);
				}

				if (extraListWeakRef?.Target == null)
					extraListWeakRef = new WeakReference(list);
			}
			Debug.Assert(!HasOverlaps(list));
			return list;
		}

		ColorInfo AddColor(List<ColorInfo> list, List<ColorInfo> stack, int currOffs, int index) {
			stack.Sort((a, b) => b.Priority.CompareTo(a.Priority));
			int end = stack.Min(a => a.Span.End);
			end = Math.Min(end, index < colorInfos.Count ? colorInfos[index].Span.Start : span.End);
			var fgColor = stack.FirstOrDefault(a => a.Foreground?.Foreground != null);
			var bgColor = stack.FirstOrDefault(a => a.Background?.Background != null);
			var newInfo = new ColorInfo(Span.FromBounds(currOffs, end), fgColor.Foreground, bgColor.Background, 0);
			Debug.Assert(list.Count == 0 || list[list.Count - 1].Span.End <= newInfo.Span.Start);
			list.Add(newInfo);
			return newInfo;
		}

		bool HasOverlaps(List<ColorInfo> sortedList) {
			for (int i = 1; i < sortedList.Count; i++) {
				if (sortedList[i - 1].Span.End > sortedList[i].Span.Start)
					return true;
			}
			return false;
		}
	}
}
