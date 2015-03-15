using System;
using System.Collections.Generic;
using System.Windows;
using dnlib.DotNet;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.NRefactory;

namespace ICSharpCode.ILSpy.Debugger
{
	static class DebugUtils
	{
		public static bool JumpToCurrentStatement(DecompilerTextView textView)
		{
			if (textView == null)
				return false;
			var info = DebugInformation.DebugStepInformation;
			if (info == null)
				return false;
			return MainWindow.Instance.JumpToReference(textView, info.Item3, success => {
				if (success)
					MoveCaretToCurrentStatement(textView);
			});
		}

		public static bool JumpTo(DecompilerTextView textView, IMemberRef mr, MethodKey key, int ilOffset)
		{
			return MainWindow.Instance.JumpToReference(textView, mr, success => {
				if (success)
					MoveCaretTo(textView, key, ilOffset);
			});
		}

		static void MoveCaretToCurrentStatement(DecompilerTextView textView)
		{
			var info = DebugInformation.DebugStepInformation;
			if (info == null)
				return;
			MoveCaretTo(textView, info.Item1, info.Item2);
		}

		static void MoveCaretTo(DecompilerTextView textView, MethodKey key, int ilOffset)
		{
			if (textView == null)
				return;
			TextLocation location, endLocation;
			var cm = textView.CodeMappings;
			if (cm == null || !cm.ContainsKey(key))
				return;
			if (!cm[key].GetInstructionByTokenAndOffset(unchecked((uint)ilOffset), out location, out endLocation)) {
				//TODO: Missing IL ranges
			}
			else
				textView.ScrollAndMoveCaretTo(location.Line, location.Column);
		}

		public static bool JumpToReference(DecompilerTextView textView, IMemberRef mr, Func<TextLocation> getLocation)
		{
			bool retVal = MainWindow.Instance.JumpToReference(textView, mr, getLocation);
			if (!retVal) {
				MessageBox.Show(MainWindow.Instance,
					string.Format("Could not find {0}\n" +
					"Make sure that it's visible in the treeview and not a hidden method or part of a hidden class. You could also try to debug the method in IL mode.", mr));
			}
			return retVal;
		}

		/// <summary>
		/// Gets the current debugged method
		/// </summary>
		/// <param name="info"></param>
		/// <param name="currentKey"></param>
		/// <param name="codeMappings"></param>
		/// <returns></returns>
		public static bool VerifyAndGetCurrentDebuggedMethod(DecompilerTextView textView, out Tuple<MethodKey, int, IMemberRef> info, out MethodKey currentKey, out Dictionary<MethodKey, MemberMapping> codeMappings)
		{
			currentKey = default(MethodKey);
			codeMappings = textView == null ? null : textView.CodeMappings;
			info = DebugInformation.DebugStepInformation;

			if (info == null)
				return false;

			currentKey = info.Item1;
			if (codeMappings == null || !codeMappings.ContainsKey(currentKey))
				return false;

			return true;
		}
	}
}
