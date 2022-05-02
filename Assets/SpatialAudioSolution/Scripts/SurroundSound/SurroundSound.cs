// Copyright - SpatialSystems 2022

using agora_gaming_rtc;
using UnityEngine;

namespace Agora.SurroundSound
{
  /// <summary>
  /// Works only for surround sound devices
  /// </summary>
  public class SurroundSound
  {
    private const float MaxChatProximity = 1.5f;
    private const float ChatRadius = 15;
    
    public Vector3 PlayerPosition { get; set; }

    private readonly IAudioEffectManager _audioEffectManager;
    
    public SurroundSound(IRtcEngine engine) => _audioEffectManager = engine.GetAudioEffectManager();

    public void UpdateSpatialAudio(uint id, VoiceParticipant voiceParticipant)
    {
      var distance = DistanceToPlayer(voiceParticipant.WorldObject);
      var pan = CalculatePan(voiceParticipant.WorldObject.transform);
      var gain = CalculateGain(distance);
      
      Debug.Log($"Pan: {pan}, gain: {gain}");
      
      _audioEffectManager.SetRemoteVoicePosition(id, pan, gain);
    }

    private double CalculateGain(float distance)
    {
      distance = Mathf.Clamp(distance, MaxChatProximity, ChatRadius);
      
      // Normalize the result between a value of - 100f:
      var gain = (distance - ChatRadius) / (MaxChatProximity - ChatRadius);
      gain *= 100;
      
      return gain;
    }

    private double CalculatePan(Transform participant)
    {
      // Get the dot product between the vector pointing from local towards the remote player,
      // and right-facing vector of local player
      var directionToRemotePlayer = participant.position - PlayerPosition;
      directionToRemotePlayer.Normalize();
      
      // When normalized, a value between -1 and 1 is produced, indicating the orientation of local player to the remote player
      var pan = Vector3.Dot(participant.right, directionToRemotePlayer);
      
      return pan;
    }

    private float DistanceToPlayer(GameObject participant) => Vector3.Distance(PlayerPosition, participant.transform.position);
  }
}