﻿using UnityEngine;
using System.Collections;

public class CubeShoot : MonoBehaviour {

    public void Start()
    {
        GetComponent<Rigidbody>().isKinematic = true;
    }

	// Update is called once per frame
	public void ShootCube (float velocity) {
        GetComponent<Rigidbody>().isKinematic = false;
        var playerpos = Camera.main.transform.position;
        var direction = (playerpos - transform.position) * velocity;
        GetComponent<Rigidbody>().AddForce(direction, ForceMode.Impulse);
	}
}