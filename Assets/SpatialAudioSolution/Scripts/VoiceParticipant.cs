using UnityEngine;

namespace Agora
{
  public readonly struct VoiceParticipant
  {
    public readonly uint Id;
    public readonly GameObject WorldObject;
    
    public VoiceParticipant(uint id, GameObject worldObject)
    {
      Id = id;
      WorldObject = worldObject;
    }
  }
}