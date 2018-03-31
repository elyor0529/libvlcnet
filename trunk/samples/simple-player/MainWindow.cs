using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Common.Logging;
using DZ.MediaPlayer;
using DZ.MediaPlayer.Vlc.Io;
using DZ.MediaPlayer.Vlc.WindowsForms;
using SimplePlayer.Playlist;
using SimplePlayer.MediaInfo;

namespace SimplePlayer {
	/// <summary>
	/// Main form of application.
	/// </summary>
	public partial class MainWindow : Form {
		private static readonly ILog logger = LogManager.GetLogger(typeof(MainWindow));
		private VideoWindow videoWindow;
		private bool shouldUpdateTrack;

		#region Initialization and clean up

		/// <summary>
		/// Default constructor.
		/// </summary>
		public MainWindow() {
			InitializeComponent();
			//
            playlistEditorControl.Playlist.PlaylistItemEntered += Playlist_PlaylistItemEntered;
            //
			initializeVideoWindow();
			initializeStatusBar();
			initializeTrackbarPosition();
			initializeTrackbarVolume();
			//
			shouldUpdateTrack = true;
			trackbarPosition.MouseDown += onTrackPositionMouseDown;
			trackbarPosition.MouseUp += onTrackPositionMouseUp;
		}

		private void onTrackPositionMouseUp(object sender, MouseEventArgs e) {
			shouldUpdateTrack = true;
		}

		private void onTrackPositionMouseDown(object sender, MouseEventArgs e) {
			shouldUpdateTrack = false;
		}

		private void initializeVlcPlayerControl(bool showVideoWindow) {
			initializeVideoWindow();
			//
			if (showVideoWindow) {
				videoWindow.Show();
			}
			//
			if (!videoWindow.VlcPlayerControl.IsInitialized) {
				videoWindow.VlcPlayerControl.Initialize(this);
			}
		}

		private void initializeVideoWindow() {
			if ((videoWindow == null) || (videoWindow.IsDisposed)) {
				if (videoWindow != null) {
					videoWindow.Closing -= VideoWindowOnClosing;
				}
				videoWindow = new VideoWindow();
				videoWindow.Closing += VideoWindowOnClosing;
                if (!videoWindow.VlcPlayerControl.IsInitialized) {
                    videoWindow.VlcPlayerControl.Initialize(this);
                }
				//
				videoWindow.VlcPlayerControl.StateChanged += vlc_onStateChanged;
				videoWindow.VlcPlayerControl.PositionChanged += vlc_onPositionChanged;
				videoWindow.VlcPlayerControl.EndReached += vlc_onEndReached;
                //
			}
		}

		private void initializeStatusBar() {
			statusStrip.Items["playerStatus"].Text = Convert.ToString(videoWindow.VlcPlayerControl.State);
			statusStrip.Items["currentTime"].Text = String.Format("{0}", videoWindow.VlcPlayerControl.Time);
		}

		private void initializeTrackbarPosition() {
			trackbarPosition.Tag = true;
		}

