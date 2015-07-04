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

using System;
using System.Collections.Generic;
using dnlib.DotNet;

namespace dnSpy.BamlDecompiler.Baml {
	internal class KnownThingsv4 : IKnownThings {
		readonly Dictionary<int, AssemblyDef> assemblies;
		readonly IAssemblyResolver resolver;
		readonly Dictionary<KnownProperties, KnownProperty> properties;
		readonly Dictionary<KnownTypes, TypeDef> types;
		readonly Dictionary<int, string> strings;

		public KnownThingsv4(ModuleDef module) {
			resolver = module.Context.AssemblyResolver;

			assemblies = new Dictionary<int, AssemblyDef>();
			types = new Dictionary<KnownTypes, TypeDef>();
			properties = new Dictionary<KnownProperties, KnownProperty>();
			strings = new Dictionary<int, string>();

			InitAssemblies(module);
			InitTypes();
			InitProperties();
			InitStrings();
		}

		public Func<KnownTypes, TypeDef> Types {
			get { return type => types[type]; }
		}

		public Func<KnownProperties, KnownProperty> Properties {
			get { return property => properties[property]; }
		}

		public Func<int, String> Strings {
			get { return str => strings[str]; }
		}

		public AssemblyDef FrameworkAssembly {
			get { return assemblies[0]; }
		}

		KnownProperty InitProperty(KnownTypes parent, string propertyName, TypeDef propertyType) {
			return new KnownProperty(types[parent], propertyName, propertyType);
		}

		// Following codes are auto-generated, do not modify.

		void InitAssemblies(ModuleDef module) {
			assemblies[0] =
				resolver.ResolveThrow(
					"PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35", module);
			assemblies[1] =
				resolver.ResolveThrow(
					"PresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35", module);
			assemblies[2] =
				resolver.ResolveThrow("System.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
					module);
			assemblies[3] =
				resolver.ResolveThrow("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", module);
			assemblies[4] =
				resolver.ResolveThrow("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", module);
			assemblies[5] =
				resolver.ResolveThrow("WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
					module);
			assemblies[6] =
				resolver.ResolveThrow("System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
					module);
		}

