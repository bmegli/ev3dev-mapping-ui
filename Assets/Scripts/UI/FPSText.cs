// Modified source from (converted to new UI):
// http://wiki.unity3d.com/index.php?title=FramesPerSecond

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FPSText : MonoBehaviour
{
	private float deltaTime = 0.0f;
	private Text text;

	void Start()
	{
		if( !GetComponent<Text>() )
		{
			Debug.Log("FPSText needs a Text component!");
			enabled = false;
			return;
		}
		text = GetComponent<Text>();
	}

	void Update()
	{
		//deltaTime = (1.0f - alpha) * deltaTime + alpha * Time.deltaTime;
		// exponential smoothing, equvalent above and below
		deltaTime += (Time.deltaTime - deltaTime) * Constants.ExponentialSmoothingAlpha;
		float msec = deltaTime * 1000.0f;
		float fps = 1.0f / deltaTime;
		text.text = string.Format("{1:0.} fps ({0:0.0} ms)", msec, fps);
	}
}

