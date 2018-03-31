using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using DZ.MediaPlayer.Vlc.Windows;
using DZ.MediaPlayer.Vlc.Windows.Interop;
using NUnit.Framework;
using DZ.MediaPlayer.Vlc.Deployment;
using System.Collections.Generic;

namespace DZ.MediaPlayer.Vlc.Tests {
	/// <summary>
	/// This is the base class for all <see cref="Player"/> class tests.
	/// </summary>
	public class BaseVlcPlayerTest {
		private string defaultFreeSampleVideo;
		private string defaultFreeSampleAudio;

	    public string GetSampleVideoPath() {
			if (defaultFreeSampleVideo == null) {
                if (VlcDeployment.OSType == VlcDeployment.DeterminedOSType.MacOS) {
                    defaultFreeSampleVideo =
                        "/Users/rz/Movies/IELTS Preparation Series/Study English IELTS Preparation Ep 1-13.mp4";
                } else {
                    defaultFreeSampleVideo =
                        @"C:\Users\Public\Videos\Sample Videos\Wildlife.wmv";
                }
			}
			return defaultFreeSampleVideo;
		}
		
		public string GetSampleAudioPath() {
			if (defaultFreeSampleAudio == null) {
                if (VlcDeployment.OSType == VlcDeployment.DeterminedOSType.MacOS) {
                    defaultFreeSampleAudio =
                        "/Users/rz/Documents/Innovations Pre-Intermediate/Innovations Pre-Intermediate CD1/01 - Track 01.mp3";
                } else {
                    defaultFreeSampleAudio =
                       @"C:\Users\Public\Music\Sample Music\Kalimba.mp3";
                }
			}
			return defaultFreeSampleAudio;
		}
		
		[SetUp]
		public void SetUp() {
			VlcDeployment deployment = VlcDeployment.Default;
			if (! deployment.CheckVlcLibraryExistence(true, false) ) {
				deployment.Install(true, true, false, false);
			}
            Thread th = new Thread(RunMessagesLoop);
            th.Start();
		}

        [STAThread]
	    private void RunMessagesLoop() {
            //Application.Run(new Form());
	    }

        [TearDown]
        public void TearDown() {
            //Application.Exit();
        }
		
		public string GetTemporaryFilePath() {
			string path = Path.Combine (Path.GetDirectoryName (Assembly.GetCallingAssembly ().Location), "temporary");
			return Path.Combine(path, Path.GetTempFileName());
		}

	    public VlcMediaLibraryFactory CreateNewFactory() {
			string path = Path.Combine (Path.GetDirectoryName (Assembly.GetCallingAssembly ().Location), "plugins");
			VlcMediaLibraryFactory factory = new VlcMediaLibraryFactory(new string[] { "--reset-config", 
					"--no-snapshot-preview", 
					"--aspect-ratio=16:9", 
					"--ignore-config", 
					"--intf", "rc", 
					"--no-osd", 
					"--plugin-path", path }
			);
			return factory;
		}
		
		public VlcMediaLibraryFactory CreateNewFactory(string[] parameters) {
			if (parameters == null) {
				throw new ArgumentNullException("parameters");
			}
			string path = Path.Combine (Path.GetDirectoryName (Assembly.GetCallingAssembly ().Location), "plugins");
			List<string> paramList = new List<string>(parameters.Length + 2);
			paramList.AddRange(parameters);
			paramList.Add("--plugin-path");
			paramList.Add(path);
			VlcMediaLibraryFactory factory = new VlcMediaLibraryFactory(paramList.ToArray());
			return factory;
		}
	}
}

