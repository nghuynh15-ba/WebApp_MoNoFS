

// 1. Scroll To Top Button
var mybutton = document.getElementById("scrollToTopBtn");

window.onscroll = function () { scrollFunction() };

function scrollFunction() {
    if (mybutton) {
        if (document.body.scrollTop > 200 || document.documentElement.scrollTop > 200) {
            mybutton.style.display = "block";
        } else {
            mybutton.style.display = "none";
        }
    }
}

if (mybutton) {
    mybutton.addEventListener("click", function (e) {
        e.preventDefault();
        window.scrollTo({
            top: 0,
            behavior: 'smooth'
        });
    });
}

// 2. AJAX Cart Logic
// Chạy sau khi toàn bộ trang đã tải
document.addEventListener("DOMContentLoaded", function () {

    // Hàm helper để gửi AJAX (dùng Fetch API)
    async function postToCart(url, data) {

        // Lấy Token chống giả mạo (CSRF Token) từ form Logout
        // Đây là cách để làm AJAX an toàn với AntiForgeryToken
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        let token = "";
        if (tokenInput) {
            token = tokenInput.value;
        }

        try {
            const response = await fetch(url, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    // Thêm Token vào header
                    'RequestVerificationToken': token
                },
                body: JSON.stringify(data)
            });

            if (!response.ok) {
                throw new Error('Network response was not ok');
            }

            return await response.json();
        } catch (error) {
            console.error('Lỗi khi gửi AJAX:', error);
            alert("Đã xảy ra lỗi. Vui lòng thử lại.");
        }
    }

    // Bắt sự kiện cho các nút "Add to Cart" trên TRANG CHỦ / TRANG SẢN PHẨM
    document.querySelectorAll('.btn-add-to-cart-ajax').forEach(button => {
        button.addEventListener('click', async function (e) {
            e.preventDefault(); // Ngăn link chạy

            const productId = this.getAttribute('data-product-id');

            const data = { productId: productId, quantity: 1 };
            const result = await postToCart('/Cart/AddToCart', data);

            if (result && result.success) {
                alert(result.message); 
                document.getElementById('cart-quantity').innerText = result.totalQuantity;
            } else if (result) {
                alert(result.message); // Thông báo lỗi (ví dụ: cần đăng nhập)
            }
        });
    });

    // Bắt sự kiện cho nút "Add to Cart" trên TRANG DETAIL (có số lượng)
    const detailButton = document.getElementById('btn-add-to-cart-detail');
    if (detailButton) {
        detailButton.addEventListener('click', async function (e) {
            e.preventDefault(); 

            const productId = this.getAttribute('data-product-id');
            const quantity = document.getElementById('quantity-input').value;

            const data = { productId: productId, quantity: parseInt(quantity) };
            const result = await postToCart('/Cart/AddToCart', data);

            if (result && result.success) {
                alert(result.message); 
                document.getElementById('cart-quantity').innerText = result.totalQuantity;
            } else if (result) {
                alert(result.message); // Thông báo lỗi
            }
        });
    }
});

document.addEventListener("DOMContentLoaded", function () {

    
    window.addEventListener('scroll', function () {

       
        if (window.scrollY > 50) {
            
            document.body.classList.add('scrolled');
        } else {
            document.body.classList.remove('scrolled');
        }
    });
});


 document.addEventListener("DOMContentLoaded", function() {
        flatpickr("#booking-date", {
            theme: "dark",          // Giao diện tối
            dateFormat: "d/m/Y",    // Định dạng ngày Việt Nam (Ngày/Tháng/Năm)
            minDate: "today",       // Không cho chọn ngày quá khứ
            disableMobile: "true"   // Bắt buộc dùng giao diện đẹp này trên cả điện thoại
        });
    });


// Đặt đoạn mã này trong thẻ <script> (sau khi đã nhúng jQuery)
$(document).ready(function () {
    // Lắng nghe sự kiện gõ phím trên input field có id="txtSearch"
    $("#txtSearch").on("keyup", function () {
        var searchTerm = $(this).val();
        var suggestionsContainer = $('#suggestionsContainer');

        // Chỉ thực hiện AJAX nếu chuỗi tìm kiếm có ít nhất 2 ký tự (hoặc tùy chỉnh)
        if (searchTerm.length > 1) {
            // Gửi yêu cầu AJAX đến Action Method GetSuggestions trong ProductController
            $.ajax({
                url: '/Product/GetSuggestions',
                type: 'GET',
                // Gửi dữ liệu: tên tham số 'term' phải khớp với tham số trong Controller
                data: { term: searchTerm },
                success: function (data) {
                    suggestionsContainer.empty(); // Xóa các gợi ý cũ

                    if (data && data.length > 0) {
                        // Tạo list (ul) để chứa các gợi ý
                        var ul = $('<ul>').css({
                            'list-style-type': 'none',
                            'padding': 0,
                            'margin': 0,
                            'border': '1px solid #ccc',
                            'background-color': '#fff',
                            'box-shadow': '0 4px 8px rgba(0,0,0,0.1)' // Thêm bóng đổ cho đẹp
                        });

                        // Duyệt qua mảng JSON nhận được (danh sách tên sản phẩm)
                        $.each(data, function (index, item) {
                            // Tạo các item gợi ý (li)
                            $('<li>')
                                .text(item)
                                .css({ 'padding': '8px 10px', 'cursor': 'pointer' })
                                // Hiệu ứng hover
                                .hover(function () {
                                    $(this).css('background-color', '#f0f0f0');
                                }, function () {
                                    $(this).css('background-color', '#fff');
                                })
                                // Xử lý khi click vào một gợi ý
                                .click(function () {
                                    // Đặt tên sản phẩm được chọn vào ô input
                                    $("#txtSearch").val(item);
                                    suggestionsContainer.empty();
                                    // Tùy chọn: Tự động submit form sau khi chọn
                                    $("#txtSearch").closest('form').submit();
                                })
                                .appendTo(ul);
                        });
                        // Đưa list gợi ý vào container
                        ul.appendTo(suggestionsContainer);
                    } else {
                        suggestionsContainer.empty(); // Không có kết quả, xóa container
                    }
                }
            });
        } else {
            suggestionsContainer.empty(); // Xóa gợi ý nếu chuỗi nhập quá ngắn
        }
    });
});