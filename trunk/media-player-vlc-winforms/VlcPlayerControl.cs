#region

using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Common.Logging;
using DZ.MediaPlayer.Vlc.Io;

#endregion

namespace DZ.MediaPlayer.Vlc.WindowsForms {
	/// <summary>
	/// User control provides straightforward access to libvlcnet features.
	/// </summary>
	public partial class VlcPlayerControl : UserControl {
		
		private MediaInput currentPlaying;
		private bool isInitialized;
		private MediaLibraryFactory mediaLibraryFactory;
		private Player player;
		private VlcPlayerControlState state = VlcPlayerControlState.Idle;
		private int volume = 50;

		/// <summary>
		/// Some error occured.
		/// </summary>
		public EventHandler EncounteredError;

		/// <summary>
		/// End of media reached.
		/// </summary>
		public EventHandler EndReached;

		/// <summary>
		/// Position of player is changed.
		/// </summary>
		public EventHandler PositionChanged;

		/// <summary>
		/// State of player changed.
		/// </summary>
		public EventHandler StateChanged;

		/// <summary>
		/// Player is stopped.
		/// </summary>
		public EventHandler Stopped;

		/// <summary>
		/// Time of player changed.
		/// </summary>
		public EventHandler TimeChanged;

		#region Nested type : VlcPlayerEventsReceiver

		/// <summary>
		/// Subscriber to the several VLC events.
		/// </summary>
		private sealed class VlcPlayerControlEventsReceiver : PlayerEventsReceiver {
			private readonly VlcPlayerControl control;

			/// <summary>
			/// Instantiates class which receives events and translates them to <see cref="VlcPlayerControl"/> instance.
			/// </summary>
			/// <param name="control"></param>
			public VlcPlayerControlEventsReceiver(VlcPlayerControl control) {
				if (control == null) {
					throw new ArgumentNullException("control");
				}
				this.control = control;
			}

			public override void OnEndReached() {
                control.setCurrentState(VlcPlayerControlState.Idle);
				EventHandler handler = control.EndReached;
				if (handler != null) {
					handler(control, EventArgs.Empty);
				}
			}

			public override void OnPositionChanged() {
				EventHandler handler = control.PositionChanged;
				if (handler != null) {
					handler(control, EventArgs.Empty);
				}
			}

			public override void OnEncounteredError() {
				EventHandler handler = control.EncounteredError;
				if (handler != null) {
					handler(control, EventArgs.Empty);
				}
			}

			public override void OnStateChanged() {
				EventHandler handler = control.StateChanged;
				if (handler != null) {
					handler(control, EventArgs.Empty);
				}
			}

			public override void OnStopped() {
				EventHandler handler = control.Stopped;
				if (handler != null) {
					handler(control, EventArgs.Empty);
				}
			}

			public override void OnTimeChanged() {
				EventHandler handler = control.TimeChanged;
				if (handler != null) {
					handler(control, EventArgs.Empty);
				}
			}
		}

		#endregion

		/// <summary>
		/// Default constructor.
		/// </summary>
		public VlcPlayerControl() {
			InitializeComponent();
		}

		/// <summary>
		/// Is VCL subsystem initialized.
		/// </summary>
		public bool IsInitialized {
			get {
				return (isInitialized);
			}
		}

		/// <summary>
		/// Returns player backed by control.
		/// </summary>
		public Player Player {
			get {
				return (player);
			}
		}

		/// <summary>
		/// Current playing item.
		/// </summary>
		public MediaInput CurrentPlaying {
			get {
				return (currentPlaying);
			}
		}

		/// <summary>
		/// Position of playing movie.
		/// (0.0 - 1.0).
		/// </summary>
		public double Position {
			get {
				if ((state != VlcPlayerControlState.Paused) && (state != VlcPlayerControlState.Playing)) {
					return (0);
				}
				//
				try {
					return (player.Position);
				} catch {
					Stop();
					throw;
				}
			}
			set {
				if ((state != VlcPlayerControlState.Paused) && (state != VlcPlayerControlState.Playing)) {
					return;
				}
				//
				try {
					player.Position = (float) value;
				} catch {
					Stop();
					throw;
				}
			}
		}

		/// <summary>
		/// Current playing time.
		/// </summary>
		public TimeSpan Time {
			get {
				if ((state != VlcPlayerControlState.Paused) && (state != VlcPlayerControlState.Playing)) {
					return (TimeSpan.Zero);
				}
				//
				try {
					return (player.Time);
				} catch (Exception) {
					Stop();
					throw;
				}
			}
			set {
				if ((state == VlcPlayerControlState.Playing) || (state == VlcPlayerControlState.Paused)) {
					try {
						player.Time = value;
					} catch {
						Stop();
						throw;
					}
				}
			}
		}

		/// <summary>
		/// Volume level.
		/// </summary>
		public int Volume {
			get {
				return (volume);
			}
			set {
				if ((volume < 0) || (volume > 200)) {
					throw new ArgumentException("Argument is out of range.", "value");
				}
				//
				if (volume != value) {
					volume = value;
					//
					if (state != VlcPlayerControlState.Idle) {
						player.Volume = volume;
					}
				}
			}
		}

		/// <summary>
		/// Control state.
		/// </summary>
		public VlcPlayerControlState State {
			get {
				return state;
			}
		}

		private void lazyInitialize() {
			if (!isInitialized) {
				Initialize();
			}
		}

