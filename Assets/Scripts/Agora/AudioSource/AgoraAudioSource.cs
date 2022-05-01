using System;
using agora_gaming_rtc;
using UnityEngine;

namespace Agora.AudioSource
{
  public class AgoraAudioSource : MonoBehaviour
  {
    private const int PullFrequencyPerSecond = 100;

    [SerializeField] private UnityEngine.AudioSource audioSource;

    IUserAudioFrameDelegate _userAudioFrameDelegate;

    private uint ParticipantId { get; set; }

    private int _channels = 2;
    private int _frequency = 48000; // this should = _clipSamples x PullFrequencyPerSecond
    private int _clipSamples = 480;
    
    private RingBuffer<float> _audioBuffer;
    private AudioClip _audioClip = null;
    private int writeCount;
    private int readCount;
    
    private void Start()
    {
      audioSource = GetComponent<UnityEngine.AudioSource>();	
      if (audioSource == null)
      {
        audioSource = gameObject.AddComponent<UnityEngine.AudioSource>();
      }
      _userAudioFrameDelegate.HandleAudioFrameForUser += HandleAudioFrame;
      MakeClip($"AudioClip_{ParticipantId}");
    }

    public void Init(uint uid, IUserAudioFrameDelegate userAudioFrameDelegate, AudioFrame audioFrame)
    {
      Debug.Log($"INIT:{uid} audioFrame: {audioFrame}");
      
      ParticipantId = uid;
      
      _userAudioFrameDelegate = userAudioFrameDelegate;
      _clipSamples = audioFrame.samples;
      _frequency = audioFrame.samplesPerSec;
      _channels = audioFrame.channels;
    }

    private void MakeClip(string clipName)
    {
      if (_audioClip != null) return;

      // 10-sec-length buffer
      var bufferLength = _frequency / PullFrequencyPerSecond * _channels * 1000; 
      _audioBuffer = new RingBuffer<float>(bufferLength);
      
      _audioClip = AudioClip.Create(clipName, _clipSamples, _channels, 
        _frequency, true, OnAudioRead);
      
      audioSource.clip = _audioClip;
      audioSource.loop = true;
      audioSource.spatialBlend = 1;
      audioSource.Play();
    }

    private void OnDisable()
    {
      _userAudioFrameDelegate.HandleAudioFrameForUser -= HandleAudioFrame;
    }

    void HandleAudioFrame(uint uid, AudioFrame audioFrame)
    {
      if (ParticipantId != uid || _audioBuffer == null) return;

      var floatArray = ConvertByteToFloat16(audioFrame.buffer);
      for (var i = 0; i < floatArray.Length; i++)
      {
        _audioBuffer.Enqueue(floatArray[i]);
      }
        
      writeCount += floatArray.Length;
    }
    
    private void OnAudioRead(float[] data)
    {
      if (_audioBuffer.Count == 0) return;
            
      for (var i = 0; i < data.Length; i++)
      {
        data[i] = _audioBuffer.Dequeue();
        readCount += 1;
      }
      // Debug.Log("buffer length remains: {0}", writeCount - readCount);
    }
    
    private static float[] ConvertByteToFloat16(byte[] byteArray)
    {
      var floatArray = new float[byteArray.Length / 2];
      for (var i = 0; i < floatArray.Length; i++)
      {
        floatArray[i] = BitConverter.ToInt16(byteArray, i * 2) / 32768f; // -Int16.MinValue
      }

      return floatArray;
    }
  }
}