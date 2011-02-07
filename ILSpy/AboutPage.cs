// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;
using System.Xml.Linq;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.TextView;

namespace ICSharpCode.ILSpy
{
	static class AboutPage
	{
		static readonly Uri UpdateUrl = new Uri("http://www.ilspy.net/updates.xml");
		
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
					return new StackPanel {
						Margin = new Thickness(0, 4, 0, 0),
						Cursor = Cursors.Arrow,
						Children = { stackPanel, checkBox }
					};
				});
			output.WriteLine();
			output.WriteLine();
			using (Stream s = typeof(AboutPage).Assembly.GetManifestResourceStream(typeof(AboutPage), "README.txt")) {
				using (StreamReader r = new StreamReader(s)) {
					string line;
					while ((line = r.ReadLine()) != null)
						output.WriteLine(line);
				}
			}
			textView.Show(output);
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
				GetLatestVersion().ContinueWith(
					delegate (Task<AvailableVersionInfo> task) {
						try {
							latestAvailableVersion = task.Result;
							stackPanel.Children.Clear();
							ShowAvailableVersion(latestAvailableVersion, stackPanel);
						} catch (Exception ex) {
							AvalonEditTextOutput exceptionOutput = new AvalonEditTextOutput();
							exceptionOutput.WriteLine(ex.ToString());
							textView.Show(exceptionOutput);
						}
					}, TaskScheduler.FromCurrentSynchronizationContext());
			};
		}
		
		static void ShowAvailableVersion(AvailableVersionInfo availableVersion, StackPanel stackPanel)
		{
			Version currentVersion = new Version(RevisionClass.Major + "." + RevisionClass.Minor + "." + RevisionClass.Build + "." + RevisionClass.Revision);
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
				if (availableVersion.DownloadUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
				    || availableVersion.DownloadUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
				{
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
		
		static Task<AvailableVersionInfo> GetLatestVersion()
		{
			var tcs = new TaskCompletionSource<AvailableVersionInfo>();
			WebClient wc = new WebClient();
			wc.DownloadDataCompleted += delegate(object sender, DownloadDataCompletedEventArgs e) {
				if (e.Error != null) {
					tcs.SetException(e.Error);
				} else {
					try {
						XDocument doc = XDocument.Load(new MemoryStream(e.Result));
						var bands = doc.Root.Elements("band");
						var currentBand = bands.FirstOrDefault(b => (string)b.Attribute("id") == "stable") ?? bands.First();
						Version version = new Version((string)currentBand.Element("latestVersion"));
						string url = (string)currentBand.Element("downloadUrl");
						tcs.SetResult(new AvailableVersionInfo { Version = version, DownloadUrl = url });
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
	}
}
