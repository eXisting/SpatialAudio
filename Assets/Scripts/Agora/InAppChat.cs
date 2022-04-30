using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Agora
{
  public class InAppChat : MonoBehaviour
  {
    [SerializeField] private Button join;
    [SerializeField] private Button leave;
    [SerializeField] private TMP_InputField room;
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject participantPrefab;

    private readonly RtcModule _rtcModule = new RtcModule();
    private readonly AudioChat _audioChat = new AudioChat();
    private readonly Lobby _lobby = new Lobby();

    private bool _inCall;

    private void Awake()
    {
      join.onClick.AddListener(JoinRoom);
      leave.onClick.AddListener(LeaveRoom);

      room.text = Guid.NewGuid().ToString();
      
      _lobby.RemoteUserConnected += InstantiateParticipant;
      _lobby.SuccessfulyJoined += CaptureAgoraPlayer;
    }

    private void JoinRoom()
    {
      if (_inCall)
        return;
      
      _rtcModule.LoadEngine();
      
      _rtcModule.StartObservation();
      _audioChat.SurroundSoundObservation(_rtcModule.AgoraEngine);
      _lobby.StartObservation(_rtcModule.AgoraEngine);
      
      _lobby.Enter(room.text);
    }

    private void LeaveRoom()
    {
      _rtcModule.AgoraEngine?.LeaveChannel();
      _audioChat.Shutdown();

      _inCall = false;
    }
    
    private void CaptureAgoraPlayer(uint currentUserAgoraId)
    {
      player.name = $"ME_{currentUserAgoraId}";

      _inCall = true;
    }

    private GameObject InstantiateParticipant(uint id)
    {
      var randomPoint = Random.insideUnitCircle * 2;
      var position = player.transform.position;
      
      var randomX = position.x + randomPoint.x;
      var randomZ = position.z + randomPoint.y;
      var randomPosition = new Vector3(randomX, position.y, randomZ);
      
      var worldObject = Instantiate(participantPrefab, randomPosition, Quaternion.identity);
      worldObject.name = id.ToString();

      var voiceParticipant = new VoiceParticipant(id, worldObject);
      _audioChat.AddParticipant(voiceParticipant);

      return worldObject;
    }

    private void Update()
    {
      // Agora's built-in solution
      _audioChat.UpdateSurroundAudio(player);
     
      // AudioSource solution
      //_audioChat.UpdateAudioClips();
    }

    private void OnDestroy()
    {
      _rtcModule.Destroy();
      
      join.onClick.RemoveAllListeners();
      leave.onClick.RemoveAllListeners();
      
      _lobby.RemoteUserConnected -= InstantiateParticipant;
      _lobby.SuccessfulyJoined -= CaptureAgoraPlayer;
    }
  }
}