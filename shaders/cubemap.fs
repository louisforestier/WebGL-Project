
precision mediump float;

varying vec3 dir;
uniform samplerCube uSampler;

vec3 adaptDir(vec3 dir)
{
	return vec3(dir.x,dir.z,dir.y);
}



// ==============================================
void main(void)
{
	gl_FragColor = textureCube(uSampler, normalize(adaptDir(dir)));
}




