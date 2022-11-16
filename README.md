# Projet WebGL - DORET & FORESTIER <!-- omit in toc -->

**Auteurs : \
DORET Bastien \
FORESTIER Louis**

## Sommaire <!-- omit in toc -->

- [Fonctionnalités implantés](#fonctionnalités-implantés)
  - [Jalon 1](#jalon-1)
  - [Jalon 2](#jalon-2)
  - [Bonus](#bonus)
- [Description de l'archive](#description-de-larchive)

## Fonctionnalités implantés

Dans cette séction, nous allons détailler les fonction implantés dans notre rendu, ils seront classés par jalon.

### Jalon 1

- Réflection
- Réfraction, avec possibilité de jouer sur l'index de réfraction
- Réflection, Réfraction et effet de **Frensel**, avec possibilité de jouer sur l'index de réfraction

### Jalon 2

- Ajout de l'équation de **Cook & Torrence**, avec possibilité de jouer sur la rugosité de l'objet et l'intensité de la lumière

### Bonus

- Possibilité de changer d'objet
- Possibilité de changer la skybox
- Possibilité d'avoir un objet réfléchissant un skybox différente que celle de la scène

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
    - **wire.fs** : fragment shader pour afficher les triangles d'un objet
    - **wire.vs** : vertex shader pour afficher les triangles d'un objet
  - *skyboxes*
    - *blue_space* : dossier contenant les textures de la skybox du même nom
    - *lake* : dossier contenant les textures de la skybox du même nom
    - *red_space* : dossier contenant les textures de la skybox du même nom
    - *room* : dossier contenant les textures de la skybox du même nom
    - *snowy_mountain* : dossier contenant les textures de la skybox du même nom
    - *test* : dossier contenant les textures de la skybox du même nom
  - *src*
    - **callbacks.js** : Contient toutes les fonctions interagissant avec le fenêtre WebGL
    - **front.js** : Contient toutes les fonctions interagissant avec la page web en elle même
    - **glCourseBasis.js** : Contient toutes les fonctions permettant de mettre en place l'environnement WebGL
    - **glMatrix.js** : Contient toutes les fonctions permettant d'utiliser des matrices
    - **objLoader.js** : Contient toutes les fonctions permettant de créer une Mesh a partir d'un fichier **.obj**
  - **main.css** : le fichier css de la page Web
  - **main.html** : Page web contenant la fenêtre WebGL

Pour les skyboxes, elle sont toute formalisé de la même manière, elle contiennent toutes 6 images réparties de la sorte :
- pos-x => right
- neg-x => left
- pos-y  => top
- neg-y => bottom
- pos-z => front
- neg-z => back