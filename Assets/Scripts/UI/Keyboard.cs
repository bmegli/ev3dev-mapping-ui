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
	}
}

} //namespace