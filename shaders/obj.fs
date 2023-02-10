
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
uniform vec3 uLightPos;

#define AIR_REFRACT_INDEX 1.
#define REFLECT 0
#define REFRACT 1
#define FRESNEL 2
#define COOKTORRANCE 4
#define ECHANTILLONNAGE 5
#define MIROIRDEPOLI 6
#define WALTERGGX 7

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
	return x*x;
}

//renvoie le max entre 0 et le produit scalaire des vecteurs en paramètres
float ddot(vec3 left,vec3 right)
{
	return max(0.,dot(left,right));
}

//renvoie le max entre 0 et le produit scalaire des vecteurs en paramètres
float ddot(vec4 left,vec4 right)
{
	return max(0.,dot(left,right));
}

// ======================================================================
// Jalon 1 : Skybox, Mirroir, Transparence et Fresnel
// ======================================================================

// Calcul le facteur de fresnel
float fresnelFactor(vec3 i,vec3 m,float ni)
{
	float c=abs(dot(i,m));
	float g=sqrt(ni*ni+c*c-1.);
	float f=.5*square(g-c)/square(g+c)*(1.+(square(c*(g+c)-1.)/square(c*(g-c)+1.)));
	return f;
}

// Fait une refraction de la skybox, c'est la fonction principale de la transparance
vec4 refractSkybox(vec3 pos,vec3 normal,mat4 invRotMatrix,float ind1,float ind2)
{
	vec3 Vo=normalize(pos);
	vec4 Vt=vec4(refract(Vo,normal,ind1/ind2),1.);
	Vt=invRotMatrix*Vt;
	
	return vec4(textureCube(uSampler,adaptDir(Vt)).rgb,1.);
}

// Fait une reflection de la skybox, c'est la fonction principale du Mirroir parfait
vec4 reflectSkybox(vec3 pos,vec3 normal,mat4 invRotMatrix)
{
	vec3 Vo=normalize(pos);
	vec4 Vi=vec4(reflect(Vo,normal),1.);
	Vi=invRotMatrix*Vi;
	
	return vec4(textureCube(uSampler,adaptDir(Vi)).rgb,1.);
}

// Fait une refraction, une reflection et le multiplie par le facteur de fresnel,
// C'est la fonction principale de l'affichage fresnel
vec4 fresnelEffect(vec3 pos,vec3 normal,mat4 invRotMatrix,float ind1,float ind2)
{
	vec3 Vo=normalize(pos);
	
	vec4 Vi=vec4(reflect(Vo,normal),1.);
	float f=fresnelFactor(Vi.xyz,normal,ind2);
	Vi=invRotMatrix*Vi;
	
	vec4 mColor=vec4(textureCube(uSampler,adaptDir(Vi)).rgb,1.);
	
	vec4 Vt=vec4(refract(Vo,normal,ind1/ind2),1.);
	Vt=invRotMatrix*Vt;
	
	vec4 tColor=vec4(textureCube(uSampler,adaptDir(Vt)).rgb,1.);
	
	return f*mColor+(1.-f)*tColor;
}

// ======================================================================
// Jalon 2 : Cook & Torrance
// ======================================================================

// Fonction calculant la distribution de Beckmann
float beckmann(vec3 n,vec3 m,float sigma)
{
	float cosinus=ddot(n,m);
	float denominateur=PI*square(sigma)*square(square(cosinus));
	float sinus=sqrt(1.-square(cosinus));
	float tangente=sinus/cosinus;
	float exposant=-(square(tangente))/(2.*square(sigma));
	return exp(exposant)/denominateur;
}

// Fonction calculant l'ombrage et le Masquage
float g(vec3 n,vec3 m,vec3 i,vec3 o)
{
	float num1=2.*ddot(n,m)*ddot(n,o);
	float num2=2.*ddot(n,m)*ddot(n,i);
	float denom1=ddot(o,m);
	float denom2=ddot(i,m);
	return min(1.,min(num1/denom1,num2/denom2));
}

// Calcul de l'éclairement d'un objet selon Cook & Torrance
// avec 0,0,0 comme position d'une source lumineuse
vec4 cookTorrance(vec3 pos,vec3 normal,mat4 invRotMatrix,float ni,float sigma)
{
	// Calcul des vecteurs nécessaires plus bas
	vec3 Vo = normalize(-pos);
	vec3 i = normalize(uLightPos - pos);
	vec3 m = normalize(i+Vo);

    // Calcul de la fonction Fs a partir des fonctions F, D et G
	float F = fresnelFactor(i,m,ni);
	float D = beckmann(normal,m,sigma);
	float G = g(normal,m,i,Vo);
	float fs = (F * D * G) / (4. * ddot(i,normal) * ddot(Vo,normal)); 

    // Calcul de la valeur final de la couleur pour l'objet
	vec3 fr = ((uKd / PI) * (1.0 - F) +  vec3(fs));

	vec3 color = uLightIntensity * fr * ddot(normal,i);
	
	return vec4(color,1.0);
}

