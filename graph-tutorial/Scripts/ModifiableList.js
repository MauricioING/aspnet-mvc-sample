$(document).ready(function () {
    // Method to take the value of the input, add it to the list,
    // then clear the input.
    $('.new-item-input').bind('addItemAndClear', function (e) {
        // Get the list group
        let newItemRaw = $(this).val();
        let newItem = escapeUserInput(newItemRaw);

        if (newItem && newItem.length > 0) {
            let name = $(this).attr('data-name');
            var newInput = $(`<input type="text" class="form-control existing-item-input" name="${name}" value="${newItem}" />`);
            var removeButton = $('<div class="input-group-append"><button type="button" class="btn btn-outline-secondary remove-button"><span>&times;</span></button></div>');
            removeButton.click(removeItemFromList);
            var newInputGroup = $('<div class="input-group mb-2"></div>');

            newInputGroup.append(newInput, removeButton);

            $(this).parent().before(newInputGroup);

            // Clear the input
            $(this).val('');
        }
    });

    // Remove item when 'x' is clicked
    $('.remove-button').click(removeItemFromList);

    // Add item if '+' is clicked
    $('.add-new-item').click(function () {
        let input = $(this).closest('.input-group').children('.new-item-input');
        input.trigger('addItemAndClear');
    });

    // Prevent form submission if enter is pressed
    // in an existing item
    $('.existing-item-input').keypress(function (e) {
        if (e.keyCode === 13) { e.preventDefault(); }
    });

    // Prevent form submission if enter is pressed
    // in the new item input
    $('.new-item-input').keypress(function (e) {
        if (e.keyCode === 13) { e.preventDefault(); }
    });

    // When enter is released, add the new value to
    // the list.
    $('.new-item-input').keyup(function (e) {
        if (e.keyCode === 13) {
            $(this).trigger('addItemAndClear');
            e.preventDefault();
        }
    });
});

function removeItemFromList() {
    $(this).closest('.input-group').remove();
}
