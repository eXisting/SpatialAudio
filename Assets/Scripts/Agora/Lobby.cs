using System;
using System.Collections.Generic;
using agora_gaming_rtc;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Agora
{
  public class Lobby
  {
    public event Action<uint> RemoteUserDropped;
    public event Func<uint, GameObject> RemoteUserConnected;
    public event Action<uint> SuccessfulyJoined;
    
    private IRtcEngine _engine;

    private Dictionary<uint, GameObject> _participants = new Dictionary<uint, GameObject>();

    // Note, it expires in 24 hours
    private string _token =
      "006516e711a0d394e938025a741fffe2301IADn7if6HOjxJ3bm+eN0ZHPrlyMpMfTpzPgVxlBkfy/P1N7YvmgAAAAAEABD/MfDH3RuYgEAAQAfdG5i";
    private string _channel = "30april";
    
    public void StartObservation(IRtcEngine engine)
    {
      _engine = engine;
      
      _engine.OnJoinChannelSuccess += JoinedChannelSuccessfully;
      _engine.OnUserOffline += RemoteUserLeft;
      _engine.OnUserJoined += RemoteUserJoined;
      _engine.OnConnectionStateChanged += CaptureConnectionStateChange;
    }

    public void Enter(string roomId)
    {
      _engine.JoinChannelByKey(_token, _channel, null);
    }

    private void RemoteUserLeft(uint uid, USER_OFFLINE_REASON reason)
    {
      Debug.Log($"User is offline: {uid} {reason}");

      var participant = _participants[uid];
      Object.Destroy(participant);
      
      RemoteUserDropped?.Invoke(uid);
    }
    
    private void RemoteUserJoined(uint uid, int elapsed)
    {
      Debug.Log($"User joined: {uid} {elapsed}");

      var prefab = RemoteUserConnected?.Invoke(uid);
      _participants[uid] = prefab;
    }

    private void JoinedChannelSuccessfully(string channelName, uint uid, int elapsed)
    {
      Debug.Log($"Agora: client joined to channel, channel:{channelName} userId:{uid}");
      
      SuccessfulyJoined?.Invoke(uid);
    }

    private void CaptureConnectionStateChange(CONNECTION_STATE_TYPE state, CONNECTION_CHANGED_REASON_TYPE reason)
      => Debug.Log($"Agora: connection with server changed, state:{state} reason:{reason}");

  }
}