// ======================================================================
// Jalon 3 : Echantillonnage d'importancce
// ======================================================================

float RAND_CONST=0.;
//Génère un nombre aléatoire entre 0 et 1
float rand()
{
	vec2 co=(invRotMatrix*gl_FragCoord).xy;
	co+=RAND_CONST;
	// limite le nombre d'échantillon à 100
	RAND_CONST+=.01;
	return fract(sin(dot(co,vec2(12.9898,78.233)))*43758.5453);
}

//Calcule la normale selon la pdf en fonction de sigma (et de 2 valeurs aléatoires)
vec3 computeNormal(float sigma)
{
	float phi=rand()*2.*PI;
	float theta=atan(sqrt(-square(sigma)*log(1.-rand())));
	vec3 m=vec3(0.);
	m.x=sin(theta)*cos(phi);
	m.y=sin(theta)*sin(phi);
	m.z=cos(theta);
	return m;
}

//Calcul de l'éclairement d'un objet avec l'échantillonnage d'importance
vec4 echantillonnagePasOpti(vec3 pos,vec3 normal,mat4 invRotMatrix,float ni,float sigma)
{
	vec3 Lo=vec3(0.);
	//100 échantillons au maximum
	for(int k=0;k<100;k++)
	{
		//si le numéro de l'échantillon est supérieur ou égal au nombre passé en uniform, on sort de la boucle
		if(k>=uNbSamples) 
			break;
		
		vec3 Vo=normalize(-pos);
		vec3 m=computeNormal(sigma);
		//Calcul de la matrice de rotation locale
		vec3 i0=vec3(1,0,0);
		if(dot(i0,normal)>.8)
		{
			i0=vec3(0,1,0);
		}
		vec3 j=cross(i0,normal);
		vec3 i1=cross(j,normal);
		mat3 Mrl=mat3(i1,j,normal);
		
		m=normalize(Mrl*m);
		vec3 i=reflect(-Vo,m);
		float test=ddot(normal,m);
		if(test == 0.0)
			continue;
		
		//Calcul de la brdf à partir des fonctions F, D et G
		float F=fresnelFactor(i,m,ni);
		float D=beckmann(normal,m,sigma);
		float G=g(normal,m,i,Vo);
		float pdf=D*ddot(normal,m);
		float brdf=(F*D*G)/(4.*ddot(i,normal)*ddot(Vo,normal));
		float iDotn = ddot(i,normal);
		vec3 Li=vec3(0.);
		Li=textureCube(uSampler,adaptDir(invRotMatrix*vec4(i,1.))).rgb;
		
		Lo+=Li*brdf*iDotn/pdf;
	}
	return vec4(Lo/float(uNbSamples),1.);
}

// Fonction calculant la distribution de Beckmann de manière "optimisée"
float beckmannOpti(float nDotm,float sigma)
{
	float sigma2 = sigma*sigma;
	float nDotm2 = nDotm * nDotm;
	float denominateur=PI*sigma2*(nDotm2*nDotm2);
	float sinus=sqrt(1.-nDotm2);
	float tangente=sinus/nDotm;
	float exposant=-(square(tangente))/(2.*sigma2);
	return exp(exposant)/denominateur;
}

// Fonction calculant l'ombrage et le Masquage de manière "optimisée"
float gOpti(float nDotm,float iDotn,float oDotn,float oDotm,float iDotm)
{
	float nDotm2 = nDotm * 2.0;
	return min(1.,min(nDotm2 * oDotn / oDotm, nDotm2 * iDotn / iDotm));
}

