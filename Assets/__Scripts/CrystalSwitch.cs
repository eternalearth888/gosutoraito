﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrystalSwitch : MonoBehaviour
{
    public bool _active = false;
    public bool _startSwitch = false;
    public GameObject _lightEmitter;
    public GameObject _startLight;
    public Color inactiveColor;
    public Color activeColor;

    private Renderer _renderer;

    void Start()
    {
        if (_startSwitch)
        {
            _startLight = GameObject.Find("StartLight");
        }

        _renderer = GetComponent<Renderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (_lightEmitter != null && (!_lightEmitter.GetComponent<LightEmitter>()._activeCrystals.Contains(this.gameObject) || 
            !_lightEmitter.activeInHierarchy))
        {
            Deactivate();
        }

        if (_active != true && _renderer.material.color == activeColor)
        {
            _renderer.sharedMaterial.color = inactiveColor;
        }
        if (_active && _renderer.material.color == inactiveColor)
        {
            _renderer.sharedMaterial.color = activeColor;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        GameObject go = collision.gameObject;
    }

    public void Activate()
    {
        _active = true;
        if (_startSwitch && _startLight) _startLight.GetComponent<LightEmitter>()._startSwitchOn = true;
        _renderer.material.color = activeColor;
    }

    public void Deactivate()
    {
        _active = false;
        _lightEmitter = null;
        _renderer.material.color = inactiveColor;
    }
}
