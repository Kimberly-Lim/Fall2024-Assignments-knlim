console.log("script.js is loaded and running!");

function apiSearch(redirectToFirst = false) {
    var query = document.getElementById('query').value;
    var params = {
        'q': $('#query').val(),
        'count': 50,
        'offset': 0,
        'mkt': 'en-us'
    };

    $.ajax({
        url: 'https://api.bing.microsoft.com/v7.0/search?' + $.param(params),
        type: 'GET',
        headers: {
            'Ocp-Apim-Subscription-Key': 'c936b43666a44e028ea8e9d5aba57293'
        }
    })
        .done(function (data) {
            if (redirectToFirst && data.webPages && data.webPages.value.length > 0) {
                // Redirect to the first result if "I'm Feeling Lucky" is clicked
                window.location.href = data.webPages.value[0].url;
            } else {
                var len = data.webPages.value.length;
                var results = '';
                for (i = 0; i < len; i++) {
                    results += `<p><a href="${data.webPages.value[i].url}">${data.webPages.value[i].name}</a>: ${data.webPages.value[i].snippet}</p>`;
                }

                $('#searchResults').html(results);
                $('#searchResults').dialog();
                $('#searchResults').css('display', 'block');
            }
        })
        .fail(function () {
            alert('Error with the search API.');
        });
}

// Search button click
$('#searchBtn').click(function () {
    apiSearch(false); // Regular search
});

// "I'm Feeling Lucky" button click
$('#luckyBtn').click(function () {
    apiSearch(true); // Redirect to first result
});

function showTime() {
    const now = new Date();
    const hours = now.getHours().toString().padStart(2, '0');
    const minutes = now.getMinutes().toString().padStart(2, '0');
    $('#time').html(`${hours}:${minutes}`).css('visibility', 'visible').dialog();
}

// Time button click
$('#timeBtn').click(function () {
    showTime();
});

$(document).ready(function () {
    // Array of background image URLs
    const backgroundImages = [
        'url(css/images/background.jpg)',
        'url(css/images/background2.jpg)',
        'url(css/images/background3.jpg)'
    ];

    let currentImageIndex = 0;

    // Function to change the background image
    function changeBackgroundImage() {
        // Increment the index and reset if it exceeds the array length
        currentImageIndex = (currentImageIndex + 1) % backgroundImages.length;

        // Change the background image
        $('body').css('background-image', backgroundImages[currentImageIndex]);
    }

    // Event listener for click on the search engine name
    $('.searchEngine').click(function () {
        changeBackgroundImage();
    });
});