//Calcul de l'éclairement d'un objet avec l'échantillonnage d'importance de manière "optimisée"
vec4 echantillonnage(vec3 pos,vec3 normal,mat4 invRotMatrix,float ni,float sigma)
{
	vec3 Lo=vec3(0.);
	//100 échantillons au maximum
	for(int k=0;k<100;k++)
	{
		//si le numéro de l'échantillon est supérieur ou égal au nombre passé en uniform, on sort de la boucle
		if(k>=uNbSamples)
			break;
		
		vec3 Vo=normalize(-pos);
		vec3 m=computeNormal(sigma);

		//Calcul de la matrice de rotation locale
		vec3 i0=vec3(1,0,0);
		if(dot(i0,normal)>.8)
		{
			i0=vec3(0,1,0);
		}
		vec3 j=cross(i0,normal);
		vec3 i1=cross(j,normal);
		mat3 Mrl=mat3(i1,j,normal);
		
		m=normalize(Mrl*m);
		vec3 i=reflect(-Vo,m);
		float nDotm = ddot(normal,m);
		float iDotn = ddot(i,normal);
		float oDotn = ddot(Vo,normal);
		//pour éviter les divisions par 0
		if(nDotm == 0.0 || iDotn == 0.0 || oDotn == 0.0)
			continue;
		
		// // Calcul de la fonction Fs a partir des fonctions F, D et G
		float F=fresnelFactor(i,m,ni);
		float D=beckmannOpti(nDotm,sigma);
		float G=gOpti(nDotm,iDotn, oDotn, ddot(Vo,m), ddot(i,m));
		//D a été supprimé du calcul de pdf et brdf par simplification
		float pdf=nDotm;
		float brdf=(F*G)/(4.*iDotn*oDotn);
		vec3 Li=vec3(0.);
		Li=textureCube(uSampler,adaptDir(invRotMatrix*vec4(i,1.))).rgb;
		
		Lo+=Li*brdf*iDotn/pdf;
	}
	return vec4(Lo/float(uNbSamples),1.);
}

//Simule un miroir dépoli en considérant les microfacettes comme des miroirs, plus ou moins dépoli en fonction de sigma 
vec4 miroirDepoli(vec3 pos,vec3 normal,mat4 invRotMatrix,float sigma)
{
	vec3 Lo=vec3(0.);
	//100 échantillons au maximum
	for(int k=0;k<100;k++)
	{
		//si le numéro de l'échantillon est supérieur ou égal au nombre passé en uniform, on sort de la boucle
		if(k>=uNbSamples)
			break;

		vec3 Vo=normalize(-pos);
		vec3 m=computeNormal(sigma);

		//Calcul de la matrice de rotation locale
		vec3 i0=vec3(1,0,0);
		if(dot(i0,normal)>.8)
		{
			i0=vec3(0,1,0);
		}
		vec3 j=cross(i0,normal);
		vec3 i1=cross(j,normal);
		mat3 Mrl=mat3(i1,j,normal);
		
		m=normalize(Mrl*m);
		vec3 i=reflect(-Vo,m);
		
		vec3 Li=textureCube(uSampler,adaptDir(invRotMatrix*vec4(i,1.))).rgb;
		
		Lo += Li;
	}
	return vec4(Lo/float(uNbSamples),1.);
}

vec4 gWalterGGX(float nDotm, float sigma){
    float sigma2 = square(sigma);
    float nDotm4 = nDotm * nDotm * nDotm * nDotm;
    float sinus = sqrt(1. - nDotm2);
    float tan2 = square(sinus / nDotm);
    float denominateur = PI * nDotm4 * square(sigma2 + tan2);
    return sigma2 / denominateur;
}

vec4 walterGGX(vec3 pos,vec3 normal,mat4 invRotMatrix,float ni,float sigma)
{
    return miroirDepoli(pos, normal, invRotMatrix, sigma);
}

// ======================================================================
// Main du Shader
// ======================================================================
void main(void)
{
	vec4 col=vec4(0.);
	if(uShaderState==REFLECT)
	{
		col=reflectSkybox(pos3D.xyz,normalize(N),invRotMatrix);
	}
	else if(uShaderState==REFRACT)
	{
		col=refractSkybox(pos3D.xyz,normalize(N),invRotMatrix,AIR_REFRACT_INDEX,uRefractIndex);
	}
	else if(uShaderState==FRESNEL)
	{
		col=fresnelEffect(pos3D.xyz,normalize(N),invRotMatrix,AIR_REFRACT_INDEX,uRefractIndex);
	}
	else if(uShaderState==COOKTORRANCE)
	{
		col=cookTorrance(pos3D.xyz,normalize(N),invRotMatrix,uRefractIndex,uSigma);
	}
	else if(uShaderState==ECHANTILLONNAGE)
	{
		col=echantillonnage(pos3D.xyz,normalize(N),invRotMatrix,uRefractIndex,uSigma);
	}
    else if(uShaderState==MIROIRDEPOLI){
        col=miroirDepoli(pos3D.xyz,normalize(N),invRotMatrix,uSigma);
    }
    else if(uShaderState==WALTERGGX){
        col=walterGGX(pos3D.xyz,normalize(N),invRotMatrix,uRefractIndex,uSigma);
    }
	else
	{
		vec3 color=uKd*dot(normalize(N),normalize(vec3(-pos3D)));// Lambert rendering, eye light source
		col=vec4(color,1.);
	}
	gl_FragColor=col;
}
