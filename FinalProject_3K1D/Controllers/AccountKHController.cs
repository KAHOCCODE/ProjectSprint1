﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FinalProject_3K1D.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using Microsoft.AspNetCore.Identity;
using FinalProject_3K1D.ViewModels;

namespace FinalProject_3K1D.Controllers
{
    public class AccountKHController : Controller
    {
        private readonly QlrapPhimContext _context;

        public AccountKHController(QlrapPhimContext context)
        {
            _context = context;
        }

        #region
        [HttpGet]
        public IActionResult Register(string? ReturnUrl)
        {
            ViewBag.ReturnUrl = ReturnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterKH model)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra xem tên người dùng đã tồn tại chưa
                var existingUser = await _context.KhachHangs
                    .FirstOrDefaultAsync(kh => kh.UserKh == model.UserKH);

                if (existingUser != null)
                {
                    ModelState.AddModelError("UserKH", "Username already exists.");
                    return View(model);
                }

                // Kiểm tra xem SDT đã tồn tại chưa
                var existingSdt = await _context.KhachHangs
                    .FirstOrDefaultAsync(kh => kh.Sdt == model.SDT);

                if (existingSdt != null)
                {
                    ModelState.AddModelError("SDT", "Phone number already exists.");
                    return View(model);
                }

                // Kiểm tra xem CCCD đã tồn tại chưa
                var existingCccd = await _context.KhachHangs
                    .FirstOrDefaultAsync(kh => kh.Cccd == model.CCCD);

                if (existingCccd != null)
                {
                    ModelState.AddModelError("CCCD", "Identity card number already exists.");
                    return View(model);
                }

                // Kiểm tra xem Email đã tồn tại chưa
                var existingEmail = await _context.KhachHangs
                    .FirstOrDefaultAsync(kh => kh.Email == model.Email);

                if (existingEmail != null)
                {
                    ModelState.AddModelError("Email", "Email already exists.");
                    return View(model);
                }

                // Tạo người dùng mới và thêm vào cơ sở dữ liệu
                string newId = "KH01"; // Default ID if no users exist
                var lastUser = await _context.KhachHangs
                    .OrderByDescending(kh => kh.IdKhachHang)
                    .FirstOrDefaultAsync();

                if (lastUser != null)
                {
                    string lastId = lastUser.IdKhachHang;
                    if (lastId.Length > 2)
                    {
                        string lastIdPart = lastId.Substring(2);
                        if (int.TryParse(lastIdPart, out int lastIdNumber))
                        {
                            newId = $"KH{(lastIdNumber + 1):D2}";
                        }
                        else
                        {
                            ModelState.AddModelError("", "Error generating ID.");
                            return View(model);
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("", "Error generating ID: ID format is invalid.");
                        return View(model);
                    }
                }

                var newUser = new KhachHang
                {
                    IdKhachHang = newId,
                    HoTen = model.HoTen,
                    NgaySinh = model.NgaySinh,
                    Sdt = model.SDT,
                    Cccd = model.CCCD,
                    Email = model.Email,
                    UserKh = model.UserKH,
                    PassKh = model.PassKH
                };

                try
                {
                    await _context.KhachHangs.AddAsync(newUser);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Registration successful! Please log in.";
                    return RedirectToAction("Login", "AccountKH");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    ModelState.AddModelError("", "An error occurred while saving the user. Please try again.");
                    return View(model);
                }
            }

            return View(model);
        }



        #endregion

