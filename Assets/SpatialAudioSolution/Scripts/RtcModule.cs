// Copyright - SpatialSystems 2022

using System;
using agora_gaming_rtc;
using UnityEngine;

namespace Agora
{
  internal sealed class RtcModule
  {
    // Warning: this is test app ID. Replace it with your ID
    private const string ID = "516e711a0d394e938025a741fffe2301";

    public IRtcEngine AgoraEngine { get; private set; }
    
    public void LoadEngine()
    {
      Debug.Log("Loading engine...");
      try
      {
        AgoraEngine = IRtcEngine.GetEngine(ID) ?? throw new Exception("Can't load Rtc engine. Something went wrong");
        AgoraEngine.SetChannelProfile(CHANNEL_PROFILE.CHANNEL_PROFILE_COMMUNICATION);
      }
      catch (Exception e)
      {
        Debug.Log("Wasn't able to create engine using config." + e.Message);
        throw;
      }
    }

    public void StartObservation()
    {
      if (AgoraEngine == null)
        return;
      
      AgoraEngine.OnWarning += CaptureWarning;
      AgoraEngine.OnError += CaptureError;
      AgoraEngine.OnLeaveChannel += DestroyEngine;
    }

    public void Destroy()
    {
      StopObservation();

      if (AgoraEngine != null)
      {
        Debug.Log("Destroy rtc");
        IRtcEngine.Destroy();
      }
      
      AgoraEngine = null;
    }
    
    private void CaptureError(int error, string msg)
      => Debug.LogError($"Agora error, code:{error} {IRtcEngine.GetErrorDescription(error)} msg:{msg}");

    private void CaptureWarning(int warn, string msg)
      => Debug.LogWarning($"Agora warning, code:{warn} {IRtcEngine.GetErrorDescription(warn)} msg:{msg}");

    private void DestroyEngine(RtcStats stats)
    {
      Debug.Log($"Agora: client leave from channel");
      
      Destroy();
    }

    private void StopObservation()
    {
      if (AgoraEngine == null) 
        return;
      
      Debug.Log("Stop listening to events...");
      
      AgoraEngine.OnWarning -= CaptureWarning;
      AgoraEngine.OnError -= CaptureError;
      AgoraEngine.OnLeaveChannel -= DestroyEngine;
    }
  }
}
