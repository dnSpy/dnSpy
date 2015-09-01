// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System.Collections.Generic;
using System.Windows.Controls;
using dnSpy;
using dnSpy.Debugger;
using dnSpy.Images;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.AvalonEdit;
using ICSharpCode.ILSpy.Bookmarks;
using ICSharpCode.ILSpy.Debugger.Bookmarks;
using ICSharpCode.ILSpy.Debugger.Services;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.NRefactory;

namespace ICSharpCode.ILSpy.Debugger.Commands
{
	[ExportIconBarActionEntry(Icon = "BreakpointMenu", Category = "Debug")]
	public class BreakpointCommand : IIconBarActionEntry
	{
		public bool IsEnabled(DecompilerTextView textView)
		{
			return true;
		}
		
		public void Execute(DecompilerTextView textView, int line)
		{
			BreakpointHelper.Toggle(textView, line, 0);
		}
	}

	static class BreakpointHelper
	{
		public static bool IsEnabled(this IEnumerable<BreakpointBookmark> bps)
		{
			foreach (var bp in bps) {
				if (bp.IsEnabled)
					return true;
			}
			return false;
		}

		public static bool IsDisabled(this IEnumerable<BreakpointBookmark> bps)
		{
			return !bps.IsEnabled();
		}

		public static List<BreakpointBookmark> GetBreakpointBookmarks(DecompilerTextView textView, int line, int column)
		{
			return GetBreakpointBookmarks(textView, SourceCodeMappingUtils.Find(textView, line, column));
		}

		static List<BreakpointBookmark> GetBreakpointBookmarks(DecompilerTextView textView, IList<SourceCodeMapping> mappings)
		{
			var list = new List<BreakpointBookmark>();
			if (mappings.Count == 0)
				return list;
			var mapping = mappings[0];
			foreach (var bm in BookmarkManager.Bookmarks) {
				var bpm = bm as BreakpointBookmark;
				if (bpm == null)
					continue;
				TextLocation location, endLocation;
				if (!bpm.GetLocation(textView, out location, out endLocation))
					continue;
				if (location != mapping.StartLocation || endLocation != mapping.EndLocation)
					continue;

				list.Add(bpm);
			}

			return list;
		}

		public static void Toggle(DecompilerTextView textView, int line, int column)
		{
			var bps = SourceCodeMappingUtils.Find(textView, line, column);
			var bpms = GetBreakpointBookmarks(textView, bps);
			if (bpms.Count > 0) {
				if (bpms.IsEnabled()) {
					foreach (var bpm in bpms)
						BookmarkManager.RemoveMark(bpm);
				}
				else {
					foreach (var bpm in bpms)
						bpm.IsEnabled = true;
				}
			}
			else if (bps.Count > 0) {
				foreach (var bp in bps) {
					if (MethodKey.Create(bp.MemberMapping.MethodDefinition) == null)
						continue;
					BookmarkManager.AddMark(new BreakpointBookmark(bp.MemberMapping.MethodDefinition, bp.StartLocation, bp.EndLocation, bp.ILInstructionOffset));
				}
				textView.ScrollAndMoveCaretTo(bps[0].StartLocation.Line, bps[0].StartLocation.Column);
			}
		}
	}

	[ExportIconBarContextMenuEntry(InputGestureText = "Ctrl+F9",
									Category = "Debug",
									Order = 110)]
	public class EnableAndDisableBreakpointCommand : IIconBarContextMenuEntry2
	{
		public bool IsVisible(IIconBarObject bookmark)
		{
			return bookmark is BreakpointBookmark;
		}

		public bool IsEnabled(IIconBarObject bookmark)
		{
			return IsVisible(bookmark);
		}

		public void Execute(IIconBarObject bookmark)
		{
			var bpm = bookmark as BreakpointBookmark;
			if (bpm != null)
				bpm.IsEnabled = !bpm.IsEnabled;
		}

		public void Initialize(IIconBarObject bookmark, MenuItem menuItem)
		{
			var bpm = bookmark as BreakpointBookmark;
			if (bpm != null)
				InitializeMenuItem(new[] { bpm }, menuItem, BackgroundType.ContextMenuItem);
		}

		public static void InitializeMenuItem(IList<BreakpointBookmark> bpms, MenuItem menuItem, BackgroundType bgType)
		{
			menuItem.IsEnabled = bpms.Count > 0;
			if (bpms.IsEnabled()) {
				menuItem.Header = bpms.Count <= 1 ? "_Disable Breakpoint" : "_Disable Breakpoints";
				menuItem.Icon = ImageService.LoadImage(ImageService.GetImage("DisableEnableBreakpoint", bgType), 16, 16);
			}
			else {
				menuItem.Header = bpms.Count <= 1 ? "Enab_le Breakpoint" : "Enab_le Breakpoints";
				menuItem.Icon = ImageService.LoadImage(ImageService.GetImage("DisableEnableBreakpoint", bgType), 16, 16);
			}
		}
	}

	[ExportIconBarContextMenuEntry(Header = "D_elete Breakpoint",
									Icon = "BreakpointMenu",
									Category = "Debug",
									Order = 100)]
	public class DeleteBreakpointCommand : IIconBarContextMenuEntry
	{
		public bool IsVisible(IIconBarObject bookmark)
		{
			return bookmark is BreakpointBookmark;
		}

		public bool IsEnabled(IIconBarObject bookmark)
		{
			return IsVisible(bookmark);
		}

		public void Execute(IIconBarObject bookmark)
		{
			var bpm = bookmark as BreakpointBookmark;
			if (bpm != null)
				BookmarkManager.RemoveMark(bpm);
		}
	}
}
