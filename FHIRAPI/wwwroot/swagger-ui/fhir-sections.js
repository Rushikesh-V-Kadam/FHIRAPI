// Swagger UI only has one level of sections. The FHIR resource endpoints are tagged
// "1. FHIR APIs - <ResourceType>" in the controllers; this script shows them as one
// "1. FHIR APIs" section with one sub-section per resource type (title = resource type only).
// Swagger UI re-renders often (expand, filter), so the page is re-checked after every change.
(function () {
    var PREFIX = "1. FHIR APIs - ";
    var GROUP = "1. FHIR APIs";
    var collapsed = false;
    var scheduled = false;

    function tagOf(h3) {
        if (!h3.dataset.fhirTag) {
            h3.dataset.fhirTag = h3.getAttribute("data-tag") || (h3.textContent || "").trim();
        }
        return h3.dataset.fhirTag;
    }

    function apply() {
        scheduled = false;
        var subs = [];
        document.querySelectorAll(".opblock-tag-section > h3.opblock-tag").forEach(function (h3) {
            var tag = tagOf(h3);
            if (tag.indexOf(PREFIX) !== 0) return;
            var section = h3.parentElement;
            section.setAttribute("data-fhir-sub", "1");
            subs.push(section);

            // show only the resource type as the sub-section title
            var span = h3.querySelector("a span") || h3.querySelector("span");
            var name = tag.substring(PREFIX.length);
            if (span && span.firstChild && span.firstChild.nodeValue !== name) {
                span.firstChild.nodeValue = name;
            }
        });

        var heading = document.querySelector(".fhir-group-heading");
        if (subs.length === 0) {
            if (heading) heading.remove();
            return;
        }
        if (!heading) {
            heading = document.createElement("h3");
            heading.className = "fhir-group-heading";
            heading.innerHTML = '<span></span>' +
                '<svg class="fhir-group-arrow" viewBox="0 0 20 20" aria-hidden="true">' +
                '<path d="M17.418 6.109c.272-.268.709-.268.979 0s.271.701 0 .969l-7.908 7.83c-.27.268-.707.268-.979 0l-7.908-7.83c-.27-.268-.27-.701 0-.969.271-.268.709-.268.979 0L10 13.25l7.418-7.141z"/></svg>';
            heading.firstChild.textContent = GROUP;
            heading.addEventListener("click", function () {
                collapsed = !collapsed;
                document.documentElement.classList.toggle("fhir-group-collapsed", collapsed);
            });
        }
        // keep the heading right above the first resource sub-section
        if (heading.nextElementSibling !== subs[0]) {
            subs[0].parentNode.insertBefore(heading, subs[0]);
        }
    }

    function schedule() {
        if (scheduled) return;
        scheduled = true;
        window.requestAnimationFrame(apply);
    }

    function start() {
        new MutationObserver(schedule).observe(document.body, { childList: true, subtree: true });
        schedule();
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", start);
    } else {
        start();
    }
})();
