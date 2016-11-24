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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	[Export(typeof(IViewTaggerProvider))]
	[ContentType(ContentTypes.Any)]
	[TagType(typeof(SpaceNegotiatingAdornmentTag))]
	sealed class IntraTextAdornmentServiceSpaceNegotiatingAdornmentTaggerProvider : IViewTaggerProvider {
		readonly IIntraTextAdornmentServiceProvider intraTextAdornmentServiceProvider;

		[ImportingConstructor]
		IntraTextAdornmentServiceSpaceNegotiatingAdornmentTaggerProvider(IIntraTextAdornmentServiceProvider intraTextAdornmentServiceProvider) {
			this.intraTextAdornmentServiceProvider = intraTextAdornmentServiceProvider;
		}

		public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag {
			if (textView.TextBuffer != buffer)
				return null;
			var wpfTextView = textView as IWpfTextView;
			Debug.Assert(wpfTextView != null);
			if (wpfTextView == null)
				return null;
			return wpfTextView.Properties.GetOrCreateSingletonProperty(
				typeof(IntraTextAdornmentServiceSpaceNegotiatingAdornmentTagger),
				() => new IntraTextAdornmentServiceSpaceNegotiatingAdornmentTagger(intraTextAdornmentServiceProvider.Get(wpfTextView))) as ITagger<T>;
		}
	}

	interface IIntraTextAdornmentServiceSpaceNegotiatingAdornmentTagger {
		void RefreshSpans(SnapshotSpanEventArgs e);
	}

	sealed class IntraTextAdornmentServiceSpaceNegotiatingAdornmentTagger : ITagger<SpaceNegotiatingAdornmentTag>, IIntraTextAdornmentServiceSpaceNegotiatingAdornmentTagger {
		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		readonly IIntraTextAdornmentService intraTextAdornmentService;

		public IntraTextAdornmentServiceSpaceNegotiatingAdornmentTagger(IIntraTextAdornmentService intraTextAdornmentService) {
			if (intraTextAdornmentService == null)
				throw new ArgumentNullException(nameof(intraTextAdornmentService));
			this.intraTextAdornmentService = intraTextAdornmentService;
			intraTextAdornmentService.RegisterTagger(this);
		}

		public IEnumerable<ITagSpan<SpaceNegotiatingAdornmentTag>> GetTags(NormalizedSnapshotSpanCollection spans) =>
			intraTextAdornmentService.GetTags(spans);

		public void RefreshSpans(SnapshotSpanEventArgs e) => TagsChanged?.Invoke(this, e);
	}

	interface IIntraTextAdornmentServiceProvider {
		IIntraTextAdornmentService Get(IWpfTextView wpfTextView);
	}

	[Export(typeof(IIntraTextAdornmentServiceProvider))]
	sealed class IntraTextAdornmentServiceProvider : IIntraTextAdornmentServiceProvider {
		readonly IViewTagAggregatorFactoryService viewTagAggregatorFactoryService;

		[ImportingConstructor]
		IntraTextAdornmentServiceProvider(IViewTagAggregatorFactoryService viewTagAggregatorFactoryService) {
			this.viewTagAggregatorFactoryService = viewTagAggregatorFactoryService;
		}

		public IIntraTextAdornmentService Get(IWpfTextView wpfTextView) {
			if (wpfTextView == null)
				throw new ArgumentNullException(nameof(wpfTextView));
			return wpfTextView.Properties.GetOrCreateSingletonProperty(typeof(IntraTextAdornmentService), () => new IntraTextAdornmentService(wpfTextView, viewTagAggregatorFactoryService));
		}
	}

	interface IIntraTextAdornmentService {
		IEnumerable<ITagSpan<SpaceNegotiatingAdornmentTag>> GetTags(NormalizedSnapshotSpanCollection spans);
		void RegisterTagger(IIntraTextAdornmentServiceSpaceNegotiatingAdornmentTagger tagger);
	}

	sealed class IntraTextAdornmentService : IIntraTextAdornmentService {
#pragma warning disable 0169
		[Export(typeof(AdornmentLayerDefinition))]
		[Name(PredefinedDsAdornmentLayers.IntraTextAdornment)]
		[Order(After = PredefinedDsAdornmentLayers.BottomLayer, Before = PredefinedDsAdornmentLayers.TopLayer)]
		[Order(After = PredefinedAdornmentLayers.Text)]
		static AdornmentLayerDefinition adornmentLayer;
#pragma warning restore 0169

		readonly IWpfTextView wpfTextView;
		readonly ITagAggregator<IntraTextAdornmentTag> tagAggregator;
		readonly List<AdornmentTagInfo> adornmentTagInfos;
		readonly HashSet<object> currentLineIdentityTags;
		IAdornmentLayer layer;
		IIntraTextAdornmentServiceSpaceNegotiatingAdornmentTagger tagger;
		static readonly object providerTag = new object();

		public IntraTextAdornmentService(IWpfTextView wpfTextView, IViewTagAggregatorFactoryService viewTagAggregatorFactoryService) {
			if (wpfTextView == null)
				throw new ArgumentNullException(nameof(wpfTextView));
			if (viewTagAggregatorFactoryService == null)
				throw new ArgumentNullException(nameof(viewTagAggregatorFactoryService));
			adornmentTagInfos = new List<AdornmentTagInfo>();
			currentLineIdentityTags = new HashSet<object>();
			this.wpfTextView = wpfTextView;
			tagAggregator = viewTagAggregatorFactoryService.CreateTagAggregator<IntraTextAdornmentTag>(wpfTextView);
			tagAggregator.TagsChanged += TagAggregator_TagsChanged;
			wpfTextView.Closed += WpfTextView_Closed;
			wpfTextView.LayoutChanged += WpfTextView_LayoutChanged;
		}

		void Selection_SelectionChanged(object sender, EventArgs e) => UpdateIsSelected();

		void UpdateIsSelected() {
			if (adornmentTagInfos.Count == 0)
				return;
			if (wpfTextView.Selection.IsEmpty) {
				foreach (var info in adornmentTagInfos)
					IntraTextAdornment.SetIsSelected(info.UserUIElement, false);
			}
			else {
				foreach (var info in adornmentTagInfos)
					UpdateIsSelected(info, null);
			}
		}

		void UpdateIsSelected(AdornmentTagInfo adornmentInfo, ITextViewLine line) {
			if (line == null)
				line = wpfTextView.TextViewLines.GetTextViewLineContainingBufferPosition(adornmentInfo.Span.Start);
			var selSpan = line == null ? null : wpfTextView.Selection.GetSelectionOnTextViewLine(line);
			bool selected = selSpan != null && selSpan.Value.Contains(new VirtualSnapshotSpan(adornmentInfo.Span));
			IntraTextAdornment.SetIsSelected(adornmentInfo.UserUIElement, selected);
		}

		sealed class AdornmentTagInfo {
			public SpaceNegotiatingAdornmentTag Tag;
			public readonly SnapshotSpan Span;
			public readonly UIElement UserUIElement;
			public ZoomingUIElement TopUIElement;
			public readonly IMappingTagSpan<IntraTextAdornmentTag> TagSpan;
			public object LineIdentityTag;

			public AdornmentTagInfo(SnapshotSpan span, UIElement element, IMappingTagSpan<IntraTextAdornmentTag> tagSpan) {
				Span = span;
				UserUIElement = element;
				TagSpan = tagSpan;
			}

			public void Initialize() {
				if (TopUIElement == null)
					TopUIElement = new ZoomingUIElement(UserUIElement);
				TopUIElement.Initialize();
				TopUIElement.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			}
		}

		void WpfTextView_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e) {
			if (adornmentTagInfos.Count > 0) {
				currentLineIdentityTags.Clear();
				foreach (var line in wpfTextView.TextViewLines)
					currentLineIdentityTags.Add(line.IdentityTag);
				foreach (var line in e.NewOrReformattedLines)
					currentLineIdentityTags.Remove(line.IdentityTag);
				for (int i = adornmentTagInfos.Count - 1; i >= 0; i--) {
					var adornmentInfo = adornmentTagInfos[i];
					if (!currentLineIdentityTags.Contains(adornmentInfo.LineIdentityTag))
						layer.RemoveAdornmentsByTag(adornmentInfo);
				}
				currentLineIdentityTags.Clear();

				foreach (var line in e.TranslatedLines) {
					var tags = line.GetAdornmentTags(providerTag);
					if (tags.Count == 0)
						continue;

					foreach (var identityTag in tags) {
						var adornmentInfo = identityTag as AdornmentTagInfo;
						Debug.Assert(adornmentInfo != null);
						if (adornmentInfo == null)
							continue;
						var bounds = line.GetAdornmentBounds(identityTag);
						Debug.Assert(bounds != null);
						if (bounds == null)
							continue;

						adornmentInfo.Initialize();
						UpdateAdornmentUIState(line, adornmentInfo, bounds.Value);
					}
				}
			}

			foreach (var line in e.NewOrReformattedLines) {
				var tags = line.GetAdornmentTags(providerTag);
				if (tags.Count == 0)
					continue;

				foreach (var identityTag in tags) {
					var adornmentInfo = identityTag as AdornmentTagInfo;
					Debug.Assert(adornmentInfo != null);
					if (adornmentInfo == null)
						continue;
					var bounds = line.GetAdornmentBounds(identityTag);
					if (bounds == null)
						continue;

					if (layer == null) {
						layer = wpfTextView.GetAdornmentLayer(PredefinedDsAdornmentLayers.IntraTextAdornment);
						// Can't do this in the ctor since Selection hasn't been initialized yet
						wpfTextView.Selection.SelectionChanged += Selection_SelectionChanged;
					}

					adornmentInfo.Initialize();
					UpdateAdornmentUIState(line, adornmentInfo, bounds.Value);
					bool added = AddAdornment(adornmentInfo, line);
					if (!added)
						continue;
					adornmentInfo.LineIdentityTag = line.IdentityTag;
					UpdateIsSelected(adornmentInfo, line);
				}
			}
		}

		void UpdateAdornmentUIState(ITextViewLine line, AdornmentTagInfo adornmentInfo, TextBounds bounds) {
			double verticalScale = line.LineTransform.VerticalScale;
			adornmentInfo.TopUIElement.SetScale(verticalScale);
			Canvas.SetTop(adornmentInfo.TopUIElement, bounds.TextTop + line.Baseline - verticalScale * adornmentInfo.Tag.Baseline);
			Canvas.SetLeft(adornmentInfo.TopUIElement, bounds.Left);
		}

		bool AddAdornment(AdornmentTagInfo adornmentInfo, ITextViewLine line) {
			SizeChangedEventHandler sizeChanged = (a, e) => {
				var bounds = line.GetAdornmentBounds(adornmentInfo);
				if (bounds == null)
					return;
				// Sometimes the size just gets changed very little, eg. from 400 to 399.95.....
				const double d = 0.5;
				if (e.NewSize.Height <= bounds.Value.Height + d && e.NewSize.Width <= bounds.Value.Width + d)
					return;
				tagger?.RefreshSpans(new SnapshotSpanEventArgs(adornmentInfo.Span));
			};
			adornmentInfo.TopUIElement.SizeChanged += sizeChanged;

			AdornmentRemovedCallback removedCallback = (a, b) => {
				adornmentTagInfos.Remove(adornmentInfo);
				adornmentInfo.TopUIElement.SizeChanged -= sizeChanged;
				adornmentInfo.TopUIElement.OnRemoved();
				adornmentInfo.TagSpan.Tag.RemovalCallback?.Invoke(adornmentInfo.TagSpan, b);
			};

			Debug.Assert(!adornmentTagInfos.Contains(adornmentInfo));
			adornmentTagInfos.Add(adornmentInfo);
			// Use OwnerControlled because there are corner cases that the adornment layer can't handle,
			// eg. an adornment with buffer span length == 0 that is shown on its own line (word wrap).
			bool added = layer.AddAdornment(AdornmentPositioningBehavior.OwnerControlled, null, adornmentInfo, adornmentInfo.TopUIElement, removedCallback);
			if (!added)
				removedCallback(null, adornmentInfo.TopUIElement);
			return added;
		}

		sealed class ZoomingUIElement : ContentControl {
			readonly UIElement uiElem;
			public ZoomingUIElement(UIElement uiElem) {
				this.uiElem = uiElem;
			}
			public void Initialize() => Content = uiElem;
			public void SetScale(double value) =>
				LayoutTransform = value == 1 ? ScaleTransform.Identity : new ScaleTransform(1, value);
			// Make sure the UIElement can be cached and re-used
			public void OnRemoved() => Content = null;
		}

		public void RegisterTagger(IIntraTextAdornmentServiceSpaceNegotiatingAdornmentTagger tagger) {
			if (this.tagger != null)
				throw new InvalidOperationException();
			if (tagger == null)
				throw new ArgumentNullException(nameof(tagger));
			this.tagger = tagger;
		}

		public IEnumerable<ITagSpan<SpaceNegotiatingAdornmentTag>> GetTags(NormalizedSnapshotSpanCollection spans) {
			if (wpfTextView.IsClosed)
				yield break;

			foreach (var span in spans) {
				foreach (var tagSpan in tagAggregator.GetTags(span)) {
					var spanColl = tagSpan.Span.GetSpans(wpfTextView.TextSnapshot);
					Debug.Assert(spanColl.Count != 0);
					if (spanColl.Count == 0)
						continue;
					var fullSpan = new SnapshotSpan(spanColl[0].Snapshot, Span.FromBounds(spanColl[0].Span.Start, spanColl[spanColl.Count - 1].Span.End));
					var uiElem = tagSpan.Tag.Adornment;
					double topSpace = tagSpan.Tag.TopSpace ?? 0;
					double bottomSpace = tagSpan.Tag.BottomSpace ?? 0;
					double textHeight = tagSpan.Tag.TextHeight ?? (Filter(uiElem.DesiredSize.Height) - (topSpace + bottomSpace));
					var adornmentInfo = new AdornmentTagInfo(fullSpan, uiElem, tagSpan);
					var tag = new SpaceNegotiatingAdornmentTag(Filter(uiElem.DesiredSize.Width), topSpace,
								tagSpan.Tag.Baseline ?? textHeight * 0.75, textHeight, bottomSpace,
								tagSpan.Tag.Affinity ?? PositionAffinity.Predecessor, adornmentInfo, providerTag);
					adornmentInfo.Tag = tag;
					yield return new TagSpan<SpaceNegotiatingAdornmentTag>(fullSpan, tag);
				}
			}
		}

		double Filter(double value) => value < 0 || value == double.PositiveInfinity || double.IsNaN(value) ? 0 : value;

		void TagAggregator_TagsChanged(object sender, TagsChangedEventArgs e) {
			if (wpfTextView.IsClosed)
				return;
			foreach (var span in e.Span.GetSpans(wpfTextView.TextBuffer.CurrentSnapshot))
				tagger?.RefreshSpans(new SnapshotSpanEventArgs(span));
		}

		void WpfTextView_Closed(object sender, EventArgs e) {
			wpfTextView.Closed -= WpfTextView_Closed;
			wpfTextView.LayoutChanged -= WpfTextView_LayoutChanged;
			wpfTextView.Selection.SelectionChanged -= Selection_SelectionChanged;
			tagAggregator.TagsChanged -= TagAggregator_TagsChanged;
			tagAggregator.Dispose();
			adornmentTagInfos.Clear();
			layer?.RemoveAllAdornments();
		}
	}
}
