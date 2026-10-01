function destroyListingDataTable(table) {
    if ($.fn.DataTable.isDataTable(table)) {
        var dataTable = $(table).DataTable();
        $(table).data("listingSearch", dataTable.search());
        $(table).data("listingOrder", dataTable.order());
        dataTable.destroy();
    }
}

function initializeListingDataTable(table, actionColumn, order) {
    var dataTable = $(table).DataTable({
        paging: false,
        info: false,
        lengthChange: false,
        order: $(table).data("listingOrder") || order || [],
        columnDefs: actionColumn == null ? [] : [{ targets: actionColumn, orderable: false, searchable: false }],
        language: {
            search: "لټون:",
            zeroRecords: "هیڅ معلومات ونه موندل سول.",
            emptyTable: "هیڅ معلومات ونه موندل سول."
        }
    });
    var search = $(table).data("listingSearch");
    if (search) {
        dataTable.search(search).draw();
    }
}
