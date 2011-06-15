// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace AvalonDock
{
	public class DockingManager
	{
		public DockingManager()
		{
		}
	}
	
	public class Resizer : Thumb
	{
		static Resizer()
		{
			//This OverrideMetadata call tells the system that this element wants to provide a style that is different than its base class.
			//This style is defined in themes\generic.xaml
			DefaultStyleKeyProperty.OverrideMetadata(typeof(Resizer), new FrameworkPropertyMetadata(typeof(Resizer)));
			MinWidthProperty.OverrideMetadata(typeof(Resizer), new FrameworkPropertyMetadata(6.0, FrameworkPropertyMetadataOptions.AffectsParentMeasure));
			MinHeightProperty.OverrideMetadata(typeof(Resizer), new FrameworkPropertyMetadata(6.0, FrameworkPropertyMetadataOptions.AffectsParentMeasure));
			HorizontalAlignmentProperty.OverrideMetadata(typeof(Resizer), new FrameworkPropertyMetadata(HorizontalAlignment.Stretch, FrameworkPropertyMetadataOptions.AffectsParentMeasure));
			VerticalAlignmentProperty.OverrideMetadata(typeof(Resizer), new FrameworkPropertyMetadata(VerticalAlignment.Stretch, FrameworkPropertyMetadataOptions.AffectsParentMeasure));
		}

	}
	
	public enum AvalonDockBrushes
	{
		DefaultBackgroundBrush,
		DockablePaneTitleBackground,
		DockablePaneTitleBackgroundSelected,
		DockablePaneTitleForeground,
		DockablePaneTitleForegroundSelected,
		PaneHeaderCommandBackground,
		PaneHeaderCommandBorderBrush,
		DocumentHeaderBackground,
		DocumentHeaderForeground,
		DocumentHeaderForegroundSelected,
		DocumentHeaderForegroundSelectedActivated,
		DocumentHeaderBackgroundSelected,
		DocumentHeaderBackgroundSelectedActivated,
		DocumentHeaderBackgroundMouseOver,
		DocumentHeaderBorderBrushMouseOver,
		DocumentHeaderBorder,
		DocumentHeaderBorderSelected,
		DocumentHeaderBorderSelectedActivated,
		NavigatorWindowTopBackground,
		NavigatorWindowTitleForeground,
		NavigatorWindowDocumentTypeForeground,
		NavigatorWindowInfoTipForeground,
		NavigatorWindowForeground,
		NavigatorWindowBackground,
		NavigatorWindowSelectionBackground,
		NavigatorWindowSelectionBorderbrush,
		NavigatorWindowBottomBackground
	}
	
	public enum ContextMenuElement
	{
		DockablePane,
		DocumentPane,
		DockableFloatingWindow,
		DocumentFloatingWindow
	}
}
