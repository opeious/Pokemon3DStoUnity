#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED



void CalculateMainLight_float(float3 WorldPos, out float3 Direction, out float3 Color, out half DistanceAtten, out half ShadowAtten) {
#if SHADERGRAPH_PREVIEW
	Direction = float3(0.5, 0.5 ,0);
	Color = 1;
	DistanceAtten = 1;
	ShadowAtten = 1;
#else
	#if SHADOWS_SCREEN
		half4 clipPos = TransformWorldToHClip(WorldPos);
		half4 shadowCoord = ComputeScreenPos(clipPos);
	#else
	#endif
	Light mainLight = GetMainLight(0);
	Direction = mainLight.direction;
	Color = mainLight.color;
	DistanceAtten = mainLight.distanceAttenuation;
	ShadowAtten = mainLight.shadowAttenuation;
#endif
}

void AddAdditionalLights_float(float3 WorldPosition, float3 WorldNormal, float3 WorldView,
	float MainDiffuse, float3 MainColor,
	out float Diffuse, out float3 Color)
{
	Diffuse = MainDiffuse;
	Color = MainColor * (MainDiffuse);

#ifndef SHADERGRAPH_PREVIEW
	int pixelLightCount = GetAdditionalLightsCount();
	for (int i = 0; i < pixelLightCount; i++)
	{
		Light light = GetAdditionalLight(i, WorldPosition);
		half NdotL = saturate(dot(WorldNormal, light.direction));
		half atten = light.distanceAttenuation * light.shadowAttenuation;
		half thisDiffuse = atten * NdotL;
		Diffuse += thisDiffuse;
		Color += light.color * (thisDiffuse); 
	}
#endif
	Color = Diffuse <= 0 ? MainColor : Color / Diffuse;
}

#endif