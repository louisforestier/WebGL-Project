
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
#define WALTERGGXBRDF 7
#define WALTERGGXBSDF 8
#define TRANSPARENCEDEPOLIE 9

#define PI 3.1415926538

// ======================================================================
// Fonction générales
// ======================================================================

// ======================================================================
// Adapte la direction selon notre origine
vec3 adaptDir(vec3 dir)
{
	return dir.xzy;
}

// ======================================================================
// Adapte la direction selon notre origine
vec3 adaptDir(vec4 dir)
{
	return dir.xzy;
}

// ======================================================================
// Met un flottant au carré
float square(float x)
{
	return x*x;
}

// ======================================================================
// Renvoie le max entre 0 et le produit scalaire des vecteurs en paramètres
float ddot(vec3 left,vec3 right)
{
	return max(0.,dot(left,right));
}

// ======================================================================
// Renvoie le max entre 0 et le produit scalaire des vecteurs en paramètres
float ddot(vec4 left,vec4 right)
{
	return max(0.,dot(left,right));
}

// ======================================================================
// Jalon 1 : Skybox, Mirroir, Transparence et Fresnel
// ======================================================================

// ======================================================================
// Calcul le facteur de fresnel
float fresnelFactor(vec3 i,vec3 m,float ni)
{
	float c=abs(dot(i,m));
	float g=sqrt(ni*ni+c*c-1.);
	float f=.5*square(g-c)/square(g+c)*(1.+(square(c*(g+c)-1.)/square(c*(g-c)+1.)));
	return f;
}

// ======================================================================
// Fait une refraction de la skybox, c'est la fonction principale de la transparance
vec4 refractSkybox(vec3 pos,vec3 normal,mat4 invRotMatrix,float ind1,float ind2)
{
	vec3 Vo=normalize(pos);
	vec4 Vt=vec4(refract(Vo,normal,ind1/ind2),1.);
	Vt=invRotMatrix*Vt;
	
	return vec4(textureCube(uSampler,adaptDir(Vt)).rgb,1.);
}

// ======================================================================
// Fait une reflection de la skybox, c'est la fonction principale du Mirroir parfait
vec4 reflectSkybox(vec3 pos,vec3 normal,mat4 invRotMatrix)
{
	vec3 Vo=normalize(pos);
	vec4 Vi=vec4(reflect(Vo,normal),1.);
	Vi=invRotMatrix*Vi;
	
	return vec4(textureCube(uSampler,adaptDir(Vi)).rgb,1.);
}

// ======================================================================
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

// ======================================================================
// Fonction calculant la distribution de Beckmann
float beckmann(vec3 n,vec3 m,float sigma)
{
	float cosinus=ddot(n,m);
	if (cosinus == 0.0)
		return 0.0;
	float denominateur=PI*square(sigma)*square(square(cosinus));
	float sinus=sqrt(1.-square(cosinus));
	float tangente=sinus/cosinus;
	float exposant=-(square(tangente))/(2.*square(sigma));
	return exp(exposant)/denominateur;
}

// ======================================================================
// Fonction calculant l'ombrage et le masquage
float g(vec3 n,vec3 m,vec3 i,vec3 o)
{
	float num1=2.*ddot(n,m)*ddot(n,o);
	float num2=2.*ddot(n,m)*ddot(n,i);
	float denom1=ddot(o,m);
	float denom2=ddot(i,m);
	if (denom1 == 0.0 || denom2 == 0.0)
		return 0.0;
	return min(1.,min(num1/denom1,num2/denom2));
}

// ======================================================================
// Calcul de l'éclairement d'un objet selon Cook & Torrance
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
// Certaines fonctions utilisées dans ce jalon ont été définies au jalon 2 ou dans les fonctions générales.

float RAND_CONST=0.;

// ======================================================================
// Génère un nombre aléatoire entre 0 et 1
float rand()
{
	vec2 co=(invRotMatrix*gl_FragCoord).xy;
	co+=RAND_CONST;
	RAND_CONST+=.01;
	return fract(sin(dot(co,vec2(12.9898,78.233)))*43758.5453);
}

// ======================================================================
// Retourne la normale d'une microfacette, calculée selon une pdf utilisant Beckmann en fonction de distribution
// paramètre sigma : la rugosité
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

