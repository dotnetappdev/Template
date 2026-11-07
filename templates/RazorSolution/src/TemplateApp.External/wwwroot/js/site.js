// Site-wide JavaScript (jQuery __JQUERY_VERSION__ + jQuery UI __JQUERYUI_VERSION__).
// Pages can add their own scripts with @section Scripts { ... }.
$(function () {
    'use strict';

    // jQuery UI date picker on any input with the js-datepicker class.
    $('.js-datepicker').datepicker({
        dateFormat: 'dd/mm/yy',
        changeMonth: true,
        changeYear: true
    });

    // jQuery UI dialog: <button class="js-open-dialog" data-dialog="#my-dialog">
    $('.js-open-dialog').on('click', function () {
        var $dialog = $($(this).data('dialog'));
        $dialog.removeClass('d-none').dialog({
            modal: true,
            width: 420,
            buttons: {
                Ok: function () {
                    $(this).dialog('close');
                }
            }
        });
    });

    // jQuery UI autocomplete fed by a Razor Page handler (for example Pages/Customers/Index.cshtml.cs).
    $('.js-customer-autocomplete').each(function () {
        var $input = $(this);
        $input.autocomplete({
            minLength: 1,
            source: $input.data('source-url'),
            select: function (event, ui) {
                $input.val(ui.item.value);
                $input.closest('form').trigger('submit');
            }
        });
    });
//#if (UseColorModes)

    // Colour mode button: automatic (follows the device) -> light -> dark. Remembered in localStorage.
    (function () {
        var order = ['auto', 'light', 'dark'];
        var names = { auto: 'automatic', light: 'light', dark: 'dark' };
        var media = window.matchMedia('(prefers-color-scheme: dark)');
        function stored() { try { return localStorage.getItem('theme') || 'auto'; } catch (e) { return 'auto'; } }
        function apply(theme) {
            var dark = theme === 'dark' || (theme === 'auto' && media.matches);
            document.documentElement.setAttribute('data-bs-theme', dark ? 'dark' : 'light');
            $('.js-theme-toggle').attr({ title: 'Colour mode: ' + names[theme], 'aria-label': 'Colour mode: ' + names[theme] });
        }
        $('.js-theme-toggle').on('click', function () {
            var next = order[(order.indexOf(stored()) + 1) % order.length];
            try { localStorage.setItem('theme', next); } catch (e) { }
            apply(next);
        });
        media.addEventListener('change', function () { if (stored() === 'auto') { apply('auto'); } });
        apply(stored());
    })();
//#endif
//#if (IncludeDataTables)

    // DataTables: sorting, paging and searching for <table class="js-datatable">.
    $('table.js-datatable').each(function () {
        new DataTable(this, { pageLength: 25 });
    });
//#endif
//#if (IncludeSelect2)

    // Select2: searchable drop-downs for <select class="js-select2">.
    $('select.js-select2').each(function () {
        var $select = $(this);
        $select.select2({
//#if (!Bootstrap4)
            theme: 'bootstrap-5',
//#endif
            width: '100%',
            placeholder: $select.find('option[value=""]').text() || 'Select…',
            allowClear: true
        });
    });
//#endif
//#if (IncludeToastr)

    // Toastr notifications (TempData["Message"] is shown with toastr.success in _Layout).
    toastr.options = { positionClass: 'toast-top-right', timeOut: 4000, progressBar: true };
//#endif

    // Confirm before submitting <form class="js-confirm" data-confirm="Are you sure?">.
    $(document).on('submit', 'form.js-confirm', function (e) {
        var form = this;
        if (form.dataset.confirmed) {
            return;
        }
        e.preventDefault();
//#if (IncludeSweetAlert)
        Swal.fire({
            title: form.dataset.confirm || 'Are you sure?',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Yes',
            confirmButtonColor: '#dc3545'
        }).then(function (result) {
            if (result.isConfirmed) {
                form.dataset.confirmed = 'true';
                form.submit();
            }
        });
//#else
        if (window.confirm(form.dataset.confirm || 'Are you sure?')) {
            form.dataset.confirmed = 'true';
            form.submit();
        }
//#endif
    });
});
