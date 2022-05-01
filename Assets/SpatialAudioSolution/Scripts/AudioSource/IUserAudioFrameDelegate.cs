using System;
using agora_gaming_rtc;

namespace Agora.AudioSource
{
  public interface IUserAudioFrameDelegate
  {
    AudioRawDataManager.OnPlaybackAudioFrameBeforeMixingHandler HandleAudioFrameForUser { get; set; }
  }
}