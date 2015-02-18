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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml.Linq;

using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.TextView;

namespace ICSharpCode.ILSpy
{
	[ExportMainMenuCommand(Menu = "_Help", Header = "_About", MenuOrder = 99999)]
	sealed class AboutPage : SimpleCommand
	{
		[Import]
		DecompilerTextView decompilerTextView = null;
		
		public override void Execute(object parameter)
		{
			MainWindow.Instance.UnselectAll();
			Display(decompilerTextView);
		}
		
		static readonly Uri UpdateUrl = new Uri("http://www.ilspy.net/updates.xml");
		const string band = "stable";
		
		static AvailableVersionInfo latestAvailableVersion;
		
		public static void Display(DecompilerTextView textView)
		{
			AvalonEditTextOutput output = new AvalonEditTextOutput();
			output.WriteLine("ILSpy version " + RevisionClass.FullVersion);
			output.AddUIElement(
				delegate {
					StackPanel stackPanel = new StackPanel();
					stackPanel.HorizontalAlignment = HorizontalAlignment.Center;
					stackPanel.Orientation = Orientation.Horizontal;
					if (latestAvailableVersion == null) {
						AddUpdateCheckButton(stackPanel, textView);
					} else {
						// we already retrieved the latest version sometime earlier
						ShowAvailableVersion(latestAvailableVersion, stackPanel);
					}
					CheckBox checkBox = new CheckBox();
					checkBox.Margin = new Thickness(4);
					checkBox.Content = "Automatically check for updates every week";
					UpdateSettings settings = new UpdateSettings(ILSpySettings.Load());
					checkBox.SetBinding(CheckBox.IsCheckedProperty, new Binding("AutomaticUpdateCheckEnabled") { Source = settings });
					return new StackPanel {
						Margin = new Thickness(0, 4, 0, 0),
						Cursor = Cursors.Arrow,
						Children = { stackPanel, checkBox }
					};
				});
			output.WriteLine();
			foreach (var plugin in App.CompositionContainer.GetExportedValues<IAboutPageAddition>())
				plugin.Write(output);
			output.WriteLine();
			using (Stream s = typeof(AboutPage).Assembly.GetManifestResourceStream(typeof(AboutPage), "README.txt")) {
				using (StreamReader r = new StreamReader(s)) {
					string line;
					while ((line = r.ReadLine()) != null) {
						output.WriteLine(line);
					}
				}
			}
			output.AddVisualLineElementGenerator(new MyLinkElementGenerator("SharpDevelop", "http://www.icsharpcode.net/opensource/sd/"));
			output.AddVisualLineElementGenerator(new MyLinkElementGenerator("MIT License", "resource:license.txt"));
			output.AddVisualLineElementGenerator(new MyLinkElementGenerator("LGPL", "resource:LGPL.txt"));
			output.AddVisualLineElementGenerator(new MyLinkElementGenerator("MS-PL", "resource:MS-PL.txt"));
			textView.ShowText(output);
			
			//reset icon bar
			textView.manager.Bookmarks.Clear();
		}
		
		sealed class MyLinkElementGenerator : LinkElementGenerator
		{
			readonly Uri uri;
			
			public MyLinkElementGenerator(string matchText, string url) : base(new Regex(Regex.Escape(matchText)))
			{
				this.uri = new Uri(url);
				this.RequireControlModifierForClick = false;
			}
			
			protected override Uri GetUriFromMatch(Match match)
			{
				return uri;
			}
		}
		
		static void AddUpdateCheckButton(StackPanel stackPanel, DecompilerTextView textView)
		{
			Button button = new Button();
			button.Content = "Check for updates";
			button.Cursor = Cursors.Arrow;
			stackPanel.Children.Add(button);
			
			button.Click += delegate {
				button.Content = "Checking...";
				button.IsEnabled = false;
				GetLatestVersionAsync().ContinueWith(
					delegate (Task<AvailableVersionInfo> task) {
						try {
							stackPanel.Children.Clear();
							ShowAvailableVersion(task.Result, stackPanel);
						} catch (Exception ex) {
							AvalonEditTextOutput exceptionOutput = new AvalonEditTextOutput();
							exceptionOutput.WriteLine(ex.ToString());
							textView.ShowText(exceptionOutput);
						}
					}, TaskScheduler.FromCurrentSynchronizationContext());
			};
		}
		
		static readonly Version currentVersion = new Version(RevisionClass.Major + "." + RevisionClass.Minor + "." + RevisionClass.Build + "." + RevisionClass.Revision);
		
		static void ShowAvailableVersion(AvailableVersionInfo availableVersion, StackPanel stackPanel)
		{
			if (currentVersion == availableVersion.Version) {
				stackPanel.Children.Add(
					new Image {
						Width = 16, Height = 16,
						Source = Images.OK,
						Margin = new Thickness(4,0,4,0)
					});
				stackPanel.Children.Add(
					new TextBlock {
						Text = "You are using the latest release.",
						VerticalAlignment = VerticalAlignment.Bottom
					});
			} else if (currentVersion < availableVersion.Version) {
				stackPanel.Children.Add(
					new TextBlock {
						Text = "Version " + availableVersion.Version + " is available.",
						Margin = new Thickness(0,0,8,0),
						VerticalAlignment = VerticalAlignment.Bottom
					});
				if (availableVersion.DownloadUrl != null) {
					Button button = new Button();
					button.Content = "Download";
					button.Cursor = Cursors.Arrow;
					button.Click += delegate {
						Process.Start(availableVersion.DownloadUrl);
					};
					stackPanel.Children.Add(button);
				}
			} else {
				stackPanel.Children.Add(new TextBlock { Text = "You are using a nightly build newer than the latest release." });
			}
		}
		