		private void initializeTrackbarVolume() {
			trackbarVolume.Value = (int)((1.0 * videoWindow.VlcPlayerControl.Volume / 100) * trackbarVolume.Maximum);
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			// It is necessary to call videoWindow.Dispose() explicitly because
			// if it is in hidden state, Dispose() will not be called from anywhere else
            if (disposing) {
                if (videoWindow != null) {
                    videoWindow.VlcPlayerControl.Uninitialize();
                }
            }
		    //
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#endregion

		#region Internal objects events handling routine

		private void vlc_onStateChanged(object sender, EventArgs args) {
			VlcPlayerControlState currentState = videoWindow.VlcPlayerControl.State;
			//
			switch (currentState) {
				case VlcPlayerControlState.Idle: {
						buttonPlay.Text = "Play";
						break;
					}
				case VlcPlayerControlState.Paused: {
						buttonPlay.Text = "Resume";
						break;
					}
				case VlcPlayerControlState.Playing: {
						buttonPlay.Text = "Pause";
						break;
					}
			}
			//
			statusStrip.Items["playerStatus"].Text = Convert.ToString(currentState);
		}

		private void vlc_onEndReached(object sender, EventArgs e) {
			if (InvokeRequired) {
				Invoke(new ThreadStart(playNextAfterEnd));
			} else {
				playNextAfterEnd();
			}
		}

		private void playNextAfterEnd() {
			buttonPlaynext_Click(this, EventArgs.Empty);
		}

		private void vlc_onPositionChanged(object sender, EventArgs e) {
			if (InvokeRequired) {
				Invoke(new ThreadStart(updatePositionDependentInfo));
			} else {
				updatePositionDependentInfo();
			}
		}

		private void updatePositionDependentInfo() {
			updateTrackBar();
			updateStatusBar();
		}

		private void updateTrackBar() {
			if (!shouldUpdateTrack)
				return;
			trackbarPosition.Tag = false;
			try {
				int newTrackbarPositionValue = (int) (videoWindow.VlcPlayerControl.Position*1000);
				if (newTrackbarPositionValue != trackbarPosition.Value) {
					trackbarPosition.Value = newTrackbarPositionValue > trackbarPosition.Maximum
					                         	? trackbarPosition.Maximum
					                         	: newTrackbarPositionValue;
				}
			} finally {
				trackbarPosition.Tag = true;
			}
			
		}

		private void updateStatusBar() {
			TimeSpan time = videoWindow.VlcPlayerControl.Time;
			statusStrip.Items["currentTime"].Text = String.Format("{0:D2}:{1:D2}:{2:D2}", time.Hours, time.Minutes, time.Seconds);
		}

		void Playlist_PlaylistItemEntered(object sender, PlaylistItemEnteredEventArgs e) {
			PlaylistItem currentItem = playlistEditorControl.Playlist.CurrentItem;
			if (currentItem == null) {
				return;
			}
			//
			try {
				if (currentItem.MediaInput.Type == MediaInputType.File) {
#if USE_MEDIA_INFO
					BasicVideoInformation information = MediaInfoHelper.GetBasicVideoInfo(currentItem.MediaInput.Source);
					if (String.IsNullOrEmpty(information.AudioCodec) && String.IsNullOrEmpty(information.VideoCodec)) {
						currentItem.IsError = true;
						stopPlayer();
						return;
					}
					//
					initializeVlcPlayerControl(!String.IsNullOrEmpty(information.VideoCodec));
					//
#else
					//
					initializeVlcPlayerControl(true);
					//
#endif
				} else {
					initializeVlcPlayerControl(false);
				}
				
				
				currentItem.IsError = false;
				videoWindow.VlcPlayerControl.Play(currentItem.MediaInput);
			} catch (FileNotFoundException exc) {
				if (logger.IsWarnEnabled) {
					logger.Warn(String.Format("File referenced from playlist item was not found. Exception : {0}", exc));
				}
				//
				currentItem.IsError = true;
			} catch (Exception exc) {
				if (logger.IsErrorEnabled) {
					logger.Error("Cannot start playing.", exc);
				}
				//
				MessageBox.Show(String.Format("Cannot start playing : {0}", exc));
				currentItem.IsError = true;
			}
		}

		private void stopPlayer() {
			try {
				if (videoWindow.VlcPlayerControl.State != VlcPlayerControlState.Idle) {
					videoWindow.VlcPlayerControl.Stop();
					updateTrackBar();
					updateStatusBar();
				}
			} catch (Exception exc) {
				if (logger.IsErrorEnabled) {
					logger.Error("Cannot stop player.", exc);
				}
				//
				MessageBox.Show(String.Format("Cannot stop player : {0}", exc));
			}
		}

		#endregion
		
		private void startPlay() {
			PlaylistItem currentItem = playlistEditorControl.Playlist.CurrentItem;
			//
			try {
				if (currentItem.MediaInput.Type == MediaInputType.File) {
#if USE_MEDIA_INFO
					BasicVideoInformation information = MediaInfoHelper.GetBasicVideoInfo(currentItem.MediaInput.Source);
					if (String.IsNullOrEmpty(information.AudioCodec) && String.IsNullOrEmpty(information.VideoCodec)) {
						currentItem.IsError = true;
						stopPlayer();
						return;
					}
					initializeVlcPlayerControl(!string.IsNullOrEmpty(information.VideoCodec));
#else
					initializeVlcPlayerControl(true);
#endif
				}
				else {
					initializeVlcPlayerControl(true);
				}
				//
				currentItem.IsError = false;
				switch (videoWindow.VlcPlayerControl.State) {
					case VlcPlayerControlState.Idle: {
							videoWindow.VlcPlayerControl.Play(currentItem.MediaInput);
							break;
						}
					case VlcPlayerControlState.Paused: {
							videoWindow.VlcPlayerControl.PauseOrResume();
							break;
						}
					case VlcPlayerControlState.Playing: {
							videoWindow.VlcPlayerControl.PauseOrResume();
							break;
						}
				}
			} catch (FileNotFoundException exc) {
				if (logger.IsWarnEnabled) {
					logger.Warn(String.Format("File referenced from playlist item was not found. Exception : {0}", exc));
				}
				//
				currentItem.IsError = true;
			} catch (Exception exc) {
				if (logger.IsErrorEnabled) {
					logger.Error("Cannot start playing.", exc);
				}
				//
				MessageBox.Show(String.Format("Cannot start playing : {0}", exc));
				currentItem.IsError = true;
			}
		}

		#region UI events

		private void buttonPlay_Click(object sender, EventArgs e) {
			PlaylistItem currentItem = playlistEditorControl.Playlist.CurrentItem;
			if (currentItem == null) {
				return;
			}
			//
			startPlay();
		}

		private void buttonPlayback_Click(object sender, EventArgs e) {
			playlistEditorControl.Playlist.MovePrev();
			PlaylistItem currentItem = playlistEditorControl.Playlist.CurrentItem;
			if (currentItem == null) {
				return;
			}
			//
            stopPlayer();
			startPlay();
		}

		private void buttonStop_Click(object sender, EventArgs e) {
			stopPlayer();
		}

		private void buttonPlaynext_Click(object sender, EventArgs e) {
			playlistEditorControl.Playlist.MoveNext();
			PlaylistItem currentItem = playlistEditorControl.Playlist.CurrentItem;
			if (currentItem == null) {
				return;
			}
			//
            stopPlayer();
			startPlay();
		}

		/// <summary>
		/// Deny video window to close.
		/// </summary>
		private void VideoWindowOnClosing(object sender, CancelEventArgs args) {
			stopPlayer();
			args.Cancel = true;
			videoWindow.Hide();
		}

		private void openFilesToolStripMenuItem_Click(object sender, EventArgs e) {
#if USE_MEDIA_INFO
			using (MediaInfoLibrary mediaInfoLibrary = new MediaInfoLibrary()) {
				using (OpenFileDialog openFileDialog = new OpenFileDialog()) {
					openFileDialog.Multiselect = true;
					//
					DialogResult result = openFileDialog.ShowDialog();
					if ((result == DialogResult.OK) || (result == DialogResult.Yes)) {
						foreach (string fileName in openFileDialog.FileNames) {
							try {
								if (!File.Exists(fileName)) {
									continue;
								}
								BasicVideoInformation information = MediaInfoHelper.GetBasicVideoInfo(mediaInfoLibrary, fileName);
								if (String.IsNullOrEmpty(information.VideoCodec) && String.IsNullOrEmpty(information.AudioCodec)) {
									if (logger.IsDebugEnabled) {
										logger.Debug(String.Format("Not suitable file : {0}", fileName));
									}
									//
									continue;
								}
								//
								PlaylistItem playlistItem = new PlaylistItem(
									new MediaInput(MediaInputType.File, information.FileName),
									fileName,
									TimeSpan.FromMilliseconds(information.DurationMilliseconds));
								//
								playlistEditorControl.Playlist.Items.Add(playlistItem);
							} catch (Exception exc) {
								if (logger.IsErrorEnabled) {
									logger.Error(String.Format("Error during processing selected file list : {0}", exc));
								}
								//
							}
						}
					}
				}
			}
#else
			using (OpenFileDialog openFileDialog = new OpenFileDialog()) {
				openFileDialog.Multiselect = true;
				//
				DialogResult result = openFileDialog.ShowDialog();
				if ((result == DialogResult.OK) || (result == DialogResult.Yes)) {
					foreach (string fileName in openFileDialog.FileNames) {
						try {
							if (!File.Exists(fileName)) {
								continue;
							}
							//
							PlaylistItem playlistItem = new PlaylistItem(
								new MediaInput(MediaInputType.File, fileName),
								fileName, TimeSpan.FromMilliseconds(0));
							//
							playlistEditorControl.Playlist.Items.Add(playlistItem);
						} catch (Exception exc) {
							if (logger.IsErrorEnabled) {
								logger.Error(String.Format("Error during processing selected file list : {0}", exc));
							}
							//
						}
					}
				}
			}
#endif
		}

		private void openNetworkStreamToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using (OpenNetworkStreamDialog dialog = new OpenNetworkStreamDialog()) {
				DialogResult dialogResult = dialog.ShowDialog();
				if (dialogResult == DialogResult.OK) {
					string urlString = dialog.UrlString;
					if (urlString.Length > 0) {
						PlaylistItem playlistItem = new PlaylistItem(
							new MediaInput(MediaInputType.NetworkStream, urlString),
							urlString,
							TimeSpan.FromSeconds(0));
						playlistEditorControl.Playlist.Items.Add(playlistItem);
					}
				}
			}
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
			Close();
		}

