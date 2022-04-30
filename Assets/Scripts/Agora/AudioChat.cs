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

    private SurroundSound.SurroundSound _surroundSound;

    public void SurroundSoundObservation(IRtcEngine engine)
    {
      _engine = engine;

      _surroundSound = new SurroundSound.SurroundSound(_engine);
      _audioRawManager = _engine.GetAudioRawDataManager();
      
      // Just to not hear myself twice during testing
      _engine.MuteLocalAudioStream(true);

      // Enable spatial audio
      _engine.EnableSoundPositionIndication(true);
    }
    
    public void AudioSourceObservation(IRtcEngine engine)
    {
      _engine = engine;

      _audioRawManager = _engine.GetAudioRawDataManager();
      
      // Just to not hear myself twice during testing
      _engine.MuteLocalAudioStream(true);
      
      // Required both in order to have spatial effect control
      _audioRawManager.RegisterAudioRawDataObserver();
      _engine.SetParameter("che.audio.external_render", true);
      
      _audioRawManager.SetOnPlaybackAudioFrameBeforeMixingCallback(RemoteUserAudioFrameReceived);
    }

    public void AddParticipant(VoiceParticipant participant) => _participants[participant.Id] = participant;

    public void UpdateAudioClips()
    {
      if (_dispatchItems.Count > 0)
        _dispatchItems.Pop().Invoke();
    }
    
    public void UpdateSurroundAudio(GameObject player)
    {
      if (_surroundSound == null)
        return;
      
      _surroundSound.PlayerPosition = player.transform.position;

      foreach (var participant in _participants) 
        _surroundSound.UpdateSpatialAudio(participant.Key, participant.Value);
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