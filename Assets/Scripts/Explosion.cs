using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Utilities.Game.ObjectPool;

public class Explosion : MonoBehaviour, IPoolable
{
    [SerializeField]
    private ParticleSystem _particle;

    public bool IsEnabled
    {
        get
        {
            return gameObject.activeInHierarchy;
        }
    }

    public void Enable()
    {
        gameObject.SetActive(true);
    }

    public void Disable()
    {
        gameObject.SetActive(false);
    }

    public void Explode(Vector2 position)
    {
        transform.position = position;
        _particle.Play(true);

        Invoke("Disable", 3.0f);
    }
}
