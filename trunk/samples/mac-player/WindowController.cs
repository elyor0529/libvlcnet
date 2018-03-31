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
	/// This class controls Cocoa window.
	/// </summary>
	[ObjectiveCClass]
	public class WindowController : NSController {

		private static readonly Common.Logging.ILog logger = Common.Logging.LogManager.GetLogger (typeof(EntryPoint));
		private bool fileOpened;

		/// <summary>
		/// Initialized outside. Used to get file path.
		/// </summary>
		[ObjectiveCField("filePathInput")]
		public NSPathControl FilePathInput;

		/// <summary>
		/// Initialized outside. Used to get view pointer.
		/// </summary>
		[ObjectiveCField("videoView")]
		public VideoOutputView VideoView;

		/// <summary>
		/// Initialized outside. Used to control button state.
		/// </summary>
		[ObjectiveCField("playButton")]
		public NSButton PlayButton;

		/// <summary>
		/// Initialized outside. Used to control button state.
		/// </summary>
		[ObjectiveCField("stopButton")]
		public NSButton StopButton;

		/// <summary>
		/// Initialized outside. Used to control button state.
		/// </summary>
		[ObjectiveCField("pauseButton")]
		public NSButton PauseButton;

		/// <summary>
		/// Initialized outside. Used to control button state.
		/// </summary>
		[ObjectiveCField("positionSlider")]
		public Slider PositionSlider;

		/// <summary>
		/// Default contructor is obligatory for monobjc.
		/// </summary>
		public WindowController () {
		}

		/// <summary>
		/// This constructor is obligatory too.
		/// </summary>
		/// <param name="ptr">
		/// A <see cref="IntPtr"/> - pointer to unmanaged NSController.
		/// </param>
		public WindowController (IntPtr ptr) : base(ptr) {
		}

		// this is central object in our library.
		private VlcMediaLibraryFactory factory;
		// this object controls playing
		private VlcSinglePlayer player;

		/// <summary>
		/// Defines current playing position.
		/// This value is bound to Slider control's position.
		/// </summary>
		public NSNumber SliderPositionValue {
			[ObjectiveCMessage("position")]
			get {
				if (player != null) {
					return (player.Position);
				} else {
					return (0.0f);
				}
			}
			[ObjectiveCMessage("setPosition:")]
			set {
				try {
					if (player == null) {
						return;
					}
					this.WillChangeValueForKey ("position");
					if (player.Position != value.FloatValue) {
						player.Position = value.FloatValue;
						this.DidChangeValueForKey ("position");
					}
				} catch (Exception exc) {
					if (logger.IsErrorEnabled) {
						logger.Error ("An error when setting position.", exc);
					}
				}
			}
		}

		/// <summary>
		/// Defines volume.
		/// This one is bound to Circle Slider control's position.
		/// </summary>
		public NSNumber SliderVolumeValue {
			[ObjectiveCMessage("volume")]
			get {
				if (player != null) {
					return (player.Volume);
				} else {
					return (0.0f);
				}
			}
			[ObjectiveCMessage("setVolume:")]
			set {
				try {
					if (player == null) {
						return;
					}
					this.WillChangeValueForKey ("volume");
					if (player.Volume != value.IntValue) {
						player.Volume = value.IntValue;
						this.DidChangeValueForKey ("volume");
					}
				} catch (Exception exc) {
					if (logger.IsErrorEnabled) {
						logger.Error ("An error when setting volume.", exc);
					}
				}
			}
		}

		/// <summary>
		/// This method is called when Play button is pressed.
		/// </summary>
		[ObjectiveCMessage("onPlayClick:")]
		public void OnPlayClick (Id button) {
			//
			try {
				initializePlayer ();
				if ( ! fileOpened ) {
					// if the file not opened - we should open it
					// if the file was opened already pressing Play means continue.
					player.SetMediaInput(new MediaInput (MediaInputType.File, FilePathInput.StringValue));
					fileOpened = true;
				}
				player.Play ();
				//
			} catch (Exception exc) {
				if (logger.IsErrorEnabled) {
					logger.Error ("Cannot play the file.", exc);
				}
				//
				AppKitFramework.NSRunAlertPanel ("Error", "Cannot play. See logs for more details.", "OK", null, null);
			}
		}

		/// <summary>
		/// This method is called when Play button is pressed.
		/// </summary>
		[ObjectiveCMessage("onStopClick:")]
		public void OnStopClick (Id button) {
			//
			try {
				initializePlayer ();
				//
				player.Stop ();
				//
			} catch (Exception exc) {
				if (logger.IsErrorEnabled) {
					logger.Error ("Cannot stop playing.", exc);
				}
				//
				AppKitFramework.NSRunAlertPanel ("Error", "Cannot stop. See logs for more details.", "OK", null, null);
			}
		}

		/// <summary>
		/// This method is called when Play button is pressed.
		/// </summary>
		[ObjectiveCMessage("onPauseClick:")]
		public void OnPauseClick (Id button) {
			//
			try {
				initializePlayer ();
				//
				if (logger.IsInfoEnabled) {
					logger.Info("Player state before pause: " + player.State);
				}
				//
				if ( fileOpened && player.State == PlayerState.Playing ) {
					player.Pause ();
				}
				//
			} catch (Exception exc) {
				if (logger.IsErrorEnabled) {
					logger.Error ("Cannot pause.", exc);
				}
				//
				AppKitFramework.NSRunAlertPanel ("Error", "Cannot pause. See logs for more details.", "OK", null, null);
			}
		}

		[ObjectiveCMessage("onOpenFile:")]
		public void OnOpenFile (Id menuItem) {
			try {
				initializePlayer ();
				//
				NSOpenPanel panel = Monobjc.Cocoa.NSOpenPanel.OpenPanel;
				int returnCode = panel.RunModalForTypes (NSArray.ArrayWithObjects ((NSString)"avi", 
					new object[] { (NSString)"mp3", (NSString)"mp4", (NSString)"mpg", null }));
				if ( returnCode == NSOpenPanel.NSOKButton ) {
					string fileToOpen = panel.Filename;
					FilePathInput.StringValue = fileToOpen;
					fileOpened = false;
					// start playing!
					OnPlayClick(menuItem);
				}
			} catch (Exception exc) {
				if (logger.IsErrorEnabled) {
					logger.Error ("Cannot open file", exc);
				}
			}
		}

		/// <summary>
		/// This method is called right after the instance is loaded from the NIB.
		/// </summary>
		[ObjectiveCMessage("awakeFromNib")]
		public void AwakeFromNib () {
			if (FilePathInput != null) {
				string defaultPath = "/Users/rz/Movies/IELTS Preparation Series/Study English IELTS Preparation Ep 1-13.mp4";
				if (File.Exists (defaultPath)) {
					FilePathInput.StringValue = defaultPath;
				}
			}
			NSApplication.SharedApplication.SetDelegate(
				new Action<NSApplication.NSApplicationEventDispatcher>(initializeEventDispatcher));
		}
		
		void initializeEventDispatcher(NSApplication.NSApplicationEventDispatcher dispatcher) {
			dispatcher.ApplicationWillTerminate += applicationWillTerminate;
		}

		void applicationWillTerminate (NSNotification aNotification) {
			if (logger.IsTraceEnabled) {
				logger.Trace("Disposing resources");
			}
			try {
				if ( player != null ) {
					player.Dispose();
					player = null;
				}
				if ( factory != null ) {
					factory.Dispose();
					factory = null;
				}
			} catch(Exception exception) {
				if (logger.IsErrorEnabled) {
					logger.Error("An error trying to dispose resources", exception);
				}
			}
		}

		private void initializePlayer () {
			// that code will initialize factory if it is not initialized
			if (factory == null) {
				// this part of code will try to deploy VLC if it is necessary.
				// main idea - unzip libraries to specific place.
				VlcDeployment deployment = VlcDeployment.Default;
				// install library if it doesn't exist
				// NOTE: first parameter tells to check hash of deployed files to be sure about vlc version without loading it.
				// since vlc library can be initialized only once it is important not to load it during check
				// but it is still possible to check vlc version using libvlc_get_version, so if you want - pass second parameter
				// as 'true'.
				if (!deployment.CheckVlcLibraryExistence (false, false)) {
					// install library
					deployment.Install (true);
				}
				// this is path to plugins. very important part of initialization of vlc.
				string path = Path.Combine (Path.GetDirectoryName (Assembly.GetEntryAssembly ().Location), "plugins");
				// we can use a lot of parameters there.
				// refer to vlc --help to learn more.
				factory = new VlcMediaLibraryFactory (new string[] { "--reset-config", 
					"--no-snapshot-preview", 
					"--aspect-ratio=16:9", 
					"--ignore-config", 
					"--intf", "rc", 
					"--no-osd", 
					"--plugin-path", path });
				// tell our factory to create new version of players
				// NOTE: if you change this to 'false' old version of implementation will be created
				// NOTE: old implementation remains for backward compatibility
				factory.CreateSinglePlayers = true;
				// we going to output to NSView instance:
				PlayerOutput output = new PlayerOutput (new VlcNativeMediaWindow (VideoView.NativePointer, VlcWindowType.NSObject));
				// we can save stream to a file:
				// output.Files.Add(new OutFile("filePath"));
				player = (VlcSinglePlayer)factory.CreatePlayer (output);
				// add event receiver to get known about player events
				player.EventsReceivers.Add (new EventReceiver (this));
			}
		}

		/// <summary>
		/// This is the way we get events from our players. 
		/// </summary>
		private sealed class EventReceiver : PlayerEventsReceiver {

			private WindowController controller;

			/// <summary>
			/// Constructs event receiver.
			/// </summary>
			/// <param name="controller">
			/// Owner.
			/// </param>
			public EventReceiver (WindowController controller) {
				this.controller = controller;
			}

			/// <summary>
			/// This method is called when player position is changed.
			/// </summary>
			public override void OnPositionChanged () {
				if (controller.PositionSlider.IsMouseDown) {
					// don't update position until mouse release
					return;
				}
				// NOTE: this part should be called in main thread
				controller.VideoView.Invoke (new Action<WindowController> (delegate(WindowController c) {
					// raise event that the position is changed
					// so bound control will refresh its state.
					c.WillChangeValueForKey ("position");
					c.DidChangeValueForKey ("position");
				}), controller);
			}
		}
	}
}
























