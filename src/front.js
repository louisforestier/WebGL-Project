function readyDocument() {
    var acc = document.getElementsByClassName("accordion");
    var slider = document.getElementById("refractIndex");
    var output = document.getElementById("value");
    var i;

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

    acc = document.getElementById("objectChoice");
    acc.addEventListener("click", function() {
        OBJ1 = new objmesh(this.value);
    });

    acc = document.getElementById("shaderChoice");
    acc.addEventListener("click", function() {
        OBJ1.shaderState = this.value;
        var slider = this.nextElementSibling;
        if(this.value == 2 || this.value == 1){
            slider.classList.remove("hidden");
        }
        else {
            slider.classList.add("hidden");
        }
    });

    acc = document.getElementById("weirdSkybox");
    acc.addEventListener("click", function() {
        var weird = document.getElementById("secondSkybox");
        weird.classList.toggle("hidden");
    });

    acc = document.getElementById("skyboxChoice");
    acc.addEventListener("click", function() {
        CUBEMAP = new cubemap(this.value);
    });

    acc = document.getElementById("secondSkybox");
    acc.addEventListener("click", function() {
        // Choix a faire pour la texture de la skybox modifié
    });

    output.value = slider.value;

    slider.oninput = function() {
        output.value = this.value;
        OBJ1.refractIndex = this.value;
    }

    output.oninput = function() {
        slider.value = this.value;
        OBJ1.refractIndex = this.value;
    }
}
