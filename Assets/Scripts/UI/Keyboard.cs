using UnityEngine;
using System.Collections;

namespace Ev3devMapping
{

public class Keyboard : MonoBehaviour
{
	void Update ()
	{
		if (Input.GetKeyDown (KeyCode.Escape))
			SceneManager.Instance.ToggleShowUI ();

		if (Input.GetButtonDown("Fire1"))
			SceneManager.Instance.ToggleShowUI ();
	}
}

} //namespace