
precision mediump float;

varying vec4 pos3D;
varying vec3 N;
varying mat4 invRotMatrix;

uniform samplerCube uSampler;

vec3 adaptDir(vec3 dir)
{
	return vec3(dir.x,dir.z,dir.y);
}

float fresnelFactor(vec3 i, vec3 m, float ni)
{
	float c = max(0.0,dot(i,m));
	float g = sqrt(ni*ni+c*c-1.0);
	float f = 1.0/2.0 * pow(g-c,2.0)/pow(g+c,2.0) * ( 1.0 + (pow(c * (g+c) - 1.0,2.0)/pow(c * (g-c) - 1.0,2.0)));
	//float f = 1.0/2.0 * ((g - c)*(g-c)/(g+c)*(g+c)) * ( 1.0 + ((c * (g+c) - 1.0)*(c * (g+c) - 1.0))/((c * (g-c) - 1.0)*(c * (g-c) - 1.0)) );
	return f;
}


vec4 refractSkybox(vec3 pos, vec3 normal, mat4 invRotMatrix, float ind1, float ind2)
{
	vec3 Vo = normalize(pos);
	vec4 Vi = vec4(refract(Vo,normal,ind1/ind2),1.0);
	Vi = invRotMatrix * Vi;

	return vec4(textureCube(uSampler,vec3(Vi.x,Vi.z,Vi.y)).rgb,1.0);
}

vec4 reflectSkybox(vec3 pos, vec3 normal, mat4 invRotMatrix)
{
	vec3 Vo = normalize(pos);
	vec4 Vi = vec4(reflect(Vo,normal),1.0);
	Vi = invRotMatrix * Vi;

	return vec4(textureCube(uSampler,vec3(Vi.x,Vi.z,Vi.y)).rgb,1.0);
}



vec4 fresnelEffect(vec3 pos, vec3 normal, mat4 invRotMatrix, float ind1, float ind2)
{
	vec3 Vo = normalize(pos);

	vec4 Vi = vec4(reflect(Vo,normal),1.0);
	Vi = invRotMatrix * Vi;

	vec4 mColor = vec4(textureCube(uSampler,vec3(Vi.x,Vi.z,Vi.y)).rgb,1.0);

	vec4 Vt = vec4(refract(Vo,normal,ind1/ind2),1.0);
	Vt = invRotMatrix * Vt;

	vec4 tColor = vec4(textureCube(uSampler,vec3(Vt.x,Vt.z,Vt.y)).rgb,1.0);

	float f = fresnelFactor(Vi.xyz,normal,ind2);
	return f * mColor + (1.0-f) * tColor;
}



// ==============================================
void main(void)
{
	//vec3 col = vec3(0.8,0.4,0.4) * dot(N,normalize(vec3(-pos3D))); // Lambert rendering, eye light source

	//gl_FragColor = refractSkybox(pos3D.xyz, normalize(N),invRotMatrix,1.0,1.52);
	
	//gl_FragColor = reflectSkybox(pos3D.xyz, normalize(N),invRotMatrix);
	gl_FragColor = fresnelEffect(pos3D.xyz, normalize(N),invRotMatrix,1.0,1.0);
}