        #region Login
        [HttpGet]
        public IActionResult Login(string? ReturnUrl)
        {
            ViewBag.ReturnUrl = ReturnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginKH model, string? ReturnUrl)
        {
            ViewBag.ReturnUrl = ReturnUrl;

            if (ModelState.IsValid)
            {
                if (model.Role == "KhachHang")
                {
                    // Logic đăng nhập cho Khách Hàng
                    var khachhang = await _context.KhachHangs
                        .SingleOrDefaultAsync(kh => kh.UserKh == model.UserKh && kh.PassKh == model.PassKh);

                    if (khachhang == null)
                    {
                        ModelState.AddModelError("", "Tài khoản hoặc mật khẩu không đúng.");
                        return View(model);
                    }

                    // Lưu thông tin người dùng vào session hoặc ClaimsPrincipal
                    HttpContext.Session.SetString("UserId", khachhang.IdKhachHang);
                    HttpContext.Session.SetString("UserName", khachhang.HoTen);
                    HttpContext.Session.SetString("UserRole", "KhachHang");

                    var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, khachhang.HoTen),
                new Claim(ClaimTypes.Email, khachhang.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, "KhachHang"),
                new Claim("UserId", khachhang.IdKhachHang) // Add user Id claim
            };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                    await HttpContext.SignInAsync(claimsPrincipal);

                    TempData["SuccessMessage"] = "Đăng nhập thành công!";
                    return RedirectToAction("Index", "Home");
                }
                else if (model.Role == "NhanVien")
                {
                    // Logic đăng nhập cho Nhân Viên
                    var nhanvien = await _context.NhanViens
                        .SingleOrDefaultAsync(nv => nv.UserNv == model.UserKh && nv.PassNv == model.PassKh);

                    if (nhanvien == null)
                    {
                        ModelState.AddModelError("", "Tài khoản hoặc mật khẩu không đúng.");
                        return View(model);
                    }

                    // Lưu thông tin người dùng vào session hoặc ClaimsPrincipal
                    HttpContext.Session.SetString("UserId", nhanvien.IdNhanVien);
                    HttpContext.Session.SetString("UserName", nhanvien.HoTen);
                    HttpContext.Session.SetString("UserRole", "NhanVien");

                    var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, nhanvien.HoTen),
                new Claim(ClaimTypes.Email, nhanvien.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, "NhanVien"),
                new Claim("UserId", nhanvien.IdNhanVien) // Add user Id claim
            };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                    await HttpContext.SignInAsync(claimsPrincipal);

                    TempData["SuccessMessage"] = "Đăng nhập thành công!";
                    return Redirect("/Admin");
                }
            }
            return View(model);
        }
 


#endregion

        #region đăng xuất 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear(); // Xóa tất cả session
            TempData["SuccessMessage"] = "Đăng xuất thành công!";
            return RedirectToAction("Index", "Home");
        }
        #endregion

        #region Profile
        public IActionResult Profile()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "AccountKH");
            }

            var khachHang = _context.KhachHangs.FirstOrDefault(kh => kh.IdKhachHang == userId);
            if (khachHang == null)
            {
                return RedirectToAction("Login", "AccountKH");
            }

            return View(khachHang);

        }

        [HttpPost]
        public IActionResult UpdateProfile(KhachHang updatedKhachHang)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "AccountKH");
            }

            var khachHang = _context.KhachHangs.FirstOrDefault(kh => kh.IdKhachHang == userId);
            if (khachHang == null)
            {
                return RedirectToAction("Login", "AccountKH");
            }

            // Cập nhật thông tin (trừ SDT và Điểm Tích Lũy)
            khachHang.HoTen = updatedKhachHang.HoTen;
            khachHang.NgaySinh = updatedKhachHang.NgaySinh;
            khachHang.Email = updatedKhachHang.Email;
            khachHang.Cccd = updatedKhachHang.Cccd;
            khachHang.DiaChi = updatedKhachHang.DiaChi;

            _context.Update(khachHang);
            _context.SaveChanges();

            return RedirectToAction("Profile");
        }

        #endregion

        #region ChangePassword
        // GET: Hiển thị form thay đổi mật khẩu
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        // POST: Xử lý yêu cầu thay đổi mật khẩu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Lấy IdKhachHang từ session
            var userId = HttpContext.Session.GetString("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            // Lấy thông tin khách hàng từ cơ sở dữ liệu
            var khachhang = await _context.KhachHangs
                .SingleOrDefaultAsync(kh => kh.IdKhachHang == userId);

            if (khachhang == null)
            {
                ModelState.AddModelError("", "Người dùng không tồn tại.");
                TempData["ErrorMessage"] = "Người dùng không tồn tại.";
                return View(model);
            }

            // Kiểm tra mật khẩu hiện tại
            if (khachhang.PassKh != model.OldPassword)
            {
                ModelState.AddModelError("", "Mật khẩu hiện tại không đúng.");
                TempData["ErrorMessage"] = "Mật khẩu hiện tại không đúng.";
                return View(model);
            }

            // Cập nhật mật khẩu mới
            khachhang.PassKh = model.NewPassword;
            _context.KhachHangs.Update(khachhang);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Mật khẩu đã được thay đổi thành công!";
            return RedirectToAction("Index", "Home");
        }
        #endregion


    }
}


