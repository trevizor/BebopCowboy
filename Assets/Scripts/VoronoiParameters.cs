using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoronoiParameters
{
    public string name = "coronoi";
    public float minHeight = 0f;
    public float maxHeight = 1f;
    public float falloff = 1f; //smaller numbers make the slope gentler, higher make it steeper
    public float maxDistanceMultiplier = 1f; //needs more testing to check what exactly changes
    public int minInstances = 0;
    public int maxInstances = 3;
    public bool powerFallout = false;
    public float powerValue = 1f;


    public VoronoiParameters (
        string _name = "cornoi",
        float _minHeight = 0f,
        float _maxHeight = 1f,
        float _falloff = 1f,
        float _maxDistanceMultiplier = 1f,
        int _minInst = 0,
        int _maxInst = 1,
        bool _powerFallout = false,
        float _powerValue = 1f
        )
    {
        name = _name;
        minHeight = _minHeight;
        maxHeight = _maxHeight;
        falloff = _falloff;
        maxDistanceMultiplier = _maxDistanceMultiplier;
        minInstances = _minInst;
        maxInstances = _maxInst;
        powerFallout = _powerFallout;
        powerValue = _powerValue;
    }


}