		static Task<AvailableVersionInfo> GetLatestVersionAsync()
		{
			var tcs = new TaskCompletionSource<AvailableVersionInfo>();
			WebClient wc = new WebClient();
			IWebProxy systemWebProxy = WebRequest.GetSystemWebProxy();
			systemWebProxy.Credentials = CredentialCache.DefaultCredentials;
			wc.Proxy = systemWebProxy;
			wc.DownloadDataCompleted += delegate(object sender, DownloadDataCompletedEventArgs e) {
				if (e.Error != null) {
					tcs.SetException(e.Error);
				} else {
					try {
						XDocument doc = XDocument.Load(new MemoryStream(e.Result));
						var bands = doc.Root.Elements("band");
						var currentBand = bands.FirstOrDefault(b => (string)b.Attribute("id") == band) ?? bands.First();
						Version version = new Version((string)currentBand.Element("latestVersion"));
						string url = (string)currentBand.Element("downloadUrl");
						if (!(url.StartsWith("http://", StringComparison.Ordinal) || url.StartsWith("https://", StringComparison.Ordinal)))
							url = null; // don't accept non-urls
						latestAvailableVersion = new AvailableVersionInfo { Version = version, DownloadUrl = url };
						tcs.SetResult(latestAvailableVersion);
					} catch (Exception ex) {
						tcs.SetException(ex);
					}
				}
			};
			wc.DownloadDataAsync(UpdateUrl);
			return tcs.Task;
		}
		
		sealed class AvailableVersionInfo
		{
			public Version Version;
			public string DownloadUrl;
		}
		
		sealed class UpdateSettings : INotifyPropertyChanged
		{
			public UpdateSettings(ILSpySettings spySettings)
			{
				XElement s = spySettings["UpdateSettings"];
				this.automaticUpdateCheckEnabled = (bool?)s.Element("AutomaticUpdateCheckEnabled") ?? true;
				try {
					this.lastSuccessfulUpdateCheck = (DateTime?)s.Element("LastSuccessfulUpdateCheck");
				} catch (FormatException) {
					// avoid crashing on settings files invalid due to
					// https://github.com/icsharpcode/ILSpy/issues/closed/#issue/2
				}
			}
			
			bool automaticUpdateCheckEnabled;
			
			public bool AutomaticUpdateCheckEnabled {
				get { return automaticUpdateCheckEnabled; }
				set {
					if (automaticUpdateCheckEnabled != value) {
						automaticUpdateCheckEnabled = value;
						Save();
						OnPropertyChanged("AutomaticUpdateCheckEnabled");
					}
				}
			}
			
			DateTime? lastSuccessfulUpdateCheck;
			
			public DateTime? LastSuccessfulUpdateCheck {
				get { return lastSuccessfulUpdateCheck; }
				set {
					if (lastSuccessfulUpdateCheck != value) {
						lastSuccessfulUpdateCheck = value;
						Save();
						OnPropertyChanged("LastSuccessfulUpdateCheck");
					}
				}
			}
			
			public void Save()
			{
				XElement updateSettings = new XElement("UpdateSettings");
				updateSettings.Add(new XElement("AutomaticUpdateCheckEnabled", automaticUpdateCheckEnabled));
				if (lastSuccessfulUpdateCheck != null)
					updateSettings.Add(new XElement("LastSuccessfulUpdateCheck", lastSuccessfulUpdateCheck));
				ILSpySettings.SaveSettings(updateSettings);
			}
			
			public event PropertyChangedEventHandler PropertyChanged;
			
			void OnPropertyChanged(string propertyName)
			{
				if (PropertyChanged != null) {
					PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
				}
			}
		}
		
		/// <summary>
		/// If automatic update checking is enabled, checks if there are any updates available.
		/// Returns the download URL if an update is available.
		/// Returns null if no update is available, or if no check was performed.
		/// </summary>
		public static Task<string> CheckForUpdatesIfEnabledAsync(ILSpySettings spySettings)
		{
			var tcs = new TaskCompletionSource<string>();
			UpdateSettings s = new UpdateSettings(spySettings);
			if (s.AutomaticUpdateCheckEnabled) {
				// perform update check if we never did one before;
				// or if the last check wasn't in the past 7 days
				if (s.LastSuccessfulUpdateCheck == null
				    || s.LastSuccessfulUpdateCheck < DateTime.UtcNow.AddDays(-7)
				    || s.LastSuccessfulUpdateCheck > DateTime.UtcNow)
				{
					GetLatestVersionAsync().ContinueWith(
						delegate (Task<AvailableVersionInfo> task) {
							try {
								s.LastSuccessfulUpdateCheck = DateTime.UtcNow;
								AvailableVersionInfo v = task.Result;
								if (v.Version > currentVersion)
									tcs.SetResult(v.DownloadUrl);
								else
									tcs.SetResult(null);
							} catch (AggregateException) {
								// ignore errors getting the version info
								tcs.SetResult(null);
							}
						});
				} else {
					tcs.SetResult(null);
				}
			} else {
				tcs.SetResult(null);
			}
			return tcs.Task;
		}
	}
	
	/// <summary>
	/// Interface that allows plugins to extend the about page.
	/// </summary>
	public interface IAboutPageAddition
	{
		void Write(ISmartTextOutput textOutput);
	}
}
