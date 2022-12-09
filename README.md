# Projet WebGL - DORET & FORESTIER <!-- omit in toc -->

**Auteurs : \
DORET Bastien \
FORESTIER Louis**

## Sommaire <!-- omit in toc -->

- [Fonctionnalités implantés](#fonctionnalités-implantés)
  - [Jalon 1](#jalon-1)
  - [Jalon 2](#jalon-2)
  - [Bonus](#bonus)
- [Problème de buffers](#problème-de-buffers)
  - [Explication de la solution](#explication-de-la-solution)
  - [Code](#code)
- [Description de l'archive](#description-de-larchive)

## Fonctionnalités implantés

Dans cette section, nous allons détailler les fonctionnalités implantées dans notre rendu, elles seront classées par jalon.

### Jalon 1

- Réflection
- Réfraction, avec possibilité de jouer sur l'index de réfraction
- Réflection, Réfraction et effet de **Frensel**, avec possibilité de jouer sur l'index de réfraction

### Jalon 2

- Ajout de l'équation de **Cook & Torrance**, avec possibilité de jouer sur la rugosité de l'objet et l'intensité de la lumière

### Bonus

- Possibilité de changer d'objet
- Possibilité de changer la skybox
- Possibilité de désactiver le plan
- Possibilité d'avoir un objet réfléchissant une skybox différente que celle de la scène
- Activation de l'extension permettant de gérer les objets avec des maillages composés de plus de 65535 sommets

## Problème de buffers

Lorsque nous avons essayé d'importer des objets avec de grands maillages, nous avons rencontré un problème. L'objet n'apparaissait pas en entier et le maillage existant était discontinu. Nous avons donc regardé dans un premier temps si l'objloader avait un problème avec de gros objets. Cependant cela ne semblait pas venir de là. Nous avons donc cherché si WebGL avait une limite de triangles qu'il pouvait afficher et c'est là que nous avons trouvé notre réponse. Nativement, l'appel de la fonction DrawElements de WebGL ne peut se faire qu'avec des buffers ayant des index en UShort. Cela signifie que DrawElements ne peut gérer nativement que des maillages ayant un nombre de point inférieur ou égal à 65536.

### Explication de la solution

Pour outrepasser ce problème, il y a deux solutions. La première consiste à activer une extension de WebGL, permettant d'utiliser des index de buffer en UInt, repoussant ainsi la limite de 65536 sommets à 2^32 sommets. La seconde, quant à elle, consiste à découper le maillage en plusieurs buffers, ayant une taille maximale de 65536. Nous avons décidé de mettre en place la première solution, qui nous a permis d'importer de gros objets, comme l'Armadillo ou le XYZ Dragon.

Cependant l'utilisation de UInt sur des ordinateurs peu puissants pourrait causer des ralentissements, nous choissons donc la taille d'indexation des buffers en adéquation avec la taille du maillage de l'objet lu.

### Code

Pour le glCourseBasis, les changements se résument à deux fonctions :
```js
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
```
```js
/**
* affiche l'objet après avoir envoyé les variables au shader
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
```
Pour l'obj loader, la modification se résume à la création d'un buffer :
```js
mesh.indexBuffer = gl.createBuffer();
gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, mesh.indexBuffer);
if (mesh.indices.length > 65535) 
{
    gl.bufferData(gl.ELEMENT_ARRAY_BUFFER, new Uint32Array(mesh.indices), gl.STATIC_DRAW);
} 
else 
{
    gl.bufferData(gl.ELEMENT_ARRAY_BUFFER, new Uint16Array(mesh.indices), gl.STATIC_DRAW);      
}
mesh.indexBuffer.itemSize = 1;
mesh.indexBuffer.numItems = mesh.indices.length;
```

## Description de l'archive

- *WebGL-Projet*
  - *objects*
    - **bunny.obj** : objet représentant un lapin
    - **duck.obj** : objet représentant un canard
    - **mustang.obj** : objet représentant une Ford Mustang Shelby
    - **porsche.obj** : objet représentant une Porsche
    - **sphere.obj** : objet représentant une sphère
  - *shaders*
    - **cubemap.fs** : fragment shader pour les cubemaps
    - **cubemap.vs** : vertex shader pour les cubemaps
    - **obj.fs** : fragment shader pour les objets
    - **obj.vs** : vertex shader pour les objets
    - **plane.fs** : fragment shader pour les plans
    - **plane.vs** : vertex shader pour les plans
    - **wire.fs** : fragment shader pour afficher les arêtes des triangles d'un objet
    - **wire.vs** : vertex shader pour afficher les arêtes des triangles d'un objet
  - *skyboxes*
    - *blue_space* : dossier contenant les textures de la skybox du même nom
    - *lake* : dossier contenant les textures de la skybox du même nom
    - *red_space* : dossier contenant les textures de la skybox du même nom
    - *room* : dossier contenant les textures de la skybox du même nom
    - *snowy_mountain* : dossier contenant les textures de la skybox du même nom
    - *test* : dossier contenant les textures de la skybox du même nom
  - *src*
    - **callbacks.js** : Contient toutes les fonctions interagissant avec la fenêtre WebGL
    - **front.js** : Contient toutes les fonctions interagissant avec la page web en elle même
    - **glCourseBasis.js** : Contient toutes les fonctions permettant de mettre en place l'environnement WebGL
    - **glMatrix.js** : Contient toutes les fonctions permettant d'utiliser des matrices
    - **objLoader.js** : Contient toutes les fonctions permettant de créer une Mesh a partir d'un fichier **.obj**
  - **main.css** : le fichier css de la page Web
  - **main.html** : Page web contenant la fenêtre WebGL

Pour les skyboxes, elles sont toutes formalisées de la même manière, elles contiennent toutes 6 images réparties de la sorte :
- pos-x => right
- neg-x => left
- pos-y  => top
- neg-y => bottom
- pos-z => front
- neg-z => back