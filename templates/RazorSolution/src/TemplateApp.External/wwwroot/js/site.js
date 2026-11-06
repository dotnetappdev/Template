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

    // jQuery UI autocomplete fed by a Razor Page handler (see Pages/Customers/Index.cshtml.cs).
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
});