// ======================================================================
// Effectue une rotation locale de m selon la normale et retourne le résultat
// paramètre normal : la normale de la macrosurface
// paramètre m : la normale de la microfacette (résultat de computeNormal)
vec3 rotateNormal(vec3 normal, vec3 m)
{
	// Calcul de la matrice de rotation locale
	vec3 i0=vec3(1,0,0);
	if(dot(i0,normal)>.8)
	{
		i0=vec3(0,1,0);
	}
	vec3 j=cross(i0,normal);
	vec3 i1=cross(j,normal);
	mat3 Mrl=mat3(i1,j,normal);
	
	return normalize(Mrl*m);
}

// ======================================================================
// Calcul de l'éclairement d'un objet avec l'échantillonnage d'importance
// paramètre pos : position, dans le repère de la caméra, du fragment traité
// paramètre normal : normal de la macrosurface
// paramètre invRotMatrix : matrice de rotation inverse
// paramètre ni : indice de réfraction de l'objet
// paramètre sigma : rugosité de l'objet
vec4 echantillonnagePasOpti(vec3 pos,vec3 normal,mat4 invRotMatrix,float ni,float sigma)
{
	vec3 Lo=vec3(0.);
	int nbSample = 0;

	for(int k=0;k<100;k++) // 100 échantillons au maximum
	{
		if(k>=uNbSamples) // Si le numéro de l'échantillon est supérieur ou égal au nombre passé en uniform, on sort de la boucle
			break;
		
		vec3 Vo=normalize(-pos); // vecteur observateur
		vec3 m=computeNormal(sigma); // normal de la microfacette

		m = rotateNormal(normal, m);

		vec3 i=reflect(-Vo,m); // rayon incident (réfléchi)

		float nDotm=ddot(normal,m);
		float iDotn = ddot(i,normal);
		float oDotn = ddot(Vo,normal);

		// Pour éviter les divisions par 0, on vérifie les produits scalaires susceptibles de les créer.
		if(nDotm == 0.0 || iDotn == 0.0 || oDotn == 0.0)
			continue;
		
		//Calcul de la brdf à partir des fonctions F, D et G
		float F=fresnelFactor(i,m,ni);
		float D=beckmann(normal,m,sigma);
		float G=g(normal,m,i,Vo);
		float pdf=D*nDotm;
		float brdf=(F*D*G)/(4.*iDotn*oDotn);

		vec3 Li=textureCube(uSampler,adaptDir(invRotMatrix*vec4(i,1.))).rgb;
		Lo+=Li*brdf*iDotn/pdf;
		nbSample+=1;
	}
	return vec4(Lo/float(nbSample),1.);
}

// ======================================================================
// Fonction calculant la distribution de Beckmann de manière "optimisée"
// paramètre nDotm : produit scalaire entre n et m
// paramètre sigma : rugosité de l'objet
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

// ======================================================================
// Fonction calculant l'ombrage et le Masquage de manière "optimisée"
// paramètre nDotm : produit scalaire entre n et m
// paramètre iDotn : produit scalaire entre i et n
// paramètre oDotn : produit scalaire entre o et n
// paramètre oDotm : produit scalaire entre o et m
// paramètre iDotm : produit scalaire entre i et m
float gOpti(float nDotm,float iDotn,float oDotn,float oDotm,float iDotm)
{
	float nDotm2 = nDotm * 2.0;
	return min(1.,min(nDotm2 * oDotn / oDotm, nDotm2 * iDotn / iDotm));
}

