function getColor(value){
    return [parseInt(value.substring(1,3), 16)/255,
                     parseInt(value.substring(3,5), 16)/255,
                     parseInt(value.substring(5), 16)/255];
}

function readyDocument() {
    // Récupération des accordions de la page web
    var acc = document.getElementsByClassName("accordion");
    
    // Récupération du slider et de l'input number de l'indice de réfraction
    var sliderRef = document.getElementById("refractIndex");
    var outputRef = document.getElementById("valueRef");
    
    // Récupération du slider et de l'input number de la rugosité
    var sliderReg = document.getElementById("rugosity");
    var outputReg = document.getElementById("valueReg");

    // Récupération du slider et de l'input number de l'intensité de la lumière
    var sliderInt = document.getElementById("intensity");
    var outputInt = document.getElementById("valueInt");
    
    // Récupération des listes déroulantes des objets, des shaders, 
    // de la skybox et de la skybox réfléchi par l'objet
    var object = document.getElementById("objectChoice");
    var shader = document.getElementById("shaderChoice");
    var skybox = document.getElementById("skyboxChoice");
    var objSkybox = document.getElementById("skyboxChoice2");

    // Récupération de la couleur du color picker
    var color = document.getElementById("colDiff");
    
    // Récupération de des checkbox
    var checkboxWS = document.getElementById("weirdSkybox");
    var checkboxP = document.getElementById("plane")
    
    // Code éxécuter quand l'on clique sur la accordion, permet de le déplier
    for(var i = 0; i < acc.length; i++){
        acc[i].addEventListener("click", function() {
            this.classList.toggle("active");
            var panel = this.nextElementSibling;
            if (panel.style.maxHeight) {
                panel.style.maxHeight = null;
            } else {
                panel.style.maxHeight = panel.scrollHeight + "px";
            } 
        });
    }

    // Code éxécuter à chaque changement de la liste déroulante des objets
    object.addEventListener("change", function() {
        OBJ1 = new objmesh(this.value);
        OBJ1.shaderState = shader.value;
        if(checkboxWS.checked){
            OBJ1.setTexture(objSkybox.value);
        }
        else {
            OBJ1.setTexture(CUBEMAP.skyboxName);
        }
        OBJ1.refractIndex = sliderRef.value;
        OBJ1.rugosity = sliderReg.value;
        OBJ1.lightIntensity = sliderInt.value
        OBJ1.setColor(getColor(color.value));
    });


    // Code éxécuter à chaque changement de la liste déroulante des shaders
    shader.addEventListener("change", function() {
        OBJ1.shaderState = this.value;
        if(this.value == 2 || this.value == 1 || this.value == 4 || this.value == 5){
            sliderRef.parentElement.classList.remove("hidden");
        }
        else {
            sliderRef.parentElement.classList.add("hidden");
        }
        if(this.value == 4 || this.value == 5){
            sliderReg.parentElement.classList.remove("hidden");
            sliderInt.parentElement.classList.remove("hidden");
        }
        else {
            sliderReg.parentElement.classList.add("hidden");
            sliderInt.parentElement.classList.add("hidden");
        }
    });

    // Code éxécuter à chaque changement du color picker de couleur spéculaire
    color.addEventListener("change", function() {
        console.log(getColor(this.value));
        OBJ1.setColor(getColor(this.value));
    });

    // Code éxécuter à chaque fois que la checkbox "weird skybox" est cliqué
    checkboxWS.addEventListener("click", function() {
        var div = document.getElementById("secondSkybox");
        objSkybox.value = skybox.value;
        if(this.checked)
        {
            div.classList.remove("hidden");
        }
        else 
        {
            div.classList.add("hidden"); 
            OBJ1.setTexture(CUBEMAP.skyboxName); 
        }
    });

    // Code éxécuter à chaque fois que la checkbox "weird skybox" est cliqué
    checkboxP.addEventListener("click", function() {
        DRAWPLANE = this.checked;
    });

    // Code éxécuter à chaque fois que l'on change la valeur de la skybox
    skybox.addEventListener("change", function() {
        console.log(this.value)
        CUBEMAP.setTexture(this.value);
        if(!checkboxWS.checked)
        {
            console.log("set texture of skybox and obj");
            OBJ1.setTexture(this.value);
        }
    });

    // Code éxécuter à chaque fois que l'on change la valeur de la skybox de l'objet
    objSkybox.addEventListener("change", function() {
        console.log("set texture of obj");
        OBJ1.setTexture(this.value);
    });

    // Affecter les valeurs pour le coefficient de refraction
    // Affecte au champ nombre la valeur du slider
    outputRef.value = sliderRef.value;

    // Code permettant de mettre à jour le slider quand on entre un nombre dans l'input number
    sliderRef.oninput = function() {
        outputRef.value = this.value;
        OBJ1.refractIndex = this.value;
    }

    // Code permettant de mettre à jour l'input nom quand le slider change
    outputRef.oninput = function() {
        sliderRef.value = this.value;
        OBJ1.refractIndex = this.value;
    }

    // Affecter les valeurs pour la Rugosité
    // Affecte au champ nombre la valeur du slider
    outputReg.value = sliderReg.value;

    // Code permettant de mettre à jour le slider quand on entre un nombre dans l'input number
    sliderReg.oninput = function() {
        outputReg.value = this.value;
        OBJ1.rugosity = this.value;
    }

    // Code permettant de mettre à jour l'input nom quand le slider change
    outputReg.oninput = function() {
        sliderReg.value = this.value;
        OBJ1.rugosity = this.value;
    }

    // Affecter les valeurs pour l'intensité de la lumière
    // Affecte au champ nombre la valeur du slider
    outputInt.value = sliderInt.value;

    // Code permettant de mettre à jour le slider quand on entre un nombre dans l'input number
    sliderInt.oninput = function() {
        outputInt.value = this.value;
        OBJ1.lightIntensity = this.value;
    }

    // Code permettant de mettre à jour l'input nom quand le slider change
    outputInt.oninput = function() {
        sliderInt.value = this.value;
        OBJ1.lightIntensity = this.value;
    }
}
