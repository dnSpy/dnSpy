// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml.Linq;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Per-session setting:
	/// Loaded at startup; saved at exit.
	/// </summary>
	public sealed class SessionSettings : INotifyPropertyChanged
	{
		public SessionSettings(ILSpySettings spySettings)
		{
			XElement doc = spySettings["SessionSettings"];
			
			XElement filterSettings = doc.Element("FilterSettings");
			if (filterSettings == null) filterSettings = new XElement("FilterSettings");
			
			this.FilterSettings = new FilterSettings(filterSettings);
			
			this.ActiveAssemblyList = (string)doc.Element("ActiveAssemblyList");
			
			XElement activeTreeViewPath = doc.Element("ActiveTreeViewPath");
			if (activeTreeViewPath != null) {
				this.ActiveTreeViewPath = activeTreeViewPath.Elements().Select(e => Unescape((string)e)).ToArray();
			}
			this.ActiveAutoLoadedAssembly = (string)doc.Element("ActiveAutoLoadedAssembly");
			
			this.WindowState = FromString((string)doc.Element("WindowState"), WindowState.Normal);
			this.WindowBounds = FromString((string)doc.Element("WindowBounds"), DefaultWindowBounds);
			this.SplitterPosition = FromString((string)doc.Element("SplitterPosition"), 0.4);
			this.TopPaneSplitterPosition = FromString((string)doc.Element("TopPaneSplitterPosition"), 0.3);
			this.BottomPaneSplitterPosition = FromString((string)doc.Element("BottomPaneSplitterPosition"), 0.3);
		}
		
		public event PropertyChangedEventHandler PropertyChanged;
		
		void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
		
		public FilterSettings FilterSettings { get; private set; }
		
		public string[] ActiveTreeViewPath;
		public string ActiveAutoLoadedAssembly;
		
		public string ActiveAssemblyList;
		
		public WindowState WindowState = WindowState.Normal;
		public Rect WindowBounds;
		internal static Rect DefaultWindowBounds =  new Rect(10, 10, 750, 550);
		/// <summary>
		/// position of the left/right splitter
		/// </summary>
		public double SplitterPosition;
		public double TopPaneSplitterPosition, BottomPaneSplitterPosition;
		
		public void Save()
		{
			XElement doc = new XElement("SessionSettings");
			doc.Add(this.FilterSettings.SaveAsXml());
			if (this.ActiveAssemblyList != null) {
				doc.Add(new XElement("ActiveAssemblyList", this.ActiveAssemblyList));
			}
			if (this.ActiveTreeViewPath != null) {
				doc.Add(new XElement("ActiveTreeViewPath", ActiveTreeViewPath.Select(p => new XElement("Node", Escape(p)))));
			}
			if (this.ActiveAutoLoadedAssembly != null) {
				doc.Add(new XElement("ActiveAutoLoadedAssembly", this.ActiveAutoLoadedAssembly));
			}
			doc.Add(new XElement("WindowState", ToString(this.WindowState)));
			doc.Add(new XElement("WindowBounds", ToString(this.WindowBounds)));
			doc.Add(new XElement("SplitterPosition", ToString(this.SplitterPosition)));
			doc.Add(new XElement("TopPaneSplitterPosition", ToString(this.TopPaneSplitterPosition)));
			doc.Add(new XElement("BottomPaneSplitterPosition", ToString(this.BottomPaneSplitterPosition)));
			
			ILSpySettings.SaveSettings(doc);
		}
		
		static Regex regex = new Regex("\\\\x(?<num>[0-9A-f]{4})");
		
		static string Escape(string p)
		{
			StringBuilder sb = new StringBuilder();
			foreach (char ch in p) {
				if (char.IsLetterOrDigit(ch))
					sb.Append(ch);
				else
					sb.AppendFormat("\\x{0:X4}", (int)ch);
			}
			return sb.ToString();
		}
		
		static string Unescape(string p)
		{
			return regex.Replace(p, m => ((char)int.Parse(m.Groups["num"].Value, NumberStyles.HexNumber)).ToString());
		}
		
		static T FromString<T>(string s, T defaultValue)
		{
			if (s == null)
				return defaultValue;
			try {
				TypeConverter c = TypeDescriptor.GetConverter(typeof(T));
				return (T)c.ConvertFromInvariantString(s);
			} catch (FormatException) {
				return defaultValue;
			}
		}
		
		static string ToString<T>(T obj)
		{
			TypeConverter c = TypeDescriptor.GetConverter(typeof(T));
			return c.ConvertToInvariantString(obj);
		}
	}
}