// ======================================================================
// Calcul de l'éclairement d'un objet avec l'échantillonnage d'importance de manière "optimisée"
// paramètre pos : position, dans le repère de la caméra, du fragment traité
// paramètre normal : normal de la macrosurface
// paramètre invRotMatrix : matrice de rotation inverse
// paramètre ni : indice de réfraction de l'objet
// paramètre sigma : rugosité de l'objet
vec4 echantillonnage(vec3 pos,vec3 normal,mat4 invRotMatrix,float ni,float sigma)
{
	vec3 Lo=vec3(0.);
	int nbSample = 0;

	for(int k=0;k<100;k++) // 100 échantillons au maximum
	{
		if(k>=uNbSamples) // Si le numéro de l'échantillon est supérieur ou égal au nombre passé en uniform, on sort de la boucle
			break;
		
		vec3 Vo=normalize(-pos); // vecteur observateur
		vec3 m=computeNormal(sigma); // normal de la microfacette
		m = rotateNormal(normal, m);

		vec3 i=reflect(-Vo,m); // rayon incident (réfléchi)

		float nDotm = ddot(normal,m);
		float iDotn = ddot(i,normal);
		float oDotn = ddot(Vo,normal);
		float iDotm = ddot(i,m);
		float oDotm = ddot(Vo,m);
		// Pour éviter les divisions par 0, on vérifie les produits scalaires susceptibles de les créer.
		if(nDotm == 0.0 || iDotn == 0.0 || oDotn == 0.0 || iDotm == 0.0 || oDotm == 0.0)
			continue;
		
		// Calcul de la brdf a partir des fonctions F et G
		// D a été supprimé des calculs de pdf et brdf par simplification
		float F=fresnelFactor(i,m,ni);
		float G=gOpti(nDotm,iDotn, oDotn, oDotm, iDotm);
		float pdf=nDotm;
		float brdf=(F*G)/(4.*iDotn*oDotn);

		vec3 Li=textureCube(uSampler,adaptDir(invRotMatrix*vec4(i,1.))).rgb;
		Lo+=Li*brdf*iDotn/pdf;
		nbSample+=1;
	}
	return vec4(Lo/float(nbSample),1.);
}


// ======================================================================
// Simule un miroir dépoli en considérant les microfacettes comme des miroirs, plus ou moins dépoli en fonction de sigma 
// paramètre pos : position, dans le repère de la caméra, du fragment traité
// paramètre normal : normal de la macrosurface
// paramètre invRotMatrix : matrice de rotation inverse
// paramètre sigma : rugosité de l'objet
vec4 miroirDepoli(vec3 pos,vec3 normal,mat4 invRotMatrix,float sigma)
{
	vec3 Lo=vec3(0.);
	int nbSample = 0;
	
	for(int k=0;k<100;k++)// 100 échantillons au maximum
	{
		if(k>=uNbSamples) // Si le numéro de l'échantillon est supérieur ou égal au nombre passé en uniform, on sort de la boucle
			break;

		vec3 Vo=normalize(-pos); // vecteur observateur
		vec3 m=computeNormal(sigma); // normale de la microfacette
		m = rotateNormal(normal, m);

		vec3 i=reflect(-Vo,m); // rayon incident (réfléchi)
		
		vec3 Li=textureCube(uSampler,adaptDir(invRotMatrix*vec4(i,1.))).rgb;
		Lo += Li;
		nbSample+=1;
	}
	return vec4(Lo/float(nbSample),1.);
}

// ======================================================================
// Jalon BONUS : WalterGGX
// ======================================================================

// ======================================================================
// Retourne la normale d'une microfacette, calculée selon une pdf utilisant WalterGGX en fonction de distribution
// paramètre sigma : la rugosité
vec3 computeNormalWalterGGX(float sigma)
{
    float ksi1 = rand();
    float numT = sigma * sqrt(ksi1);
    float denumT = sqrt(1. - ksi1);
	float theta=atan(numT/denumT);
	float phi=rand()*2.*PI;
	vec3 m=vec3(0.);
	m.x=sin(theta)*cos(phi);
	m.y=sin(theta)*sin(phi);
	m.z=cos(theta);
	return m;
}

// ======================================================================
// Calcule la distribution de WalterGGX
// paramètre nDotm : produit scalaire entre n et m
// paramètre sigma : rugosité de l'objet
float dWalterGGX(float nDotm, float sigma){
    float sigma2 = sigma * sigma;
    float cosTv2 = nDotm * nDotm;
    float sinTv2 = 1. - cosTv2;
    float tanTv2 = sinTv2/cosTv2;
    float denominateur = PI * cosTv2 * cosTv2 * square(sigma2 + tanTv2);
    return sigma2 / denominateur;
}

// ======================================================================
// Fonction g1 aidant au calcul de l'ombrage et du masquage de gWalterGGX
// paramètre vDotn : produit scalaire entre v et n
// paramètre vDotm : produit scalaire entre v et m
// paramètre sigma : rugosité de l'objet
float g1WalterGGX(float vDotn, float vDotm, float sigma) {
    float sign = max(0., vDotm / vDotn);
	if(sign != 0.)
	{
		sign = 1.;
	}
    float cosTv2 = vDotn * vDotn;
    float sinTv2 = 1. - cosTv2;
    float tanTv2 = sinTv2/cosTv2;
    float denom = 1. + sqrt(1. + square(sigma) * tanTv2);
	return sign * (2. / denom);
}

