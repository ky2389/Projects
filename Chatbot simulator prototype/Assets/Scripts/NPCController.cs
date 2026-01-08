using UnityEngine;

public class NPCController : MonoBehaviour
{
    Animator anim;
    [SerializeField]
    SkinnedMeshRenderer face_blendShape;
    int blinking=0;
    float blinkValue=0f;
    float blinkTimer=0f;
    float blinkTimerTotal=3.5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        anim = GetComponent<Animator>();
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
    public void showAnimation(string animID)
    {   
        for(int i=0; i<face_blendShape.sharedMesh.blendShapeCount; i++)
        {
            if (i!=35){
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
            face_blendShape.SetBlendShapeWeight(32, 100f);
        }
        else if(animID=="joking")
        {
            anim.SetTrigger("joking");
            face_blendShape.SetBlendShapeWeight(33, 190f);
        }
        else if(animID=="surprise")
        {
            anim.SetTrigger("surprise");
            face_blendShape.SetBlendShapeWeight(53, 100f);
        }
        else if(animID=="focus")
        {
            anim.SetTrigger("focus");
            face_blendShape.SetBlendShapeWeight(50, 100f);
        }
        else if(animID=="angry")
        {
            anim.SetTrigger("angry");
            face_blendShape.SetBlendShapeWeight(49, 100f);
        }
        else if(animID=="cheers")
        {
            anim.SetTrigger("cheers");
            face_blendShape.SetBlendShapeWeight(24, 100f);
        }
        else if(animID=="nod")
        {
            anim.SetTrigger("nod");
            face_blendShape.SetBlendShapeWeight(9, 100f);
        }
        else if(animID=="waving_arm")
        {
            anim.SetTrigger("waving_arm");
            face_blendShape.SetBlendShapeWeight(24, 100f);
        }
        else if(animID=="proud")
        {
            anim.SetTrigger("proud");
            face_blendShape.SetBlendShapeWeight(24, 100f);
        }
    }
}
