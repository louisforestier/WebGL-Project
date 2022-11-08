
precision mediump float;

varying vec4 pos3D;
varying vec3 N;
varying mat4 invRotMatrix;

uniform samplerCube uSampler;
uniform float uRefractIndex;
uniform int uShaderState;

#define AIR_REFRACT_INDEX 1.0
#define REFLECT 0
#define REFRACT 1
#define FRESNEL 2

#define PI 3.1415926538

// ======================================================================
// Fonction générales
// ======================================================================

// Adapte la direction selon notre origine
vec3 adaptDir(vec3 dir)
{
	return dir.xzy;
}

// Adapte la direction selon notre origine
vec3 adaptDir(vec4 dir)
{
	return dir.xzy;
}

// Met un flottant au carré
float square(float x)
{
	return x * x;
}

// ======================================================================
// Jalon 1 : Skybox, Mirroir, Transparence et Fresnel
// ======================================================================

// Calcul le facteur de fresnel
float fresnelFactor(vec3 i, vec3 m, float ni)
{
	float c = abs(dot(i,m));
	float g = sqrt(ni*ni+c*c-1.0);
	float f = 0.5 * square(g-c)/square(g+c) * ( 1.0 + (square(c * (g+c) - 1.0)/square(c * (g-c) + 1.0)));
	return f;
}

// Fait une refraction de la skybox, c'est la fonction principale de la transparance
vec4 refractSkybox(vec3 pos, vec3 normal, mat4 invRotMatrix, float ind1, float ind2)
{
	vec3 Vo = normalize(pos);
	vec4 Vt = vec4(refract(Vo,normal,ind1/ind2),1.0);
	Vt = invRotMatrix * Vt;

	return vec4(textureCube(uSampler,adaptDir(Vt)).rgb,1.0);
}

// Fait une reflection de la skybox, c'est la fonction principale du Mirroir parfait
vec4 reflectSkybox(vec3 pos, vec3 normal, mat4 invRotMatrix)
{
	vec3 Vo = normalize(pos);
	vec4 Vi = vec4(reflect(Vo,normal),1.0);
	Vi = invRotMatrix * Vi;

	return vec4(textureCube(uSampler,adaptDir(Vi)).rgb,1.0);
}

// Fait une refraction, une reflection et le multiplie par le facteur de fresnel, 
// C'est la fonction principale de l'affichage fresnel
vec4 fresnelEffect(vec3 pos, vec3 normal, mat4 invRotMatrix, float ind1, float ind2)
{
	vec3 Vo = normalize(pos);

	vec4 Vi = vec4(reflect(Vo,normal),1.0);
	float f = fresnelFactor(Vi.xyz,normal,ind2);
	Vi = invRotMatrix * Vi;

	vec4 mColor = vec4(textureCube(uSampler,adaptDir(Vi)).rgb,1.0);

	vec4 Vt = vec4(refract(Vo,normal,ind1/ind2),1.0);
	Vt = invRotMatrix * Vt;

	vec4 tColor = vec4(textureCube(uSampler,adaptDir(Vt)).rgb,1.0);

	return f * mColor + (1.0-f) * tColor;
}


// ======================================================================
// Jalon 2 : Cook & Torrence
// ======================================================================

// Fonction calculant la distribution de Beckmann
float beckmann(vec3 i, vec3 m, float sigma)
{
	float cosinus = dot(i,m);
	float denominateur = PI * square(sigma) *square(square(cosinus));
	float sinus = sqrt(1.0 - square(cosinus));
	float tangente = sinus / cosinus;
	float exposant = -(square(tangente))/(2.0*square(sigma));
	return 1.0 / denominateur * exp(exposant);
}

// Fonction calculant l'ombrage et le Masquage
float g(vec3 n, vec3 m, vec3 i, vec3 o)
{
	float num1 = 2. * dot(n, m) * dot(n, o);
	float num2 = 2. * dot(n, m) * dot(n, i);
	float denom1 = dot(o, m);
	float denom2 = dot(i, m);
	return min(1., min(num1/denom1, num2/denom2));
}

// ======================================================================
// Main du Shader
// ======================================================================
void main(void)
{
	vec4 col= vec4(0.0);
	if(uShaderState == REFLECT)
	{
		col = reflectSkybox(pos3D.xyz, normalize(N),invRotMatrix);
	}
	else if(uShaderState == REFRACT)
	{
		col = refractSkybox(pos3D.xyz, normalize(N),invRotMatrix,1.0,1.52);
	}
	else if(uShaderState == FRESNEL)
	{
		col = fresnelEffect(pos3D.xyz, normalize(N),invRotMatrix,AIR_REFRACT_INDEX,uRefractIndex);
	}
	else 
	{
		vec3 color = vec3(0.8,0.4,0.4) * dot(normalize(N),normalize(vec3(-pos3D))); // Lambert rendering, eye light source
		col= vec4(color,1.0);
	}
	gl_FragColor = col;
}
