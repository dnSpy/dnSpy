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
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using System.Xml.Linq;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Manages IL Spy settings.
	/// </summary>
	public class ILSpySettings
	{
		readonly XElement root;
		
		ILSpySettings()
		{
			this.root = new XElement("ILSpy");
		}
		
		ILSpySettings(XElement root)
		{
			this.root = root;
		}
		
		public XElement this[string section] {
			get {
				return root.Element(section) ?? new XElement(section);
			}
		}
		
		public static ILSpySettings Load()
		{
			using (new MutexProtector(ConfigFileMutex)) {
				try {
					XDocument doc = XDocument.Load(GetConfigFile());
					return new ILSpySettings(doc.Root);
				} catch (IOException) {
					return new ILSpySettings();
				} catch (XmlException) {
					return new ILSpySettings();
				}
			}
		}
		
		public static void SaveSettings(XElement section)
		{
			using (new MutexProtector(ConfigFileMutex)) {
				string config = GetConfigFile();
				XDocument doc;
				try {
					doc = XDocument.Load(config);
				} catch (IOException) {
					// ensure the directory exists
					Directory.CreateDirectory(Path.GetDirectoryName(config));
					doc = new XDocument(new XElement("ILSpy"));
				} catch (XmlException) {
					doc = new XDocument(new XElement("ILSpy"));
				}
				doc.Root.SetAttributeValue("version", RevisionClass.Major + "." + RevisionClass.Minor + "." + RevisionClass.Build + "." + RevisionClass.Revision);
				XElement existingElement = doc.Root.Element(section.Name);
				if (existingElement != null)
					existingElement.ReplaceWith(section);
				else
					doc.Root.Add(section);
				doc.Save(config);
			}
		}
		
		public static string GetConfigFile()
		{
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ICSharpCode\\ILSpy.xml");
		}
		
		const string ConfigFileMutex = "01A91708-49D1-410D-B8EB-4DE2662B3971";
		
		sealed class MutexProtector : IDisposable
		{
			Mutex mutex;
			
			public MutexProtector(string name)
			{
				bool createdNew;
				this.mutex = new Mutex(true, name, out createdNew);
				if (!createdNew) {
					try {
						mutex.WaitOne();
					} catch (AbandonedMutexException) {
					}
				}
			}
			
			public void Dispose()
			{
				mutex.ReleaseMutex();
				mutex.Dispose();
			}
		}
	}
}
