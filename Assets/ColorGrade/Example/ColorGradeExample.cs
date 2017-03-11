using UnityEngine;
using System.Collections;

public class ColorGradeExample : MonoBehaviour 
{

	public UnityStandardAssets.ImageEffects.ColorGradingEffect m_CameraEffect;
	public ColorGradeConfig[] m_Configs;

	private int m_CurrentConfig = 0;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () 
	{
		if (Input.GetKeyDown(KeyCode.Space))
		{
			this.m_CurrentConfig = (this.m_CurrentConfig + 1) % this.m_Configs.Length;
			this.m_CameraEffect.applyColorGrading(this.m_Configs[this.m_CurrentConfig]);
		}
	}
}
