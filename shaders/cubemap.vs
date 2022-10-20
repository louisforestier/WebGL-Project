attribute vec3 aVertexPosition;

uniform mat4 uMVMatrix;
uniform mat4 uPMatrix;

varying vec3 dir;

void main(void) {
	dir = aVertexPosition,1.0;
	gl_Position = uPMatrix * uMVMatrix * vec4(aVertexPosition,1.0);
}
