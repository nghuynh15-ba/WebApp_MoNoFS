# WebApp MoNoFS — Website bán hàng ASP.NET MVC

Website thương mại điện tử gồm giới thiệu sản phẩm, giỏ hàng, đặt hàng, yêu thích, blog và liên hệ.

## Chức năng

- Đăng ký / đăng nhập (Forms Authentication, mật khẩu băm BCrypt)
- Danh mục và sản phẩm, nhiều ảnh cho mỗi sản phẩm, đánh giá sản phẩm
- Giỏ hàng, đặt hàng và chi tiết đơn hàng
- Danh sách yêu thích (Wishlist)
- Blog, trang giới thiệu, liên hệ
- Nhật ký hoạt động khách hàng (`CustomerLog`)

## Công nghệ

ASP.NET MVC 5 · .NET Framework 4.7.2 · Entity Framework 6 (Database First, file `.edmx`) · SQL Server · BCrypt.Net

## Mô hình dữ liệu chính

`Customer`, `Category`, `Product`, `ProductImage`, `Review`, `Wishlist`, `Order`, `OrderDetail`, `BlogPost`, `ContactRequest`, `CustomerLog`

## Chạy thử ở máy

1. Mở `WebApp_MoNoFS.sln` bằng Visual Studio.
2. Tạo CSDL `MoNoFSDB` trên SQL Server, rồi sửa chuỗi kết nối `MoNoFSDB` và `MoNoFSDBEntities` trong `Web.config` cho đúng tên máy của bạn.
3. Restore NuGet và nhấn F5.

## Vai trò của tôi

> _Điền vào: bạn phụ trách phần nào._
