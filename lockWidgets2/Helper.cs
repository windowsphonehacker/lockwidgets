using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using Microsoft.Phone.BackgroundAudio;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Xml.Serialization;

namespace lockWidgets2
{
    public class Helper : AudioPlayerAgent
    {
        
        

        public Helper()
        {
            
        }

        /// Code to execute on Unhandled Exceptions
        private void AudioPlayer_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        /// <summary>
        /// Called when the playstate changes, except for the Error state (see OnError)
        /// </summary>
        /// <param name="player">The BackgroundAudioPlayer</param>
        /// <param name="track">The track playing at the time the playstate changed</param>
        /// <param name="playState">The new playstate of the player</param>
        /// <remarks>
        /// Play State changes cannot be cancelled. They are raised even if the application
        /// caused the state change itself, assuming the application has opted-in to the callback.
        /// 
        /// Notable playstate events: 
        /// (a) TrackEnded: invoked when the player has no current track. The agent can set the next track.
        /// (b) TrackReady: an audio track has been set and it is now ready for playack.
        /// 
        /// Call NotifyComplete() only once, after the agent request has been completed, including async callbacks.
        /// </remarks>
        protected override void OnPlayStateChanged(BackgroundAudioPlayer player, AudioTrack track, PlayState playState)
        {
            switch (playState)
            {
                case PlayState.TrackEnded:
                    player.Track = GetNextTrack();
                    break;

                case PlayState.TrackReady:
                    player.Volume = 1.0;
                    player.Play();
                    break;

                case PlayState.Shutdown:
                    // TODO: Handle the shutdown state here (e.g. save state)
                    break;

                case PlayState.Unknown:
                    break;

                case PlayState.Stopped:
                    break;

                case PlayState.Paused:
                    break;

                case PlayState.Playing:
                    break;

                case PlayState.BufferingStarted:
                    break;

                case PlayState.BufferingStopped:
                    break;

                case PlayState.Rewinding:
                    break;

                case PlayState.FastForwarding:
                    break;
            }

            NotifyComplete();
        }


        /// <summary>
        /// Called when the user requests an action using application/system provided UI
        /// </summary>
        /// <param name="player">The BackgroundAudioPlayer</param>
        /// <param name="track">The track playing at the time of the user action</param>
        /// <param name="action">The action the user has requested</param>
        /// <param name="param">The data associated with the requested action.
        /// In the current version this parameter is only for use with the Seek action,
        /// to indicate the requested position of an audio track</param>
        /// <remarks>
        /// User actions do not automatically make any changes in system state; the agent is responsible
        /// for carrying out the user actions if they are supported.
        /// 
        /// Call NotifyComplete() only once, after the agent request has been completed, including async callbacks.
        /// </remarks>
        protected override void OnUserAction(BackgroundAudioPlayer player, AudioTrack track, UserAction action, object param)
        {
            switch (action)
            {
                case UserAction.Play:
                    
                    if (PlayState.Playing != player.PlayerState)
                    {
                        player.Track = new AudioTrack();
                    }
                    break;

                case UserAction.Stop:
                    try
                    {
                        player.Stop();
                    }
                    catch
                    {
                    }
                    break;

                case UserAction.Pause:
                    if (PlayState.Playing == player.PlayerState)
                    {
                        player.Pause();
                    }
                    break;

                case UserAction.FastForward:
                    // Fast Forward only works with non-MSS clients.
                    // If the Source is null, we are streaming an MSS.
                    if (track.Source != null)
                    {
                        player.FastForward();
                    }
                    break;

                case UserAction.Rewind:
                    // Rewind only works with non-MSS clients.
                    // If the Source is null, we are streaming an MSS.
                    if (track.Source != null)
                    {
                        player.Rewind();
                    }
                    break;

                case UserAction.Seek:
                    // Seek only works with non-MSS clients.
                    // If the Source is null, we are streaming an MSS.
                    if (track.Source != null)
                    {
                        player.Position = (TimeSpan)param;
                    }
                    break;

                case UserAction.SkipNext:
                    player.Track = GetNextTrack();
                    break;

                case UserAction.SkipPrevious:
                    player.Track = GetPreviousTrack();
                    break;
            }

            NotifyComplete();
        }


        /// <summary>
        /// Implements the logic to get the next AudioTrack instance.
        /// In a playlist, the source can be from a file, a web request, etc.
        /// </summary>
        /// <remarks>
        /// The AudioTrack URI determines the source, which can be:
        /// (a) Isolated-storage file (Relative URI, represents path in the isolated storage)
        /// (b) HTTP URL (absolute URI)
        /// (c) MediaStreamSource (null)
        /// </remarks>
        /// <returns>an instance of AudioTrack, or null if the playback is completed</returns>
        private AudioTrack GetNextTrack()
        {
            System.Diagnostics.Debug.WriteLine("next audio");
            return new AudioTrack();
        }


        /// <summary>
        /// Implements the logic to get the previous AudioTrack instance.
        /// </summary>
        /// <remarks>
        /// The AudioTrack URI determines the source, which can be:
        /// (a) Isolated-storage file (Relative URI, represents path in the isolated storage)
        /// (b) HTTP URL (absolute URI)
        /// (c) MediaStreamSource (null)
        /// </remarks>
        /// <returns>an instance of AudioTrack, or null if previous track is not allowed</returns>
        private AudioTrack GetPreviousTrack()
        {
            return new AudioTrack();
        }

        /// <summary>
        /// Called whenever there is an error with playback, such as an AudioTrack not downloading correctly
        /// </summary>
        /// <param name="player">The BackgroundAudioPlayer</param>
        /// <param name="track">The track that had the error</param>
        /// <param name="error">The error that occured</param>
        /// <param name="isFatal">If true, playback cannot continue and playback of the track will stop</param>
        /// <remarks>
        /// This method is not guaranteed to be called in all cases. For example, if the background agent 
        /// itself has an unhandled exception, it won't get called back to handle its own errors.
        /// </remarks>
        protected override void OnError(BackgroundAudioPlayer player, AudioTrack track, Exception error, bool isFatal)
        {
            int o;
            //CSharp___DllImport.DllImportCaller.lib.MessageBox7("managed error: " + error.Message.ToString(), "lockwidgets", 0, out o);
            

        }
        void msgbox(string str)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                int q = 0;
                //CSharp___DllImport.DllImportCaller.lib.MessageBox7(str, "lockwidgets", 0, out q);
            });
        }
        /// <summary>
        /// Called when the agent request is getting cancelled
        /// </summary>
        /// <remarks>
        /// Once the request is Cancelled, the agent gets 5 seconds to finish its work,
        /// by calling NotifyComplete()/Abort().
        /// </remarks>
        protected override void OnCancel()
        {
            msgbox("I was canceled.");
            // Do any necessary cleanup work, such as saving state.
        }
    }
}
