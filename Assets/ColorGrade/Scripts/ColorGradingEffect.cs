using UnityEngine;

using UnityEditor;

namespace UnityStandardAssets.ImageEffects
{
    [ExecuteInEditMode]
    [RequireComponent (typeof(Camera))]
    public class ColorGradingEffect : PostEffectsBase
	{
        public Color _Shadows = new Color(0,0,0,0);
        public Color _Midtones = new Color(0,0,0,0);
        public Color _Hilights = new Color(0,0,0,0);

        public Shader overlayShader = null;
        private Material overlayMaterial = null;
        private ColorGradeConfig m_TargetConfig;

        public override bool CheckResources ()
		{
            CheckSupport (false);

            overlayMaterial = CheckShaderAndCreateMaterial (overlayShader, overlayMaterial);

            if	(!isSupported)
                ReportAutoDisable ();
            return isSupported;
        }

        public static void getLevels(
            Vector3 HSVshadows, Vector3 HSVmidtones, Vector3 HSVhighlights,
            out Color gamma, out Color lowIn, out Color lowOut, out Color hiIn, out Color hiOut
        )
        {
            Color shadowColor = Color.HSVToRGB(HSVshadows.x / 360f,1.0f,(HSVshadows.y / 100f) * 0.40f);
            Color midtoneColor = Color.HSVToRGB(HSVmidtones.x / 360f,1.0f,(HSVmidtones.y / 100f) * 0.40f);
            Color highColor = Color.HSVToRGB(HSVhighlights.x / 360f,1.0f,(HSVhighlights.y / 100f) * 0.40f);

            gamma = new Color(0.0f,0.0f,0.0f); // 1f neutral
            lowIn = new Color(0.0f,0.0f,0.0f); // 0 - 1
            lowOut = new Color(0.0f,0.0f,0.0f); // 0 - 1
            hiIn = new Color(1f,1f,1f); // 0 - 1
            hiOut = new Color(1f,1f,1f); // 0 - 1
            float midlev = HSVmidtones.z / 255f; // <-100;100>
            float shlev = HSVshadows.z / 255f; // <-100;100>
            float highlev = HSVhighlights.z / 255f; // <-100;100>

            // MIDTONES
            gamma += midtoneColor.r * 255f / 100f * new Color( 0.4f, -0.3f, -0.3f );
            gamma += midtoneColor.g * 255f / 100f * new Color( -0.3f, +0.4f, -0.3f );
            gamma += midtoneColor.b * 255f / 100f * new Color( -0.3f, -0.3f, 0.4f );
            gamma += new Color( 1f, 1f, 1f );
            gamma += (midlev > 0f ? 0.4f : 0.3f) * midlev / 100f * new Color( 1f, 1f, 1f );

            // SHADOWS
            shadowColor.r *= 0.3f;
            shadowColor.g *= 0.3f;
            shadowColor.b *= 0.3f;
            if (shadowColor.r > 0)
            {
                lowIn += new Color(0f,shadowColor.r,shadowColor.r);
                lowOut.r += shadowColor.r;
            } else 
            {
                lowIn.r -= shadowColor.r;
                lowOut -= new Color(0f,shadowColor.r,shadowColor.r);
            }
            if (shadowColor.g > 0)
            {
                lowIn += new Color(shadowColor.g,0f,shadowColor.g);
                lowOut.g += shadowColor.g;
            } else 
            {
                lowIn.g -= shadowColor.g;
                lowOut -= new Color(shadowColor.g,0,shadowColor.g);
            }
            if (shadowColor.b > 0)
            {
                lowIn += new Color(shadowColor.b,shadowColor.b,0);
                lowOut.b += shadowColor.b;
            } else 
            {
                lowIn.b -= shadowColor.b;
                lowOut -= new Color(shadowColor.b,shadowColor.b,0);
            }

            if (shlev > 0)
            {
                lowOut += new Color(shlev,shlev,shlev);
            } else 
            {
                lowIn -= new Color(shlev,shlev,shlev);
            }

            // HIGHLIGHTS
            highColor.r *= 0.3f;
            highColor.g *= 0.3f;
            highColor.b *= 0.3f;
            if (highColor.r > 0)
            {
                hiIn.r -= highColor.r;
                hiOut -= new Color(0,highColor.r,highColor.r);
            } else 
            {
                hiIn += new Color(0,highColor.r,highColor.r);
                hiOut.r += highColor.r;
            }
            if (highColor.g > 0)
            {
                hiIn.g -= highColor.g;
                hiOut -= new Color(highColor.g,0,highColor.g);
            } else 
            {
                hiIn += new Color(highColor.g,0,highColor.g);
                hiOut.g += highColor.g;
            }
            if (highColor.b > 0)
            {
                hiIn.b -= highColor.b;
                hiOut -= new Color(highColor.b,highColor.b,0);
            } else 
            {
                hiIn += new Color(highColor.b,highColor.b,0);
                hiOut.b += highColor.b;
            }
            if (highlev > 0)
            {
                hiIn -= new Color(highlev,highlev,highlev);
            } else 
            {
                hiOut += new Color(highlev,highlev,highlev);
            }
        }

