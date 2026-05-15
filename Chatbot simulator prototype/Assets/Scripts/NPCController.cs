using UnityEngine;
using uLipSync;
// Namespace and primary class share the name "uLipSync"; this alias lets us
// reference the MonoBehaviour without C# ambiguity errors.
using LipSyncAnalyzer = uLipSync.uLipSync;

public class NPCController : MonoBehaviour
{
    Animator anim;
    [SerializeField]
    SkinnedMeshRenderer face_blendShape;
    // Blendshape indices that uLipSync owns (e.g. MTH A/I/U/E/O). showAnimation()
    // and the per-frame Animator output are kept from clobbering these so the mouth
    // keeps animating with the voice while an emotion is playing.
    [SerializeField]
    int[] lipSyncBlendShapeIndices = new int[0];
    // Blendshapes whose names start with any of these prefixes are zeroed every
    // frame while TTS audio is playing, so emotion shapes like ALL Fun / vrc.v_aa
    // and the MTH family of mouth shapes don't drown out the lipsync motion.
    [SerializeField]
    string[] mouthBlendShapePrefixes = new string[] { "MTH ", "ALL ", "vrc.v_" };
    // The uLipSync analyzer for the voice. We poll its rawVolume to detect
    // active speech. This is more reliable than AudioSource.isPlaying when
    // audio is played via PlayOneShot (the path TtsServiceExtensions uses).
    [SerializeField]
    LipSyncAnalyzer lipSyncSource;
    [SerializeField, Tooltip("Raw audio amplitude above which mouth-conflict suppression activates.")]
    float speechRawVolumeThreshold = 0.0005f;
    [SerializeField, Tooltip("Hold suppression on for this many seconds after the last loud sample, so the smile doesn't flash back between syllables.")]
    float speechHoldSeconds = 0.4f;
    [SerializeField, Tooltip("DEBUG: when true, showAnimation only fires body animation triggers and skips every facial blendshape write. Use this to verify whether uLipSync alone can drive the mouth.")]
    bool disableFaceEmotions = false;
    [SerializeField, Tooltip("How wide the mouth opens during speech. 100 = normal blendshape range, 200 = double, 300 = triple. Most rigs accept values up to ~300 before looking distorted.")]
    float lipSyncMouthIntensity = 200f;
    int[] _conflictingMouthIndices;
    float _lastSpokenTime = -1f;
    int blinking=0;
    float blinkValue=0f;
    float blinkTimer=0f;
    float blinkTimerTotal=3.5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        anim = GetComponent<Animator>();
        BuildConflictingMouthIndices();
    }

    void BuildConflictingMouthIndices()
    {
        _conflictingMouthIndices = new int[0];
        if (face_blendShape == null || face_blendShape.sharedMesh == null) return;
        if (mouthBlendShapePrefixes == null || mouthBlendShapePrefixes.Length == 0) return;
        var mesh = face_blendShape.sharedMesh;
        var list = new System.Collections.Generic.List<int>();
        for (int i = 0; i < mesh.blendShapeCount; i++)
        {
            if (IsLipSyncIndex(i)) continue;
            string name = mesh.GetBlendShapeName(i);
            if (string.IsNullOrEmpty(name)) continue;
            for (int p = 0; p < mouthBlendShapePrefixes.Length; p++)
            {
                string prefix = mouthBlendShapePrefixes[p];
                if (!string.IsNullOrEmpty(prefix) && name.StartsWith(prefix))
                {
                    list.Add(i);
                    break;
                }
            }
        }
        _conflictingMouthIndices = list.ToArray();
    }

    void LateUpdate()
    {
        // Suppress mouth-conflicting shapes while uLipSync is detecting voice,
        // with a small hold so the smile doesn't flash back during brief pauses
        // between syllables. When fully silent, emotions can drive the face again.
        if (face_blendShape == null || _conflictingMouthIndices == null) return;
        if (lipSyncSource == null) return;

        if (lipSyncSource.result.rawVolume > speechRawVolumeThreshold)
        {
            _lastSpokenTime = Time.time;
        }
        bool isSpeaking = _lastSpokenTime >= 0f && (Time.time - _lastSpokenTime) < speechHoldSeconds;
        if (!isSpeaking) return;

        for (int i = 0; i < _conflictingMouthIndices.Length; i++)
        {
            face_blendShape.SetBlendShapeWeight(_conflictingMouthIndices[i], 0f);
        }
    }

    // Right-click NPCController in the Inspector and choose this to dump every
    // blendshape name + index to the Console. Use it to find the actual indices
    // of MTH A/I/U/E/O (they almost certainly aren't 1..5).
    [ContextMenu("Log Blend Shapes")]
    void LogBlendShapes()
    {
        if (face_blendShape == null || face_blendShape.sharedMesh == null)
        {
            Debug.LogWarning("NPCController: face_blendShape is not assigned.");
            return;
        }
        var mesh = face_blendShape.sharedMesh;
        for (int i = 0; i < mesh.blendShapeCount; i++)
        {
            Debug.Log("[BlendShape " + i + "] " + mesh.GetBlendShapeName(i));
        }
    }

    // Update is called once per frame
    void Update()
    {
        blinkTimer+=Time.deltaTime;
        if(blinking==0&&(Random.value<0.01f||blinkTimer>blinkTimerTotal))
        {
            blinking=1;
            blinkValue=0;
            blinkTimer=0f;
            blinkTimerTotal=Random.Range(1.1f, 5.01f);
        }
        else if(blinking==1)
        {
            blinkValue+=Time.deltaTime*1000f;
            if(blinkValue>100f)
            {
                blinking=2;
                face_blendShape.SetBlendShapeWeight(35, 100f);
            }
            else{
                face_blendShape.SetBlendShapeWeight(35, blinkValue);
            }
        }
        else if(blinking==2)
        {
            blinkValue-=Time.deltaTime*600f;
            if(blinkValue<0f)
            {
                blinking=0;
                face_blendShape.SetBlendShapeWeight(35, 0f);
            }
            else{
                face_blendShape.SetBlendShapeWeight(35, blinkValue);
            }
        }

    }
    bool IsLipSyncIndex(int index)
    {
        if (lipSyncBlendShapeIndices == null) return false;
        for (int i = 0; i < lipSyncBlendShapeIndices.Length; i++)
        {
            if (lipSyncBlendShapeIndices[i] == index) return true;
        }
        return false;
    }

    // Wire this as a listener on the uLipSync component's On Lip Sync Update (LipSyncInfo)
    // event when debugging. Logs to the Console so you can confirm uLipSync is actually
    // receiving audio and emitting phoneme/volume values.
    public void DebugLogLipSync(LipSyncInfo info)
    {
        Debug.Log("[uLipSync] phoneme=" + info.phoneme + " volume=" + info.volume.ToString("F2") + " raw=" + info.rawVolume.ToString("F4"));
    }

    float[] _directLipSyncWeights;
    // Bypass uLipSyncBlendShape entirely and drive the 5 mouth blendshapes directly
    // from the LipSyncInfo events. Use this to verify the pipeline works without
    // depending on uLipSyncBlendShape's configuration.
    // Expects lipSyncBlendShapeIndices in the order: [A, I, U, E, O] (5 entries).
    public void ApplyLipSyncDirect(LipSyncInfo info)
    {
        if (face_blendShape == null) return;
        if (lipSyncBlendShapeIndices == null || lipSyncBlendShapeIndices.Length == 0) return;
        if (_directLipSyncWeights == null || _directLipSyncWeights.Length != lipSyncBlendShapeIndices.Length)
        {
            _directLipSyncWeights = new float[lipSyncBlendShapeIndices.Length];
        }

        int activeSlot = -1;
        if (info.phoneme == "A") activeSlot = 0;
        else if (info.phoneme == "I") activeSlot = 1;
        else if (info.phoneme == "U") activeSlot = 2;
        else if (info.phoneme == "E") activeSlot = 3;
        else if (info.phoneme == "O") activeSlot = 4;

        float targetIfActive = info.volume * lipSyncMouthIntensity;
        for (int i = 0; i < lipSyncBlendShapeIndices.Length; i++)
        {
            float target = (i == activeSlot) ? targetIfActive : 0f;
            _directLipSyncWeights[i] = Mathf.Lerp(_directLipSyncWeights[i], target, 0.4f);
            face_blendShape.SetBlendShapeWeight(lipSyncBlendShapeIndices[i], _directLipSyncWeights[i]);
        }
    }

    void SetEmotionShape(int index, float weight)
    {
        if (disableFaceEmotions) return;
        face_blendShape.SetBlendShapeWeight(index, weight);
    }

    public void showAnimation(string animID)
    {
        if (!disableFaceEmotions)
        {
            for(int i=0; i<face_blendShape.sharedMesh.blendShapeCount; i++)
            {
                if (i == 35) continue;            // blink (driven by per-frame logic in Update)
                if (IsLipSyncIndex(i)) continue;  // mouth (driven by uLipSync)
                face_blendShape.SetBlendShapeWeight(i, 0f);
            }
        }
        if(animID=="idle")
        {
            if(Random.value<0.3f)
            {
                anim.SetTrigger("idle1");
            }
            else if(Random.value<0.6f)
            {
                anim.SetTrigger("idle2");
            }
            else
            {
                anim.SetTrigger("idle3");
            }
        }
        else if(animID=="shy")
        {
            anim.SetTrigger("shy");
        }
        else if(animID=="confused")
        {
            anim.SetTrigger("confused");
            SetEmotionShape(32, 100f);
        }
        else if(animID=="joking")
        {
            anim.SetTrigger("joking");
            SetEmotionShape(33, 190f);
        }
        else if(animID=="surprise")
        {
            anim.SetTrigger("surprise");
            SetEmotionShape(53, 100f);
        }
        else if(animID=="focus")
        {
            anim.SetTrigger("focus");
            SetEmotionShape(50, 100f);
        }
        else if(animID=="angry")
        {
            anim.SetTrigger("angry");
            SetEmotionShape(49, 100f);
        }
        else if(animID=="cheers")
        {
            anim.SetTrigger("cheers");
            SetEmotionShape(24, 100f);
        }
        else if(animID=="nod")
        {
            anim.SetTrigger("nod");
            SetEmotionShape(9, 100f);
        }
        else if(animID=="waving_arm")
        {
            anim.SetTrigger("waving_arm");
            SetEmotionShape(24, 100f);
        }
        else if(animID=="proud")
        {
            anim.SetTrigger("proud");
            SetEmotionShape(24, 100f);
        }
    }
}
