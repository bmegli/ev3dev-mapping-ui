using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

//fix for unity Extension methods requires `System.Runtime.CompilerServices.ExtensionAttribute'
//see http://schoening.it/blog/solution-cannot-define-a-new-extension-method-because-the-compiler-required-type-system-runtime-compilerservices-extensionattribute-cannot-be-found-are-you-missing-a-reference-to-system-core-dll/
namespace System.Runtime.CompilerServices
{
	public class ExtensionAttribute : Attribute { }
}

public static class UISetExtensions
{
	static MethodInfo toggleSetMethod;

	static UISetExtensions()
	{
		MethodInfo[] methods = typeof(Toggle).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
		for (var i = 0; i < methods.Length; i++)
		{
			if (methods[i].Name == "Set" && methods[i].GetParameters().Length == 2)
			{
				toggleSetMethod = methods[i];
				break;
			}
		}
	}
	public static void Set(this Toggle instance, bool value, bool sendCallback)
	{
		toggleSetMethod.Invoke(instance, new object[] {value, sendCallback});
	}
}