// ======================================================================
// Fonction calculant l'ombrage et le Masquage de WalterGGX
// paramètre iDotn : produit scalaire entre i et n
// paramètre oDotn : produit scalaire entre o et n
// paramètre oDotm : produit scalaire entre o et m
// paramètre iDotm : produit scalaire entre i et m
// paramètre sigma : rugosité de l'objet
float gWalterGGX(float iDotn, float iDotm, float oDotn, float oDotm, float sigma){
    float g1IM = g1WalterGGX(iDotn, iDotm, sigma);
    float g1OM = g1WalterGGX(oDotn, oDotm, sigma);
    return g1OM * g1IM;
}

// ======================================================================
// Calcul de l'éclairement d'un objet avec l'échantillonnage d'importance
// Utilise la distribution et le masquage de WalterGGX
// paramètre pos : position, dans le repère de la caméra, du fragment traité
// paramètre normal : normal de la macrosurface
// paramètre invRotMatrix : matrice de rotation inverse
// paramètre ni : indice de réfraction de l'objet
// paramètre sigma : rugosité de l'objet
vec4 walterGGXBRDF(vec3 pos,vec3 normal,mat4 invRotMatrix,float ni,float sigma)
{
    vec3 Lo=vec3(0.);
	int nbSample = 0;
	
	for(int k=0;k<100;k++) // 100 échantillons au maximum
	{		
		if(k>=uNbSamples) // Si le numéro de l'échantillon est supérieur ou égal au nombre passé en uniform, on sort de la boucle
			break;
		
		vec3 Vo=normalize(-pos); // vecteur observateur
		vec3 m=computeNormalWalterGGX(sigma); // normale de la microfacette
		m = rotateNormal(normal, m);

		vec3 i=reflect(-Vo,m); // rayon incident (réfléchi)

		float nDotm = ddot(normal,m);
		float iDotn = ddot(i,normal);
		float oDotn = ddot(Vo,normal);
		float iDotm = ddot(i,m);
		float oDotm = ddot(Vo,m);
		// Pour éviter les divisions par 0, on vérifie les produits scalaires susceptibles de les créer.
		if(nDotm == 0.0 || iDotn == 0.0 || oDotn == 0.0)
			continue;
		
		// Calcul de la brdf a partir des fonctions F et G
		// D a été supprimé des calculs de pdf et brdf par simplification
		float F=fresnelFactor(i,m,ni);
		float G=gWalterGGX(iDotn, iDotm, oDotn, oDotm, sigma);
		float pdf=nDotm;
		float brdf=(F*G)/(4.*iDotn*oDotn);

		vec3 Li=textureCube(uSampler,adaptDir(invRotMatrix*vec4(i,1.))).rgb;
		Lo+=Li*brdf*iDotn/pdf;
		nbSample+=1;
	}
	return vec4(Lo/float(nbSample),1.);
}

