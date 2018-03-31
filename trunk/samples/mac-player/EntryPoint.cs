using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Diagnostics;
using DZ.MediaPlayer;
using DZ.MediaPlayer.Vlc;
using DZ.MediaPlayer.Vlc.Deployment;
using DZ.MediaPlayer.Vlc.Common;
using DZ.MediaPlayer.Vlc.Io;
using Monobjc;
using Monobjc.Cocoa;


namespace MacPlayer {
	
	/// <summary>
	/// This is the entry point for the application.
	/// </summary>
	public static class EntryPoint {
		
		private static readonly Common.Logging.ILog logger = 
			Common.Logging.LogManager.GetLogger(typeof(EntryPoint));
		
		/// <summary>
		/// First method called.
		/// </summary>
		public static void Main () {
			try {
				//
				// NOTE: refer to monobjc documentation about this code.
				// load cocoa library
	            ObjectiveCRuntime.LoadFramework("Cocoa");
	            ObjectiveCRuntime.Initialize();
	            // 
	            NSApplication.Bootstrap();
				// load application bundle
	            NSApplication.LoadNib("Window.nib");
	            NSApplication.RunApplication();
				//
			} catch(Exception exc) {
				// log errors:
				if (logger.IsFatalEnabled) {
					logger.Fatal("An exception at the top level was catched.", exc);
				}
			}
		}
	}
}