		private static string getStartupPath() {
			return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
		}

        /// <summary>
        /// Initializes VLC resources will be used by control. Default VLC
        /// parameters is used in this overload.
        /// </summary>
        /// <exception cref="InvalidOperationException">Reinitialization is not allowed.</exception>
        /// <exception cref="MediaPlayerException">Media player cannot be initialized.</exception>
        public void Initialize() {
            Initialize(null);
        }

		/// <summary>
		/// Initializes VLC resources will be used by control. Default VLC
		/// parameters is used in this overload.
		/// </summary>
		/// <exception cref="InvalidOperationException">Reinitialization is not allowed.</exception>
		/// <exception cref="MediaPlayerException">Media player cannot be initialized.</exception>
		public void Initialize(ISynchronizeInvoke syncEventsTo) {
			string path = Path.Combine(getStartupPath(), "plugins");
			Initialize(syncEventsTo, new string[] {
				"--reset-config",
				"--no-snapshot-preview",
				"--ignore-config",
				"--intf", "rc",
				"--no-osd",
				"--plugin-path", path
			}, null);
		}

		/// <summary>
		/// Initializes VLC resources will be used by control.
		/// </summary>
		/// <param name="vlcParameters">Parameters passed to VLC library</param>
		/// <exception cref="InvalidOperationException">Reinitialization is not allowed.</exception>
		/// <exception cref="MediaPlayerException">Media player cannot be initialized.</exception>
		public void Initialize(ISynchronizeInvoke syncEventsTo, string[] vlcParameters) {
            Initialize(syncEventsTo, vlcParameters, null);
		}

		/// <summary>
		/// Initializes VLC resources will be used by control.
		/// </summary>
		/// <param name="vlcParameters">Parameters passed to VLC library</param>
		/// <param name="handler">Callback which can be used to initialize factory instance parameters.</param>
		/// <exception cref="InvalidOperationException">Reinitialization is not allowed.</exception>
		/// <exception cref="MediaPlayerException">Media player cannot be initialized.</exception>
		public void Initialize(ISynchronizeInvoke syncEventsTo, string[] vlcParameters, VlcMediaLibraryFactoryInitHandler handler) {
			if (isInitialized) {
				throw new InvalidOperationException("This object does not support multi time initialization.");
			}
			//
			isInitialized = true;
			initializeFactory(vlcParameters);
			if (handler != null) {
				handler(mediaLibraryFactory as VlcMediaLibraryFactory);
			}
			//
			player = mediaLibraryFactory.CreatePlayer(new PlayerOutput(vlcWindowControl.Window));
			// Subscribe to events
			VlcPlayerControlEventsReceiver receiver = new VlcPlayerControlEventsReceiver(this);
            player.EventsReceivers.Add(new SynchronizedEventsReceiver(syncEventsTo ?? this, receiver, true));
		}

		private void initializeFactory(string[] vlcParameters) {
			if (vlcParameters == null) {
				vlcParameters = new string[] {
				};
			}
			mediaLibraryFactory = new VlcMediaLibraryFactory(vlcParameters);
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing) {
				Uninitialize();
			}
			//
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// Starts playing.
		/// </summary>
		/// <param name="mediaInput">Media to play.</param>
		public void Play(MediaInput mediaInput) {
			if (mediaInput == null) {
				throw new ArgumentNullException("mediaInput");
			}
			//
			lazyInitialize();
			//
			try {
				currentPlaying = mediaInput;
				player.SetNextMediaInput(mediaInput);
				player.Volume = volume;
				player.PlayNext();
				//
				setCurrentState(VlcPlayerControlState.Playing);
			} catch {
				Stop();
				throw;
			}
		}

		private void setCurrentState(VlcPlayerControlState _state) {
			// control state is not the same as player state
            if (state != _state) {
                state = _state;
                EventHandler handler = StateChanged;
                if (handler != null) {
                    handler(this, EventArgs.Empty);
                }
            }
		}

		/// <summary>
		/// Pauses or resumes playing current movie.
		/// If no movie is loaded, no actions will be given.
		/// </summary>
		public void PauseOrResume() {
			if ((state != VlcPlayerControlState.Playing) && (state != VlcPlayerControlState.Paused)) {
				return;
			}
			//
			lazyInitialize();
			//
			if (state == VlcPlayerControlState.Playing) {
				try {
					player.Pause();
					setCurrentState(VlcPlayerControlState.Paused);
				} catch {
					Stop();
					throw;
				}
			} else if (state == VlcPlayerControlState.Paused) {
				try {
					player.Resume();
					setCurrentState(VlcPlayerControlState.Playing);
				} catch {
					Stop();
					throw;
				}
			}
		}

		/// <summary>
		/// Stops currently playing movie if player is not empty.
		/// </summary>
		public void Stop() {
			if (state != VlcPlayerControlState.Idle) {
				lazyInitialize();
				//
				try {
					player.Stop();
				} finally {
					currentPlaying = null;
					setCurrentState(VlcPlayerControlState.Idle);
				}
			}
		}

	    public void Uninitialize() {
            // Clean up vlclib resources
            if (player != null) {
                player.Dispose();
            }
            if (mediaLibraryFactory != null) {
                mediaLibraryFactory.Dispose();
            }
	    }
	}

	public delegate void VlcMediaLibraryFactoryInitHandler(VlcMediaLibraryFactory factory);
}