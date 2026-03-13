using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ParallaxWinterLandscape 
{

public class BGScroller : MonoBehaviour {

    public float scrollSpeed;

    private float tileSizeX;
    private Vector2 startPosition;

	void Start () {
        //get the width of the scrollable object
        SpriteRenderer sr = this.gameObject.GetComponentInChildren<SpriteRenderer>();
        tileSizeX = sr.bounds.size.x;
        //get the startposition
        Transform startTransform = GetComponent<Transform>();
        startPosition.x = startTransform.position.x;
        startPosition.y = startTransform.position.y;

    }
	
	void Update () {
        //scroll the object according to the defined speed, loop when running out of width
        float newPosition = Mathf.Repeat(Time.time * scrollSpeed, tileSizeX);
        transform.position = startPosition + Vector2.left * newPosition;
	}
}

}