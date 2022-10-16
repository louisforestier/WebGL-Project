
precision mediump float;


varying vec4 pos3D;
varying vec3 N;

uniform samplerCube uSampler;



// ==============================================
void main(void)
{
	vec3 col = vec3(0.8,0.4,0.4) * dot(N,normalize(vec3(-pos3D))); // Lambert rendering, eye light source
	vec3 I = normalize(pos3D.xyz);
	vec3 R = refract(I,normalize(N),1.0);

	gl_FragColor = vec4(textureCube(uSampler,R).rgb,1.0);
}




/*
precision mediump float;

varying vec4 pos3D;
varying vec3 N;

uniform samplerCube uSampler;


// ==============================================
void main(void)
{
	vec3 I = normalize(pos3D.xyz);
	vec3 R = refract(I,normalize(N),1.0);
	//vec3 col = vec3(0.8,0.4,0.4) * dot(N,normalize(vec3(-pos3D))); // Lambert rendering, eye light source
	//gl_FragColor = vec4(col,1.0);
	gl_FragColor = vec4(R,1.0);
	//gl_FragColor = vec4(textureCube(uSampler,R).rgb,1.0);
}




*/