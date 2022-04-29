using System;
using System.Collections.Generic;
using agora_gaming_rtc;
using UnityEngine;

namespace Agora
{
  public class AudioChat : IUserAudioFrameDelegate
  {
    public AudioRawDataManager.OnPlaybackAudioFrameBeforeMixingHandler HandleAudioFrameForUser { get; set; }

    private IRtcEngine _engine;
    private IAudioRawDataManager _audioRawManager;

    private readonly Dictionary<uint, VoiceParticipant> _participants = new Dictionary<uint, VoiceParticipant>();
    private readonly HashSet<uint> _remoteUserConfigured = new HashSet<uint>();
    private readonly Stack<Action> _dispatchItems = new Stack<Action>();

    private int _ticks;
    private const int TicksThreeHold = 5;
    private static readonly object Locker = new object();

    private VoiceParticipant _player;

    public void Start(VoiceParticipant player, IRtcEngine engine)
    {
      _engine = engine;
      _player = player;
      
      
      _audioRawManager = _engine.GetAudioRawDataManager();
      
      _engine.MuteLocalAudioStream(true);
      _audioRawManager.RegisterAudioRawDataObserver();
      
      _engine.SetParameter("che.audio.external_render", true);

      _audioRawManager.SetOnPlaybackAudioFrameBeforeMixingCallback(RemoteUserAudioFrameReceived);
    }

    public void AddParticipant(VoiceParticipant participant) => _participants[participant.Id] = participant;

    public void UpdateSpatialAudio()
    {
      if (_dispatchItems.Count > 0)
        _dispatchItems.Pop().Invoke();
    }
    
    public void Shutdown()
    {
      _participants.Clear();
      _remoteUserConfigured.Clear();
      _dispatchItems.Clear();
    }

    // Gets an audio frame of a specified remote user.
    private void RemoteUserAudioFrameReceived(uint uid, AudioFrame audioFrame)
    {
      if (_ticks < TicksThreeHold)
        Debug.LogWarning($"count({_ticks}): OnPlaybackAudioFrameBeforeMixingHandler > {audioFrame}");

      _ticks++;

      // The audio stream info contains in this audioframe, we will use this construct the AudioClip
      lock (Locker)
      {
        if (!_remoteUserConfigured.Contains(uid) && _participants.ContainsKey(uid))
        {
          if (_ticks < TicksThreeHold)
            DispatchOnMainThread(() => { Debug.Log($"Uid:{uid} setting up audio frame handler...."); });

          var go = _participants[uid].WorldObject;
          if (go != null)
            DispatchOnMainThread(() =>
            {
              var userAudio = go.GetComponent<UserAudioFrameHandler>();
              if (userAudio != null) return;
              
              userAudio = go.AddComponent<UserAudioFrameHandler>();
              userAudio.Init(uid, this, audioFrame);
              _remoteUserConfigured.Add(uid);
            });
          else
            DispatchOnMainThread(() => { Debug.Log($"Uid: {uid} not found"); });
        }
      }

      HandleAudioFrameForUser?.Invoke(uid, audioFrame);
    }

    private void DispatchOnMainThread(Action action) => _dispatchItems.Push(action);
  }
}