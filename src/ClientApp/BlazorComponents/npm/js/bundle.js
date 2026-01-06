function hide(elements) {
    elements = elements.length ? elements : [elements];
    for (var index = 0; index < elements.length; index++) {
        elements[index].style.display = 'none';
    }
}

async function expandMenu() {
    var ele = document.getElementsByClassName('mud-nav-link', 'mud-ripple', 'active');
    var c = ele[0].parentElement.getAttribute('aria-label');
    alert(c);
}