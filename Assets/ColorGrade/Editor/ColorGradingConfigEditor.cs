using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(ColorGradeConfig))]
public class ColorGradingConfigEditor : Editor 
{

	public static ColorGradeConfig SYNCED_CONFIG = null;

	private Texture m_ColorWheel;
	private Texture m_ColorWheelMarker;
	private bool m_PendingClick = false;
	private Vector2 m_ClickPosition = Vector2.zero;
	private WheelConfig m_Shadow;
	private WheelConfig m_Midtone;
	private WheelConfig m_Highlight;

	private class WheelConfig
	{

		public static float COLOR_RADIUS = 38f;
		public static float IMAGE_RADIUS = 41f;

		public Vector2 wheelTopLeft = new Vector2(0f,0f);
		public Color color = Color.white;

		public void setColor(Color c)
		{
			color = c;
		}

		public void setHueSat(float hue, float sat)
		{
			Color c = Color.HSVToRGB( hue, sat, 1.0f);
			this.color.r = c.r;
			this.color.g = c.g;
			this.color.b = c.b;
		}

		// -100,100
		public void setLevel(float level)
		{
			this.color.a = (level + 100) / 200f;
		}

		public float getLevel()
		{
			return (color.a - 0.5f)*200f;
		}

		public Vector2 getMarkerPosition()
		{
			float h,s,v;
			Color.RGBToHSV(color, out h, out s, out v);
			float angle = h * Mathf.PI * 2f;
			Vector2 markerPosition = this.wheelTopLeft + new Vector2(IMAGE_RADIUS,IMAGE_RADIUS) + 
			new Vector2(
				Mathf.Cos(angle),
				-Mathf.Sin(angle)
			) * COLOR_RADIUS * s; 
			return markerPosition;
		}

		public void reset()
		{
			this.setColor(new Color(1,1,1,0.5f));
		}
	}

	public override void OnInspectorGUI()
    {
        ColorGradeConfig config = (ColorGradeConfig) target;		

		if (this.m_ColorWheel == null)
		{
			this.m_ColorWheel = AssetDatabase.LoadAssetAtPath<Texture>("Assets/ColorGrade/Editor/colorwheel_150.png");
		}
		if (this.m_ColorWheelMarker == null)
		{
			this.m_ColorWheelMarker = AssetDatabase.LoadAssetAtPath<Texture>("Assets/ColorGrade/Editor/colorwheel_marker.png");
		}
		if (this.m_Shadow == null)
		{
			this.m_Shadow = new WheelConfig();
		}
		this.m_Shadow.setColor(config._Shadows);
		if (this.m_Midtone == null)
		{
			this.m_Midtone = new WheelConfig();
		}
		this.m_Midtone.setColor(config._Midtones);
		if (this.m_Highlight == null)
		{
			this.m_Highlight = new WheelConfig();
		}
		this.m_Highlight.setColor(config._Hilights);
		EditorGUILayout.BeginHorizontal();

		bool updated = false;
		updated |= DrawWheelHSV(this.m_Shadow);
		updated |= DrawWheelHSV(this.m_Midtone);
		updated |= DrawWheelHSV(this.m_Highlight);

		EditorGUILayout.EndHorizontal();

		if (Event.current.type == EventType.Repaint && this.m_PendingClick)
		{									
			this.m_PendingClick = false;
		}

		if (
			(Event.current.button == 0) && 			
			(				
				Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag
			)
		)
		{
			this.m_PendingClick = true;
			this.m_ClickPosition = Event.current.mousePosition;
			Repaint();
		}

		ColorGradeConfig wasSyncedConfig = SYNCED_CONFIG;
		bool wasSynced = (config == SYNCED_CONFIG);
		bool synced = EditorGUILayout.Toggle("Preview Main Camera",wasSynced);
		if (wasSynced != synced)
		{
			if (synced)
			{
				SYNCED_CONFIG = config;
				updated = true;
			}
			if (!synced && wasSynced && wasSyncedConfig == config)
			{
				SYNCED_CONFIG = null;
				setMainCamera(null);
			}
		}

		if (updated)
		{
			config._Shadows = this.m_Shadow.color;
			config._Midtones = this.m_Midtone.color;
			config._Hilights = this.m_Highlight.color;

			if (SYNCED_CONFIG == config)
			{
				setMainCamera(config);
			}
		}
    }

	private void setMainCamera(ColorGradeConfig config)
	{
		UnityStandardAssets.ImageEffects.ColorGradingEffect effect = GameObject.FindObjectOfType<UnityStandardAssets.ImageEffects.ColorGradingEffect>();
		if (effect != null)
		{
			if (config != null)
			{
				effect._Shadows = config._Shadows;
				effect._Midtones = config._Midtones;
				effect._Hilights = config._Hilights;
			} else 
			{
				Color defaultColor = new Color(1,1,1,0.5f);
				effect._Shadows = defaultColor;
				effect._Midtones = defaultColor;
				effect._Hilights = defaultColor;
			}
		}			
		System.Reflection.Assembly assembly = typeof(UnityEditor.EditorWindow).Assembly;
		EditorWindow gameview = EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
		gameview.Repaint();
	}

	private bool DrawWheelHSV(WheelConfig config)
	{
		bool updated = false;
		EditorGUILayout.BeginVertical();
		GUILayout.Label(this.m_ColorWheel, GUIStyle.none,GUILayout.Width(80),GUILayout.Height(80));
		
		if (Event.current.type == EventType.Repaint && config.wheelTopLeft == Vector2.zero)
		{
			config.wheelTopLeft = new Vector2(
				GUILayoutUtility.GetLastRect().x,
				GUILayoutUtility.GetLastRect().y
			);
		}

		if (Event.current.type == EventType.Repaint && this.m_PendingClick)
		{									
			Vector2 center = new Vector2(WheelConfig.IMAGE_RADIUS,WheelConfig.IMAGE_RADIUS);
			Vector2 mouseClick = this.m_ClickPosition - config.wheelTopLeft - center;
			if (mouseClick.magnitude < WheelConfig.COLOR_RADIUS)
			{
				mouseClick = Vector2.ClampMagnitude(mouseClick, WheelConfig.COLOR_RADIUS);

				float dist = (mouseClick).magnitude / WheelConfig.COLOR_RADIUS;
				float theta = Mathf.Atan2( -mouseClick.y, mouseClick.x);
				float h = 0;
				if (theta > 0)
				{
					h = theta;
					h = h / (2*Mathf.PI);
				}
				else
				{
					h = 2*Mathf.PI + theta;
					h = h / (2*Mathf.PI);
				}

				config.setHueSat(h, dist);
				updated = true;
			}
		}
		Vector2 markerPosition = config.getMarkerPosition();
		GUI.Label(
			new Rect(markerPosition.x - 5,markerPosition.y-5,10,10),
			this.m_ColorWheelMarker
		);		
		GUI.enabled = false;
		EditorGUILayout.ColorField(config.color);
		GUI.enabled = true; // to make color field readonly
		EditorGUIUtility.fieldWidth = 25;
		float origLevels = config.getLevel();
		config.setLevel(EditorGUILayout.Slider(config.getLevel(),-100, 100));
		if (config.getLevel() != origLevels)
		{
			updated = true;
		}
		if (GUILayout.Button("Reset"))
		{
			config.reset();
			updated = true;
		}
		EditorGUILayout.EndVertical();
		return updated;
	}

}
