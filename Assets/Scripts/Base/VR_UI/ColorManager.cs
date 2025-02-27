using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorManager : MonoBehaviour
{
    private Material _mat;
    // Start is called before the first frame update
    void Start()
    {
        _mat = GetComponent<MeshRenderer>().material;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ChangeColor()
    {
        _mat.color = _mat.color == Color.white ? Color.cyan : Color.white;
    }
}
