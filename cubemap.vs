attribute vec3 aVertexPosition;

uniform mat4 uMVMatrix;
uniform mat4 uPMatrix;

varying vec3 TexCoords;

void main(void) {
	TexCoords = (vec4(aVertexPosition,1.0)).xyz;
	gl_Position = uPMatrix * uMVMatrix * vec4(aVertexPosition,1.0);
}
