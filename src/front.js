function readyDocument() {
    var acc = document.getElementsByClassName("accordion");
    var slider = document.getElementById("refractIndex");
    var output = document.getElementById("value");
    var i;

    // Code éxécuter quand l'on clique sur la accordion, permet de le déplier
    for(i = 0; i < acc.length; i++){
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
    acc = document.getElementById("objectChoice");
    acc.addEventListener("change", function() {
        OBJ1 = new objmesh(this.value);
    });


    // Code éxécuter à chaque changement de la liste déroulante des shaders
    acc = document.getElementById("shaderChoice");
    acc.addEventListener("change", function() {
        OBJ1.shaderState = this.value;
        var slider = this.nextElementSibling;
        if(this.value == 2 || this.value == 1 || this.value == 4){
            slider.classList.remove("hidden");
        }
        else {
            slider.classList.add("hidden");
        }
    });

    // Code éxécuter à chaque changement du color picker de couleur spéculaire
    acc = document.getElementById("colDiff");
    acc.addEventListener("change", function() {
        // A faire : quand la couleur diffuse change
        console.log("Nouvelle couleur diff");
    });

    // Code éxécuter à chaque changement du color picker de couleur diffuse
    acc = document.getElementById("colSpec");
    acc.addEventListener("change", function() {
        // A faire : quand la couleur spéculaire change
        console.log("Nouvelle couleur spec");
    });

    // Code éxécuter à chaque fois que la checkbox "weird skybox" est cliqué
    acc = document.getElementById("weirdSkybox");
    acc.addEventListener("click", function() {
        var div = document.getElementById("secondSkybox");
        var choice1 = document.getElementById("skyboxChoice");
        var choice2 = document.getElementById("skyboxChoice2");
        choice2.value = choice1.value;
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

    // Code éxécuter à chaque fois que l'on change la valeur de la skybox
    acc = document.getElementById("skyboxChoice");
    acc.addEventListener("change", function() {
        console.log(this.value)
        CUBEMAP.setTexture(this.value);
        if(!document.getElementById("weirdSkybox").checked)
        {
            console.log("set texture of skybox and obj");
            OBJ1.setTexture(this.value);
        }
    });

    // Code éxécuter à chaque fois que l'on change la valeur de la skybox de l'objet
    acc = document.getElementById("skyboxChoice2");
    acc.addEventListener("change", function() {
        console.log("set texture of obj");
        OBJ1.setTexture(this.value);
    });

    // Affecte au champ nombre la valeur du slider
    output.value = slider.value;

    // Code permettant de mettre à jour le slider quand on entre un nombre dans l'input number
    slider.oninput = function() {
        output.value = this.value;
        OBJ1.refractIndex = this.value;
    }

    // Code permettant de mettre à jour l'input nom quand le slider change
    output.oninput = function() {
        slider.value = this.value;
        OBJ1.refractIndex = this.value;
    }
}
