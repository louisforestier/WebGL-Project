
precision mediump float;

varying vec3 TexCoords;
uniform samplerCube uSampler;
// ==============================================
void main(void)
{
	vec3 col = vec3(1.0,0.0,0.0) ; 
	gl_FragColor = textureCube(uSampler, normalize(TexCoords));
}