//=======================================================================
// Calcul de la transparence selon Walter avec échantillonnage d'importance
// Utilise la distribution et le masquage de WalterGGX
// Le résultat obtenu n'est pas satisfaisant car la transparence est très foncée, comme si on avait une perte d'énergie.
// paramètre pos : position, dans le repère de la caméra, du fragment traité
// paramètre normal : normal de la macrosurface
// paramètre invRotMatrix : matrice de rotation inverse
// paramètre ni : indice de réfraction de l'objet
// paramètre sigma : rugosité de l'objet
vec4 walterGGXBSDF(vec3 pos,vec3 normal,mat4 invRotMatrix,float ni,float sigma)
{
    vec3 Lo=vec3(0.);
	int nbSample = 0;
	
	for(int k=0;k<100;k++) // 100 échantillons au maximum
	{		
		if(k>=uNbSamples) // Si le numéro de l'échantillon est supérieur ou égal au nombre passé en uniform, on sort de la boucle
			break;
		
		vec3 Vo=normalize(-pos); // vecteur observateur
		vec3 m=computeNormalWalterGGX(sigma); // normale de la microfacette
		m = rotateNormal(normal, m);

		vec3 i=reflect(-Vo,m); // rayon incident (réfléchi)
		vec3 t=refract(-Vo,m,AIR_REFRACT_INDEX/ni); // rayon réfracté/transmis

		float nDotm = ddot(normal,m);
		float iDotn = ddot(i,normal);
		float oDotn = ddot(Vo,normal);
		float iDotm = ddot(i,m);
		float oDotm = ddot(Vo,m);
		float tDotn = ddot(t,-normal);
		float tDotm = ddot(t,-m);
		// Pour éviter les divisions par 0, on vérifie les produits scalaires susceptibles de les créer.
		if(nDotm == 0.0 || iDotn == 0.0 || oDotn == 0.0 || tDotn == 0.0)
			continue;
		
		// Calcul de la brdf a partir des fonctions F, D et G
		// D a été supprimé des calculs de pdf, brdf et btdf par simplification
		float F=fresnelFactor(i,m,ni);
		float G=gWalterGGX(iDotn, iDotm, oDotn, oDotm, sigma);
		float pdf=nDotm;
		float brdf=(F*G)/(4.*iDotn*oDotn);

		// Eta_o n'est pas pris en compte car considéré à 1, l'indice de réfraction de l'air
		float G_t = gWalterGGX(tDotn, tDotm, oDotn, oDotm, sigma); // calcul du masquage pour le rayon réfracté
		float btdf = (tDotm * oDotm)/(tDotn * oDotn); // calcul de la première partie de la btdf
		btdf *= (1.0 - F) * G_t; // seconde partie
		btdf /= (square(ni*tDotm+oDotm)); // troisieme partie
		
		vec3 Li=textureCube(uSampler,adaptDir(invRotMatrix*vec4(i,1.))).rgb; // "texture réfléchie"
		vec3 Lt=textureCube(uSampler,adaptDir(invRotMatrix*vec4(t,1.))).rgb; // "texture réfractée"
		Lo+=(brdf*Li*iDotn+btdf*Lt*tDotn)/pdf;
		nbSample +=1;
	}
	return vec4(Lo/float(nbSample),1.);
}


//=======================================================================
// Calcul de la "transparence dépolie"
// paramètre pos : position, dans le repère de la caméra, du fragment traité
// paramètre normal : normal de la macrosurface
// paramètre invRotMatrix : matrice de rotation inverse
// paramètre ni : indice de réfraction de l'objet
// paramètre sigma : rugosité de l'objet

vec4 transparenceDepolie(vec3 pos,vec3 normal,mat4 invRotMatrix,float ni,float sigma)
{
    vec3 Lo=vec3(0.);
	int nbSample = 0;
	
	for(int k=0;k<100;k++) // 100 échantillons au maximum
	{
		if(k>=uNbSamples) // Si le numéro de l'échantillon est supérieur ou égal au nombre passé en uniform, on sort de la boucle
			break;
		
		vec3 Vo=normalize(-pos); // vecteur observateur
		vec3 m=computeNormalWalterGGX(sigma); // normale de la microfacette
		m = rotateNormal(normal, m);

		vec3 t=refract(-Vo,m,AIR_REFRACT_INDEX/ni); // rayon réfracté/transmis

		vec3 Lt=textureCube(uSampler,adaptDir(invRotMatrix*vec4(t,1.))).rgb;
		Lo+=Lt;
		nbSample+=1;
	}
	return vec4(Lo/float(nbSample),1.);
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
    else if(uShaderState==MIROIRDEPOLI)
	{
        col=miroirDepoli(pos3D.xyz,normalize(N),invRotMatrix,uSigma);
    }
    else if(uShaderState==WALTERGGXBRDF)
	{
        col=walterGGXBRDF(pos3D.xyz,normalize(N),invRotMatrix,uRefractIndex,uSigma);
    }
    else if(uShaderState==WALTERGGXBSDF)
	{
        col=walterGGXBSDF(pos3D.xyz,normalize(N),invRotMatrix,uRefractIndex,uSigma);
    }
	else if(uShaderState==TRANSPARENCEDEPOLIE)
	{
		col=transparenceDepolie(pos3D.xyz,normalize(N),invRotMatrix,uRefractIndex,uSigma);
	}
	else
	{
		vec3 color=uKd*dot(normalize(N),normalize(vec3(-pos3D)));// Lambert rendering, eye light source
		col=vec4(color,1.);
	}
	gl_FragColor=col;
}
