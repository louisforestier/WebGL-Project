
precision mediump float;


varying vec4 pos3D;
varying vec3 N;

uniform samplerCube uSampler;

vec3 adaptDir(vec3 dir)
{
	return vec3(dir.x,dir.z,dir.y);
}

// ==============================================
void main(void)
{
	//vec3 col = vec3(0.8,0.4,0.4) * dot(N,normalize(vec3(-pos3D))); // Lambert rendering, eye light source
	vec3 I = normalize(adaptDir(pos3D.xyz/pos3D.w));
	vec3 R = reflect(I,normalize(N));

	gl_FragColor = vec4(textureCube(uSampler,R).rgb,1.0);
}
