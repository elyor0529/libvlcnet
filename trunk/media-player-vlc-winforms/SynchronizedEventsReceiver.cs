using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;

namespace DZ.MediaPlayer.Vlc.WindowsForms {
	/// <summary>
	/// Event receiver which wraps events and synchronizes them with the main thread of Windows.Forms application.
	/// </summary>
	public sealed class SynchronizedEventsReceiver : PlayerEventsReceiver {
		private readonly bool useThreadPool;
		private readonly PlayerEventsReceiver delegateTo;
		private readonly ISynchronizeInvoke invoker;

		/// <summary>
		/// Instantiates events receiver with specified parameters.
		/// </summary>
		/// <param name="invoker"><see cref="ISynchronizeInvoke"/> used to synchronize with.</param>
		/// <param name="delegateTo"><see cref="PlayerEventsReceiver"/> to delegate invokes to</param>
		/// <param name="useThreadPool">Set this value to <code>true</code> to wrap synchronization calls with <see cref="ThreadPool.QueueUserWorkItem(System.Threading.WaitCallback)"/>. 
		/// This maybe necessary to reduce delays during events.</param>
		public SynchronizedEventsReceiver(ISynchronizeInvoke invoker, PlayerEventsReceiver delegateTo, bool useThreadPool) {
			
			if (invoker == null) {
				throw new ArgumentNullException("invoker");
			}
			if (delegateTo == null) {
				throw new ArgumentNullException("delegateTo");
			}
			this.invoker = invoker;
			this.delegateTo = delegateTo;
			this.useThreadPool = useThreadPool;
		}

        private void DoInvoke(Delegate handler, object[] parameters) {
            this.invoker.BeginInvoke(handler, parameters);
        }

	    public override void OnEncounteredError() {
			if (useThreadPool) {
				ThreadPool.QueueUserWorkItem(OnEncounteredErrorInternal);
			} else {
				OnEncounteredErrorInternal(null);
			}
		}

		private void OnEncounteredErrorInternal(object param) {
			if (invoker.InvokeRequired) {
				DoInvoke(new Action<Object>(OnEncounteredErrorInternal), new object[] {
					param
				});
			} else {
				delegateTo.OnEncounteredError();
			}
		}

		public override void OnEndReached() {
			if (useThreadPool) {
				ThreadPool.QueueUserWorkItem(OnEndReachedInternal);
			} else {
				OnEndReachedInternal(null);
			}
		}

		private void OnEndReachedInternal(object param) {
			if (invoker.InvokeRequired) {
				DoInvoke(new Action<Object>(OnEndReachedInternal), new object[] {
					param
				});
			} else {
				delegateTo.OnEndReached();
			}
		}

		public override void OnPositionChanged() {
			if (useThreadPool) {
				ThreadPool.QueueUserWorkItem(OnPositionChangedInternal);
			} else {
				OnPositionChangedInternal(null);
			}
		}

		private void OnPositionChangedInternal(object param) {
			if (invoker.InvokeRequired) {
				DoInvoke(new Action<Object>(OnPositionChangedInternal), new object[] {
					param
				});
			} else {
				delegateTo.OnPositionChanged();
			}
		}
		
		public override void OnStateChanged() {
			if (useThreadPool) {
				ThreadPool.QueueUserWorkItem(OnStateChangedInternal);
			} else {
				OnStateChangedInternal(null);
			}
		}

		private void OnStateChangedInternal(object param) {
			if (invoker.InvokeRequired) {
				DoInvoke(new Action<Object>(OnStateChangedInternal), new object[] {
					param
				});
			} else {
				delegateTo.OnStateChanged();
			}
		}
		
		public override void OnStopped() {
			if (useThreadPool) {
				ThreadPool.QueueUserWorkItem(OnStoppedInternal);
			} else {
				OnStoppedInternal(null);
			}
		}

		private void OnStoppedInternal(object param) {
			if (invoker.InvokeRequired) {
				DoInvoke(new Action<Object>(OnStoppedInternal), new object[] {
					param
				});
			} else {
				delegateTo.OnStopped();
			}
		}
		
		public override void OnTimeChanged() {
			if (useThreadPool) {
				ThreadPool.QueueUserWorkItem(OnTimeChangedInternal);
			} else {
				OnTimeChangedInternal(null);
			}
		}

		private void OnTimeChangedInternal(object param) {
			if (invoker.InvokeRequired) {
				DoInvoke(new Action<Object>(OnTimeChangedInternal), new object[] {
					param
				});
			} else {
				delegateTo.OnTimeChanged();
			}
		}
	}
}
