using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spray : MonoBehaviour
{
    ParticleSystem particle;
    ParticleSystem.MainModule main; // updated usage
    bool isSpraying = false;

    void Awake()
    {
        particle = transform.GetChild(0).GetComponent<ParticleSystem>();
        if(particle == null)
        {
            Debug.LogError("ParticleSystem is null!");
            Debug.Break();
        }

        main = particle.main;
        main.playOnAwake = false;
        particle.Stop();
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        StopSpray();
    }

    public void StartSpray()
    {
        particle.Play();
        isSpraying = true;
    }

    public void StopSpray()
    {
        particle.Stop();
        isSpraying = false;
    }

    public void UpdateParticleColor(Color color)
    {
        main.startColor = color;
    }

    public bool IsSpraying()
    {
        return isSpraying;
    }
}