		private void aboutToolStripMenuItem_Click(object sender, EventArgs e) {
			MessageBox.Show("Simple player ver. 0.1", "About..", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}

		private void trackbarPosition_ValueChanged(object sender, EventArgs e) {
			try {
				TrackBar trackBar = ((TrackBar)sender);
				if ((trackBar.Tag is Boolean) && ((Boolean)trackBar.Tag)) {
					//
					if (videoWindow.VlcPlayerControl.State == VlcPlayerControlState.Idle) {
						trackBar.Value = 0;
						return;
					}
					//
					float val = 1f*trackBar.Value/trackBar.Maximum;
					if (val != videoWindow.VlcPlayerControl.Position)
						videoWindow.VlcPlayerControl.Position = val;
				}
			} catch (Exception exc) {
				if (logger.IsErrorEnabled) {
					logger.Error("Cannot change position.", exc);
				}
				//
				MessageBox.Show(String.Format("Cannot change position : {0}", exc));
			}
		}

		#endregion

		private void trackbarVolume_ValueChanged(object sender, EventArgs e) {
			try {
				TrackBar trackBar = (TrackBar)sender;
				videoWindow.VlcPlayerControl.Volume = (int)((1.0 * trackBar.Value / trackBar.Maximum) * 100);
			} catch (Exception exc) {
				if (logger.IsErrorEnabled) {
					logger.Error("Cannot change volume level.", exc);
				}
				//
				MessageBox.Show(String.Format("Cannot change volume level: {0}", exc));
			}
		}
	}
}