		void InitTypes() {
			types[KnownTypes.AccessText] = assemblies[0].Find("System.Windows.Controls.AccessText", true);
			types[KnownTypes.AdornedElementPlaceholder] = assemblies[0].Find(
				"System.Windows.Controls.AdornedElementPlaceholder", true);
			types[KnownTypes.Adorner] = assemblies[0].Find("System.Windows.Documents.Adorner", true);
			types[KnownTypes.AdornerDecorator] = assemblies[0].Find("System.Windows.Documents.AdornerDecorator", true);
			types[KnownTypes.AdornerLayer] = assemblies[0].Find("System.Windows.Documents.AdornerLayer", true);
			types[KnownTypes.AffineTransform3D] = assemblies[1].Find("System.Windows.Media.Media3D.AffineTransform3D", true);
			types[KnownTypes.AmbientLight] = assemblies[1].Find("System.Windows.Media.Media3D.AmbientLight", true);
			types[KnownTypes.AnchoredBlock] = assemblies[0].Find("System.Windows.Documents.AnchoredBlock", true);
			types[KnownTypes.Animatable] = assemblies[1].Find("System.Windows.Media.Animation.Animatable", true);
			types[KnownTypes.AnimationClock] = assemblies[1].Find("System.Windows.Media.Animation.AnimationClock", true);
			types[KnownTypes.AnimationTimeline] = assemblies[1].Find("System.Windows.Media.Animation.AnimationTimeline", true);
			types[KnownTypes.Application] = assemblies[0].Find("System.Windows.Application", true);
			types[KnownTypes.ArcSegment] = assemblies[1].Find("System.Windows.Media.ArcSegment", true);
			types[KnownTypes.ArrayExtension] = assemblies[2].Find("System.Windows.Markup.ArrayExtension", true);
			types[KnownTypes.AxisAngleRotation3D] = assemblies[1].Find("System.Windows.Media.Media3D.AxisAngleRotation3D", true);
			types[KnownTypes.BaseIListConverter] = assemblies[1].Find("System.Windows.Media.Converters.BaseIListConverter", true);
			types[KnownTypes.BeginStoryboard] = assemblies[0].Find("System.Windows.Media.Animation.BeginStoryboard", true);
			types[KnownTypes.BevelBitmapEffect] = assemblies[1].Find("System.Windows.Media.Effects.BevelBitmapEffect", true);
			types[KnownTypes.BezierSegment] = assemblies[1].Find("System.Windows.Media.BezierSegment", true);
			types[KnownTypes.Binding] = assemblies[0].Find("System.Windows.Data.Binding", true);
			types[KnownTypes.BindingBase] = assemblies[0].Find("System.Windows.Data.BindingBase", true);
			types[KnownTypes.BindingExpression] = assemblies[0].Find("System.Windows.Data.BindingExpression", true);
			types[KnownTypes.BindingExpressionBase] = assemblies[0].Find("System.Windows.Data.BindingExpressionBase", true);
			types[KnownTypes.BindingListCollectionView] = assemblies[0].Find("System.Windows.Data.BindingListCollectionView",
				true);
			types[KnownTypes.BitmapDecoder] = assemblies[1].Find("System.Windows.Media.Imaging.BitmapDecoder", true);
			types[KnownTypes.BitmapEffect] = assemblies[1].Find("System.Windows.Media.Effects.BitmapEffect", true);
			types[KnownTypes.BitmapEffectCollection] = assemblies[1].Find("System.Windows.Media.Effects.BitmapEffectCollection",
				true);
			types[KnownTypes.BitmapEffectGroup] = assemblies[1].Find("System.Windows.Media.Effects.BitmapEffectGroup", true);
			types[KnownTypes.BitmapEffectInput] = assemblies[1].Find("System.Windows.Media.Effects.BitmapEffectInput", true);
			types[KnownTypes.BitmapEncoder] = assemblies[1].Find("System.Windows.Media.Imaging.BitmapEncoder", true);
			types[KnownTypes.BitmapFrame] = assemblies[1].Find("System.Windows.Media.Imaging.BitmapFrame", true);
			types[KnownTypes.BitmapImage] = assemblies[1].Find("System.Windows.Media.Imaging.BitmapImage", true);
			types[KnownTypes.BitmapMetadata] = assemblies[1].Find("System.Windows.Media.Imaging.BitmapMetadata", true);
			types[KnownTypes.BitmapPalette] = assemblies[1].Find("System.Windows.Media.Imaging.BitmapPalette", true);
			types[KnownTypes.BitmapSource] = assemblies[1].Find("System.Windows.Media.Imaging.BitmapSource", true);
			types[KnownTypes.Block] = assemblies[0].Find("System.Windows.Documents.Block", true);
			types[KnownTypes.BlockUIContainer] = assemblies[0].Find("System.Windows.Documents.BlockUIContainer", true);
			types[KnownTypes.BlurBitmapEffect] = assemblies[1].Find("System.Windows.Media.Effects.BlurBitmapEffect", true);
			types[KnownTypes.BmpBitmapDecoder] = assemblies[1].Find("System.Windows.Media.Imaging.BmpBitmapDecoder", true);
			types[KnownTypes.BmpBitmapEncoder] = assemblies[1].Find("System.Windows.Media.Imaging.BmpBitmapEncoder", true);
			types[KnownTypes.Bold] = assemblies[0].Find("System.Windows.Documents.Bold", true);
			types[KnownTypes.BoolIListConverter] = assemblies[1].Find("System.Windows.Media.Converters.BoolIListConverter", true);
			types[KnownTypes.Boolean] = assemblies[3].Find("System.Boolean", true);
			types[KnownTypes.BooleanAnimationBase] = assemblies[1].Find("System.Windows.Media.Animation.BooleanAnimationBase",
				true);
			types[KnownTypes.BooleanAnimationUsingKeyFrames] =
				assemblies[1].Find("System.Windows.Media.Animation.BooleanAnimationUsingKeyFrames", true);
			types[KnownTypes.BooleanConverter] = assemblies[4].Find("System.ComponentModel.BooleanConverter", true);
			types[KnownTypes.BooleanKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.BooleanKeyFrame", true);
			types[KnownTypes.BooleanKeyFrameCollection] =
				assemblies[1].Find("System.Windows.Media.Animation.BooleanKeyFrameCollection", true);
			types[KnownTypes.BooleanToVisibilityConverter] =
				assemblies[0].Find("System.Windows.Controls.BooleanToVisibilityConverter", true);
			types[KnownTypes.Border] = assemblies[0].Find("System.Windows.Controls.Border", true);
			types[KnownTypes.BorderGapMaskConverter] = assemblies[0].Find("System.Windows.Controls.BorderGapMaskConverter", true);
			types[KnownTypes.Brush] = assemblies[1].Find("System.Windows.Media.Brush", true);
			types[KnownTypes.BrushConverter] = assemblies[1].Find("System.Windows.Media.BrushConverter", true);
			types[KnownTypes.BulletDecorator] = assemblies[0].Find("System.Windows.Controls.Primitives.BulletDecorator", true);
			types[KnownTypes.Button] = assemblies[0].Find("System.Windows.Controls.Button", true);
			types[KnownTypes.ButtonBase] = assemblies[0].Find("System.Windows.Controls.Primitives.ButtonBase", true);
			types[KnownTypes.Byte] = assemblies[3].Find("System.Byte", true);
			types[KnownTypes.ByteAnimation] = assemblies[1].Find("System.Windows.Media.Animation.ByteAnimation", true);
			types[KnownTypes.ByteAnimationBase] = assemblies[1].Find("System.Windows.Media.Animation.ByteAnimationBase", true);
			types[KnownTypes.ByteAnimationUsingKeyFrames] =
				assemblies[1].Find("System.Windows.Media.Animation.ByteAnimationUsingKeyFrames", true);
			types[KnownTypes.ByteConverter] = assemblies[4].Find("System.ComponentModel.ByteConverter", true);
			types[KnownTypes.ByteKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.ByteKeyFrame", true);
			types[KnownTypes.ByteKeyFrameCollection] = assemblies[1].Find(
				"System.Windows.Media.Animation.ByteKeyFrameCollection", true);
			types[KnownTypes.CachedBitmap] = assemblies[1].Find("System.Windows.Media.Imaging.CachedBitmap", true);
			types[KnownTypes.Camera] = assemblies[1].Find("System.Windows.Media.Media3D.Camera", true);
			types[KnownTypes.Canvas] = assemblies[0].Find("System.Windows.Controls.Canvas", true);
			types[KnownTypes.Char] = assemblies[3].Find("System.Char", true);
			types[KnownTypes.CharAnimationBase] = assemblies[1].Find("System.Windows.Media.Animation.CharAnimationBase", true);
			types[KnownTypes.CharAnimationUsingKeyFrames] =
				assemblies[1].Find("System.Windows.Media.Animation.CharAnimationUsingKeyFrames", true);
			types[KnownTypes.CharConverter] = assemblies[4].Find("System.ComponentModel.CharConverter", true);
			types[KnownTypes.CharIListConverter] = assemblies[1].Find("System.Windows.Media.Converters.CharIListConverter", true);
			types[KnownTypes.CharKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.CharKeyFrame", true);
			types[KnownTypes.CharKeyFrameCollection] = assemblies[1].Find(
				"System.Windows.Media.Animation.CharKeyFrameCollection", true);
			types[KnownTypes.CheckBox] = assemblies[0].Find("System.Windows.Controls.CheckBox", true);
			types[KnownTypes.Clock] = assemblies[1].Find("System.Windows.Media.Animation.Clock", true);
			types[KnownTypes.ClockController] = assemblies[1].Find("System.Windows.Media.Animation.ClockController", true);
			types[KnownTypes.ClockGroup] = assemblies[1].Find("System.Windows.Media.Animation.ClockGroup", true);
			types[KnownTypes.CollectionContainer] = assemblies[0].Find("System.Windows.Data.CollectionContainer", true);
			types[KnownTypes.CollectionView] = assemblies[0].Find("System.Windows.Data.CollectionView", true);
			types[KnownTypes.CollectionViewSource] = assemblies[0].Find("System.Windows.Data.CollectionViewSource", true);
			types[KnownTypes.Color] = assemblies[1].Find("System.Windows.Media.Color", true);
			types[KnownTypes.ColorAnimation] = assemblies[1].Find("System.Windows.Media.Animation.ColorAnimation", true);
			types[KnownTypes.ColorAnimationBase] = assemblies[1].Find("System.Windows.Media.Animation.ColorAnimationBase", true);
			types[KnownTypes.ColorAnimationUsingKeyFrames] =
				assemblies[1].Find("System.Windows.Media.Animation.ColorAnimationUsingKeyFrames", true);
			types[KnownTypes.ColorConvertedBitmap] = assemblies[1].Find("System.Windows.Media.Imaging.ColorConvertedBitmap", true);
			types[KnownTypes.ColorConvertedBitmapExtension] = assemblies[0].Find("System.Windows.ColorConvertedBitmapExtension",
				true);
			types[KnownTypes.ColorConverter] = assemblies[1].Find("System.Windows.Media.ColorConverter", true);
			types[KnownTypes.ColorKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.ColorKeyFrame", true);
			types[KnownTypes.ColorKeyFrameCollection] =
				assemblies[1].Find("System.Windows.Media.Animation.ColorKeyFrameCollection", true);
			types[KnownTypes.ColumnDefinition] = assemblies[0].Find("System.Windows.Controls.ColumnDefinition", true);
			types[KnownTypes.CombinedGeometry] = assemblies[1].Find("System.Windows.Media.CombinedGeometry", true);
			types[KnownTypes.ComboBox] = assemblies[0].Find("System.Windows.Controls.ComboBox", true);
			types[KnownTypes.ComboBoxItem] = assemblies[0].Find("System.Windows.Controls.ComboBoxItem", true);
			types[KnownTypes.CommandConverter] = assemblies[0].Find("System.Windows.Input.CommandConverter", true);
			types[KnownTypes.ComponentResourceKey] = assemblies[0].Find("System.Windows.ComponentResourceKey", true);
			types[KnownTypes.ComponentResourceKeyConverter] =
				assemblies[0].Find("System.Windows.Markup.ComponentResourceKeyConverter", true);
			types[KnownTypes.CompositionTarget] = assemblies[1].Find("System.Windows.Media.CompositionTarget", true);
			types[KnownTypes.Condition] = assemblies[0].Find("System.Windows.Condition", true);
			types[KnownTypes.ContainerVisual] = assemblies[1].Find("System.Windows.Media.ContainerVisual", true);
			types[KnownTypes.ContentControl] = assemblies[0].Find("System.Windows.Controls.ContentControl", true);
			types[KnownTypes.ContentElement] = assemblies[1].Find("System.Windows.ContentElement", true);
			types[KnownTypes.ContentPresenter] = assemblies[0].Find("System.Windows.Controls.ContentPresenter", true);
			types[KnownTypes.ContentPropertyAttribute] = assemblies[2].Find("System.Windows.Markup.ContentPropertyAttribute",
				true);
			types[KnownTypes.ContentWrapperAttribute] = assemblies[2].Find("System.Windows.Markup.ContentWrapperAttribute", true);
			types[KnownTypes.ContextMenu] = assemblies[0].Find("System.Windows.Controls.ContextMenu", true);
			types[KnownTypes.ContextMenuService] = assemblies[0].Find("System.Windows.Controls.ContextMenuService", true);
			types[KnownTypes.Control] = assemblies[0].Find("System.Windows.Controls.Control", true);
			types[KnownTypes.ControlTemplate] = assemblies[0].Find("System.Windows.Controls.ControlTemplate", true);
			types[KnownTypes.ControllableStoryboardAction] =
				assemblies[0].Find("System.Windows.Media.Animation.ControllableStoryboardAction", true);
			types[KnownTypes.CornerRadius] = assemblies[0].Find("System.Windows.CornerRadius", true);
			types[KnownTypes.CornerRadiusConverter] = assemblies[0].Find("System.Windows.CornerRadiusConverter", true);
			types[KnownTypes.CroppedBitmap] = assemblies[1].Find("System.Windows.Media.Imaging.CroppedBitmap", true);
			types[KnownTypes.CultureInfo] = assemblies[3].Find("System.Globalization.CultureInfo", true);
			types[KnownTypes.CultureInfoConverter] = assemblies[4].Find("System.ComponentModel.CultureInfoConverter", true);
			types[KnownTypes.CultureInfoIetfLanguageTagConverter] =
				assemblies[1].Find("System.Windows.CultureInfoIetfLanguageTagConverter", true);
			types[KnownTypes.Cursor] = assemblies[1].Find("System.Windows.Input.Cursor", true);
			types[KnownTypes.CursorConverter] = assemblies[1].Find("System.Windows.Input.CursorConverter", true);
			types[KnownTypes.DashStyle] = assemblies[1].Find("System.Windows.Media.DashStyle", true);
			types[KnownTypes.DataChangedEventManager] = assemblies[0].Find("System.Windows.Data.DataChangedEventManager", true);
			types[KnownTypes.DataTemplate] = assemblies[0].Find("System.Windows.DataTemplate", true);
			types[KnownTypes.DataTemplateKey] = assemblies[0].Find("System.Windows.DataTemplateKey", true);
			types[KnownTypes.DataTrigger] = assemblies[0].Find("System.Windows.DataTrigger", true);
			types[KnownTypes.DateTime] = assemblies[3].Find("System.DateTime", true);
			types[KnownTypes.DateTimeConverter] = assemblies[4].Find("System.ComponentModel.DateTimeConverter", true);
			types[KnownTypes.DateTimeConverter2] = assemblies[5].Find("System.Windows.Markup.DateTimeConverter2", true);
			types[KnownTypes.Decimal] = assemblies[3].Find("System.Decimal", true);
			types[KnownTypes.DecimalAnimation] = assemblies[1].Find("System.Windows.Media.Animation.DecimalAnimation", true);
			types[KnownTypes.DecimalAnimationBase] = assemblies[1].Find("System.Windows.Media.Animation.DecimalAnimationBase",
				true);
			types[KnownTypes.DecimalAnimationUsingKeyFrames] =
				assemblies[1].Find("System.Windows.Media.Animation.DecimalAnimationUsingKeyFrames", true);
			types[KnownTypes.DecimalConverter] = assemblies[4].Find("System.ComponentModel.DecimalConverter", true);
			types[KnownTypes.DecimalKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.DecimalKeyFrame", true);
			types[KnownTypes.DecimalKeyFrameCollection] =
				assemblies[1].Find("System.Windows.Media.Animation.DecimalKeyFrameCollection", true);
			types[KnownTypes.Decorator] = assemblies[0].Find("System.Windows.Controls.Decorator", true);
			types[KnownTypes.DefinitionBase] = assemblies[0].Find("System.Windows.Controls.DefinitionBase", true);
			types[KnownTypes.DependencyObject] = assemblies[5].Find("System.Windows.DependencyObject", true);
			types[KnownTypes.DependencyProperty] = assemblies[5].Find("System.Windows.DependencyProperty", true);
			types[KnownTypes.DependencyPropertyConverter] =
				assemblies[0].Find("System.Windows.Markup.DependencyPropertyConverter", true);
			types[KnownTypes.DialogResultConverter] = assemblies[0].Find("System.Windows.DialogResultConverter", true);
			types[KnownTypes.DiffuseMaterial] = assemblies[1].Find("System.Windows.Media.Media3D.DiffuseMaterial", true);
			types[KnownTypes.DirectionalLight] = assemblies[1].Find("System.Windows.Media.Media3D.DirectionalLight", true);
			types[KnownTypes.DiscreteBooleanKeyFrame] =
				assemblies[1].Find("System.Windows.Media.Animation.DiscreteBooleanKeyFrame", true);
			types[KnownTypes.DiscreteByteKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.DiscreteByteKeyFrame",
				true);
			types[KnownTypes.DiscreteCharKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.DiscreteCharKeyFrame",
				true);
			types[KnownTypes.DiscreteColorKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.DiscreteColorKeyFrame",
				true);
			types[KnownTypes.DiscreteDecimalKeyFrame] =
				assemblies[1].Find("System.Windows.Media.Animation.DiscreteDecimalKeyFrame", true);
			types[KnownTypes.DiscreteDoubleKeyFrame] = assemblies[1].Find(
				"System.Windows.Media.Animation.DiscreteDoubleKeyFrame", true);
			types[KnownTypes.DiscreteInt16KeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.DiscreteInt16KeyFrame",
				true);
			types[KnownTypes.DiscreteInt32KeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.DiscreteInt32KeyFrame",
				true);
			types[KnownTypes.DiscreteInt64KeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.DiscreteInt64KeyFrame",
				true);
			types[KnownTypes.DiscreteMatrixKeyFrame] = assemblies[1].Find(
				"System.Windows.Media.Animation.DiscreteMatrixKeyFrame", true);
			types[KnownTypes.DiscreteObjectKeyFrame] = assemblies[1].Find(
				"System.Windows.Media.Animation.DiscreteObjectKeyFrame", true);
			types[KnownTypes.DiscretePoint3DKeyFrame] =
				assemblies[1].Find("System.Windows.Media.Animation.DiscretePoint3DKeyFrame", true);
			types[KnownTypes.DiscretePointKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.DiscretePointKeyFrame",
				true);
			types[KnownTypes.DiscreteQuaternionKeyFrame] =
				assemblies[1].Find("System.Windows.Media.Animation.DiscreteQuaternionKeyFrame", true);
			types[KnownTypes.DiscreteRectKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.DiscreteRectKeyFrame",
				true);
			types[KnownTypes.DiscreteRotation3DKeyFrame] =
				assemblies[1].Find("System.Windows.Media.Animation.DiscreteRotation3DKeyFrame", true);
			types[KnownTypes.DiscreteSingleKeyFrame] = assemblies[1].Find(
				"System.Windows.Media.Animation.DiscreteSingleKeyFrame", true);
			types[KnownTypes.DiscreteSizeKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.DiscreteSizeKeyFrame",
				true);
			types[KnownTypes.DiscreteStringKeyFrame] = assemblies[1].Find(
				"System.Windows.Media.Animation.DiscreteStringKeyFrame", true);
			types[KnownTypes.DiscreteThicknessKeyFrame] =
				assemblies[0].Find("System.Windows.Media.Animation.DiscreteThicknessKeyFrame", true);
			types[KnownTypes.DiscreteVector3DKeyFrame] =
				assemblies[1].Find("System.Windows.Media.Animation.DiscreteVector3DKeyFrame", true);
			types[KnownTypes.DiscreteVectorKeyFrame] = assemblies[1].Find(
				"System.Windows.Media.Animation.DiscreteVectorKeyFrame", true);
			types[KnownTypes.DockPanel] = assemblies[0].Find("System.Windows.Controls.DockPanel", true);
			types[KnownTypes.DocumentPageView] = assemblies[0].Find("System.Windows.Controls.Primitives.DocumentPageView", true);
			types[KnownTypes.DocumentReference] = assemblies[0].Find("System.Windows.Documents.DocumentReference", true);
			types[KnownTypes.DocumentViewer] = assemblies[0].Find("System.Windows.Controls.DocumentViewer", true);
			types[KnownTypes.DocumentViewerBase] = assemblies[0].Find("System.Windows.Controls.Primitives.DocumentViewerBase",
				true);
			types[KnownTypes.Double] = assemblies[3].Find("System.Double", true);
			types[KnownTypes.DoubleAnimation] = assemblies[1].Find("System.Windows.Media.Animation.DoubleAnimation", true);
			types[KnownTypes.DoubleAnimationBase] = assemblies[1].Find("System.Windows.Media.Animation.DoubleAnimationBase", true);
			types[KnownTypes.DoubleAnimationUsingKeyFrames] =
				assemblies[1].Find("System.Windows.Media.Animation.DoubleAnimationUsingKeyFrames", true);
			types[KnownTypes.DoubleAnimationUsingPath] =
				assemblies[1].Find("System.Windows.Media.Animation.DoubleAnimationUsingPath", true);
			types[KnownTypes.DoubleCollection] = assemblies[1].Find("System.Windows.Media.DoubleCollection", true);
			types[KnownTypes.DoubleCollectionConverter] = assemblies[1].Find("System.Windows.Media.DoubleCollectionConverter",
				true);
			types[KnownTypes.DoubleConverter] = assemblies[4].Find("System.ComponentModel.DoubleConverter", true);
			types[KnownTypes.DoubleIListConverter] = assemblies[1].Find("System.Windows.Media.Converters.DoubleIListConverter",
				true);
			types[KnownTypes.DoubleKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.DoubleKeyFrame", true);
			types[KnownTypes.DoubleKeyFrameCollection] =
				assemblies[1].Find("System.Windows.Media.Animation.DoubleKeyFrameCollection", true);
			types[KnownTypes.Drawing] = assemblies[1].Find("System.Windows.Media.Drawing", true);
			types[KnownTypes.DrawingBrush] = assemblies[1].Find("System.Windows.Media.DrawingBrush", true);
			types[KnownTypes.DrawingCollection] = assemblies[1].Find("System.Windows.Media.DrawingCollection", true);
			types[KnownTypes.DrawingContext] = assemblies[1].Find("System.Windows.Media.DrawingContext", true);
			types[KnownTypes.DrawingGroup] = assemblies[1].Find("System.Windows.Media.DrawingGroup", true);
			types[KnownTypes.DrawingImage] = assemblies[1].Find("System.Windows.Media.DrawingImage", true);
			types[KnownTypes.DrawingVisual] = assemblies[1].Find("System.Windows.Media.DrawingVisual", true);
			types[KnownTypes.DropShadowBitmapEffect] = assemblies[1].Find("System.Windows.Media.Effects.DropShadowBitmapEffect",
				true);
			types[KnownTypes.Duration] = assemblies[1].Find("System.Windows.Duration", true);
			types[KnownTypes.DurationConverter] = assemblies[1].Find("System.Windows.DurationConverter", true);
			types[KnownTypes.DynamicResourceExtension] = assemblies[0].Find("System.Windows.DynamicResourceExtension", true);
			types[KnownTypes.DynamicResourceExtensionConverter] =
				assemblies[0].Find("System.Windows.DynamicResourceExtensionConverter", true);
			types[KnownTypes.Ellipse] = assemblies[0].Find("System.Windows.Shapes.Ellipse", true);
			types[KnownTypes.EllipseGeometry] = assemblies[1].Find("System.Windows.Media.EllipseGeometry", true);
			types[KnownTypes.EmbossBitmapEffect] = assemblies[1].Find("System.Windows.Media.Effects.EmbossBitmapEffect", true);
			types[KnownTypes.EmissiveMaterial] = assemblies[1].Find("System.Windows.Media.Media3D.EmissiveMaterial", true);
			types[KnownTypes.EnumConverter] = assemblies[4].Find("System.ComponentModel.EnumConverter", true);
			types[KnownTypes.EventManager] = assemblies[1].Find("System.Windows.EventManager", true);
			types[KnownTypes.EventSetter] = assemblies[0].Find("System.Windows.EventSetter", true);
			types[KnownTypes.EventTrigger] = assemblies[0].Find("System.Windows.EventTrigger", true);
			types[KnownTypes.Expander] = assemblies[0].Find("System.Windows.Controls.Expander", true);
			types[KnownTypes.Expression] = assemblies[5].Find("System.Windows.Expression", true);
			types[KnownTypes.ExpressionConverter] = assemblies[5].Find("System.Windows.ExpressionConverter", true);
			types[KnownTypes.Figure] = assemblies[0].Find("System.Windows.Documents.Figure", true);
			types[KnownTypes.FigureLength] = assemblies[0].Find("System.Windows.FigureLength", true);
			types[KnownTypes.FigureLengthConverter] = assemblies[0].Find("System.Windows.FigureLengthConverter", true);
			types[KnownTypes.FixedDocument] = assemblies[0].Find("System.Windows.Documents.FixedDocument", true);
			types[KnownTypes.FixedDocumentSequence] = assemblies[0].Find("System.Windows.Documents.FixedDocumentSequence", true);
			types[KnownTypes.FixedPage] = assemblies[0].Find("System.Windows.Documents.FixedPage", true);
			types[KnownTypes.Floater] = assemblies[0].Find("System.Windows.Documents.Floater", true);
			types[KnownTypes.FlowDocument] = assemblies[0].Find("System.Windows.Documents.FlowDocument", true);
			types[KnownTypes.FlowDocumentPageViewer] = assemblies[0].Find("System.Windows.Controls.FlowDocumentPageViewer", true);
			types[KnownTypes.FlowDocumentReader] = assemblies[0].Find("System.Windows.Controls.FlowDocumentReader", true);
			types[KnownTypes.FlowDocumentScrollViewer] = assemblies[0].Find("System.Windows.Controls.FlowDocumentScrollViewer",
				true);
			types[KnownTypes.FocusManager] = assemblies[1].Find("System.Windows.Input.FocusManager", true);
			types[KnownTypes.FontFamily] = assemblies[1].Find("System.Windows.Media.FontFamily", true);
			types[KnownTypes.FontFamilyConverter] = assemblies[1].Find("System.Windows.Media.FontFamilyConverter", true);
			types[KnownTypes.FontSizeConverter] = assemblies[0].Find("System.Windows.FontSizeConverter", true);
			types[KnownTypes.FontStretch] = assemblies[1].Find("System.Windows.FontStretch", true);
			types[KnownTypes.FontStretchConverter] = assemblies[1].Find("System.Windows.FontStretchConverter", true);
			types[KnownTypes.FontStyle] = assemblies[1].Find("System.Windows.FontStyle", true);
			types[KnownTypes.FontStyleConverter] = assemblies[1].Find("System.Windows.FontStyleConverter", true);
			types[KnownTypes.FontWeight] = assemblies[1].Find("System.Windows.FontWeight", true);
			types[KnownTypes.FontWeightConverter] = assemblies[1].Find("System.Windows.FontWeightConverter", true);
			types[KnownTypes.FormatConvertedBitmap] = assemblies[1].Find("System.Windows.Media.Imaging.FormatConvertedBitmap",
				true);
			types[KnownTypes.Frame] = assemblies[0].Find("System.Windows.Controls.Frame", true);
			types[KnownTypes.FrameworkContentElement] = assemblies[0].Find("System.Windows.FrameworkContentElement", true);
			types[KnownTypes.FrameworkElement] = assemblies[0].Find("System.Windows.FrameworkElement", true);
			types[KnownTypes.FrameworkElementFactory] = assemblies[0].Find("System.Windows.FrameworkElementFactory", true);
			types[KnownTypes.FrameworkPropertyMetadata] = assemblies[0].Find("System.Windows.FrameworkPropertyMetadata", true);
			types[KnownTypes.FrameworkPropertyMetadataOptions] =
				assemblies[0].Find("System.Windows.FrameworkPropertyMetadataOptions", true);
			types[KnownTypes.FrameworkRichTextComposition] =
				assemblies[0].Find("System.Windows.Documents.FrameworkRichTextComposition", true);
			types[KnownTypes.FrameworkTemplate] = assemblies[0].Find("System.Windows.FrameworkTemplate", true);
			types[KnownTypes.FrameworkTextComposition] = assemblies[0].Find("System.Windows.Documents.FrameworkTextComposition",
				true);
			types[KnownTypes.Freezable] = assemblies[5].Find("System.Windows.Freezable", true);
			types[KnownTypes.GeneralTransform] = assemblies[1].Find("System.Windows.Media.GeneralTransform", true);
			types[KnownTypes.GeneralTransformCollection] = assemblies[1].Find("System.Windows.Media.GeneralTransformCollection",
				true);
			types[KnownTypes.GeneralTransformGroup] = assemblies[1].Find("System.Windows.Media.GeneralTransformGroup", true);
			types[KnownTypes.Geometry] = assemblies[1].Find("System.Windows.Media.Geometry", true);
			types[KnownTypes.Geometry3D] = assemblies[1].Find("System.Windows.Media.Media3D.Geometry3D", true);
			types[KnownTypes.GeometryCollection] = assemblies[1].Find("System.Windows.Media.GeometryCollection", true);
			types[KnownTypes.GeometryConverter] = assemblies[1].Find("System.Windows.Media.GeometryConverter", true);
			types[KnownTypes.GeometryDrawing] = assemblies[1].Find("System.Windows.Media.GeometryDrawing", true);
			types[KnownTypes.GeometryGroup] = assemblies[1].Find("System.Windows.Media.GeometryGroup", true);
			types[KnownTypes.GeometryModel3D] = assemblies[1].Find("System.Windows.Media.Media3D.GeometryModel3D", true);
			types[KnownTypes.GestureRecognizer] = assemblies[1].Find("System.Windows.Ink.GestureRecognizer", true);
			types[KnownTypes.GifBitmapDecoder] = assemblies[1].Find("System.Windows.Media.Imaging.GifBitmapDecoder", true);
			types[KnownTypes.GifBitmapEncoder] = assemblies[1].Find("System.Windows.Media.Imaging.GifBitmapEncoder", true);
			types[KnownTypes.GlyphRun] = assemblies[1].Find("System.Windows.Media.GlyphRun", true);
			types[KnownTypes.GlyphRunDrawing] = assemblies[1].Find("System.Windows.Media.GlyphRunDrawing", true);
			types[KnownTypes.GlyphTypeface] = assemblies[1].Find("System.Windows.Media.GlyphTypeface", true);
			types[KnownTypes.Glyphs] = assemblies[0].Find("System.Windows.Documents.Glyphs", true);
			types[KnownTypes.GradientBrush] = assemblies[1].Find("System.Windows.Media.GradientBrush", true);
			types[KnownTypes.GradientStop] = assemblies[1].Find("System.Windows.Media.GradientStop", true);
			types[KnownTypes.GradientStopCollection] = assemblies[1].Find("System.Windows.Media.GradientStopCollection", true);
			types[KnownTypes.Grid] = assemblies[0].Find("System.Windows.Controls.Grid", true);
			types[KnownTypes.GridLength] = assemblies[0].Find("System.Windows.GridLength", true);
			types[KnownTypes.GridLengthConverter] = assemblies[0].Find("System.Windows.GridLengthConverter", true);
			types[KnownTypes.GridSplitter] = assemblies[0].Find("System.Windows.Controls.GridSplitter", true);
			types[KnownTypes.GridView] = assemblies[0].Find("System.Windows.Controls.GridView", true);
			types[KnownTypes.GridViewColumn] = assemblies[0].Find("System.Windows.Controls.GridViewColumn", true);
			types[KnownTypes.GridViewColumnHeader] = assemblies[0].Find("System.Windows.Controls.GridViewColumnHeader", true);
			types[KnownTypes.GridViewHeaderRowPresenter] =
				assemblies[0].Find("System.Windows.Controls.GridViewHeaderRowPresenter", true);
			types[KnownTypes.GridViewRowPresenter] = assemblies[0].Find("System.Windows.Controls.GridViewRowPresenter", true);
			types[KnownTypes.GridViewRowPresenterBase] =
				assemblies[0].Find("System.Windows.Controls.Primitives.GridViewRowPresenterBase", true);
			types[KnownTypes.GroupBox] = assemblies[0].Find("System.Windows.Controls.GroupBox", true);
			types[KnownTypes.GroupItem] = assemblies[0].Find("System.Windows.Controls.GroupItem", true);
			types[KnownTypes.Guid] = assemblies[3].Find("System.Guid", true);
			types[KnownTypes.GuidConverter] = assemblies[4].Find("System.ComponentModel.GuidConverter", true);
			types[KnownTypes.GuidelineSet] = assemblies[1].Find("System.Windows.Media.GuidelineSet", true);
			types[KnownTypes.HeaderedContentControl] = assemblies[0].Find("System.Windows.Controls.HeaderedContentControl", true);
			types[KnownTypes.HeaderedItemsControl] = assemblies[0].Find("System.Windows.Controls.HeaderedItemsControl", true);
			types[KnownTypes.HierarchicalDataTemplate] = assemblies[0].Find("System.Windows.HierarchicalDataTemplate", true);
			types[KnownTypes.HostVisual] = assemblies[1].Find("System.Windows.Media.HostVisual", true);
			types[KnownTypes.Hyperlink] = assemblies[0].Find("System.Windows.Documents.Hyperlink", true);
			types[KnownTypes.IAddChild] = assemblies[1].Find("System.Windows.Markup.IAddChild", true);
			types[KnownTypes.IAddChildInternal] = assemblies[1].Find("System.Windows.Markup.IAddChildInternal", true);
			types[KnownTypes.ICommand] = assemblies[1].Find("System.Windows.Input.ICommand", true);
			types[KnownTypes.IComponentConnector] = assemblies[2].Find("System.Windows.Markup.IComponentConnector", true);
			types[KnownTypes.INameScope] = assemblies[2].Find("System.Windows.Markup.INameScope", true);
			types[KnownTypes.IStyleConnector] = assemblies[0].Find("System.Windows.Markup.IStyleConnector", true);
			types[KnownTypes.IconBitmapDecoder] = assemblies[1].Find("System.Windows.Media.Imaging.IconBitmapDecoder", true);
			types[KnownTypes.Image] = assemblies[0].Find("System.Windows.Controls.Image", true);
			types[KnownTypes.ImageBrush] = assemblies[1].Find("System.Windows.Media.ImageBrush", true);
			types[KnownTypes.ImageDrawing] = assemblies[1].Find("System.Windows.Media.ImageDrawing", true);
			types[KnownTypes.ImageMetadata] = assemblies[1].Find("System.Windows.Media.ImageMetadata", true);
			types[KnownTypes.ImageSource] = assemblies[1].Find("System.Windows.Media.ImageSource", true);
			types[KnownTypes.ImageSourceConverter] = assemblies[1].Find("System.Windows.Media.ImageSourceConverter", true);
			types[KnownTypes.InPlaceBitmapMetadataWriter] =
				assemblies[1].Find("System.Windows.Media.Imaging.InPlaceBitmapMetadataWriter", true);
			types[KnownTypes.InkCanvas] = assemblies[0].Find("System.Windows.Controls.InkCanvas", true);
			types[KnownTypes.InkPresenter] = assemblies[0].Find("System.Windows.Controls.InkPresenter", true);
			types[KnownTypes.Inline] = assemblies[0].Find("System.Windows.Documents.Inline", true);
			types[KnownTypes.InlineCollection] = assemblies[0].Find("System.Windows.Documents.InlineCollection", true);
			types[KnownTypes.InlineUIContainer] = assemblies[0].Find("System.Windows.Documents.InlineUIContainer", true);
			types[KnownTypes.InputBinding] = assemblies[1].Find("System.Windows.Input.InputBinding", true);
			types[KnownTypes.InputDevice] = assemblies[1].Find("System.Windows.Input.InputDevice", true);
			types[KnownTypes.InputLanguageManager] = assemblies[1].Find("System.Windows.Input.InputLanguageManager", true);
			types[KnownTypes.InputManager] = assemblies[1].Find("System.Windows.Input.InputManager", true);
			types[KnownTypes.InputMethod] = assemblies[1].Find("System.Windows.Input.InputMethod", true);
			types[KnownTypes.InputScope] = assemblies[1].Find("System.Windows.Input.InputScope", true);
			types[KnownTypes.InputScopeConverter] = assemblies[1].Find("System.Windows.Input.InputScopeConverter", true);
			types[KnownTypes.InputScopeName] = assemblies[1].Find("System.Windows.Input.InputScopeName", true);
			types[KnownTypes.InputScopeNameConverter] = assemblies[1].Find("System.Windows.Input.InputScopeNameConverter", true);
			types[KnownTypes.Int16] = assemblies[3].Find("System.Int16", true);
			types[KnownTypes.Int16Animation] = assemblies[1].Find("System.Windows.Media.Animation.Int16Animation", true);
			types[KnownTypes.Int16AnimationBase] = assemblies[1].Find("System.Windows.Media.Animation.Int16AnimationBase", true);
			types[KnownTypes.Int16AnimationUsingKeyFrames] =
				assemblies[1].Find("System.Windows.Media.Animation.Int16AnimationUsingKeyFrames", true);
			types[KnownTypes.Int16Converter] = assemblies[4].Find("System.ComponentModel.Int16Converter", true);
			types[KnownTypes.Int16KeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.Int16KeyFrame", true);
			types[KnownTypes.Int16KeyFrameCollection] =
				assemblies[1].Find("System.Windows.Media.Animation.Int16KeyFrameCollection", true);
			types[KnownTypes.Int32] = assemblies[3].Find("System.Int32", true);
			types[KnownTypes.Int32Animation] = assemblies[1].Find("System.Windows.Media.Animation.Int32Animation", true);
			types[KnownTypes.Int32AnimationBase] = assemblies[1].Find("System.Windows.Media.Animation.Int32AnimationBase", true);
			types[KnownTypes.Int32AnimationUsingKeyFrames] =
				assemblies[1].Find("System.Windows.Media.Animation.Int32AnimationUsingKeyFrames", true);
			types[KnownTypes.Int32Collection] = assemblies[1].Find("System.Windows.Media.Int32Collection", true);
			types[KnownTypes.Int32CollectionConverter] = assemblies[1].Find("System.Windows.Media.Int32CollectionConverter", true);
			types[KnownTypes.Int32Converter] = assemblies[4].Find("System.ComponentModel.Int32Converter", true);
			types[KnownTypes.Int32KeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.Int32KeyFrame", true);
			types[KnownTypes.Int32KeyFrameCollection] =
				assemblies[1].Find("System.Windows.Media.Animation.Int32KeyFrameCollection", true);
			types[KnownTypes.Int32Rect] = assemblies[5].Find("System.Windows.Int32Rect", true);
			types[KnownTypes.Int32RectConverter] = assemblies[5].Find("System.Windows.Int32RectConverter", true);
			types[KnownTypes.Int64] = assemblies[3].Find("System.Int64", true);
			types[KnownTypes.Int64Animation] = assemblies[1].Find("System.Windows.Media.Animation.Int64Animation", true);
			types[KnownTypes.Int64AnimationBase] = assemblies[1].Find("System.Windows.Media.Animation.Int64AnimationBase", true);
			types[KnownTypes.Int64AnimationUsingKeyFrames] =
				assemblies[1].Find("System.Windows.Media.Animation.Int64AnimationUsingKeyFrames", true);
			types[KnownTypes.Int64Converter] = assemblies[4].Find("System.ComponentModel.Int64Converter", true);
			types[KnownTypes.Int64KeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.Int64KeyFrame", true);
			types[KnownTypes.Int64KeyFrameCollection] =
				assemblies[1].Find("System.Windows.Media.Animation.Int64KeyFrameCollection", true);
			types[KnownTypes.Italic] = assemblies[0].Find("System.Windows.Documents.Italic", true);
			types[KnownTypes.ItemCollection] = assemblies[0].Find("System.Windows.Controls.ItemCollection", true);
			types[KnownTypes.ItemsControl] = assemblies[0].Find("System.Windows.Controls.ItemsControl", true);
			types[KnownTypes.ItemsPanelTemplate] = assemblies[0].Find("System.Windows.Controls.ItemsPanelTemplate", true);
			types[KnownTypes.ItemsPresenter] = assemblies[0].Find("System.Windows.Controls.ItemsPresenter", true);
			types[KnownTypes.JournalEntry] = assemblies[0].Find("System.Windows.Navigation.JournalEntry", true);
			types[KnownTypes.JournalEntryListConverter] =
				assemblies[0].Find("System.Windows.Navigation.JournalEntryListConverter", true);
			types[KnownTypes.JournalEntryUnifiedViewConverter] =
				assemblies[0].Find("System.Windows.Navigation.JournalEntryUnifiedViewConverter", true);
			types[KnownTypes.JpegBitmapDecoder] = assemblies[1].Find("System.Windows.Media.Imaging.JpegBitmapDecoder", true);
			types[KnownTypes.JpegBitmapEncoder] = assemblies[1].Find("System.Windows.Media.Imaging.JpegBitmapEncoder", true);
			types[KnownTypes.KeyBinding] = assemblies[1].Find("System.Windows.Input.KeyBinding", true);
			types[KnownTypes.KeyConverter] = assemblies[5].Find("System.Windows.Input.KeyConverter", true);
			types[KnownTypes.KeyGesture] = assemblies[1].Find("System.Windows.Input.KeyGesture", true);
			types[KnownTypes.KeyGestureConverter] = assemblies[1].Find("System.Windows.Input.KeyGestureConverter", true);
			types[KnownTypes.KeySpline] = assemblies[1].Find("System.Windows.Media.Animation.KeySpline", true);
			types[KnownTypes.KeySplineConverter] = assemblies[1].Find("System.Windows.KeySplineConverter", true);
			types[KnownTypes.KeyTime] = assemblies[1].Find("System.Windows.Media.Animation.KeyTime", true);
			types[KnownTypes.KeyTimeConverter] = assemblies[1].Find("System.Windows.KeyTimeConverter", true);
			types[KnownTypes.KeyboardDevice] = assemblies[1].Find("System.Windows.Input.KeyboardDevice", true);
			types[KnownTypes.Label] = assemblies[0].Find("System.Windows.Controls.Label", true);
			types[KnownTypes.LateBoundBitmapDecoder] = assemblies[1].Find("System.Windows.Media.Imaging.LateBoundBitmapDecoder",
				true);
			types[KnownTypes.LengthConverter] = assemblies[0].Find("System.Windows.LengthConverter", true);
			types[KnownTypes.Light] = assemblies[1].Find("System.Windows.Media.Media3D.Light", true);
			types[KnownTypes.Line] = assemblies[0].Find("System.Windows.Shapes.Line", true);
			types[KnownTypes.LineBreak] = assemblies[0].Find("System.Windows.Documents.LineBreak", true);
			types[KnownTypes.LineGeometry] = assemblies[1].Find("System.Windows.Media.LineGeometry", true);
			types[KnownTypes.LineSegment] = assemblies[1].Find("System.Windows.Media.LineSegment", true);
			types[KnownTypes.LinearByteKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.LinearByteKeyFrame", true);
			types[KnownTypes.LinearColorKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.LinearColorKeyFrame", true);
			types[KnownTypes.LinearDecimalKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.LinearDecimalKeyFrame",
				true);
			types[KnownTypes.LinearDoubleKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.LinearDoubleKeyFrame",
				true);
			types[KnownTypes.LinearGradientBrush] = assemblies[1].Find("System.Windows.Media.LinearGradientBrush", true);
			types[KnownTypes.LinearInt16KeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.LinearInt16KeyFrame", true);
			types[KnownTypes.LinearInt32KeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.LinearInt32KeyFrame", true);
			types[KnownTypes.LinearInt64KeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.LinearInt64KeyFrame", true);
			types[KnownTypes.LinearPoint3DKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.LinearPoint3DKeyFrame",
				true);
			types[KnownTypes.LinearPointKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.LinearPointKeyFrame", true);
			types[KnownTypes.LinearQuaternionKeyFrame] =
				assemblies[1].Find("System.Windows.Media.Animation.LinearQuaternionKeyFrame", true);
			types[KnownTypes.LinearRectKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.LinearRectKeyFrame", true);
			types[KnownTypes.LinearRotation3DKeyFrame] =
				assemblies[1].Find("System.Windows.Media.Animation.LinearRotation3DKeyFrame", true);
			types[KnownTypes.LinearSingleKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.LinearSingleKeyFrame",
				true);
			types[KnownTypes.LinearSizeKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.LinearSizeKeyFrame", true);
			types[KnownTypes.LinearThicknessKeyFrame] =
				assemblies[0].Find("System.Windows.Media.Animation.LinearThicknessKeyFrame", true);
			types[KnownTypes.LinearVector3DKeyFrame] = assemblies[1].Find(
				"System.Windows.Media.Animation.LinearVector3DKeyFrame", true);
			types[KnownTypes.LinearVectorKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.LinearVectorKeyFrame",
				true);
			types[KnownTypes.List] = assemblies[0].Find("System.Windows.Documents.List", true);
			types[KnownTypes.ListBox] = assemblies[0].Find("System.Windows.Controls.ListBox", true);
			types[KnownTypes.ListBoxItem] = assemblies[0].Find("System.Windows.Controls.ListBoxItem", true);
			types[KnownTypes.ListCollectionView] = assemblies[0].Find("System.Windows.Data.ListCollectionView", true);
			types[KnownTypes.ListItem] = assemblies[0].Find("System.Windows.Documents.ListItem", true);
			types[KnownTypes.ListView] = assemblies[0].Find("System.Windows.Controls.ListView", true);
			types[KnownTypes.ListViewItem] = assemblies[0].Find("System.Windows.Controls.ListViewItem", true);
			types[KnownTypes.Localization] = assemblies[0].Find("System.Windows.Localization", true);
			types[KnownTypes.LostFocusEventManager] = assemblies[0].Find("System.Windows.LostFocusEventManager", true);
			types[KnownTypes.MarkupExtension] = assemblies[2].Find("System.Windows.Markup.MarkupExtension", true);
			types[KnownTypes.Material] = assemblies[1].Find("System.Windows.Media.Media3D.Material", true);
			types[KnownTypes.MaterialCollection] = assemblies[1].Find("System.Windows.Media.Media3D.MaterialCollection", true);
			types[KnownTypes.MaterialGroup] = assemblies[1].Find("System.Windows.Media.Media3D.MaterialGroup", true);
			types[KnownTypes.Matrix] = assemblies[5].Find("System.Windows.Media.Matrix", true);
			types[KnownTypes.Matrix3D] = assemblies[1].Find("System.Windows.Media.Media3D.Matrix3D", true);
			types[KnownTypes.Matrix3DConverter] = assemblies[1].Find("System.Windows.Media.Media3D.Matrix3DConverter", true);
			types[KnownTypes.MatrixAnimationBase] = assemblies[1].Find("System.Windows.Media.Animation.MatrixAnimationBase", true);
			types[KnownTypes.MatrixAnimationUsingKeyFrames] =
				assemblies[1].Find("System.Windows.Media.Animation.MatrixAnimationUsingKeyFrames", true);
			types[KnownTypes.MatrixAnimationUsingPath] =
				assemblies[1].Find("System.Windows.Media.Animation.MatrixAnimationUsingPath", true);
			types[KnownTypes.MatrixCamera] = assemblies[1].Find("System.Windows.Media.Media3D.MatrixCamera", true);
			types[KnownTypes.MatrixConverter] = assemblies[5].Find("System.Windows.Media.MatrixConverter", true);
			types[KnownTypes.MatrixKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.MatrixKeyFrame", true);
			types[KnownTypes.MatrixKeyFrameCollection] =
				assemblies[1].Find("System.Windows.Media.Animation.MatrixKeyFrameCollection", true);
			types[KnownTypes.MatrixTransform] = assemblies[1].Find("System.Windows.Media.MatrixTransform", true);
			types[KnownTypes.MatrixTransform3D] = assemblies[1].Find("System.Windows.Media.Media3D.MatrixTransform3D", true);
			types[KnownTypes.MediaClock] = assemblies[1].Find("System.Windows.Media.MediaClock", true);
			types[KnownTypes.MediaElement] = assemblies[0].Find("System.Windows.Controls.MediaElement", true);
			types[KnownTypes.MediaPlayer] = assemblies[1].Find("System.Windows.Media.MediaPlayer", true);
			types[KnownTypes.MediaTimeline] = assemblies[1].Find("System.Windows.Media.MediaTimeline", true);
			types[KnownTypes.Menu] = assemblies[0].Find("System.Windows.Controls.Menu", true);
			types[KnownTypes.MenuBase] = assemblies[0].Find("System.Windows.Controls.Primitives.MenuBase", true);
			types[KnownTypes.MenuItem] = assemblies[0].Find("System.Windows.Controls.MenuItem", true);
			types[KnownTypes.MenuScrollingVisibilityConverter] =
				assemblies[0].Find("System.Windows.Controls.MenuScrollingVisibilityConverter", true);
			types[KnownTypes.MeshGeometry3D] = assemblies[1].Find("System.Windows.Media.Media3D.MeshGeometry3D", true);
			types[KnownTypes.Model3D] = assemblies[1].Find("System.Windows.Media.Media3D.Model3D", true);
			types[KnownTypes.Model3DCollection] = assemblies[1].Find("System.Windows.Media.Media3D.Model3DCollection", true);
			types[KnownTypes.Model3DGroup] = assemblies[1].Find("System.Windows.Media.Media3D.Model3DGroup", true);
			types[KnownTypes.ModelVisual3D] = assemblies[1].Find("System.Windows.Media.Media3D.ModelVisual3D", true);
			types[KnownTypes.ModifierKeysConverter] = assemblies[5].Find("System.Windows.Input.ModifierKeysConverter", true);
			types[KnownTypes.MouseActionConverter] = assemblies[1].Find("System.Windows.Input.MouseActionConverter", true);
			types[KnownTypes.MouseBinding] = assemblies[1].Find("System.Windows.Input.MouseBinding", true);
			types[KnownTypes.MouseDevice] = assemblies[1].Find("System.Windows.Input.MouseDevice", true);
			types[KnownTypes.MouseGesture] = assemblies[1].Find("System.Windows.Input.MouseGesture", true);
			types[KnownTypes.MouseGestureConverter] = assemblies[1].Find("System.Windows.Input.MouseGestureConverter", true);
			types[KnownTypes.MultiBinding] = assemblies[0].Find("System.Windows.Data.MultiBinding", true);
			types[KnownTypes.MultiBindingExpression] = assemblies[0].Find("System.Windows.Data.MultiBindingExpression", true);
			types[KnownTypes.MultiDataTrigger] = assemblies[0].Find("System.Windows.MultiDataTrigger", true);
			types[KnownTypes.MultiTrigger] = assemblies[0].Find("System.Windows.MultiTrigger", true);
			types[KnownTypes.NameScope] = assemblies[5].Find("System.Windows.NameScope", true);
			types[KnownTypes.NavigationWindow] = assemblies[0].Find("System.Windows.Navigation.NavigationWindow", true);
			types[KnownTypes.NullExtension] = assemblies[2].Find("System.Windows.Markup.NullExtension", true);
			types[KnownTypes.NullableBoolConverter] = assemblies[0].Find("System.Windows.NullableBoolConverter", true);
			types[KnownTypes.NullableConverter] = assemblies[4].Find("System.ComponentModel.NullableConverter", true);
			types[KnownTypes.NumberSubstitution] = assemblies[1].Find("System.Windows.Media.NumberSubstitution", true);
			types[KnownTypes.Object] = assemblies[3].Find("System.Object", true);
			types[KnownTypes.ObjectAnimationBase] = assemblies[1].Find("System.Windows.Media.Animation.ObjectAnimationBase", true);
			types[KnownTypes.ObjectAnimationUsingKeyFrames] =
				assemblies[1].Find("System.Windows.Media.Animation.ObjectAnimationUsingKeyFrames", true);
			types[KnownTypes.ObjectDataProvider] = assemblies[0].Find("System.Windows.Data.ObjectDataProvider", true);
			types[KnownTypes.ObjectKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.ObjectKeyFrame", true);
			types[KnownTypes.ObjectKeyFrameCollection] =
				assemblies[1].Find("System.Windows.Media.Animation.ObjectKeyFrameCollection", true);
			types[KnownTypes.OrthographicCamera] = assemblies[1].Find("System.Windows.Media.Media3D.OrthographicCamera", true);
			types[KnownTypes.OuterGlowBitmapEffect] = assemblies[1].Find("System.Windows.Media.Effects.OuterGlowBitmapEffect",
				true);
			types[KnownTypes.Page] = assemblies[0].Find("System.Windows.Controls.Page", true);
			types[KnownTypes.PageContent] = assemblies[0].Find("System.Windows.Documents.PageContent", true);
			types[KnownTypes.PageFunctionBase] = assemblies[0].Find("System.Windows.Navigation.PageFunctionBase", true);
			types[KnownTypes.Panel] = assemblies[0].Find("System.Windows.Controls.Panel", true);
			types[KnownTypes.Paragraph] = assemblies[0].Find("System.Windows.Documents.Paragraph", true);
			types[KnownTypes.ParallelTimeline] = assemblies[1].Find("System.Windows.Media.Animation.ParallelTimeline", true);
			types[KnownTypes.ParserContext] = assemblies[0].Find("System.Windows.Markup.ParserContext", true);
			types[KnownTypes.PasswordBox] = assemblies[0].Find("System.Windows.Controls.PasswordBox", true);
			types[KnownTypes.Path] = assemblies[0].Find("System.Windows.Shapes.Path", true);
			types[KnownTypes.PathFigure] = assemblies[1].Find("System.Windows.Media.PathFigure", true);
			types[KnownTypes.PathFigureCollection] = assemblies[1].Find("System.Windows.Media.PathFigureCollection", true);
			types[KnownTypes.PathFigureCollectionConverter] =
				assemblies[1].Find("System.Windows.Media.PathFigureCollectionConverter", true);
			types[KnownTypes.PathGeometry] = assemblies[1].Find("System.Windows.Media.PathGeometry", true);
			types[KnownTypes.PathSegment] = assemblies[1].Find("System.Windows.Media.PathSegment", true);
			types[KnownTypes.PathSegmentCollection] = assemblies[1].Find("System.Windows.Media.PathSegmentCollection", true);
			types[KnownTypes.PauseStoryboard] = assemblies[0].Find("System.Windows.Media.Animation.PauseStoryboard", true);
			types[KnownTypes.Pen] = assemblies[1].Find("System.Windows.Media.Pen", true);
			types[KnownTypes.PerspectiveCamera] = assemblies[1].Find("System.Windows.Media.Media3D.PerspectiveCamera", true);
			types[KnownTypes.PixelFormat] = assemblies[1].Find("System.Windows.Media.PixelFormat", true);
			types[KnownTypes.PixelFormatConverter] = assemblies[1].Find("System.Windows.Media.PixelFormatConverter", true);
			types[KnownTypes.PngBitmapDecoder] = assemblies[1].Find("System.Windows.Media.Imaging.PngBitmapDecoder", true);
			types[KnownTypes.PngBitmapEncoder] = assemblies[1].Find("System.Windows.Media.Imaging.PngBitmapEncoder", true);
			types[KnownTypes.Point] = assemblies[5].Find("System.Windows.Point", true);
			types[KnownTypes.Point3D] = assemblies[1].Find("System.Windows.Media.Media3D.Point3D", true);
			types[KnownTypes.Point3DAnimation] = assemblies[1].Find("System.Windows.Media.Animation.Point3DAnimation", true);
			types[KnownTypes.Point3DAnimationBase] = assemblies[1].Find("System.Windows.Media.Animation.Point3DAnimationBase",
				true);
			types[KnownTypes.Point3DAnimationUsingKeyFrames] =
				assemblies[1].Find("System.Windows.Media.Animation.Point3DAnimationUsingKeyFrames", true);
			types[KnownTypes.Point3DCollection] = assemblies[1].Find("System.Windows.Media.Media3D.Point3DCollection", true);
			types[KnownTypes.Point3DCollectionConverter] =
				assemblies[1].Find("System.Windows.Media.Media3D.Point3DCollectionConverter", true);
			types[KnownTypes.Point3DConverter] = assemblies[1].Find("System.Windows.Media.Media3D.Point3DConverter", true);
			types[KnownTypes.Point3DKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.Point3DKeyFrame", true);
			types[KnownTypes.Point3DKeyFrameCollection] =
				assemblies[1].Find("System.Windows.Media.Animation.Point3DKeyFrameCollection", true);
			types[KnownTypes.Point4D] = assemblies[1].Find("System.Windows.Media.Media3D.Point4D", true);
			types[KnownTypes.Point4DConverter] = assemblies[1].Find("System.Windows.Media.Media3D.Point4DConverter", true);
			types[KnownTypes.PointAnimation] = assemblies[1].Find("System.Windows.Media.Animation.PointAnimation", true);
			types[KnownTypes.PointAnimationBase] = assemblies[1].Find("System.Windows.Media.Animation.PointAnimationBase", true);
			types[KnownTypes.PointAnimationUsingKeyFrames] =
				assemblies[1].Find("System.Windows.Media.Animation.PointAnimationUsingKeyFrames", true);
			types[KnownTypes.PointAnimationUsingPath] =
				assemblies[1].Find("System.Windows.Media.Animation.PointAnimationUsingPath", true);
			types[KnownTypes.PointCollection] = assemblies[1].Find("System.Windows.Media.PointCollection", true);
			types[KnownTypes.PointCollectionConverter] = assemblies[1].Find("System.Windows.Media.PointCollectionConverter", true);
			types[KnownTypes.PointConverter] = assemblies[5].Find("System.Windows.PointConverter", true);
			types[KnownTypes.PointIListConverter] = assemblies[1].Find("System.Windows.Media.Converters.PointIListConverter",
				true);
			types[KnownTypes.PointKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.PointKeyFrame", true);
			types[KnownTypes.PointKeyFrameCollection] =
				assemblies[1].Find("System.Windows.Media.Animation.PointKeyFrameCollection", true);
			types[KnownTypes.PointLight] = assemblies[1].Find("System.Windows.Media.Media3D.PointLight", true);
			types[KnownTypes.PointLightBase] = assemblies[1].Find("System.Windows.Media.Media3D.PointLightBase", true);
			types[KnownTypes.PolyBezierSegment] = assemblies[1].Find("System.Windows.Media.PolyBezierSegment", true);
			types[KnownTypes.PolyLineSegment] = assemblies[1].Find("System.Windows.Media.PolyLineSegment", true);
			types[KnownTypes.PolyQuadraticBezierSegment] = assemblies[1].Find("System.Windows.Media.PolyQuadraticBezierSegment",
				true);
			types[KnownTypes.Polygon] = assemblies[0].Find("System.Windows.Shapes.Polygon", true);
			types[KnownTypes.Polyline] = assemblies[0].Find("System.Windows.Shapes.Polyline", true);
			types[KnownTypes.Popup] = assemblies[0].Find("System.Windows.Controls.Primitives.Popup", true);
			types[KnownTypes.PresentationSource] = assemblies[1].Find("System.Windows.PresentationSource", true);
			types[KnownTypes.PriorityBinding] = assemblies[0].Find("System.Windows.Data.PriorityBinding", true);
			types[KnownTypes.PriorityBindingExpression] = assemblies[0].Find("System.Windows.Data.PriorityBindingExpression",
				true);
			types[KnownTypes.ProgressBar] = assemblies[0].Find("System.Windows.Controls.ProgressBar", true);
			types[KnownTypes.ProjectionCamera] = assemblies[1].Find("System.Windows.Media.Media3D.ProjectionCamera", true);
			types[KnownTypes.PropertyPath] = assemblies[0].Find("System.Windows.PropertyPath", true);
			types[KnownTypes.PropertyPathConverter] = assemblies[0].Find("System.Windows.PropertyPathConverter", true);
			types[KnownTypes.QuadraticBezierSegment] = assemblies[1].Find("System.Windows.Media.QuadraticBezierSegment", true);
			types[KnownTypes.Quaternion] = assemblies[1].Find("System.Windows.Media.Media3D.Quaternion", true);
			types[KnownTypes.QuaternionAnimation] = assemblies[1].Find("System.Windows.Media.Animation.QuaternionAnimation", true);
			types[KnownTypes.QuaternionAnimationBase] =
				assemblies[1].Find("System.Windows.Media.Animation.QuaternionAnimationBase", true);
			types[KnownTypes.QuaternionAnimationUsingKeyFrames] =
				assemblies[1].Find("System.Windows.Media.Animation.QuaternionAnimationUsingKeyFrames", true);
			types[KnownTypes.QuaternionConverter] = assemblies[1].Find("System.Windows.Media.Media3D.QuaternionConverter", true);
			types[KnownTypes.QuaternionKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.QuaternionKeyFrame", true);
			types[KnownTypes.QuaternionKeyFrameCollection] =
				assemblies[1].Find("System.Windows.Media.Animation.QuaternionKeyFrameCollection", true);
			types[KnownTypes.QuaternionRotation3D] = assemblies[1].Find("System.Windows.Media.Media3D.QuaternionRotation3D", true);
			types[KnownTypes.RadialGradientBrush] = assemblies[1].Find("System.Windows.Media.RadialGradientBrush", true);
			types[KnownTypes.RadioButton] = assemblies[0].Find("System.Windows.Controls.RadioButton", true);
			types[KnownTypes.RangeBase] = assemblies[0].Find("System.Windows.Controls.Primitives.RangeBase", true);
			types[KnownTypes.Rect] = assemblies[5].Find("System.Windows.Rect", true);
			types[KnownTypes.Rect3D] = assemblies[1].Find("System.Windows.Media.Media3D.Rect3D", true);
			types[KnownTypes.Rect3DConverter] = assemblies[1].Find("System.Windows.Media.Media3D.Rect3DConverter", true);
			types[KnownTypes.RectAnimation] = assemblies[1].Find("System.Windows.Media.Animation.RectAnimation", true);
			types[KnownTypes.RectAnimationBase] = assemblies[1].Find("System.Windows.Media.Animation.RectAnimationBase", true);
			types[KnownTypes.RectAnimationUsingKeyFrames] =
				assemblies[1].Find("System.Windows.Media.Animation.RectAnimationUsingKeyFrames", true);
			types[KnownTypes.RectConverter] = assemblies[5].Find("System.Windows.RectConverter", true);
			types[KnownTypes.RectKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.RectKeyFrame", true);
			types[KnownTypes.RectKeyFrameCollection] = assemblies[1].Find(
				"System.Windows.Media.Animation.RectKeyFrameCollection", true);
			types[KnownTypes.Rectangle] = assemblies[0].Find("System.Windows.Shapes.Rectangle", true);
			types[KnownTypes.RectangleGeometry] = assemblies[1].Find("System.Windows.Media.RectangleGeometry", true);
			types[KnownTypes.RelativeSource] = assemblies[0].Find("System.Windows.Data.RelativeSource", true);
			types[KnownTypes.RemoveStoryboard] = assemblies[0].Find("System.Windows.Media.Animation.RemoveStoryboard", true);
			types[KnownTypes.RenderOptions] = assemblies[1].Find("System.Windows.Media.RenderOptions", true);
			types[KnownTypes.RenderTargetBitmap] = assemblies[1].Find("System.Windows.Media.Imaging.RenderTargetBitmap", true);
			types[KnownTypes.RepeatBehavior] = assemblies[1].Find("System.Windows.Media.Animation.RepeatBehavior", true);
			types[KnownTypes.RepeatBehaviorConverter] =
				assemblies[1].Find("System.Windows.Media.Animation.RepeatBehaviorConverter", true);
			types[KnownTypes.RepeatButton] = assemblies[0].Find("System.Windows.Controls.Primitives.RepeatButton", true);
			types[KnownTypes.ResizeGrip] = assemblies[0].Find("System.Windows.Controls.Primitives.ResizeGrip", true);
			types[KnownTypes.ResourceDictionary] = assemblies[0].Find("System.Windows.ResourceDictionary", true);
			types[KnownTypes.ResourceKey] = assemblies[0].Find("System.Windows.ResourceKey", true);
			types[KnownTypes.ResumeStoryboard] = assemblies[0].Find("System.Windows.Media.Animation.ResumeStoryboard", true);
			types[KnownTypes.RichTextBox] = assemblies[0].Find("System.Windows.Controls.RichTextBox", true);
			types[KnownTypes.RotateTransform] = assemblies[1].Find("System.Windows.Media.RotateTransform", true);
			types[KnownTypes.RotateTransform3D] = assemblies[1].Find("System.Windows.Media.Media3D.RotateTransform3D", true);
			types[KnownTypes.Rotation3D] = assemblies[1].Find("System.Windows.Media.Media3D.Rotation3D", true);
			types[KnownTypes.Rotation3DAnimation] = assemblies[1].Find("System.Windows.Media.Animation.Rotation3DAnimation", true);
			types[KnownTypes.Rotation3DAnimationBase] =
				assemblies[1].Find("System.Windows.Media.Animation.Rotation3DAnimationBase", true);
			types[KnownTypes.Rotation3DAnimationUsingKeyFrames] =
				assemblies[1].Find("System.Windows.Media.Animation.Rotation3DAnimationUsingKeyFrames", true);
			types[KnownTypes.Rotation3DKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.Rotation3DKeyFrame", true);
			types[KnownTypes.Rotation3DKeyFrameCollection] =
				assemblies[1].Find("System.Windows.Media.Animation.Rotation3DKeyFrameCollection", true);
			types[KnownTypes.RoutedCommand] = assemblies[1].Find("System.Windows.Input.RoutedCommand", true);
			types[KnownTypes.RoutedEvent] = assemblies[1].Find("System.Windows.RoutedEvent", true);
			types[KnownTypes.RoutedEventConverter] = assemblies[0].Find("System.Windows.Markup.RoutedEventConverter", true);
			types[KnownTypes.RoutedUICommand] = assemblies[1].Find("System.Windows.Input.RoutedUICommand", true);
			types[KnownTypes.RoutingStrategy] = assemblies[1].Find("System.Windows.RoutingStrategy", true);
			types[KnownTypes.RowDefinition] = assemblies[0].Find("System.Windows.Controls.RowDefinition", true);
			types[KnownTypes.Run] = assemblies[0].Find("System.Windows.Documents.Run", true);
			types[KnownTypes.RuntimeNamePropertyAttribute] =
				assemblies[2].Find("System.Windows.Markup.RuntimeNamePropertyAttribute", true);
			types[KnownTypes.SByte] = assemblies[3].Find("System.SByte", true);
			types[KnownTypes.SByteConverter] = assemblies[4].Find("System.ComponentModel.SByteConverter", true);
			types[KnownTypes.ScaleTransform] = assemblies[1].Find("System.Windows.Media.ScaleTransform", true);
			types[KnownTypes.ScaleTransform3D] = assemblies[1].Find("System.Windows.Media.Media3D.ScaleTransform3D", true);
			types[KnownTypes.ScrollBar] = assemblies[0].Find("System.Windows.Controls.Primitives.ScrollBar", true);
			types[KnownTypes.ScrollContentPresenter] = assemblies[0].Find("System.Windows.Controls.ScrollContentPresenter", true);
			types[KnownTypes.ScrollViewer] = assemblies[0].Find("System.Windows.Controls.ScrollViewer", true);
			types[KnownTypes.Section] = assemblies[0].Find("System.Windows.Documents.Section", true);
			types[KnownTypes.SeekStoryboard] = assemblies[0].Find("System.Windows.Media.Animation.SeekStoryboard", true);
			types[KnownTypes.Selector] = assemblies[0].Find("System.Windows.Controls.Primitives.Selector", true);
			types[KnownTypes.Separator] = assemblies[0].Find("System.Windows.Controls.Separator", true);
			types[KnownTypes.SetStoryboardSpeedRatio] =
				assemblies[0].Find("System.Windows.Media.Animation.SetStoryboardSpeedRatio", true);
			types[KnownTypes.Setter] = assemblies[0].Find("System.Windows.Setter", true);
			types[KnownTypes.SetterBase] = assemblies[0].Find("System.Windows.SetterBase", true);
			types[KnownTypes.Shape] = assemblies[0].Find("System.Windows.Shapes.Shape", true);
			types[KnownTypes.Single] = assemblies[3].Find("System.Single", true);
			types[KnownTypes.SingleAnimation] = assemblies[1].Find("System.Windows.Media.Animation.SingleAnimation", true);
			types[KnownTypes.SingleAnimationBase] = assemblies[1].Find("System.Windows.Media.Animation.SingleAnimationBase", true);
			types[KnownTypes.SingleAnimationUsingKeyFrames] =
				assemblies[1].Find("System.Windows.Media.Animation.SingleAnimationUsingKeyFrames", true);
			types[KnownTypes.SingleConverter] = assemblies[4].Find("System.ComponentModel.SingleConverter", true);
			types[KnownTypes.SingleKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.SingleKeyFrame", true);
			types[KnownTypes.SingleKeyFrameCollection] =
				assemblies[1].Find("System.Windows.Media.Animation.SingleKeyFrameCollection", true);
			types[KnownTypes.Size] = assemblies[5].Find("System.Windows.Size", true);
			types[KnownTypes.Size3D] = assemblies[1].Find("System.Windows.Media.Media3D.Size3D", true);
			types[KnownTypes.Size3DConverter] = assemblies[1].Find("System.Windows.Media.Media3D.Size3DConverter", true);
			types[KnownTypes.SizeAnimation] = assemblies[1].Find("System.Windows.Media.Animation.SizeAnimation", true);
			types[KnownTypes.SizeAnimationBase] = assemblies[1].Find("System.Windows.Media.Animation.SizeAnimationBase", true);
			types[KnownTypes.SizeAnimationUsingKeyFrames] =
				assemblies[1].Find("System.Windows.Media.Animation.SizeAnimationUsingKeyFrames", true);
			types[KnownTypes.SizeConverter] = assemblies[5].Find("System.Windows.SizeConverter", true);
			types[KnownTypes.SizeKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.SizeKeyFrame", true);
			types[KnownTypes.SizeKeyFrameCollection] = assemblies[1].Find(
				"System.Windows.Media.Animation.SizeKeyFrameCollection", true);
			types[KnownTypes.SkewTransform] = assemblies[1].Find("System.Windows.Media.SkewTransform", true);
			types[KnownTypes.SkipStoryboardToFill] = assemblies[0].Find("System.Windows.Media.Animation.SkipStoryboardToFill",
				true);
			types[KnownTypes.Slider] = assemblies[0].Find("System.Windows.Controls.Slider", true);
			types[KnownTypes.SolidColorBrush] = assemblies[1].Find("System.Windows.Media.SolidColorBrush", true);
			types[KnownTypes.SoundPlayerAction] = assemblies[0].Find("System.Windows.Controls.SoundPlayerAction", true);
			types[KnownTypes.Span] = assemblies[0].Find("System.Windows.Documents.Span", true);
			types[KnownTypes.SpecularMaterial] = assemblies[1].Find("System.Windows.Media.Media3D.SpecularMaterial", true);
			types[KnownTypes.SpellCheck] = assemblies[0].Find("System.Windows.Controls.SpellCheck", true);
			types[KnownTypes.SplineByteKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.SplineByteKeyFrame", true);
			types[KnownTypes.SplineColorKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.SplineColorKeyFrame", true);
			types[KnownTypes.SplineDecimalKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.SplineDecimalKeyFrame",
				true);
			types[KnownTypes.SplineDoubleKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.SplineDoubleKeyFrame",
				true);
			types[KnownTypes.SplineInt16KeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.SplineInt16KeyFrame", true);
			types[KnownTypes.SplineInt32KeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.SplineInt32KeyFrame", true);
			types[KnownTypes.SplineInt64KeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.SplineInt64KeyFrame", true);
			types[KnownTypes.SplinePoint3DKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.SplinePoint3DKeyFrame",
				true);
			types[KnownTypes.SplinePointKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.SplinePointKeyFrame", true);
			types[KnownTypes.SplineQuaternionKeyFrame] =
				assemblies[1].Find("System.Windows.Media.Animation.SplineQuaternionKeyFrame", true);
			types[KnownTypes.SplineRectKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.SplineRectKeyFrame", true);
			types[KnownTypes.SplineRotation3DKeyFrame] =
				assemblies[1].Find("System.Windows.Media.Animation.SplineRotation3DKeyFrame", true);
			types[KnownTypes.SplineSingleKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.SplineSingleKeyFrame",
				true);
			types[KnownTypes.SplineSizeKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.SplineSizeKeyFrame", true);
			types[KnownTypes.SplineThicknessKeyFrame] =
				assemblies[0].Find("System.Windows.Media.Animation.SplineThicknessKeyFrame", true);
			types[KnownTypes.SplineVector3DKeyFrame] = assemblies[1].Find(
				"System.Windows.Media.Animation.SplineVector3DKeyFrame", true);
			types[KnownTypes.SplineVectorKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.SplineVectorKeyFrame",
				true);
			types[KnownTypes.SpotLight] = assemblies[1].Find("System.Windows.Media.Media3D.SpotLight", true);
			types[KnownTypes.StackPanel] = assemblies[0].Find("System.Windows.Controls.StackPanel", true);
			types[KnownTypes.StaticExtension] = assemblies[2].Find("System.Windows.Markup.StaticExtension", true);
			types[KnownTypes.StaticResourceExtension] = assemblies[0].Find("System.Windows.StaticResourceExtension", true);
			types[KnownTypes.StatusBar] = assemblies[0].Find("System.Windows.Controls.Primitives.StatusBar", true);
			types[KnownTypes.StatusBarItem] = assemblies[0].Find("System.Windows.Controls.Primitives.StatusBarItem", true);
			types[KnownTypes.StickyNoteControl] = assemblies[0].Find("System.Windows.Controls.StickyNoteControl", true);
			types[KnownTypes.StopStoryboard] = assemblies[0].Find("System.Windows.Media.Animation.StopStoryboard", true);
			types[KnownTypes.Storyboard] = assemblies[0].Find("System.Windows.Media.Animation.Storyboard", true);
			types[KnownTypes.StreamGeometry] = assemblies[1].Find("System.Windows.Media.StreamGeometry", true);
			types[KnownTypes.StreamGeometryContext] = assemblies[1].Find("System.Windows.Media.StreamGeometryContext", true);
			types[KnownTypes.StreamResourceInfo] = assemblies[0].Find("System.Windows.Resources.StreamResourceInfo", true);
			types[KnownTypes.String] = assemblies[3].Find("System.String", true);
			types[KnownTypes.StringAnimationBase] = assemblies[1].Find("System.Windows.Media.Animation.StringAnimationBase", true);
			types[KnownTypes.StringAnimationUsingKeyFrames] =
				assemblies[1].Find("System.Windows.Media.Animation.StringAnimationUsingKeyFrames", true);
			types[KnownTypes.StringConverter] = assemblies[4].Find("System.ComponentModel.StringConverter", true);
			types[KnownTypes.StringKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.StringKeyFrame", true);
			types[KnownTypes.StringKeyFrameCollection] =
				assemblies[1].Find("System.Windows.Media.Animation.StringKeyFrameCollection", true);
			types[KnownTypes.StrokeCollection] = assemblies[1].Find("System.Windows.Ink.StrokeCollection", true);
			types[KnownTypes.StrokeCollectionConverter] = assemblies[1].Find("System.Windows.StrokeCollectionConverter", true);
			types[KnownTypes.Style] = assemblies[0].Find("System.Windows.Style", true);
			types[KnownTypes.Stylus] = assemblies[1].Find("System.Windows.Input.Stylus", true);
			types[KnownTypes.StylusDevice] = assemblies[1].Find("System.Windows.Input.StylusDevice", true);
			types[KnownTypes.TabControl] = assemblies[0].Find("System.Windows.Controls.TabControl", true);
			types[KnownTypes.TabItem] = assemblies[0].Find("System.Windows.Controls.TabItem", true);
			types[KnownTypes.TabPanel] = assemblies[0].Find("System.Windows.Controls.Primitives.TabPanel", true);
			types[KnownTypes.Table] = assemblies[0].Find("System.Windows.Documents.Table", true);
			types[KnownTypes.TableCell] = assemblies[0].Find("System.Windows.Documents.TableCell", true);
			types[KnownTypes.TableColumn] = assemblies[0].Find("System.Windows.Documents.TableColumn", true);
			types[KnownTypes.TableRow] = assemblies[0].Find("System.Windows.Documents.TableRow", true);
			types[KnownTypes.TableRowGroup] = assemblies[0].Find("System.Windows.Documents.TableRowGroup", true);
			types[KnownTypes.TabletDevice] = assemblies[1].Find("System.Windows.Input.TabletDevice", true);
			types[KnownTypes.TemplateBindingExpression] = assemblies[0].Find("System.Windows.TemplateBindingExpression", true);
			types[KnownTypes.TemplateBindingExpressionConverter] =
				assemblies[0].Find("System.Windows.TemplateBindingExpressionConverter", true);
			types[KnownTypes.TemplateBindingExtension] = assemblies[0].Find("System.Windows.TemplateBindingExtension", true);
			types[KnownTypes.TemplateBindingExtensionConverter] =
				assemblies[0].Find("System.Windows.TemplateBindingExtensionConverter", true);
			types[KnownTypes.TemplateKey] = assemblies[0].Find("System.Windows.TemplateKey", true);
			types[KnownTypes.TemplateKeyConverter] = assemblies[0].Find("System.Windows.Markup.TemplateKeyConverter", true);
			types[KnownTypes.TextBlock] = assemblies[0].Find("System.Windows.Controls.TextBlock", true);
			types[KnownTypes.TextBox] = assemblies[0].Find("System.Windows.Controls.TextBox", true);
			types[KnownTypes.TextBoxBase] = assemblies[0].Find("System.Windows.Controls.Primitives.TextBoxBase", true);
			types[KnownTypes.TextComposition] = assemblies[1].Find("System.Windows.Input.TextComposition", true);
			types[KnownTypes.TextCompositionManager] = assemblies[1].Find("System.Windows.Input.TextCompositionManager", true);
			types[KnownTypes.TextDecoration] = assemblies[1].Find("System.Windows.TextDecoration", true);
			types[KnownTypes.TextDecorationCollection] = assemblies[1].Find("System.Windows.TextDecorationCollection", true);
			types[KnownTypes.TextDecorationCollectionConverter] =
				assemblies[1].Find("System.Windows.TextDecorationCollectionConverter", true);
			types[KnownTypes.TextEffect] = assemblies[1].Find("System.Windows.Media.TextEffect", true);
			types[KnownTypes.TextEffectCollection] = assemblies[1].Find("System.Windows.Media.TextEffectCollection", true);
			types[KnownTypes.TextElement] = assemblies[0].Find("System.Windows.Documents.TextElement", true);
			types[KnownTypes.TextSearch] = assemblies[0].Find("System.Windows.Controls.TextSearch", true);
			types[KnownTypes.ThemeDictionaryExtension] = assemblies[0].Find("System.Windows.ThemeDictionaryExtension", true);
			types[KnownTypes.Thickness] = assemblies[0].Find("System.Windows.Thickness", true);
			types[KnownTypes.ThicknessAnimation] = assemblies[0].Find("System.Windows.Media.Animation.ThicknessAnimation", true);
			types[KnownTypes.ThicknessAnimationBase] = assemblies[0].Find(
				"System.Windows.Media.Animation.ThicknessAnimationBase", true);
			types[KnownTypes.ThicknessAnimationUsingKeyFrames] =
				assemblies[0].Find("System.Windows.Media.Animation.ThicknessAnimationUsingKeyFrames", true);
			types[KnownTypes.ThicknessConverter] = assemblies[0].Find("System.Windows.ThicknessConverter", true);
			types[KnownTypes.ThicknessKeyFrame] = assemblies[0].Find("System.Windows.Media.Animation.ThicknessKeyFrame", true);
			types[KnownTypes.ThicknessKeyFrameCollection] =
				assemblies[0].Find("System.Windows.Media.Animation.ThicknessKeyFrameCollection", true);
			types[KnownTypes.Thumb] = assemblies[0].Find("System.Windows.Controls.Primitives.Thumb", true);
			types[KnownTypes.TickBar] = assemblies[0].Find("System.Windows.Controls.Primitives.TickBar", true);
			types[KnownTypes.TiffBitmapDecoder] = assemblies[1].Find("System.Windows.Media.Imaging.TiffBitmapDecoder", true);
			types[KnownTypes.TiffBitmapEncoder] = assemblies[1].Find("System.Windows.Media.Imaging.TiffBitmapEncoder", true);
			types[KnownTypes.TileBrush] = assemblies[1].Find("System.Windows.Media.TileBrush", true);
			types[KnownTypes.TimeSpan] = assemblies[3].Find("System.TimeSpan", true);
			types[KnownTypes.TimeSpanConverter] = assemblies[4].Find("System.ComponentModel.TimeSpanConverter", true);
			types[KnownTypes.Timeline] = assemblies[1].Find("System.Windows.Media.Animation.Timeline", true);
			types[KnownTypes.TimelineCollection] = assemblies[1].Find("System.Windows.Media.Animation.TimelineCollection", true);
			types[KnownTypes.TimelineGroup] = assemblies[1].Find("System.Windows.Media.Animation.TimelineGroup", true);
			types[KnownTypes.ToggleButton] = assemblies[0].Find("System.Windows.Controls.Primitives.ToggleButton", true);
			types[KnownTypes.ToolBar] = assemblies[0].Find("System.Windows.Controls.ToolBar", true);
			types[KnownTypes.ToolBarOverflowPanel] = assemblies[0].Find(
				"System.Windows.Controls.Primitives.ToolBarOverflowPanel", true);
			types[KnownTypes.ToolBarPanel] = assemblies[0].Find("System.Windows.Controls.Primitives.ToolBarPanel", true);
			types[KnownTypes.ToolBarTray] = assemblies[0].Find("System.Windows.Controls.ToolBarTray", true);
			types[KnownTypes.ToolTip] = assemblies[0].Find("System.Windows.Controls.ToolTip", true);
			types[KnownTypes.ToolTipService] = assemblies[0].Find("System.Windows.Controls.ToolTipService", true);
			types[KnownTypes.Track] = assemblies[0].Find("System.Windows.Controls.Primitives.Track", true);
			types[KnownTypes.Transform] = assemblies[1].Find("System.Windows.Media.Transform", true);
			types[KnownTypes.Transform3D] = assemblies[1].Find("System.Windows.Media.Media3D.Transform3D", true);
			types[KnownTypes.Transform3DCollection] = assemblies[1].Find("System.Windows.Media.Media3D.Transform3DCollection",
				true);
			types[KnownTypes.Transform3DGroup] = assemblies[1].Find("System.Windows.Media.Media3D.Transform3DGroup", true);
			types[KnownTypes.TransformCollection] = assemblies[1].Find("System.Windows.Media.TransformCollection", true);
			types[KnownTypes.TransformConverter] = assemblies[1].Find("System.Windows.Media.TransformConverter", true);
			types[KnownTypes.TransformGroup] = assemblies[1].Find("System.Windows.Media.TransformGroup", true);
			types[KnownTypes.TransformedBitmap] = assemblies[1].Find("System.Windows.Media.Imaging.TransformedBitmap", true);
			types[KnownTypes.TranslateTransform] = assemblies[1].Find("System.Windows.Media.TranslateTransform", true);
			types[KnownTypes.TranslateTransform3D] = assemblies[1].Find("System.Windows.Media.Media3D.TranslateTransform3D", true);
			types[KnownTypes.TreeView] = assemblies[0].Find("System.Windows.Controls.TreeView", true);
			types[KnownTypes.TreeViewItem] = assemblies[0].Find("System.Windows.Controls.TreeViewItem", true);
			types[KnownTypes.Trigger] = assemblies[0].Find("System.Windows.Trigger", true);
			types[KnownTypes.TriggerAction] = assemblies[0].Find("System.Windows.TriggerAction", true);
			types[KnownTypes.TriggerBase] = assemblies[0].Find("System.Windows.TriggerBase", true);
			types[KnownTypes.TypeExtension] = assemblies[2].Find("System.Windows.Markup.TypeExtension", true);
			types[KnownTypes.TypeTypeConverter] = assemblies[5].Find("System.Windows.Markup.TypeTypeConverter", true);
			types[KnownTypes.Typography] = assemblies[0].Find("System.Windows.Documents.Typography", true);
			types[KnownTypes.UIElement] = assemblies[1].Find("System.Windows.UIElement", true);
			types[KnownTypes.UInt16] = assemblies[3].Find("System.UInt16", true);
			types[KnownTypes.UInt16Converter] = assemblies[4].Find("System.ComponentModel.UInt16Converter", true);
			types[KnownTypes.UInt32] = assemblies[3].Find("System.UInt32", true);
			types[KnownTypes.UInt32Converter] = assemblies[4].Find("System.ComponentModel.UInt32Converter", true);
			types[KnownTypes.UInt64] = assemblies[3].Find("System.UInt64", true);
			types[KnownTypes.UInt64Converter] = assemblies[4].Find("System.ComponentModel.UInt64Converter", true);
			types[KnownTypes.UShortIListConverter] = assemblies[1].Find("System.Windows.Media.Converters.UShortIListConverter",
				true);
			types[KnownTypes.Underline] = assemblies[0].Find("System.Windows.Documents.Underline", true);
			types[KnownTypes.UniformGrid] = assemblies[0].Find("System.Windows.Controls.Primitives.UniformGrid", true);
			types[KnownTypes.Uri] = assemblies[4].Find("System.Uri", true);
			types[KnownTypes.UriTypeConverter] = assemblies[4].Find("System.UriTypeConverter", true);
			types[KnownTypes.UserControl] = assemblies[0].Find("System.Windows.Controls.UserControl", true);
			types[KnownTypes.Validation] = assemblies[0].Find("System.Windows.Controls.Validation", true);
			types[KnownTypes.Vector] = assemblies[5].Find("System.Windows.Vector", true);
			types[KnownTypes.Vector3D] = assemblies[1].Find("System.Windows.Media.Media3D.Vector3D", true);
			types[KnownTypes.Vector3DAnimation] = assemblies[1].Find("System.Windows.Media.Animation.Vector3DAnimation", true);
			types[KnownTypes.Vector3DAnimationBase] = assemblies[1].Find("System.Windows.Media.Animation.Vector3DAnimationBase",
				true);
			types[KnownTypes.Vector3DAnimationUsingKeyFrames] =
				assemblies[1].Find("System.Windows.Media.Animation.Vector3DAnimationUsingKeyFrames", true);
			types[KnownTypes.Vector3DCollection] = assemblies[1].Find("System.Windows.Media.Media3D.Vector3DCollection", true);
			types[KnownTypes.Vector3DCollectionConverter] =
				assemblies[1].Find("System.Windows.Media.Media3D.Vector3DCollectionConverter", true);
			types[KnownTypes.Vector3DConverter] = assemblies[1].Find("System.Windows.Media.Media3D.Vector3DConverter", true);
			types[KnownTypes.Vector3DKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.Vector3DKeyFrame", true);
			types[KnownTypes.Vector3DKeyFrameCollection] =
				assemblies[1].Find("System.Windows.Media.Animation.Vector3DKeyFrameCollection", true);
			types[KnownTypes.VectorAnimation] = assemblies[1].Find("System.Windows.Media.Animation.VectorAnimation", true);
			types[KnownTypes.VectorAnimationBase] = assemblies[1].Find("System.Windows.Media.Animation.VectorAnimationBase", true);
			types[KnownTypes.VectorAnimationUsingKeyFrames] =
				assemblies[1].Find("System.Windows.Media.Animation.VectorAnimationUsingKeyFrames", true);
			types[KnownTypes.VectorCollection] = assemblies[1].Find("System.Windows.Media.VectorCollection", true);
			types[KnownTypes.VectorCollectionConverter] = assemblies[1].Find("System.Windows.Media.VectorCollectionConverter",
				true);
			types[KnownTypes.VectorConverter] = assemblies[5].Find("System.Windows.VectorConverter", true);
			types[KnownTypes.VectorKeyFrame] = assemblies[1].Find("System.Windows.Media.Animation.VectorKeyFrame", true);
			types[KnownTypes.VectorKeyFrameCollection] =
				assemblies[1].Find("System.Windows.Media.Animation.VectorKeyFrameCollection", true);
			types[KnownTypes.VideoDrawing] = assemblies[1].Find("System.Windows.Media.VideoDrawing", true);
			types[KnownTypes.ViewBase] = assemblies[0].Find("System.Windows.Controls.ViewBase", true);
			types[KnownTypes.Viewbox] = assemblies[0].Find("System.Windows.Controls.Viewbox", true);
			types[KnownTypes.Viewport3D] = assemblies[0].Find("System.Windows.Controls.Viewport3D", true);
			types[KnownTypes.Viewport3DVisual] = assemblies[1].Find("System.Windows.Media.Media3D.Viewport3DVisual", true);
			types[KnownTypes.VirtualizingPanel] = assemblies[0].Find("System.Windows.Controls.VirtualizingPanel", true);
			types[KnownTypes.VirtualizingStackPanel] = assemblies[0].Find("System.Windows.Controls.VirtualizingStackPanel", true);
			types[KnownTypes.Visual] = assemblies[1].Find("System.Windows.Media.Visual", true);
			types[KnownTypes.Visual3D] = assemblies[1].Find("System.Windows.Media.Media3D.Visual3D", true);
			types[KnownTypes.VisualBrush] = assemblies[1].Find("System.Windows.Media.VisualBrush", true);
			types[KnownTypes.VisualTarget] = assemblies[1].Find("System.Windows.Media.VisualTarget", true);
			types[KnownTypes.WeakEventManager] = assemblies[5].Find("System.Windows.WeakEventManager", true);
			types[KnownTypes.WhitespaceSignificantCollectionAttribute] =
				assemblies[2].Find("System.Windows.Markup.WhitespaceSignificantCollectionAttribute", true);
			types[KnownTypes.Window] = assemblies[0].Find("System.Windows.Window", true);
			types[KnownTypes.WmpBitmapDecoder] = assemblies[1].Find("System.Windows.Media.Imaging.WmpBitmapDecoder", true);
			types[KnownTypes.WmpBitmapEncoder] = assemblies[1].Find("System.Windows.Media.Imaging.WmpBitmapEncoder", true);
			types[KnownTypes.WrapPanel] = assemblies[0].Find("System.Windows.Controls.WrapPanel", true);
			types[KnownTypes.WriteableBitmap] = assemblies[1].Find("System.Windows.Media.Imaging.WriteableBitmap", true);
			types[KnownTypes.XamlBrushSerializer] = assemblies[0].Find("System.Windows.Markup.XamlBrushSerializer", true);
			types[KnownTypes.XamlInt32CollectionSerializer] =
				assemblies[0].Find("System.Windows.Markup.XamlInt32CollectionSerializer", true);
			types[KnownTypes.XamlPathDataSerializer] = assemblies[0].Find("System.Windows.Markup.XamlPathDataSerializer", true);
			types[KnownTypes.XamlPoint3DCollectionSerializer] =
				assemblies[0].Find("System.Windows.Markup.XamlPoint3DCollectionSerializer", true);
			types[KnownTypes.XamlPointCollectionSerializer] =
				assemblies[0].Find("System.Windows.Markup.XamlPointCollectionSerializer", true);
			types[KnownTypes.XamlReader] = assemblies[0].Find("System.Windows.Markup.XamlReader", true);
			types[KnownTypes.XamlStyleSerializer] = assemblies[0].Find("System.Windows.Markup.XamlStyleSerializer", true);
			types[KnownTypes.XamlTemplateSerializer] = assemblies[0].Find("System.Windows.Markup.XamlTemplateSerializer", true);
			types[KnownTypes.XamlVector3DCollectionSerializer] =
				assemblies[0].Find("System.Windows.Markup.XamlVector3DCollectionSerializer", true);
			types[KnownTypes.XamlWriter] = assemblies[0].Find("System.Windows.Markup.XamlWriter", true);
			types[KnownTypes.XmlDataProvider] = assemblies[0].Find("System.Windows.Data.XmlDataProvider", true);
			types[KnownTypes.XmlLangPropertyAttribute] = assemblies[2].Find("System.Windows.Markup.XmlLangPropertyAttribute",
				true);
			types[KnownTypes.XmlLanguage] = assemblies[1].Find("System.Windows.Markup.XmlLanguage", true);
			types[KnownTypes.XmlLanguageConverter] = assemblies[1].Find("System.Windows.Markup.XmlLanguageConverter", true);
			types[KnownTypes.XmlNamespaceMapping] = assemblies[0].Find("System.Windows.Data.XmlNamespaceMapping", true);
			types[KnownTypes.ZoomPercentageConverter] = assemblies[0].Find("System.Windows.Documents.ZoomPercentageConverter",
				true);
		}

		void InitProperties() {
			properties[KnownProperties.AccessText_Text] = InitProperty(KnownTypes.AccessText, "Text",
				assemblies[3].Find("System.Char", true));
			properties[KnownProperties.BeginStoryboard_Storyboard] = InitProperty(KnownTypes.BeginStoryboard, "Storyboard",
				assemblies[0].Find("System.Windows.Media.Animation.Storyboard", true));
			properties[KnownProperties.BitmapEffectGroup_Children] = InitProperty(KnownTypes.BitmapEffectGroup, "Children",
				assemblies[1].Find("System.Windows.Media.Effects.BitmapEffect", true));
			properties[KnownProperties.Border_Background] = InitProperty(KnownTypes.Border, "Background",
				assemblies[1].Find("System.Windows.Media.Brush", true));
			properties[KnownProperties.Border_BorderBrush] = InitProperty(KnownTypes.Border, "BorderBrush",
				assemblies[1].Find("System.Windows.Media.Brush", true));
			properties[KnownProperties.Border_BorderThickness] = InitProperty(KnownTypes.Border, "BorderThickness",
				assemblies[0].Find("System.Windows.Thickness", true));
			properties[KnownProperties.ButtonBase_Command] = InitProperty(KnownTypes.ButtonBase, "Command",
				assemblies[1].Find("System.Windows.Input.ICommand", true));
			properties[KnownProperties.ButtonBase_CommandParameter] = InitProperty(KnownTypes.ButtonBase, "CommandParameter",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.ButtonBase_CommandTarget] = InitProperty(KnownTypes.ButtonBase, "CommandTarget",
				assemblies[1].Find("System.Windows.IInputElement", true));
			properties[KnownProperties.ButtonBase_IsPressed] = InitProperty(KnownTypes.ButtonBase, "IsPressed",
				assemblies[3].Find("System.Boolean", true));
			properties[KnownProperties.ColumnDefinition_MaxWidth] = InitProperty(KnownTypes.ColumnDefinition, "MaxWidth",
				assemblies[3].Find("System.Double", true));
			properties[KnownProperties.ColumnDefinition_MinWidth] = InitProperty(KnownTypes.ColumnDefinition, "MinWidth",
				assemblies[3].Find("System.Double", true));
			properties[KnownProperties.ColumnDefinition_Width] = InitProperty(KnownTypes.ColumnDefinition, "Width",
				assemblies[0].Find("System.Windows.GridLength", true));
			properties[KnownProperties.ContentControl_Content] = InitProperty(KnownTypes.ContentControl, "Content",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.ContentControl_ContentTemplate] = InitProperty(KnownTypes.ContentControl,
				"ContentTemplate", assemblies[0].Find("System.Windows.DataTemplate", true));
			properties[KnownProperties.ContentControl_ContentTemplateSelector] = InitProperty(KnownTypes.ContentControl,
				"ContentTemplateSelector", assemblies[0].Find("System.Windows.Controls.DataTemplateSelector", true));
			properties[KnownProperties.ContentControl_HasContent] = InitProperty(KnownTypes.ContentControl, "HasContent",
				assemblies[3].Find("System.Boolean", true));
			properties[KnownProperties.ContentElement_Focusable] = InitProperty(KnownTypes.ContentElement, "Focusable",
				assemblies[3].Find("System.Boolean", true));
			properties[KnownProperties.ContentPresenter_Content] = InitProperty(KnownTypes.ContentPresenter, "Content",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.ContentPresenter_ContentSource] = InitProperty(KnownTypes.ContentPresenter,
				"ContentSource", assemblies[3].Find("System.Char", true));
			properties[KnownProperties.ContentPresenter_ContentTemplate] = InitProperty(KnownTypes.ContentPresenter,
				"ContentTemplate", assemblies[0].Find("System.Windows.DataTemplate", true));
			properties[KnownProperties.ContentPresenter_ContentTemplateSelector] = InitProperty(KnownTypes.ContentPresenter,
				"ContentTemplateSelector", assemblies[0].Find("System.Windows.Controls.DataTemplateSelector", true));
			properties[KnownProperties.ContentPresenter_RecognizesAccessKey] = InitProperty(KnownTypes.ContentPresenter,
				"RecognizesAccessKey", assemblies[3].Find("System.Boolean", true));
			properties[KnownProperties.Control_Background] = InitProperty(KnownTypes.Control, "Background",
				assemblies[1].Find("System.Windows.Media.Brush", true));
			properties[KnownProperties.Control_BorderBrush] = InitProperty(KnownTypes.Control, "BorderBrush",
				assemblies[1].Find("System.Windows.Media.Brush", true));
			properties[KnownProperties.Control_BorderThickness] = InitProperty(KnownTypes.Control, "BorderThickness",
				assemblies[0].Find("System.Windows.Thickness", true));
			properties[KnownProperties.Control_FontFamily] = InitProperty(KnownTypes.Control, "FontFamily",
				assemblies[1].Find("System.Windows.Media.FontFamily", true));
			properties[KnownProperties.Control_FontSize] = InitProperty(KnownTypes.Control, "FontSize",
				assemblies[3].Find("System.Double", true));
			properties[KnownProperties.Control_FontStretch] = InitProperty(KnownTypes.Control, "FontStretch",
				assemblies[1].Find("System.Windows.FontStretch", true));
			properties[KnownProperties.Control_FontStyle] = InitProperty(KnownTypes.Control, "FontStyle",
				assemblies[1].Find("System.Windows.FontStyle", true));
			properties[KnownProperties.Control_FontWeight] = InitProperty(KnownTypes.Control, "FontWeight",
				assemblies[1].Find("System.Windows.FontWeight", true));
			properties[KnownProperties.Control_Foreground] = InitProperty(KnownTypes.Control, "Foreground",
				assemblies[1].Find("System.Windows.Media.Brush", true));
			properties[KnownProperties.Control_HorizontalContentAlignment] = InitProperty(KnownTypes.Control,
				"HorizontalContentAlignment", assemblies[0].Find("System.Windows.HorizontalAlignment", true));
			properties[KnownProperties.Control_IsTabStop] = InitProperty(KnownTypes.Control, "IsTabStop",
				assemblies[3].Find("System.Boolean", true));
			properties[KnownProperties.Control_Padding] = InitProperty(KnownTypes.Control, "Padding",
				assemblies[0].Find("System.Windows.Thickness", true));
			properties[KnownProperties.Control_TabIndex] = InitProperty(KnownTypes.Control, "TabIndex",
				assemblies[3].Find("System.Int32", true));
			properties[KnownProperties.Control_Template] = InitProperty(KnownTypes.Control, "Template",
				assemblies[0].Find("System.Windows.Controls.ControlTemplate", true));
			properties[KnownProperties.Control_VerticalContentAlignment] = InitProperty(KnownTypes.Control,
				"VerticalContentAlignment", assemblies[0].Find("System.Windows.VerticalAlignment", true));
			properties[KnownProperties.DockPanel_Dock] = InitProperty(KnownTypes.DockPanel, "Dock",
				assemblies[0].Find("System.Windows.Controls.Dock", true));
			properties[KnownProperties.DockPanel_LastChildFill] = InitProperty(KnownTypes.DockPanel, "LastChildFill",
				assemblies[3].Find("System.Boolean", true));
			properties[KnownProperties.DocumentViewerBase_Document] = InitProperty(KnownTypes.DocumentViewerBase, "Document",
				assemblies[1].Find("System.Windows.Documents.IDocumentPaginatorSource", true));
			properties[KnownProperties.DrawingGroup_Children] = InitProperty(KnownTypes.DrawingGroup, "Children",
				assemblies[1].Find("System.Windows.Media.Drawing", true));
			properties[KnownProperties.FlowDocumentReader_Document] = InitProperty(KnownTypes.FlowDocumentReader, "Document",
				assemblies[0].Find("System.Windows.Documents.FlowDocument", true));
			properties[KnownProperties.FlowDocumentScrollViewer_Document] = InitProperty(KnownTypes.FlowDocumentScrollViewer,
				"Document", assemblies[0].Find("System.Windows.Documents.FlowDocument", true));
			properties[KnownProperties.FrameworkContentElement_Style] = InitProperty(KnownTypes.FrameworkContentElement, "Style",
				assemblies[0].Find("System.Windows.Style", true));
			properties[KnownProperties.FrameworkElement_FlowDirection] = InitProperty(KnownTypes.FrameworkElement,
				"FlowDirection", assemblies[1].Find("System.Windows.FlowDirection", true));
			properties[KnownProperties.FrameworkElement_Height] = InitProperty(KnownTypes.FrameworkElement, "Height",
				assemblies[3].Find("System.Double", true));
			properties[KnownProperties.FrameworkElement_HorizontalAlignment] = InitProperty(KnownTypes.FrameworkElement,
				"HorizontalAlignment", assemblies[0].Find("System.Windows.HorizontalAlignment", true));
			properties[KnownProperties.FrameworkElement_Margin] = InitProperty(KnownTypes.FrameworkElement, "Margin",
				assemblies[0].Find("System.Windows.Thickness", true));
			properties[KnownProperties.FrameworkElement_MaxHeight] = InitProperty(KnownTypes.FrameworkElement, "MaxHeight",
				assemblies[3].Find("System.Double", true));
			properties[KnownProperties.FrameworkElement_MaxWidth] = InitProperty(KnownTypes.FrameworkElement, "MaxWidth",
				assemblies[3].Find("System.Double", true));
			properties[KnownProperties.FrameworkElement_MinHeight] = InitProperty(KnownTypes.FrameworkElement, "MinHeight",
				assemblies[3].Find("System.Double", true));
			properties[KnownProperties.FrameworkElement_MinWidth] = InitProperty(KnownTypes.FrameworkElement, "MinWidth",
				assemblies[3].Find("System.Double", true));
			properties[KnownProperties.FrameworkElement_Name] = InitProperty(KnownTypes.FrameworkElement, "Name",
				assemblies[3].Find("System.Char", true));
			properties[KnownProperties.FrameworkElement_Style] = InitProperty(KnownTypes.FrameworkElement, "Style",
				assemblies[0].Find("System.Windows.Style", true));
			properties[KnownProperties.FrameworkElement_VerticalAlignment] = InitProperty(KnownTypes.FrameworkElement,
				"VerticalAlignment", assemblies[0].Find("System.Windows.VerticalAlignment", true));
			properties[KnownProperties.FrameworkElement_Width] = InitProperty(KnownTypes.FrameworkElement, "Width",
				assemblies[3].Find("System.Double", true));
			properties[KnownProperties.GeneralTransformGroup_Children] = InitProperty(KnownTypes.GeneralTransformGroup,
				"Children", assemblies[1].Find("System.Windows.Media.GeneralTransform", true));
			properties[KnownProperties.GeometryGroup_Children] = InitProperty(KnownTypes.GeometryGroup, "Children",
				assemblies[1].Find("System.Windows.Media.Geometry", true));
			properties[KnownProperties.GradientBrush_GradientStops] = InitProperty(KnownTypes.GradientBrush, "GradientStops",
				assemblies[1].Find("System.Windows.Media.GradientStop", true));
			properties[KnownProperties.Grid_Column] = InitProperty(KnownTypes.Grid, "Column",
				assemblies[3].Find("System.Int32", true));
			properties[KnownProperties.Grid_ColumnSpan] = InitProperty(KnownTypes.Grid, "ColumnSpan",
				assemblies[3].Find("System.Int32", true));
			properties[KnownProperties.Grid_Row] = InitProperty(KnownTypes.Grid, "Grid_Row",
				assemblies[3].Find("System.Int32", true));
			properties[KnownProperties.Grid_RowSpan] = InitProperty(KnownTypes.Grid, "RowSpan",
				assemblies[3].Find("System.Int32", true));
			properties[KnownProperties.GridViewColumn_Header] = InitProperty(KnownTypes.GridViewColumn, "Header",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.HeaderedContentControl_HasHeader] = InitProperty(KnownTypes.HeaderedContentControl,
				"HasHeader", assemblies[3].Find("System.Boolean", true));
			properties[KnownProperties.HeaderedContentControl_Header] = InitProperty(KnownTypes.HeaderedContentControl, "Header",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.HeaderedContentControl_HeaderTemplate] = InitProperty(KnownTypes.HeaderedContentControl,
				"HeaderTemplate", assemblies[0].Find("System.Windows.DataTemplate", true));
			properties[KnownProperties.HeaderedContentControl_HeaderTemplateSelector] =
				InitProperty(KnownTypes.HeaderedContentControl, "HeaderTemplateSelector",
					assemblies[0].Find("System.Windows.Controls.DataTemplateSelector", true));
			properties[KnownProperties.HeaderedItemsControl_HasHeader] = InitProperty(KnownTypes.HeaderedItemsControl,
				"HasHeader", assemblies[3].Find("System.Boolean", true));
			properties[KnownProperties.HeaderedItemsControl_Header] = InitProperty(KnownTypes.HeaderedItemsControl, "Header",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.HeaderedItemsControl_HeaderTemplate] = InitProperty(KnownTypes.HeaderedItemsControl,
				"HeaderTemplate", assemblies[0].Find("System.Windows.DataTemplate", true));
			properties[KnownProperties.HeaderedItemsControl_HeaderTemplateSelector] =
				InitProperty(KnownTypes.HeaderedItemsControl, "HeaderTemplateSelector",
					assemblies[0].Find("System.Windows.Controls.DataTemplateSelector", true));
			properties[KnownProperties.Hyperlink_NavigateUri] = InitProperty(KnownTypes.Hyperlink, "NavigateUri",
				assemblies[4].Find("System.Uri", true));
			properties[KnownProperties.Image_Source] = InitProperty(KnownTypes.Image, "Source",
				assemblies[1].Find("System.Windows.Media.ImageSource", true));
			properties[KnownProperties.Image_Stretch] = InitProperty(KnownTypes.Image, "Stretch",
				assemblies[1].Find("System.Windows.Media.Stretch", true));
			properties[KnownProperties.ItemsControl_ItemContainerStyle] = InitProperty(KnownTypes.ItemsControl,
				"ItemContainerStyle", assemblies[0].Find("System.Windows.Style", true));
			properties[KnownProperties.ItemsControl_ItemContainerStyleSelector] = InitProperty(KnownTypes.ItemsControl,
				"ItemContainerStyleSelector", assemblies[0].Find("System.Windows.Controls.StyleSelector", true));
			properties[KnownProperties.ItemsControl_ItemTemplate] = InitProperty(KnownTypes.ItemsControl, "ItemTemplate",
				assemblies[0].Find("System.Windows.DataTemplate", true));
			properties[KnownProperties.ItemsControl_ItemTemplateSelector] = InitProperty(KnownTypes.ItemsControl,
				"ItemTemplateSelector", assemblies[0].Find("System.Windows.Controls.DataTemplateSelector", true));
			properties[KnownProperties.ItemsControl_ItemsPanel] = InitProperty(KnownTypes.ItemsControl, "ItemsPanel",
				assemblies[0].Find("System.Windows.Controls.ItemsPanelTemplate", true));
			properties[KnownProperties.ItemsControl_ItemsSource] = InitProperty(KnownTypes.ItemsControl, "ItemsSource",
				assemblies[3].Find("System.Collections.IEnumerable", true));
			properties[KnownProperties.MaterialGroup_Children] = InitProperty(KnownTypes.MaterialGroup, "Children",
				assemblies[1].Find("System.Windows.Media.Media3D.Material", true));
			properties[KnownProperties.Model3DGroup_Children] = InitProperty(KnownTypes.Model3DGroup, "Children",
				assemblies[1].Find("System.Windows.Media.Media3D.Model3D", true));
			properties[KnownProperties.Page_Content] = InitProperty(KnownTypes.Page, "Content",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.Panel_Background] = InitProperty(KnownTypes.Panel, "Background",
				assemblies[1].Find("System.Windows.Media.Brush", true));
			properties[KnownProperties.Path_Data] = InitProperty(KnownTypes.Path, "Data",
				assemblies[1].Find("System.Windows.Media.Geometry", true));
			properties[KnownProperties.PathFigure_Segments] = InitProperty(KnownTypes.PathFigure, "Segments",
				assemblies[1].Find("System.Windows.Media.PathSegment", true));
			properties[KnownProperties.PathGeometry_Figures] = InitProperty(KnownTypes.PathGeometry, "Figures",
				assemblies[1].Find("System.Windows.Media.PathFigure", true));
			properties[KnownProperties.Popup_Child] = InitProperty(KnownTypes.Popup, "Child",
				assemblies[1].Find("System.Windows.UIElement", true));
			properties[KnownProperties.Popup_IsOpen] = InitProperty(KnownTypes.Popup, "IsOpen",
				assemblies[3].Find("System.Boolean", true));
			properties[KnownProperties.Popup_Placement] = InitProperty(KnownTypes.Popup, "Placement",
				assemblies[0].Find("System.Windows.Controls.Primitives.PlacementMode", true));
			properties[KnownProperties.Popup_PopupAnimation] = InitProperty(KnownTypes.Popup, "PopupAnimation",
				assemblies[0].Find("System.Windows.Controls.Primitives.PopupAnimation", true));
			properties[KnownProperties.RowDefinition_Height] = InitProperty(KnownTypes.RowDefinition, "Height",
				assemblies[0].Find("System.Windows.GridLength", true));
			properties[KnownProperties.RowDefinition_MaxHeight] = InitProperty(KnownTypes.RowDefinition, "MaxHeight",
				assemblies[3].Find("System.Double", true));
			properties[KnownProperties.RowDefinition_MinHeight] = InitProperty(KnownTypes.RowDefinition, "MinHeight",
				assemblies[3].Find("System.Double", true));
			properties[KnownProperties.ScrollViewer_CanContentScroll] = InitProperty(KnownTypes.ScrollViewer, "CanContentScroll",
				assemblies[3].Find("System.Boolean", true));
			properties[KnownProperties.ScrollViewer_HorizontalScrollBarVisibility] = InitProperty(KnownTypes.ScrollViewer,
				"HorizontalScrollBarVisibility", assemblies[0].Find("System.Windows.Controls.ScrollBarVisibility", true));
			properties[KnownProperties.ScrollViewer_VerticalScrollBarVisibility] = InitProperty(KnownTypes.ScrollViewer,
				"VerticalScrollBarVisibility", assemblies[0].Find("System.Windows.Controls.ScrollBarVisibility", true));
			properties[KnownProperties.Shape_Fill] = InitProperty(KnownTypes.Shape, "Fill",
				assemblies[1].Find("System.Windows.Media.Brush", true));
			properties[KnownProperties.Shape_Stroke] = InitProperty(KnownTypes.Shape, "Stroke",
				assemblies[1].Find("System.Windows.Media.Brush", true));
			properties[KnownProperties.Shape_StrokeThickness] = InitProperty(KnownTypes.Shape, "StrokeThickness",
				assemblies[3].Find("System.Double", true));
			properties[KnownProperties.TextBlock_Background] = InitProperty(KnownTypes.TextBlock, "Background",
				assemblies[1].Find("System.Windows.Media.Brush", true));
			properties[KnownProperties.TextBlock_FontFamily] = InitProperty(KnownTypes.TextBlock, "FontFamily",
				assemblies[1].Find("System.Windows.Media.FontFamily", true));
			properties[KnownProperties.TextBlock_FontSize] = InitProperty(KnownTypes.TextBlock, "FontSize",
				assemblies[3].Find("System.Double", true));
			properties[KnownProperties.TextBlock_FontStretch] = InitProperty(KnownTypes.TextBlock, "FontStretch",
				assemblies[1].Find("System.Windows.FontStretch", true));
			properties[KnownProperties.TextBlock_FontStyle] = InitProperty(KnownTypes.TextBlock, "FontStyle",
				assemblies[1].Find("System.Windows.FontStyle", true));
			properties[KnownProperties.TextBlock_FontWeight] = InitProperty(KnownTypes.TextBlock, "FontWeight",
				assemblies[1].Find("System.Windows.FontWeight", true));
			properties[KnownProperties.TextBlock_Foreground] = InitProperty(KnownTypes.TextBlock, "Foreground",
				assemblies[1].Find("System.Windows.Media.Brush", true));
			properties[KnownProperties.TextBlock_Text] = InitProperty(KnownTypes.TextBlock, "Text",
				assemblies[3].Find("System.Char", true));
			properties[KnownProperties.TextBlock_TextDecorations] = InitProperty(KnownTypes.TextBlock, "TextDecorations",
				assemblies[1].Find("System.Windows.TextDecoration", true));
			properties[KnownProperties.TextBlock_TextTrimming] = InitProperty(KnownTypes.TextBlock, "TextTrimming",
				assemblies[1].Find("System.Windows.TextTrimming", true));
			properties[KnownProperties.TextBlock_TextWrapping] = InitProperty(KnownTypes.TextBlock, "TextWrapping",
				assemblies[1].Find("System.Windows.TextWrapping", true));
			properties[KnownProperties.TextBox_Text] = InitProperty(KnownTypes.TextBox, "Text",
				assemblies[3].Find("System.Char", true));
			properties[KnownProperties.TextElement_Background] = InitProperty(KnownTypes.TextElement, "Background",
				assemblies[1].Find("System.Windows.Media.Brush", true));
			properties[KnownProperties.TextElement_FontFamily] = InitProperty(KnownTypes.TextElement, "FontFamily",
				assemblies[1].Find("System.Windows.Media.FontFamily", true));
			properties[KnownProperties.TextElement_FontSize] = InitProperty(KnownTypes.TextElement, "FontSize",
				assemblies[3].Find("System.Double", true));
			properties[KnownProperties.TextElement_FontStretch] = InitProperty(KnownTypes.TextElement, "FontStretch",
				assemblies[1].Find("System.Windows.FontStretch", true));
			properties[KnownProperties.TextElement_FontStyle] = InitProperty(KnownTypes.TextElement, "FontStyle",
				assemblies[1].Find("System.Windows.FontStyle", true));
			properties[KnownProperties.TextElement_FontWeight] = InitProperty(KnownTypes.TextElement, "FontWeight",
				assemblies[1].Find("System.Windows.FontWeight", true));
			properties[KnownProperties.TextElement_Foreground] = InitProperty(KnownTypes.TextElement, "Foreground",
				assemblies[1].Find("System.Windows.Media.Brush", true));
			properties[KnownProperties.TimelineGroup_Children] = InitProperty(KnownTypes.TimelineGroup, "Children",
				assemblies[1].Find("System.Windows.Media.Animation.Timeline", true));
			properties[KnownProperties.Track_IsDirectionReversed] = InitProperty(KnownTypes.Track, "IsDirectionReversed",
				assemblies[3].Find("System.Boolean", true));
			properties[KnownProperties.Track_Maximum] = InitProperty(KnownTypes.Track, "Maximum",
				assemblies[3].Find("System.Double", true));
			properties[KnownProperties.Track_Minimum] = InitProperty(KnownTypes.Track, "Minimum",
				assemblies[3].Find("System.Double", true));
			properties[KnownProperties.Track_Orientation] = InitProperty(KnownTypes.Track, "Orientation",
				assemblies[0].Find("System.Windows.Controls.Orientation", true));
			properties[KnownProperties.Track_Value] = InitProperty(KnownTypes.Track, "Value",
				assemblies[3].Find("System.Double", true));
			properties[KnownProperties.Track_ViewportSize] = InitProperty(KnownTypes.Track, "ViewportSize",
				assemblies[3].Find("System.Double", true));
			properties[KnownProperties.Transform3DGroup_Children] = InitProperty(KnownTypes.Transform3DGroup, "Children",
				assemblies[1].Find("System.Windows.Media.Media3D.Transform3D", true));
			properties[KnownProperties.TransformGroup_Children] = InitProperty(KnownTypes.TransformGroup, "Children",
				assemblies[1].Find("System.Windows.Media.Transform", true));
			properties[KnownProperties.UIElement_ClipToBounds] = InitProperty(KnownTypes.UIElement, "ClipToBounds",
				assemblies[3].Find("System.Boolean", true));
			properties[KnownProperties.UIElement_Focusable] = InitProperty(KnownTypes.UIElement, "Focusable",
				assemblies[3].Find("System.Boolean", true));
			properties[KnownProperties.UIElement_IsEnabled] = InitProperty(KnownTypes.UIElement, "IsEnabled",
				assemblies[3].Find("System.Boolean", true));
			properties[KnownProperties.UIElement_RenderTransform] = InitProperty(KnownTypes.UIElement, "RenderTransform",
				assemblies[1].Find("System.Windows.Media.Transform", true));
			properties[KnownProperties.UIElement_Visibility] = InitProperty(KnownTypes.UIElement, "Visibility",
				assemblies[1].Find("System.Windows.Visibility", true));
			properties[KnownProperties.Viewport3D_Children] = InitProperty(KnownTypes.Viewport3D, "Children",
				assemblies[1].Find("System.Windows.Media.Media3D.Visual3D", true));
			properties[KnownProperties.AdornedElementPlaceholder_Child] = InitProperty(KnownTypes.AdornedElementPlaceholder,
				"Child", assemblies[1].Find("System.Windows.UIElement", true));
			properties[KnownProperties.AdornerDecorator_Child] = InitProperty(KnownTypes.AdornerDecorator, "Child",
				assemblies[1].Find("System.Windows.UIElement", true));
			properties[KnownProperties.AnchoredBlock_Blocks] = InitProperty(KnownTypes.AnchoredBlock, "Blocks",
				assemblies[0].Find("System.Windows.Documents.Block", true));
			properties[KnownProperties.ArrayExtension_Items] = InitProperty(KnownTypes.ArrayExtension, "Items",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.BlockUIContainer_Child] = InitProperty(KnownTypes.BlockUIContainer, "Child",
				assemblies[1].Find("System.Windows.UIElement", true));
			properties[KnownProperties.Bold_Inlines] = InitProperty(KnownTypes.Bold, "Inlines",
				assemblies[0].Find("System.Windows.Documents.Inline", true));
			properties[KnownProperties.BooleanAnimationUsingKeyFrames_KeyFrames] =
				InitProperty(KnownTypes.BooleanAnimationUsingKeyFrames, "KeyFrames",
					assemblies[1].Find("System.Windows.Media.Animation.BooleanKeyFrame", true));
			properties[KnownProperties.Border_Child] = InitProperty(KnownTypes.Border, "Child",
				assemblies[1].Find("System.Windows.UIElement", true));
			properties[KnownProperties.BulletDecorator_Child] = InitProperty(KnownTypes.BulletDecorator, "Child",
				assemblies[1].Find("System.Windows.UIElement", true));
			properties[KnownProperties.Button_Content] = InitProperty(KnownTypes.Button, "Content",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.ButtonBase_Content] = InitProperty(KnownTypes.ButtonBase, "Content",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.ByteAnimationUsingKeyFrames_KeyFrames] =
				InitProperty(KnownTypes.ByteAnimationUsingKeyFrames, "KeyFrames",
					assemblies[1].Find("System.Windows.Media.Animation.ByteKeyFrame", true));
			properties[KnownProperties.Canvas_Children] = InitProperty(KnownTypes.Canvas, "Children",
				assemblies[1].Find("System.Windows.UIElement", true));
			properties[KnownProperties.CharAnimationUsingKeyFrames_KeyFrames] =
				InitProperty(KnownTypes.CharAnimationUsingKeyFrames, "KeyFrames",
					assemblies[1].Find("System.Windows.Media.Animation.CharKeyFrame", true));
			properties[KnownProperties.CheckBox_Content] = InitProperty(KnownTypes.CheckBox, "Content",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.ColorAnimationUsingKeyFrames_KeyFrames] =
				InitProperty(KnownTypes.ColorAnimationUsingKeyFrames, "KeyFrames",
					assemblies[1].Find("System.Windows.Media.Animation.ColorKeyFrame", true));
			properties[KnownProperties.ComboBox_Items] = InitProperty(KnownTypes.ComboBox, "Items",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.ComboBoxItem_Content] = InitProperty(KnownTypes.ComboBoxItem, "Content",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.ContextMenu_Items] = InitProperty(KnownTypes.ContextMenu, "Items",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.ControlTemplate_VisualTree] = InitProperty(KnownTypes.ControlTemplate, "VisualTree",
				assemblies[0].Find("System.Windows.FrameworkElementFactory", true));
			properties[KnownProperties.DataTemplate_VisualTree] = InitProperty(KnownTypes.DataTemplate, "VisualTree",
				assemblies[0].Find("System.Windows.FrameworkElementFactory", true));
			properties[KnownProperties.DataTrigger_Setters] = InitProperty(KnownTypes.DataTrigger, "Setters",
				assemblies[0].Find("System.Windows.SetterBase", true));
			properties[KnownProperties.DecimalAnimationUsingKeyFrames_KeyFrames] =
				InitProperty(KnownTypes.DecimalAnimationUsingKeyFrames, "KeyFrames",
					assemblies[1].Find("System.Windows.Media.Animation.DecimalKeyFrame", true));
			properties[KnownProperties.Decorator_Child] = InitProperty(KnownTypes.Decorator, "Child",
				assemblies[1].Find("System.Windows.UIElement", true));
			properties[KnownProperties.DockPanel_Children] = InitProperty(KnownTypes.DockPanel, "Children",
				assemblies[1].Find("System.Windows.UIElement", true));
			properties[KnownProperties.DocumentViewer_Document] = InitProperty(KnownTypes.DocumentViewer, "Document",
				assemblies[1].Find("System.Windows.Documents.IDocumentPaginatorSource", true));
			properties[KnownProperties.DoubleAnimationUsingKeyFrames_KeyFrames] =
				InitProperty(KnownTypes.DoubleAnimationUsingKeyFrames, "KeyFrames",
					assemblies[1].Find("System.Windows.Media.Animation.DoubleKeyFrame", true));
			properties[KnownProperties.EventTrigger_Actions] = InitProperty(KnownTypes.EventTrigger, "Actions",
				assemblies[0].Find("System.Windows.TriggerAction", true));
			properties[KnownProperties.Expander_Content] = InitProperty(KnownTypes.Expander, "Content",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.Figure_Blocks] = InitProperty(KnownTypes.Figure, "Blocks",
				assemblies[0].Find("System.Windows.Documents.Block", true));
			properties[KnownProperties.FixedDocument_Pages] = InitProperty(KnownTypes.FixedDocument, "Pages",
				assemblies[0].Find("System.Windows.Documents.PageContent", true));
			properties[KnownProperties.FixedDocumentSequence_References] = InitProperty(KnownTypes.FixedDocumentSequence,
				"References", assemblies[0].Find("System.Windows.Documents.DocumentReference", true));
			properties[KnownProperties.FixedPage_Children] = InitProperty(KnownTypes.FixedPage, "Children",
				assemblies[1].Find("System.Windows.UIElement", true));
			properties[KnownProperties.Floater_Blocks] = InitProperty(KnownTypes.Floater, "Blocks",
				assemblies[0].Find("System.Windows.Documents.Block", true));
			properties[KnownProperties.FlowDocument_Blocks] = InitProperty(KnownTypes.FlowDocument, "Blocks",
				assemblies[0].Find("System.Windows.Documents.Block", true));
			properties[KnownProperties.FlowDocumentPageViewer_Document] = InitProperty(KnownTypes.FlowDocumentPageViewer,
				"Document", assemblies[1].Find("System.Windows.Documents.IDocumentPaginatorSource", true));
			properties[KnownProperties.FrameworkTemplate_VisualTree] = InitProperty(KnownTypes.FrameworkTemplate, "VisualTree",
				assemblies[0].Find("System.Windows.FrameworkElementFactory", true));
			properties[KnownProperties.Grid_Children] = InitProperty(KnownTypes.Grid, "Children",
				assemblies[1].Find("System.Windows.UIElement", true));
			properties[KnownProperties.GridView_Columns] = InitProperty(KnownTypes.GridView, "Columns",
				assemblies[0].Find("System.Windows.Controls.GridViewColumn", true));
			properties[KnownProperties.GridViewColumnHeader_Content] = InitProperty(KnownTypes.GridViewColumnHeader, "Content",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.GroupBox_Content] = InitProperty(KnownTypes.GroupBox, "Content",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.GroupItem_Content] = InitProperty(KnownTypes.GroupItem, "Content",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.HeaderedContentControl_Content] = InitProperty(KnownTypes.HeaderedContentControl,
				"Content", assemblies[3].Find("System.Object", true));
			properties[KnownProperties.HeaderedItemsControl_Items] = InitProperty(KnownTypes.HeaderedItemsControl, "Items",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.HierarchicalDataTemplate_VisualTree] = InitProperty(KnownTypes.HierarchicalDataTemplate,
				"VisualTree", assemblies[0].Find("System.Windows.FrameworkElementFactory", true));
			properties[KnownProperties.Hyperlink_Inlines] = InitProperty(KnownTypes.Hyperlink, "Inlines",
				assemblies[0].Find("System.Windows.Documents.Inline", true));
			properties[KnownProperties.InkCanvas_Children] = InitProperty(KnownTypes.InkCanvas, "Children",
				assemblies[1].Find("System.Windows.UIElement", true));
			properties[KnownProperties.InkPresenter_Child] = InitProperty(KnownTypes.InkPresenter, "Child",
				assemblies[1].Find("System.Windows.UIElement", true));
			properties[KnownProperties.InlineUIContainer_Child] = InitProperty(KnownTypes.InlineUIContainer, "Child",
				assemblies[1].Find("System.Windows.UIElement", true));
			properties[KnownProperties.InputScopeName_NameValue] = InitProperty(KnownTypes.InputScopeName, "NameValue",
				assemblies[1].Find("System.Windows.Input.InputScopeNameValue", true));
			properties[KnownProperties.Int16AnimationUsingKeyFrames_KeyFrames] =
				InitProperty(KnownTypes.Int16AnimationUsingKeyFrames, "KeyFrames",
					assemblies[1].Find("System.Windows.Media.Animation.Int16KeyFrame", true));
			properties[KnownProperties.Int32AnimationUsingKeyFrames_KeyFrames] =
				InitProperty(KnownTypes.Int32AnimationUsingKeyFrames, "KeyFrames",
					assemblies[1].Find("System.Windows.Media.Animation.Int32KeyFrame", true));
			properties[KnownProperties.Int64AnimationUsingKeyFrames_KeyFrames] =
				InitProperty(KnownTypes.Int64AnimationUsingKeyFrames, "KeyFrames",
					assemblies[1].Find("System.Windows.Media.Animation.Int64KeyFrame", true));
			properties[KnownProperties.Italic_Inlines] = InitProperty(KnownTypes.Italic, "Inlines",
				assemblies[0].Find("System.Windows.Documents.Inline", true));
			properties[KnownProperties.ItemsControl_Items] = InitProperty(KnownTypes.ItemsControl, "Items",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.ItemsPanelTemplate_VisualTree] = InitProperty(KnownTypes.ItemsPanelTemplate, "VisualTree",
				assemblies[0].Find("System.Windows.FrameworkElementFactory", true));
			properties[KnownProperties.Label_Content] = InitProperty(KnownTypes.Label, "Content",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.LinearGradientBrush_GradientStops] = InitProperty(KnownTypes.LinearGradientBrush,
				"GradientStops", assemblies[1].Find("System.Windows.Media.GradientStop", true));
			properties[KnownProperties.List_ListItems] = InitProperty(KnownTypes.List, "ListItems",
				assemblies[0].Find("System.Windows.Documents.ListItem", true));
			properties[KnownProperties.ListBox_Items] = InitProperty(KnownTypes.ListBox, "Items",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.ListBoxItem_Content] = InitProperty(KnownTypes.ListBoxItem, "Content",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.ListItem_Blocks] = InitProperty(KnownTypes.ListItem, "Blocks",
				assemblies[0].Find("System.Windows.Documents.Block", true));
			properties[KnownProperties.ListView_Items] = InitProperty(KnownTypes.ListView, "Items",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.ListViewItem_Content] = InitProperty(KnownTypes.ListViewItem, "Content",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.MatrixAnimationUsingKeyFrames_KeyFrames] =
				InitProperty(KnownTypes.MatrixAnimationUsingKeyFrames, "KeyFrames",
					assemblies[1].Find("System.Windows.Media.Animation.MatrixKeyFrame", true));
			properties[KnownProperties.Menu_Items] = InitProperty(KnownTypes.Menu, "Items",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.MenuBase_Items] = InitProperty(KnownTypes.MenuBase, "Items",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.MenuItem_Items] = InitProperty(KnownTypes.MenuItem, "Items",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.ModelVisual3D_Children] = InitProperty(KnownTypes.ModelVisual3D, "Children",
				assemblies[1].Find("System.Windows.Media.Media3D.Visual3D", true));
			properties[KnownProperties.MultiBinding_Bindings] = InitProperty(KnownTypes.MultiBinding, "Bindings",
				assemblies[0].Find("System.Windows.Data.BindingBase", true));
			properties[KnownProperties.MultiDataTrigger_Setters] = InitProperty(KnownTypes.MultiDataTrigger, "Setters",
				assemblies[0].Find("System.Windows.SetterBase", true));
			properties[KnownProperties.MultiTrigger_Setters] = InitProperty(KnownTypes.MultiTrigger, "Setters",
				assemblies[0].Find("System.Windows.SetterBase", true));
			properties[KnownProperties.ObjectAnimationUsingKeyFrames_KeyFrames] =
				InitProperty(KnownTypes.ObjectAnimationUsingKeyFrames, "KeyFrames",
					assemblies[1].Find("System.Windows.Media.Animation.ObjectKeyFrame", true));
			properties[KnownProperties.PageContent_Child] = InitProperty(KnownTypes.PageContent, "Child",
				assemblies[0].Find("System.Windows.Documents.FixedPage", true));
			properties[KnownProperties.PageFunctionBase_Content] = InitProperty(KnownTypes.PageFunctionBase, "Content",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.Panel_Children] = InitProperty(KnownTypes.Panel, "Children",
				assemblies[1].Find("System.Windows.UIElement", true));
			properties[KnownProperties.Paragraph_Inlines] = InitProperty(KnownTypes.Paragraph, "Inlines",
				assemblies[0].Find("System.Windows.Documents.Inline", true));
			properties[KnownProperties.ParallelTimeline_Children] = InitProperty(KnownTypes.ParallelTimeline, "Children",
				assemblies[1].Find("System.Windows.Media.Animation.Timeline", true));
			properties[KnownProperties.Point3DAnimationUsingKeyFrames_KeyFrames] =
				InitProperty(KnownTypes.Point3DAnimationUsingKeyFrames, "KeyFrames",
					assemblies[1].Find("System.Windows.Media.Animation.Point3DKeyFrame", true));
			properties[KnownProperties.PointAnimationUsingKeyFrames_KeyFrames] =
				InitProperty(KnownTypes.PointAnimationUsingKeyFrames, "KeyFrames",
					assemblies[1].Find("System.Windows.Media.Animation.PointKeyFrame", true));
			properties[KnownProperties.PriorityBinding_Bindings] = InitProperty(KnownTypes.PriorityBinding, "Bindings",
				assemblies[0].Find("System.Windows.Data.BindingBase", true));
			properties[KnownProperties.QuaternionAnimationUsingKeyFrames_KeyFrames] =
				InitProperty(KnownTypes.QuaternionAnimationUsingKeyFrames, "KeyFrames",
					assemblies[1].Find("System.Windows.Media.Animation.QuaternionKeyFrame", true));
			properties[KnownProperties.RadialGradientBrush_GradientStops] = InitProperty(KnownTypes.RadialGradientBrush,
				"GradientStops", assemblies[1].Find("System.Windows.Media.GradientStop", true));
			properties[KnownProperties.RadioButton_Content] = InitProperty(KnownTypes.RadioButton, "Content",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.RectAnimationUsingKeyFrames_KeyFrames] =
				InitProperty(KnownTypes.RectAnimationUsingKeyFrames, "KeyFrames",
					assemblies[1].Find("System.Windows.Media.Animation.RectKeyFrame", true));
			properties[KnownProperties.RepeatButton_Content] = InitProperty(KnownTypes.RepeatButton, "Content",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.RichTextBox_Document] = InitProperty(KnownTypes.RichTextBox, "Document",
				assemblies[0].Find("System.Windows.Documents.FlowDocument", true));
			properties[KnownProperties.Rotation3DAnimationUsingKeyFrames_KeyFrames] =
				InitProperty(KnownTypes.Rotation3DAnimationUsingKeyFrames, "KeyFrames",
					assemblies[1].Find("System.Windows.Media.Animation.Rotation3DKeyFrame", true));
			properties[KnownProperties.Run_Text] = InitProperty(KnownTypes.Run, "Text", assemblies[3].Find("System.Char", true));
			properties[KnownProperties.ScrollViewer_Content] = InitProperty(KnownTypes.ScrollViewer, "Content",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.Section_Blocks] = InitProperty(KnownTypes.Section, "Blocks",
				assemblies[0].Find("System.Windows.Documents.Block", true));
			properties[KnownProperties.Selector_Items] = InitProperty(KnownTypes.Selector, "Items",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.SingleAnimationUsingKeyFrames_KeyFrames] =
				InitProperty(KnownTypes.SingleAnimationUsingKeyFrames, "KeyFrames",
					assemblies[1].Find("System.Windows.Media.Animation.SingleKeyFrame", true));
			properties[KnownProperties.SizeAnimationUsingKeyFrames_KeyFrames] =
				InitProperty(KnownTypes.SizeAnimationUsingKeyFrames, "KeyFrames",
					assemblies[1].Find("System.Windows.Media.Animation.SizeKeyFrame", true));
			properties[KnownProperties.Span_Inlines] = InitProperty(KnownTypes.Span, "Inlines",
				assemblies[0].Find("System.Windows.Documents.Inline", true));
			properties[KnownProperties.StackPanel_Children] = InitProperty(KnownTypes.StackPanel, "Children",
				assemblies[1].Find("System.Windows.UIElement", true));
			properties[KnownProperties.StatusBar_Items] = InitProperty(KnownTypes.StatusBar, "Items",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.StatusBarItem_Content] = InitProperty(KnownTypes.StatusBarItem, "Content",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.Storyboard_Children] = InitProperty(KnownTypes.Storyboard, "Children",
				assemblies[1].Find("System.Windows.Media.Animation.Timeline", true));
			properties[KnownProperties.StringAnimationUsingKeyFrames_KeyFrames] =
				InitProperty(KnownTypes.StringAnimationUsingKeyFrames, "KeyFrames",
					assemblies[1].Find("System.Windows.Media.Animation.StringKeyFrame", true));
			properties[KnownProperties.Style_Setters] = InitProperty(KnownTypes.Style, "Setters",
				assemblies[0].Find("System.Windows.SetterBase", true));
			properties[KnownProperties.TabControl_Items] = InitProperty(KnownTypes.TabControl, "Items",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.TabItem_Content] = InitProperty(KnownTypes.TabItem, "Content",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.TabPanel_Children] = InitProperty(KnownTypes.TabPanel, "Children",
				assemblies[1].Find("System.Windows.UIElement", true));
			properties[KnownProperties.Table_RowGroups] = InitProperty(KnownTypes.Table, "RowGroups",
				assemblies[0].Find("System.Windows.Documents.TableRowGroup", true));
			properties[KnownProperties.TableCell_Blocks] = InitProperty(KnownTypes.TableCell, "Blocks",
				assemblies[0].Find("System.Windows.Documents.Block", true));
			properties[KnownProperties.TableRow_Cells] = InitProperty(KnownTypes.TableRow, "Cells",
				assemblies[0].Find("System.Windows.Documents.TableCell", true));
			properties[KnownProperties.TableRowGroup_Rows] = InitProperty(KnownTypes.TableRowGroup, "Rows",
				assemblies[0].Find("System.Windows.Documents.TableRow", true));
			properties[KnownProperties.TextBlock_Inlines] = InitProperty(KnownTypes.TextBlock, "Inlines",
				assemblies[0].Find("System.Windows.Documents.Inline", true));
			properties[KnownProperties.ThicknessAnimationUsingKeyFrames_KeyFrames] =
				InitProperty(KnownTypes.ThicknessAnimationUsingKeyFrames, "KeyFrames",
					assemblies[0].Find("System.Windows.Media.Animation.ThicknessKeyFrame", true));
			properties[KnownProperties.ToggleButton_Content] = InitProperty(KnownTypes.ToggleButton, "Content",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.ToolBar_Items] = InitProperty(KnownTypes.ToolBar, "Items",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.ToolBarOverflowPanel_Children] = InitProperty(KnownTypes.ToolBarOverflowPanel, "Children",
				assemblies[1].Find("System.Windows.UIElement", true));
			properties[KnownProperties.ToolBarPanel_Children] = InitProperty(KnownTypes.ToolBarPanel, "Children",
				assemblies[1].Find("System.Windows.UIElement", true));
			properties[KnownProperties.ToolBarTray_ToolBars] = InitProperty(KnownTypes.ToolBarTray, "ToolBars",
				assemblies[0].Find("System.Windows.Controls.ToolBar", true));
			properties[KnownProperties.ToolTip_Content] = InitProperty(KnownTypes.ToolTip, "Content",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.TreeView_Items] = InitProperty(KnownTypes.TreeView, "Items",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.TreeViewItem_Items] = InitProperty(KnownTypes.TreeViewItem, "Items",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.Trigger_Setters] = InitProperty(KnownTypes.Trigger, "Setters",
				assemblies[0].Find("System.Windows.SetterBase", true));
			properties[KnownProperties.Underline_Inlines] = InitProperty(KnownTypes.Underline, "Inlines",
				assemblies[0].Find("System.Windows.Documents.Inline", true));
			properties[KnownProperties.UniformGrid_Children] = InitProperty(KnownTypes.UniformGrid, "Children",
				assemblies[1].Find("System.Windows.UIElement", true));
			properties[KnownProperties.UserControl_Content] = InitProperty(KnownTypes.UserControl, "Content",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.Vector3DAnimationUsingKeyFrames_KeyFrames] =
				InitProperty(KnownTypes.Vector3DAnimationUsingKeyFrames, "KeyFrames",
					assemblies[1].Find("System.Windows.Media.Animation.Vector3DKeyFrame", true));
			properties[KnownProperties.VectorAnimationUsingKeyFrames_KeyFrames] =
				InitProperty(KnownTypes.VectorAnimationUsingKeyFrames, "KeyFrames",
					assemblies[1].Find("System.Windows.Media.Animation.VectorKeyFrame", true));
			properties[KnownProperties.Viewbox_Child] = InitProperty(KnownTypes.Viewbox, "Child",
				assemblies[1].Find("System.Windows.UIElement", true));
			properties[KnownProperties.Viewport3DVisual_Children] = InitProperty(KnownTypes.Viewport3DVisual, "Children",
				assemblies[1].Find("System.Windows.Media.Media3D.Visual3D", true));
			properties[KnownProperties.VirtualizingPanel_Children] = InitProperty(KnownTypes.VirtualizingPanel, "Children",
				assemblies[1].Find("System.Windows.UIElement", true));
			properties[KnownProperties.VirtualizingStackPanel_Children] = InitProperty(KnownTypes.VirtualizingStackPanel,
				"Children", assemblies[1].Find("System.Windows.UIElement", true));
			properties[KnownProperties.Window_Content] = InitProperty(KnownTypes.Window, "Content",
				assemblies[3].Find("System.Object", true));
			properties[KnownProperties.WrapPanel_Children] = InitProperty(KnownTypes.WrapPanel, "Children",
				assemblies[1].Find("System.Windows.UIElement", true));
			properties[KnownProperties.XmlDataProvider_XmlSerializer] = InitProperty(KnownTypes.XmlDataProvider, "XmlSerializer",
				assemblies[6].Find("System.Xml.Serialization.IXmlSerializable", true));
		}

		void InitStrings() {
			strings[0] = null;
			strings[1] = "Name";
			strings[2] = "Uid";
		}
	}
}