        private static Vector3 ColorToHSV(Color c)
        {
            float h;
            float s;
            float v;
            Color.RGBToHSV(c, out h, out s, out v);
            return new Vector3(
                h * 360f,
                s * 100f,
                // levels is alpha
                // alpha = 0.5 > levels = 0 neutral
                // alpha = 0 > levels = -100 darken
                // alpha = 1 > levels = 100 brighten
                (c.a - 0.5f) * 200f
            );
        }

        public void applyColorGrading(ColorGradeConfig config)
        {
            this.m_TargetConfig = config;
        }

        private void Update()
        {
            if (this.m_TargetConfig != null)
            {
                // apply gradually
                float lerpSpeed = 3.8f;
                this._Shadows = Color.Lerp(this._Shadows,this.m_TargetConfig._Shadows, Time.deltaTime * lerpSpeed);
                this._Midtones = Color.Lerp(this._Midtones,this.m_TargetConfig._Midtones, Time.deltaTime * lerpSpeed);
                this._Hilights = Color.Lerp(this._Hilights,this.m_TargetConfig._Hilights, Time.deltaTime * lerpSpeed);
            }
        }

        void OnRenderImage (RenderTexture source, RenderTexture destination)
		{            
            if (CheckResources() == false)
			{
                Graphics.Blit (source, destination);
                return;
            }

            Vector3 HSVshadows = ColorToHSV(this._Shadows);
            Vector3 HSVmidtones = ColorToHSV(this._Midtones);
            Vector3 HSVhighlights = ColorToHSV(this._Hilights);

            Color gamma = new Color(1.0f,1.0f,1.0f);
            Color lowIn = new Color(0.0f,0.0f,0.0f);
            Color lowOut = new Color(0.0f,0.0f,0.0f);
            Color hiIn = new Color(1f,1f,1f);
            Color hiOut = new Color(1f,1f,1f);

            getLevels(
                HSVshadows, HSVmidtones, HSVhighlights, 
                out gamma, out lowIn, out lowOut, out hiIn, out hiOut
            );

            overlayMaterial.SetVector("_RedIn",new Vector3( lowIn.r,gamma.r,hiIn.r));
			overlayMaterial.SetVector("_RedOut",new Vector3(lowOut.r,1,hiOut.r));
            overlayMaterial.SetVector("_GreenIn",new Vector3( lowIn.g,gamma.g,hiIn.g));
			overlayMaterial.SetVector("_GreenOut",new Vector3(lowOut.g,1,hiOut.g));
            overlayMaterial.SetVector("_BlueIn",new Vector3( lowIn.b,gamma.b,hiIn.b));
			overlayMaterial.SetVector("_BlueOut",new Vector3(lowOut.b,1,hiOut.b));
			
            Graphics.Blit (source, destination, overlayMaterial, 0);
        }
    }
}
