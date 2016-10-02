using UnityEngine;
using System.Collections;

public class UserInput : MonoBehaviour
{
	public string horizontal="Horizontal";
	public string vertical="Vertical";
	public string acceleration="Acceleration";

	public UserInput DeepCopy()
	{
		UserInput other = (UserInput) this.MemberwiseClone();
		other.horizontal = string.Copy(horizontal);
		other.vertical = string.Copy(vertical);
		other.acceleration = string.Copy(acceleration);
		return other;
	}
}
