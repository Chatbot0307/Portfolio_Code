using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleAutoDestroyer : MonoBehaviour
{
    private ParticleSystem particle;
    [SerializeField] private AudioSource sound;

    private void Awake()
    {
        particle = GetComponent<ParticleSystem>();
        sound.Play();
            
    }

    // Update is called once per frame
    void Update()
    {
        if (particle.isPlaying == false)
        {
            Destroy(gameObject);
        }
    }
}