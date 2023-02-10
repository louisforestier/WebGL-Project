// =====================================================
var gl;

// =====================================================
var mvMatrix = mat4.create();
var pMatrix = mat4.create();
var rotMatrix = mat4.create();
var distCENTER;
// =====================================================

var USHORT_MAX = 65535;
var DRAWPLANE = true;
var DRAWLIGHT = false;
var OBJ1 = null;
var PLANE = null;
var CUBEMAP = null;
var NBSAMPLES = 1;
var LIGHT= {};

/**
 * Enumeration pour décrire le calcul à appliquer dans la shader obj.fs
 */
const ShaderState = {
	Reflect:0,
	Refract:1,
	Fresnel:2,
	Color:3,
	CookTorrance:4,
	Echantillonnage:5,
    MiroirDepoli:6,
    WalterGGX:7
};

// =====================================================
// CUBEMAP
// =====================================================
/**
 * classe permettant d'afficher un cube texturé
 */
class cubemap {

	/**
	 * Constructeur.
	 * @param {string} name nom de la skybox à charger
	 */
	constructor(name) {
		this.shaderName='cubemap';
		this.loaded=-1;
		this.shader = null;
		this.texture = 0;
		this.skyboxName = name;
		this.initAll();
	}

	/**
	 * intialise les buffers de positions et d'indices de sommets, la shader et la texture
	 */
	initAll() {
		var size=20.0;
		var vertices = [
			-size/2,-size/2 ,-size/2, 
			-size/2,-size/2 ,size/2, 
			size/2,-size/2 ,size/2, 
			size/2,-size/2,-size/2,
			-size/2,size/2 ,-size/2, 
			-size/2,size/2 ,size/2, 
			size/2,size/2 ,size/2, 
			size/2,size/2 ,-size/2
		];

		var indices = [
			0,1,2,
			0,2,3,
			0,4,1,
			4,5,1,
			0,3,7,
			7,4,0,
			1,5,6,
			6,2,1,
			3,2,6,
			6,7,3,
			7,5,4,
			7,6,5
		];

		// création du buffer des sommets sur gpu
		this.vertexBuffer = gl.createBuffer();
		gl.bindBuffer(gl.ARRAY_BUFFER, this.vertexBuffer);
		gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(vertices), gl.STATIC_DRAW);
		this.vertexBuffer.itemSize = 3;
		this.vertexBuffer.numItems = vertices.length / 3;

