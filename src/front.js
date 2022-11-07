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
