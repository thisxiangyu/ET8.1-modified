using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ET
{
    public class LocationNode : MonoBehaviour
    {
        public string Name;

        public void Awake()
        {
            if (string.IsNullOrEmpty(Name))
                Name = this.gameObject.name;
        }
    }
}
