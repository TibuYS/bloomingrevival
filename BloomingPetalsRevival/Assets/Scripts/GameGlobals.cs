using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GameGlobals : MonoBehaviour
{
    [Header("Student Vision Global Settings")]
    public LayerMask StudentIgnore; //will be ignored by students
    public LayerMask StudentTarget; //will be seen by students
    [Space]
    [Header("Item Global Settings")]
    public LayerMask GroundLayer;
    [Header("Sounds")]
    public List<AudioClip> FemaleScreams = new List<AudioClip>();
    public List<AudioClip> MaleScreams = new List<AudioClip>();
    public AudioClip KnifeStab;

    //instance so we can access from anywhere in the project
    public static GameGlobals instance;

    private void Start()
    {
        instance = this;
    }
}
