// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="Chest.cs" author="Lars" company="None">
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), 
// to deal in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software,
// and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// // </copyright>
// // <summary>
// //   TODO - Insert file description
// // </summary>
// // --------------------------------------------------------------------------------------------------------------------

using System;

using Controllers;

using DG.Tweening;

using ECS.Components.Type;
using ECS.Systems;

using UI;

using UnityEngine;

using Utilities.Game;
using Utilities.Game.ECSCache;

public class Chest : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer _renderer;

    [SerializeField]
    private GameObject _healthPickup;

    [SerializeField]
    private GameObject _powerPickup;

    [SerializeField]
    private GameObject _fovPickup;

    [SerializeField]
    private ParticleSystem _lootExplosion;

    private InventoryController _inventoryController;

    private Vector2Int _position;

    private Pickup _type;

    public enum Pickup
    {
        Power,
        Health,
        Fov
    }

    public Vector2Int Position
    {
        get
        {
            return _position;
        }
    }

    public void Create(Vector2Int position, Pickup type)
    {
        if (_inventoryController == null)
            _inventoryController = GameObject.Find("InventoryController").GetComponent<InventoryController>();

        _position = position;
        _type = type;
        transform.position = (Vector2)position;
    }

    public void Open()
    {
        var sequence = DOTween.Sequence();
        AudioPlayer.Instance.PlayAudio(AudioPlayer.Type.ChestOpen);
        sequence.Append(transform.DOShakePosition(1f, new Vector3(0.25f, 0.25f, 0))).OnComplete(
            () =>
                {
                    AudioPlayer.Instance.PlayAudio(AudioPlayer.Type.Powerup);

                    _lootExplosion.gameObject.SetActive(true);
                    _renderer.enabled = false;

                    AnimateItemPickup();

                    DestroyAfterSeconds();
                });
    }

    private void AnimateItemPickup()
    {
        GameObject item;
        switch (_type)
        {
            case Pickup.Health:
                item = _healthPickup;
                break;
            case Pickup.Power:
                item = _powerPickup;
                break;
            case Pickup.Fov:
                item = _fovPickup;
                break;
                default:
                throw new ArgumentOutOfRangeException("_type");
        }

        item.SetActive(true);
        item.transform.SetParent(null, true);
        item.transform.position = (Vector2)Position;

        var sequence = DOTween.Sequence();
        sequence.Append(item.transform.DOScale(1.0f, 0.5f).SetRelative().SetEase(Ease.OutElastic));
        sequence.AppendInterval(0.5f);
        sequence.OnComplete(
            () =>
                {
                    if (_type == Pickup.Power)
                    {
                        _inventoryController.AddPower();
                    }
                    else if (_type == Pickup.Health)
                    {
                        _inventoryController.AddHealth();
                    }
                    else if (_type == Pickup.Fov)
                    {
                        _inventoryController.AddFov();
                    }

                    Destroy(item);
                });
    }

    private void DestroyAfterSeconds()
    {
        Destroy(gameObject, 4.0f);
    }

    private void Update()
    {
        if (MapSystem.Instance.Map.IsInFov(_position.x, _position.y))
        {
            _renderer.color = Visibility.Visible;
        }
        else if (MapSystem.Instance.Map.IsExplored(_position.x, _position.y))
        {
            _renderer.color = Visibility.ExploredInvisible;
        }
        else
        {
            _renderer.color = Visibility.Invisible;
        }
    }
}