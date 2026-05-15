using UnityEngine;

[CreateAssetMenu(fileName = "PersonalityDB", menuName = "Scriptable Objects/PersonalityDB", order=2)]
public class PersonalityDB : ScriptableObject
{
    public Personality[] personalities;
    [System.Serializable]
    public struct Personality
    {

        public string name;
        [TextArea(4,100)]
        public string initialPrompt;
        // Name of the Sherpa-ONNX TTS profile to use for this NPC.
        // Must match a profile created in Project Settings > Sherpa-ONNX > TTS.
        public string ttsProfileName;

    }
}
