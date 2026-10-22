// Menu akcji (trzykropek) stoi w komórce tabeli opakowanej w .table-responsive,
// czyli w kontenerze z overflow-x. Domyślne pozycjonowanie absolutne powoduje,
// że rozwinięte menu jest przez ten kontener obcinane - strategia "fixed"
// wyrywa je z przewijanego obszaru.
document.addEventListener('DOMContentLoaded', function () {
    var toggles = document.querySelectorAll('.table-responsive [data-bs-toggle="dropdown"]');
    Array.prototype.forEach.call(toggles, function (toggle) {
        bootstrap.Dropdown.getOrCreateInstance(toggle, {
            popperConfig: { strategy: 'fixed' }
        });
    });
});
