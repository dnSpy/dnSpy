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
using dnSpy.Contracts.Text;

namespace dnSpy.Contracts.Scripting.Roslyn {
	/// <summary>
	/// Prints text
	/// </summary>
	public interface ITextPrinter : IOutputWriter {
		/// <summary>
		/// Print options
		/// </summary>
		IPrintOptions PrintOptions { get; }

		/// <summary>
		/// Prints text to the screen
		/// </summary>
		/// <param name="text">Text</param>
		void PrintError(string text);

		/// <summary>
		/// Prints text to the screen
		/// </summary>
		/// <param name="fmt">Format</param>
		/// <param name="args">Args</param>
		void PrintError(string fmt, params object[] args);

		/// <summary>
		/// Prints text followed by a new line to the screen
		/// </summary>
		/// <param name="text">Text or null</param>
		void PrintLineError(string text);

		/// <summary>
		/// Prints text followed by a new line to the screen
		/// </summary>
		/// <param name="fmt">Format</param>
		/// <param name="args">Args</param>
		void PrintLineError(string fmt, params object[] args);

		/// <summary>
		/// Prints text to the screen
		/// </summary>
		/// <param name="color">Color</param>
		/// <param name="text">Text</param>
		void Print(object color, string text);

		/// <summary>
		/// Prints text to the screen
		/// </summary>
		/// <param name="color">Color</param>
		/// <param name="text">Text</param>
		void Print(OutputColor color, string text);

		/// <summary>
		/// Prints text to the screen
		/// </summary>
		/// <param name="text">Text</param>
		void Print(string text);

		/// <summary>
		/// Prints text to the screen
		/// </summary>
		/// <param name="color">Color</param>
		/// <param name="fmt">Format</param>
		/// <param name="args">Args</param>
		void Print(object color, string fmt, params object[] args);

		/// <summary>
		/// Prints text to the screen
		/// </summary>
		/// <param name="color">Color</param>
		/// <param name="fmt">Format</param>
		/// <param name="args">Args</param>
		void Print(OutputColor color, string fmt, params object[] args);

		/// <summary>
		/// Prints text to the screen
		/// </summary>
		/// <param name="fmt">Format</param>
		/// <param name="args">Args</param>
		void Print(string fmt, params object[] args);

		/// <summary>
		/// Prints text followed by a new line to the screen
		/// </summary>
		/// <param name="color">Color</param>
		/// <param name="text">Text or null</param>
		void PrintLine(object color, string text);

		/// <summary>
		/// Prints text followed by a new line to the screen
		/// </summary>
		/// <param name="color">Color</param>
		/// <param name="text">Text or null</param>
		void PrintLine(OutputColor color, string text);

		/// <summary>
		/// Prints text followed by a new line to the screen
		/// </summary>
		/// <param name="text">Text or null</param>
		void PrintLine(string text = null);

		/// <summary>
		/// Prints text followed by a new line to the screen
		/// </summary>
		/// <param name="color">Color</param>
		/// <param name="fmt">Format</param>
		/// <param name="args">Args</param>
		void PrintLine(object color, string fmt, params object[] args);

		/// <summary>
		/// Prints text followed by a new line to the screen
		/// </summary>
		/// <param name="color">Color</param>
		/// <param name="fmt">Format</param>
		/// <param name="args">Args</param>
		void PrintLine(OutputColor color, string fmt, params object[] args);

		/// <summary>
		/// Prints text followed by a new line to the screen
		/// </summary>
		/// <param name="fmt">Format</param>
		/// <param name="args">Args</param>
		void PrintLine(string fmt, params object[] args);

		/// <summary>
		/// Formats and prints a value to the screen
		/// </summary>
		/// <param name="value">Value, can be null</param>
		/// <param name="color">Color</param>
		void Print(object value, object color);

		/// <summary>
		/// Formats and prints a value to the screen
		/// </summary>
		/// <param name="value">Value, can be null</param>
		/// <param name="color">Color</param>
		void Print(object value, OutputColor color = OutputColor.ReplScriptOutputText);

		/// <summary>
		/// Formats and prints a value followed by a new line to the screen
		/// </summary>
		/// <param name="value">Value or null</param>
		/// <param name="color">Color</param>
		void PrintLine(object value, object color);

		/// <summary>
		/// Formats and prints a value followed by a new line to the screen
		/// </summary>
		/// <param name="value">Value or null</param>
		/// <param name="color">Color</param>
		void PrintLine(object value, OutputColor color = OutputColor.ReplScriptOutputText);

		/// <summary>
		/// Formats and prints an exception to the screen
		/// </summary>
		/// <param name="ex">Exception</param>
		/// <param name="color">Color</param>
		void Print(Exception ex, object color);

		/// <summary>
		/// Formats and prints an exception to the screen
		/// </summary>
		/// <param name="ex">Exception</param>
		/// <param name="color">Color</param>
		void Print(Exception ex, OutputColor color = OutputColor.Error);

		/// <summary>
		/// Formats and prints an exception followed by a new line to the screen
		/// </summary>
		/// <param name="ex">Exception</param>
		/// <param name="color">Color</param>
		void PrintLine(Exception ex, object color);

		/// <summary>
		/// Formats and prints an exception followed by a new line to the screen
		/// </summary>
		/// <param name="ex">Exception</param>
		/// <param name="color">Color</param>
		void PrintLine(Exception ex, OutputColor color = OutputColor.Error);
	}
}
