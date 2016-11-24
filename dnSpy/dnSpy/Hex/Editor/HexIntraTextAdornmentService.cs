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
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Formatting;
using dnSpy.Contracts.Hex.Tagging;
using VST = Microsoft.VisualStudio.Text;
using VSTE = Microsoft.VisualStudio.Text.Editor;
using VSTF = Microsoft.VisualStudio.Text.Formatting;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Editor {
	[Export(typeof(HexViewTaggerProvider))]
	[HexTagType(typeof(HexSpaceNegotiatingAdornmentTag))]
	sealed class IntraTextAdornmentServiceSpaceNegotiatingAdornmentTaggerProvider : HexViewTaggerProvider {
		readonly HexIntraTextAdornmentServiceProvider intraTextAdornmentServiceProvider;

		[ImportingConstructor]
		IntraTextAdornmentServiceSpaceNegotiatingAdornmentTaggerProvider(HexIntraTextAdornmentServiceProvider intraTextAdornmentServiceProvider) {
			this.intraTextAdornmentServiceProvider = intraTextAdornmentServiceProvider;
		}

		public override HexTagger<T> CreateTagger<T>(HexView hexView, HexBuffer buffer) {
			if (hexView.Buffer != buffer)
				return null;
			var wpfHexView = hexView as WpfHexView;
			Debug.Assert(wpfHexView != null);
			if (wpfHexView == null)
				return null;
			return wpfHexView.Properties.GetOrCreateSingletonProperty(
				typeof(IntraTextAdornmentServiceSpaceNegotiatingAdornmentTagger),
				() => new IntraTextAdornmentServiceSpaceNegotiatingAdornmentTagger(intraTextAdornmentServiceProvider.Get(wpfHexView))) as HexTagger<T>;
		}
	}

	interface IIntraTextAdornmentServiceSpaceNegotiatingAdornmentTagger {
		void RefreshSpans(HexBufferSpanEventArgs e);
	}

	sealed class IntraTextAdornmentServiceSpaceNegotiatingAdornmentTagger : HexTagger<HexSpaceNegotiatingAdornmentTag>, IIntraTextAdornmentServiceSpaceNegotiatingAdornmentTagger {
		public override event EventHandler<HexBufferSpanEventArgs> TagsChanged;

		readonly HexIntraTextAdornmentService intraTextAdornmentService;

		public IntraTextAdornmentServiceSpaceNegotiatingAdornmentTagger(HexIntraTextAdornmentService intraTextAdornmentService) {
			if (intraTextAdornmentService == null)
				throw new ArgumentNullException(nameof(intraTextAdornmentService));
			this.intraTextAdornmentService = intraTextAdornmentService;
			intraTextAdornmentService.RegisterTagger(this);
		}

		public override IEnumerable<HexTagSpan<HexSpaceNegotiatingAdornmentTag>> GetTags(NormalizedHexBufferSpanCollection spans) =>
			intraTextAdornmentService.GetTags(spans);

		public override IEnumerable<HexTextTagSpan<HexSpaceNegotiatingAdornmentTag>> GetTags(HexTaggerContext context) =>
			intraTextAdornmentService.GetLineTags(context);

		public void RefreshSpans(HexBufferSpanEventArgs e) => TagsChanged?.Invoke(this, e);
	}

	abstract class HexIntraTextAdornmentServiceProvider {
		public abstract HexIntraTextAdornmentService Get(WpfHexView wpfHexView);
	}

	[Export(typeof(HexIntraTextAdornmentServiceProvider))]
	sealed class HexIntraTextAdornmentServiceProviderImpl : HexIntraTextAdornmentServiceProvider {
		readonly HexViewTagAggregatorFactoryService viewTagAggregatorFactoryService;

		[ImportingConstructor]
		HexIntraTextAdornmentServiceProviderImpl(HexViewTagAggregatorFactoryService viewTagAggregatorFactoryService) {
			this.viewTagAggregatorFactoryService = viewTagAggregatorFactoryService;
		}

		public override HexIntraTextAdornmentService Get(WpfHexView wpfHexView) {
			if (wpfHexView == null)
				throw new ArgumentNullException(nameof(wpfHexView));
			return wpfHexView.Properties.GetOrCreateSingletonProperty(typeof(HexIntraTextAdornmentServiceImpl), () => new HexIntraTextAdornmentServiceImpl(wpfHexView, viewTagAggregatorFactoryService));
		}
	}

	abstract class HexIntraTextAdornmentService {
		public abstract IEnumerable<HexTagSpan<HexSpaceNegotiatingAdornmentTag>> GetTags(NormalizedHexBufferSpanCollection spans);
		public abstract IEnumerable<HexTextTagSpan<HexSpaceNegotiatingAdornmentTag>> GetLineTags(HexTaggerContext context);
		public abstract void RegisterTagger(IIntraTextAdornmentServiceSpaceNegotiatingAdornmentTagger tagger);
	}

	sealed class HexIntraTextAdornmentServiceImpl : HexIntraTextAdornmentService {
#pragma warning disable 0169
		[Export(typeof(HexAdornmentLayerDefinition))]
		[VSUTIL.Name(PredefinedHexAdornmentLayers.IntraTextAdornment)]
		[VSUTIL.Order(After = PredefinedHexAdornmentLayers.BottomLayer, Before = PredefinedHexAdornmentLayers.TopLayer)]
		[VSUTIL.Order(After = PredefinedHexAdornmentLayers.Text)]
		static HexAdornmentLayerDefinition adornmentLayer;
#pragma warning restore 0169

		readonly WpfHexView wpfHexView;
		readonly HexTagAggregator<HexIntraTextAdornmentTag> tagAggregator;
		readonly List<AdornmentTagInfo> adornmentTagInfos;
		readonly HashSet<object> currentLineIdentityTags;
		HexAdornmentLayer layer;
		IIntraTextAdornmentServiceSpaceNegotiatingAdornmentTagger tagger;
		static readonly object providerTag = new object();

		public HexIntraTextAdornmentServiceImpl(WpfHexView wpfHexView, HexViewTagAggregatorFactoryService viewTagAggregatorFactoryService) {
			if (wpfHexView == null)
				throw new ArgumentNullException(nameof(wpfHexView));
			if (viewTagAggregatorFactoryService == null)
				throw new ArgumentNullException(nameof(viewTagAggregatorFactoryService));
			adornmentTagInfos = new List<AdornmentTagInfo>();
			currentLineIdentityTags = new HashSet<object>();
			this.wpfHexView = wpfHexView;
			tagAggregator = viewTagAggregatorFactoryService.CreateTagAggregator<HexIntraTextAdornmentTag>(wpfHexView);
			tagAggregator.TagsChanged += TagAggregator_TagsChanged;
			wpfHexView.Closed += WpfHexView_Closed;
			wpfHexView.LayoutChanged += WpfHexView_LayoutChanged;
		}

		void Selection_SelectionChanged(object sender, EventArgs e) => UpdateIsSelected();

		void UpdateIsSelected() {
			if (adornmentTagInfos.Count == 0)
				return;
			if (wpfHexView.Selection.IsEmpty) {
				foreach (var info in adornmentTagInfos)
					VSTE.IntraTextAdornment.SetIsSelected(info.UserUIElement, false);
			}
			else {
				foreach (var info in adornmentTagInfos)
					UpdateIsSelected(info, null);
			}
		}

		void UpdateIsSelected(AdornmentTagInfo adornmentInfo, HexViewLine line) {
			if (line == null)
				line = wpfHexView.HexViewLines.GetHexViewLineContainingBufferPosition(adornmentInfo.BufferSpan.Start);
			bool selected = IsSelected(adornmentInfo, line);
			VSTE.IntraTextAdornment.SetIsSelected(adornmentInfo.UserUIElement, selected);
		}

		bool IsSelected(AdornmentTagInfo adornmentInfo, HexViewLine line) {
			if (line == null)
				return false;
			if (wpfHexView.Selection.IsEmpty)
				return false;
			if (adornmentInfo.HexTagSpan != null) {
				foreach (var span in wpfHexView.Selection.SelectedSpans) {
					if (span.Contains(adornmentInfo.BufferSpan))
						return true;
				}
			}
			else {
				foreach (var span in wpfHexView.Selection.GetSelectionOnHexViewLine(line)) {
					if (span.Contains(adornmentInfo.HexTextTagSpan.Value.Span))
						return true;
				}
			}
			return false;
		}

		sealed class AdornmentTagInfo {
			public HexSpaceNegotiatingAdornmentTag Tag;
			public readonly UIElement UserUIElement;
			public ZoomingUIElement TopUIElement;
			public object LineIdentityTag;

			// The full buffer line span if HexTextTagSpan != null, else it's an accurate span
			public readonly HexBufferSpan BufferSpan;
			// Mutually exclusive with HexTextTagSpan
			public readonly HexTagSpan<HexIntraTextAdornmentTag>? HexTagSpan;
			public readonly HexTextTagSpan<HexIntraTextAdornmentTag>? HexTextTagSpan;

			public AdornmentTagInfo(HexBufferSpan span, UIElement element, HexTagSpan<HexIntraTextAdornmentTag> tagSpan) {
				BufferSpan = span;
				UserUIElement = element;
				HexTagSpan = tagSpan;
			}

			public AdornmentTagInfo(HexBufferSpan span, UIElement element, HexTextTagSpan<HexIntraTextAdornmentTag> textTagSpan) {
				BufferSpan = span;
				UserUIElement = element;
				HexTextTagSpan = textTagSpan;
			}

			public void Initialize() {
				if (TopUIElement == null)
					TopUIElement = new ZoomingUIElement(UserUIElement);
				TopUIElement.Initialize();
				TopUIElement.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			}
		}

		void WpfHexView_LayoutChanged(object sender, HexViewLayoutChangedEventArgs e) {
			if (adornmentTagInfos.Count > 0) {
				currentLineIdentityTags.Clear();
				foreach (var line in wpfHexView.HexViewLines)
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
						layer = wpfHexView.GetAdornmentLayer(PredefinedHexAdornmentLayers.IntraTextAdornment);
						// Can't do this in the ctor since Selection hasn't been initialized yet
						wpfHexView.Selection.SelectionChanged += Selection_SelectionChanged;
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

		void UpdateAdornmentUIState(HexViewLine line, AdornmentTagInfo adornmentInfo, VSTF.TextBounds bounds) {
			double verticalScale = line.LineTransform.VerticalScale;
			adornmentInfo.TopUIElement.SetScale(verticalScale);
			Canvas.SetTop(adornmentInfo.TopUIElement, bounds.TextTop + line.Baseline - verticalScale * adornmentInfo.Tag.Baseline);
			Canvas.SetLeft(adornmentInfo.TopUIElement, bounds.Left);
		}

		bool AddAdornment(AdornmentTagInfo adornmentInfo, HexViewLine line) {
			SizeChangedEventHandler sizeChanged = (a, e) => {
				var bounds = line.GetAdornmentBounds(adornmentInfo);
				if (bounds == null)
					return;
				// Sometimes the size just gets changed very little, eg. from 400 to 399.95.....
				const double d = 0.5;
				if (e.NewSize.Height <= bounds.Value.Height + d && e.NewSize.Width <= bounds.Value.Width + d)
					return;
				tagger?.RefreshSpans(new HexBufferSpanEventArgs(adornmentInfo.BufferSpan));
			};
			adornmentInfo.TopUIElement.SizeChanged += sizeChanged;

			VSTE.AdornmentRemovedCallback removedCallback = (a, b) => {
				adornmentTagInfos.Remove(adornmentInfo);
				adornmentInfo.TopUIElement.SizeChanged -= sizeChanged;
				adornmentInfo.TopUIElement.OnRemoved();
				if (adornmentInfo.HexTagSpan != null)
					adornmentInfo.HexTagSpan.Value.Tag.RemovalCallback?.Invoke(adornmentInfo.HexTagSpan, b);
				else
					adornmentInfo.HexTextTagSpan.Value.Tag.RemovalCallback?.Invoke(adornmentInfo.HexTextTagSpan, b);
			};

			Debug.Assert(!adornmentTagInfos.Contains(adornmentInfo));
			adornmentTagInfos.Add(adornmentInfo);
			// Use OwnerControlled because there are corner cases that the adornment layer can't handle,
			// eg. an adornment with buffer span length == 0 that is shown on its own line (word wrap).
			bool added = layer.AddAdornment(VSTE.AdornmentPositioningBehavior.OwnerControlled, (HexBufferSpan?)null, adornmentInfo, adornmentInfo.TopUIElement, removedCallback);
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

		public override void RegisterTagger(IIntraTextAdornmentServiceSpaceNegotiatingAdornmentTagger tagger) {
			if (this.tagger != null)
				throw new InvalidOperationException();
			if (tagger == null)
				throw new ArgumentNullException(nameof(tagger));
			this.tagger = tagger;
		}

		public override IEnumerable<HexTagSpan<HexSpaceNegotiatingAdornmentTag>> GetTags(NormalizedHexBufferSpanCollection spans) {
			if (wpfHexView.IsClosed)
				yield break;

			foreach (var span in spans) {
				foreach (var tagSpan in tagAggregator.GetTags(span)) {
					var uiElem = tagSpan.Tag.Adornment;
					double topSpace = tagSpan.Tag.TopSpace ?? 0;
					double bottomSpace = tagSpan.Tag.BottomSpace ?? 0;
					double textHeight = tagSpan.Tag.TextHeight ?? (Filter(uiElem.DesiredSize.Height) - (topSpace + bottomSpace));
					var adornmentInfo = new AdornmentTagInfo(tagSpan.Span, uiElem, tagSpan);
					var tag = new HexSpaceNegotiatingAdornmentTag(Filter(uiElem.DesiredSize.Width), topSpace,
								tagSpan.Tag.Baseline ?? textHeight * 0.75, textHeight, bottomSpace,
								tagSpan.Tag.Affinity ?? VST.PositionAffinity.Predecessor, adornmentInfo, providerTag);
					adornmentInfo.Tag = tag;
					yield return new HexTagSpan<HexSpaceNegotiatingAdornmentTag>(tagSpan.Span, tagSpan.Flags, tag);
				}
			}
		}

		public override IEnumerable<HexTextTagSpan<HexSpaceNegotiatingAdornmentTag>> GetLineTags(HexTaggerContext context) {
			if (wpfHexView.IsClosed)
				yield break;

			var taggerContext = new HexTaggerContext(context.Line, context.LineSpan);
			foreach (var tagSpan in tagAggregator.GetLineTags(taggerContext)) {
				var uiElem = tagSpan.Tag.Adornment;
				double topSpace = tagSpan.Tag.TopSpace ?? 0;
				double bottomSpace = tagSpan.Tag.BottomSpace ?? 0;
				double textHeight = tagSpan.Tag.TextHeight ?? (Filter(uiElem.DesiredSize.Height) - (topSpace + bottomSpace));
				var adornmentInfo = new AdornmentTagInfo(context.Line.BufferSpan, uiElem, tagSpan);
				var tag = new HexSpaceNegotiatingAdornmentTag(Filter(uiElem.DesiredSize.Width), topSpace,
							tagSpan.Tag.Baseline ?? textHeight * 0.75, textHeight, bottomSpace,
							tagSpan.Tag.Affinity ?? VST.PositionAffinity.Predecessor, adornmentInfo, providerTag);
				adornmentInfo.Tag = tag;
				yield return new HexTextTagSpan<HexSpaceNegotiatingAdornmentTag>(tagSpan.Span, tag);
			}
		}

		double Filter(double value) => value < 0 || value == double.PositiveInfinity || double.IsNaN(value) ? 0 : value;

		void TagAggregator_TagsChanged(object sender, HexTagsChangedEventArgs e) {
			if (wpfHexView.IsClosed)
				return;
			tagger?.RefreshSpans(new HexBufferSpanEventArgs(e.Span));
		}

		void WpfHexView_Closed(object sender, EventArgs e) {
			wpfHexView.Closed -= WpfHexView_Closed;
			wpfHexView.LayoutChanged -= WpfHexView_LayoutChanged;
			wpfHexView.Selection.SelectionChanged -= Selection_SelectionChanged;
			tagAggregator.TagsChanged -= TagAggregator_TagsChanged;
			tagAggregator.Dispose();
			adornmentTagInfos.Clear();
			layer?.RemoveAllAdornments();
		}
	}
}
