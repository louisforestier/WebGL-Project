
precision mediump float;

varying vec4 pos3D;
varying vec3 N;
varying mat4 invRotMatrix;

uniform samplerCube uSampler;
uniform float uRefractIndex;
uniform float uSigma;
uniform float uLightIntensity;
uniform int uShaderState;
uniform vec3 uKd;
uniform int uNbSamples;

#define AIR_REFRACT_INDEX 1.0
#define REFLECT 0
#define REFRACT 1
#define FRESNEL 2
#define COOKTORRANCE 4
#define ECHANTILLONNAGE 5

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

float ddot(vec3 left, vec3 right)
{
	return max(0.0,dot(left,right));
}

float ddot(vec4 left, vec4 right)
{
	return max(0.0,dot(left,right));
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
// Jalon 2 : Cook & Torrance
// ======================================================================

// Fonction calculant la distribution de Beckmann
float beckmann(vec3 n, vec3 m, float sigma)
{
	float cosinus = dot(n,m);
	float denominateur = PI * square(sigma) *square(square(cosinus));
	float sinus = sqrt(1.0 - square(cosinus));
	float tangente = sinus / cosinus;
	float exposant = -(square(tangente))/(2.0*square(sigma));
	return exp(exposant) / denominateur ;
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

// Calcul de l'éclairement d'un objet selon Cook & Torrance
// avec 0,0,0 comme position d'une source lumineuse
vec4 cookTorrance(vec3 pos, vec3 normal, mat4 invRotMatrix, float ni, float sigma)
{
    // Calcul des vecteurs nécessaires plus bas
	vec3 Vo = normalize(-pos);
	vec3 i = Vo;
	vec3 m = normalize(i+Vo);

    // Calcul de la fonction Fs a partir des fonctions F, D et G
	float F = fresnelFactor(i,m,ni);
	float D = beckmann(normal,m,sigma);
	float G = g(normal,m,i,Vo);
	float fs = (F * D * G) / (4. * abs(dot(i,normal)) * abs(dot(Vo,normal))); 

    // Calcul de la valeur final de la couleur pour l'objet
	vec3 color = (uKd / PI) * (1.0 - F) +  vec3(fs);

	color = uLightIntensity * color * dot(normal,i);
	
	return vec4(color,1.0);
}

// ======================================================================
// Jalon 3 : Echantillonnage d'importancce
// ======================================================================

//utiliser le temps + randconst pour la seed, en donnant le temps en uniform
float PHI = 1.61803398874989484820459;  // Φ = Golden Ratio   

float gold_noise(vec2 xy, float seed){
	return fract(tan(distance(xy*PHI, xy)*seed)*xy.x);
}
float RAND_CONST = 0.0;
float rand()
{
	vec2 co = (invRotMatrix * gl_FragCoord).xy;
	co += RAND_CONST;
	// limite le nombre d'échantillon à 100
	RAND_CONST += 0.1;
    return fract(sin(dot(co, vec2(12.9898, 78.233))) * 43758.5453);
}

vec3 computeNormal(float sigma)
{
	float rand1 = rand();
	float rand2 = rand(); 
	float phi = rand1 * 2.0 * PI;
	float theta = atan(sqrt(-square(sigma) * log(1.0-rand2)));
	vec3 m = vec3(0.0);
	m.x = sin(theta) * cos(phi);
	m.y = sin(theta) * sin(phi);
	m.z = cos(theta);
	return m;
}

vec4 echantillonnage(vec3 pos, vec3 normal, mat4 invRotMatrix, float ni, float sigma)
{
	vec3 color = vec3(0.0);
	int N = 1;
	for(int k = 0; k < 100 ; k++)
	{
		if(k > uNbSamples)
			break;
		// Calcul des vecteurs nécessaires plus bas
		vec3 Vo = normalize(-pos);
		vec3 i = normalize(computeNormal(sigma));
		vec3 i0 = vec3(1,0,0);
		if(dot(i0,normal)>0.8)
		{
			i0 = vec3(0,1,0);
		}

		vec3 j = cross(i0,normal);
		vec3 i1 = cross(j,normal);
		mat3 Mrl = mat3(0.0);
		Mrl[0] = i1;
		Mrl[1] = j;
		Mrl[2] = normal;

		i = normalize(Mrl * i);
		vec3 m = normalize(i+Vo);

		// Calcul de la fonction Fs a partir des fonctions F, D et G
		float F = fresnelFactor(i,m,ni);
		float D = beckmann(normal,m,sigma);
		float G = g(normal,m,i,Vo);
		float pdf = D * ddot(normal,m);
		float fs = (F * D * G) / (4. * ddot(i,normal) * ddot(Vo,normal)); 

		// Calcul de la valeur final de la couleur pour l'objet
		vec3 color1 = (uKd / PI) * (1.0 - F) + vec3(fs);

		vec3 mColor = textureCube(uSampler,adaptDir(invRotMatrix* vec4(i,1.0))).rgb;

		color1 = uLightIntensity* mColor * color1 * ddot(i,normal);
		color += color1;
	}
	return vec4(color/float(uNbSamples),1.0);
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
		col = refractSkybox(pos3D.xyz, normalize(N),invRotMatrix,AIR_REFRACT_INDEX,uRefractIndex);
	}
	else if(uShaderState == FRESNEL)
	{
		col = fresnelEffect(pos3D.xyz, normalize(N),invRotMatrix,AIR_REFRACT_INDEX,uRefractIndex);
	}
	else if(uShaderState == COOKTORRANCE)
	{
		col = cookTorrance(pos3D.xyz, normalize(N), invRotMatrix,uRefractIndex,uSigma);
	}
	else if(uShaderState == ECHANTILLONNAGE)
	{
		col = echantillonnage(pos3D.xyz, normalize(N), invRotMatrix,uRefractIndex,uSigma);
	}
	else 
	{
		vec3 color = uKd * dot(normalize(N),normalize(vec3(-pos3D))); // Lambert rendering, eye light source
		col= vec4(color,1.0);
	}
	gl_FragColor = col;
}
