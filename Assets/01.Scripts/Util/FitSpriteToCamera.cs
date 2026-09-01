using _01.Scripts.Util.Logging;
using UnityEngine;

namespace _01.Scripts.Util
{
    public class FitSpriteToCamera : MonoBehaviour
    {
        private void Start()
        {
            var spriteRenderer = GetComponent<SpriteRenderer>();
            var sprite = spriteRenderer.sprite;
            var cam = Camera.main;

            if (cam == null)
            {
                GameLog.Error($"{GetType().Name} fail to find camera", "system", this);
                return;
            } 
                
            
            var worldHeight = cam.orthographicSize * 2f;
            var worldWidth = worldHeight * cam.aspect;

            var spriteWidth = sprite.bounds.size.x;
            var spriteHeight = sprite.bounds.size.y;

            var scale = Mathf.Max(
                worldWidth / spriteWidth,
                worldHeight / spriteHeight
            );

            transform.localScale = Vector3.one * scale;
        }
    }
}