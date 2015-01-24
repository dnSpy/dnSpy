using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ICSharpCode.ILSpy.AddIn
{
	class Utils
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass")]
		[DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		static extern unsafe char** CommandLineToArgvW([MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine, out int pNumArgs);

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass")]
		[DllImport("kernel32.dll")]
		static extern IntPtr LocalFree(IntPtr hMem);

		#region CommandLine <-> Argument Array
		/// <summary>
		/// Decodes a command line into an array of arguments according to the CommandLineToArgvW rules.
		/// </summary>
		/// <remarks>
		/// Command line parsing rules:
		/// - 2n backslashes followed by a quotation mark produce n backslashes, and the quotation mark is considered to be the end of the argument.
		/// - (2n) + 1 backslashes followed by a quotation mark again produce n backslashes followed by a quotation mark.
		/// - n backslashes not followed by a quotation mark simply produce n backslashes.
		/// </remarks>
		public static unsafe string[] CommandLineToArgumentArray(string commandLine)
		{
			if (string.IsNullOrEmpty(commandLine))
				return new string[0];
			int numberOfArgs;
			char** arr = CommandLineToArgvW(commandLine, out numberOfArgs);
			if (arr == null)
				throw new Win32Exception();
			try {
				string[] result = new string[numberOfArgs];
				for (int i = 0; i < numberOfArgs; i++) {
					result[i] = new string(arr[i]);
				}
				return result;
			} finally {
				// Free memory obtained by CommandLineToArgW.
				LocalFree(new IntPtr(arr));
			}
		}

		static readonly char[] charsNeedingQuoting = { ' ', '\t', '\n', '\v', '"' };

		/// <summary>
		/// Escapes a set of arguments according to the CommandLineToArgvW rules.
		/// </summary>
		/// <remarks>
		/// Command line parsing rules:
		/// - 2n backslashes followed by a quotation mark produce n backslashes, and the quotation mark is considered to be the end of the argument.
		/// - (2n) + 1 backslashes followed by a quotation mark again produce n backslashes followed by a quotation mark.
		/// - n backslashes not followed by a quotation mark simply produce n backslashes.
		/// </remarks>
		public static string ArgumentArrayToCommandLine(params string[] arguments)
		{
			if (arguments == null)
				return null;
			StringBuilder b = new StringBuilder();
			for (int i = 0; i < arguments.Length; i++) {
				if (i > 0)
					b.Append(' ');
				AppendArgument(b, arguments[i]);
			}
			return b.ToString();
		}

		static void AppendArgument(StringBuilder b, string arg)
		{
			if (arg.Length > 0 && arg.IndexOfAny(charsNeedingQuoting) < 0) {
				b.Append(arg);
			} else {
				b.Append('"');
				for (int j = 0; ; j++) {
					int backslashCount = 0;
					while (j < arg.Length && arg[j] == '\\') {
						backslashCount++;
						j++;
					}
					if (j == arg.Length) {
						b.Append('\\', backslashCount * 2);
						break;
					} else if (arg[j] == '"') {
						b.Append('\\', backslashCount * 2 + 1);
						b.Append('"');
					} else {
						b.Append('\\', backslashCount);
						b.Append(arg[j]);
					}
				}
				b.Append('"');
			}
		}
		#endregion

		public static byte[] HexStringToBytes(string hex)
		{
			var result = new byte[hex.Length / 2];
			for (int i = 0; i < hex.Length / 2; i++) {
				result[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
			}
			return result;
		}
	}
}
