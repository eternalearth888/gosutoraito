﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrismPickup : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnMouseDown()
    {
        PlayerBehavior.S.prismCount++;
        InventoryUI.S.UpdateInterfaceText();
        Destroy(gameObject);
    }
}
