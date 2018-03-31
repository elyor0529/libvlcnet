using System;
using System.Threading;
using System.Runtime.InteropServices;
using NUnit.Framework;
using DZ.MediaPlayer.Vlc.Internal.Interop;
using DZ.MediaPlayer.Vlc.Deployment;
using DZ.MediaPlayer.Vlc.Io;
using NLog;

namespace DZ.MediaPlayer.Vlc.Tests {
	
	[TestFixture]
	public class VlcMediaTest : BaseVlcPlayerTest {
		
		[Test]
		public void TestPreparsedMediaCreate() {
			using (VlcMediaLibraryFactory factory = CreateNewFactory()) {
				factory.CreateSinglePlayers = true;
				PlayerOutput nullOutput = new PlayerOutput();
				//
				string path = GetSampleAudioPath();
				if (!System.IO.File.Exists(path)) {
					Assert.Ignore("The sample file doesn't exists. Ignoring.");
				}
				MediaInput input = new MediaInput(MediaInputType.File,
					path);
				//
				using (Player player = factory.CreatePlayer(nullOutput)) {
					PreparsedMedia media = player.ParseMediaInput(input);
					media = player.ParseMediaInput(input);
					//
					Assert.IsTrue(media.ContainsAudio);
					Assert.IsFalse(media.ContainsVideo);
					//
					AudioTrackInfo[] tracks = media.GetAudioTracks();
					Assert.IsTrue(tracks.Length == 1, "There should be one audio track.");
					//
					VideoTrackInfo[] tracksVideo = media.GetVideoTracks();
					Assert.IsTrue(tracksVideo.Length == 0, "There shouldn't be any video tracks.");
				}
			}
		}
	}
	
	/// <summary>
	/// Test <see cref="Player.State"/> property behaviour.
	/// </summary>
	[TestFixture]
	public class VlcPlayerTestState : BaseVlcPlayerTest {
		
		/// <summary>
		/// Test if <see cref="Player.State"/> is valid after Play, Stop, Pause, Resume calls.
		/// Test target is <see cref="VlcSinglePlayer"/>.
		/// </summary>
		[Test]
		public void TestPlayStateSinglePlayer() {
			using (VlcMediaLibraryFactory factory = CreateNewFactory()) {
				//
				factory.CreateSinglePlayers = true;
				testPlayerState(factory);
			}
		}
		
		/// <summary>
		/// Test if <see cref="Player.State"/> is valid after Play, Stop, Pause, Resume calls.
		/// Test target is <see cref="VlcPlayer"/>
		/// </summary>
		[Test]
		public void TestPlayStateDoublePlayer() {
			using (VlcMediaLibraryFactory factory = CreateNewFactory()) {
				//
				factory.CreateSinglePlayers = false;
				testPlayerState(factory);
			}
		}
		
		private void testPlayerState(MediaLibraryFactory factory) {
			PlayerOutput nullOutput = new PlayerOutput();
			//
			string path = GetSampleAudioPath();
			if (!System.IO.File.Exists(path)) {
				Assert.Ignore("The sample file doesn't exists. Ignoring.");
			}
			MediaInput input = new MediaInput(MediaInputType.File,
				path);
			//
			using (Player player = factory.CreatePlayer(nullOutput)) {
				testPlayerState(player, input);
			}
		}
		
		private void testPlayerState(Player player, MediaInput input) {
			Assert.AreEqual(player.State, PlayerState.Stopped);
            
			//
			player.SetMediaInput(input);
			//
			player.Play();
			Assert.AreEqual(PlayerState.Playing, player.State);
			Thread.Sleep(1000);
			//
			player.Pause();
			Assert.AreEqual(PlayerState.Paused, player.State);
			Thread.Sleep(1000);
			//
			player.Resume();
			Assert.AreEqual(PlayerState.Playing, player.State);
			Thread.Sleep(1000);
			//
			player.Stop();
			Assert.AreEqual(PlayerState.Stopped, player.State);
			//
			TestState state = new TestState();
			state.player = player;
			state.onEndReachedCalled = false;
			state.handle = new EventWaitHandle(false, EventResetMode.AutoReset);
			state.state = 0;
			//
			try {
				EventsBasedPlayerEventsReceiver receiver = 
					new EventsBasedPlayerEventsReceiver(state);
				receiver.EndReached += onEndReached;
				player.EventsReceivers.Add(receiver);
				//
				player.Play();
				player.Position = 0.90f;
				//
				state.handle.WaitOne(5000);
			    //player.Stop();
				Assert.IsTrue(state.onEndReachedCalled, 
				              "EndReached event is expected.");
				Assert.AreEqual(PlayerState.Stopped, player.State);
			} finally {
				state.handle.Close();
				state.handle = null;
			}
		}

		void onEndReached (object sender, EventArgs e) {
			TestState state = (TestState)sender;
			Player player = state.player;
			state.state = player.State;
			//
			state.onEndReachedCalled = true;
			state.handle.Set();
		}
		
		private class TestState {
			public Player player;
			public bool onEndReachedCalled;
			public EventWaitHandle handle;
			public PlayerState state;
		}
	}
}

