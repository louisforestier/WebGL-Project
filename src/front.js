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
    acc.addEventListener("change", function() {
        OBJ1 = new objmesh(this.value);
    });

    acc = document.getElementById("shaderChoice");
    acc.addEventListener("change", function() {
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

    acc = document.getElementById("skyboxChoice2");
    acc.addEventListener("change", function() {
        console.log("set texture of obj");
        OBJ1.setTexture(this.value);
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
