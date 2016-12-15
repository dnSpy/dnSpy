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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Intellisense;
using dnSpy.Contracts.Hex.Tagging;
using CTC = dnSpy.Contracts.Text.Classification;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Intellisense {
	abstract class HexToolTipServiceFactory {
		public abstract HexToolTipService Get(HexView hexView);
	}

	[Export(typeof(HexToolTipServiceFactory))]
	sealed class HexToolTipServiceFactoryImpl : HexToolTipServiceFactory {
		readonly HexViewTagAggregatorFactoryService viewTagAggregatorFactoryService;

		[ImportingConstructor]
		HexToolTipServiceFactoryImpl(HexViewTagAggregatorFactoryService viewTagAggregatorFactoryService) {
			this.viewTagAggregatorFactoryService = viewTagAggregatorFactoryService;
		}

		public override HexToolTipService Get(HexView hexView) {
			if (hexView == null)
				throw new ArgumentNullException(nameof(hexView));
			return hexView.Properties.GetOrCreateSingletonProperty(typeof(HexToolTipServiceImpl), () => new HexToolTipServiceImpl(viewTagAggregatorFactoryService, hexView));
		}
	}

	sealed class HexToolTipInfo {
		public HexBufferSpan BufferSpan { get; }
		public string ClassificationType { get; set; }
		public object ToolTip { get; }

		public HexToolTipInfo(HexBufferSpan bufferSpan, object toolTip) {
			if (bufferSpan.IsDefault)
				throw new ArgumentException();
			BufferSpan = bufferSpan;
			ToolTip = toolTip;
		}
	}

	sealed class HexToolTipInfoCollection : IEnumerable<HexToolTipInfo> {
		public HexBufferSpan FullBufferSpan { get; }
		public HexBufferSpan BufferSpan { get; }
		public int Count => infos.Length;
		public HexToolTipInfo this[int index] => infos[index];
		readonly HexToolTipInfo[] infos;

		public HexToolTipInfoCollection(HexToolTipInfo[] infos) {
			if (infos == null)
				throw new ArgumentNullException(nameof(infos));
			if (infos.Length == 0)
				throw new ArgumentOutOfRangeException(nameof(infos));
			this.infos = infos;

			var start = infos[0].BufferSpan.Start;
			var end = infos[0].BufferSpan.End;
			for (int i = 1; i < infos.Length; i++) {
				var span = infos[i].BufferSpan;
				if (span.Start < start)
					start = span.Start;
				if (span.End > end)
					end = span.End;
			}
			FullBufferSpan = HexBufferSpan.FromBounds(start, end);

			Array.Sort(infos, (a, b) => {
				if ((a.ToolTip != null) != (b.ToolTip != null))
					return a.ToolTip != null ? -1 : 1;
				if (a.BufferSpan.Length != b.BufferSpan.Length)
					return a.BufferSpan.Length.CompareTo(b.BufferSpan.Length);
				return Array.IndexOf(infos, a) - Array.IndexOf(infos, b);
			});
			BufferSpan = infos[0].BufferSpan;
			Debug.Assert(infos[0].ToolTip != null);

			int index = 0;
			foreach (var info in infos.OrderBy(a => a.BufferSpan.Start)) {
				if (info.ClassificationType == null) {
					info.ClassificationType = info.ToolTip != null ?
						CTC.ThemeClassificationTypeNameKeys.HexToolTipServiceCurrentField :
						(index & 1) == 0 ? CTC.ThemeClassificationTypeNameKeys.HexToolTipServiceField0 :
						CTC.ThemeClassificationTypeNameKeys.HexToolTipServiceField1;
					if (info.ToolTip == null)
						index++;
				}
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		public IEnumerator<HexToolTipInfo> GetEnumerator() {
			foreach (var info in infos)
				yield return info;
		}
	}

	abstract class HexToolTipService {
		public abstract HexToolTipInfoCollection GetToolTipInfo(HexBufferPoint position);
		public abstract void SetActiveToolTip(HexToolTipInfoCollection collection);
		public abstract void RemoveActiveToolTip(HexToolTipInfoCollection collection);
		public abstract IEnumerable<IHexTagSpan<HexMarkerTag>> GetTags(NormalizedHexBufferSpanCollection spans);
		public abstract void RegisterTagger(IHexToolTipServiceTagger tagger);
	}

	[Export(typeof(HexQuickInfoSourceProvider))]
	[VSUTIL.Name("HexToolTipService")]
	sealed class HexToolTipServiceQuickInfoSourceProvider : HexQuickInfoSourceProvider {
		readonly HexToolTipServiceFactory hexToolTipServiceFactory;

		[ImportingConstructor]
		HexToolTipServiceQuickInfoSourceProvider(HexToolTipServiceFactory hexToolTipServiceFactory) {
			this.hexToolTipServiceFactory = hexToolTipServiceFactory;
		}

		public override HexQuickInfoSource TryCreateQuickInfoSource(HexView hexView) =>
			new HexToolTipServiceQuickInfoSource(hexToolTipServiceFactory.Get(hexView));
	}

	sealed class HexToolTipServiceQuickInfoSource : HexQuickInfoSource {
		readonly HexToolTipService hexToolTipService;
		HexToolTipInfoCollection toolTipInfoCollection;

		public HexToolTipServiceQuickInfoSource(HexToolTipService hexToolTipService) {
			if (hexToolTipService == null)
				throw new ArgumentNullException(nameof(hexToolTipService));
			this.hexToolTipService = hexToolTipService;
		}

		public override void AugmentQuickInfoSession(HexQuickInfoSession session, IList<object> quickInfoContent, out HexBufferSpanSelection applicableToSpan) {
			applicableToSpan = default(HexBufferSpanSelection);

			RemoveToolTipInfo();

			toolTipInfoCollection = hexToolTipService.GetToolTipInfo(session.TriggerPoint.BufferPosition);
			if (toolTipInfoCollection == null)
				return;

			const HexSpanSelectionFlags flags = HexSpanSelectionFlags.Values | HexSpanSelectionFlags.Ascii | HexSpanSelectionFlags.Cell;
			applicableToSpan = new HexBufferSpanSelection(toolTipInfoCollection.BufferSpan, flags);
			hexToolTipService.SetActiveToolTip(toolTipInfoCollection);
			session.Dismissed += Session_Dismissed;
			foreach (var info in toolTipInfoCollection) {
				if (info.ToolTip != null)
					quickInfoContent.Add(info.ToolTip);
			}
		}

		void RemoveToolTipInfo() {
			if (toolTipInfoCollection != null) {
				hexToolTipService.RemoveActiveToolTip(toolTipInfoCollection);
				toolTipInfoCollection = null;
			}
		}

		void Session_Dismissed(object sender, EventArgs e) {
			var session = (HexQuickInfoSession)sender;
			session.Dismissed -= Session_Dismissed;
			RemoveToolTipInfo();
		}
	}

	[Export(typeof(HexViewTaggerProvider))]
	[HexTagType(typeof(HexMarkerTag))]
	sealed class HexToolTipServiceViewTaggerProvider : HexViewTaggerProvider {
		readonly HexToolTipServiceFactory hexToolTipServiceFactory;

		[ImportingConstructor]
		HexToolTipServiceViewTaggerProvider(HexToolTipServiceFactory hexToolTipServiceFactory) {
			this.hexToolTipServiceFactory = hexToolTipServiceFactory;
		}

		public override IHexTagger<T> CreateTagger<T>(HexView hexView, HexBuffer buffer) =>
			new HexToolTipServiceTagger(hexToolTipServiceFactory.Get(hexView)) as IHexTagger<T>;
	}

	interface IHexToolTipServiceTagger {
		void RaiseTagsChanged(HexBufferSpan span);
	}

	sealed class HexToolTipServiceTagger : HexTagger<HexMarkerTag>, IHexToolTipServiceTagger {
		readonly HexToolTipService hexToolTipService;

		public HexToolTipServiceTagger(HexToolTipService hexToolTipService) {
			if (hexToolTipService == null)
				throw new ArgumentNullException(nameof(hexToolTipService));
			this.hexToolTipService = hexToolTipService;
			hexToolTipService.RegisterTagger(this);
		}

		public override event EventHandler<HexBufferSpanEventArgs> TagsChanged;

		public override IEnumerable<IHexTextTagSpan<HexMarkerTag>> GetTags(HexTaggerContext context) {
			yield break;
		}

		public override IEnumerable<IHexTagSpan<HexMarkerTag>> GetTags(NormalizedHexBufferSpanCollection spans) =>
			hexToolTipService.GetTags(spans);

		void IHexToolTipServiceTagger.RaiseTagsChanged(HexBufferSpan span) => TagsChanged?.Invoke(this, new HexBufferSpanEventArgs(span));
	}

	sealed class HexToolTipServiceImpl : HexToolTipService {
		readonly HexView hexView;
		readonly HexTagAggregator<HexToolTipStructureSpanTag> tagAggregator;

		public HexToolTipServiceImpl(HexViewTagAggregatorFactoryService viewTagAggregatorFactoryService, HexView hexView) {
			if (hexView == null)
				throw new ArgumentNullException(nameof(hexView));
			if (viewTagAggregatorFactoryService == null)
				throw new ArgumentNullException(nameof(viewTagAggregatorFactoryService));
			this.hexView = hexView;
			tagAggregator = viewTagAggregatorFactoryService.CreateTagAggregator<HexToolTipStructureSpanTag>(hexView);
			hexView.Closed += HexView_Closed;
		}

		public override HexToolTipInfoCollection GetToolTipInfo(HexBufferPoint position) {
			if (position.IsDefault)
				throw new ArgumentException();
			if (position > HexPosition.MaxEndPosition)
				throw new ArgumentOutOfRangeException(nameof(position));
			if (hexView.IsClosed)
				return null;
			if (position >= HexPosition.MaxEndPosition)
				return null;
			return TryCreateToolTipInfoCollection(position, tagAggregator.GetTags(new HexBufferSpan(position, 1)).ToArray());
		}

		HexToolTipInfoCollection TryCreateToolTipInfoCollection(HexBufferPoint position, IHexTagSpan<HexToolTipStructureSpanTag>[] tagSpans) {
			if (tagSpans.Length == 0)
				return null;
			var toolTipInfos = new List<HexToolTipInfo>(tagSpans.Length);
			int toolTips = 0;
			for (int i = 0; i < tagSpans.Length; i++) {
				var tagSpan = tagSpans[i];
				if (!tagSpan.Span.Contains(position))
					continue;

				if (tagSpan.Tag.ToolTip != null) {
					if (!tagSpan.Tag.BufferSpan.Contains(position))
						continue;
					toolTips++;
				}
				var info = new HexToolTipInfo(tagSpan.Tag.BufferSpan, tagSpan.Tag.ToolTip);
				toolTipInfos.Add(info);
			}
			if (toolTips == 0)
				return null;
			return new HexToolTipInfoCollection(toolTipInfos.ToArray());
		}

		public override void SetActiveToolTip(HexToolTipInfoCollection collection) {
			if (collection == null)
				throw new ArgumentNullException(nameof(collection));
			if (hexView.IsClosed)
				return;
			RemoveCurrentToolTip();
			activeToolTipInfoCollection = collection;
			tagger?.RaiseTagsChanged(activeToolTipInfoCollection.FullBufferSpan);
		}
		HexToolTipInfoCollection activeToolTipInfoCollection;

		public override void RemoveActiveToolTip(HexToolTipInfoCollection collection) {
			if (collection == null)
				throw new ArgumentNullException(nameof(collection));
			if (hexView.IsClosed)
				return;
			if (activeToolTipInfoCollection != collection)
				return;
			RemoveCurrentToolTip();
		}

		void RemoveCurrentToolTip() {
			var oldCollection = activeToolTipInfoCollection;
			if (oldCollection == null)
				return;
			activeToolTipInfoCollection = null;
			tagger?.RaiseTagsChanged(oldCollection.FullBufferSpan);
		}

		public override IEnumerable<IHexTagSpan<HexMarkerTag>> GetTags(NormalizedHexBufferSpanCollection spans) {
			var collection = activeToolTipInfoCollection;
			if (collection == null)
				yield break;

			foreach (var span in spans) {
				if (!collection.FullBufferSpan.IntersectsWith(span))
					continue;

				const HexSpanSelectionFlags flags = HexSpanSelectionFlags.Values | HexSpanSelectionFlags.Ascii | HexSpanSelectionFlags.Cell;
				foreach (var info in collection)
					yield return new HexTagSpan<HexMarkerTag>(info.BufferSpan, flags, new HexMarkerTag(info.ClassificationType));
			}
		}

		public override void RegisterTagger(IHexToolTipServiceTagger tagger) {
			if (tagger == null)
				throw new ArgumentNullException(nameof(tagger));
			if (this.tagger != null)
				throw new InvalidOperationException();
			this.tagger = tagger;
		}
		IHexToolTipServiceTagger tagger;

		void HexView_Closed(object sender, EventArgs e) {
			hexView.Closed -= HexView_Closed;
			tagAggregator.Dispose();
		}
	}
}
