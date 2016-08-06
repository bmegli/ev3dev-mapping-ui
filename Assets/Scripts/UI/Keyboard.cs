﻿using UnityEngine;
using System.Collections;

public class Keyboard : MonoBehaviour
{
	void Update ()
	{
		if (Input.GetKeyDown (KeyCode.Escape))
			SceneManager.Instance.ToggleShowUI ();
		
	
		if (Input.GetButton("Fire1"))
		{
			print("fire");
			SceneManager.Instance.StartReplay();
		}
		
	}
}
