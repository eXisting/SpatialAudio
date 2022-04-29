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

    private void Awake()
    {
      join.onClick.AddListener(JoinRoom);
      leave.onClick.AddListener(LeaveRoom);

      room.text = Guid.NewGuid().ToString();
      
      _lobby.RemoteUserConnected += InstantiateParticipant;
      _lobby.SuccessfulyJoined += StartVoiceChat;
    }

    public void JoinRoom()
    {
      _rtcModule.LoadEngine();
      _rtcModule.StartObservation();
      
      _lobby.StartObservation(_rtcModule.AgoraEngine);
      _lobby.Enter(room.text);
    }

    public void LeaveRoom()
    {
      _rtcModule.AgoraEngine?.LeaveChannel();
      _audioChat.Shutdown();
    }
    
    private void StartVoiceChat(uint currentUserAgoraId)
    {
      player.name = $"ME_{currentUserAgoraId}";
      
      _audioChat.Start(new VoiceParticipant(currentUserAgoraId, player), _rtcModule.AgoraEngine);
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

    private void Update() => _audioChat.UpdateSpatialAudio();

    private void OnDestroy()
    {
      _rtcModule.Destroy();
      
      join.onClick.RemoveAllListeners();
      leave.onClick.RemoveAllListeners();
    }
  }
}