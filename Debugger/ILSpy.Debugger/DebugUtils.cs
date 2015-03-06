using System;
using System.Collections.Generic;
using System.Windows;
using dnlib.DotNet;
using ICSharpCode.Decompiler;

namespace ICSharpCode.ILSpy.Debugger
{
	static class DebugUtils
	{
		/// <summary>
		/// Jumps to the reference, honoring DecompileFullType option
		/// </summary>
		/// <param name="mr"></param>
		/// <returns></returns>
		public static bool JumpToReference(IMemberRef mr)
		{
			bool alreadySelected;
			return JumpToReference(mr, out alreadySelected);
		}

		/// <summary>
		/// Jumps to the reference, honoring DecompileFullType option
		/// </summary>
		/// <param name="mr"></param>
		/// <param name="alreadySelected"></param>
		/// <returns></returns>
		public static bool JumpToReference(IMemberRef mr, out bool alreadySelected)
		{
			IMemberRef whatToCompile = mr;
			var md = mr as IMemberDef;
			if (md != null && DebuggerSettings.Instance.DecompileFullType) {
				var type = md.DeclaringType;
				for (int i = 0; i < 100; i++) {
					var declType = type.DeclaringType;
					if (declType == null)
						break;
					type = declType;
				}
				whatToCompile = type;
			}
			bool retVal = MainWindow.Instance.JumpToReference(whatToCompile, out alreadySelected);
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
		public static bool VerifyAndGetCurrentDebuggedMethod(out Tuple<MethodKey, int, IMemberRef> info, out MethodKey currentKey, out Dictionary<MethodKey, MemberMapping> codeMappings)
		{
			currentKey = default(MethodKey);
			codeMappings = DebugInformation.CodeMappings;
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