		// création du buffer des indices sur gpu
		this.indexBuffer = gl.createBuffer();
		gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, this.indexBuffer);
		gl.bufferData(gl.ELEMENT_ARRAY_BUFFER, new Uint16Array(indices), gl.STATIC_DRAW);
		this.indexBuffer.itemSize = 1;
		this.indexBuffer.numItems = indices.length;
	
		// chargement de la shader cubemap
		loadShaders(this);

		// charge la texture de la cubemap
		this.setTexture(this.skyboxName);
	}

	/**
	 * change la texture courante de la cubemap
	 * supprime l'ancienne
	 * @param {string} skyboxName nom de la skybox à charger 
	 */
	setTexture(skyboxName)
	{
		if( skyboxName != null)
		{
			if(this.texture != 0)
				gl.deleteTexture(this.texture);
			this.skyboxName = skyboxName;
			this.initTextures();
		}
	}

	/**
	 * crée une texture pour cubemap et crée une callback pour la charger sur gpu
	 */
	initTextures()
	{
		this.texture = gl.createTexture();
		gl.bindTexture(gl.TEXTURE_CUBE_MAP, this.texture);
		// crée un tableau contenant la face selon les axes x,y,z et l'image source correspondante
		var cubemapInfo =[
			{target:gl.TEXTURE_CUBE_MAP_POSITIVE_X, src:"./skyboxes/" + this.skyboxName + "/right.jpg"} ,
			{target:gl.TEXTURE_CUBE_MAP_NEGATIVE_X, src:"./skyboxes/" + this.skyboxName + "/left.jpg"} ,
			{target:gl.TEXTURE_CUBE_MAP_POSITIVE_Y, src:"./skyboxes/" + this.skyboxName + "/top.jpg"},
			{target:gl.TEXTURE_CUBE_MAP_NEGATIVE_Y, src:"./skyboxes/" + this.skyboxName + "/bottom.jpg"},
			{target:gl.TEXTURE_CUBE_MAP_POSITIVE_Z, src:"./skyboxes/" + this.skyboxName + "/front.jpg"},
			{target:gl.TEXTURE_CUBE_MAP_NEGATIVE_Z, src:"./skyboxes/" + this.skyboxName + "/back.jpg"} 
		];

		var cubemap_image = [];

		var self = this;
		// pour chaque image composant la cubemap, on crée une texture 2d avec la face et l'image source correspondante
		for (let i = 0; i < cubemapInfo.length; i++) {
			const {target,src} = cubemapInfo[i];
			cubemap_image[i] = new Image();
			cubemap_image[i].src = cubemapInfo[i].src;
			cubemap_image[i].onload = function () {
				gl.bindTexture(gl.TEXTURE_CUBE_MAP, self.texture);
				gl.texImage2D(target, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, cubemap_image[i]);
			}	
		}
		gl.texParameteri(gl.TEXTURE_CUBE_MAP, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
		gl.texParameteri(gl.TEXTURE_CUBE_MAP, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
		gl.texParameteri(gl.TEXTURE_CUBE_MAP, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
		gl.texParameteri(gl.TEXTURE_CUBE_MAP, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
		gl.texParameteri(gl.TEXTURE_CUBE_MAP, gl.TEXTURE_WRAP_R, gl.CLAMP_TO_EDGE);
	}

	/**
	 * bind la shader de l'objet et sa texture et récupère les handles des attributs et des uniforms
	 */
	setShadersParams() {
		gl.useProgram(this.shader);
		gl.bindTexture(gl.TEXTURE_CUBE_MAP, this.texture);
		this.shader.vAttrib = gl.getAttribLocation(this.shader, "aVertexPosition");
		gl.enableVertexAttribArray(this.shader.vAttrib);
		this.shader.mvMatrixUniform = gl.getUniformLocation(this.shader, "uMVMatrix");
		this.shader.pMatrixUniform = gl.getUniformLocation(this.shader, "uPMatrix");
		gl.bindBuffer(gl.ARRAY_BUFFER, this.vertexBuffer);
		gl.vertexAttribPointer(this.shader.vAttrib, this.vertexBuffer.itemSize, gl.FLOAT, false, 0, 0);
	}
	
	/**
	 * calcul et envoie les matrices de rotation, modelview et projection
	 */
	setMatrixUniforms() {
		mat4.identity(mvMatrix);
		mat4.multiply(mvMatrix, rotMatrix);
		gl.uniformMatrix4fv(this.shader.rMatrixUniform, false, rotMatrix);
		gl.uniformMatrix4fv(this.shader.mvMatrixUniform, false, mvMatrix);
		gl.uniformMatrix4fv(this.shader.pMatrixUniform, false, pMatrix);
	}

	/**
	 * affiche l'objet après avoir envoyé les variables à la shader
	 */
	draw() {
		if(this.shader && this.loaded==4) {
			this.setShadersParams();
			this.setMatrixUniforms();
			gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, this.indexBuffer);
			gl.drawElements(gl.TRIANGLES, this.indexBuffer.numItems, gl.UNSIGNED_SHORT, 0);
		}
	}
}

// =====================================================
// OBJET 3D, lecture fichier obj
// =====================================================

/**
 * classe permettant d'afficher un maillage à partir d'un fichier obj
 */
class objmesh {

	/**
	 * Constructeur.
	 * initialise la cubemap utilisé pour la réflexion et la réfraction à lake
	 * initialise l'indice de réfraction à 1.52 (indice du verre)
	 * initialise la rugosité à 0.1
	 * initialise le calcul effectué dans la shader à fresnel
	 * initialise la couleur à (0.8,0.4,0.4)
	 * charge la mesh, la shader et la texture
	 * @param {string} objFname nom de l'obj à charger
	 */
	constructor(objFname) {
		this.objName = objFname;
		this.shaderName = 'obj';
		this.skyboxName = 'lake';
		this.loaded = -1;
		this.shader = null;
		this.mesh = null;
		this.refractIndex = 1.52;
		this.rugosity = 0.1;
		this.lightIntensity = 1.0;
		this.shaderState = ShaderState.WalterGGX;
		this.texture = 0;
		this.color = [0.8,0.4,0.4];
		this.initAll();
	}

	/**
	 * charge la mesh, la shader et la texture
	 */
	initAll() {
		loadObjFile(this);
		loadShaders(this);
		this.setTexture(this.skyboxName);
	}

	/**
	 * change la texture courante de la cubemap utilisée pour la réflexion et réfraction
	 * supprime l'ancienne
	 * @param {string} textureName nom de la texture 
	 */
	setTexture(textureName)
	{
		if( textureName != null)
		{
			if(this.texture != 0)
				gl.deleteTexture(this.texture);
			this.skyboxName = textureName;
			this.initTextures();
		}
	}

	/**
	 * modifie la couleur diffuse de l'objet
	 * @param {[float, float, float]} color couleur de l'objet composé de trois flottants entre 0 et 1
	 */
	setColor(color){
		if(color != null){
			this.color = color;
		}
	}

	/**
	 * crée une texture et crée une callback pour la charger sur gpu
	 */
	initTextures()
	{
		this.texture = gl.createTexture();
		gl.bindTexture(gl.TEXTURE_CUBE_MAP, this.texture);
		// crée un tableau contenant la face selon les axes x,y,z et l'image source correspondante
		var cubemapInfo =[
			{target:gl.TEXTURE_CUBE_MAP_POSITIVE_X, src:"./skyboxes/" + this.skyboxName + "/right.jpg"} ,
			{target:gl.TEXTURE_CUBE_MAP_NEGATIVE_X, src:"./skyboxes/" + this.skyboxName + "/left.jpg"} ,
			{target:gl.TEXTURE_CUBE_MAP_POSITIVE_Y, src:"./skyboxes/" + this.skyboxName + "/top.jpg"},
			{target:gl.TEXTURE_CUBE_MAP_NEGATIVE_Y, src:"./skyboxes/" + this.skyboxName + "/bottom.jpg"},
			{target:gl.TEXTURE_CUBE_MAP_POSITIVE_Z, src:"./skyboxes/" + this.skyboxName + "/front.jpg"},
			{target:gl.TEXTURE_CUBE_MAP_NEGATIVE_Z, src:"./skyboxes/" + this.skyboxName + "/back.jpg"} 
		];

		var cubemap_image = [];

		var self = this;
		// pour chaque image composant la cubemap, on crée une texture 2d avec la face et l'image source correspondante
		for (let i = 0; i < cubemapInfo.length; i++) {
			const {target,src} = cubemapInfo[i];
			cubemap_image[i] = new Image();
			cubemap_image[i].src = cubemapInfo[i].src;
			cubemap_image[i].onload = function () {
				gl.bindTexture(gl.TEXTURE_CUBE_MAP, self.texture);
				gl.texImage2D(target, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, cubemap_image[i]);
			}	
		}
		gl.texParameteri(gl.TEXTURE_CUBE_MAP, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
		gl.texParameteri(gl.TEXTURE_CUBE_MAP, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
		gl.texParameteri(gl.TEXTURE_CUBE_MAP, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
		gl.texParameteri(gl.TEXTURE_CUBE_MAP, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
		gl.texParameteri(gl.TEXTURE_CUBE_MAP, gl.TEXTURE_WRAP_R, gl.CLAMP_TO_EDGE);
	}

	/**
	 * bind la shader de l'objet et sa texture et récupère les handles des attributs et des uniforms
	 */
	setShadersParams() {
		gl.useProgram(this.shader);
		this.shader.vAttrib = gl.getAttribLocation(this.shader, "aVertexPosition");
		gl.enableVertexAttribArray(this.shader.vAttrib);
		gl.bindBuffer(gl.ARRAY_BUFFER, this.mesh.vertexBuffer);
		gl.vertexAttribPointer(this.shader.vAttrib, this.mesh.vertexBuffer.itemSize, gl.FLOAT, false, 0, 0);

		this.shader.nAttrib = gl.getAttribLocation(this.shader, "aVertexNormal");
		gl.enableVertexAttribArray(this.shader.nAttrib);
		gl.bindBuffer(gl.ARRAY_BUFFER, this.mesh.normalBuffer);
		gl.vertexAttribPointer(this.shader.nAttrib, this.mesh.vertexBuffer.itemSize, gl.FLOAT, false, 0, 0);
		
		gl.bindTexture(gl.TEXTURE_CUBE_MAP, this.texture);
		this.shader.refractIndex = gl.getUniformLocation(this.shader, "uRefractIndex");
		gl.uniform1f(this.shader.refractIndex,this.refractIndex);
		this.shader.sigma = gl.getUniformLocation(this.shader, "uSigma");
		gl.uniform1f(this.shader.sigma,this.rugosity);
		this.shader.lightIntensity = gl.getUniformLocation(this.shader, "uLightIntensity");
		gl.uniform1f(this.shader.lightIntensity, LIGHT.lightIntensity);
		this.shader.shaderState = gl.getUniformLocation(this.shader, "uShaderState");		
		gl.uniform1i(this.shader.shaderState,this.shaderState);
		this.shader.nbSamples = gl.getUniformLocation(this.shader, "uNbSamples");		
		gl.uniform1i(this.shader.nbSamples,NBSAMPLES);
		this.shader.Kd = gl.getUniformLocation(this.shader, "uKd");
		gl.uniform3f(this.shader.Kd, this.color[0], this.color[1], this.color[2]);
		
		var lightpos = [0.,0.,0.];
		if(LIGHT.detached) {
			mat4.identity(mvMatrix);
			mat4.translate(mvMatrix, distCENTER);
			mat4.multiply(mvMatrix, rotMatrix);
			mat4.multiplyVec3(mvMatrix,LIGHT.position,lightpos)
		}
		this.shader.lightPos = gl.getUniformLocation(this.shader, "uLightPos");
		gl.uniform3f(this.shader.lightPos, lightpos[0], lightpos[1], lightpos[2]);
		this.shader.rMatrixUniform = gl.getUniformLocation(this.shader, "uRMatrix");
		this.shader.mvMatrixUniform = gl.getUniformLocation(this.shader, "uMVMatrix");
		this.shader.pMatrixUniform = gl.getUniformLocation(this.shader, "uPMatrix");
	}
	
	/**
	 * calcul et envoie les matrices de rotation, modelview et projection
	 */
	 setMatrixUniforms() {
		mat4.identity(mvMatrix);
		mat4.translate(mvMatrix, distCENTER);
		mat4.multiply(mvMatrix, rotMatrix);
		gl.uniformMatrix4fv(this.shader.rMatrixUniform, false, rotMatrix);
		gl.uniformMatrix4fv(this.shader.mvMatrixUniform, false, mvMatrix);
		gl.uniformMatrix4fv(this.shader.pMatrixUniform, false, pMatrix);
	}
	
	/**
	 * affiche l'objet après avoir envoyé les variables à la shader
	 */
	 draw() {
		if(this.shader && this.loaded==4 && this.mesh != null) {
			this.setShadersParams();
			this.setMatrixUniforms();
			gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, this.mesh.indexBuffer);
			if(this.mesh.indexBuffer.numItems > USHORT_MAX)
			{
				gl.drawElements(gl.TRIANGLES, this.mesh.indexBuffer.numItems, gl.UNSIGNED_INT, 0);
			}
			else
			{
				gl.drawElements(gl.TRIANGLES, this.mesh.indexBuffer.numItems, gl.UNSIGNED_SHORT, 0);
			}
		}
	}
}

// =====================================================
// LIGHT, lecture fichier obj
// =====================================================

/**
 * classe permettant de changer la position de la lumière de la scène, ainsi que de la faire apparaître
 */

class light {
	constructor(){
		this.objName = 'sphere.obj';
		this.shaderName = 'wire';
		this.position = [0.,0.,0.]
		this.loaded = -1;
		this.shader = null;
		this.mesh = null;
		this.lightIntensity = 1.0;
		this.detached = false;
		this.initAll();
	}

	/**
	 * charge la mesh, la shader et la texture
	 */
	initAll() {
		loadObjFile(this);
		loadShaders(this);
	}

	/**
	 * bind la shader de l'objet et sa texture et récupère les handles des attributs et des uniforms
	 */
	setShadersParams() {
		gl.useProgram(this.shader);
		this.shader.vAttrib = gl.getAttribLocation(this.shader, "aVertexPosition");
		gl.enableVertexAttribArray(this.shader.vAttrib);
		gl.bindBuffer(gl.ARRAY_BUFFER, this.mesh.vertexBuffer);
		gl.vertexAttribPointer(this.shader.vAttrib, this.mesh.vertexBuffer.itemSize, gl.FLOAT, false, 0, 0);

		this.shader.mvMatrixUniform = gl.getUniformLocation(this.shader, "uMVMatrix");
		this.shader.pMatrixUniform = gl.getUniformLocation(this.shader, "uPMatrix");
	}

	/**
	 * calcul et envoie les matrices de rotation, modelview et projection
	 */
	setMatrixUniforms() {

		mat4.identity(mvMatrix);
		mat4.translate(mvMatrix, distCENTER);
		mat4.multiply(mvMatrix, rotMatrix);

		mat4.translate(mvMatrix,this.position);
		mat4.scale(mvMatrix,[0.3,0.3,0.3]);
		gl.uniformMatrix4fv(this.shader.mvMatrixUniform, false, mvMatrix);
		gl.uniformMatrix4fv(this.shader.pMatrixUniform, false, pMatrix);
	}

	/**
	 * affiche l'objet après avoir envoyé les variables à la shader
	 */
	draw() {
		if(this.shader && this.loaded==4 && this.mesh != null) {
			this.setShadersParams();
			this.setMatrixUniforms();
			gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, this.mesh.indexBuffer);
			if(this.mesh.indexBuffer.numItems > USHORT_MAX)
			{
				gl.drawElements(gl.LINE_STRIP, this.mesh.indexBuffer.numItems, gl.UNSIGNED_INT, 0);
			}
			else
			{
				gl.drawElements(gl.LINE_STRIP, this.mesh.indexBuffer.numItems, gl.UNSIGNED_SHORT, 0);
			}
		}
	}
}

// =====================================================
// PLAN 3D, Support géométrique
// =====================================================

/**
 * classe permettant d'afficher un plan
 */
class plane {
	
	/**
	 * Constructeur.
	 */
	constructor() {
		this.shaderName='plane';
		this.loaded=-1;
		this.shader=null;
		this.initAll();
	}
		
	/**
	 * Initialise les buffers de position et de coordonnées de texture et la shader.
	 */
	initAll() {
		var size=1.0;
		var vertices = [
			-size, -size, 0,
			 size, -size, 0,
			 size, size, 0,
			-size, size, 0
		];

		var texcoords = [
			0.0,0.0,
			0.0,1.0,
			1.0,1.0,
			1.0,0.0
		];

		this.vBuffer = gl.createBuffer();
		gl.bindBuffer(gl.ARRAY_BUFFER, this.vBuffer);
		gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(vertices), gl.STATIC_DRAW);
		this.vBuffer.itemSize = 3;
		this.vBuffer.numItems = 4;

		this.tBuffer = gl.createBuffer();
		gl.bindBuffer(gl.ARRAY_BUFFER, this.tBuffer);
		gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(texcoords), gl.STATIC_DRAW);
		this.tBuffer.itemSize = 2;
		this.tBuffer.numItems = 4;

		loadShaders(this);
	}
	
	/**
	 * bind la shader de l'objet et sa texture et récupère les handles des attributs et des uniforms
	 * calcul et envoie les matrices de rotation, modelview et projection
	 */
	 setShadersParams() {
		gl.useProgram(this.shader);

		this.shader.vAttrib = gl.getAttribLocation(this.shader, "aVertexPosition");
		gl.enableVertexAttribArray(this.shader.vAttrib);
		gl.bindBuffer(gl.ARRAY_BUFFER, this.vBuffer);
		gl.vertexAttribPointer(this.shader.vAttrib, this.vBuffer.itemSize, gl.FLOAT, false, 0, 0);

		this.shader.tAttrib = gl.getAttribLocation(this.shader, "aTexCoords");
		gl.enableVertexAttribArray(this.shader.tAttrib);
		gl.bindBuffer(gl.ARRAY_BUFFER, this.tBuffer);
		gl.vertexAttribPointer(this.shader.tAttrib,this.tBuffer.itemSize, gl.FLOAT, false, 0, 0);

		this.shader.pMatrixUniform = gl.getUniformLocation(this.shader, "uPMatrix");
		this.shader.mvMatrixUniform = gl.getUniformLocation(this.shader, "uMVMatrix");

		mat4.identity(mvMatrix);
		mat4.translate(mvMatrix, distCENTER);
		mat4.multiply(mvMatrix, rotMatrix);

		gl.uniformMatrix4fv(this.shader.pMatrixUniform, false, pMatrix);
		gl.uniformMatrix4fv(this.shader.mvMatrixUniform, false, mvMatrix);
	}

	/**
	 * affiche l'objet après avoir envoyé les variables à la shader
	 */
	 draw() {
		if(this.shader && this.loaded==4) {		
			this.setShadersParams();
			
			gl.drawArrays(gl.TRIANGLE_FAN, 0, this.vBuffer.numItems);
			gl.drawArrays(gl.LINE_LOOP, 0, this.vBuffer.numItems);
		}
	}
}

// =====================================================
// FONCTIONS GENERALES, INITIALISATIONS
// =====================================================

// =====================================================
function initGL(canvas)
{
	try {
		gl = canvas.getContext("experimental-webgl");
		gl.viewportWidth = canvas.width;
		gl.viewportHeight = canvas.height;
		gl.viewport(0, 0, canvas.width, canvas.height);

		gl.clearColor(0.7, 0.7, 0.7, 1.0);
		gl.enable(gl.DEPTH_TEST);
		gl.enable(gl.CULL_FACE);
		gl.cullFace(gl.BACK); 
		// active l'extension permettant d'utiliser gl.UNSIGNED_INT dans gl.drawElements, pour afficher des modèles plus complexes
		gl.getExtension('OES_element_index_uint');
	} catch (e) {}
	if (!gl) {
		console.log("Could not initialise WebGL");
	}
}

// =====================================================
loadObjFile = function(OBJ3D)
{
	var xhttp = new XMLHttpRequest();

	xhttp.onreadystatechange = function() {
		if (xhttp.readyState == 4 && xhttp.status == 200) {
			var tmpMesh = new OBJ.Mesh(xhttp.responseText);
			OBJ.initMeshBuffers(gl,tmpMesh);
			OBJ3D.mesh=tmpMesh;
		}
	}



	xhttp.open("GET", "./objects/" + OBJ3D.objName, true);
	xhttp.send();
}

// =====================================================
function loadShaders(Obj3D) {
	loadShaderText(Obj3D,'.vs');
	loadShaderText(Obj3D,'.fs');
}

// =====================================================
function loadShaderText(Obj3D,ext) {   // lecture asynchrone...
  var xhttp = new XMLHttpRequest();
  
  xhttp.onreadystatechange = function() {
	if (xhttp.readyState == 4 && xhttp.status == 200) {
		if(ext=='.vs') { Obj3D.vsTxt = xhttp.responseText; Obj3D.loaded ++; }
		if(ext=='.fs') { Obj3D.fsTxt = xhttp.responseText; Obj3D.loaded ++; }
		if(Obj3D.loaded==2) {
			Obj3D.loaded ++;
			compileShaders(Obj3D);
			Obj3D.loaded ++;
		}
	}
  }
  
  Obj3D.loaded = 0;
  xhttp.open("GET", "./shaders/" + Obj3D.shaderName + ext, true);
  xhttp.send();
}

// =====================================================
function compileShaders(Obj3D)
{
	Obj3D.vshader = gl.createShader(gl.VERTEX_SHADER);
	gl.shaderSource(Obj3D.vshader, Obj3D.vsTxt);
	gl.compileShader(Obj3D.vshader);
	if (!gl.getShaderParameter(Obj3D.vshader, gl.COMPILE_STATUS)) {
		console.log("Vertex Shader FAILED... "+Obj3D.shaderName+".vs");
		console.log(gl.getShaderInfoLog(Obj3D.vshader));
	}

	Obj3D.fshader = gl.createShader(gl.FRAGMENT_SHADER);
	gl.shaderSource(Obj3D.fshader, Obj3D.fsTxt);
	gl.compileShader(Obj3D.fshader);
	if (!gl.getShaderParameter(Obj3D.fshader, gl.COMPILE_STATUS)) {
		console.log("Fragment Shader FAILED... "+Obj3D.shaderName+".fs");
		console.log(gl.getShaderInfoLog(Obj3D.fshader));
	}

	Obj3D.shader = gl.createProgram();
	gl.attachShader(Obj3D.shader, Obj3D.vshader);
	gl.attachShader(Obj3D.shader, Obj3D.fshader);
	gl.linkProgram(Obj3D.shader);
	if (!gl.getProgramParameter(Obj3D.shader, gl.LINK_STATUS)) {
		console.log("Could not initialise shaders");
		console.log(gl.getShaderInfoLog(Obj3D.shader));
	}
}

// =====================================================
function webGLStart() {
	var canvas = document.getElementById("WebGL-test");

	canvas.onmousedown = handleMouseDown;
	document.onmouseup = handleMouseUp;
	document.onmousemove = handleMouseMove;
	canvas.onwheel = handleMouseWheel;

	initGL(canvas);

	
	mat4.perspective(45, gl.viewportWidth / gl.viewportHeight, 0.1, 100.0, pMatrix);
	mat4.identity(rotMatrix);
	mat4.rotate(rotMatrix, rotX, [1, 0, 0]);
	mat4.rotate(rotMatrix, rotY, [0, 0, 1]);
	
	distCENTER = vec3.create([0,-0.2,-3]);
	
	LIGHT = new light();
	PLANE = new plane();
	OBJ1 = new objmesh('sphere.obj');
	CUBEMAP = new cubemap('lake');

	tick();
}

// =====================================================
function drawScene() {
	gl.clear(gl.COLOR_BUFFER_BIT);
	
	// Drawing objects
    if(DRAWPLANE){
        PLANE.draw();
    }
	OBJ1.draw();
	CUBEMAP.draw();
	if(LIGHT.detached){
		LIGHT.draw();
	}
}



