using System.Collections.Generic;
using UnityEngine;

namespace IsntGwent.Scripts.Match.UI
{
    public class PlayerHpUI : MonoBehaviour
    {
        public GameObject heartPrefab;
        public float heartSpacing = 60f;
        
        private readonly List<GameObject> _hearts = new();
        
        public void SetHp(int hp)
        {
            while (_hearts.Count > hp)
            {
                Destroy(_hearts[^1]);
                _hearts.RemoveAt(_hearts.Count - 1);
            }
            
            while (_hearts.Count < hp)
            {
                var heart = Instantiate(heartPrefab, transform);
                _hearts.Add(heart);
            }

            RefreshLayout();
        }

        private void RefreshLayout()
        {
            int count = _hearts.Count;
            if (count == 0) return;

            float totalWidth = (count - 1) * heartSpacing;
            float startX = -totalWidth / 2f;

            for (int i = 0; i < count; i++)
            {
                _hearts[i].transform.localPosition = new Vector3(startX + i * heartSpacing, 0f, 0f);
            }
        }
    }
}